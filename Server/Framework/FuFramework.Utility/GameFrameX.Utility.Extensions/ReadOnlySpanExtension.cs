using System;
using System.Buffers.Binary;

namespace FuFramework.Utility.Extensions;

/// <summary>
/// </summary>
public static class ReadOnlySpanExtension
{
	/// <summary>
	/// 从字节数组中以指定偏移量读取无符号整型。
	/// </summary>
	/// <param name="buffer">要从中读取数据的字节数组。</param>
	/// <param name="offset">读取数据的起始偏移量，此偏移量在读取后会自动增加。</param>
	/// <returns>读取的无符号整型，若读取长度小于等于0或偏移量超出数组长度，返回0。</returns>
	public static uint ReadUInt(this ReadOnlySpan<byte> buffer, ref int offset)
	{
		if (offset > buffer.Length + 4)
		{
			throw new Exception("buffer read out of index");
		}
		int num = offset;
		uint result = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(num, buffer.Length - num));
		offset += 4;
		return result;
	}

	/// <summary>
	/// 从字节数组中以指定偏移量读取整型。
	/// </summary>
	/// <param name="buffer">要从中读取数据的字节数组。</param>
	/// <param name="offset">读取数据的起始偏移量，此偏移量在读取后会自动增加。</param>
	/// <returns>读取的整型，若读取长度小于等于0或偏移量超出数组长度，返回0。</returns>
	public static int ReadInt(this ReadOnlySpan<byte> buffer, ref int offset)
	{
		if (offset > buffer.Length + 4)
		{
			throw new Exception("buffer read out of index");
		}
		int num = offset;
		int result = BinaryPrimitives.ReadInt32BigEndian(buffer.Slice(num, buffer.Length - num));
		offset += 4;
		return result;
	}

	/// <summary>
	/// 从字节数组中以指定偏移量读取无符号长整型。
	/// </summary>
	/// <param name="buffer">要从中读取数据的字节数组。</param>
	/// <param name="offset">读取数据的起始偏移量，此偏移量在读取后会自动增加。</param>
	/// <returns>读取的无符号长整型，若读取长度小于等于0或偏移量超出数组长度，返回0。</returns>
	public static ulong ReadULong(this ReadOnlySpan<byte> buffer, ref int offset)
	{
		if (offset > buffer.Length + 8)
		{
			throw new Exception("buffer read out of index");
		}
		int num = offset;
		ulong result = BinaryPrimitives.ReadUInt64BigEndian(buffer.Slice(num, buffer.Length - num));
		offset += 8;
		return result;
	}

	/// <summary>
	/// 从字节数组中以指定偏移量读取长整型。
	/// </summary>
	/// <param name="buffer">要从中读取数据的字节数组。</param>
	/// <param name="offset">读取数据的起始偏移量，此偏移量在读取后会自动增加。</param>
	/// <returns>读取的长整型，若读取长度小于等于0或偏移量超出数组长度，返回0。</returns>
	public static long ReadLong(this ReadOnlySpan<byte> buffer, ref int offset)
	{
		if (offset > buffer.Length + 8)
		{
			throw new Exception("buffer read out of index");
		}
		int num = offset;
		long result = BinaryPrimitives.ReadInt64BigEndian(buffer.Slice(num, buffer.Length - num));
		offset += 8;
		return result;
	}

	/// <summary>
	/// 从字节数组中以指定偏移量读取无符号短整型。
	/// </summary>
	/// <param name="buffer">要从中读取数据的字节数组。</param>
	/// <param name="offset">读取数据的起始偏移量，此偏移量在读取后会自动增加。</param>
	/// <returns>读取的无符号短整型，若读取长度小于等于0或偏移量超出数组长度，返回0。</returns>
	public static ushort ReadUShort(this ReadOnlySpan<byte> buffer, ref int offset)
	{
		if (offset > buffer.Length + 2)
		{
			throw new Exception("buffer read out of index");
		}
		int num = offset;
		ushort result = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(num, buffer.Length - num));
		offset += 2;
		return result;
	}

	/// <summary>
	/// 从字节数组中以指定偏移量读取短整型。
	/// </summary>
	/// <param name="buffer">要从中读取数据的字节数组。</param>
	/// <param name="offset">读取数据的起始偏移量，此偏移量在读取后会自动增加。</param>
	/// <returns>读取的短整型，若读取长度小于等于0或偏移量超出数组长度，返回0。</returns>
	public static short ReadShort(this ReadOnlySpan<byte> buffer, ref int offset)
	{
		if (offset > buffer.Length + 2)
		{
			throw new Exception("buffer read out of index");
		}
		int num = offset;
		short result = BinaryPrimitives.ReadInt16BigEndian(buffer.Slice(num, buffer.Length - num));
		offset += 2;
		return result;
	}

	/// <summary>
	/// 从字节数组中以指定偏移量读取单精度浮点数。
	/// </summary>
	/// <param name="buffer">要从中读取数据的字节数组。</param>
	/// <param name="offset">读取数据的起始偏移量，此偏移量在读取后会自动增加。</param>
	/// <returns>读取的单精度浮点数，若读取长度小于等于0或偏移量超出数组长度，返回0。</returns>
	public static float ReadFloat(this ReadOnlySpan<byte> buffer, ref int offset)
	{
		if (offset > buffer.Length + 4)
		{
			throw new Exception("buffer read out of index");
		}
		int num = offset;
		float result = BinaryPrimitives.ReadSingleBigEndian(buffer.Slice(num, buffer.Length - num));
		offset += 4;
		return result;
	}

	/// <summary>
	/// 从字节数组中以指定偏移量读取双精度浮点数。
	/// </summary>
	/// <param name="buffer">要从中读取数据的字节数组。</param>
	/// <param name="offset">读取数据的起始偏移量，此偏移量在读取后会自动增加。</param>
	/// <returns>读取的双精度浮点数，若读取长度小于等于0或偏移量超出数组长度，返回0。</returns>
	public static double ReadDouble(this ReadOnlySpan<byte> buffer, ref int offset)
	{
		if (offset > buffer.Length + 8)
		{
			throw new Exception("buffer read out of index");
		}
		int num = offset;
		double result = BinaryPrimitives.ReadDoubleBigEndian(buffer.Slice(num, buffer.Length - num));
		offset += 8;
		return result;
	}
}
