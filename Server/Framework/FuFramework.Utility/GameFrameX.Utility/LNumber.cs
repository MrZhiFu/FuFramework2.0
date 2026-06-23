using System;
using FuFramework.Foundation.Logger;

namespace FuFramework.Utility;

/// <summary>
/// </summary>
public struct LNumber : IComparable<LNumber>, IEquatable<LNumber>
{
	/// <summary>
	/// </summary>
	public const int FRACTION_BITS = 14;

	private const int INTEGER_BITS = 50;

	private const int FRACTION_MASK = 16383;

	private const int INTEGER_MASK = -16384;

	private const int FRACTION_RANGE = 16384;

	/// <summary>
	/// </summary>
	public const long Max = 562949953421311L;

	/// <summary>
	/// </summary>
	public const long FMax = 9999L;

	/// <summary>
	/// </summary>
	public static readonly LNumber MaxValue = Create_Row(562949953421311L);

	/// <summary>
	/// 最小值
	/// </summary>
	public static readonly LNumber MinValue = Create_Row(-562949953421311L);

	/// <summary>
	/// 0
	/// </summary>
	public static readonly LNumber zero = Create_Row(0L);

	/// <summary>
	/// 1
	/// </summary>
	public static readonly LNumber one = 1;

	/// <summary>
	/// -1
	/// </summary>
	public static readonly LNumber minus_one = -one;

	/// <summary>
	/// 1
	/// </summary>
	public static readonly LNumber epsilon = Create_Row(1L);

	/// <summary>
	/// 0
	/// </summary>
	public static readonly LNumber Zero = default(LNumber);

	private const int Muti_FACTOR = 16384;

	/// <summary>
	/// </summary>
	public long Raw;

	/// <summary>
	/// 天花板数
	/// </summary>
	public long Ceiling
	{
		get
		{
			LNumber lNumber = default(LNumber);
			lNumber.Raw = (Raw + 16383) & -16384;
			return (long)lNumber;
		}
	}

	/// <summary>
	/// 地板数
	/// </summary>
	public long Floor
	{
		get
		{
			LNumber lNumber = default(LNumber);
			lNumber.Raw = Raw & -16384;
			return (long)lNumber;
		}
	}

	/// <summary>
	/// </summary>
	/// <param name="i"></param>
	/// <param name="f"></param>
	/// <returns></returns>
	public static LNumber Create(long i, long f)
	{
		int num = (((i ^ f) >= 0) ? 1 : (-1));
		if (i < 0)
		{
			i = -i;
		}
		if (f < 0)
		{
			f = -f;
		}
		i <<= 14;
		f = (f << 14) / 10000;
		LNumber result = default(LNumber);
		result.Raw = num * (i + f);
		return result;
	}

	/// <summary>
	/// </summary>
	/// <param name="i"></param>
	/// <returns></returns>
	public static LNumber Create_Row(long i)
	{
		LNumber result = default(LNumber);
		result.Raw = i;
		return result;
	}

	/// <summary>Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.</summary>
	/// <param name="other">An object to compare with this instance.</param>
	/// <returns>
	/// A value that indicates the relative order of the objects being compared. The return value has these meanings:
	/// <list type="table">
	///     <listheader>
	///         <term> Value</term><description> Meaning</description>
	///     </listheader>
	///     <item>
	///         <term> Less than zero</term><description> This instance precedes <paramref name="other" /> in the sort order.</description>
	///     </item>
	///     <item>
	///         <term> Zero</term><description> This instance occurs in the same position in the sort order as <paramref name="other" />.</description>
	///     </item>
	///     <item>
	///         <term> Greater than zero</term><description> This instance follows <paramref name="other" /> in the sort order.</description>
	///     </item>
	/// </list>
	/// </returns>
	public int CompareTo(LNumber other)
	{
		return CompareTo(other.Raw);
	}

	private int CompareTo(long other)
	{
		return Raw.CompareTo(other);
	}

	/// <summary>
	/// 判断是否相等
	/// </summary>
	/// <param name="other"></param>
	/// <returns></returns>
	public bool Equals(LNumber other)
	{
		return Raw == other.Raw;
	}

