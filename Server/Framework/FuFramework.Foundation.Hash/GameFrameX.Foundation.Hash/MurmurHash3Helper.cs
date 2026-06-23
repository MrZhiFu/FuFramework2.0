using System;
using System.Text;

namespace FuFramework.Foundation.Hash;

/// <summary>
/// MurmurHash3 计算工具类。
/// MurmurHash是一种非加密型哈希算法，具有高性能和良好的哈希分布特性。
/// 此实现基于MurmurHash3的32位版本。
/// </summary>
public static class MurmurHash3Helper
{
	/// <summary>
	/// 使用 MurmurHash3 算法计算字符串的哈希值。
	/// 将字符串按UTF-8编码转换为字节数组后进行哈希计算。
	/// </summary>
	/// <param name="str">要计算哈希值的字符串</param>
	/// <param name="seed">哈希算法的种子值，默认为27。不同的种子值会产生不同的哈希结果。</param>
	/// <returns>32位无符号整数形式的哈希值</returns>
	public static uint Hash(string str, uint seed = 27u)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(str);
		return Hash(bytes, (uint)bytes.Length, seed);
	}

	/// <summary>
	/// 使用 MurmurHash3 算法计算字节数组的哈希值。
	/// 此方法实现了MurmurHash3的核心算法逻辑。
	/// </summary>
	/// <param name="data">要计算哈希值的字节数组</param>
	/// <param name="length">字节数组的有效长度</param>
	/// <param name="seed">哈希算法的种子值，用于初始化哈希计算</param>
	/// <returns>32位无符号整数形式的哈希值</returns>
	public static uint Hash(byte[] data, uint length, uint seed)
	{
		uint num = length >> 2;
		uint num2 = seed;
		int num3 = 0;
		for (uint num4 = num; num4 != 0; num4--)
		{
			uint num5 = BitConverter.ToUInt32(data, num3);
			num5 *= 3432918353u;
			num5 = Rotl32(num5, 15);
			num5 *= 461845907;
			num2 ^= num5;
			num2 = Rotl32(num2, 13);
			num2 = num2 * 5 + 3864292196u;
			num3 += 4;
		}
		num <<= 2;
		uint num6 = 0u;
		uint num7 = length & 3;
		if (num7 == 3)
		{
			num6 ^= (uint)(data[2 + num] << 16);
		}
		if (num7 >= 2)
		{
			num6 ^= (uint)(data[1 + num] << 8);
		}
		if (num7 >= 1)
		{
			num6 ^= data[num];
			num6 *= 3432918353u;
			num6 = Rotl32(num6, 15);
			num6 *= 461845907;
			num2 ^= num6;
		}
		num2 ^= length;
		return Fmix32(num2);
	}

	/// <summary>
	/// 对哈希值进行最终混合操作。
	/// 通过多次异或、乘法和位移操作增加最终哈希值的随机性。
	/// </summary>
	/// <param name="h">要混合的哈希值</param>
	/// <returns>混合后的最终哈希值</returns>
	private static uint Fmix32(uint h)
	{
		h ^= h >> 16;
		h *= 2246822507u;
		h ^= h >> 13;
		h *= 3266489909u;
		h ^= h >> 16;
		return h;
	}

	/// <summary>
	/// 对32位整数进行循环左移操作。
	/// 循环左移是指将溢出的高位补到低位。
	/// </summary>
	/// <param name="x">要进行循环左移的整数</param>
	/// <param name="r">左移的位数</param>
	/// <returns>循环左移后的整数</returns>
	private static uint Rotl32(uint x, byte r)
	{
		return (x << (int)r) | (x >> 32 - r);
	}
}
