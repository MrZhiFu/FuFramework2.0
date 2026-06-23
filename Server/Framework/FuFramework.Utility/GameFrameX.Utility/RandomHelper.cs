using System;
using System.Collections.Generic;
using System.Linq;
using FuFramework.Utility.Extensions;
using Tedd.RandomUtils;

namespace FuFramework.Utility;

/// <summary>
/// 随机相关的实用函数。
/// </summary>
public static class RandomHelper
{
	private static readonly Random RandomShared = Random.Shared;

	/// <summary>
	/// 获取UInt32范围内的随机数。
	/// </summary>
	/// <returns>一个大于等于零且小于 UInt32.MaxValue 的 32 位无符号整数。</returns>
	public static uint NextUInt32()
	{
		byte[] array = new byte[4];
		RandomShared.NextBytes(array);
		return BitConverter.ToUInt32(array, 0);
	}

	/// <summary>
	/// 返回非负随机数。
	/// </summary>
	/// <returns>大于等于零且小于 System.Int32.MaxValue 的 32 位带符号整数。</returns>
	public static int Next()
	{
		return RandomShared.Next();
	}

	/// <summary>
	/// 返回一个小于所指定最大值的非负随机数。
	/// </summary>
	/// <param name="maxValue">要生成的随机数的上界（随机数不能取该上界值）。maxValue 必须大于等于零。</param>
	/// <returns>大于等于零且小于 maxValue 的 32 位带符号整数，即：返回值的范围通常包括零但不包括 maxValue。不过，如果 maxValue 等于零，则返回 maxValue。</returns>
	public static int Next(int maxValue)
	{
		return RandomShared.Next(maxValue);
	}

	/// <summary>
	/// 返回一个指定范围内的随机数。
	/// </summary>
	/// <param name="minValue">返回的随机数的下界（随机数可取该下界值）。</param>
	/// <param name="maxValue">返回的随机数的上界（随机数不能取该上界值）。maxValue 必须大于等于 minValue。</param>
	/// <returns>一个大于等于 minValue 且小于 maxValue 的 32 位带符号整数，即：返回的值范围包括 minValue 但不包括 maxValue。如果 minValue 等于 maxValue，则返回 minValue。</returns>
	public static int Next(int minValue, int maxValue)
	{
		return RandomShared.Next(minValue, maxValue);
	}

	/// <summary>
	/// 返回非负随机数。
	/// </summary>
	/// <returns>大于等于零且小于 System.Int64.MaxValue 的 64 位带符号整数。</returns>
	public static long NextInt64()
	{
		return RandomShared.NextInt64();
	}

	/// <summary>
	/// 返回一个小于所指定最大值的非负随机数。
	/// </summary>
	/// <param name="maxValue">要生成的随机数的上界（随机数不能取该上界值）。maxValue 必须大于等于零。</param>
	/// <returns>大于等于零且小于 maxValue 的 64 位带符号整数，即：返回值的范围通常包括零但不包括 maxValue。不过，如果 maxValue 等于零，则返回 maxValue。</returns>
	public static long NextInt64(int maxValue)
	{
		return RandomShared.NextInt64(maxValue);
	}

	/// <summary>
	/// 返回一个指定范围内的随机数。
	/// </summary>
	/// <param name="minValue">返回的随机数的下界（随机数可取该下界值）。</param>
	/// <param name="maxValue">返回的随机数的上界（随机数不能取该上界值）。maxValue 必须大于等于 minValue。</param>
	/// <returns>一个大于等于 minValue 且小于 maxValue 的 64 位带符号整数，即：返回的值范围包括 minValue 但不包括 maxValue。如果 minValue 等于 maxValue，则返回 minValue。</returns>
	public static long NxtInt64(long minValue, long maxValue)
	{
		return RandomShared.NextInt64(minValue, maxValue);
	}

	/// <summary>
	/// 获取UInt64范围内的随机数。
	/// </summary>
	/// <returns>一个大于等于零且小于 UInt64.MaxValue 的 64 位无符号整数。</returns>
	public static ulong NextUInt64()
	{
		byte[] array = new byte[8];
		RandomShared.NextBytes(array);
		return BitConverter.ToUInt64(array, 0);
	}

	/// <summary>
	/// 返回一个介于 0.0 和 1.0 之间的随机数。
	/// </summary>
	/// <returns>大于等于 0.0 并且小于 1.0 的双精度浮点数。</returns>
	public static double NextDouble()
	{
		return RandomShared.NextDouble();
	}

	/// <summary>
	/// 用随机数填充指定字节数组的元素。
	/// </summary>
	/// <param name="buffer">包含随机数的字节数组。</param>
	public static void NextBytes(byte[] buffer)
	{
		RandomShared.NextBytes(buffer);
	}

