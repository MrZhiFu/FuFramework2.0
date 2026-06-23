using System;
using System.Buffers;

namespace FuFramework.SuperSocket.WebSocket;

public class WebSocketMaskedEncoder : WebSocketEncoder
{
	private class MaskingContext
	{
		public ReadOnlyMemory<byte> Mask { get; set; }

		public byte[] MaskBuffer { get; set; }
	}

	private static readonly Random _random = new Random();

	private const int MASK_LEN = 4;

	private const int MASK_OFFSET_RESET_THRESHOLD = 100000;

	public WebSocketMaskedEncoder(ArrayPool<byte> bufferPool, int[] fragmentSizes)
		: base(bufferPool, fragmentSizes)
	{
	}

	protected override object CreateDataEncodingContext(IBufferWriter<byte> writer)
	{
		MaskingContext maskingContext = new MaskingContext();
		if (writer == null)
		{
			byte[] array = base.BufferPool.Rent(4);
			Memory<byte> memory = array.AsMemory(0, 4);
			GenerateMask(memory);
			maskingContext.Mask = memory;
			maskingContext.MaskBuffer = array;
		}
		else
		{
			Memory<byte> memory2 = writer.GetMemory(4).Slice(0, 4);
			GenerateMask(memory2);
			maskingContext.Mask = memory2;
		}
		return maskingContext;
	}

	protected override Span<byte> WriteHead(IBufferWriter<byte> writer, long length, out int headLen)
	{
		Span<byte> result = base.WriteHead(writer, length, out headLen);
		result[1] = (byte)(result[1] | 0x80);
		return result;
	}

	protected override void OnHeadEncoded(IBufferWriter<byte> writer, object encodingContext)
	{
		if (encodingContext is MaskingContext maskingContext)
		{
			if (maskingContext.MaskBuffer == null)
			{
				writer.Advance(4);
			}
			else
			{
				writer.Write(maskingContext.Mask.Span);
			}
		}
	}

	protected override void OnDataEncoded(Span<byte> encodedData, object encodingContext, int previusEncodedDataSize)
	{
		MaskingContext maskingContext = encodingContext as MaskingContext;
		MaskData(encodedData, maskingContext.Mask.Span, encodedData.Length, previusEncodedDataSize);
	}

	protected override void EncodeDataMessageBody(IBufferWriter<byte> writer, WebSocketPackage pack)
	{
		Memory<byte> memory = writer.GetMemory(4);
		GenerateMask(memory);
		writer.Advance(4);
		int num = 0;
		ReadOnlySequence<byte>.Enumerator enumerator = pack.Data.GetEnumerator();
		while (enumerator.MoveNext())
		{
			ReadOnlySpan<byte> readOnlySpan = enumerator.Current.Span;
			do
			{
				Span<byte> span = writer.GetSpan();
				int num2 = Math.Min(span.Length, readOnlySpan.Length);
				readOnlySpan.Slice(0, num2).CopyTo(span);
				MaskData(span, memory.Span, num2, num);
				writer.Advance(num2);
				num += num2;
				if (num > 100000)
				{
					num %= 4;
				}
				int num3 = num2;
				readOnlySpan = readOnlySpan.Slice(num3, readOnlySpan.Length - num3);
			}
			while (readOnlySpan.Length != 0);
		}
	}

	private void GenerateMask(Memory<byte> mask)
	{
		Span<byte> span = mask.Span;
		for (int i = 0; i < 4; i++)
		{
			span[i] = (byte)_random.Next(0, 255);
		}
	}

	private void MaskData(Span<byte> data, ReadOnlySpan<byte> mask, int dataLength, int maskOffset = 0)
	{
		for (int i = 0; i < dataLength; i++)
		{
			data[i] ^= mask[(i + maskOffset) % 4];
		}
	}
}
