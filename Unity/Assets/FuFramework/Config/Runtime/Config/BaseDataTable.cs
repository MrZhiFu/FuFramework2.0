using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace FuFramework.Config.Runtime
{
    /// <summary>
    /// 数据表基类。
    /// 功能：
    /// 1. 维护数据表的key与数据的映射关系。
    /// 2. 实现数据表的相关操作方法，如获取，查找，遍历，最大值，求最小值，求和等。
    /// </summary>
    /// <typeparam name="T">具体数据表类型</typeparam>
    public abstract class BaseDataTable<T> : IDataTable<T> where T : class
    {
        /// <summary>
        /// 数据表的字典映射关系。key为long类型的id，value为具体的数据表类型。
        /// </summary>
        protected readonly SortedDictionary<long, T> LongKeyDataDict = new();

        /// <summary>
        /// 数据表的字典映射关系。key为string类型的id，value为具体的数据表类型。
        /// </summary>
        protected readonly SortedDictionary<string, T> StrKeyDataDict = new();

        /// <summary>
        /// 数据表的数据列表。
        /// </summary>
        protected readonly List<T> DataList = new();

        /// <summary>
        /// 异步加载数据表。
        /// </summary>
        /// <returns></returns>
        public abstract Task LoadAsync();

        /// <summary>
        /// 通过int类型的id获取数据。
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public T Get(int id)
        {
            LongKeyDataDict.TryGetValue(id, out var value);
            return value;
        }

        /// <summary>
        /// 通过long类型的id获取数据。
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public T Get(long id)
        {
            LongKeyDataDict.TryGetValue(id, out var value);
            return value;
        }

        /// <summary>
        /// 通过string类型的id获取数据。
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public T Get(string id)
        {
            StrKeyDataDict.TryGetValue(id, out var value);
            return value;
        }

        /// <summary>
        /// 索引器。
        /// </summary>
        /// <param name="index"></param>
        public T this[int index] => index >= Count || index < 0 ? throw new IndexOutOfRangeException(nameof(index)) : DataList[index];

        /// <summary>
        /// 获取数据表的数量。
        /// </summary>
        public int Count => Math.Max(LongKeyDataDict.Count, StrKeyDataDict.Count);

        /// <summary>
        /// 获取数据表的第一个数据。
        /// </summary>
        public T FirstOrDefault => DataList.FirstOrDefault();

        /// <summary>
        /// 获取数据表的最后一个数据。
        /// </summary>
        public T LastOrDefault => DataList.LastOrDefault();

        /// <summary>
        /// 获取数据表的所有数据。
        /// </summary>
        public T[] All => DataList.ToArray();

        /// <summary>
        /// 数据表List转化为数组。
        /// </summary>
        /// <returns></returns>
        public T[] ToArray() => DataList.ToArray();

        /// <summary>
        /// 数据表List转化为List copy。
        /// </summary>
        /// <returns></returns>
        public List<T> ToList() => DataList.ToList();

        /// <summary>
        /// 查找符合条件的第一个数据。
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public T Find(Func<T, bool> func) => DataList.FirstOrDefault(func);

        /// <summary>
        /// 查找所有符合条件的数据。
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public T[] FindListArray(Func<T, bool> func) => DataList.Where(func).ToArray();

        /// <summary>
        /// 查找所有符合条件的数据。
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public List<T> FindList(Func<T, bool> func) => DataList.Where(func).ToList();

        /// <summary>
        /// 遍历数据表。
        /// </summary>
        /// <param name="func"></param>
        public void ForEach(Action<T> func) => DataList.ForEach(func);

        /// <summary>
        /// 获取数据表的最大值。
        /// </summary>
        /// <param name="func"></param>
        /// <typeparam name="TK"></typeparam>
        /// <returns></returns>
        public TK Max<TK>(Func<T, TK> func) => DataList.Max(func);

        /// <summary>
        /// 获取数据表的最小值。
        /// </summary>
        /// <param name="func"></param>
        /// <typeparam name="TK"></typeparam>
        /// <returns></returns>
        public TK Min<TK>(Func<T, TK> func) => DataList.Min(func);

        /// <summary>
        /// 获取数据表的和。
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public int Sum(Func<T, int> func) => DataList.Sum(func);

        /// <summary>
        /// 获取数据表的和。
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public long Sum(Func<T, long> func) => DataList.Sum(func);

        /// <summary>
        /// 获取数据表的和。
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public float Sum(Func<T, float> func) => DataList.Sum(func);

        /// <summary>
        /// 获取数据表的和。
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public double Sum(Func<T, double> func) => DataList.Sum(func);

        /// <summary>
        /// 获取数据表的和。
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public decimal Sum(Func<T, decimal> func) => DataList.Sum(func);
    }
}