	/// <summary>
	/// 从1~n中随机选取m个数，m小于n
	/// </summary>
	/// <param name="m">需要选取的数量</param>
	/// <param name="n">范围上限</param>
	/// <returns>包含随机选取的m个数的集合</returns>
	/// <exception cref="T:System.ArgumentOutOfRangeException">当m大于n时抛出此异常</exception>
	public static HashSet<int> RandomSelect(int m, int n)
	{
		if (m == 0)
		{
			return new HashSet<int>();
		}
		if (m > n)
		{
			throw new ArgumentOutOfRangeException("m", "m must less than n");
		}
		HashSet<int> hashSet = RandomSelect(m - 1, n - 1);
		int num = ConcurrentCryptoRandom.Next(0, n);
		hashSet.Add(hashSet.Contains(num) ? (n - 1) : num);
		return hashSet;
	}

	/// <summary>
	/// 根据权重随机选取，如果需求数量超过权重和（除以权重最大公约数后的），那么按照权重比例加入id，剩余数量再进行随机
	/// 不可重复随机num一定小于等于id数量
	/// </summary>
	/// <param name="weightStr">权重字符串，格式为"id1+weight1;id2+weight2;..."</param>
	/// <param name="num">需要选取的数量</param>
	/// <param name="weightIndex">权重索引</param>
	/// <param name="canRepeat">是否允许重复选取</param>
	/// <returns>包含随机选取的结果数组列表</returns>
	/// <exception cref="T:System.ArgumentException">当不可重复选取且需求数量大于id数量时抛出此异常</exception>
	private static List<int[]> RandomSelect(string weightStr, int num, int weightIndex, bool canRepeat = true)
	{
		return RandomSelect(weightStr.SplitTo2IntArray(), num, weightIndex, canRepeat);
	}

	/// <summary>
	/// 根据权重随机选取，如果需求数量超过权重和（除以权重最大公约数后的），那么按照权重比例加入id，剩余数量再进行随机
	/// 不可重复随机num一定小于等于id数量
	/// </summary>
	/// <param name="array">权重数组，每个元素为[id, weight]</param>
	/// <param name="num">需要选取的数量</param>
	/// <param name="weightIndex">权重索引</param>
	/// <param name="canRepeat">是否允许重复选取</param>
	/// <returns>包含随机选取的结果数组列表</returns>
	/// <exception cref="T:System.ArgumentException">当不可重复选取且需求数量大于id数量时抛出此异常</exception>
	private static List<int[]> RandomSelect(int[][] array, int num, int weightIndex, bool canRepeat = true)
	{
		if (canRepeat)
		{
			return CanRepeatRandom(array, num, weightIndex);
		}
		if (num > array.Length)
		{
			throw new ArgumentException($"can't repeat random arg error, num:{num} is great than id count:{array.Length}");
		}
		return NoRepeatRandom(num, weightIndex, array);
	}

	/// <summary>
	/// 不可重复随机选取
	/// </summary>
	/// <param name="num">需要选取的数量</param>
	/// <param name="weightIndex">权重索引</param>
	/// <param name="array">权重数组，每个元素为[id, weight]</param>
	/// <returns>包含随机选取的结果数组列表</returns>
	private static List<int[]> NoRepeatRandom(int num, int weightIndex, int[][] array)
	{
		List<int[]> list = new List<int[]>();
		HashSet<int> hashSet = new HashSet<int>();
		for (int i = 0; i < num; i++)
		{
			int num2 = 0;
			for (int j = 0; j < array.Length; j++)
			{
				if (!hashSet.Contains(j))
				{
					num2 += array[j][weightIndex];
				}
			}
			int num3 = ConcurrentCryptoRandom.Next(num2);
			int num4 = 0;
			int num5 = 0;
			for (int k = 0; k < array.Length; k++)
			{
				if (!hashSet.Contains(k))
				{
					num4 += array[k][weightIndex];
					if (num4 > num3)
					{
						num5 = k;
						break;
					}
				}
			}
			hashSet.Add(num5);
			list.Add(array[num5]);
		}
		return list;
	}

	/// <summary>
	/// 可重复随机选取
	/// </summary>
	/// <param name="array">权重数组，每个元素为[id, weight]</param>
	/// <param name="num">需要选取的数量</param>
	/// <param name="weightIndex">权重索引</param>
	/// <returns>包含随机选取的结果数组列表</returns>
	private static List<int[]> CanRepeatRandom(int[][] array, int num, int weightIndex)
	{
		int num2 = 0;
		foreach (int[] array2 in array)
		{
			num2 += array2[weightIndex];
		}
		List<int[]> list = new List<int[]>(num);
		for (int j = 0; j < num; j++)
		{
			list.Add(SingleRandom(array, num2, weightIndex));
		}
		return list;
	}

	/// <summary>
	/// 根据权重独立随机选取
	/// </summary>
	/// <param name="weightStr">权重字符串，格式为"id1+weight1;id2+weight2;..."</param>
	/// <param name="num">需要选取的数量</param>
	/// <param name="weightIndex">权重索引</param>
	/// <returns>包含随机选取的结果数组列表</returns>
	private static List<int[]> CanRepeatRandom(string weightStr, int num, int weightIndex)
	{
		return CanRepeatRandom(weightStr.SplitTo2IntArray(), num, weightIndex);
	}

