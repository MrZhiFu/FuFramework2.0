using System;
using System.Buffers;
using System.IO.Compression;
using System.Text;
using FuFramework.SuperSocket.ProtoBase;

namespace FuFramework.SuperSocket.WebSocket.Extensions.Compression;

/// <summary>
/// WebSocket Per-Message Compression Extension
/// https://tools.ietf.org/html/rfc7692
/// </summary>
public class WebSocketPerMessageCompressionExtension : IWebSocketExtension
{
	public const string PMCE = "permessage-deflate";

	private const int _deflateBufferSize = 4194304;

	private static readonly Encoding _encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

	private static readonly byte[] LAST_FOUR_OCTETS = new byte[4] { 0, 0, 255, 255 };

	private static readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;

	public string Name => "permessage-deflate";

	public void Decode(WebSocketPackage package)
	{
		if (!package.RSV1)
		{
			return;
		}
		ReadOnlySequence<byte> first = package.Data;
		first = first.ConcatSequence(new SequenceSegment(LAST_FOUR_OCTETS, LAST_FOUR_OCTETS.Length, pooled: false));
		SequenceSegment sequenceSegment = null;
		SequenceSegment sequenceSegment2 = null;
		using (DeflateStream deflateStream = new DeflateStream(new ReadOnlySequenceStream(first), CompressionMode.Decompress))
		{
			while (true)
			{
				byte[] array = _arrayPool.Rent(4194304);
				int num = deflateStream.Read(array, 0, array.Length);
				if (num == 0)
				{
					break;
				}
				SequenceSegment sequenceSegment3 = new SequenceSegment(array, num);
				if (sequenceSegment == null)
				{
					sequenceSegment2 = (sequenceSegment = sequenceSegment3);
				}
				else
				{
					sequenceSegment2.SetNext(sequenceSegment3);
				}
			}
		}
		package.Data = new ReadOnlySequence<byte>(sequenceSegment, 0, sequenceSegment2, sequenceSegment2.Memory.Length);
	}

	public void Encode(WebSocketPackage package)
	{
		package.RSV1 = true;
		if (package.Data.IsEmpty)
		{
			EncodeTextMessage(package);
		}
		else
		{
			EncodeDataMessage(package);
		}
	}

	private void EncodeTextMessage(WebSocketPackage package)
	{
		Encoder encoder = _encoding.GetEncoder();
		ReadOnlySpan<char> chars = package.Message.AsSpan();
		bool completed = false;
		WritableSequenceStream writableSequenceStream = new WritableSequenceStream();
		using (DeflateStream deflateStream = new DeflateStream(writableSequenceStream, CompressionMode.Compress))
		{
			while (!completed)
			{
				byte[] array = _arrayPool.Rent(4194304);
				Span<byte> bytes = array;
				encoder.Convert(chars, bytes, flush: false, out var charsUsed, out var bytesUsed, out completed);
				if (charsUsed > 0)
				{
					chars = chars.Slice(charsUsed);
				}
				deflateStream.Write(array, 0, bytesUsed);
			}
			deflateStream.Flush();
		}
		ReadOnlySequence<byte> data = writableSequenceStream.GetUnderlyingSequence();
		RemoveLastFourOctets(ref data);
		package.Data = data;
	}

	private void RemoveLastFourOctets(ref ReadOnlySequence<byte> data)
	{
		int num = LAST_FOUR_OCTETS.Length;
		if (data.Length < num)
		{
			return;
		}
		ReadOnlySequence<byte> readOnlySequence = data.Slice(data.Length - num, num);
		int num2 = 0;
		ReadOnlySequence<byte>.Enumerator enumerator = readOnlySequence.GetEnumerator();
		while (enumerator.MoveNext())
		{
			ReadOnlyMemory<byte> current = enumerator.Current;
			for (int i = 0; i < current.Length; i++)
			{
				if (current.Span[i] != LAST_FOUR_OCTETS[num2++])
				{
					return;
				}
			}
		}
		data = data.Slice(0L, data.Length - num);
	}

	private void EncodeDataMessage(WebSocketPackage package)
	{
		ReadOnlySequence<byte> data = package.Data;
		WritableSequenceStream writableSequenceStream = new WritableSequenceStream();
		using (DeflateStream deflateStream = new DeflateStream(writableSequenceStream, CompressionMode.Compress))
		{
			ReadOnlySequence<byte>.Enumerator enumerator = data.GetEnumerator();
			while (enumerator.MoveNext())
			{
				deflateStream.Write(enumerator.Current.Span);
			}
			deflateStream.Flush();
		}
		data = writableSequenceStream.GetUnderlyingSequence();
		RemoveLastFourOctets(ref data);
		package.Data = data;
	}
}
