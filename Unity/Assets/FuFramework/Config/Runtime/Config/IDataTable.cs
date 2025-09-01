using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace FuFramework.Config.Runtime
{
    public interface IDataTable
    {
        /// <summary>
        /// 异步加载
        /// </summary>
        /// <returns></returns>
        Task LoadAsync();

        /// <summary>
        /// 获取数据表中对象的数量
        /// </summary>
        /// <returns></returns>
        int Count { get; }
    }

    /// <summary>
    /// 数据表基础接口
    /// </summary>
    public interface IDataTable<T> : IDataTable where T : class
    {
        /// <summary>
        /// 根据ID获取对象
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        T Get(int id);

        /// <summary>
        /// 根据ID获取对象
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        T Get(long id);

        /// <summary>
        /// 根据ID获取对象
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        T Get(string id);

        /// <summary>
        /// 根据列表索引获取对象
        /// </summary>
        /// <param name="index">索引值</param>
        /// <returns></returns>
        T this[int index] { get; }

        /// <summary>
        /// 获取数据表中第一个对象
        /// </summary>
        /// <returns></returns>
        T FirstOrDefault { get; }

        /// <summary>
        /// 获取数据表中最后一个对象
        /// </summary>
        /// <returns></returns>
        T LastOrDefault { get; }

        /// <summary>
        /// 获取数据表中所有对象
        /// </summary>
        /// <returns></returns>
        T[] All { get; }

        /// <summary>
        /// 获取数据表中所有对象
        /// </summary>
        /// <returns></returns>
        T[] ToArray();

        /// <summary>
        /// 获取数据表中所有对象
        /// </summary>
        /// <returns></returns>
        List<T> ToList();

        /// <summary>
        /// 根据条件查找
        /// </summary>
        /// <param name="func">查询条件</param>
        /// <returns></returns>
        T Find(System.Func<T, bool> func);

        /// <summary>
        /// 根据条件查找
        /// </summary>
        /// <param name="func">查询条件</param>
        /// <returns></returns>
        T[] FindListArray(System.Func<T, bool> func);

        /// <summary>
        /// 根据条件查找
        /// </summary>
        /// <param name="func">查询条件</param>
        /// <returns></returns>
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
        /// <returns></returns>
        TK Max<TK>(Func<T, TK> func);

        /// <summary>
        /// 取最小值
        /// </summary>
        /// <param name="func">查询条件</param>
        /// <typeparam name="TK">返回值类型</typeparam>
        /// <returns></returns>
        TK Min<TK>(Func<T, TK> func);

        /// <summary>
        /// 求和
        /// </summary>
        /// <param name="func">查询条件</param>
        /// <returns></returns>
        int Sum(Func<T, int> func);

        /// <summary>
        /// 求和
        /// </summary>
        /// <param name="func">查询条件</param>
        /// <returns></returns>
        long Sum(Func<T, long> func);

        /// <summary>
        /// 求和
        /// </summary>
        /// <param name="func">查询条件</param>
        /// <returns></returns>
        float Sum(Func<T, float> func);

        /// <summary>
        /// 求和
        /// </summary>
        /// <param name="func">查询条件</param>
        /// <returns></returns>
        double Sum(Func<T, double> func);

        /// <summary>
        /// 求和
        /// </summary>
        /// <param name="func">查询条件</param>
        /// <returns></returns>
        decimal Sum(Func<T, decimal> func);
    }
}