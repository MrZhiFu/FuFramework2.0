using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace FuFramework.Config.Runtime
{
    /// <summary>
    /// 数据表基类。
    /// 功能：
    /// 1. 维护数据表的key与数据的映射关系。
    /// 2. 实现数据表泛型基础接口IDataTable<T> 相关的相关操作方法，如获取，查找，遍历，最大值，求最小值，求和等。
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
        public T FirstOrDefault
        {
            get
            {
                if (DataList.Count == 0) return null;
                return DataList[0];
            }
        }

        /// <summary>
        /// 获取数据表的最后一个数据。
        /// </summary>
        public T LastOrDefault
        {
            get
            {
                if (DataList.Count == 0) return null;
                return DataList[DataList.Count - 1];
            }
        }

        /// <summary>
        /// 获取数据表的所有数据。
        /// </summary>
        public T[] All
        {
            get
            {
                var results = new T[DataList.Count];
                DataList.CopyTo(results, 0);
                return results;
            }
        }

        /// <summary>
        /// 数据表List转化为数组。
        /// </summary>
        /// <returns></returns>
        public T[] ToArray()
        {
            var results = new T[DataList.Count];
            DataList.CopyTo(results, 0);
            return results;
        }

        /// <summary>
        /// 数据表List转化为List copy。
        /// </summary>
        /// <returns></returns>
        public List<T> ToList() => new List<T>(DataList);

        /// <summary>
        /// 查找符合条件的第一个数据。
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public T Find(Func<T, bool> func)
        {
            foreach (var item in DataList)
            {
                if (func(item))
                    return item;
            }

            return null;
        }

        /// <summary>
        /// 查找所有符合条件的数据。
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public T[] FindListArray(Func<T, bool> func)
        {
            var results = new List<T>();
            foreach (var item in DataList)
            {
                if (func(item))
                    results.Add(item);
            }

            return results.ToArray();
        }

        /// <summary>
        /// 查找所有符合条件的数据。
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public List<T> FindList(Func<T, bool> func)
        {
            var results = new List<T>();
            foreach (var item in DataList)
            {
                if (func(item))
                    results.Add(item);
            }

            return results;
        }

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
        public TK Max<TK>(Func<T, TK> func) where TK : IComparable<TK>
        {
            if (DataList.Count == 0) return default;
            var max = func(DataList[0]);
            for (var i = 1; i < DataList.Count; i++)
            {
                var value = func(DataList[i]);
                if (value.CompareTo(max) > 0)
                    max = value;
            }

            return max;
        }

        /// <summary>
        /// 获取数据表的最小值。
        /// </summary>
        /// <param name="func"></param>
        /// <typeparam name="TK"></typeparam>
        /// <returns></returns>
        public TK Min<TK>(Func<T, TK> func) where TK : IComparable<TK>
        {
            if (DataList.Count == 0) return default;
            var min = func(DataList[0]);
            for (var i = 1; i < DataList.Count; i++)
            {
                var value = func(DataList[i]);
                if (value.CompareTo(min) < 0)
                    min = value;
            }

            return min;
        }

        /// <summary>
        /// 获取数据表的和。
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public int Sum(Func<T, int> func)
        {
            var sum = 0;
            foreach (var item in DataList)
            {
                sum += func(item);
            }

            return sum;
        }

        /// <summary>
        /// 获取数据表的和。
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public long Sum(Func<T, long> func)
        {
            var sum = 0L;
            foreach (var item in DataList)
            {
                sum += func(item);
            }

            return sum;
        }

        /// <summary>
        /// 获取数据表的和。
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public float Sum(Func<T, float> func)
        {
            var sum = 0f;
            foreach (var item in DataList)
            {
                sum += func(item);
            }

            return sum;
        }

        /// <summary>
        /// 获取数据表的和。
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public double Sum(Func<T, double> func)
        {
            var sum = 0.0;
            foreach (var item in DataList)
            {
                sum += func(item);
            }

            return sum;
        }

        /// <summary>
        /// 获取数据表的和。
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public decimal Sum(Func<T, decimal> func)
        {
            var sum = 0m;
            foreach (var item in DataList)
            {
                sum += func(item);
            }

            return sum;
        }
    }
}