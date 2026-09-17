using System;
using System.Buffers.Binary;
using System.Net;
using System.Text;

namespace FuFramework.Utility.Extensions;

/// <summary>
/// 提供字节和字节数组的扩展方法，用于各种格式的转换和读写操作。
/// </summary>
public static class ByteExtension
{
	/// <summary>
	/// 将字节转换为16进制字符串。
	/// </summary>
	/// <param name="b">要转换的字节。</param>
	/// <returns>16进制字符串。</returns>
	public static string ToHex(this byte b)
	{
		return b.ToString("X2");
	}

	/// <summary>
	/// 将字节数组转换为字符串，每个字节之间用空格分隔。
	/// </summary>
	/// <param name="bytes">要转换的字节数组。</param>
	/// <returns>字符串表示形式。</returns>
	public static string ToArrayString(this byte[] bytes)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (byte b in bytes)
		{
			stringBuilder.Append(b + " ");
		}
		return stringBuilder.ToString();
	}

	/// <summary>
	/// 将字节数组转换为16进制字符串。
	/// </summary>
	/// <param name="bytes">要转换的字节数组。</param>
	/// <returns>16进制字符串。</returns>
	public static string ToHex(this byte[] bytes)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (byte b in bytes)
		{
			stringBuilder.Append(b.ToString("X2"));
		}
		return stringBuilder.ToString();
	}

	/// <summary>
	/// 将字节数组转换为指定格式的字符串。
	/// </summary>
	/// <param name="bytes">要转换的字节数组。</param>
	/// <param name="format">格式化字符串。</param>
	/// <returns>格式化后的字符串。</returns>
	public static string ToHex(this byte[] bytes, string format)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (byte b in bytes)
		{
			stringBuilder.Append(b.ToString(format));
		}
		return stringBuilder.ToString();
	}

	/// <summary>
	/// 将字节数组的指定范围转换为16进制字符串。
	/// </summary>
	/// <param name="bytes">要转换的字节数组。</param>
	/// <param name="offset">起始偏移量。</param>
	/// <param name="count">要转换的字节数。</param>
	/// <returns>16进制字符串。</returns>
	public static string ToHex(this byte[] bytes, int offset, int count)
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = offset; i < offset + count; i++)
		{
			stringBuilder.Append(bytes[i].ToString("X2"));
		}
		return stringBuilder.ToString();
	}

	/// <summary>
	/// 将字节数组转换为默认编码的字符串。
	/// </summary>
	/// <param name="bytes">要转换的字节数组。</param>
	/// <returns>字符串。</returns>
	public static string ToDefaultString(this byte[] bytes)
	{
		return Encoding.Default.GetString(bytes);
	}

	/// <summary>
	/// 将字节数组的指定范围转换为默认编码的字符串。
	/// </summary>
	/// <param name="bytes">要转换的字节数组。</param>
	/// <param name="index">起始偏移量。</param>
	/// <param name="count">要转换的字节数。</param>
	/// <returns>字符串。</returns>
	public static string ToDefaultString(this byte[] bytes, int index, int count)
	{
		return Encoding.Default.GetString(bytes, index, count);
	}

	/// <summary>
	/// 将字节数组转换为UTF8编码的字符串。
	/// </summary>
	/// <param name="bytes">要转换的字节数组。</param>
	/// <returns>UTF8编码的字符串。</returns>
	public static string ToUtf8String(this byte[] bytes)
	{
		return Encoding.UTF8.GetString(bytes);
	}

	/// <summary>
	/// 将字节数组的指定范围转换为UTF8编码的字符串。
	/// </summary>
	/// <param name="bytes">要转换的字节数组。</param>
	/// <param name="index">起始偏移量。</param>
	/// <param name="count">要转换的字节数。</param>
	/// <returns>UTF8编码的字符串。</returns>
	public static string ToUtf8String(this byte[] bytes, int index, int count)
	{
		return Encoding.UTF8.GetString(bytes, index, count);
	}

	/// <summary>
	/// 将一个32位无符号整数写入指定的缓冲区，并更新偏移量。
	/// </summary>
	/// <param name="buffer">要写入的缓冲区。</param>
	/// <param name="value">要写入的值。</param>
	/// <param name="offset">要写入值的缓冲区中的偏移量。</param>
	public static void WriteUInt(this byte[] buffer, uint value, ref int offset)
	{
		if (offset + 4 > buffer.Length)
		{
			offset += 4;
			return;
		}
		Span<byte> span = buffer.AsSpan();
		int num = offset;
		BinaryPrimitives.WriteUInt32BigEndian(span.Slice(num, span.Length - num), value);
		offset += 4;
	}

	/// <summary>
	/// 将一个32位整数写入指定的缓冲区，并更新偏移量。
	/// </summary>
	/// <param name="buffer">要写入的缓冲区。</param>
	/// <param name="value">要写入的值。</param>
	/// <param name="offset">要写入值的缓冲区中的偏移量。</param>
	public static void WriteInt(this byte[] buffer, int value, ref int offset)
	{
		if (offset + 4 > buffer.Length)
		{
			offset += 4;
			return;
		}
		Span<byte> span = buffer.AsSpan();
		int num = offset;
		BinaryPrimitives.WriteInt32BigEndian(span.Slice(num, span.Length - num), value);
		offset += 4;
	}

	/// <summary>
	/// 将一个8位整数写入指定的缓冲区，并更新偏移量。
	/// </summary>
	/// <param name="buffer">要写入的缓冲区。</param>
	/// <param name="value">要写入的值。</param>
	/// <param name="offset">要写入值的缓冲区中的偏移量。</param>
	public static void WriteByte(this byte[] buffer, byte value, ref int offset)
	{
		if (offset + 1 > buffer.Length)
		{
			offset++;
			return;
		}
		buffer[offset] = value;
		offset++;
	}

	/// <summary>
	/// 将一个16位整数写入指定的缓冲区，并更新偏移量。
	/// </summary>
	/// <param name="buffer">要写入的缓冲区。</param>
	/// <param name="value">要写入的值。</param>
	/// <param name="offset">要写入值的缓冲区中的偏移量。</param>
	public static void WriteShort(this byte[] buffer, short value, ref int offset)
	{
		if (offset + 2 > buffer.Length)
		{
			offset += 2;
			return;
		}
		Span<byte> span = buffer.AsSpan();
		int num = offset;
		BinaryPrimitives.WriteInt16BigEndian(span.Slice(num, span.Length - num), value);
		offset += 2;
	}

	/// <summary>
	/// 将一个16位无符号整数写入指定的缓冲区，并更新偏移量。
	/// </summary>
	/// <param name="buffer">要写入的缓冲区。</param>
	/// <param name="value">要写入的值。</param>
	/// <param name="offset">要写入值的缓冲区中的偏移量。</param>
	public static void WriteUShort(this byte[] buffer, ushort value, ref int offset)
	{
		if (offset + 2 > buffer.Length)
		{
			offset += 2;
			return;
		}
		Span<byte> span = buffer.AsSpan();
		int num = offset;
		BinaryPrimitives.WriteUInt16BigEndian(span.Slice(num, span.Length - num), value);
		offset += 2;
	}

	/// <summary>
	/// 将一个64位整数写入指定的缓冲区，并更新偏移量。
	/// </summary>
	/// <param name="buffer">要写入的缓冲区。</param>
	/// <param name="value">要写入的值。</param>
	/// <param name="offset">要写入值的缓冲区中的偏移量。</param>
	public static void WriteLong(this byte[] buffer, long value, ref int offset)
	{
		if (offset + 8 > buffer.Length)
		{
			offset += 8;
			return;
		}
		Span<byte> span = buffer.AsSpan();
		int num = offset;
		BinaryPrimitives.WriteInt64BigEndian(span.Slice(num, span.Length - num), value);
		offset += 8;
	}

	/// <summary>
	/// 将一个64位无符号整数写入指定的缓冲区，并更新偏移量。
	/// </summary>
	/// <param name="buffer">要写入的缓冲区。</param>
	/// <param name="value">要写入的值。</param>
	/// <param name="offset">要写入值的缓冲区中的偏移量。</param>
	public static void WriteULong(this byte[] buffer, ulong value, ref int offset)
	{
		if (offset + 8 > buffer.Length)
		{
			offset += 8;
			return;
		}
		Span<byte> span = buffer.AsSpan();
		int num = offset;
		BinaryPrimitives.WriteUInt64BigEndian(span.Slice(num, span.Length - num), value);
		offset += 8;
	}

	/// <summary>
	/// 从字节数组中读取16位无符号整数，并将偏移量向前移动。
	/// </summary>
	/// <param name="buffer">要读取的字节数组。</param>
	/// <param name="offset">引用偏移量。</param>
	/// <returns>返回读取的16位无符号整数。</returns>
	public static ushort ReadUShort(this byte[] buffer, ref int offset)
	{
		if (offset > buffer.Length + 2)
		{
			throw new Exception("buffer read out of index");
		}
		Span<byte> span = buffer.AsSpan();
		int num = offset;
		ushort result = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(num, span.Length - num));
		offset += 2;
		return result;
	}

	/// <summary>
	/// 从字节数组读取16位有符号整数，并将偏移量前移。
	/// </summary>
	/// <param name="buffer">要读取的字节数组。</param>
	/// <param name="offset">引用偏移量。</param>
	/// <returns>返回读取的16位有符号整数。</returns>
	public static short ReadShort(this byte[] buffer, ref int offset)
	{
		if (offset > buffer.Length + 2)
		{
			throw new Exception("buffer read out of index");
		}
		Span<byte> span = buffer.AsSpan();
		int num = offset;
		short result = BinaryPrimitives.ReadInt16BigEndian(span.Slice(num, span.Length - num));
		offset += 2;
		return result;
	}

	/// <summary>
	/// 从字节数组中读取32位无符号整数，并将偏移量向前移动。
	/// </summary>
	/// <param name="buffer">要读取的字节数组。</param>
	/// <param name="offset">引用偏移量。</param>
	/// <returns>返回读取的32位无符号整数。</returns>
	public static uint ReadUInt(this byte[] buffer, ref int offset)
	{
		if (offset > buffer.Length + 4)
		{
			throw new Exception("buffer read out of index");
		}
		Span<byte> span = buffer.AsSpan();
		int num = offset;
		uint result = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(num, span.Length - num));
		offset += 4;
		return result;
	}

	/// <summary>
	/// 从字节数组中读取32位有符号整数，并将偏移量向前移动。
	/// </summary>
	/// <param name="buffer">要读取的字节数组。</param>
	/// <param name="offset">引用偏移量。</param>
	/// <returns>返回读取的32位有符号整数。</returns>
	public static int ReadInt(this byte[] buffer, ref int offset)
	{
		if (offset > buffer.Length + 4)
		{
			throw new Exception("buffer read out of index");
		}
		Span<byte> span = buffer.AsSpan();
		int num = offset;
		int result = BinaryPrimitives.ReadInt32BigEndian(span.Slice(num, span.Length - num));
		offset += 4;
		return result;
	}

	/// <summary>
	/// 从字节数组中读取64位无符号整数，并将偏移量向前移动。
	/// </summary>
	/// <param name="buffer">要读取的字节数组。</param>
	/// <param name="offset">引用偏移量。</param>
	/// <returns>返回读取的64位无符号整数。</returns>
	public static ulong ReadULong(this byte[] buffer, ref int offset)
	{
		if (offset > buffer.Length + 8)
		{
			throw new Exception("buffer read out of index");
		}
		Span<byte> span = buffer.AsSpan();
		int num = offset;
		ulong result = BinaryPrimitives.ReadUInt64BigEndian(span.Slice(num, span.Length - num));
		offset += 8;
		return result;
	}

	/// <summary>
	/// 从字节数组中读取64位有符号整数，并将偏移量向前移动。
	/// </summary>
	/// <param name="buffer">要读取的字节数组。</param>
	/// <param name="offset">引用偏移量。</param>
	/// <returns>返回读取的64位有符号整数。</returns>
	public static long ReadLong(this byte[] buffer, ref int offset)
	{
		if (offset > buffer.Length + 8)
		{
			throw new Exception("buffer read out of index");
		}
		Span<byte> span = buffer.AsSpan();
		int num = offset;
		long result = BinaryPrimitives.ReadInt64BigEndian(span.Slice(num, span.Length - num));
		offset += 8;
		return result;
	}

	/// <summary>
	/// 将一个单精度浮点数写入指定的缓冲区，并更新偏移量。
	/// </summary>
	/// <param name="buffer">要写入的缓冲区。</param>
	/// <param name="value">要写入的值。</param>
	/// <param name="offset">要写入值的缓冲区中的偏移量。</param>
	public unsafe static void WriteFloat(this byte[] buffer, float value, ref int offset)
	{
		if (offset + 4 > buffer.Length)
		{
			offset += 4;
			return;
		}
		fixed (byte* ptr = buffer)
		{
			*(float*)(ptr + offset) = value;
			*(int*)(ptr + offset) = IPAddress.HostToNetworkOrder(*(int*)(ptr + offset));
			offset += 4;
		}
	}

	/// <summary>
	/// 将一个双精度浮点数写入指定的缓冲区，并更新偏移量。
	/// </summary>
	/// <param name="buffer">要写入的缓冲区。</param>
	/// <param name="value">要写入的值。</param>
	/// <param name="offset">要写入值的缓冲区中的偏移量。</param>
	public unsafe static void WriteDouble(this byte[] buffer, double value, ref int offset)
	{
		if (offset + 8 > buffer.Length)
		{
			offset += 8;
			return;
		}
		fixed (byte* ptr = buffer)
		{
			*(double*)(ptr + offset) = value;
			*(long*)(ptr + offset) = IPAddress.HostToNetworkOrder(*(long*)(ptr + offset));
			offset += 8;
		}
	}

	/// <summary>
	/// 将一个字节数组写入指定的缓冲区，并更新偏移量。
	/// </summary>
	/// <param name="buffer">要写入的缓冲区。</param>
	/// <param name="value">要写入的值。</param>
	/// <param name="offset">要写入值的缓冲区中的偏移量。</param>
	public static void WriteBytes(this byte[] buffer, byte[] value, ref int offset)
	{
		if (value == null)
		{
			buffer.WriteInt(0, ref offset);
			return;
		}
		if (offset + value.Length + 4 > buffer.Length)
		{
			offset += value.Length + 4;
			return;
		}
		buffer.WriteInt(value.Length, ref offset);
		Array.Copy(value, 0, buffer, offset, value.Length);
		offset += value.Length;
	}

	/// <summary>
	/// 将一个字节数组写入指定的缓冲区，并更新偏移量。
	/// </summary>
	/// <param name="buffer">要写入的缓冲区。</param>
	/// <param name="value">要写入的值。</param>
	/// <param name="offset">要写入值的缓冲区中的偏移量。</param>
	public unsafe static void WriteBytesWithoutLength(this byte[] buffer, byte[] value, ref int offset)
	{
		if (value == null)
		{
			buffer.WriteInt(0, ref offset);
			return;
		}
		if (offset + value.Length > buffer.Length)
		{
			throw new ArgumentException($"buffer write out of index {offset + value.Length}, {buffer.Length}");
		}
		fixed (byte* ptr = buffer)
		{
			fixed (byte* source = value)
			{
				Buffer.MemoryCopy(source, ptr + offset, value.Length, value.Length);
				offset += value.Length;
			}
		}
	}

	/// <summary>
	/// 将一个字节写入指定的缓冲区，并更新偏移量。
	/// </summary>
	/// <param name="buffer">要写入的缓冲区。</param>
	/// <param name="value">要写入的值。</param>
	/// <param name="offset">要写入值的缓冲区中的偏移量。</param>
	public unsafe static void WriteSByte(this byte[] buffer, sbyte value, ref int offset)
	{
		if (offset + 1 > buffer.Length)
		{
			offset++;
			return;
		}
		fixed (byte* ptr = buffer)
		{
			ptr[offset] = (byte)value;
			offset++;
		}
	}

	/// <summary>
	/// 将一个字符串写入指定的缓冲区，并更新偏移量。
	/// </summary>
	/// <param name="buffer">要写入的缓冲区。</param>
	/// <param name="value">要写入的值。</param>
	/// <param name="offset">要写入值的缓冲区中的偏移量。</param>
	public unsafe static void WriteString(this byte[] buffer, string value, ref int offset)
	{
		if (value == null)
		{
			value = string.Empty;
		}
		int byteCount = Encoding.UTF8.GetByteCount(value);
		if (byteCount > 32767)
		{
			throw new ArgumentException($"string length exceed short.MaxValue {byteCount}, {32767}");
		}
		if (offset + byteCount + 2 > buffer.Length)
		{
			offset += byteCount + 2;
			return;
		}
		fixed (byte* ptr = buffer)
		{
			Encoding.UTF8.GetBytes(value, 0, value.Length, buffer, offset + 2);
			buffer.WriteShort((short)byteCount, ref offset);
			offset += byteCount;
		}
	}

	/// <summary>
	/// 将一个布尔值写入指定的缓冲区，并更新偏移量。
	/// </summary>
	/// <param name="buffer">要写入的缓冲区。</param>
	/// <param name="value">要写入的值。</param>
	/// <param name="offset">要写入值的缓冲区中的偏移量。</param>
	public unsafe static void WriteBool(this byte[] buffer, bool value, ref int offset)
	{
		if (offset + 1 > buffer.Length)
		{
			offset++;
			return;
		}
		fixed (byte* ptr = buffer)
		{
			ptr[offset] = (value ? ((byte)1) : ((byte)0));
			offset++;
		}
	}

	/// <summary>
	/// 从给定的字节缓冲区中读取浮点数，并更新偏移量。
	/// </summary>
	/// <param name="buffer">包含了要读取数据的字节缓冲区。</param>
	/// <param name="offset">读取数据的起始位置，该方法会更新该值。</param>
	/// <returns>从字节缓冲区中读取的浮点数。</returns>
	/// <exception cref="T:System.Exception">当尝试读取的位置超出了缓冲区的边界时，会抛出此异常。</exception>
	public unsafe static float ReadFloat(this byte[] buffer, ref int offset)
	{
		if (offset > buffer.Length + 4)
		{
			throw new Exception("buffer read out of index");
		}
		fixed (byte* ptr = buffer)
		{
			*(int*)(ptr + offset) = IPAddress.NetworkToHostOrder(*(int*)(ptr + offset));
			float result = *(float*)(ptr + offset);
			offset += 4;
			return result;
		}
	}

	/// <summary>
	/// 从指定偏移量读取 double 类型数据。
	/// </summary>
	/// <param name="buffer">要操作的字节缓冲区。</param>
	/// <param name="offset">操作的起始偏移量，操作完成后，会自动累加双精度浮点数的字节数。</param>
	/// <returns>返回从缓冲区读取的 double 类型数据。</returns>
	public unsafe static double ReadDouble(this byte[] buffer, ref int offset)
	{
		if (offset > buffer.Length + 8)
		{
			throw new Exception("buffer read out of index");
		}
		fixed (byte* ptr = buffer)
		{
			*(long*)(ptr + offset) = IPAddress.NetworkToHostOrder(*(long*)(ptr + offset));
			double result = *(double*)(ptr + offset);
			offset += 8;
			return result;
		}
	}

	/// <summary>
	/// 从指定偏移量读取 byte 类型数据。
	/// </summary>
	/// <param name="buffer">要操作的字节缓冲区。</param>
	/// <param name="offset">操作的起始偏移量，操作完成后，会自动累加字节的字节数。</param>
	/// <returns>返回从缓冲区读取的 byte 类型数据。</returns>
	public unsafe static byte ReadByte(this byte[] buffer, ref int offset)
	{
		if (offset > buffer.Length + 1)
		{
			throw new Exception("buffer read out of index");
		}
		fixed (byte* ptr = buffer)
		{
			byte result = ptr[offset];
			offset++;
			return result;
		}
	}

	/// <summary>
	/// 从指定偏移量开始读取指定长度的字节数组。
	/// </summary>
	/// <param name="buffer">要操作的字节缓冲区。</param>
	/// <param name="offset">操作的起始偏移量。</param>
	/// <param name="len">需要读取的字节数组长度。</param>
	/// <returns>返回从缓冲区读取的 byte[] 类型数据。</returns>
	public static byte[] ReadBytes(this byte[] buffer, int offset, int len)
	{
		if (len <= 0 || offset > buffer.Length + len)
		{
			return Array.Empty<byte>();
		}
		byte[] array = new byte[len];
		Array.Copy(buffer, offset, array, 0, len);
		return array;
	}

	/// <summary>
	/// 从指定偏移量开始读取指定长度的字节数组。
	/// </summary>
	/// <param name="buffer">要操作的字节缓冲区。</param>
	/// <param name="offset">操作的起始偏移量。</param>
	/// <param name="len">需要读取的字节数组长度。</param>
	/// <returns>返回从缓冲区读取的 byte[] 类型数据。</returns>
	public static byte[] ReadBytes(this byte[] buffer, ref int offset, int len)
	{
		if (len <= 0 || offset > buffer.Length + len)
		{
			return Array.Empty<byte>();
		}
		byte[] array = new byte[len];
		Array.Copy(buffer, offset, array, 0, len);
		offset += len;
		return array;
	}

	/// <summary>
	/// 从指定偏移量开始读取指定长度的字节数组，长度作为 int 类型数据在字节数组的开头。
	/// </summary>
	/// <param name="buffer">要操作的字节缓冲区。</param>
	/// <param name="offset">操作的起始偏移量，操作完成后，会自动累加读取的字节长度以及 int 类型长度。</param>
	/// <returns>返回从缓冲区读取的 byte[] 类型数据。</returns>
	public static byte[] ReadBytes(this byte[] buffer, ref int offset)
	{
		int num = buffer.ReadInt(ref offset);
		if (num <= 0 || offset > buffer.Length + num)
		{
			return Array.Empty<byte>();
		}
		byte[] array = new byte[num];
		Array.Copy(buffer, offset, array, 0, num);
		offset += num;
		return array;
	}

	/// <summary>
	/// 从字节数组中以指定偏移量读取有符号字节。
	/// </summary>
	/// <param name="buffer">要从中读取数据的字节数组。</param>
	/// <param name="offset">读取数据的起始偏移量，此偏移量在读取后会自动增加。</param>
	/// <returns>读取的有符号字节。</returns>
	/// <exception cref="T:System.Exception">当偏移量超过数组长度时，将抛出异常。</exception>
	public unsafe static sbyte ReadSByte(this byte[] buffer, ref int offset)
	{
		if (offset > buffer.Length + 1)
		{
			throw new Exception("buffer read out of index");
		}
		fixed (byte* ptr = buffer)
		{
			byte result = ptr[offset];
			offset++;
			return (sbyte)result;
		}
	}

	/// <summary>
	/// 从字节数组中以指定偏移量读取字符串。
	/// </summary>
	/// <param name="buffer">要从中读取数据的字节数组。</param>
	/// <param name="offset">读取数据的起始偏移量，此偏移量在读取后会自动增加。</param>
	/// <returns>读取的字符串，若读取长度小于等于0或偏移量超出数组长度，返回空字符串。</returns>
	public unsafe static string ReadString(this byte[] buffer, ref int offset)
	{
		fixed (byte* ptr = buffer)
		{
			short num = buffer.ReadShort(ref offset);
			if (num <= 0 || offset > buffer.Length + num)
			{
				return string.Empty;
			}
			string @string = Encoding.UTF8.GetString(buffer, offset, num);
			offset += num;
			return @string;
		}
	}

	/// <summary>
	/// 从字节数组中以指定偏移量读取布尔值。
	/// </summary>
	/// <param name="buffer">要从中读取数据的字节数组。</param>
	/// <param name="offset">读取数据的起始偏移量，此偏移量在读取后会自动增加。</param>
	/// <returns>读取的布尔值。</returns>
	/// <exception cref="T:System.Exception">当偏移量超过数组长度时，将抛出异常。</exception>
	public unsafe static bool ReadBool(this byte[] buffer, ref int offset)
	{
		if (offset > buffer.Length + 1)
		{
			throw new Exception("buffer read out of index");
		}
		fixed (byte* ptr = buffer)
		{
			byte result = ptr[offset];
			offset++;
			return result != 0;
		}
	}
}
