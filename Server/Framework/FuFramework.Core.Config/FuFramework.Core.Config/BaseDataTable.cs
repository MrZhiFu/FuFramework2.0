using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace FuFramework.Core.Config;

/// <summary>
/// 基础数据表
/// </summary>
/// <typeparam name="T">数据表中的数据类型</typeparam>
public abstract class BaseDataTable<T> : IDataTable<T>, IDataTable
{
	/// <summary>
	/// 异步加载器
	/// </summary>
	protected readonly Func<Task<JsonElement>> _loadFunc;

	/// <summary>
	/// 数据列表
	/// </summary>
	protected readonly List<T> DataList = new List<T>();

	/// <summary>
	/// 长整型键的数据表
	/// </summary>
	protected readonly SortedDictionary<long, T> LongDataMaps = new SortedDictionary<long, T>();

	/// <summary>
	/// 字符串键的数据表
	/// </summary>
	protected readonly SortedDictionary<string, T> StringDataMaps = new SortedDictionary<string, T>();

	/// <summary>
	/// 根据整型ID获取对象
	/// </summary>
	/// <param name="id">对象的ID</param>
	/// <returns>找到的对象，如果未找到则返回默认值</returns>
	public T this[int id] => Get(id);

	/// <summary>
	/// 根据长整型ID获取对象
	/// </summary>
	/// <param name="id">对象的ID</param>
	/// <returns>找到的对象，如果未找到则返回默认值</returns>
	public T this[long id] => Get(id);

	/// <summary>
	/// 根据字符串ID获取对象
	/// </summary>
	/// <param name="id">对象的ID</param>
	/// <returns>找到的对象，如果未找到则返回默认值</returns>
	public T this[string id] => Get(id);

	/// <summary>
	/// 获取数据表中对象的数量
	/// </summary>
	/// <returns>数据表中对象的数量</returns>
	public int Count => Math.Max(LongDataMaps.Count, StringDataMaps.Count);

	/// <summary>
	/// 获取数据表中第一个对象
	/// </summary>
	/// <returns>数据表中的第一个对象，如果数据表为空则返回默认值</returns>
	public T FirstOrDefault => DataList.FirstOrDefault((T m) => m != null);

	/// <summary>
	/// 获取数据表中最后一个对象
	/// </summary>
	/// <returns>数据表中的最后一个对象，如果数据表为空则返回默认值</returns>
	public T LastOrDefault => DataList.LastOrDefault((T m) => m != null);

	/// <summary>
	/// 获取数据表中所有对象
	/// </summary>
	/// <returns>数据表中的所有对象数组</returns>
	public T[] All => DataList.ToArray();

	/// <summary>
	/// 初始化基础数据表
	/// </summary>
	/// <param name="loadFunc">异步加载数据的委托</param>
	public BaseDataTable(Func<Task<JsonElement>> loadFunc)
	{
		_loadFunc = loadFunc;
	}

	/// <summary>
	/// 默认构造函数
	/// </summary>
	public BaseDataTable()
	{
	}

	/// <summary>
	/// 异步加载数据
	/// </summary>
	/// <returns>一个表示异步操作的任务</returns>
	public abstract Task LoadAsync();

	/// <summary>
	/// 根据整型ID获取对象
	/// </summary>
	/// <param name="id">对象的ID</param>
	/// <returns>找到的对象，如果未找到则返回默认值</returns>
	public T Get(int id)
	{
		LongDataMaps.TryGetValue(id, out var value);
		return value;
	}

	/// <summary>
	/// 根据长整型ID获取对象
	/// </summary>
	/// <param name="id">对象的ID</param>
	/// <returns>找到的对象，如果未找到则返回默认值</returns>
	public T Get(long id)
	{
		LongDataMaps.TryGetValue(id, out var value);
		return value;
	}

	/// <summary>
	/// 根据字符串ID获取对象
	/// </summary>
	/// <param name="id">对象的ID</param>
	/// <returns>找到的对象，如果未找到则返回默认值</returns>
	public T Get(string id)
	{
		StringDataMaps.TryGetValue(id, out var value);
		return value;
	}

	/// <summary>
	/// 获取数据表中所有对象
	/// </summary>
	/// <returns>数据表中的所有对象数组</returns>
	public T[] ToArray()
	{
		return DataList.ToArray();
	}

	/// <summary>
	/// 根据条件查找对象
	/// </summary>
	/// <param name="func">查找条件</param>
	/// <returns>满足条件的第一个对象，如果未找到则返回默认值</returns>
	public T Find(Func<T, bool> func)
	{
		return DataList.FirstOrDefault(func);
	}

	/// <summary>
	/// 根据条件查找多个对象
	/// </summary>
	/// <param name="func">查找条件</param>
	/// <returns>满足条件的所有对象数组</returns>
	public T[] FindList(Func<T, bool> func)
	{
		return DataList.Where(func).ToArray();
	}
}
