using System;
using System.Collections.Generic;
using System.Linq;

namespace FuFramework.Utility.Extensions;

/// <summary>
/// 值可被Dispose的并发字典类型
/// </summary>
/// <typeparam name="TKey">键的类型。</typeparam>
/// <typeparam name="TValue">值的类型，必须实现IDisposable接口。</typeparam>
public class DisposableConcurrentDictionary<TKey, TValue> : NullableConcurrentDictionary<TKey, TValue>, IDisposable where TValue : IDisposable
{
	private bool _isDisposed;

	/// <summary>
	/// 初始化一个新的 <see cref="T:FuFramework.Utility.Extensions.DisposableConcurrentDictionary`2" /> 实例。
	/// </summary>
	public DisposableConcurrentDictionary()
	{
	}

	/// <summary>
	/// 使用指定的默认值初始化一个新的 <see cref="T:FuFramework.Utility.Extensions.DisposableConcurrentDictionary`2" /> 实例。
	/// </summary>
	/// <param name="fallbackValue">当键不存在时返回的默认值。</param>
	public DisposableConcurrentDictionary(TValue fallbackValue)
	{
		base.FallbackValue = fallbackValue;
	}

	/// <summary>
	/// 使用指定的并发级别和初始容量初始化一个新的 <see cref="T:FuFramework.Utility.Extensions.DisposableConcurrentDictionary`2" /> 实例。
	/// </summary>
	/// <param name="concurrencyLevel">并发级别，即字典可以同时支持的线程数。</param>
	/// <param name="capacity">字典的初始容量。</param>
	public DisposableConcurrentDictionary(int concurrencyLevel, int capacity)
		: base(concurrencyLevel, capacity)
	{
	}

	/// <summary>
	/// 使用指定的比较器初始化一个新的 <see cref="T:FuFramework.Utility.Extensions.DisposableConcurrentDictionary`2" /> 实例。
	/// </summary>
	/// <param name="comparer">用于比较键的比较器。</param>
	public DisposableConcurrentDictionary(IEqualityComparer<NullObject<TKey>> comparer)
		: base(comparer)
	{
	}

	/// <summary>
	/// 释放资源。
	/// </summary>
	public void Dispose()
	{
		if (!_isDisposed)
		{
			Dispose(disposing: true);
			_isDisposed = true;
			GC.SuppressFinalize(this);
		}
	}

	/// <summary>
	/// 终结器，确保未释放的资源在对象被垃圾回收时被释放。
	/// </summary>
	~DisposableConcurrentDictionary()
	{
		Dispose(disposing: false);
	}

	/// <summary>
	/// 释放资源。
	/// </summary>
	/// <param name="disposing">指示是否应释放托管资源。</param>
	protected virtual void Dispose(bool disposing)
	{
		foreach (TValue item in base.Values.Where((TValue v) => v != null))
		{
			item.Dispose();
		}
	}
}