	/// <summary>Indicates whether this instance and a specified object are equal.</summary>
	/// <param name="obj">The object to compare with the current instance.</param>
	/// <returns>
	/// <see langword="true" /> if <paramref name="obj" /> and this instance are the same type and represent the same value; otherwise, <see langword="false" />.
	/// </returns>
	public override bool Equals(object obj)
	{
		if (obj is LNumber)
		{
			return (LNumber)obj == this;
		}
		return false;
	}

	/// <summary>
	/// 获取哈希
	/// </summary>
	/// <returns></returns>
	public override int GetHashCode()
	{
		return Raw.GetHashCode();
	}

	/// <summary>
	/// 转为字符串
	/// </summary>
	/// <returns></returns>
	public override string ToString()
	{
		return ((double)this).ToString("f4");
	}

	/// <summary>
	/// 格式化
	/// </summary>
	/// <param name="str"></param>
	/// <returns></returns>
	public string ToString(string str)
	{
		return ((double)this).ToString(str);
	}

	/// <summary>
	/// 二元操作符 +
	/// </summary>
	/// <param name="lhs"></param>
	/// <param name="rhs"></param>
	/// <returns></returns>
	public static LNumber operator +(LNumber lhs, LNumber rhs)
	{
		LNumber result = default(LNumber);
		result.Raw = lhs.Raw + rhs.Raw;
		return result;
	}

	/// <summary>
	/// 二元操作符 -
	/// </summary>
	/// <param name="lhs"></param>
	/// <param name="rhs"></param>
	/// <returns></returns>
	public static LNumber operator -(LNumber lhs, LNumber rhs)
	{
		LNumber result = default(LNumber);
		result.Raw = lhs.Raw - rhs.Raw;
		return result;
	}

	/// <summary>
	/// 二元操作符 *
	/// </summary>
	/// <param name="lhs"></param>
	/// <param name="rhs"></param>
	/// <returns></returns>
	public static LNumber operator *(LNumber lhs, LNumber rhs)
	{
		LNumber result = default(LNumber);
		if (lhs.Raw > int.MaxValue || rhs.Raw > int.MaxValue || lhs.Raw < int.MinValue || rhs.Raw < int.MinValue)
		{
			BigInteger bigInteger = lhs.Raw;
			BigInteger bigInteger2 = rhs.Raw;
			BigInteger bigInteger3 = bigInteger * bigInteger2 + 8192 >> 14;
			if (bigInteger3 > long.MinValue && bigInteger3 < long.MaxValue)
			{
				result.Raw = long.Parse(bigInteger3.ToString());
			}
			else if ((lhs > 0 && rhs > 0) || (lhs < 0 && rhs < 0))
			{
				LogHelper.Error("LNumber*已越界>" + bigInteger3);
				result.Raw = long.MaxValue;
			}
			else
			{
				LogHelper.Error("LNumber*已越界>" + bigInteger3);
				result.Raw = long.MinValue;
			}
		}
		else
		{
			result.Raw = lhs.Raw * rhs.Raw + 8192 >> 14;
		}
		return result;
	}

	/// <summary>
	/// 二元操作符 /
	/// </summary>
	/// <param name="lhs"></param>
	/// <param name="rhs"></param>
	/// <returns></returns>
	public static LNumber operator /(LNumber lhs, LNumber rhs)
	{
		if (lhs.Raw == 0L)
		{
			return 0;
		}
		int num = 1;
		if (rhs.Raw < 0)
		{
			num = -1;
		}
		if (rhs.Raw + num >> 1 == 0L)
		{
			return 0;
		}
		LNumber result = default(LNumber);
		if (lhs.Raw > 281474976710656L)
		{
			BigInteger bigInteger = lhs.Raw;
			BigInteger bigInteger2 = rhs.Raw;
			BigInteger bigInteger3 = (bigInteger << 15) / bigInteger2 + num >> 1;
			if (bigInteger3 > long.MinValue && bigInteger3 < long.MaxValue)
			{
				result.Raw = long.Parse(bigInteger3.ToString());
			}
			else if ((lhs > 0 && rhs > 0) || (lhs < 0 && rhs < 0))
			{
				LogHelper.Error("LNumber/已越界>" + bigInteger3);
				result.Raw = long.MaxValue;
			}
			else
			{
				LogHelper.Error("LNumber/已越界>" + bigInteger3);
				result.Raw = long.MinValue;
			}
		}
		else
		{
			result.Raw = (lhs.Raw << 15) / rhs.Raw + num >> 1;
		}
		return result;
	}

