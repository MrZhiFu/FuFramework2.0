using System;
using System.Runtime.CompilerServices;
using System.Text;
using Standart.Hash.xxHash;

namespace FuFramework.Foundation.Hash;

/// <summary>
/// xxHash 哈希算法工具类。
/// 提供32位、64位和128位哈希值计算功能。
/// xxHash是一种非加密型哈希算法，专注于高性能和高质量的哈希计算。
/// </summary>
public static class XxHashHelper
{
	/// <summary>
	/// 内部xxHash实现帮助类。
	/// 提供32位和64位哈希算法的底层实现。
	/// </summary>
	private static class InternalXxHashHelper
	{
		/// <summary>
		/// 计算32位xxHash值的核心算法。
		/// 直接操作内存指针以获得最佳性能。
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe static uint Hash32(byte* input, int length, uint seed = 0u)
		{
			uint num = seed + 374761393;
			if (length >= 16)
			{
				uint num2 = (uint)((int)seed + -1640531535 + -2048144777);
				uint num3 = seed + 2246822519u;
				uint num4 = seed;
				uint num5 = seed - 2654435761u;
				int num6 = length >> 4;
				for (int i = 0; i < num6; i++)
				{
					uint num7 = *(uint*)input;
					uint num8 = *(uint*)(input + 4);
					uint num9 = *(uint*)(input + 8);
					uint num10 = *(uint*)(input + 12);
					num2 += (uint)((int)num7 * -2048144777);
					num2 = (num2 << 13) | (num2 >> 19);
					num2 *= 2654435761u;
					num3 += (uint)((int)num8 * -2048144777);
					num3 = (num3 << 13) | (num3 >> 19);
					num3 *= 2654435761u;
					num4 += (uint)((int)num9 * -2048144777);
					num4 = (num4 << 13) | (num4 >> 19);
					num4 *= 2654435761u;
					num5 += (uint)((int)num10 * -2048144777);
					num5 = (num5 << 13) | (num5 >> 19);
					num5 *= 2654435761u;
					input += 16;
				}
				num = ((num2 << 1) | (num2 >> 31)) + ((num3 << 7) | (num3 >> 25)) + ((num4 << 12) | (num4 >> 20)) + ((num5 << 18) | (num5 >> 14));
			}
			num += (uint)length;
			for (length &= 0xF; length >= 4; length -= 4)
			{
				num += (uint)((int)(*(uint*)input) * -1028477379);
				num = ((num << 17) | (num >> 15)) * 668265263;
				input += 4;
			}
			while (length > 0)
			{
				num += (uint)(*input * 374761393);
				num = ((num << 11) | (num >> 21)) * 2654435761u;
				input++;
				length--;
			}
			num ^= num >> 15;
			num *= 2246822519u;
			num ^= num >> 13;
			num *= 3266489917u;
			return num ^ (num >> 16);
		}

