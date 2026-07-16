using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 多值字典。
    /// 功能：
    ///     1. 支持多值字典的添加，移除，获取，遍历等操作。
    ///     2. 支持多值字典的值数量的获取。
    ///     3. 支持多值字典的键下是否包含指定值的判断。
    /// 
    /// 实现原理：使用一个链表来实现多个值，优化插入操作性能。
    /// </summary>
    /// <typeparam name="TKey">指定多值字典的主键类型。</typeparam>
    /// <typeparam name="TValue">指定多值字典的值类型。</typeparam>

    #region 使用示例

    // // 创建多值字典
    // var multiDict = new FuMultiDictionary&lt;string, int&gt;();
    //
    // // 添加键值对（一个键对应多个值）
    // multiDict.Add("fruits", 1);
    // multiDict.Add("fruits", 2);
    // multiDict.Add("fruits", 3);
    // multiDict.Add("numbers", 10);
    // multiDict.Add("numbers", 20);
    //
    // // 检查是否包含指定键
    // bool hasFruits = multiDict.Contains("fruits");  // true
    // bool hasVegetables = multiDict.Contains("vegetables");  // false
    //
    // // 检查指定键是否包含指定值
    // bool hasApple = multiDict.Contains("fruits", 1);  // true
    // bool hasOrange = multiDict.Contains("fruits", 4);  // false
    //
    // // 获取指定键的所有值
    // if (multiDict.TryGetValue("fruits", out var range))
    // {
    //     foreach (var value in range)
    //     {
    //         Debug.Log($"fruit value: {value}");  // 输出: 1, 2, 3
    //     }
    // }
    //
    // // 通过索引器获取值范围
    // var numberRange = multiDict["numbers"];
    // if (numberRange.IsValid)
    // {
    //     Debug.Log($"numbers count: {numberRange.Count}");  // 输出: 2
    // }
    //
    // // 获取主键数量
    // Debug.Log($"key count: {multiDict.Count}");  // 输出: 2
    //
    // // 从指定键中移除指定值
    // multiDict.Remove("fruits", 2);
    //
    // // 移除指定键的所有值
    // multiDict.RemoveAll("numbers");
    //
    // // 遍历所有键值对
    // foreach (var pair in multiDict)
    // {
    //     Debug.Log($"key: {pair.Key}, value count: {pair.Value.Count}");
    // }
    //
    // // 清空字典
    // multiDict.Clear();

    #endregion

    public sealed class FuMultiDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, FuLinkedListRange<TValue>>>
    {
        /// <summary>
        /// 存放值的链表
        /// </summary>
        private readonly FuLinkedList<TValue> m_LinkedList;

        /// <summary>
        /// 存放主键与多值的字典, key:指定多值字典的主键类型--Value:存放值的一段范围链表
        /// </summary>
        private readonly Dictionary<TKey, FuLinkedListRange<TValue>> m_Dictionary;

        /// <summary>
        /// 初始化游戏框架多值字典类的新实例。
        /// </summary>
        public FuMultiDictionary()
        {
            m_LinkedList = new FuLinkedList<TValue>();
            m_Dictionary = new Dictionary<TKey, FuLinkedListRange<TValue>>();
        }

        /// <summary>
        /// 获取多值字典中实际包含的主键数量。
        /// </summary>
        public int Count => m_Dictionary.Count;

        /// <summary>
        /// 获取多值字典中指定主键的范围。
        /// </summary>
        /// <param name="key">指定的主键。</param>
        /// <returns>指定主键的范围。</returns>
        public FuLinkedListRange<TValue> this[TKey key]
        {
            get
            {
                m_Dictionary.TryGetValue(key, out var range);
                return range;
            }
        }

        /// <summary>
        /// 清理多值字典。
        /// </summary>
        public void Clear()
        {
            m_Dictionary.Clear();
            m_LinkedList.Clear();
        }

        /// <summary>
        /// 检查多值字典中是否包含指定主键。
        /// </summary>
        /// <param name="key">要检查的主键。</param>
        /// <returns>多值字典中是否包含指定主键。</returns>
        public bool Contains(TKey key) => m_Dictionary.ContainsKey(key);

        /// <summary>
        /// 检查多值字典中是否包含指定值。
        /// </summary>
        /// <param name="key">要检查的主键。</param>
        /// <param name="value">要检查的值。</param>
        /// <returns>多值字典中是否包含指定值。</returns>
        public bool Contains(TKey key, TValue value) => m_Dictionary.TryGetValue(key, out var range) && range.Contains(value);

        /// <summary>
        /// 尝试获取多值字典中指定主键的范围。
        /// </summary>
        /// <param name="key">指定的主键。</param>
        /// <param name="range">指定主键的范围。</param>
        /// <returns>是否获取成功。</returns>
        public bool TryGetValue(TKey key, out FuLinkedListRange<TValue> range) => m_Dictionary.TryGetValue(key, out range);

        /// <summary>
        /// 向指定的主键增加指定的值。
        /// </summary>
        /// <param name="key">指定的主键。</param>
        /// <param name="value">指定的值。</param>
        public void Add(TKey key, TValue value)
        {
            if (m_Dictionary.TryGetValue(key, out var range))
            {
                m_LinkedList.AddBefore(range.End, value);
                return;
            }

            var firstNode    = m_LinkedList.AddLast(value);
            var terminalNode = m_LinkedList.AddLast(default(TValue));

            m_Dictionary.Add(key, new FuLinkedListRange<TValue>(firstNode, terminalNode));
        }

        /// <summary>
        /// 从指定的主键中移除指定的值。
        /// </summary>
        /// <param name="key">指定的主键。</param>
        /// <param name="value">指定的值。</param>
        /// <returns>是否移除成功。</returns>
        public bool Remove(TKey key, TValue value)
        {
            if (!m_Dictionary.TryGetValue(key, out var range)) return false;

            for (var current = range.First; current != null && current != range.End; current = current.Next)
            {
                if (!current.Value.Equals(value)) continue;

                if (current == range.First)
                {
                    var next = current.Next;
                    if (next == range.End)
                    {
                        m_LinkedList.Remove(range.End);
                        m_Dictionary.Remove(key);
                    }
                    else
                    {
                        m_Dictionary[key] = new FuLinkedListRange<TValue>(next, range.End);
                    }
                }

                m_LinkedList.Remove(current);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 从指定的主键中移除所有的值。
        /// </summary>
        /// <param name="key">指定的主键。</param>
        /// <returns>是否移除成功。</returns>
        public bool RemoveAll(TKey key)
        {
            if (!m_Dictionary.Remove(key, out var range)) return false;

            var current = range.First;
            while (current != null)
            {
                var next = current != range.End ? current.Next : null;
                m_LinkedList.Remove(current);
                current = next;
            }

            return true;
        }

        /// <summary>
        /// 返回循环访问集合的枚举数。
        /// </summary>
        /// <returns>循环访问集合的枚举数。</returns>
        public Enumerator GetEnumerator() => new(m_Dictionary);

        /// <summary>
        /// 返回循环访问集合的枚举数。
        /// </summary>
        /// <returns>循环访问集合的枚举数。</returns>
        IEnumerator<KeyValuePair<TKey, FuLinkedListRange<TValue>>> IEnumerable<KeyValuePair<TKey, FuLinkedListRange<TValue>>>.GetEnumerator()
            => GetEnumerator();

        /// <summary>
        /// 返回循环访问集合的枚举数。
        /// </summary>
        /// <returns>循环访问集合的枚举数。</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// 循环访问集合的枚举数。
        /// </summary>
        [StructLayout(LayoutKind.Auto)]
        public struct Enumerator : IEnumerator<KeyValuePair<TKey, FuLinkedListRange<TValue>>>
        {
            private Dictionary<TKey, FuLinkedListRange<TValue>>.Enumerator m_Enumerator;

            internal Enumerator(Dictionary<TKey, FuLinkedListRange<TValue>> dictionary)
            {
                if (dictionary == null)
                {
                    throw new InvalidOperationException("Dictionary is invalid.");
                }

                m_Enumerator = dictionary.GetEnumerator();
            }

            /// <summary>
            /// 获取当前结点。
            /// </summary>
            public KeyValuePair<TKey, FuLinkedListRange<TValue>> Current => m_Enumerator.Current;

            /// <summary>
            /// 获取当前的枚举数。
            /// </summary>
            object IEnumerator.Current => m_Enumerator.Current;

            /// <summary>
            /// 清理枚举数。
            /// </summary>
            public void Dispose() => m_Enumerator.Dispose();

            /// <summary>
            /// 获取下一个结点。
            /// </summary>
            /// <returns>返回下一个结点。</returns>
            public bool MoveNext() => m_Enumerator.MoveNext();

            /// <summary>
            /// 重置枚举数。
            /// </summary>
            void IEnumerator.Reset()
            {
                ((IEnumerator<KeyValuePair<TKey, FuLinkedListRange<TValue>>>)m_Enumerator).Reset();
            }
        }
    }
}