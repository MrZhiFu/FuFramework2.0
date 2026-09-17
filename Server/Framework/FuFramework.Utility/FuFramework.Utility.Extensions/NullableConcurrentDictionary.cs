using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace FuFramework.Utility.Extensions;

/// <summary>
/// 支持null键和值的并发字典类型
/// </summary>
/// <typeparam name="TKey">键的类型</typeparam>
/// <typeparam name="TValue">值的类型</typeparam>
public class NullableConcurrentDictionary<TKey, TValue> : ConcurrentDictionary<NullObject<TKey>, TValue>
{
	/// <summary>
	/// 获取或设置当键不存在时返回的默认值。
	/// </summary>
	internal TValue FallbackValue { get; set; }

	/// <summary>
	/// 获取或设置指定键的值。
	/// </summary>
	/// <param name="key">键。</param>
	public new TValue this[NullObject<TKey> key]
	{
		get
		{
			if (!base.TryGetValue(key, out TValue value))
			{
				return FallbackValue;
			}
			return value;
		}
		set
		{
			base[key] = value;
		}
	}

	/// <summary>
	/// 根据条件获取或设置第一个匹配的键值对的值。
	/// </summary>
	/// <param name="condition">用于筛选键值对的条件。</param>
	public TValue this[Func<KeyValuePair<TKey, TValue>, bool> condition]
	{
		get
		{
			using (IEnumerator<KeyValuePair<NullObject<TKey>, TValue>> enumerator = this.Where((KeyValuePair<NullObject<TKey>, TValue> pair) => condition(new KeyValuePair<TKey, TValue>(pair.Key.item, pair.Value))).GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					return enumerator.Current.Value;
				}
			}
			return FallbackValue;
		}
		set
		{
			foreach (KeyValuePair<NullObject<TKey>, TValue> item in this.Where((KeyValuePair<NullObject<TKey>, TValue> pair) => condition(new KeyValuePair<TKey, TValue>(pair.Key.item, pair.Value))))
			{
				this[item.Key] = value;
			}
		}
	}

	/// <summary>
	/// 根据条件获取或设置第一个匹配的键值对的值。
	/// </summary>
	/// <param name="condition">用于筛选键值对的条件。</param>
	public TValue this[Func<TKey, TValue, bool> condition]
	{
		get
		{
			using (IEnumerator<KeyValuePair<NullObject<TKey>, TValue>> enumerator = this.Where((KeyValuePair<NullObject<TKey>, TValue> pair) => condition(pair.Key.item, pair.Value)).GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					return enumerator.Current.Value;
				}
			}
			return FallbackValue;
		}
		set
		{
			foreach (KeyValuePair<NullObject<TKey>, TValue> item in this.Where((KeyValuePair<NullObject<TKey>, TValue> pair) => condition(pair.Key.item, pair.Value)))
			{
				this[item.Key] = value;
			}
		}
	}

	/// <summary>
	/// 根据条件获取或设置第一个匹配的键值对的值。
	/// </summary>
	/// <param name="condition">用于筛选键的条件。</param>
	public TValue this[Func<TKey, bool> condition]
	{
		get
		{
			using (IEnumerator<KeyValuePair<NullObject<TKey>, TValue>> enumerator = this.Where((KeyValuePair<NullObject<TKey>, TValue> pair) => condition(pair.Key.item)).GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					return enumerator.Current.Value;
				}
			}
			return FallbackValue;
		}
		set
		{
			foreach (KeyValuePair<NullObject<TKey>, TValue> item in this.Where((KeyValuePair<NullObject<TKey>, TValue> pair) => condition(pair.Key.item)))
			{
				this[item.Key] = value;
			}
		}
	}

	/// <summary>
	/// 根据条件获取或设置第一个匹配的键值对的值。
	/// </summary>
	/// <param name="condition">用于筛选值的条件。</param>
	public TValue this[Func<TValue, bool> condition]
	{
		get
		{
			using (IEnumerator<KeyValuePair<NullObject<TKey>, TValue>> enumerator = this.Where((KeyValuePair<NullObject<TKey>, TValue> pair) => condition(pair.Value)).GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					return enumerator.Current.Value;
				}
			}
			return FallbackValue;
		}
		set
		{
			foreach (KeyValuePair<NullObject<TKey>, TValue> item in this.Where((KeyValuePair<NullObject<TKey>, TValue> pair) => condition(pair.Value)))
			{
				this[item.Key] = value;
			}
		}
	}

	/// <summary>
	/// 获取或设置指定键的值。
	/// </summary>
	/// <param name="key">键。</param>
	public TValue this[TKey key]
	{
		get
		{
			if (!base.TryGetValue(new NullObject<TKey>(key), out TValue value))
			{
				return FallbackValue;
			}
			return value;
		}
		set
		{
			base[new NullObject<TKey>(key)] = value;
		}
	}

	/// <summary>
	/// 初始化一个新的 <see cref="T:FuFramework.Utility.Extensions.NullableConcurrentDictionary`2" /> 实例。
	/// </summary>
	public NullableConcurrentDictionary()
	{
	}

	/// <summary>
	/// 使用指定的默认值初始化一个新的 <see cref="T:FuFramework.Utility.Extensions.NullableConcurrentDictionary`2" /> 实例。
	/// </summary>
	/// <param name="fallbackValue">当键不存在时返回的默认值。</param>
	public NullableConcurrentDictionary(TValue fallbackValue)
	{
		FallbackValue = fallbackValue;
	}

	/// <summary>
	/// 使用指定的并发级别和初始容量初始化一个新的 <see cref="T:FuFramework.Utility.Extensions.NullableConcurrentDictionary`2" /> 实例。
	/// </summary>
	/// <param name="concurrencyLevel">并发级别。</param>
	/// <param name="capacity">初始容量。</param>
	public NullableConcurrentDictionary(int concurrencyLevel, int capacity)
		: base(concurrencyLevel, capacity)
	{
	}

	/// <summary>
	/// 使用指定的比较器初始化一个新的 <see cref="T:FuFramework.Utility.Extensions.NullableConcurrentDictionary`2" /> 实例。
	/// </summary>
	/// <param name="comparer">用于比较键的比较器。</param>
	public NullableConcurrentDictionary(IEqualityComparer<NullObject<TKey>> comparer)
		: base(comparer)
	{
	}

	/// <summary>
	/// 判断字典中是否包含指定的键。
	/// </summary>
	/// <param name="key">键。</param>
	/// <returns>如果包含指定的键，则返回 true；否则返回 false。</returns>
	public bool ContainsKey(TKey key)
	{
		return base.ContainsKey(new NullObject<TKey>(key));
	}

	/// <summary>
	/// 尝试添加一个键值对。
	/// </summary>
	/// <param name="key">键。</param>
	/// <param name="value">值。</param>
	/// <returns>如果成功添加，则返回 true；否则返回 false。</returns>
	public bool TryAdd(TKey key, TValue value)
	{
		return base.TryAdd(new NullObject<TKey>(key), value);
	}

	/// <summary>
	/// 尝试移除一个键值对。
	/// </summary>
	/// <param name="key">键。</param>
	/// <param name="value">移除的值。</param>
	/// <returns>如果成功移除，则返回 true；否则返回 false。</returns>
	public bool TryRemove(TKey key, out TValue value)
	{
		return base.TryRemove(new NullObject<TKey>(key), out value);
	}

	/// <summary>
	/// 尝试更新一个键值对。
	/// </summary>
	/// <param name="key">键。</param>
	/// <param name="value">新的值。</param>
	/// <param name="comparisionValue">比较值。</param>
	/// <returns>如果成功更新，则返回 true；否则返回 false。</returns>
	public bool TryUpdate(TKey key, TValue value, TValue comparisionValue)
	{
		return base.TryUpdate(new NullObject<TKey>(key), value, comparisionValue);
	}

	/// <summary>
	/// 尝试获取指定键的值。
	/// </summary>
	/// <param name="key">键。</param>
	/// <param name="value">获取的值。</param>
	/// <returns>如果成功获取，则返回 true；否则返回 false。</returns>
	public bool TryGetValue(TKey key, out TValue value)
	{
		return base.TryGetValue(new NullObject<TKey>(key), out value);
	}

	/// <summary>
	/// 从 <see cref="T:System.Collections.Generic.Dictionary`2" /> 隐式转换为 <see cref="T:FuFramework.Utility.Extensions.NullableConcurrentDictionary`2" />。
	/// </summary>
	/// <param name="dic">要转换的字典。</param>
	public static implicit operator NullableConcurrentDictionary<TKey, TValue>(Dictionary<TKey, TValue> dic)
	{
		NullableConcurrentDictionary<TKey, TValue> nullableConcurrentDictionary = new NullableConcurrentDictionary<TKey, TValue>();
		foreach (KeyValuePair<TKey, TValue> item in dic)
		{
			nullableConcurrentDictionary[item.Key] = item.Value;
		}
		return nullableConcurrentDictionary;
	}

	/// <summary>
	/// 从 <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> 隐式转换为 <see cref="T:FuFramework.Utility.Extensions.NullableConcurrentDictionary`2" />。
	/// </summary>
	/// <param name="dic">要转换的并发字典。</param>
	public static implicit operator NullableConcurrentDictionary<TKey, TValue>(ConcurrentDictionary<TKey, TValue> dic)
	{
		NullableConcurrentDictionary<TKey, TValue> nullableConcurrentDictionary = new NullableConcurrentDictionary<TKey, TValue>();
		foreach (KeyValuePair<TKey, TValue> item in dic)
		{
			nullableConcurrentDictionary[item.Key] = item.Value;
		}
		return nullableConcurrentDictionary;
	}

	/// <summary>
	/// 从 <see cref="T:FuFramework.Utility.Extensions.NullableConcurrentDictionary`2" /> 隐式转换为 <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" />。
	/// </summary>
	/// <param name="dic">要转换的字典。</param>
	public static implicit operator ConcurrentDictionary<TKey, TValue>(NullableConcurrentDictionary<TKey, TValue> dic)
	{
		ConcurrentDictionary<TKey, TValue> concurrentDictionary = new ConcurrentDictionary<TKey, TValue>();
		foreach (KeyValuePair<NullObject<TKey>, TValue> item in dic)
		{
			concurrentDictionary[item.Key] = item.Value;
		}
		return concurrentDictionary;
	}
}