		/// <summary>
		/// 计算64位xxHash值的核心算法。
		/// 直接操作内存指针以获得最佳性能。
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe static ulong Hash64(byte* input, int length, uint seed = 0u)
		{
			ulong num = (ulong)seed + 2870177450012600261uL;
			if (length >= 32)
			{
				ulong num2 = (ulong)(seed + -7046029288634856825L + -4417276706812531889L);
				ulong num3 = (ulong)(seed + -4417276706812531889L);
				ulong num4 = seed;
				ulong num5 = (ulong)(seed - -7046029288634856825L);
				int num6 = length >> 5;
				for (int i = 0; i < num6; i++)
				{
					ulong num7 = *(ulong*)input;
					ulong num8 = *(ulong*)(input + 8);
					ulong num9 = *(ulong*)(input + 16);
					ulong num10 = *(ulong*)(input + 24);
					num2 += (ulong)((long)num7 * -4417276706812531889L);
					num2 = (num2 << 31) | (num2 >> 33);
					num2 *= 11400714785074694791uL;
					num3 += (ulong)((long)num8 * -4417276706812531889L);
					num3 = (num3 << 31) | (num3 >> 33);
					num3 *= 11400714785074694791uL;
					num4 += (ulong)((long)num9 * -4417276706812531889L);
					num4 = (num4 << 31) | (num4 >> 33);
					num4 *= 11400714785074694791uL;
					num5 += (ulong)((long)num10 * -4417276706812531889L);
					num5 = (num5 << 31) | (num5 >> 33);
					num5 *= 11400714785074694791uL;
					input += 32;
				}
				num = ((num2 << 1) | (num2 >> 63)) + ((num3 << 7) | (num3 >> 57)) + ((num4 << 12) | (num4 >> 52)) + ((num5 << 18) | (num5 >> 46));
				num2 *= 14029467366897019727uL;
				num2 = (num2 << 31) | (num2 >> 33);
				num2 *= 11400714785074694791uL;
				num ^= num2;
				num = (ulong)((long)num * -7046029288634856825L + -8796714831421723037L);
				num3 *= 14029467366897019727uL;
				num3 = (num3 << 31) | (num3 >> 33);
				num3 *= 11400714785074694791uL;
				num ^= num3;
				num = (ulong)((long)num * -7046029288634856825L + -8796714831421723037L);
				num4 *= 14029467366897019727uL;
				num4 = (num4 << 31) | (num4 >> 33);
				num4 *= 11400714785074694791uL;
				num ^= num4;
				num = (ulong)((long)num * -7046029288634856825L + -8796714831421723037L);
				num5 *= 14029467366897019727uL;
				num5 = (num5 << 31) | (num5 >> 33);
				num5 *= 11400714785074694791uL;
				num ^= num5;
				num = (ulong)((long)num * -7046029288634856825L + -8796714831421723037L);
			}
			num += (ulong)length;
			for (length &= 0x1F; length >= 8; length -= 8)
			{
				ulong num11 = (ulong)(*(long*)input * -4417276706812531889L);
				num11 = ((num11 << 31) | (num11 >> 33)) * 11400714785074694791uL;
				num ^= num11;
				num = (ulong)((long)((num << 27) | (num >> 37)) * -7046029288634856825L + -8796714831421723037L);
				input += 8;
			}
			if (length >= 4)
			{
				num ^= (ulong)((uint)(*(int*)input) * -7046029288634856825L);
				num = (ulong)((long)((num << 23) | (num >> 41)) * -4417276706812531889L + 1609587929392839161L);
				input += 4;
				length -= 4;
			}
			while (length > 0)
			{
				num ^= (ulong)(*input * 2870177450012600261L);
				num = ((num << 11) | (num >> 53)) * 11400714785074694791uL;
				input++;
				length--;
			}
			num ^= num >> 33;
			num *= 14029467366897019727uL;
			num ^= num >> 29;
			num *= 1609587929392839161L;
			return num ^ (num >> 32);
		}

		/// <summary>
		/// 计算字节数组的32位哈希值。
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static uint Hash32(byte[] buffer)
		{
			int length = buffer.Length;
			fixed (byte* input = buffer)
			{
				return Hash32(input, length);
			}
		}

		/// <summary>
		/// 计算字符串的32位哈希值。
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint Hash32(string text)
		{
			return Hash32(Encoding.UTF8.GetBytes(text));
		}

		/// <summary>
		/// 计算类型的32位哈希值。
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint Hash32(Type type)
		{
			return Hash32(type.FullName);
		}

		/// <summary>
		/// 计算泛型类型的32位哈希值。
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint Hash32<T>()
		{
			return Hash32(typeof(T));
		}

		/// <summary>
		/// 计算字节数组的64位哈希值。
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static ulong Hash64(byte[] buffer)
		{
			int length = buffer.Length;
			fixed (byte* input = buffer)
			{
				return Hash64(input, length);
			}
		}

		/// <summary>
		/// 计算字符串的64位哈希值。
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ulong Hash64(string text)
		{
			return Hash64(Encoding.UTF8.GetBytes(text));
		}

		/// <summary>
		/// 计算类型的64位哈希值。
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ulong Hash64(Type type)
		{
			return Hash64(type.FullName);
		}

		/// <summary>
		/// 计算泛型类型的64位哈希值。
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ulong Hash64<T>()
		{
			return Hash64(typeof(T));
		}
	}

	/// <summary>
	/// 计算给定字节数组的32位哈希值。
	/// </summary>
	/// <param name="buffer">要计算哈希值的字节数组</param>
	/// <returns>32位无符号整数形式的哈希值</returns>
	public static ulong Hash32(byte[] buffer)
	{
		return xxHash32.ComputeHash(buffer);
	}

