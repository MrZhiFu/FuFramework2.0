using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace FuFramework.Utility.Extensions;

/// <summary>
/// </summary>
public static class IEnumerableExtensions
{
	/// <summary>
	/// 按字段属性判等取交集
	/// </summary>
	/// <typeparam name="TFirst">第一个集合的元素类型</typeparam>
	/// <typeparam name="TSecond">第二个集合的元素类型</typeparam>
	/// <param name="first">第一个集合</param>
	/// <param name="second">第二个集合</param>
	/// <param name="condition">用于判断两个元素是否相等的条件</param>
	/// <returns>返回两个集合中满足条件的交集元素</returns>
	public static IEnumerable<TFirst> IntersectBy<TFirst, TSecond>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second, Func<TFirst, TSecond, bool> condition)
	{
		return first.Where((TFirst f) => second.Any((TSecond s) => condition(f, s)));
	}

	/// <summary>
	/// 按字段属性判等取交集
	/// </summary>
	/// <typeparam name="TSource">集合的元素类型</typeparam>
	/// <typeparam name="TKey">键的选择器返回的键类型</typeparam>
	/// <param name="first">第一个集合</param>
	/// <param name="second">第二个集合</param>
	/// <param name="keySelector">用于从每个元素中提取键的函数</param>
	/// <returns>返回两个集合中具有相同键的交集元素</returns>
	public static IEnumerable<TSource> IntersectBy<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TKey> keySelector)
	{
		return first.IntersectBy(second, keySelector, null);
	}

	/// <summary>
	/// 按字段属性判等取交集
	/// </summary>
	/// <typeparam name="TSource">集合的元素类型</typeparam>
	/// <typeparam name="TKey">键的选择器返回的键类型</typeparam>
	/// <param name="first">第一个集合</param>
	/// <param name="second">第二个集合</param>
	/// <param name="keySelector">用于从每个元素中提取键的函数</param>
	/// <param name="comparer">用于比较键的比较器</param>
	/// <returns>返回两个集合中具有相同键的交集元素</returns>
	public static IEnumerable<TSource> IntersectBy<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
	{
		ArgumentNullException.ThrowIfNull(first, "first");
		ArgumentNullException.ThrowIfNull(second, "second");
		ArgumentNullException.ThrowIfNull(keySelector, "keySelector");
		return IntersectByIterator(first, second, keySelector, comparer);
	}

	private static IEnumerable<TSource> IntersectByIterator<TSource, TKey>(IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
	{
		HashSet<TKey> set = new HashSet<TKey>(second.Select(keySelector), comparer);
		foreach (TSource item in first.Where((TSource source) => set.Remove(keySelector(source))))
		{
			yield return item;
		}
	}

	/// <summary>
	/// 多个集合取交集元素
	/// </summary>
	/// <typeparam name="T">集合的元素类型</typeparam>
	/// <param name="source">多个集合的序列</param>
	/// <returns>返回所有集合的交集元素</returns>
	public static IEnumerable<T> IntersectAll<T>(this IEnumerable<IEnumerable<T>> source)
	{
		return source.Aggregate((IEnumerable<T> current, IEnumerable<T> item) => current.Intersect(item));
	}

	/// <summary>
	/// 多个集合取交集元素
	/// </summary>
	/// <typeparam name="TSource">集合的元素类型</typeparam>
	/// <typeparam name="TKey">键的选择器返回的键类型</typeparam>
	/// <param name="source">多个集合的序列</param>
	/// <param name="keySelector">用于从每个元素中提取键的函数</param>
	/// <returns>返回所有集合的交集元素</returns>
	public static IEnumerable<TSource> IntersectAll<TSource, TKey>(this IEnumerable<IEnumerable<TSource>> source, Func<TSource, TKey> keySelector)
	{
		return source.Aggregate((IEnumerable<TSource> current, IEnumerable<TSource> item) => current.IntersectBy(item, keySelector));
	}

	/// <summary>
	/// 多个集合取交集元素
	/// </summary>
	/// <typeparam name="TSource">集合的元素类型</typeparam>
	/// <typeparam name="TKey">键的选择器返回的键类型</typeparam>
	/// <param name="source">多个集合的序列</param>
	/// <param name="keySelector">用于从每个元素中提取键的函数</param>
	/// <param name="comparer">用于比较键的比较器</param>
	/// <returns>返回所有集合的交集元素</returns>
	public static IEnumerable<TSource> IntersectAll<TSource, TKey>(this IEnumerable<IEnumerable<TSource>> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
	{
		return source.Aggregate((IEnumerable<TSource> current, IEnumerable<TSource> item) => current.IntersectBy(item, keySelector, comparer));
	}

	/// <summary>
	/// 多个集合取交集元素
	/// </summary>
	/// <typeparam name="T">集合的元素类型</typeparam>
	/// <param name="source">多个集合的序列</param>
	/// <param name="comparer">用于比较元素的比较器</param>
	/// <returns>返回所有集合的交集元素</returns>
	public static IEnumerable<T> IntersectAll<T>(this IEnumerable<IEnumerable<T>> source, IEqualityComparer<T> comparer)
	{
		return source.Aggregate((IEnumerable<T> current, IEnumerable<T> item) => current.Intersect(item, comparer));
	}

	/// <summary>
	/// 按字段属性判等取差集
	/// </summary>
	/// <typeparam name="TFirst">第一个集合的元素类型</typeparam>
	/// <typeparam name="TSecond">第二个集合的元素类型</typeparam>
	/// <param name="first">第一个集合</param>
	/// <param name="second">第二个集合</param>
	/// <param name="condition">用于判断两个元素是否相等的条件</param>
	/// <returns>返回第一个集合中不在第二个集合中的元素</returns>
	public static IEnumerable<TFirst> ExceptBy<TFirst, TSecond>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second, Func<TFirst, TSecond, bool> condition)
	{
		return first.Where((TFirst f) => !second.Any((TSecond s) => condition(f, s)));
	}

	/// <summary>
	/// 按字段去重
	/// </summary>
	/// <typeparam name="TSource">集合的元素类型</typeparam>
	/// <typeparam name="TKey">键的选择器返回的键类型</typeparam>
	/// <param name="source">集合</param>
	/// <param name="keySelector">用于从每个元素中提取键的函数</param>
	/// <returns>返回去重后的集合</returns>
	public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(keySelector, "keySelector");
		HashSet<TKey> set = new HashSet<TKey>();
		return source.Where((TSource item) => set.Add(keySelector(item)));
	}

	/// <summary>
	/// 按字段属性判等取交集
	/// </summary>
	/// <typeparam name="TSource">集合的元素类型</typeparam>
	/// <typeparam name="TKey">键的选择器返回的键类型</typeparam>
	/// <param name="first">第一个集合</param>
	/// <param name="second">第二个集合</param>
	/// <param name="keySelector">用于从每个元素中提取键的函数</param>
	/// <returns>返回两个集合中具有相同键的交集元素</returns>
	public static IEnumerable<TSource> IntersectBy<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TKey> second, Func<TSource, TKey> keySelector)
	{
		return first.IntersectBy(second, keySelector, null);
	}

	/// <summary>
	/// 按字段属性判等取交集
	/// </summary>
	/// <typeparam name="TSource">集合的元素类型</typeparam>
	/// <typeparam name="TKey">键的选择器返回的键类型</typeparam>
	/// <param name="first">第一个集合</param>
	/// <param name="second">第二个集合</param>
	/// <param name="keySelector">用于从每个元素中提取键的函数</param>
	/// <param name="comparer">用于比较键的比较器</param>
	/// <returns>返回两个集合中具有相同键的交集元素</returns>
	public static IEnumerable<TSource> IntersectBy<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TKey> second, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
	{
		ArgumentNullException.ThrowIfNull(first, "first");
		ArgumentNullException.ThrowIfNull(second, "second");
		ArgumentNullException.ThrowIfNull(keySelector, "keySelector");
		return IntersectByIterator(first, second, keySelector, comparer);
	}

	private static IEnumerable<TSource> IntersectByIterator<TSource, TKey>(IEnumerable<TSource> first, IEnumerable<TKey> second, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
	{
		HashSet<TKey> set = new HashSet<TKey>(second, comparer);
		return first.Where((TSource source) => set.Remove(keySelector(source)));
	}

	/// <summary>
	/// 按字段属性判等取差集
	/// </summary>
	/// <typeparam name="TSource">源集合中的元素类型</typeparam>
	/// <typeparam name="TKey">键的选择器返回的类型</typeparam>
	/// <param name="first">第一个集合</param>
	/// <param name="second">第二个集合</param>
	/// <param name="keySelector">从源集合中的每个元素中提取键的函数</param>
	/// <returns>一个包含第一个集合中存在但第二个集合中不存在的元素的新集合</returns>
	public static IEnumerable<TSource> ExceptBy<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TKey> second, Func<TSource, TKey> keySelector)
	{
		return first.ExceptBy(second, keySelector, null);
	}

	/// <summary>
	/// 按字段属性判等取差集
	/// </summary>
	/// <typeparam name="TSource">源集合中的元素类型</typeparam>
	/// <typeparam name="TKey">键的选择器返回的类型</typeparam>
	/// <param name="first">第一个集合</param>
	/// <param name="second">第二个集合</param>
	/// <param name="keySelector">从源集合中的每个元素中提取键的函数</param>
	/// <param name="comparer">用于比较键的相等比较器，如果为 null，则使用默认的相等比较器</param>
	/// <returns>一个包含第一个集合中存在但第二个集合中不存在的元素的新集合</returns>
	/// <exception cref="T:System.ArgumentNullException">如果 first、second 或 keySelector 为 null，则抛出此异常</exception>
	public static IEnumerable<TSource> ExceptBy<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TKey> second, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
	{
		ArgumentNullException.ThrowIfNull(first, "first");
		ArgumentNullException.ThrowIfNull(second, "second");
		ArgumentNullException.ThrowIfNull(keySelector, "keySelector");
		return ExceptByIterator(first, second, keySelector, comparer);
	}

	private static IEnumerable<TSource> ExceptByIterator<TSource, TKey>(IEnumerable<TSource> first, IEnumerable<TKey> second, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
	{
		HashSet<TKey> set = new HashSet<TKey>(second, comparer);
		return first.Where((TSource source) => set.Add(keySelector(source)));
	}

	/// <summary>
	/// 向集合中添加多个元素
	/// </summary>
	/// <typeparam name="T">集合中的元素类型</typeparam>
	/// <param name="slef">要添加元素的集合</param>
	/// <param name="values">要添加的元素数组</param>
	public static void AddRange<T>(this ICollection<T> slef, params T[] values)
	{
		foreach (T item in values)
		{
			slef.Add(item);
		}
	}

	/// <summary>
	/// 向集合中添加多个元素
	/// </summary>
	/// <typeparam name="T">集合中的元素类型</typeparam>
	/// <param name="self">要添加元素的集合</param>
	/// <param name="values">要添加的元素集合</param>
	public static void AddRange<T>(this ICollection<T> self, IEnumerable<T> values)
	{
		foreach (T value in values)
		{
			self.Add(value);
		}
	}

	/// <summary>
	/// 向并发袋中添加多个元素
	/// </summary>
	/// <typeparam name="T">并发袋中的元素类型</typeparam>
	/// <param name="self">要添加元素的并发袋</param>
	/// <param name="values">要添加的元素数组</param>
	public static void AddRange<T>(this ConcurrentBag<T> self, params T[] values)
	{
		foreach (T item in values)
		{
			self.Add(item);
		}
	}

	/// <summary>
	/// 向并发队列中添加多个元素
	/// </summary>
	/// <typeparam name="T">并发队列中的元素类型</typeparam>
	/// <param name="self">要添加元素的并发队列</param>
	/// <param name="values">要添加的元素数组</param>
	public static void AddRange<T>(this ConcurrentQueue<T> self, params T[] values)
	{
		foreach (T item in values)
		{
			self.Enqueue(item);
		}
	}

	/// <summary>
	/// 向集合中添加符合条件的多个元素
	/// </summary>
	/// <typeparam name="T">集合中的元素类型</typeparam>
	/// <param name="self">要添加元素的集合</param>
	/// <param name="predicate">用于筛选元素的条件函数</param>
	/// <param name="values">要添加的元素数组</param>
	public static void AddRangeIf<T>(this ICollection<T> self, Func<T, bool> predicate, params T[] values)
	{
		foreach (T item in values.Where(predicate))
		{
			self.Add(item);
		}
	}

	/// <summary>
	/// 向并发袋中添加符合条件的多个元素
	/// </summary>
	/// <typeparam name="T">并发袋中的元素类型</typeparam>
	/// <param name="self">要添加元素的并发袋</param>
	/// <param name="predicate">用于筛选元素的条件函数</param>
	/// <param name="values">要添加的元素数组</param>
	public static void AddRangeIf<T>(this ConcurrentBag<T> self, Func<T, bool> predicate, params T[] values)
	{
		foreach (T item in values.Where(predicate))
		{
			self.Add(item);
		}
	}

	/// <summary>
	/// 向并发队列中添加符合条件的多个元素
	/// </summary>
	/// <typeparam name="T">并发队列中的元素类型</typeparam>
	/// <param name="self">要添加元素的并发队列</param>
	/// <param name="predicate">用于筛选元素的条件函数</param>
	/// <param name="values">要添加的元素数组</param>
	public static void AddRangeIf<T>(this ConcurrentQueue<T> self, Func<T, bool> predicate, params T[] values)
	{
		foreach (T item in values.Where(predicate))
		{
			self.Enqueue(item);
		}
	}

	/// <summary>
	/// 向集合中添加不重复的元素
	/// </summary>
	/// <typeparam name="T">集合中的元素类型</typeparam>
	/// <param name="self">要添加元素的集合</param>
	/// <param name="values">要添加的元素数组</param>
	public static void AddRangeIfNotContains<T>(this ICollection<T> self, params T[] values)
	{
		foreach (T item in values)
		{
			if (!self.Contains(item))
			{
				self.Add(item);
			}
		}
	}

	/// <summary>
	/// 从集合中移除符合条件的元素
	/// </summary>
	/// <typeparam name="T">集合中的元素类型</typeparam>
	/// <param name="self">要移除元素的集合</param>
	/// <param name="where">用于筛选要移除的元素的条件函数</param>
	public static void RemoveWhere<T>(this ICollection<T> self, Func<T, bool> where)
	{
		foreach (T item in self.Where(where).ToList())
		{
			self.Remove(item);
		}
	}

	/// <summary>
	/// 在满足条件的元素之后添加元素
	/// </summary>
	/// <typeparam name="T">集合中的元素类型</typeparam>
	/// <param name="self">当前集合</param>
	/// <param name="condition">判断条件</param>
	/// <param name="value">要插入的值</param>
	public static void InsertAfter<T>(this IList<T> self, Func<T, bool> condition, T value)
	{
		foreach (int item in from p in self.Select((T item, int index) => new { item, index })
			where condition(p.item)
			orderby p.index descending
			select p into t
			select t.index)
		{
			if (item + 1 == self.Count)
			{
				self.Add(value);
			}
			else
			{
				self.Insert(item + 1, value);
			}
		}
	}

	/// <summary>
	/// 在指定索引位置之后添加元素
	/// </summary>
	/// <typeparam name="T">集合中的元素类型</typeparam>
	/// <param name="list">当前集合</param>
	/// <param name="index">索引位置</param>
	/// <param name="value">要插入的值</param>
	public static void InsertAfter<T>(this IList<T> list, int index, T value)
	{
		foreach (int item in from p in list.Select((T v, int i) => new
			{
				Value = v,
				Index = i
			})
			where p.Index == index
			orderby p.Index descending
			select p into t
			select t.Index)
		{
			if (item + 1 == list.Count)
			{
				list.Add(value);
			}
			else
			{
				list.Insert(item + 1, value);
			}
		}
	}

	/// <summary>
	/// 将集合转换为HashSet
	/// </summary>
	/// <typeparam name="T">源集合中的元素类型</typeparam>
	/// <typeparam name="TResult">目标HashSet中的元素类型</typeparam>
	/// <param name="source">源集合</param>
	/// <param name="selector">选择器函数</param>
	/// <returns>转换后的HashSet</returns>
	public static HashSet<TResult> ToHashSet<T, TResult>(this IEnumerable<T> source, Func<T, TResult> selector)
	{
		return new HashSet<TResult>(source.Select(selector));
	}

	/// <summary>
	/// 遍历IEnumerable集合，并对每个元素执行操作
	/// </summary>
	/// <typeparam name="T">集合中的元素类型</typeparam>
	/// <param name="self">当前集合</param>
	/// <param name="action">要执行的操作</param>
	public static void ForEach<T>(this IEnumerable<T> self, Action<T> action)
	{
		foreach (T item in self)
		{
			action(item);
		}
	}

	/// <summary>
	/// 异步遍历IEnumerable集合，并对每个元素执行异步操作
	/// </summary>
	/// <typeparam name="T">集合中的元素类型</typeparam>
	/// <param name="source">当前集合</param>
	/// <param name="maxParallelCount">最大并行数</param>
	/// <param name="action">要执行的异步操作</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>任务对象</returns>
	public static async Task ForeachAsync<T>(this IEnumerable<T> source, Func<T, Task> action, int maxParallelCount, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (Debugger.IsAttached)
		{
			foreach (T item in source)
			{
				await action(item);
			}
			return;
		}
		List<Task> list = new List<Task>();
		foreach (T item2 in source)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return;
			}
			list.Add(action(item2));
			if (list.Count((Task t) => !t.IsCompleted) >= maxParallelCount)
			{
				await Task.WhenAny(list);
				list.RemoveAll((Task t) => t.IsCompleted);
			}
		}
		await Task.WhenAll(list);
	}

	/// <summary>
	/// 异步遍历IEnumerable集合，并对每个元素执行异步操作
	/// </summary>
	/// <typeparam name="T">集合中的元素类型</typeparam>
	/// <param name="source">当前集合</param>
	/// <param name="action">要执行的异步操作</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>任务对象</returns>
	public static Task ForeachAsync<T>(this IEnumerable<T> source, Func<T, Task> action, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (source is ICollection<T> collection)
		{
			return collection.ForeachAsync(action, collection.Count, cancellationToken);
		}
		List<T> list = source.ToList();
		return list.ForeachAsync(action, list.Count, cancellationToken);
	}

	/// <summary>
	/// 异步选择集合中的每个元素，并返回结果数组
	/// </summary>
	/// <typeparam name="T">源集合中的元素类型</typeparam>
	/// <typeparam name="TResult">结果数组中的元素类型</typeparam>
	/// <param name="source">源集合</param>
	/// <param name="selector">选择器函数</param>
	/// <returns>结果数组</returns>
	public static Task<TResult[]> SelectAsync<T, TResult>(this IEnumerable<T> source, Func<T, Task<TResult>> selector)
	{
		return Task.WhenAll(source.Select(selector));
	}

	/// <summary>
	/// 异步选择集合中的每个元素，并返回结果数组
	/// </summary>
	/// <typeparam name="T">源集合中的元素类型</typeparam>
	/// <typeparam name="TResult">结果数组中的元素类型</typeparam>
	/// <param name="source">源集合</param>
	/// <param name="selector">选择器函数</param>
	/// <returns>结果数组</returns>
	public static Task<TResult[]> SelectAsync<T, TResult>(this IEnumerable<T> source, Func<T, int, Task<TResult>> selector)
	{
		return Task.WhenAll(source.Select(selector));
	}

	/// <summary>
	/// 异步选择集合中的每个元素，并返回结果列表
	/// </summary>
	/// <typeparam name="T">源集合中的元素类型</typeparam>
	/// <typeparam name="TResult">结果列表中的元素类型</typeparam>
	/// <param name="source">源集合</param>
	/// <param name="selector">选择器函数</param>
	/// <param name="maxParallelCount">最大并行数</param>
	/// <returns>结果列表</returns>
	public static async Task<List<TResult>> SelectAsync<T, TResult>(this IEnumerable<T> source, Func<T, Task<TResult>> selector, int maxParallelCount)
	{
		List<TResult> results = new List<TResult>();
		List<Task<TResult>> tasks = new List<Task<TResult>>();
		foreach (T item2 in source)
		{
			Task<TResult> item = selector(item2);
			tasks.Add(item);
			if (tasks.Count >= maxParallelCount)
			{
				await Task.WhenAny(tasks);
				Task<TResult>[] completedTasks = tasks.Where((Task<TResult> t) => t.IsCompleted).ToArray();
				results.AddRange(completedTasks.Select((Task<TResult> t) => t.Result));
				tasks.RemoveWhere((Task<TResult> t) => completedTasks.Contains(t));
			}
		}
		List<TResult> list = results;
		list.AddRange(await Task.WhenAll(tasks));
		return results;
	}

	/// <summary>
	/// 异步选择集合中的每个元素，并返回结果列表
	/// </summary>
	/// <typeparam name="T">源集合中的元素类型</typeparam>
	/// <typeparam name="TResult">结果列表中的元素类型</typeparam>
	/// <param name="source">源集合</param>
	/// <param name="selector">选择器函数</param>
	/// <param name="maxParallelCount">最大并行数</param>
	/// <returns>结果列表</returns>
	public static async Task<List<TResult>> SelectAsync<T, TResult>(this IEnumerable<T> source, Func<T, int, Task<TResult>> selector, int maxParallelCount)
	{
		List<TResult> results = new List<TResult>();
		List<Task<TResult>> tasks = new List<Task<TResult>>();
		int index = 0;
		foreach (T item2 in source)
		{
			Task<TResult> item = selector(item2, index);
			tasks.Add(item);
			Interlocked.Add(ref index, 1);
			if (tasks.Count >= maxParallelCount)
			{
				await Task.WhenAny(tasks);
				Task<TResult>[] completedTasks = tasks.Where((Task<TResult> t) => t.IsCompleted).ToArray();
				results.AddRange(completedTasks.Select((Task<TResult> t) => t.Result));
				tasks.RemoveWhere((Task<TResult> t) => completedTasks.Contains(t));
			}
		}
		List<TResult> list = results;
		list.AddRange(await Task.WhenAll(tasks));
		return results;
	}

	/// <summary>
	/// 异步遍历集合中的每个元素，并执行指定的操作。
	/// </summary>
	/// <typeparam name="T">集合中元素的类型。</typeparam>
	/// <param name="source">要遍历的集合。</param>
	/// <param name="selector">要对每个元素执行的操作，返回一个任务。</param>
	/// <param name="maxParallelCount">最大并行数。</param>
	/// <param name="cancellationToken">取消令牌，用于取消操作。</param>
	/// <returns>一个任务，表示异步操作。</returns>
	public static async Task ForAsync<T>(this IEnumerable<T> source, Func<T, int, Task> selector, int maxParallelCount, CancellationToken cancellationToken = default(CancellationToken))
	{
		int index = 0;
		if (Debugger.IsAttached)
		{
			foreach (T item in source)
			{
				await selector(item, index);
				index++;
			}
			return;
		}
		List<Task> list = new List<Task>();
		foreach (T item2 in source)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return;
			}
			list.Add(selector(item2, index));
			Interlocked.Add(ref index, 1);
			if (list.Count >= maxParallelCount)
			{
				await Task.WhenAny(list);
				list.RemoveAll((Task t) => t.IsCompleted);
			}
		}
		await Task.WhenAll(list);
	}

	/// <summary>
	/// 异步遍历集合中的每个元素，并执行指定的操作。
	/// </summary>
	/// <typeparam name="T">集合中元素的类型。</typeparam>
	/// <param name="source">要遍历的集合。</param>
	/// <param name="selector">要对每个元素执行的操作，返回一个任务。</param>
	/// <param name="cancellationToken">取消令牌，用于取消操作。</param>
	/// <returns>一个任务，表示异步操作。</returns>
	public static Task ForAsync<T>(this IEnumerable<T> source, Func<T, int, Task> selector, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (source is ICollection<T> collection)
		{
			return collection.ForAsync(selector, collection.Count, cancellationToken);
		}
		List<T> list = source.ToList();
		return list.ForAsync(selector, list.Count, cancellationToken);
	}

	/// <summary>
	/// 获取集合中的最大值，如果集合为空则返回默认值。
	/// </summary>
	/// <typeparam name="TSource">集合中元素的类型。</typeparam>
	/// <typeparam name="TResult">结果的类型。</typeparam>
	/// <param name="source">要查询的集合。</param>
	/// <param name="selector">用于从每个元素中提取值的函数。</param>
	/// <returns>集合中的最大值，如果集合为空则返回默认值。</returns>
	public static TResult MaxOrDefault<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector)
	{
		return source.Select(selector).DefaultIfEmpty().Max();
	}

	/// <summary>
	/// 获取集合中的最大值，如果集合为空则返回指定的默认值。
	/// </summary>
	/// <typeparam name="TSource">集合中元素的类型。</typeparam>
	/// <typeparam name="TResult">结果的类型。</typeparam>
	/// <param name="source">要查询的集合。</param>
	/// <param name="selector">用于从每个元素中提取值的函数。</param>
	/// <param name="defaultValue">集合为空时返回的默认值。</param>
	/// <returns>集合中的最大值，如果集合为空则返回指定的默认值。</returns>
	public static TResult MaxOrDefault<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector, TResult defaultValue)
	{
		return source.Select(selector).DefaultIfEmpty(defaultValue).Max();
	}

	/// <summary>
	/// 获取集合中的最大值，如果集合为空则返回默认值。
	/// </summary>
	/// <typeparam name="TSource">集合中元素的类型。</typeparam>
	/// <param name="source">要查询的集合。</param>
	/// <returns>集合中的最大值，如果集合为空则返回默认值。</returns>
	public static TSource MaxOrDefault<TSource>(this IQueryable<TSource> source)
	{
		return source.DefaultIfEmpty().Max();
	}

	/// <summary>
	/// 获取集合中的最大值，如果集合为空则返回指定的默认值。
	/// </summary>
	/// <typeparam name="TSource">集合中元素的类型。</typeparam>
	/// <param name="source">要查询的集合。</param>
	/// <param name="defaultValue">集合为空时返回的默认值。</param>
	/// <returns>集合中的最大值，如果集合为空则返回指定的默认值。</returns>
	public static TSource MaxOrDefault<TSource>(this IQueryable<TSource> source, TSource defaultValue)
	{
		return source.DefaultIfEmpty(defaultValue).Max();
	}

	/// <summary>
	/// 获取集合中的最大值，如果集合为空则返回指定的默认值。
	/// </summary>
	/// <typeparam name="TSource">集合中元素的类型。</typeparam>
	/// <typeparam name="TResult">结果的类型。</typeparam>
	/// <param name="source">要查询的集合。</param>
	/// <param name="selector">用于从每个元素中提取值的函数。</param>
	/// <param name="defaultValue">集合为空时返回的默认值。</param>
	/// <returns>集合中的最大值，如果集合为空则返回指定的默认值。</returns>
	public static TResult MaxOrDefault<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector, TResult defaultValue)
	{
		return source.Select(selector).DefaultIfEmpty(defaultValue).Max();
	}

	/// <summary>
	/// 获取集合中的最大值，如果集合为空则返回默认值。
	/// </summary>
	/// <typeparam name="TSource">集合中元素的类型。</typeparam>
	/// <typeparam name="TResult">结果的类型。</typeparam>
	/// <param name="source">要查询的集合。</param>
	/// <param name="selector">用于从每个元素中提取值的函数。</param>
	/// <returns>集合中的最大值，如果集合为空则返回默认值。</returns>
	public static TResult MaxOrDefault<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
	{
		return source.Select(selector).DefaultIfEmpty().Max();
	}

	/// <summary>
	/// 获取集合中的最大值，如果集合为空则返回默认值。
	/// </summary>
	/// <typeparam name="TSource">集合中元素的类型。</typeparam>
	/// <param name="source">要查询的集合。</param>
	/// <returns>集合中的最大值，如果集合为空则返回默认值。</returns>
	public static TSource MaxOrDefault<TSource>(this IEnumerable<TSource> source)
	{
		return source.DefaultIfEmpty().Max();
	}

	/// <summary>
	/// 获取集合中的最大值，如果集合为空则返回指定的默认值。
	/// </summary>
	/// <typeparam name="TSource">集合中元素的类型。</typeparam>
	/// <param name="source">要查询的集合。</param>
	/// <param name="defaultValue">集合为空时返回的默认值。</param>
	/// <returns>集合中的最大值，如果集合为空则返回指定的默认值。</returns>
	public static TSource MaxOrDefault<TSource>(this IEnumerable<TSource> source, TSource defaultValue)
	{
		return source.DefaultIfEmpty(defaultValue).Max();
	}

	/// <summary>
	/// 获取集合中的最小值，如果集合为空则返回默认值。
	/// </summary>
	/// <typeparam name="TSource">集合中元素的类型。</typeparam>
	/// <typeparam name="TResult">结果的类型。</typeparam>
	/// <param name="source">要查询的集合。</param>
	/// <param name="selector">用于从每个元素中提取值的函数。</param>
	/// <returns>集合中的最小值，如果集合为空则返回默认值。</returns>
	public static TResult MinOrDefault<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector)
	{
		return source.Select(selector).DefaultIfEmpty().Min();
	}

	/// <summary>
	/// 获取集合中的最小值，如果集合为空则返回指定的默认值。
	/// </summary>
	/// <typeparam name="TSource">集合中元素的类型。</typeparam>
	/// <typeparam name="TResult">结果的类型。</typeparam>
	/// <param name="source">要查询的集合。</param>
	/// <param name="selector">用于从每个元素中提取值的函数。</param>
	/// <param name="defaultValue">集合为空时返回的默认值。</param>
	/// <returns>集合中的最小值，如果集合为空则返回指定的默认值。</returns>
	public static TResult MinOrDefault<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector, TResult defaultValue)
	{
		return source.Select(selector).DefaultIfEmpty(defaultValue).Min();
	}

	/// <summary>
	/// 获取集合中的最小值，如果集合为空则返回默认值。
	/// </summary>
	/// <typeparam name="TSource">集合中元素的类型。</typeparam>
	/// <param name="source">要查询的集合。</param>
	/// <returns>集合中的最小值，如果集合为空则返回默认值。</returns>
	public static TSource MinOrDefault<TSource>(this IQueryable<TSource> source)
	{
		return source.DefaultIfEmpty().Min();
	}

	/// <summary>
	/// 获取集合中的最小值，如果集合为空则返回指定的默认值。
	/// </summary>
	/// <typeparam name="TSource">集合中元素的类型。</typeparam>
	/// <param name="source">要查询的集合。</param>
	/// <param name="defaultValue">集合为空时返回的默认值。</param>
	/// <returns>集合中的最小值，如果集合为空则返回指定的默认值。</returns>
	public static TSource MinOrDefault<TSource>(this IQueryable<TSource> source, TSource defaultValue)
	{
		return source.DefaultIfEmpty(defaultValue).Min();
	}

	/// <summary>
	/// 获取集合中的最小值，如果集合为空则返回默认值。
	/// </summary>
	/// <typeparam name="TSource">集合中元素的类型。</typeparam>
	/// <typeparam name="TResult">结果的类型。</typeparam>
	/// <param name="source">要查询的集合。</param>
	/// <param name="selector">用于从每个元素中提取值的函数。</param>
	/// <returns>集合中的最小值，如果集合为空则返回默认值。</returns>
	public static TResult MinOrDefault<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
	{
		return source.Select(selector).DefaultIfEmpty().Min();
	}

	/// <summary>
	/// 获取集合中的最小值，如果集合为空则返回指定的默认值。
	/// </summary>
	/// <typeparam name="TSource">集合中元素的类型。</typeparam>
	/// <typeparam name="TResult">结果的类型。</typeparam>
	/// <param name="source">要查询的集合。</param>
	/// <param name="selector">用于从每个元素中提取值的函数。</param>
	/// <param name="defaultValue">集合为空时返回的默认值。</param>
	/// <returns>集合中的最小值，如果集合为空则返回指定的默认值。</returns>
	public static TResult MinOrDefault<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector, TResult defaultValue)
	{
		return source.Select(selector).DefaultIfEmpty(defaultValue).Min();
	}

	/// <summary>
	/// 获取序列中的最小值，如果序列为空，则返回默认值。
	/// </summary>
	/// <typeparam name="TSource">序列中元素的类型。</typeparam>
	/// <param name="source">要从中获取最小值的序列。</param>
	/// <returns>序列中的最小值，如果序列为空则返回默认值。</returns>
	public static TSource MinOrDefault<TSource>(this IEnumerable<TSource> source)
	{
		return source.DefaultIfEmpty().Min();
	}

	/// <summary>
	/// 获取序列中的最小值，如果序列为空，则返回指定的默认值。
	/// </summary>
	/// <typeparam name="TSource">序列中元素的类型。</typeparam>
	/// <param name="source">要从中获取最小值的序列。</param>
	/// <param name="defaultValue">如果序列为空时返回的默认值。</param>
	/// <returns>序列中的最小值，如果序列为空则返回指定的默认值。</returns>
	public static TSource MinOrDefault<TSource>(this IEnumerable<TSource> source, TSource defaultValue)
	{
		return source.DefaultIfEmpty(defaultValue).Min();
	}

	/// <summary>
	/// 计算序列的标准差。
	/// </summary>
	/// <param name="source">要计算标准差的双精度浮点数序列。</param>
	/// <returns>序列的标准差。</returns>
	public static double StandardDeviation(this IEnumerable<double> source)
	{
		double result = 0.0;
		ICollection<double> collection = (source as ICollection<double>) ?? source.ToList();
		int count = collection.Count;
		if (count > 1)
		{
			double avg = collection.Average();
			result = System.Math.Sqrt(collection.Sum((double d) => (d - avg) * (d - avg)) / (double)count);
		}
		return result;
	}

	/// <summary>
	/// 按随机顺序对序列进行排序。
	/// </summary>
	/// <typeparam name="T">序列中元素的类型。</typeparam>
	/// <param name="source">要排序的序列。</param>
	/// <returns>按随机顺序排序后的序列。</returns>
	public static IOrderedEnumerable<T> OrderByRandom<T>(this IEnumerable<T> source)
	{
		return source.OrderBy((T _) => Guid.NewGuid());
	}

	/// <summary>
	/// 判断两个序列是否相等，使用自定义的比较条件。
	/// </summary>
	/// <typeparam name="T">序列中元素的类型。</typeparam>
	/// <param name="first">第一个序列。</param>
	/// <param name="second">第二个序列。</param>
	/// <param name="condition">用于比较两个元素的条件。</param>
	/// <returns>如果两个序列相等则返回 true，否则返回 false。</returns>
	public static bool SequenceEqual<T>(this IEnumerable<T> first, IEnumerable<T> second, Func<T, T, bool> condition)
	{
		if (first is ICollection<T> collection && second is ICollection<T> collection2)
		{
			if (collection.Count != collection2.Count)
			{
				return false;
			}
			if (collection is IList<T> list && collection2 is IList<T> list2)
			{
				int count = collection.Count;
				for (int i = 0; i < count; i++)
				{
					if (!condition(list[i], list2[i]))
					{
						return false;
					}
				}
				return true;
			}
		}
		using IEnumerator<T> enumerator = first.GetEnumerator();
		using IEnumerator<T> enumerator2 = second.GetEnumerator();
		while (enumerator.MoveNext())
		{
			if (!enumerator2.MoveNext() || !condition(enumerator.Current, enumerator2.Current))
			{
				return false;
			}
		}
		return !enumerator2.MoveNext();
	}

	/// <summary>
	/// 判断两个不同类型的序列是否相等，使用自定义的比较条件。
	/// </summary>
	/// <typeparam name="T1">第一个序列中元素的类型。</typeparam>
	/// <typeparam name="T2">第二个序列中元素的类型。</typeparam>
	/// <param name="first">第一个序列。</param>
	/// <param name="second">第二个序列。</param>
	/// <param name="condition">用于比较两个元素的条件。</param>
	/// <returns>如果两个序列相等则返回 true，否则返回 false。</returns>
	public static bool SequenceEqual<T1, T2>(this IEnumerable<T1> first, IEnumerable<T2> second, Func<T1, T2, bool> condition)
	{
		if (first is ICollection<T1> collection && second is ICollection<T2> collection2)
		{
			if (collection.Count != collection2.Count)
			{
				return false;
			}
			if (collection is IList<T1> list && collection2 is IList<T2> list2)
			{
				int count = collection.Count;
				for (int i = 0; i < count; i++)
				{
					if (!condition(list[i], list2[i]))
					{
						return false;
					}
				}
				return true;
			}
		}
		using IEnumerator<T1> enumerator = first.GetEnumerator();
		using IEnumerator<T2> enumerator2 = second.GetEnumerator();
		while (enumerator.MoveNext())
		{
			if (!enumerator2.MoveNext() || !condition(enumerator.Current, enumerator2.Current))
			{
				return false;
			}
		}
		return !enumerator2.MoveNext();
	}

	/// <summary>
	/// 对比两个集合，找出新增的、删除的和修改的项。
	/// </summary>
	/// <typeparam name="T1">第一个集合中元素的类型。</typeparam>
	/// <typeparam name="T2">第二个集合中元素的类型。</typeparam>
	/// <param name="first">第一个集合。</param>
	/// <param name="second">第二个集合。</param>
	/// <param name="condition">用于比较两个元素的条件。</param>
	/// <returns>一个元组，包含新增的项、删除的项和修改的项。</returns>
	public static (List<T1> adds, List<T2> remove, List<T1> updates) CompareChanges<T1, T2>(this IEnumerable<T1> first, IEnumerable<T2> second, Func<T1, T2, bool> condition)
	{
		if (first == null)
		{
			first = new List<T1>();
		}
		if (second == null)
		{
			second = new List<T2>();
		}
		ICollection<T1> collection = (first as ICollection<T1>) ?? first.ToList();
		ICollection<T2> collection2 = (second as ICollection<T2>) ?? second.ToList();
		List<T1> item = collection.ExceptBy(collection2, condition).ToList();
		List<T2> item2 = collection2.ExceptBy(collection, (T2 s, T1 f) => condition(f, s)).ToList();
		List<T1> item3 = collection.IntersectBy(collection2, condition).ToList();
		return (adds: item, remove: item2, updates: item3);
	}

	/// <summary>
	/// 对比两个集合，找出新增的、删除的和修改的项，并返回修改项的详细信息。
	/// </summary>
	/// <typeparam name="T1">第一个集合中元素的类型。</typeparam>
	/// <typeparam name="T2">第二个集合中元素的类型。</typeparam>
	/// <param name="first">第一个集合。</param>
	/// <param name="second">第二个集合。</param>
	/// <param name="condition">用于比较两个元素的条件。</param>
	/// <returns>一个元组，包含新增的项、删除的项和修改项的详细信息。</returns>
	public static (List<T1> adds, List<T2> remove, List<(T1 first, T2 second)> updates) CompareChangesPlus<T1, T2>(this IEnumerable<T1> first, IEnumerable<T2> second, Func<T1, T2, bool> condition)
	{
		if (first == null)
		{
			first = new List<T1>();
		}
		if (second == null)
		{
			second = new List<T2>();
		}
		ICollection<T1> collection = (first as ICollection<T1>) ?? first.ToList();
		ICollection<T2> secondSource = (second as ICollection<T2>) ?? second.ToList();
		List<T1> item = collection.ExceptBy(secondSource, condition).ToList();
		List<T2> item2 = secondSource.ExceptBy(collection, (T2 s, T1 f) => condition(f, s)).ToList();
		List<(T1, T2)> item3 = (from t1 in collection.IntersectBy(secondSource, condition)
			select (t1: t1, secondSource.FirstOrDefault((T2 t2) => condition(t1, t2)))).ToList();
		return (adds: item, remove: item2, updates: item3);
	}

	/// <summary>
	/// 将列表声明为非空列表，如果列表为 null，则返回一个新的空列表。
	/// </summary>
	/// <typeparam name="T">列表中元素的类型。</typeparam>
	/// <param name="list">要检查的列表。</param>
	/// <returns>非空列表。</returns>
	public static List<T> AsNotNull<T>(this List<T> list)
	{
		return list ?? new List<T>();
	}

	/// <summary>
	/// 将集合声明为非空集合，如果集合为 null，则返回一个新的空集合。
	/// </summary>
	/// <typeparam name="T">集合中元素的类型。</typeparam>
	/// <param name="list">要检查的集合。</param>
	/// <returns>非空集合。</returns>
	public static IEnumerable<T> AsNotNull<T>(this IEnumerable<T> list)
	{
		return list ?? new List<T>();
	}

	/// <summary>
	/// 如果满足条件，则执行筛选操作。
	/// </summary>
	/// <typeparam name="T">集合中元素的类型。</typeparam>
	/// <param name="source">要筛选的集合。</param>
	/// <param name="condition">决定是否执行筛选的布尔条件。</param>
	/// <param name="where">用于筛选的条件表达式。</param>
	/// <returns>筛选后的集合，如果条件不满足则返回原集合。</returns>
	public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> source, bool condition, Func<T, bool> where)
	{
		if (!condition)
		{
			return source;
		}
		return source.Where(where);
	}

	/// <summary>
	/// 如果满足条件，则执行筛选操作。
	/// </summary>
	/// <typeparam name="T">集合中元素的类型。</typeparam>
	/// <param name="source">要筛选的集合。</param>
	/// <param name="condition">决定是否执行筛选的布尔条件表达式。</param>
	/// <param name="where">用于筛选的条件表达式。</param>
	/// <returns>筛选后的集合，如果条件不满足则返回原集合。</returns>
	public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> source, Func<bool> condition, Func<T, bool> where)
	{
		if (!condition())
		{
			return source;
		}
		return source.Where(where);
	}

	/// <summary>
	/// 如果满足条件，则执行筛选操作。
	/// </summary>
	/// <typeparam name="T">集合中元素的类型。</typeparam>
	/// <param name="source">要筛选的查询集合。</param>
	/// <param name="condition">决定是否执行筛选的布尔条件。</param>
	/// <param name="where">用于筛选的条件表达式。</param>
	/// <returns>筛选后的查询集合，如果条件不满足则返回原查询集合。</returns>
	public static IQueryable<T> WhereIf<T>(this IQueryable<T> source, bool condition, Expression<Func<T, bool>> where)
	{
		if (!condition)
		{
			return source;
		}
		return source.Where(where);
	}

	/// <summary>
	/// 如果满足条件，则执行筛选操作。
	/// </summary>
	/// <typeparam name="T">集合中元素的类型。</typeparam>
	/// <param name="source">要筛选的查询集合。</param>
	/// <param name="condition">决定是否执行筛选的布尔条件表达式。</param>
	/// <param name="where">用于筛选的条件表达式。</param>
	/// <returns>筛选后的查询集合，如果条件不满足则返回原查询集合。</returns>
	public static IQueryable<T> WhereIf<T>(this IQueryable<T> source, Func<bool> condition, Expression<Func<T, bool>> where)
	{
		if (!condition())
		{
			return source;
		}
		return source.Where(where);
	}

	/// <summary>
	/// 改变集合中指定元素的索引位置。
	/// </summary>
	/// <typeparam name="T">集合中元素的类型。</typeparam>
	/// <param name="list">要操作的集合。</param>
	/// <param name="item">要改变索引位置的元素。</param>
	/// <param name="index">新的索引位置。</param>
	/// <exception cref="T:System.ArgumentNullException">如果元素为 null，则抛出此异常。</exception>
	/// <returns>操作后的集合。</returns>
	public static IList<T> ChangeIndex<T>(this IList<T> list, T item, int index)
	{
		ArgumentNullException.ThrowIfNull(item, "item");
		ChangeIndexInternal(list, item, index);
		return list;
	}

	/// <summary>
	/// 改变集合中满足条件的元素的索引位置。
	/// </summary>
	/// <typeparam name="T">集合中元素的类型。</typeparam>
	/// <param name="list">要操作的集合。</param>
	/// <param name="condition">用于定位元素的条件表达式。</param>
	/// <param name="index">新的索引位置。</param>
	/// <returns>操作后的集合。</returns>
	public static IList<T> ChangeIndex<T>(this IList<T> list, Func<T, bool> condition, int index)
	{
		T val = list.FirstOrDefault(condition);
		if (val != null)
		{
			ChangeIndexInternal(list, val, index);
		}
		return list;
	}

	/// <summary>
	/// 内部方法，用于实际改变元素的索引位置。
	/// </summary>
	/// <typeparam name="T">集合中元素的类型。</typeparam>
	/// <param name="list">要操作的集合。</param>
	/// <param name="item">要改变索引位置的元素。</param>
	/// <param name="index">新的索引位置。</param>
	private static void ChangeIndexInternal<T>(IList<T> list, T item, int index)
	{
		index = System.Math.Max(0, index);
		index = System.Math.Min(list.Count - 1, index);
		list.Remove(item);
		list.Insert(index, item);
	}
}