	/// <summary>
	/// 一元操作符 - (负数操作)
	/// </summary>
	/// <param name="x"></param>
	/// <returns></returns>
	public static LNumber operator -(LNumber x)
	{
		LNumber result = default(LNumber);
		result.Raw = -x.Raw;
		return result;
	}

	/// <summary>
	/// 二元操作符 %
	/// </summary>
	/// <param name="lhs"></param>
	/// <param name="rhs"></param>
	/// <returns></returns>
	public static LNumber operator %(LNumber lhs, LNumber rhs)
	{
		LNumber result = default(LNumber);
		result.Raw = lhs.Raw % rhs.Raw;
		return result;
	}

	/// <summary>
	/// 比较运算符 小于
	/// </summary>
	/// <param name="lhs"></param>
	/// <param name="rhs"></param>
	/// <returns></returns>
	public static bool operator <(LNumber lhs, LNumber rhs)
	{
		return lhs.Raw < rhs.Raw;
	}

	/// <summary>
	/// 比较运算符 小于等于
	/// </summary>
	/// <param name="lhs"></param>
	/// <param name="rhs"></param>
	/// <returns></returns>
	public static bool operator <=(LNumber lhs, LNumber rhs)
	{
		return lhs.Raw <= rhs.Raw;
	}

	/// <summary>
	/// 比较运算符 &gt;
	/// </summary>
	/// <param name="lhs"></param>
	/// <param name="rhs"></param>
	/// <returns></returns>
	public static bool operator >(LNumber lhs, LNumber rhs)
	{
		return lhs.Raw > rhs.Raw;
	}

	/// <summary>
	/// 比较运算符 &gt;=
	/// </summary>
	/// <param name="lhs"></param>
	/// <param name="rhs"></param>
	/// <returns></returns>
	public static bool operator >=(LNumber lhs, LNumber rhs)
	{
		return lhs.Raw >= rhs.Raw;
	}

	/// <summary>
	/// 比较运算符 ==
	/// </summary>
	/// <param name="lhs"></param>
	/// <param name="rhs"></param>
	/// <returns></returns>
	public static bool operator ==(LNumber lhs, LNumber rhs)
	{
		return lhs.Raw == rhs.Raw;
	}

	/// <summary>
	/// 比较运算符 !=
	/// </summary>
	/// <param name="lhs"></param>
	/// <param name="rhs"></param>
	/// <returns></returns>
	public static bool operator !=(LNumber lhs, LNumber rhs)
	{
		return lhs.Raw != rhs.Raw;
	}

	/// <summary>
	/// long类型转换
	/// </summary>
	/// <param name="number"></param>
	/// <returns></returns>
	public static explicit operator long(LNumber number)
	{
		if (number.Raw > 0)
		{
			return number.Raw >> 14;
		}
		return number.Raw + 16383 >> 14;
	}

	/// <summary>
	/// double类型转换
	/// </summary>
	/// <param name="number"></param>
	/// <returns></returns>
	public static explicit operator double(LNumber number)
	{
		return (double)(number.Raw >> 14) + (double)(number.Raw & 0x3FFF) / 16384.0;
	}

	/// <summary>
	/// float 类型转换
	/// </summary>
	/// <param name="number"></param>
	/// <returns></returns>
	public static implicit operator float(LNumber number)
	{
		return (float)(double)number;
	}

	/// <summary>
	/// 赋值运算
	/// </summary>
	/// <param name="value"></param>
	public static implicit operator LNumber(long value)
	{
		return Create(value, 0L);
	}

	/// <summary>
	/// 赋值运算
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	public static implicit operator LNumber(int value)
	{
		return Create(value, 0L);
	}
}
