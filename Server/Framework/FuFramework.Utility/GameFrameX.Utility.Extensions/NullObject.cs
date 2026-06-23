using System;

namespace FuFramework.Utility.Extensions;

/// <summary>
/// 表示可为空的对象。
/// </summary>
/// <typeparam name="T">对象的类型。</typeparam>
public readonly record struct NullObject<T>(T item) : IComparable, IComparable<T>
{
	public static NullObject<T> Null => default(NullObject<T>);

	/// <summary>
	/// 比较当前对象与另一个对象。
	/// </summary>
	/// <param name="value">要比较的对象。</param>
	/// <returns>一个整数，指示当前对象与 <paramref name="value" /> 的相对顺序。</returns>
	public int CompareTo(object value)
	{
		if (value is NullObject<T> nullObject)
		{
			if (nullObject.item is IComparable obj)
			{
				return ((IComparable)(object)item).CompareTo(obj);
			}
			return item.ToString().CompareTo(nullObject.item.ToString());
		}
		return 1;
	}

	/// <summary>
	/// 比较当前对象与同一类型的另一个对象。
	/// </summary>
	/// <param name="other">要比较的对象。</param>
	/// <returns>一个整数，指示当前对象与 <paramref name="other" /> 的相对顺序。</returns>
	public int CompareTo(T other)
	{
		if (other is IComparable obj)
		{
			return ((IComparable)(object)item).CompareTo(obj);
		}
		return item.ToString().CompareTo(other.ToString());
	}

	/// <summary>
	/// 将 <see cref="T:FuFramework.Utility.Extensions.NullObject`1" /> 隐式转换为类型 <typeparamref name="T" />。
	/// </summary>
	/// <param name="nullObject">要转换的 <see cref="T:FuFramework.Utility.Extensions.NullObject`1" /> 实例。</param>
	public static implicit operator T(NullObject<T> nullObject)
	{
		return nullObject.item;
	}

	/// <summary>
	/// 将类型 <typeparamref name="T" /> 隐式转换为 <see cref="T:FuFramework.Utility.Extensions.NullObject`1" />。
	/// </summary>
	/// <param name="item">要转换的值。</param>
	public static implicit operator NullObject<T>(T item)
	{
		return new NullObject<T>(item);
	}

	/// <summary>
	/// 返回对象的字符串表示形式。
	/// </summary>
	/// <returns>对象的字符串表示形式，如果对象为 null，则返回 "NULL"。</returns>
	public override string ToString()
	{
		if (item == null)
		{
			return "NULL";
		}
		return item.ToString();
	}
}
