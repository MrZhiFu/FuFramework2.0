using System;
using System.Buffers;
using System.Text;

namespace FuFramework.SuperSocket.ProtoBase;

/// <summary>
/// Provides utility extension methods for working with sequences and buffers.
/// </summary>
public static class Extensions
{
	/// <summary>
	/// Reads a string from the sequence reader using UTF-8 encoding.
	/// </summary>
	/// <param name="reader">The sequence reader.</param>
	/// <param name="length">The length of the string to read. If 0, reads the remaining length.</param>
	/// <returns>The decoded string.</returns>
	public static string ReadString(this ref SequenceReader<byte> reader, long length = 0L)
	{
		return reader.ReadString(Encoding.UTF8, length);
	}

	/// <summary>
	/// Reads a string from the sequence reader using the specified encoding.
	/// </summary>
	/// <param name="reader">The sequence reader.</param>
	/// <param name="encoding">The encoding to use for decoding the string.</param>
	/// <param name="length">The length of the string to read. If 0, reads the remaining length.</param>
	/// <returns>The decoded string.</returns>
	public static string ReadString(this ref SequenceReader<byte> reader, Encoding encoding, long length = 0L)
	{
		if (length == 0L)
		{
			length = reader.Remaining;
		}
		ReadOnlySequence<byte> buffer = reader.Sequence.Slice(reader.Consumed, length);
		try
		{
			return buffer.GetString(encoding);
		}
		finally
		{
			reader.Advance(length);
		}
	}

	/// <summary>
	/// Attempts to read a 16-bit unsigned integer in big-endian format from the sequence reader.
	/// </summary>
	/// <param name="reader">The sequence reader.</param>
	/// <param name="value">The read value.</param>
	/// <returns><c>true</c> if the value was successfully read; otherwise, <c>false</c>.</returns>
	public static bool TryReadBigEndian(this ref SequenceReader<byte> reader, out ushort value)
	{
		value = 0;
		if (reader.Remaining < 2)
		{
			return false;
		}
		if (!reader.TryRead(out var value2))
		{
			return false;
		}
		if (!reader.TryRead(out var value3))
		{
			return false;
		}
		value = (ushort)(value2 * 256 + value3);
		return true;
	}

	/// <summary>
	/// Attempts to read a 32-bit unsigned integer in big-endian format from the sequence reader.
	/// </summary>
	/// <param name="reader">The sequence reader.</param>
	/// <param name="value">The read value.</param>
	/// <returns><c>true</c> if the value was successfully read; otherwise, <c>false</c>.</returns>
	public static bool TryReadBigEndian(this ref SequenceReader<byte> reader, out uint value)
	{
		value = 0u;
		if (reader.Remaining < 4)
		{
			return false;
		}
		int num = 0;
		int num2 = (int)Math.Pow(256.0, 3.0);
		for (int i = 0; i < 4; i++)
		{
			if (!reader.TryRead(out var value2))
			{
				return false;
			}
			num += num2 * value2;
			num2 /= 256;
		}
		value = (uint)num;
		return true;
	}

	/// <summary>
	/// Attempts to read a 64-bit unsigned integer in big-endian format from the sequence reader.
	/// </summary>
	/// <param name="reader">The sequence reader.</param>
	/// <param name="value">The read value.</param>
	/// <returns><c>true</c> if the value was successfully read; otherwise, <c>false</c>.</returns>
	public static bool TryReadBigEndian(this ref SequenceReader<byte> reader, out ulong value)
	{
		value = 0uL;
		if (reader.Remaining < 8)
		{
			return false;
		}
		long num = 0L;
		long num2 = (long)Math.Pow(256.0, 7.0);
		for (int i = 0; i < 8; i++)
		{
			if (!reader.TryRead(out var value2))
			{
				return false;
			}
			num += num2 * value2;
			num2 /= 256;
		}
		value = (ulong)num;
		return true;
	}

	/// <summary>
	/// Converts a read-only sequence of bytes to a string using the specified encoding.
	/// </summary>
	/// <param name="buffer">The read-only sequence of bytes.</param>
	/// <param name="encoding">The encoding to use for decoding the string.</param>
	/// <returns>The decoded string.</returns>
	public static string GetString(this ReadOnlySequence<byte> buffer, Encoding encoding)
	{
		if (buffer.IsSingleSegment)
		{
			return encoding.GetString(buffer.First.Span);
		}
		if (encoding.IsSingleByte)
		{
			return string.Create((int)buffer.Length, buffer, delegate(Span<char> span, ReadOnlySequence<byte> sequence)
			{
				ReadOnlySequence<byte>.Enumerator enumerator = sequence.GetEnumerator();
				while (enumerator.MoveNext())
				{
					ReadOnlyMemory<byte> current = enumerator.Current;
					int chars = encoding.GetChars(current.Span, span);
					span = span.Slice(chars);
				}
			});
		}
		StringBuilder stringBuilder = new StringBuilder();
		Decoder decoder = encoding.GetDecoder();
		ReadOnlySequence<byte>.Enumerator enumerator2 = buffer.GetEnumerator();
		while (enumerator2.MoveNext())
		{
			ReadOnlyMemory<byte> current2 = enumerator2.Current;
			Span<char> span2 = new char[current2.Length].AsSpan();
			int chars2 = decoder.GetChars(current2.Span, span2, flush: false);
			stringBuilder.Append(new string((chars2 == span2.Length) ? span2 : span2.Slice(0, chars2)));
		}
		return stringBuilder.ToString();
	}

	/// <summary>
	/// Writes the specified text to the buffer writer using the specified encoding.
	/// </summary>
	/// <param name="writer">The buffer writer.</param>
	/// <param name="text">The text to write.</param>
	/// <param name="encoding">The encoding to use for encoding the text.</param>
	/// <returns>The total number of bytes written to the buffer writer.</returns>
	public static int Write(this IBufferWriter<byte> writer, ReadOnlySpan<char> text, Encoding encoding)
	{
		Encoder encoder = encoding.GetEncoder();
		bool completed = false;
		int num = 0;
		int maxByteCount = encoding.GetMaxByteCount(1);
		while (!completed)
		{
			Span<byte> span = writer.GetSpan(maxByteCount);
			encoder.Convert(text, span, flush: false, out var charsUsed, out var bytesUsed, out completed);
			if (charsUsed > 0)
			{
				text = text.Slice(charsUsed);
			}
			num += bytesUsed;
			writer.Advance(bytesUsed);
		}
		return num;
	}
}
