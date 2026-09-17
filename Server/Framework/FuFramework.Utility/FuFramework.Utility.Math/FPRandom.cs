using System;

namespace FuFramework.Utility.Math;

/// <summary>
/// 生成基于确定性方法的随机数。
/// </summary>
public sealed class FPRandom
{
	private const int N = 624;

	private const int M = 397;

	private const uint MATRIX_A = 2567483615u;

	private const uint UPPER_MASK = 2147483648u;

	private const uint LOWER_MASK = 2147483647u;

	private const int MAX_RAND_INT = int.MaxValue;

	/// <summary>
	/// 使用种子 1 的 {@link TSRandom} 的静态实例。
	/// </summary>
	public static FPRandom instance;

	private readonly uint[] mag01 = new uint[2] { 0u, 2567483615u };

	private readonly uint[] mt = new uint[624];

	private int mti = 625;

	/// <summary>
	/// 获取最大随机整数值。
	/// </summary>
	public static int MaxRandomInt => int.MaxValue;

	/// <summary>
	/// 返回一个 {@link FP} 值，范围在 0.0 [包含] 到 1.0 [包含] 之间。
	/// </summary>
	public static FP value => instance.NextFP();

	/// <summary>
	/// 返回一个随机的 {@link TSVector}，表示半径为 1 的球体内的一个点。
	/// </summary>
	public static FPVector3 insideUnitSphere => new FPVector3(value, value, value);

	/// <summary>
	/// 初始化一个新的实例，使用当前时间的毫秒数作为种子。
	/// </summary>
	private FPRandom()
	{
		init_genrand((uint)DateTime.Now.Millisecond);
	}

	/// <summary>
	/// 使用指定的种子初始化一个新的实例。
	/// </summary>
	/// <param name="seed">用于初始化的种子。</param>
	private FPRandom(int seed)
	{
		init_genrand((uint)seed);
	}

	/// <summary>
	/// 使用指定的初始化数组初始化一个新的实例。
	/// </summary>
	/// <param name="init">初始化数组。</param>
	private FPRandom(int[] init)
	{
		uint[] array = new uint[init.Length];
		for (int i = 0; i < init.Length; i++)
		{
			array[i] = (uint)init[i];
		}
		init_by_array(array, (uint)array.Length);
	}

	internal static void Init()
	{
		instance = New(1);
	}

	/// <summary>
	/// 根据给定的种子生成一个新的实例。
	/// </summary>
	/// <param name="seed">用于生成新实例的种子。</param>
	/// <returns>新的 FPRandom 实例。</returns>
	public static FPRandom New(int seed)
	{
		return new FPRandom(seed);
	}

	/// <summary>
	/// 返回一个随机整数。
	/// </summary>
	/// <returns>生成的随机整数。</returns>
	public int Next()
	{
		return genrand_int31();
	}

	/// <summary>
	/// 返回一个随机整数。
	/// </summary>
	/// <returns>生成的随机整数。</returns>
	public static int CallNext()
	{
		return instance.Next();
	}

	/// <summary>
	/// 返回一个介于 minValue [包含] 和 maxValue [不包含] 之间的整数。
	/// </summary>
	/// <param name="minValue">最小值。</param>
	/// <param name="maxValue">最大值。</param>
	/// <returns>生成的随机整数。</returns>
	public int Next(int minValue, int maxValue)
	{
		if (minValue > maxValue)
		{
			int num = minValue;
			minValue = maxValue;
			maxValue = num;
		}
		int num2 = maxValue - minValue;
		return minValue + Next() % num2;
	}

	/// <summary>
	/// 返回一个 {@link FP} 值，范围在 minValue [包含] 到 maxValue [包含] 之间。
	/// </summary>
	/// <param name="minValue">最小值。</param>
	/// <param name="maxValue">最大值。</param>
	/// <returns>生成的随机 FP 值。</returns>
	public FP Next(float minValue, float maxValue)
	{
		int num = (int)(minValue * 1000f);
		int num2 = (int)(maxValue * 1000f);
		if (num > num2)
		{
			int num3 = num;
			num = num2;
			num2 = num3;
		}
		return FP.Floor((num2 - num + 1) * NextFP() + num) / 1000;
	}

	/// <summary>
	/// 返回一个介于 minValue [包含] 和 maxValue [不包含] 之间的整数。
	/// </summary>
	/// <param name="minValue">最小值。</param>
	/// <param name="maxValue">最大值。</param>
	/// <returns>生成的随机整数。</returns>
	public static int Range(int minValue, int maxValue)
	{
		return instance.Next(minValue, maxValue);
	}

	/// <summary>
	/// 返回一个 {@link FP} 值，范围在 minValue [包含] 到 maxValue [包含] 之间。
	/// </summary>
	/// <param name="minValue">最小值。</param>
	/// <param name="maxValue">最大值。</param>
	/// <returns>生成的随机 FP 值。</returns>
	public static FP Range(float minValue, float maxValue)
	{
		return instance.Next(minValue, maxValue);
	}