	/// <summary>
	/// 单次随机选取
	/// </summary>
	/// <param name="array">权重数组，每个元素为[id, weight]</param>
	/// <param name="totalWeight">总权重</param>
	/// <param name="weightIndex">权重索引</param>
	/// <returns>随机选取的结果数组</returns>
	private static int[] SingleRandom(int[][] array, int totalWeight, int weightIndex)
	{
		int num = ConcurrentCryptoRandom.Next(totalWeight);
		int num2 = 0;
		foreach (int[] array2 in array)
		{
			num2 += array2[weightIndex];
			if (num2 > num)
			{
				return array2;
			}
		}
		return array[0];
	}

	/// <summary>
	/// 根据权重随机选取一个id
	/// </summary>
	/// <param name="weights">权重数组</param>
	/// <returns>随机选取的id索引</returns>
	public static int Idx(int[] weights)
	{
		int num = ConcurrentCryptoRandom.Next(weights.Sum());
		int num2 = 0;
		for (int i = 0; i < weights.Length; i++)
		{
			num2 += weights[i];
			if (num2 > num)
			{
				return i;
			}
		}
		return 0;
	}

	/// <summary>
	/// 根据权重随机选取一个id
	/// </summary>
	/// <param name="array">数据列表，每个元素为[id, weight]</param>
	/// <param name="weightIndex">权重索引</param>
	/// <returns>随机选取的id索引</returns>
	public static int Idx(int[][] array, int weightIndex = 1)
	{
		int num = 0;
		foreach (int[] array2 in array)
		{
			num += array2[weightIndex];
		}
		int num2 = ConcurrentCryptoRandom.Next(num);
		int num3 = 0;
		for (int j = 0; j < array.Length; j++)
		{
			int[] array3 = array[j];
			num3 += array3[weightIndex];
			if (num3 > num2)
			{
				return j;
			}
		}
		return 0;
	}

	/// <summary>
	/// 随机获取指定数量的id
	/// </summary>
	/// <param name="array">数据列表，每个元素为[id, weight]</param>
	/// <param name="num">需要选取的数量</param>
	/// <param name="isCanRepeat">是否允许重复选取</param>
	/// <returns>包含随机选取的id列表</returns>
	public static List<int> Ids(int[][] array, int num, bool isCanRepeat = true)
	{
		return (from t in RandomSelect(array, num, 1, isCanRepeat)
			select t[0]).ToList();
	}

	/// <summary>
	/// 随机获取指定数量的id
	/// </summary>
	/// <param name="str">权重字符串，格式为"id1+weight1;id2+weight2;..."</param>
	/// <param name="num">需要选取的数量</param>
	/// <param name="isCanRepeat">是否允许重复选取</param>
	/// <returns>包含随机选取的id列表</returns>
	public static List<int> Ids(string str, int num, bool isCanRepeat = true)
	{
		return (from t in RandomSelect(str, num, 1, isCanRepeat)
			select t[0]).ToList();
	}

	/// <summary>
	/// 随机获取指定数量的项目
	/// </summary>
	/// <param name="str">权重字符串，格式为"id1+weight1;id2+weight2;..."</param>
	/// <param name="num">需要选取的数量</param>
	/// <param name="isCanRepeat">是否允许重复选取</param>
	/// <returns>包含随机选取的项目列表</returns>
	public static List<int[]> Items(string str, int num, bool isCanRepeat = true)
	{
		return RandomSelect(str, num, 2, isCanRepeat);
	}

	/// <summary>
	/// 随机获取指定数量的项目
	/// </summary>
	/// <param name="array">数据列表，每个元素为[id, weight]</param>
	/// <param name="num">需要选取的数量</param>
	/// <param name="isCanRepeat">是否允许重复选取</param>
	/// <returns>包含随机选取的项目列表</returns>
	public static List<int[]> Items(int[][] array, int num, bool isCanRepeat = true)
	{
		return RandomSelect(array, num, 2, isCanRepeat);
	}

	/// <summary>
	/// 求多个数的最大公约数
	/// </summary>
	/// <param name="input">输入的整数数组</param>
	/// <returns>最大公约数</returns>
	public static int Gcd(params int[] input)
	{
		if (input.Length == 0)
		{
			return 1;
		}
		if (input.Length == 1)
		{
			return input[0];
		}
		int num = input[0];
		for (int i = 1; i < input.Length; i++)
		{
			num = Gcd(num, input[i]);
		}
		return num;
	}

	/// <summary>
	/// 求两个数的最大公约数
	/// </summary>
	/// <param name="a">第一个整数</param>
	/// <param name="b">第二个整数</param>
	/// <returns>最大公约数</returns>
	public static int Gcd(int a, int b)
	{
		if (a < b)
		{
			int num = a;
			a = b;
			b = num;
		}
		if (b == 0)
		{
			return a;
		}
		return Gcd(b, a % b);
	}
}
