using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Hotfix.Framework.Config
{
	/// <summary>
	/// 数据表基础接口：定义表的异步加载与行数查询。
	/// </summary>
	public interface IDataTable
	{
		/// <summary>
		/// 异步加载
		/// </summary>
		UniTask LoadAsync();

		/// <summary>
		/// 获取数据表中对象的数量
		/// </summary>
		int Count { get; }
	}

	/// <summary>
	/// 数据表泛型基础接口。
	/// 功能：定义数据表的相关操作方法，如获取，查找，遍历，最大值，求最小值，求和等。
	/// </summary>
	public interface IDataTable<T> : IDataTable where T : class
	{
		/// <summary>
		/// 根据ID获取对象
		/// </summary>
		/// <param name="id">数据行 id。</param>
		T Get(int id);

		/// <summary>
		/// 根据ID获取对象
		/// </summary>
		/// <param name="id">数据行 id。</param>
		T Get(long id);

		/// <summary>
		/// 根据ID获取对象
		/// </summary>
		/// <param name="id">数据行 id。</param>
		T Get(string id);

		/// <summary>
		/// 根据列表索引获取对象
		/// </summary>
		/// <param name="index">索引值</param>
		T this[int index] { get; }

		/// <summary>
		/// 获取数据表中第一个对象
		/// </summary>
		T FirstOrDefault { get; }

		/// <summary>
		/// 获取数据表中最后一个对象
		/// </summary>
		T LastOrDefault { get; }

		/// <summary>
		/// 获取数据表中所有对象
		/// </summary>
		T[] All { get; }

		/// <summary>
		/// 获取数据表中所有对象
		/// </summary>
		T[] ToArray();

		/// <summary>
		/// 获取数据表中所有对象
		/// </summary>
		List<T> ToList();

		/// <summary>
		/// 根据条件查找
		/// </summary>
		/// <param name="func">查询条件</param>
		T Find(System.Func<T, bool> func);

		/// <summary>
		/// 根据条件查找
		/// </summary>
		/// <param name="func">查询条件</param>
		T[] FindListArray(System.Func<T, bool> func);

		/// <summary>
		/// 根据条件查找
		/// </summary>
		/// <param name="func">查询条件</param>
		List<T> FindList(Func<T, bool> func);

		/// <summary>
		/// 遍历
		/// </summary>
		/// <param name="func">查询条件</param>
		void ForEach(Action<T> func);

		/// <summary>
		/// 取最大值
		/// </summary>
		/// <param name="func">查询条件</param>
		TK Max<TK>(Func<T, TK> func) where TK : IComparable<TK>;

		/// <summary>
		/// 取最小值
		/// </summary>
		/// <param name="func">查询条件</param>
		/// <typeparam name="TK">返回值类型</typeparam>
		TK Min<TK>(Func<T, TK> func) where TK : IComparable<TK>;

		/// <summary>
		/// 求和
		/// </summary>
		/// <param name="func">查询条件</param>
		int Sum(Func<T, int> func);

		/// <summary>
		/// 求和
		/// </summary>
		/// <param name="func">查询条件</param>
		long Sum(Func<T, long> func);

		/// <summary>
		/// 求和
		/// </summary>
		/// <param name="func">查询条件</param>
		float Sum(Func<T, float> func);

		/// <summary>
		/// 求和
		/// </summary>
		/// <param name="func">查询条件</param>
		double Sum(Func<T, double> func);

		/// <summary>
		/// 求和
		/// </summary>
		/// <param name="func">查询条件</param>
		decimal Sum(Func<T, decimal> func);
	}
}