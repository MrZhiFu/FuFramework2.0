using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Core
{
    /// <summary>
    /// 双向字典。
    /// 功能：
    ///     1. 键值对可以双向查找(即通过键或值进行查找)。
    ///     2. 支持通过键或值进行添加、移除、查找等操作。
    /// 实现原理：使用两个字典，一个正向字典(键 -> 值)，一个反向字典(值 -> 键)。
    ///         正向字典用于通过键查找值，反向字典用于通过值查找键。
    /// 
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class FuBidirectionalDictionary<TKey, TValue>
    {
        /// <summary>
        /// 正向字典, 键 -> 值
        /// </summary>
        private readonly Dictionary<TKey, TValue> m_ForwardDict;

        /// <summary>
        /// 反向字典, 值 -> 键
        /// </summary>
        private readonly Dictionary<TValue, TKey> m_ReverseDict;


        /// <summary>
        /// 键值对数量。
        /// </summary>
        public int Count { get; private set; }


        public FuBidirectionalDictionary(int capacity = 8)
        {
            Count         = 0;
            m_ForwardDict = new Dictionary<TKey, TValue>(capacity);
            m_ReverseDict = new Dictionary<TValue, TKey>(capacity);
        }


        /// <summary>
        /// 尝试通过值获取键。
        /// </summary>
        /// <param name="value"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool TryGetKeyByValue(TValue value, out TKey key) => m_ReverseDict.TryGetValue(value, out key);


        /// <summary>
        /// 尝试通过键获取值。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValueByKey(TKey key, out TValue value) => m_ForwardDict.TryGetValue(key, out value);

        /// <summary>
        /// 尝试添加键值对。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryAdd(TKey key, TValue value)
        {
            if (!m_ForwardDict.TryAdd(key, value)) return false;
            m_ReverseDict.Add(value, key);
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
            if (!m_ForwardDict.Remove(key, out var value)) return false;
            m_ReverseDict.Remove(value);
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
            if (!m_ReverseDict.Remove(value, out var key)) return false;
            m_ForwardDict.Remove(key);
            Count--;
            return true;
        }

        /// <summary>
        /// 清空字典。
        /// </summary>
        public void Clear()
        {
            Count = 0;
            m_ForwardDict.Clear();
            m_ReverseDict.Clear();
        }
    }
}
