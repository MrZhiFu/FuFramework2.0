using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 双向字典。
    /// 键值对可以双向查找，但只能通过键或值进行查找，不能通过索引。
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class FuBidirectionalDictionary<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> _forwardDictionary; // 正向字典
        private readonly Dictionary<TValue, TKey> _reverseDictionary; // 反向字典
        
        
        /// <summary>
        /// 键值对数量。
        /// </summary>
        public int Count { get; private set; }
        

        public FuBidirectionalDictionary(int capacity = 8)
        {
            Count              = 0;
            _forwardDictionary = new Dictionary<TKey, TValue>(capacity);
            _reverseDictionary = new Dictionary<TValue, TKey>(capacity);
        }


        public bool TryGetKey(TValue value, out TKey key)
        {
            return _reverseDictionary.TryGetValue(value, out key);
        }


        public bool TryGetValue(TKey key, out TValue value)
        {
            return _forwardDictionary.TryGetValue(key, out value);
        }

        public bool TryAdd(TKey key, TValue value)
        {
            if (_forwardDictionary.TryAdd(key, value))
            {
                _reverseDictionary.Add(value, key);
                Count++;
                return true;
            }

            return false;
        }

        public bool TryRemoveByKey(TKey key)
        {
            if (_forwardDictionary.Remove(key, out var value))
            {
                _reverseDictionary.Remove(value);
                Count--;
                return true;
            }

            return false;
        }

        public bool TryRemoveByValue(TValue value)
        {
            if (_reverseDictionary.Remove(value, out var key))
            {
                _forwardDictionary.Remove(key);
                Count--;
                return true;
            }

            return false;
        }

        public void Clear()
        {
            Count = 0;
            _forwardDictionary.Clear();
            _reverseDictionary.Clear();
        }
    }
}