using System;
using System.Collections.Generic;
using System.Threading;

namespace FuFramework.SuperSocket.ClientEngine;

public class ConcurrentBatchQueue<T> : IBatchQueue<T>
{
	private class Entity
	{
		public int Count;

		public T[] Array { get; set; }
	}

	private object m_Entity;

	private Entity m_BackEntity;

	private static readonly T m_Null;

	private Func<T, bool> m_NullValidator;

	public bool IsEmpty => Count <= 0;

	public int Count => ((Entity)m_Entity).Count;

	public ConcurrentBatchQueue()
		: this(16)
	{
	}

	public ConcurrentBatchQueue(int capacity)
		: this(new T[capacity])
	{
	}

	public ConcurrentBatchQueue(int capacity, Func<T, bool> nullValidator)
		: this(new T[capacity], nullValidator)
	{
	}

	public ConcurrentBatchQueue(T[] array)
		: this(array, (Func<T, bool>)((T t) => t == null))
	{
	}

	public ConcurrentBatchQueue(T[] array, Func<T, bool> nullValidator)
	{
		m_Entity = new Entity
		{
			Array = array
		};
		m_BackEntity = new Entity();
		m_BackEntity.Array = new T[array.Length];
		m_NullValidator = nullValidator;
	}

	public bool Enqueue(T item)
	{
		bool full;
		while (!TryEnqueue(item, out full) && !full)
		{
		}
		return !full;
	}

	private bool TryEnqueue(T item, out bool full)
	{
		full = false;
		Entity entity = m_Entity as Entity;
		T[] array = entity.Array;
		int count = entity.Count;
		if (count >= array.Length)
		{
			full = true;
			return false;
		}
		if (entity != m_Entity)
		{
			return false;
		}
		if (Interlocked.CompareExchange(ref entity.Count, count + 1, count) != count)
		{
			return false;
		}
		array[count] = item;
		return true;
	}

	public bool Enqueue(IList<T> items)
	{
		bool full;
		while (!TryEnqueue(items, out full) && !full)
		{
		}
		return !full;
	}

	private bool TryEnqueue(IList<T> items, out bool full)
	{
		full = false;
		Entity entity = m_Entity as Entity;
		T[] array = entity.Array;
		int count = entity.Count;
		int count2 = items.Count;
		int num = count + count2;
		if (num > array.Length)
		{
			full = true;
			return false;
		}
		if (entity != m_Entity)
		{
			return false;
		}
		if (Interlocked.CompareExchange(ref entity.Count, num, count) != count)
		{
			return false;
		}
		foreach (T item in items)
		{
			array[count++] = item;
		}
		return true;
	}

	public bool TryDequeue(IList<T> outputItems)
	{
		Entity entity = m_Entity as Entity;
		if (entity.Count <= 0)
		{
			return false;
		}
		if (Interlocked.CompareExchange(ref m_Entity, m_BackEntity, entity) != entity)
		{
			return false;
		}
		SpinWait spinWait = default(SpinWait);
		spinWait.SpinOnce();
		_ = entity.Count;
		T[] array = entity.Array;
		int num = 0;
		while (true)
		{
			T val = array[num];
			while (m_NullValidator(val))
			{
				spinWait.SpinOnce();
				val = array[num];
			}
			outputItems.Add(val);
			array[num] = m_Null;
			if (entity.Count <= num + 1)
			{
				break;
			}
			num++;
		}
		entity.Count = 0;
		m_BackEntity = entity;
		return true;
	}
}
