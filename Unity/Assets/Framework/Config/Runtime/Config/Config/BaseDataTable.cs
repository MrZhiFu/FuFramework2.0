using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.Scripting;

namespace GameFrameX.Config.Runtime
{
    [Preserve]
    public abstract class BaseDataTable<T> : IDataTable<T> where T : class
    {
        protected readonly SortedDictionary<long, T> LongDataMaps = new SortedDictionary<long, T>();
        protected readonly SortedDictionary<string, T> StringDataMaps = new SortedDictionary<string, T>();

        protected readonly List<T> DataList = new List<T>();
        public abstract Task LoadAsync();

        public T Get(int id)
        {
            LongDataMaps.TryGetValue(id, out T value);
            return value;
        }

        public T Get(long id)
        {
            LongDataMaps.TryGetValue(id, out T value);
            return value;
        }

        public T Get(string id)
        {
            StringDataMaps.TryGetValue(id, out T value);
            return value;
        }

        public T this[int index]
        {
            get
            {
                if (index >= Count || index < 0)
                {
                    throw new IndexOutOfRangeException(nameof(index));
                }

                return DataList[index];
            }
        }

        public int Count
        {
            get { return Math.Max(LongDataMaps.Count, StringDataMaps.Count); }
        }

        public T FirstOrDefault
        {
            get { return DataList.FirstOrDefault(); }
        }

        public T LastOrDefault
        {
            get { return DataList.LastOrDefault(); }
        }

        public T[] All
        {
            get { return DataList.ToArray(); }
        }

        public T[] ToArray()
        {
            return DataList.ToArray();
        }

        public List<T> ToList()
        {
            return DataList.ToList();
        }

        public T Find(Func<T, bool> func)
        {
            return DataList.FirstOrDefault(func);
        }

        public T[] FindListArray(Func<T, bool> func)
        {
            return DataList.Where(func).ToArray();
        }

        public List<T> FindList(Func<T, bool> func)
        {
            return DataList.Where(func).ToList();
        }

        public void ForEach(Action<T> func)
        {
            DataList.ForEach(func);
        }

        public TK Max<TK>(Func<T, TK> func)
        {
            return DataList.Max(func);
        }

        public TK Min<TK>(Func<T, TK> func)
        {
            return DataList.Min(func);
        }

        public int Sum(Func<T, int> func)
        {
            return DataList.Sum(func);
        }

        public long Sum(Func<T, long> func)
        {
            return DataList.Sum(func);
        }

        public float Sum(Func<T, float> func)
        {
            return DataList.Sum(func);
        }

        public double Sum(Func<T, double> func)
        {
            return DataList.Sum(func);
        }

        public decimal Sum(Func<T, decimal> func)
        {
            return DataList.Sum(func);
        }
    }
}