	/// <summary>
	/// 计算给定文本的32位哈希值。
	/// 使用UTF-8编码将文本转换为字节数组后计算哈希值。
	/// </summary>
	/// <param name="text">要计算哈希值的文本</param>
	/// <returns>32位无符号整数形式的哈希值</returns>
	public static uint Hash32(string text)
	{
		return xxHash32.ComputeHash(text);
	}

	/// <summary>
	/// 计算给定类型的32位哈希值。
	/// 基于类型的完全限定名计算哈希值。
	/// </summary>
	/// <param name="type">要计算哈希值的类型</param>
	/// <returns>32位无符号整数形式的哈希值</returns>
	public static uint Hash32(Type type)
	{
		return InternalXxHashHelper.Hash32(type);
	}

	/// <summary>
	/// 计算给定泛型类型参数的32位哈希值。
	/// 基于类型的完全限定名计算哈希值。
	/// </summary>
	/// <typeparam name="T">要计算哈希值的泛型类型参数</typeparam>
	/// <returns>32位无符号整数形式的哈希值</returns>
	public static uint Hash32<T>()
	{
		return InternalXxHashHelper.Hash32<T>();
	}

	/// <summary>
	/// 计算给定字节数组的64位哈希值。
	/// </summary>
	/// <param name="buffer">要计算哈希值的字节数组</param>
	/// <returns>64位无符号整数形式的哈希值</returns>
	public static ulong Hash64(byte[] buffer)
	{
		return xxHash64.ComputeHash(buffer, 0uL);
	}

	/// <summary>
	/// 计算给定文本的64位哈希值。
	/// 使用UTF-8编码将文本转换为字节数组后计算哈希值。
	/// </summary>
	/// <param name="text">要计算哈希值的文本</param>
	/// <returns>64位无符号整数形式的哈希值</returns>
	public static ulong Hash64(string text)
	{
		return xxHash64.ComputeHash(text);
	}

	/// <summary>
	/// 计算给定类型的64位哈希值。
	/// 基于类型的完全限定名计算哈希值。
	/// </summary>
	/// <param name="type">要计算哈希值的类型</param>
	/// <returns>64位无符号整数形式的哈希值</returns>
	public static ulong Hash64(Type type)
	{
		return InternalXxHashHelper.Hash64(type);
	}

	/// <summary>
	/// 计算给定泛型类型参数的64位哈希值。
	/// 基于类型的完全限定名计算哈希值。
	/// </summary>
	/// <typeparam name="T">要计算哈希值的泛型类型参数</typeparam>
	/// <returns>64位无符号整数形式的哈希值</returns>
	public static ulong Hash64<T>()
	{
		return InternalXxHashHelper.Hash64<T>();
	}

	/// <summary>
	/// 计算给定字节数组的128位哈希值。
	/// 使用数组的全部长度进行计算。
	/// </summary>
	/// <param name="buffer">要计算哈希值的字节数组</param>
	/// <returns>128位无符号整数形式的哈希值</returns>
	public static uint128 Hash128(byte[] buffer)
	{
		return xxHash128.ComputeHash(buffer, buffer.Length, 0uL);
	}

	/// <summary>
	/// 判断128位哈希值是否为默认值(全0)。
	/// </summary>
	/// <param name="self">要判断的128位哈希值</param>
	/// <returns>如果高64位和低64位都为0则返回true，否则返回false</returns>
	public static bool IsDefault(uint128 self)
	{
		if (self.high64 == 0L)
		{
			return self.low64 == 0;
		}
		return false;
	}

	/// <summary>
	/// 计算给定字节数组的128位哈希值。
	/// 使用指定的长度进行计算。
	/// </summary>
	/// <param name="buffer">要计算哈希值的字节数组</param>
	/// <param name="length">要参与计算的字节长度</param>
	/// <returns>128位无符号整数形式的哈希值</returns>
	public static uint128 Hash128(byte[] buffer, int length)
	{
		return xxHash128.ComputeHash(buffer, length, 0uL);
	}

	/// <summary>
	/// 计算给定文本的128位哈希值。
	/// 使用UTF-8编码将文本转换为字节数组后计算哈希值。
	/// </summary>
	/// <param name="text">要计算哈希值的文本</param>
	/// <returns>128位无符号整数形式的哈希值</returns>
	public static uint128 Hash128(string text)
	{
		return xxHash128.ComputeHash(text, 0uL);
	}
}
