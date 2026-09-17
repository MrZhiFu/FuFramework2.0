using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FuFramework.SuperSocket.ProtoBase;
using FuFramework.SuperSocket.WebSocket.Extensions;

namespace FuFramework.SuperSocket.WebSocket;

public class WebSocketEncoder : IPackageEncoder<WebSocketPackage>
{
	private static readonly Encoding _textEncoding;

	private static readonly int _minEncodeBufferSize;

	private const int _size0 = 126;

	private const int _size1 = 65536;

	private readonly int[] _fragmentSizes;

	private readonly ArrayPool<byte> _bufferPool;

	private static int[] _defaultFragmentSizes;

	protected ArrayPool<byte> BufferPool => _bufferPool;

	public IReadOnlyList<IWebSocketExtension> Extensions { get; set; }

	static WebSocketEncoder()
	{
		_textEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
		_defaultFragmentSizes = new int[6] { 1024, 4096, 8192, 16384, 32768, 65536 };
		_minEncodeBufferSize = _textEncoding.GetMaxByteCount(1);
	}

	public WebSocketEncoder()
		: this(ArrayPool<byte>.Shared, _defaultFragmentSizes)
	{
	}

	public WebSocketEncoder(ArrayPool<byte> bufferPool, int[] fragmentSizes)
	{
		_bufferPool = bufferPool;
		_fragmentSizes = fragmentSizes;
		if (fragmentSizes.Any((int size) => size <= _minEncodeBufferSize))
		{
			throw new ArgumentException($"fragmentSize should be larger than {_minEncodeBufferSize}.", "fragmentSizes");
		}
	}

	private int WriteHead(IBufferWriter<byte> writer, byte opCode, long length)
	{
		WriteHead(writer, length, out var headLen)[0] = opCode;
		writer.Advance(headLen);
		return headLen;
	}

	protected virtual Span<byte> WriteHead(IBufferWriter<byte> writer, long length, out int headLen)
	{
		if (length < 126)
		{
			headLen = 2;
			Span<byte> span = writer.GetSpan(headLen);
			span[1] = (byte)length;
			return span;
		}
		if (length < 65536)
		{
			headLen = 4;
			Span<byte> span2 = writer.GetSpan(headLen);
			span2[1] = 126;
			span2[2] = (byte)(length / 256);
			span2[3] = (byte)(length % 256);
			return span2;
		}
		headLen = 10;
		Span<byte> span3 = writer.GetSpan(headLen);
		span3[1] = 127;
		long num = length;
		int num2 = 256;
		for (int num3 = 9; num3 > 1; num3--)
		{
			if (num == 0L)
			{
				span3[num3] = 0;
			}
			else
			{
				span3[num3] = (byte)(num % num2);
				num /= num2;
			}
		}
		return span3;
	}

	private int EncodeEmptyFragment(IBufferWriter<byte> writer, byte opCode)
	{
		return EncodeFinalFragment(writer, opCode, ReadOnlySpan<char>.Empty, null, default(ArraySegment<byte>));
	}

	private int EncodeFragment(IBufferWriter<byte> writer, byte opCode, int fragmentSize, ReadOnlySpan<char> text, Encoder encoder, ref ArraySegment<byte> unwrittenBytes, out int charsUsed)
	{
		charsUsed = 0;
		int headLen = WriteHead(writer, opCode, fragmentSize);
		object encodingContext = CreateDataEncodingContext(writer);
		OnHeadEncoded(writer, encodingContext);
		int num = 0;
		int num2 = fragmentSize;
		int count = unwrittenBytes.Count;
		if (count > 0)
		{
			Span<byte> span = writer.GetSpan(count);
			unwrittenBytes.AsSpan().CopyTo(span);
			OnDataEncoded(span.Slice(0, count), encodingContext, 0);
			writer.Advance(count);
			num += count;
			num2 -= count;
			unwrittenBytes = unwrittenBytes.Slice(0, 0);
		}
		while (true)
		{
			int num3 = Math.Max(num2, _minEncodeBufferSize);
			Span<byte> span2 = writer.GetSpan(num3);
			span2 = span2.Slice(0, num3);
			int bytesUsed = 0;
			int charsUsed2 = 0;
			try
			{
				encoder.Convert(text, span2, flush: false, out charsUsed2, out bytesUsed, out var _);
			}
			catch (ArgumentException innerException)
			{
				throw new Exception($"textToEncode: {text.Length}, buffer: {span2.Length}.", innerException);
			}
			if (bytesUsed > num2)
			{
				int num4 = bytesUsed - num2;
				if (num4 > 0)
				{
					byte[] array = unwrittenBytes.Array ?? _bufferPool.Rent(_minEncodeBufferSize);
					span2.Slice(num2, num4).CopyTo(array.AsSpan(0, num4));
					unwrittenBytes = new ArraySegment<byte>(array, 0, num4);
				}
				bytesUsed = num2;
			}
			span2 = span2.Slice(0, bytesUsed);
			OnDataEncoded(span2, encodingContext, num);
			writer.Advance(bytesUsed);
			num += bytesUsed;
			charsUsed += charsUsed2;
			num2 -= bytesUsed;
			if (num2 == 0)
			{
				break;
			}
			text = text.Slice(charsUsed2);
			if (num > fragmentSize)
			{
				throw new Exception("Size of the data from the decoding must be equal to the fragment size.");
			}
		}
		return GetFragmentTotalLength(headLen, num);
	}

