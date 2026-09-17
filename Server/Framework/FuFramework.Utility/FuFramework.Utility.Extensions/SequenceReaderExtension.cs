using System;
using System.Buffers;

namespace FuFramework.Utility.Extensions;

/// <summary>
/// 提供对 <see cref="T:System.Buffers.SequenceReader`1" /> 类的扩展方法，用于从只读内存中读取数据。
/// </summary>
public static class SequenceReaderExtension
{
	/// <summary>
	/// 从只读内存中获取一个字节数据。
	/// </summary>
	/// <param name="reader">只读内存读取器。</param>
	/// <param name="value">结果值。</param>
	/// <returns>读取成功返回 True，否则返回 False。</returns>
	public static bool TryReadBigEndian(this ref SequenceReader<byte> reader, out byte value)
	{
		value = 0;
		if (reader.Remaining < 1 || !reader.TryRead(out var value2))
		{
			return false;
		}
		value = value2;
		return true;
	}

	/// <summary>
	/// 从只读内存中获取指定长度的字节数据，并移动读取位置。
	/// </summary>
	/// <param name="reader">读取器。</param>
	/// <param name="length">读取的长度。</param>
	/// <param name="value">结果值。</param>
	/// <returns>读取成功返回 True，否则返回 False。</returns>
	public static bool TryReadBytes(this ref SequenceReader<byte> reader, int length, out byte[] value)
	{
		value = new byte[length];
		if (reader.Remaining < length || !reader.TryCopyTo(value))
		{
			return false;
		}
		reader.Advance(length);
		return true;
	}

	/// <summary>
	/// 从只读内存中获取一个字节数据，但不移动读取位置。
	/// </summary>
	/// <param name="reader">读取器。</param>
	/// <param name="value">结果值。</param>
	/// <returns>读取成功返回 True，否则返回 False。</returns>
	public static bool TryPeekBigEndian(this ref SequenceReader<byte> reader, out byte value)
	{
		value = 0;
		if (reader.Remaining < 1 || !reader.TryPeek(0L, out var value2))
		{
			return false;
		}
		value = value2;
		return true;
	}

	/// <summary>
	/// 从只读内存中获取一个无符号短整型数据，但不移动读取位置。
	/// </summary>
	/// <param name="reader">读取器。</param>
	/// <param name="value">结果值。</param>
	/// <returns>读取成功返回 True，否则返回 False。</returns>
	public static bool TryPeekBigEndian(this ref SequenceReader<byte> reader, out ushort value)
	{
		value = 0;
		if (reader.Remaining < 2 || !reader.TryPeek(0L, out var value2) || !reader.TryPeek(1L, out var value3))
		{
			return false;
		}
		value = (ushort)(value2 * 256 + value3);
		return true;
	}

	/// <summary>
	/// 从只读内存中获取一个无符号整型数据，但不移动读取位置。
	/// </summary>
	/// <param name="reader">读取器。</param>
	/// <param name="value">结果值。</param>
	/// <returns>读取成功返回 True，否则返回 False。</returns>
	public static bool TryPeekBigEndian(this ref SequenceReader<byte> reader, out uint value)
	{
		value = 0u;
		if (reader.Remaining < 4)
		{
			return false;
		}
		int num = 0;
		int num2 = (int)System.Math.Pow(256.0, 3.0);
		for (int i = 0; i < 4; i++)
		{
			if (!reader.TryPeek(i, out var value2))
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
	/// 从只读内存中获取一个无符号长整型数据，但不移动读取位置。
	/// </summary>
	/// <param name="reader">读取器。</param>
	/// <param name="value">结果值。</param>
	/// <returns>读取成功返回 True，否则返回 False。</returns>
	public static bool TryPeekBigEndian(this ref SequenceReader<byte> reader, out ulong value)
	{
		value = 0uL;
		if (reader.Remaining < 8)
		{
			return false;
		}
		long num = 0L;
		long num2 = (long)System.Math.Pow(256.0, 7.0);
		for (int i = 0; i < 8; i++)
		{
			if (!reader.TryPeek(i, out var value2))
			{
				return false;
			}
			num += num2 * value2;
			num2 /= 256;
		}
		value = (ulong)num;
		return true;
	}
}