	/// <summary>
	/// 返回一个 {@link FP} 值，范围在 0.0 [包含] 到 1.0 [包含] 之间。
	/// </summary>
	/// <returns>生成的随机 FP 值。</returns>
	public FP NextFP()
	{
		return (FP)Next() / (FP)MaxRandomInt;
	}

	private float NextFloat()
	{
		return (float)genrand_real2();
	}

	private float NextFloat(bool includeOne)
	{
		if (includeOne)
		{
			return (float)genrand_real1();
		}
		return (float)genrand_real2();
	}

	private float NextFloatPositive()
	{
		return (float)genrand_real3();
	}

	private double NextDouble()
	{
		return genrand_real2();
	}

	private double NextDouble(bool includeOne)
	{
		if (includeOne)
		{
			return genrand_real1();
		}
		return genrand_real2();
	}

	private double NextDoublePositive()
	{
		return genrand_real3();
	}

	private double Next53BitRes()
	{
		return genrand_res53();
	}

	/// <summary>
	/// 使用当前时间的毫秒数初始化随机数生成器。
	/// </summary>
	public void Initialize()
	{
		init_genrand((uint)DateTime.Now.Millisecond);
	}

	/// <summary>
	/// 使用指定的种子初始化随机数生成器。
	/// </summary>
	/// <param name="seed">用于初始化的种子。</param>
	public void Initialize(int seed)
	{
		init_genrand((uint)seed);
	}

	/// <summary>
	/// 使用指定的初始化数组初始化随机数生成器。
	/// </summary>
	/// <param name="init">初始化数组。</param>
	public void Initialize(int[] init)
	{
		uint[] array = new uint[init.Length];
		for (int i = 0; i < init.Length; i++)
		{
			array[i] = (uint)init[i];
		}
		init_by_array(array, (uint)array.Length);
	}

	private void init_genrand(uint s)
	{
		mt[0] = s & 0xFFFFFFFFu;
		for (mti = 1; mti < 624; mti++)
		{
			mt[mti] = (uint)(1812433253 * (mt[mti - 1] ^ (mt[mti - 1] >> 30)) + mti);
			mt[mti] &= uint.MaxValue;
		}
	}

	private void init_by_array(uint[] init_key, uint key_length)
	{
		init_genrand(19650218u);
		int num = 1;
		int num2 = 0;
		for (int num3 = (int)((624 > key_length) ? 624 : key_length); num3 > 0; num3--)
		{
			mt[num] = (uint)((mt[num] ^ ((mt[num - 1] ^ (mt[num - 1] >> 30)) * 1664525)) + init_key[num2] + num2);
			mt[num] &= uint.MaxValue;
			num++;
			num2++;
			if (num >= 624)
			{
				mt[0] = mt[623];
				num = 1;
			}
			if (num2 >= key_length)
			{
				num2 = 0;
			}
		}
		for (int num3 = 623; num3 > 0; num3--)
		{
			mt[num] = (uint)((mt[num] ^ ((mt[num - 1] ^ (mt[num - 1] >> 30)) * 1566083941)) - num);
			mt[num] &= uint.MaxValue;
			num++;
			if (num >= 624)
			{
				mt[0] = mt[623];
				num = 1;
			}
		}
		mt[0] = 2147483648u;
	}

	private uint genrand_int32()
	{
		uint num;
		if (mti >= 624)
		{
			if (mti == 625)
			{
				init_genrand(5489u);
			}
			int i;
			for (i = 0; i < 227; i++)
			{
				num = (mt[i] & 0x80000000u) | (mt[i + 1] & 0x7FFFFFFF);
				mt[i] = mt[i + 397] ^ (num >> 1) ^ mag01[num & 1];
			}
			for (; i < 623; i++)
			{
				num = (mt[i] & 0x80000000u) | (mt[i + 1] & 0x7FFFFFFF);
				mt[i] = mt[i + -227] ^ (num >> 1) ^ mag01[num & 1];
			}
			num = (mt[623] & 0x80000000u) | (mt[0] & 0x7FFFFFFF);
			mt[623] = mt[396] ^ (num >> 1) ^ mag01[num & 1];
			mti = 0;
		}
		num = mt[mti++];
		num ^= num >> 11;
		num ^= (num << 7) & 0x9D2C5680u;
		num ^= (num << 15) & 0xEFC60000u;
		return num ^ (num >> 18);
	}

	private int genrand_int31()
	{
		return (int)(genrand_int32() >> 1);
	}

	private FP genrand_FP()
	{
		return genrand_int32() * (FP.One / 4294967295L);
	}

	private double genrand_real1()
	{
		return (double)genrand_int32() * 2.3283064370807974E-10;
	}

	private double genrand_real2()
	{
		return (double)genrand_int32() * 2.3283064365386963E-10;
	}

	private double genrand_real3()
	{
		return ((double)genrand_int32() + 0.5) * 2.3283064365386963E-10;
	}

	private double genrand_res53()
	{
		uint num = genrand_int32() >> 5;
		uint num2 = genrand_int32() >> 6;
		return ((double)num * 67108864.0 + (double)num2) * 1.1102230246251565E-16;
	}
}
