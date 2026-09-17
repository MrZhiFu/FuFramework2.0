using System;
using System.Buffers;
using System.Text;
using FuFramework.SuperSocket.ProtoBase;
using FuFramework.SuperSocket.WebSocket.Extensions;

namespace FuFramework.SuperSocket.WebSocket.FramePartReader;

internal class PayloadDataReader : PackagePartReader
{
	public override bool Process(WebSocketPackage package, object filterContext, ref SequenceReader<byte> reader, out IPackagePartReader<WebSocketPackage> nextPartReader, out bool needMoreData)
	{
		nextPartReader = null;
		long payloadLength = package.PayloadLength;
		if (reader.Remaining < payloadLength)
		{
			needMoreData = true;
			return false;
		}
		needMoreData = false;
		ReadOnlySequence<byte> sequence = reader.Sequence.Slice(reader.Consumed, payloadLength);
		if (package.HasMask)
		{
			DecodeMask(ref sequence, package.MaskKey);
		}
		try
		{
			if (package.FIN && package.Head == null)
			{
				package.Data = sequence;
			}
			else
			{
				package.ConcatSequence(ref sequence);
			}
			if (package.FIN)
			{
				if (package.Head != null)
				{
					package.BuildData();
				}
				if (filterContext is WebSocketPipelineFilterContext { Extensions: not null } webSocketPipelineFilterContext && webSocketPipelineFilterContext.Extensions.Count > 0)
				{
					foreach (IWebSocketExtension extension in webSocketPipelineFilterContext.Extensions)
					{
						try
						{
							extension.Decode(package);
						}
						catch (Exception innerException)
						{
							throw new Exception("Problem happened when decode with the extension " + extension.Name + ".", innerException);
						}
					}
				}
				ReadOnlySequence<byte> seq = package.Data;
				if (package.OpCode == OpCode.Text)
				{
					package.Message = seq.GetString(Encoding.UTF8);
					package.Data = default(ReadOnlySequence<byte>);
				}
				else
				{
					package.Data = seq.CopySequence();
				}
				return true;
			}
			nextPartReader = PackagePartReader.FixPartReader;
			return false;
		}
		finally
		{
			reader.Advance(payloadLength);
		}
	}

	internal unsafe void DecodeMask(ref ReadOnlySequence<byte> sequence, byte[] mask)
	{
		int num = 0;
		int num2 = mask.Length;
		ReadOnlySequence<byte>.Enumerator enumerator = sequence.GetEnumerator();
		while (enumerator.MoveNext())
		{
			ReadOnlyMemory<byte> current = enumerator.Current;
			try
			{
				ReadOnlySpan<byte> span = current.Span;
				fixed (byte* pointer = span)
				{
					span = current.Span;
					Span<byte> span2 = new Span<byte>(pointer, span.Length);
					for (int i = 0; i < span2.Length; i++)
					{
						span2[i] ^= mask[num++ % num2];
					}
				}
			}
			finally
			{
			}
		}
	}
}
