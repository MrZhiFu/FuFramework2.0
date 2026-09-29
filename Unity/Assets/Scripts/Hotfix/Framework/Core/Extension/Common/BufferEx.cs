using System;
using System.Buffers.Binary;
using System.Text;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Core
{
	/// <summary>
	/// 字节数组相关的扩展方法。
	/// 功能：
	///     1. 字节数组的读写操作。
	///     2. 字节数组的转换操作。
	/// </summary>
	public static class BufferEx
	{
		/// <summary>
		/// 整型的大小
		/// </summary>
		public const int INT_SIZE = sizeof(int);

		/// <summary>
		/// 无符号整型的大小
		/// </summary>
		public const int UIntSize = sizeof(uint);

		/// <summary>
		/// 短整型的大小
		/// </summary>
		public const int SHORT_SIZE = sizeof(short);

		/// <summary>
		/// 无符号短整型的大小
		/// </summary>
		public const int UShortSize = sizeof(ushort);

		/// <summary>
		/// 长整型的大小
		/// </summary>
		public const int LONG_SIZE = sizeof(long);

		/// <summary>
		/// 单精度浮点数的大小
		/// </summary>
		public const int FLOAT_SIZE = sizeof(float);

		/// <summary>
		/// 双精度浮点数的大小
		/// </summary>
		public const int DOUBLE_SIZE = sizeof(double);

		/// <summary>
		/// 字节的大小
		/// </summary>
		public const int BYTE_SIZE = sizeof(byte);

		/// <summary>
		/// 有符号字节的大小
		/// </summary>
		public const int SBYTE_SIZE = sizeof(sbyte);

		/// <summary>
		/// 布尔值的大小
		/// </summary>
		public const int BOOL_SIZE = sizeof(bool);


		#region Write

		/// <summary>
		/// 将整数写入字节数组中的指定偏移量处。
		/// </summary>
		/// <param name="buffer">要写入的字节数组。</param>
		/// <param name="value">要写入的整数值。</param>
		/// <param name="offset">写入操作的偏移量。</param>
		public static unsafe void WriteInt(this byte[] buffer, int value, ref int offset)
		{
			if (offset + INT_SIZE > buffer.Length)
			{
				offset += INT_SIZE;
				return;
			}

			fixed (byte* ptr = buffer)
			{
				*(int*)(ptr + offset) =  System.Net.IPAddress.HostToNetworkOrder(value);
				offset                += INT_SIZE;
			}
		}

		/// <summary>
		/// 将无符号整数写入字节数组中的指定偏移量处。
		/// </summary>
		/// <param name="buffer">要写入的字节数组。</param>
		/// <param name="value">要写入的整数值。</param>
		/// <param name="offset">写入操作的偏移量。</param>
		public static void WriteUInt(this byte[] buffer, uint value, ref int offset)
		{
			if (offset + INT_SIZE > buffer.Length)
			{
				offset += INT_SIZE;
				return;
			}

			var     span  = buffer.AsSpan();
			ref var local = ref span;
			var start = offset;
			BinaryPrimitives.WriteUInt32BigEndian(local.Slice(start, local.Length - start), value);
			offset += INT_SIZE;
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
			}
			else
			{
				Span<byte>     span  = buffer.AsSpan();
				ref Span<byte> local = ref span;
				var start = offset;
				BinaryPrimitives.WriteUInt16BigEndian(local.Slice(start, local.Length - start), value);
				offset += 2;
			}
		}

		/// <summary>
		/// 将短整数写入字节数组中的指定偏移量处。
		/// </summary>
		/// <param name="buffer">要写入的字节数组。</param>
		/// <param name="value">要写入的短整数值。</param>
		/// <param name="offset">写入操作的偏移量。</param>
		public static unsafe void WriteShort(this byte[] buffer, short value, ref int offset)
		{
			if (offset + SHORT_SIZE > buffer.Length)
			{
				offset += SHORT_SIZE;
				return;
			}

			fixed (byte* ptr = buffer)
			{
				*(short*)(ptr + offset) =  System.Net.IPAddress.HostToNetworkOrder(value);
				offset                  += SHORT_SIZE;
			}
		}

		/// <summary>
		/// 将长整数写入字节数组中的指定偏移量处。
		/// </summary>
		/// <param name="buffer">要写入的字节数组。</param>
		/// <param name="value">要写入的长整数值。</param>
		/// <param name="offset">写入操作的偏移量。</param>
		public static unsafe void WriteLong(this byte[] buffer, long value, ref int offset)
		{
			if (offset + LONG_SIZE > buffer.Length)
			{
				offset += LONG_SIZE;
				return;
			}

			fixed (byte* ptr = buffer)
			{
				*(long*)(ptr + offset) =  System.Net.IPAddress.HostToNetworkOrder(value);
				offset                 += LONG_SIZE;
			}
		}

		/// <summary>
		/// 将单精度浮点数写入字节数组中的指定偏移量处。
		/// </summary>
		/// <param name="buffer">要写入的字节数组。</param>
		/// <param name="value">要写入的单精度浮点数值。</param>
		/// <param name="offset">字节数组中的偏移量，传递引用以便更新偏移量。</param>
		public static unsafe void WriteFloat(this byte[] buffer, float value, ref int offset)
		{
			if (offset + FLOAT_SIZE > buffer.Length)
			{
				offset += FLOAT_SIZE;
				return;
			}

			fixed (byte* ptr = buffer)
			{
				*(float*)(ptr + offset) =  value;
				*(int*)(ptr   + offset) =  System.Net.IPAddress.HostToNetworkOrder(*(int*)(ptr + offset));
				offset                  += FLOAT_SIZE;
			}
		}

		/// <summary>
		/// 将双精度浮点数写入字节数组中的指定偏移量处。
		/// </summary>
		/// <param name="buffer">要写入的字节数组。</param>
		/// <param name="value">要写入的双精度浮点数值。</param>
		/// <param name="offset">字节数组中的偏移量，传递引用以便更新偏移量。</param>
		public static unsafe void WriteDouble(this byte[] buffer, double value, ref int offset)
		{
			if (offset + DOUBLE_SIZE > buffer.Length)
			{
				offset += DOUBLE_SIZE;
				return;
			}

			fixed (byte* ptr = buffer)
			{
				*(double*)(ptr + offset) =  value;
				*(long*)(ptr   + offset) =  System.Net.IPAddress.HostToNetworkOrder(*(long*)(ptr + offset));
				offset                   += DOUBLE_SIZE;
			}
		}

		/// <summary>
		/// 将字节写入字节数组中的指定偏移量处。
		/// </summary>
		/// <param name="buffer">要写入的字节数组。</param>
		/// <param name="value">要写入的字节值。</param>
		/// <param name="offset">字节数组中的偏移量，传递引用以便更新偏移量。</param>
		public static unsafe void WriteByte(this byte[] buffer, byte value, ref int offset)
		{
			if (offset + BYTE_SIZE > buffer.Length)
			{
				offset += BYTE_SIZE;
				return;
			}

			fixed (byte* ptr = buffer)
			{
				*(ptr + offset) =  value;
				offset          += BYTE_SIZE;
			}
		}

		/// <summary>
		/// 在给定的偏移量位置，向缓冲区中写入字节序列，不包含长度信息。
		/// </summary>
		/// <param name="buffer">目标缓冲区。</param>
		/// <param name="value">要写入的字节数组。</param>
		/// <param name="offset">偏移量。</param>
		public static unsafe void WriteBytesWithoutLength(this byte[] buffer, byte[] value, ref int offset)
		{
			if (value == null)
			{
				buffer.WriteInt(0, ref offset);
				return;
			}

			if (offset + value.Length + INT_SIZE > buffer.Length)
			{
				throw new ArgumentException($"buffer write out of index {offset + value.Length + INT_SIZE}, {buffer.Length}");
			}

			fixed (byte* ptr = buffer, valPtr = value)
			{
				Buffer.MemoryCopy(valPtr, ptr + offset, value.Length, value.Length);
				offset += value.Length;
			}
		}

		/// <summary>
		/// 将字节数组写入到缓冲区中，同时更新偏移量。
		/// </summary>
		/// <param name="buffer">目标缓冲区。</param>
		/// <param name="value">要写入的字节数组。</param>
		/// <param name="offset">偏移量。</param>
		public static void WriteBytes(this byte[] buffer, byte[] value, ref int offset)
		{
			if (value == null)
			{
				buffer.WriteInt(0, ref offset);
				return;
			}

			if (offset + value.Length + INT_SIZE > buffer.Length)
			{
				offset += value.Length + INT_SIZE;
				return;
			}

			buffer.WriteInt(value.Length, ref offset);
			Array.Copy(value, 0, buffer, offset, value.Length);
			offset += value.Length;
		}

		/// <summary>
		/// 将有符号字节写入到缓冲区中，同时更新偏移量。
		/// </summary>
		/// <param name="buffer">目标缓冲区。</param>
		/// <param name="value">要写入的有符号字节。</param>
		/// <param name="offset">偏移量。</param>
		public static unsafe void WriteSByte(this byte[] buffer, sbyte value, ref int offset)
		{
			if (offset + SBYTE_SIZE > buffer.Length)
			{
				offset += SBYTE_SIZE;
				return;
			}

			fixed (byte* ptr = buffer)
			{
				*(sbyte*)(ptr + offset) =  value;
				offset                  += SBYTE_SIZE;
			}
		}

		/// <summary>
		/// 将字符串写入到缓冲区中，同时更新偏移量。
		/// </summary>
		/// <param name="buffer">目标缓冲区。</param>
		/// <param name="value">要写入的字符串。</param>
		/// <param name="offset">偏移量。</param>
		public static unsafe void WriteString(this byte[] buffer, string value, ref int offset)
		{
			if (value == null)
			{
				value = string.Empty;
			}

			var len = Encoding.UTF8.GetByteCount(value);

			if (len > short.MaxValue)
			{
				throw new ArgumentException($"字符串长度超过了 short.MaxValue {len}, {short.MaxValue}");
			}

			// 预判已经超出长度了，直接计算长度就行了
			if (offset + len + SHORT_SIZE > buffer.Length)
			{
				offset += len + SHORT_SIZE;
				return;
			}

			// ReSharper disable once UnusedVariable
			fixed (byte* ptr = buffer)
			{
				Encoding.UTF8.GetBytes(value, 0, value.Length, buffer, offset + SHORT_SIZE);
				buffer.WriteShort((short)len, ref offset);
				offset += len;
			}
		}

		/// <summary>
		/// 将布尔值写入到缓冲区中，同时更新偏移量。
		/// </summary>
		/// <param name="buffer">目标缓冲区。</param>
		/// <param name="value">要写入的布尔值。</param>
		/// <param name="offset">偏移量。</param>
		public static unsafe void WriteBool(this byte[] buffer, bool value, ref int offset)
		{
			if (offset + BOOL_SIZE > buffer.Length)
			{
				offset += BOOL_SIZE;
				return;
			}

			fixed (byte* ptr = buffer)
			{
				*(bool*)(ptr + offset) =  value;
				offset                 += BOOL_SIZE;
			}
		}

		#endregion

		#region Read

		/// <summary>
		/// 从字节数组中读取一个整数值。
		/// </summary>
		/// <param name="buffer">包含整数值的字节数组。</param>
		/// <param name="offset">从字节数组中读取整数值的偏移量。</param>
		/// <returns>从字节数组中读取的整数值。</returns>
		public static unsafe int ReadInt(this byte[] buffer, ref int offset)
		{
			if (offset < 0 || offset + INT_SIZE > buffer.Length)
			{
				throw new ArgumentOutOfRangeException(nameof(offset), "buffer read out of index");
			}

			fixed (byte* ptr = buffer)
			{
				var value = *(int*)(ptr + offset);
				offset += INT_SIZE;
				return System.Net.IPAddress.NetworkToHostOrder(value);
			}
		}

		/// <summary>
		/// 从字节数组中读取一个无符号整数值。
		/// </summary>
		/// <param name="buffer">包含整数值的字节数组。</param>
		/// <param name="offset">从字节数组中读取整数值的偏移量。</param>
		/// <returns>从字节数组中读取的无符号整数值。</returns>
		/// <exception cref="Exception"></exception>
		public static uint ReadUInt(this byte[] buffer, ref int offset)
		{
			if (offset < 0 || offset + UIntSize > buffer.Length)
			{
				throw new Exception("buffer read out of index");
			}

			Span<byte>     span  = buffer.AsSpan();
			ref Span<byte> local = ref span;
			var start = offset;
			var num   = (int)BinaryPrimitives.ReadUInt32BigEndian(local.Slice(start, local.Length - start));
			offset += UIntSize;
			return (uint)num;
		}

		/// <summary>
		/// 从字节数组中读取一个短整数值。
		/// </summary>
		/// <param name="buffer">包含短整数值的字节数组。</param>
		/// <param name="offset">从字节数组中读取短整数值的偏移量。</param>
		/// <returns>从字节数组中读取的短整数值。</returns>
		public static unsafe short ReadShort(this byte[] buffer, ref int offset)
		{
			if (offset < 0 || offset + SHORT_SIZE > buffer.Length)
			{
				throw new ArgumentOutOfRangeException(nameof(offset), "buffer read out of index");
			}

			fixed (byte* ptr = buffer)
			{
				var value = *(short*)(ptr + offset);
				offset += SHORT_SIZE;
				return System.Net.IPAddress.NetworkToHostOrder(value);
			}
		}

		/// <summary>
		/// 从字节数组中读取16位无符号整数，并将偏移量向前移动。
		/// </summary>
		/// <param name="buffer">要读取的字节数组。</param>
		/// <param name="offset">引用偏移量。</param>
		/// <returns>返回读取的16位无符号整数。</returns>
		public static ushort ReadUShort(this byte[] buffer, ref int offset)
		{
			if (offset < 0 || offset + UShortSize > buffer.Length)
			{
				throw new Exception("buffer read out of index");
			}

			Span<byte>     span  = buffer.AsSpan();
			ref Span<byte> local = ref span;
			var start = offset;
			var num   = BinaryPrimitives.ReadUInt16BigEndian(local.Slice(start, local.Length - start));
			offset += UShortSize;
			return (ushort)num;
		}

		/// <summary>
		/// 从字节数组中读取一个长整型数值。
		/// </summary>
		/// <param name="buffer">字节数组。</param>
		/// <param name="offset">偏移量。</param>
		/// <returns>长整型数值。</returns>
		public static unsafe long ReadLong(this byte[] buffer, ref int offset)
		{
			if (offset < 0 || offset + LONG_SIZE > buffer.Length)
			{
				throw new ArgumentOutOfRangeException(nameof(offset), "buffer read out of index");
			}

			fixed (byte* ptr = buffer)
			{
				var value = *(long*)(ptr + offset);
				offset += LONG_SIZE;
				return System.Net.IPAddress.NetworkToHostOrder(value);
			}
		}

		/// <summary>
		/// 从字节数组中读取一个单精度浮点数值。
		/// </summary>
		/// <param name="buffer">字节数组。</param>
		/// <param name="offset">偏移量。</param>
		/// <returns>单精度浮点数值。</returns>
		public static unsafe float ReadFloat(this byte[] buffer, ref int offset)
		{
			if (offset < 0 || offset + FLOAT_SIZE > buffer.Length)
			{
				throw new ArgumentOutOfRangeException(nameof(offset), "buffer read out of index");
			}

			// 大端序解码：先读入局部变量再翻转，不原地改写源数组（保证读操作幂等，同一 buffer 可重复解析）
			int raw;
			fixed (byte* ptr = buffer)
			{
				raw = System.Net.IPAddress.NetworkToHostOrder(*(int*)(ptr + offset));
			}

			var value = *(float*)&raw;
			offset += FLOAT_SIZE;
			return value;
		}

		/// <summary>
		/// 从字节数组中读取一个双精度浮点数值。
		/// </summary>
		/// <param name="buffer">字节数组。</param>
		/// <param name="offset">偏移量。</param>
		/// <returns>双精度浮点数值。</returns>
		public static unsafe double ReadDouble(this byte[] buffer, ref int offset)
		{
			if (offset < 0 || offset + DOUBLE_SIZE > buffer.Length)
			{
				throw new ArgumentOutOfRangeException(nameof(offset), "buffer read out of index");
			}

			// 大端序解码：先读入局部变量再翻转，不原地改写源数组（保证读操作幂等，同一 buffer 可重复解析）
			long raw;
			fixed (byte* ptr = buffer)
			{
				raw = System.Net.IPAddress.NetworkToHostOrder(*(long*)(ptr + offset));
			}

			var value = *(double*)&raw;
			offset += DOUBLE_SIZE;
			return value;
		}

		/// <summary>
		/// 从字节数组中读取一个字节。
		/// </summary>
		/// <param name="buffer">字节数组。</param>
		/// <param name="offset">偏移量。</param>
		/// <returns>字节值。</returns>
		public static unsafe byte ReadByte(this byte[] buffer, ref int offset)
		{
			if (offset < 0 || offset + BYTE_SIZE > buffer.Length)
			{
				throw new ArgumentOutOfRangeException(nameof(offset), "buffer read out of index");
			}

			fixed (byte* ptr = buffer)
			{
				var value = *(ptr + offset);
				offset += BYTE_SIZE;
				return value;
			}
		}

		/// <summary>
		/// 从字节数组中读取一定长度的字节。
		/// </summary>
		/// <param name="buffer">字节数组。</param>
		/// <param name="offset">偏移量。</param>
		/// <param name="len">数据长度。</param>
		/// <returns>读取的字节数组。</returns>
		public static byte[] ReadBytes(this byte[] buffer, int offset, int len)
		{
			//数据不可信：len 极大时 len * BYTE_SIZE 会整数溢出，改为「剩余可读长度」上界判断
			if (len <= 0 || offset < 0 || offset > buffer.Length || len > buffer.Length - offset)
			{
				return Array.Empty<byte>();
			}

			var data = new byte[len];
			Array.Copy(buffer, offset, data, 0, len);
			return data;
		}

		/// <summary>
		/// 从字节数组中读取一定长度的字节。
		/// </summary>
		/// <param name="buffer">字节数组。</param>
		/// <param name="offset">偏移量。</param>
		/// <param name="len">数据长度。</param>
		/// <returns>读取的字节数组。</returns>
		public static byte[] ReadBytes(this byte[] buffer, ref int offset, int len)
		{
			//数据不可信：len 极大时 len * BYTE_SIZE 会整数溢出，改为「剩余可读长度」上界判断
			if (len <= 0 || offset < 0 || offset > buffer.Length || len > buffer.Length - offset)
			{
				return Array.Empty<byte>();
			}

			var data = new byte[len];
			Array.Copy(buffer, offset, data, 0, len);
			offset += len;
			return data;
		}

		/// <summary>
		/// 从字节数组中读取一定长度的字节。
		/// </summary>
		/// <param name="buffer">字节数组。</param>
		/// <param name="offset">偏移量。</param>
		/// <returns>读取的字节数组。</returns>
		public static byte[] ReadBytes(this byte[] buffer, ref int offset)
		{
			var len = ReadInt(buffer, ref offset);
			//数据不可信：len 极大时 len * BYTE_SIZE 会整数溢出，改为「剩余可读长度」上界判断
			if (len <= 0 || offset < 0 || offset > buffer.Length || len > buffer.Length - offset)
			{
				return Array.Empty<byte>();
			}

			var data = new byte[len];
			Array.Copy(buffer, offset, data, 0, len);
			offset += len;
			return data;
		}

		/// <summary>
		/// 从字节数组中读取有符号字节。
		/// </summary>
		/// <param name="buffer">字节数组。</param>
		/// <param name="offset">偏移量。</param>
		/// <returns>读取的有符号字节。</returns>
		public static unsafe sbyte ReadSByte(this byte[] buffer, ref int offset)
		{
			if (offset < 0 || offset + BYTE_SIZE > buffer.Length)
			{
				throw new ArgumentOutOfRangeException(nameof(offset), "buffer read out of index");
			}

			fixed (byte* ptr = buffer)
			{
				var value = *(sbyte*)(ptr + offset);
				offset += BYTE_SIZE;
				return value;
			}
		}

		/// <summary>
		/// 从字节数组中读取字符串。
		/// </summary>
		/// <param name="buffer">字节数组。</param>
		/// <param name="offset">偏移量。</param>
		/// <returns>读取的字符串。</returns>
		public static unsafe string ReadString(this byte[] buffer, ref int offset)
		{
			// ReSharper disable once UnusedVariable
			fixed (byte* ptr = buffer)
			{
				var len = ReadShort(buffer, ref offset);
				//数据不可信：len 为 short 但长度上界判断同样不能用乘法（offset 可能已被前序解析推到非法值）
				if (len <= 0 || offset < 0 || offset > buffer.Length || len > buffer.Length - offset)
					return "";

				var value = Encoding.UTF8.GetString(buffer, offset, len);
				offset += len;
				return value;
			}
		}

		/// <summary>
		/// 从字节数组中读取布尔值。
		/// </summary>
		/// <param name="buffer">字节数组。</param>
		/// <param name="offset">偏移量。</param>
		/// <returns>读取的布尔值。</returns>
		public static unsafe bool ReadBool(this byte[] buffer, ref int offset)
		{
			if (offset < 0 || offset + BOOL_SIZE > buffer.Length)
			{
				throw new ArgumentOutOfRangeException(nameof(offset), "buffer read out of index");
			}

			fixed (byte* ptr = buffer)
			{
				var value = *(bool*)(ptr + offset);
				offset += BOOL_SIZE;
				return value;
			}
		}

		#endregion

		/// <summary>
		/// 将字节数组转换为字符串
		/// </summary>
		/// <param name="bytes"></param>
		/// <returns></returns>
		public static string ToArrayString(this byte[] bytes)
		{
			StringBuilder.Clear();
			foreach (byte b in bytes)
			{
				StringBuilder.Append(b + " ");
			}

			return StringBuilder.ToString();
		}

		private static readonly StringBuilder StringBuilder = new StringBuilder();

		/// <summary>
		/// 将字节转换为十六进制字符串。
		/// </summary>
		/// <param name="b">要转换的字节。</param>
		/// <returns>表示字节的十六进制字符串。</returns>
		public static string ToHex(this byte b)
		{
			return b.ToString("X2");
		}

		/// <summary>
		/// 将字节数组转换为十六进制字符串。
		/// </summary>
		/// <param name="bytes">要转换的字节数组。</param>
		/// <returns>表示字节数组的十六进制字符串。</returns>
		public static string ToHex(this byte[] bytes)
		{
			StringBuilder.Clear();
			foreach (byte b in bytes)
			{
				StringBuilder.Append(b.ToString("X2"));
			}

			return StringBuilder.ToString();
		}

		/// <summary>
		/// 使用指定格式将字节数组转换为十六进制字符串。
		/// </summary>
		/// <param name="bytes">要转换的字节数组。</param>
		/// <param name="format">十六进制格式。</param>
		/// <returns>表示字节数组的十六进制字符串。</returns>
		public static string ToHex(this byte[] bytes, string format)
		{
			StringBuilder.Clear();
			foreach (byte b in bytes)
			{
				StringBuilder.Append(b.ToString(format));
			}

			return StringBuilder.ToString();
		}

		/// <summary>
		/// 将字节数组中指定范围的字节转换为十六进制字符串。
		/// </summary>
		/// <param name="bytes">要转换的字节数组。</param>
		/// <param name="offset">起始偏移量。</param>
		/// <param name="count">要转换的字节数。</param>
		/// <returns>表示指定范围内字节的十六进制字符串。</returns>
		public static string ToHex(this byte[] bytes, int offset, int count)
		{
			StringBuilder.Clear();
			for (int i = offset; i < offset + count; ++i)
			{
				StringBuilder.Append(bytes[i].ToString("X2"));
			}

			return StringBuilder.ToString();
		}

		/// <summary>
		/// 将字节数组转换为字符串，使用默认编码。
		/// </summary>
		/// <param name="bytes">要转换的字节数组。</param>
		/// <returns>转换后的字符串。</returns>
		public static string ToDefaultString(this byte[] bytes)
		{
			return Encoding.Default.GetString(bytes);
		}

		/// <summary>
		/// 将字节数组的一部分转换为字符串，使用默认编码。
		/// </summary>
		/// <param name="bytes">要转换的字节数组。</param>
		/// <param name="index">起始位置。</param>
		/// <param name="count">要转换的字节数。</param>
		/// <returns>转换后的字符串。</returns>
		public static string ToDefaultString(this byte[] bytes, int index, int count)
		{
			return Encoding.Default.GetString(bytes, index, count);
		}

		/// <summary>
		/// 将字节数组转换为字符串，使用UTF-8编码。
		/// </summary>
		/// <param name="bytes">要转换的字节数组。</param>
		/// <returns>转换后的字符串。</returns>
		public static string ToUtf8String(this byte[] bytes)
		{
			return Encoding.UTF8.GetString(bytes);
		}

		/// <summary>
		/// 将字节数组的一部分转换为字符串，使用UTF-8编码。
		/// </summary>
		/// <param name="bytes">要转换的字节数组。</param>
		/// <param name="index">起始位置。</param>
		/// <param name="count">要转换的字节数。</param>
		/// <returns>转换后的字符串。</returns>
		public static string ToUtf8String(this byte[] bytes, int index, int count)
		{
			return Encoding.UTF8.GetString(bytes, index, count);
		}
	}
}
