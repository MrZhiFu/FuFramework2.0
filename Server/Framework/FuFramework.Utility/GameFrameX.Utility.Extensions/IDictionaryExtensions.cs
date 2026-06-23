using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FuFramework.Utility.Extensions;

/// <summary>
/// 字典扩展
/// </summary>
public static class IDictionaryExtensions
{
	/// <summary>
	/// 添加或更新键值对
	/// </summary>
	/// <param name="self"></param>
	/// <param name="that">另一个字典集</param>
	public static void AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> self, IDictionary<TKey, TValue> that)
	{
		foreach (KeyValuePair<TKey, TValue> item in that)
		{
			self[item.Key] = item.Value;
		}
	}

	/// <summary>
	/// 添加或更新键值对
	/// </summary>
	/// <param name="self"></param>
	/// <param name="that">另一个字典集</param>
	public static void AddOrUpdate<TKey, TValue>(this NullableDictionary<TKey, TValue> self, IDictionary<TKey, TValue> that)
	{
		foreach (KeyValuePair<TKey, TValue> item in that)
		{
			self[item.Key] = item.Value;
		}
	}

	/// <summary>
	/// 添加或更新键值对
	/// </summary>
	/// <param name="self"></param>
	/// <param name="that">另一个字典集</param>
	public static void AddOrUpdate<TKey, TValue>(this NullableConcurrentDictionary<TKey, TValue> self, IDictionary<TKey, TValue> that)
	{
		foreach (KeyValuePair<TKey, TValue> item in that)
		{
			self[item.Key] = item.Value;
		}
	}

	/// <summary>
	/// 添加或更新键值对
	/// </summary>
	/// <param name="self"></param>
	/// <param name="key">键</param>
	/// <param name="addValue">添加时的值</param>
	/// <param name="updateValueFactory">更新时的操作</param>
	public static TValue AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> self, TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
	{
		if (!self.TryAdd(key, addValue))
		{
			self[key] = updateValueFactory(key, self[key]);
		}
		return self[key];
	}

	/// <summary>
	/// 添加或更新键值对
	/// </summary>
	/// <param name="self"></param>
	/// <param name="key">键</param>
	/// <param name="addValue">添加时的值</param>
	/// <param name="updateValueFactory">更新时的操作</param>
	public static TValue AddOrUpdate<TKey, TValue>(this NullableDictionary<TKey, TValue> self, TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
	{
		if (!self.TryAdd(key, addValue))
		{
			self[key] = updateValueFactory(key, self[key]);
		}
		return self[key];
	}

	/// <summary>
	/// 添加或更新键值对
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	/// <param name="self"></param>
	/// <param name="key">键</param>
	/// <param name="addValue">添加时的值</param>
	/// <param name="updateValueFactory">更新时的操作</param>
	public static TValue AddOrUpdate<TKey, TValue>(this NullableConcurrentDictionary<TKey, TValue> self, TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
	{
		if (!self.TryAdd(key, addValue))
		{
			self[key] = updateValueFactory(key, self[key]);
		}
		return self[key];
	}

	/// <summary>
	/// 添加或更新键值对
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	/// <param name="self"></param>
	/// <param name="key">键</param>
	/// <param name="addValue">添加时的值</param>
	/// <param name="updateValue">更新时的值</param>
	public static TValue AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> self, TKey key, TValue addValue, TValue updateValue)
	{
		if (!self.TryAdd(key, addValue))
		{
			self[key] = updateValue;
		}
		return self[key];
	}

	/// <summary>
	/// 添加或更新键值对
	/// </summary>
	/// <param name="self"></param>
	/// <param name="key">键</param>
	/// <param name="addValue">添加时的值</param>
	/// <param name="updateValue">更新时的值</param>
	public static TValue AddOrUpdate<TKey, TValue>(this NullableDictionary<TKey, TValue> self, TKey key, TValue addValue, TValue updateValue)
	{
		if (!self.TryAdd(key, addValue))
		{
			self[key] = updateValue;
		}
		return self[key];
	}

	/// <summary>
	/// 添加或更新键值对
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	/// <param name="self"></param>
	/// <param name="key">键</param>
	/// <param name="addValue">添加时的值</param>
	/// <param name="updateValue">更新时的值</param>
	public static TValue AddOrUpdate<TKey, TValue>(this NullableConcurrentDictionary<TKey, TValue> self, TKey key, TValue addValue, TValue updateValue)
	{
		if (!self.TryAdd(key, addValue))
		{
			self[key] = updateValue;
		}
		return self[key];
	}

	/// <summary>
	/// 添加或更新键值对
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	/// <param name="self"></param>
	/// <param name="that">另一个字典集</param>
	/// <param name="updateValueFactory">更新时的操作</param>
	public static void AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> self, IDictionary<TKey, TValue> that, Func<TKey, TValue, TValue> updateValueFactory)
	{
		foreach (KeyValuePair<TKey, TValue> item in that)
		{
			self.AddOrUpdate(item.Key, item.Value, updateValueFactory);
		}
	}

	/// <summary>
	/// 添加或更新键值对
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	/// <param name="self"></param>
	/// <param name="that">另一个字典集</param>
	/// <param name="updateValueFactory">更新时的操作</param>
	public static void AddOrUpdate<TKey, TValue>(this NullableDictionary<TKey, TValue> self, IDictionary<TKey, TValue> that, Func<TKey, TValue, TValue> updateValueFactory)
	{
		foreach (KeyValuePair<TKey, TValue> item in that)
		{
			self.AddOrUpdate(item.Key, item.Value, updateValueFactory);
		}
	}

	/// <summary>
	/// 添加或更新键值对
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	/// <param name="self"></param>
	/// <param name="that">另一个字典集</param>
	/// <param name="updateValueFactory">更新时的操作</param>
	public static void AddOrUpdate<TKey, TValue>(this NullableConcurrentDictionary<TKey, TValue> self, IDictionary<TKey, TValue> that, Func<TKey, TValue, TValue> updateValueFactory)
	{
		foreach (KeyValuePair<TKey, TValue> item in that)
		{
			self.AddOrUpdate(item.Key, item.Value, updateValueFactory);
		}
	}

	/// <summary>
	/// 添加或更新键值对
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	/// <param name="self"></param>
	/// <param name="key">键</param>
	/// <param name="addValueFactory">添加时的操作</param>
	/// <param name="updateValueFactory">更新时的操作</param>
	public static TValue AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> self, TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
	{
		if (!self.TryAdd(key, addValueFactory(key)))
		{
			self[key] = updateValueFactory(key, self[key]);
		}
		return self[key];
	}

	/// <summary>
	/// 添加或更新键值对
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	/// <param name="self"></param>
	/// <param name="key">键</param>
	/// <param name="addValueFactory">添加时的操作</param>
	/// <param name="updateValueFactory">更新时的操作</param>
	public static TValue AddOrUpdate<TKey, TValue>(this NullableDictionary<TKey, TValue> self, TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
	{
		if (!self.TryAdd(key, addValueFactory(key)))
		{
			self[key] = updateValueFactory(key, self[key]);
		}
		return self[key];
	}

	/// <summary>
	/// 添加或更新键值对
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	/// <param name="self"></param>
	/// <param name="key">键</param>
	/// <param name="addValueFactory">添加时的操作</param>
	/// <param name="updateValueFactory">更新时的操作</param>
	public static TValue AddOrUpdate<TKey, TValue>(this NullableConcurrentDictionary<TKey, TValue> self, TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
	{
		if (!self.TryAdd(key, addValueFactory(key)))
		{
			self[key] = updateValueFactory(key, self[key]);
		}
		return self[key];
	}

	/// <summary>
	/// 添加或更新键值对
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	/// <param name="self"></param>
	/// <param name="key">键</param>
	/// <param name="addValue">添加时的值</param>
	/// <param name="updateValueFactory">更新时的操作</param>
	public static async Task<TValue> AddOrUpdateAsync<TKey, TValue>(this IDictionary<TKey, TValue> self, TKey key, TValue addValue, Func<TKey, TValue, Task<TValue>> updateValueFactory)
	{
		if (!self.TryAdd(key, addValue))
		{
			self[key] = await updateValueFactory(key, self[key]);
		}
		return self[key];
	}

	/// <summary>
	/// 添加或更新键值对
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	/// <param name="self"></param>
	/// <param name="key">键</param>
	/// <param name="addValue">添加时的值</param>
	/// <param name="updateValueFactory">更新时的操作</param>
	public static async Task<TValue> AddOrUpdateAsync<TKey, TValue>(this NullableDictionary<TKey, TValue> self, TKey key, TValue addValue, Func<TKey, TValue, Task<TValue>> updateValueFactory)
	{
		if (!self.TryAdd(key, addValue))
		{
			self[key] = await updateValueFactory(key, self[key]);
		}
		return self[key];
	}

	/// <summary>
	/// 添加或更新键值对
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	/// <param name="self"></param>
	/// <param name="key">键</param>
	/// <param name="addValue">添加时的值</param>
	/// <param name="updateValueFactory">更新时的操作</param>
	public static async Task<TValue> AddOrUpdateAsync<TKey, TValue>(this NullableConcurrentDictionary<TKey, TValue> self, TKey key, TValue addValue, Func<TKey, TValue, Task<TValue>> updateValueFactory)
	{
		if (!self.TryAdd(key, addValue))
		{
			self[key] = await updateValueFactory(key, self[key]);
		}
		return self[key];
	}

	/// <summary>
	/// 添加或更新键值对
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	/// <param name="self"></param>
	/// <param name="that">另一个字典集</param>
	/// <param name="updateValueFactory">更新时的操作</param>
	public static Task AddOrUpdateAsync<TKey, TValue>(this IDictionary<TKey, TValue> self, IDictionary<TKey, TValue> that, Func<TKey, TValue, Task<TValue>> updateValueFactory)
	{
		return that.ForeachAsync((KeyValuePair<TKey, TValue> item) => self.AddOrUpdateAsync(item.Key, item.Value, updateValueFactory));
	}

	/// <summary>
	/// 添加或更新键值对
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	/// <param name="self"></param>
	/// <param name="that">另一个字典集</param>
	/// <param name="updateValueFactory">更新时的操作</param>
	public static Task AddOrUpdateAsync<TKey, TValue>(this NullableDictionary<TKey, TValue> self, IDictionary<TKey, TValue> that, Func<TKey, TValue, Task<TValue>> updateValueFactory)
	{
		return that.ForeachAsync((KeyValuePair<TKey, TValue> item) => self.AddOrUpdateAsync(item.Key, item.Value, updateValueFactory));
	}

	/// <summary>
	/// 添加或更新键值对
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	/// <param name="self"></param>
	/// <param name="that">另一个字典集</param>
	/// <param name="updateValueFactory">更新时的操作</param>
	public static Task AddOrUpdateAsync<TKey, TValue>(this NullableConcurrentDictionary<TKey, TValue> self, IDictionary<TKey, TValue> that, Func<TKey, TValue, Task<TValue>> updateValueFactory)
	{
		return that.ForeachAsync((KeyValuePair<TKey, TValue> item) => self.AddOrUpdateAsync(item.Key, item.Value, updateValueFactory));
	}

	/// <summary>
	/// 添加或更新键值对
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	/// <param name="self"></param>
	/// <param name="key">键</param>
	/// <param name="addValueFactory">添加时的操作</param>
	/// <param name="updateValueFactory">更新时的操作</param>
	public static async Task<TValue> AddOrUpdateAsync<TKey, TValue>(this IDictionary<TKey, TValue> self, TKey key, Func<TKey, Task<TValue>> addValueFactory, Func<TKey, TValue, Task<TValue>> updateValueFactory)
	{
		IDictionary<TKey, TValue> dictionary = self;
		TKey key2 = key;
		if (!dictionary.TryAdd(key2, await addValueFactory(key)))
		{
			dictionary = self;
			key2 = key;
			dictionary[key2] = await updateValueFactory(key, self[key]);
		}
		return self[key];
	}

	/// <summary>
	/// 添加或更新键值对
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	/// <param name="self"></param>
	/// <param name="key">键</param>
	/// <param name="addValueFactory">添加时的操作</param>
	/// <param name="updateValueFactory">更新时的操作</param>
	public static async Task<TValue> AddOrUpdateAsync<TKey, TValue>(this NullableDictionary<TKey, TValue> self, TKey key, Func<TKey, Task<TValue>> addValueFactory, Func<TKey, TValue, Task<TValue>> updateValueFactory)
	{
		NullableDictionary<TKey, TValue> nullableDictionary = self;
		NullObject<TKey> key2 = key;
		if (!nullableDictionary.TryAdd(key2, await addValueFactory(key)))
		{
			nullableDictionary = self;
			nullableDictionary[key] = await updateValueFactory(key, self[key]);
		}
		return self[key];
	}

	/// <summary>
	/// 添加或更新键值对
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	/// <param name="self"></param>
	/// <param name="key">键</param>
	/// <param name="addValueFactory">添加时的操作</param>
	/// <param name="updateValueFactory">更新时的操作</param>
	public static async Task<TValue> AddOrUpdateAsync<TKey, TValue>(this NullableConcurrentDictionary<TKey, TValue> self, TKey key, Func<TKey, Task<TValue>> addValueFactory, Func<TKey, TValue, Task<TValue>> updateValueFactory)
	{
		NullableConcurrentDictionary<TKey, TValue> nullableConcurrentDictionary = self;
		TKey key2 = key;
		if (!nullableConcurrentDictionary.TryAdd(key2, await addValueFactory(key)))
		{
			nullableConcurrentDictionary = self;
			key2 = key;
			nullableConcurrentDictionary[key2] = await updateValueFactory(key, self[key]);
		}
		return self[key];
	}

	/// <summary>
	/// 添加或更新键值对
	/// </summary>
	/// <param name="self"></param>
	/// <param name="that">另一个字典集</param>
	public static void AddOrUpdateTo<TKey, TValue>(this IDictionary<TKey, TValue> self, IDictionary<TKey, TValue> that)
	{
		foreach (KeyValuePair<TKey, TValue> item in self)
		{
			that[item.Key] = item.Value;
		}
	}

	/// <summary>
	/// 添加或更新键值对
	/// </summary>
	/// <param name="self"></param>
	/// <param name="that">另一个字典集</param>
	public static void AddOrUpdateTo<TKey, TValue>(this NullableDictionary<TKey, TValue> self, IDictionary<TKey, TValue> that)
	{
		foreach (KeyValuePair<NullObject<TKey>, TValue> item in self)
		{
			that[item.Key] = item.Value;
		}
	}

	/// <summary>
	/// 添加或更新键值对
	/// </summary>
	/// <param name="self"></param>
	/// <param name="that">另一个字典集</param>
	public static void AddOrUpdateTo<TKey, TValue>(this NullableConcurrentDictionary<TKey, TValue> self, IDictionary<TKey, TValue> that)
	{
		foreach (KeyValuePair<NullObject<TKey>, TValue> item in self)
		{
			that[item.Key] = item.Value;
		}
	}

	/// <summary>
	/// 添加或更新键值对
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	/// <param name="self"></param>
	/// <param name="that">另一个字典集</param>
	/// <param name="updateValueFactory">更新时的操作</param>
	public static void AddOrUpdateTo<TKey, TValue>(this IDictionary<TKey, TValue> self, IDictionary<TKey, TValue> that, Func<TKey, TValue, TValue> updateValueFactory)
	{
		foreach (KeyValuePair<TKey, TValue> item in self)
		{
			that.AddOrUpdate(item.Key, item.Value, updateValueFactory);
		}
	}

	/// <summary>
	/// 添加或更新键值对
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	/// <param name="self"></param>
	/// <param name="that">另一个字典集</param>
	/// <param name="updateValueFactory">更新时的操作</param>
	public static void AddOrUpdateTo<TKey, TValue>(this IDictionary<TKey, TValue> self, NullableDictionary<TKey, TValue> that, Func<TKey, TValue, TValue> updateValueFactory)
	{
		foreach (KeyValuePair<TKey, TValue> item in self)
		{
			that.AddOrUpdate(item.Key, item.Value, updateValueFactory);
		}
	}

	/// <summary>
	/// 添加或更新键值对
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	/// <param name="self"></param>
	/// <param name="that">另一个字典集</param>
	/// <param name="updateValueFactory">更新时的操作</param>
	public static void AddOrUpdateTo<TKey, TValue>(this IDictionary<TKey, TValue> self, NullableConcurrentDictionary<TKey, TValue> that, Func<TKey, TValue, TValue> updateValueFactory)
	{
		foreach (KeyValuePair<TKey, TValue> item in self)
		{
			that.AddOrUpdate(item.Key, item.Value, updateValueFactory);
		}
	}

	/// <summary>
	/// 添加或更新键值对
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	/// <param name="self"></param>
	/// <param name="that">另一个字典集</param>
	/// <param name="updateValueFactory">更新时的操作</param>
	public static Task AddOrUpdateToAsync<TKey, TValue>(this IDictionary<TKey, TValue> self, IDictionary<TKey, TValue> that, Func<TKey, TValue, Task<TValue>> updateValueFactory)
	{
		return self.ForeachAsync((KeyValuePair<TKey, TValue> item) => that.AddOrUpdateAsync(item.Key, item.Value, updateValueFactory));
	}

	/// <summary>
	/// 添加或更新键值对
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	/// <param name="self"></param>
	/// <param name="that">另一个字典集</param>
	/// <param name="updateValueFactory">更新时的操作</param>
	public static Task AddOrUpdateToAsync<TKey, TValue>(this IDictionary<TKey, TValue> self, NullableDictionary<TKey, TValue> that, Func<TKey, TValue, Task<TValue>> updateValueFactory)
	{
		return self.ForeachAsync((KeyValuePair<TKey, TValue> item) => that.AddOrUpdateAsync(item.Key, item.Value, updateValueFactory));
	}

	/// <summary>
	/// 添加或更新键值对
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	/// <param name="self"></param>
	/// <param name="that">另一个字典集</param>
	/// <param name="updateValueFactory">更新时的操作</param>
	public static Task AddOrUpdateToAsync<TKey, TValue>(this IDictionary<TKey, TValue> self, NullableConcurrentDictionary<TKey, TValue> that, Func<TKey, TValue, Task<TValue>> updateValueFactory)
	{
		return self.ForeachAsync((KeyValuePair<TKey, TValue> item) => that.AddOrUpdateAsync(item.Key, item.Value, updateValueFactory));
	}

	/// <summary>
	/// 获取或添加
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	/// <param name="self"></param>
	/// <param name="key"></param>
	/// <param name="addValueFactory"></param>
	public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> self, TKey key, Func<TValue> addValueFactory)
	{
		if (!self.ContainsKey(key))
		{
			self[key] = addValueFactory();
		}
		return self[key];
	}

	/// <summary>
	/// 获取或添加
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	/// <param name="self"></param>
	/// <param name="key"></param>
	/// <param name="addValue"></param>
	public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> self, TKey key, TValue addValue)
	{
		if (!self.TryAdd(key, addValue))
		{
			return self[key];
		}
		return addValue;
	}

	/// <summary>
	/// 获取或添加
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	/// <param name="self"></param>
	/// <param name="key"></param>
	/// <param name="addValueFactory"></param>
	public static async Task<TValue> GetOrAddAsync<TKey, TValue>(this IDictionary<TKey, TValue> self, TKey key, Func<Task<TValue>> addValueFactory)
	{
		if (!self.ContainsKey(key))
		{
			self[key] = await addValueFactory();
		}
		return self[key];
	}

	private static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value) where TKey : notnull
	{
		if (dictionary == null)
		{
			throw new ArgumentNullException("dictionary");
		}
		if (dictionary.IsReadOnly || dictionary.ContainsKey(key))
		{
			return false;
		}
		dictionary.Add(key, value);
		return true;
	}

	/// <summary>
	/// 遍历IEnumerable
	/// </summary>
	/// <param name="dic"></param>
	/// <param name="action">回调方法</param>
	public static void ForEach<TKey, TValue>(this IDictionary<TKey, TValue> dic, Action<TKey, TValue> action)
	{
		foreach (KeyValuePair<TKey, TValue> item in dic)
		{
			action(item.Key, item.Value);
		}
	}

	/// <summary>
	/// 遍历IDictionary
	/// </summary>
	/// <param name="dic"></param>
	/// <param name="action">回调方法</param>
	public static Task ForEachAsync<TKey, TValue>(this IDictionary<TKey, TValue> dic, Func<TKey, TValue, Task> action)
	{
		return dic.ForeachAsync((KeyValuePair<TKey, TValue> x) => action(x.Key, x.Value));
	}

	/// <summary>
	/// 安全的转换成字典集
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <typeparam name="TKey"></typeparam>
	/// <param name="source"></param>
	/// <param name="keySelector">键选择器</param>
	public static NullableDictionary<TKey, TSource> ToDictionarySafety<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
	{
		IList<TSource> obj = (source as IList<TSource>) ?? source.ToList();
		NullableDictionary<TKey, TSource> nullableDictionary = new NullableDictionary<TKey, TSource>(obj.Count);
		foreach (TSource item in obj)
		{
			nullableDictionary[keySelector(item)] = item;
		}
		return nullableDictionary;
	}

	/// <summary>
	/// 安全的转换成字典集
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <typeparam name="TKey"></typeparam>
	/// <param name="source"></param>
	/// <param name="keySelector">键选择器</param>
	/// <param name="defaultValue">键未找到时的默认值</param>
	public static NullableDictionary<TKey, TSource> ToDictionarySafety<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, TSource defaultValue)
	{
		IList<TSource> obj = (source as IList<TSource>) ?? source.ToList();
		NullableDictionary<TKey, TSource> nullableDictionary = new NullableDictionary<TKey, TSource>(obj.Count)
		{
			FallbackValue = defaultValue
		};
		foreach (TSource item in obj)
		{
			nullableDictionary[keySelector(item)] = item;
		}
		return nullableDictionary;
	}

	/// <summary>
	/// 安全的转换成字典集
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TElement"></typeparam>
	/// <param name="source"></param>
	/// <param name="keySelector">键选择器</param>
	/// <param name="elementSelector">值选择器</param>
	public static NullableDictionary<TKey, TElement> ToDictionarySafety<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
	{
		IList<TSource> obj = (source as IList<TSource>) ?? source.ToList();
		NullableDictionary<TKey, TElement> nullableDictionary = new NullableDictionary<TKey, TElement>(obj.Count);
		foreach (TSource item in obj)
		{
			nullableDictionary[keySelector(item)] = elementSelector(item);
		}
		return nullableDictionary;
	}

	/// <summary>
	/// 安全的转换成字典集
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TElement"></typeparam>
	/// <param name="source"></param>
	/// <param name="keySelector">键选择器</param>
	/// <param name="elementSelector">值选择器</param>
	/// <param name="defaultValue">键未找到时的默认值</param>
	public static NullableDictionary<TKey, TElement> ToDictionarySafety<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, TElement defaultValue)
	{
		IList<TSource> obj = (source as IList<TSource>) ?? source.ToList();
		NullableDictionary<TKey, TElement> nullableDictionary = new NullableDictionary<TKey, TElement>(obj.Count)
		{
			FallbackValue = defaultValue
		};
		foreach (TSource item in obj)
		{
			nullableDictionary[keySelector(item)] = elementSelector(item);
		}
		return nullableDictionary;
	}

	/// <summary>
	/// 安全的转换成字典集
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TElement"></typeparam>
	/// <param name="source"></param>
	/// <param name="keySelector">键选择器</param>
	/// <param name="elementSelector">值选择器</param>
	public static async Task<NullableDictionary<TKey, TElement>> ToDictionarySafetyAsync<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, Task<TElement>> elementSelector)
	{
		IList<TSource> list = (source as IList<TSource>) ?? source.ToList();
		NullableDictionary<TKey, TElement> dic = new NullableDictionary<TKey, TElement>(list.Count);
		await list.ForeachAsync(async delegate(TSource item)
		{
			NullableDictionary<TKey, TElement> nullableDictionary = dic;
			TKey key = keySelector(item);
			nullableDictionary[key] = await elementSelector(item);
		});
		return dic;
	}

	/// <summary>
	/// 安全的转换成字典集
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TElement"></typeparam>
	/// <param name="source"></param>
	/// <param name="keySelector">键选择器</param>
	/// <param name="elementSelector">值选择器</param>
	/// <param name="defaultValue">键未找到时的默认值</param>
	public static async Task<NullableDictionary<TKey, TElement>> ToDictionarySafetyAsync<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, Task<TElement>> elementSelector, TElement defaultValue)
	{
		IList<TSource> list = (source as IList<TSource>) ?? source.ToList();
		NullableDictionary<TKey, TElement> dic = new NullableDictionary<TKey, TElement>(list.Count)
		{
			FallbackValue = defaultValue
		};
		await list.ForeachAsync(async delegate(TSource item)
		{
			NullableDictionary<TKey, TElement> nullableDictionary = dic;
			TKey key = keySelector(item);
			nullableDictionary[key] = await elementSelector(item);
		});
		return dic;
	}

	/// <summary>
	/// 安全的转换成字典集
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <typeparam name="TKey"></typeparam>
	/// <param name="source"></param>
	/// <param name="keySelector">键选择器</param>
	public static DisposableDictionary<TKey, TSource> ToDisposableDictionarySafety<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) where TSource : IDisposable
	{
		IList<TSource> obj = (source as IList<TSource>) ?? source.ToList();
		DisposableDictionary<TKey, TSource> disposableDictionary = new DisposableDictionary<TKey, TSource>(obj.Count);
		foreach (TSource item in obj)
		{
			disposableDictionary[keySelector(item)] = item;
		}
		return disposableDictionary;
	}

	/// <summary>
	/// 安全的转换成字典集
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <typeparam name="TKey"></typeparam>
	/// <param name="source"></param>
	/// <param name="keySelector">键选择器</param>
	/// <param name="defaultValue">键未找到时的默认值</param>
	public static DisposableDictionary<TKey, TSource> ToDisposableDictionarySafety<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, TSource defaultValue) where TSource : IDisposable
	{
		IList<TSource> obj = (source as IList<TSource>) ?? source.ToList();
		DisposableDictionary<TKey, TSource> disposableDictionary = new DisposableDictionary<TKey, TSource>(obj.Count)
		{
			FallbackValue = defaultValue
		};
		foreach (TSource item in obj)
		{
			disposableDictionary[keySelector(item)] = item;
		}
		return disposableDictionary;
	}

	/// <summary>
	/// 安全的转换成字典集
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TElement"></typeparam>
	/// <param name="source"></param>
	/// <param name="keySelector">键选择器</param>
	/// <param name="elementSelector">值选择器</param>
	public static DisposableDictionary<TKey, TElement> ToDisposableDictionarySafety<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector) where TElement : IDisposable
	{
		IList<TSource> obj = (source as IList<TSource>) ?? source.ToList();
		DisposableDictionary<TKey, TElement> disposableDictionary = new DisposableDictionary<TKey, TElement>(obj.Count);
		foreach (TSource item in obj)
		{
			disposableDictionary[keySelector(item)] = elementSelector(item);
		}
		return disposableDictionary;
	}

	/// <summary>
	/// 安全的转换成字典集
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TElement"></typeparam>
	/// <param name="source"></param>
	/// <param name="keySelector">键选择器</param>
	/// <param name="elementSelector">值选择器</param>
	/// <param name="defaultValue">键未找到时的默认值</param>
	public static DisposableDictionary<TKey, TElement> ToDisposableDictionarySafety<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, TElement defaultValue) where TElement : IDisposable
	{
		IList<TSource> obj = (source as IList<TSource>) ?? source.ToList();
		DisposableDictionary<TKey, TElement> disposableDictionary = new DisposableDictionary<TKey, TElement>(obj.Count)
		{
			FallbackValue = defaultValue
		};
		foreach (TSource item in obj)
		{
			disposableDictionary[keySelector(item)] = elementSelector(item);
		}
		return disposableDictionary;
	}

	/// <summary>
	/// 安全的转换成字典集
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TElement"></typeparam>
	/// <param name="source"></param>
	/// <param name="keySelector">键选择器</param>
	/// <param name="elementSelector">值选择器</param>
	public static async Task<DisposableDictionary<TKey, TElement>> ToDisposableDictionarySafetyAsync<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, Task<TElement>> elementSelector) where TElement : IDisposable
	{
		IList<TSource> list = (source as IList<TSource>) ?? source.ToList();
		DisposableDictionary<TKey, TElement> dic = new DisposableDictionary<TKey, TElement>(list.Count);
		await list.ForeachAsync(async delegate(TSource item)
		{
			DisposableDictionary<TKey, TElement> disposableDictionary = dic;
			TKey key = keySelector(item);
			disposableDictionary[key] = await elementSelector(item);
		});
		return dic;
	}

	/// <summary>
	/// 安全的转换成字典集
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TElement"></typeparam>
	/// <param name="source"></param>
	/// <param name="keySelector">键选择器</param>
	/// <param name="elementSelector">值选择器</param>
	/// <param name="defaultValue">键未找到时的默认值</param>
	public static async Task<DisposableDictionary<TKey, TElement>> ToDisposableDictionarySafetyAsync<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, Task<TElement>> elementSelector, TElement defaultValue) where TElement : IDisposable
	{
		IList<TSource> list = (source as IList<TSource>) ?? source.ToList();
		DisposableDictionary<TKey, TElement> dic = new DisposableDictionary<TKey, TElement>(list.Count)
		{
			FallbackValue = defaultValue
		};
		await list.ForeachAsync(async delegate(TSource item)
		{
			DisposableDictionary<TKey, TElement> disposableDictionary = dic;
			TKey key = keySelector(item);
			disposableDictionary[key] = await elementSelector(item);
		});
		return dic;
	}

	/// <summary>
	/// 安全的转换成字典集
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <typeparam name="TKey"></typeparam>
	/// <param name="source"></param>
	/// <param name="keySelector">键选择器</param>
	public static NullableConcurrentDictionary<TKey, TSource> ToConcurrentDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
	{
		NullableConcurrentDictionary<TKey, TSource> nullableConcurrentDictionary = new NullableConcurrentDictionary<TKey, TSource>();
		foreach (TSource item in source)
		{
			nullableConcurrentDictionary[keySelector(item)] = item;
		}
		return nullableConcurrentDictionary;
	}

	/// <summary>
	/// 安全的转换成字典集
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <typeparam name="TKey"></typeparam>
	/// <param name="source"></param>
	/// <param name="keySelector">键选择器</param>
	/// <param name="defaultValue">键未找到时的默认值</param>
	public static NullableConcurrentDictionary<TKey, TSource> ToConcurrentDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, TSource defaultValue)
	{
		NullableConcurrentDictionary<TKey, TSource> nullableConcurrentDictionary = new NullableConcurrentDictionary<TKey, TSource>
		{
			FallbackValue = defaultValue
		};
		foreach (TSource item in source)
		{
			nullableConcurrentDictionary[keySelector(item)] = item;
		}
		return nullableConcurrentDictionary;
	}

	/// <summary>
	/// 安全的转换成字典集
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TElement"></typeparam>
	/// <param name="source"></param>
	/// <param name="keySelector">键选择器</param>
	/// <param name="elementSelector">值选择器</param>
	public static NullableConcurrentDictionary<TKey, TElement> ToConcurrentDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
	{
		NullableConcurrentDictionary<TKey, TElement> nullableConcurrentDictionary = new NullableConcurrentDictionary<TKey, TElement>();
		foreach (TSource item in source)
		{
			nullableConcurrentDictionary[keySelector(item)] = elementSelector(item);
		}
		return nullableConcurrentDictionary;
	}

	/// <summary>
	/// 安全的转换成字典集
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TElement"></typeparam>
	/// <param name="source"></param>
	/// <param name="keySelector">键选择器</param>
	/// <param name="elementSelector">值选择器</param>
	/// <param name="defaultValue"></param>
	public static NullableConcurrentDictionary<TKey, TElement> ToConcurrentDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, TElement defaultValue)
	{
		NullableConcurrentDictionary<TKey, TElement> nullableConcurrentDictionary = new NullableConcurrentDictionary<TKey, TElement>
		{
			FallbackValue = defaultValue
		};
		foreach (TSource item in source)
		{
			nullableConcurrentDictionary[keySelector(item)] = elementSelector(item);
		}
		return nullableConcurrentDictionary;
	}

	/// <summary>
	/// 安全的转换成字典集
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TElement"></typeparam>
	/// <param name="source"></param>
	/// <param name="keySelector">键选择器</param>
	/// <param name="elementSelector">值选择器</param>
	public static async Task<NullableConcurrentDictionary<TKey, TElement>> ToConcurrentDictionaryAsync<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, Task<TElement>> elementSelector)
	{
		ConcurrentDictionary<TKey, TElement> dic = new ConcurrentDictionary<TKey, TElement>();
		await source.ForeachAsync(async delegate(TSource item)
		{
			ConcurrentDictionary<TKey, TElement> concurrentDictionary = dic;
			TKey key = keySelector(item);
			concurrentDictionary[key] = await elementSelector(item);
		});
		return dic;
	}

	/// <summary>
	/// 安全的转换成字典集
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TElement"></typeparam>
	/// <param name="source"></param>
	/// <param name="keySelector">键选择器</param>
	/// <param name="elementSelector">值选择器</param>
	/// <param name="defaultValue">键未找到时的默认值</param>
	public static async Task<NullableConcurrentDictionary<TKey, TElement>> ToConcurrentDictionaryAsync<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, Task<TElement>> elementSelector, TElement defaultValue)
	{
		NullableConcurrentDictionary<TKey, TElement> dic = new NullableConcurrentDictionary<TKey, TElement>
		{
			FallbackValue = defaultValue
		};
		await source.ForeachAsync(async delegate(TSource item)
		{
			NullableConcurrentDictionary<TKey, TElement> nullableConcurrentDictionary = dic;
			TKey key = keySelector(item);
			nullableConcurrentDictionary[key] = await elementSelector(item);
		});
		return dic;
	}

	/// <summary>
	/// 安全的转换成字典集
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <typeparam name="TKey"></typeparam>
	/// <param name="source"></param>
	/// <param name="keySelector">键选择器</param>
	public static DisposableConcurrentDictionary<TKey, TSource> ToDisposableConcurrentDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) where TSource : IDisposable
	{
		DisposableConcurrentDictionary<TKey, TSource> disposableConcurrentDictionary = new DisposableConcurrentDictionary<TKey, TSource>();
		foreach (TSource item in source)
		{
			disposableConcurrentDictionary[keySelector(item)] = item;
		}
		return disposableConcurrentDictionary;
	}

	/// <summary>
	/// 安全的转换成字典集
	/// </summary>
	/// <param name="source"></param>
	/// <param name="keySelector">键选择器</param>
	/// <param name="defaultValue">键未找到时的默认值</param>
	public static DisposableConcurrentDictionary<TKey, TSource> ToDisposableConcurrentDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, TSource defaultValue) where TSource : IDisposable
	{
		DisposableConcurrentDictionary<TKey, TSource> disposableConcurrentDictionary = new DisposableConcurrentDictionary<TKey, TSource>
		{
			FallbackValue = defaultValue
		};
		foreach (TSource item in source)
		{
			disposableConcurrentDictionary[keySelector(item)] = item;
		}
		return disposableConcurrentDictionary;
	}

	/// <summary>
	/// 安全的转换成字典集
	/// </summary>
	/// <param name="source"></param>
	/// <param name="keySelector">键选择器</param>
	/// <param name="elementSelector">值选择器</param>
	public static DisposableConcurrentDictionary<TKey, TElement> ToDisposableConcurrentDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector) where TElement : IDisposable
	{
		DisposableConcurrentDictionary<TKey, TElement> disposableConcurrentDictionary = new DisposableConcurrentDictionary<TKey, TElement>();
		foreach (TSource item in source)
		{
			disposableConcurrentDictionary[keySelector(item)] = elementSelector(item);
		}
		return disposableConcurrentDictionary;
	}

	/// <summary>
	/// 安全的转换成字典集
	/// </summary>
	/// <param name="source"></param>
	/// <param name="keySelector">键选择器</param>
	/// <param name="elementSelector">值选择器</param>
	/// <param name="defaultValue"></param>
	public static DisposableConcurrentDictionary<TKey, TElement> ToDisposableConcurrentDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, TElement defaultValue) where TElement : IDisposable
	{
		DisposableConcurrentDictionary<TKey, TElement> disposableConcurrentDictionary = new DisposableConcurrentDictionary<TKey, TElement>
		{
			FallbackValue = defaultValue
		};
		foreach (TSource item in source)
		{
			disposableConcurrentDictionary[keySelector(item)] = elementSelector(item);
		}
		return disposableConcurrentDictionary;
	}

	/// <summary>
	/// 安全的转换成字典集
	/// </summary>
	/// <param name="source"></param>
	/// <param name="keySelector">键选择器</param>
	/// <param name="elementSelector">值选择器</param>
	public static async Task<DisposableConcurrentDictionary<TKey, TElement>> ToDisposableConcurrentDictionaryAsync<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, Task<TElement>> elementSelector) where TElement : IDisposable
	{
		DisposableConcurrentDictionary<TKey, TElement> dic = new DisposableConcurrentDictionary<TKey, TElement>();
		await source.ForeachAsync(async delegate(TSource item)
		{
			DisposableConcurrentDictionary<TKey, TElement> disposableConcurrentDictionary = dic;
			TKey key = keySelector(item);
			disposableConcurrentDictionary[key] = await elementSelector(item);
		});
		return dic;
	}

	/// <summary>
	/// 安全的转换成字典集
	/// </summary>
	/// <param name="source"></param>
	/// <param name="keySelector">键选择器</param>
	/// <param name="elementSelector">值选择器</param>
	/// <param name="defaultValue">键未找到时的默认值</param>
	public static async Task<DisposableConcurrentDictionary<TKey, TElement>> ToDisposableConcurrentDictionaryAsync<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, Task<TElement>> elementSelector, TElement defaultValue) where TElement : IDisposable
	{
		DisposableConcurrentDictionary<TKey, TElement> dic = new DisposableConcurrentDictionary<TKey, TElement>
		{
			FallbackValue = defaultValue
		};
		await source.ForeachAsync(async delegate(TSource item)
		{
			DisposableConcurrentDictionary<TKey, TElement> disposableConcurrentDictionary = dic;
			TKey key = keySelector(item);
			disposableConcurrentDictionary[key] = await elementSelector(item);
		});
		return dic;
	}

	/// <summary>
	/// 转换为Lookup
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <typeparam name="TKey"></typeparam>
	/// <param name="source"></param>
	/// <param name="keySelector">键选择器</param>
	public static LookupX<TKey, TSource> ToLookupX<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
	{
		IList<TSource> obj = (source as IList<TSource>) ?? source.ToList();
		Dictionary<TKey, List<TSource>> dictionary = new Dictionary<TKey, List<TSource>>(obj.Count);
		foreach (TSource item in obj)
		{
			TKey key = keySelector(item);
			if (dictionary.TryGetValue(key, out var value))
			{
				value.Add(item);
				continue;
			}
			dictionary.Add(key, new List<TSource> { item });
		}
		return new LookupX<TKey, TSource>(dictionary);
	}

	/// <summary>
	/// 转换为Lookup
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TElement"></typeparam>
	/// <param name="source"></param>
	/// <param name="keySelector">键选择器</param>
	/// <param name="elementSelector">值选择器</param>
	public static LookupX<TKey, TElement> ToLookupX<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
	{
		IList<TSource> obj = (source as IList<TSource>) ?? source.ToList();
		Dictionary<TKey, List<TElement>> dictionary = new Dictionary<TKey, List<TElement>>(obj.Count);
		foreach (TSource item in obj)
		{
			TKey key = keySelector(item);
			if (dictionary.TryGetValue(key, out var value))
			{
				value.Add(elementSelector(item));
				continue;
			}
			dictionary.Add(key, new List<TElement> { elementSelector(item) });
		}
		return new LookupX<TKey, TElement>(dictionary);
	}

	/// <summary>
	/// 转换为Lookup
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TElement"></typeparam>
	/// <param name="source"></param>
	/// <param name="keySelector">键选择器</param>
	/// <param name="elementSelector">值选择器</param>
	public static async Task<LookupX<TKey, TElement>> ToLookupAsync<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, Task<TElement>> elementSelector)
	{
		IList<TSource> source2 = (source as IList<TSource>) ?? source.ToList();
		ConcurrentDictionary<TKey, List<TElement>> dic = new ConcurrentDictionary<TKey, List<TElement>>();
		await source2.ForeachAsync(async delegate(TSource item)
		{
			TKey val = keySelector(item);
			if (dic.TryGetValue(val, out var value))
			{
				List<TElement> list = value;
				list.Add(await elementSelector(item));
			}
			else
			{
				ConcurrentDictionary<TKey, List<TElement>> concurrentDictionary = dic;
				TKey key = val;
				List<TElement> list2 = new List<TElement>();
				List<TElement> list = list2;
				list.Add(await elementSelector(item));
				concurrentDictionary.TryAdd(key, list2);
			}
		});
		return new LookupX<TKey, TElement>(dic);
	}

	/// <summary>
	/// 转换成并发字典集合
	/// </summary>
	public static NullableConcurrentDictionary<TKey, TValue> AsConcurrentDictionary<TKey, TValue>(this Dictionary<TKey, TValue> dic)
	{
		return dic;
	}

	/// <summary>
	/// 转换成并发字典集合
	/// </summary>
	/// <param name="dic"></param>
	/// <param name="defaultValue">键未找到时的默认值</param>
	public static NullableConcurrentDictionary<TKey, TValue> AsConcurrentDictionary<TKey, TValue>(this Dictionary<TKey, TValue> dic, TValue defaultValue)
	{
		NullableConcurrentDictionary<TKey, TValue> nullableConcurrentDictionary = new NullableConcurrentDictionary<TKey, TValue>
		{
			FallbackValue = defaultValue
		};
		foreach (KeyValuePair<TKey, TValue> item in dic)
		{
			nullableConcurrentDictionary[item.Key] = item.Value;
		}
		return nullableConcurrentDictionary;
	}

	/// <summary>
	/// 转换成普通字典集合
	/// </summary>
	public static NullableDictionary<TKey, TValue> AsDictionary<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dic)
	{
		return dic;
	}

	/// <summary>
	/// 转换成普通字典集合
	/// </summary>
	/// <param name="dic"></param>
	/// <param name="defaultValue">键未找到时的默认值</param>
	public static NullableDictionary<TKey, TValue> AsDictionary<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dic, TValue defaultValue)
	{
		NullableDictionary<TKey, TValue> nullableDictionary = new NullableDictionary<TKey, TValue>
		{
			FallbackValue = defaultValue
		};
		foreach (KeyValuePair<TKey, TValue> item in dic)
		{
			nullableDictionary[item.Key] = item.Value;
		}
		return nullableDictionary;
	}
}