	protected virtual object CreateDataEncodingContext(IBufferWriter<byte> writer)
	{
		return null;
	}

	protected virtual void OnHeadEncoded(IBufferWriter<byte> writer, object encodingContext)
	{
	}

	protected virtual void OnDataEncoded(Span<byte> encodedData, object encodingContext, int previusEncodedDataSize)
	{
	}

	protected virtual void CleanupEncodingContext(object encodingContext)
	{
	}

	protected virtual int GetFragmentTotalLength(int headLen, int bodyLen)
	{
		return headLen + bodyLen;
	}

	private int EncodeFinalFragment(IBufferWriter<byte> writer, byte opCode, ReadOnlySpan<char> text, Encoder encoder, ArraySegment<byte> unwrittenBytes)
	{
		byte[] array = null;
		Span<byte> span = default(Span<byte>);
		object encodingContext = null;
		try
		{
			int num = 0;
			encodingContext = CreateDataEncodingContext(null);
			if (encoder != null)
			{
				int num2 = ((text.Length > 0) ? encoder.GetByteCount(text, flush: true) : 0) + unwrittenBytes.Count;
				if (num2 == 0)
				{
					num2 = _minEncodeBufferSize;
				}
				array = _bufferPool.Rent(num2);
				span = array.AsSpan();
				if (unwrittenBytes.Count > 0)
				{
					unwrittenBytes.AsSpan().CopyTo(span);
					num += unwrittenBytes.Count;
					OnDataEncoded(span.Slice(0, unwrittenBytes.Count), encodingContext, 0);
				}
				encoder.Convert(text, (num == 0) ? span : span.Slice(num), flush: true, out var charsUsed, out var bytesUsed, out var completed);
				OnDataEncoded(span.Slice(num, bytesUsed), encodingContext, num);
				num += bytesUsed;
				if (!completed || text.Length != charsUsed)
				{
					throw new ProtocolException("Unexpected encoding behavior: the text encoding didn't complete with enough buffer.");
				}
			}
			opCode |= 0x80;
			int headLen = WriteHead(writer, opCode, num);
			OnHeadEncoded(writer, encodingContext);
			if (num > 0)
			{
				writer.Write(span.Slice(0, num));
			}
			return GetFragmentTotalLength(headLen, num);
		}
		finally
		{
			if (array != null)
			{
				_bufferPool.Return(array);
			}
			CleanupEncodingContext(encodingContext);
		}
	}

	protected virtual void EncodeDataMessageBody(IBufferWriter<byte> writer, WebSocketPackage pack)
	{
		ReadOnlySequence<byte>.Enumerator enumerator = pack.Data.GetEnumerator();
		while (enumerator.MoveNext())
		{
			writer.Write(enumerator.Current.Span);
		}
	}

	public int EncodeDataMessage(IBufferWriter<byte> writer, WebSocketPackage pack)
	{
		int num = WriteHead(writer, (byte)(pack.OpCodeByte | 0x80), pack.Data.Length);
		EncodeDataMessageBody(writer, pack);
		return (int)(pack.Data.Length + num);
	}

	private int GetFragmentSize(int msgSize)
	{
		for (int num = _fragmentSizes.Length - 1; num >= 0; num--)
		{
			int num2 = _fragmentSizes[num];
			if (msgSize >= num2)
			{
				return num2;
			}
		}
		return 0;
	}

	public int Encode(IBufferWriter<byte> writer, WebSocketPackage pack)
	{
		pack.SaveOpCodeByte();
		IReadOnlyList<IWebSocketExtension> extensions = Extensions;
		if (extensions != null && extensions.Count > 0)
		{
			foreach (IWebSocketExtension item in extensions)
			{
				item.Encode(pack);
			}
		}
		if (!pack.Data.IsEmpty)
		{
			return EncodeDataMessage(writer, pack);
		}
		if (string.IsNullOrEmpty(pack.Message) || pack.Message.Length == 0)
		{
			return EncodeEmptyFragment(writer, pack.OpCodeByte);
		}
		int num = 0;
		ReadOnlySpan<char> text = pack.Message.AsSpan();
		Encoder encoder = _textEncoding.GetEncoder();
		bool flag = false;
		ArraySegment<byte> unwrittenBytes = default(ArraySegment<byte>);
		while (true)
		{
			int fragmentSize = GetFragmentSize(text.Length + unwrittenBytes.Count);
			if (fragmentSize <= 0)
			{
				break;
			}
			num += EncodeFragment(writer, (byte)((!flag) ? pack.OpCodeByte : 0), fragmentSize, text, encoder, ref unwrittenBytes, out var charsUsed);
			text = text.Slice(charsUsed);
			if (!flag)
			{
				flag = true;
			}
		}
		try
		{
			return num + EncodeFinalFragment(writer, (byte)((!flag) ? pack.OpCodeByte : 0), text, encoder, unwrittenBytes);
		}
		finally
		{
			if (unwrittenBytes.Count > 0)
			{
				_bufferPool.Return(unwrittenBytes.Array);
			}
		}
	}
}
