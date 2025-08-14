using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace FuFramework.Config.Runtime
{
    /// <summary>
    /// 数据表基类
    /// </summary>
    /// <typeparam name="T">具体数据表类型</typeparam>
    public abstract class BaseDataTable<T> : IDataTable<T> where T : class
    {
        protected readonly SortedDictionary<long, T>   LongDataMaps   = new();
        protected readonly SortedDictionary<string, T> StringDataMaps = new();

        protected readonly List<T> DataList = new();

        public abstract Task LoadAsync();

        public T Get(int id)
        {
            LongDataMaps.TryGetValue(id, out var value);
            return value;
        }

        public T Get(long id)
        {
            LongDataMaps.TryGetValue(id, out var value);
            return value;
        }

        public T Get(string id)
        {
            StringDataMaps.TryGetValue(id, out var value);
            return value;
        }

        public T this[int index] => index >= Count || index < 0 ? throw new IndexOutOfRangeException(nameof(index)) : DataList[index];

        public int Count => Math.Max(LongDataMaps.Count, StringDataMaps.Count);

        public T FirstOrDefault => DataList.FirstOrDefault();

        public T LastOrDefault => DataList.LastOrDefault();

        public T[] All => DataList.ToArray();

        public T[] ToArray() => DataList.ToArray();

        public List<T> ToList() => DataList.ToList();

        public T Find(Func<T, bool> func) => DataList.FirstOrDefault(func);

        public T[] FindListArray(Func<T, bool> func) => DataList.Where(func).ToArray();

        public List<T> FindList(Func<T, bool> func) => DataList.Where(func).ToList();

        public void ForEach(Action<T> func) => DataList.ForEach(func);

        public TK Max<TK>(Func<T, TK> func) => DataList.Max(func);

        public TK Min<TK>(Func<T, TK> func) => DataList.Min(func);

        public int Sum(Func<T, int> func) => DataList.Sum(func);

        public long Sum(Func<T, long> func) => DataList.Sum(func);

        public float Sum(Func<T, float> func) => DataList.Sum(func);

        public double Sum(Func<T, double> func) => DataList.Sum(func);

        public decimal Sum(Func<T, decimal> func) => DataList.Sum(func);
    }
}