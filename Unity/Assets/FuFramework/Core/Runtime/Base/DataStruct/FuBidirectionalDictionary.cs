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
        /// 正向字典
        private readonly Dictionary<TKey, TValue> m_ForwardDictionary;

        /// 反向字典
        private readonly Dictionary<TValue, TKey> m_ReverseDictionary;


        /// <summary>
        /// 键值对数量。
        /// </summary>
        public int Count { get; private set; }


        public FuBidirectionalDictionary(int capacity = 8)
        {
            Count               = 0;
            m_ForwardDictionary = new Dictionary<TKey, TValue>(capacity);
            m_ReverseDictionary = new Dictionary<TValue, TKey>(capacity);
        }


        /// <summary>
        /// 尝试通过键获取值。
        /// </summary>
        /// <param name="value"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool TryGetKey(TValue value, out TKey key) => m_ReverseDictionary.TryGetValue(value, out key);


        /// <summary>
        /// 尝试通过值获取键。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue(TKey key, out TValue value) => m_ForwardDictionary.TryGetValue(key, out value);

        /// <summary>
        /// 尝试添加键值对。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryAdd(TKey key, TValue value)
        {
            if (!m_ForwardDictionary.TryAdd(key, value)) return false;
            m_ReverseDictionary.Add(value, key);
            Count++;
            return true;
        }

        /// <summary>
        /// 尝试通过键移除键值对。
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool TryRemoveByKey(TKey key)
        {
            if (!m_ForwardDictionary.Remove(key, out var value)) return false;
            m_ReverseDictionary.Remove(value);
            Count--;
            return true;
        }

        /// <summary>
        /// 尝试通过值移除键值对。
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryRemoveByValue(TValue value)
        {
            if (!m_ReverseDictionary.Remove(value, out var key)) return false;
            m_ForwardDictionary.Remove(key);
            Count--;
            return true;
        }

        /// <summary>
        /// 清空字典。
        /// </summary>
        public void Clear()
        {
            Count = 0;
            m_ForwardDictionary.Clear();
            m_ReverseDictionary.Clear();
        }
    }
}