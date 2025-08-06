using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 游戏框架多值字典类(使用一个链表来实现多个值，优化插入操作性能)。
    /// </summary>
    /// <typeparam name="TKey">指定多值字典的主键类型。</typeparam>
    /// <typeparam name="TValue">指定多值字典的值类型。</typeparam>
    public sealed class GameFrameworkMultiDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, GameFrameworkLinkedListRange<TValue>>>
    {
        /// 存放值的链表
        private readonly GameFrameworkLinkedList<TValue> m_LinkedList;

        /// 存放主键与多值的字典, key:指定多值字典的主键类型--Value:存放值的一段范围链表
        private readonly Dictionary<TKey, GameFrameworkLinkedListRange<TValue>> m_Dictionary;

        /// <summary>
        /// 初始化游戏框架多值字典类的新实例。
        /// </summary>
        public GameFrameworkMultiDictionary()
        {
            m_LinkedList = new GameFrameworkLinkedList<TValue>();
            m_Dictionary = new Dictionary<TKey, GameFrameworkLinkedListRange<TValue>>();
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
        public GameFrameworkLinkedListRange<TValue> this[TKey key]
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
        public bool TryGetValue(TKey key, out GameFrameworkLinkedListRange<TValue> range) => m_Dictionary.TryGetValue(key, out range);

        /// <summary>
        /// 向指定的主键增加指定的值。
        /// </summary>
        /// <param name="key">指定的主键。</param>
        /// <param name="value">指定的值。</param>
        public void Add(TKey key, TValue value)
        {
            if (m_Dictionary.TryGetValue(key, out var range))
            {
                m_LinkedList.AddBefore(range.Terminal, value);
                return;
            }

            var firstNode    = m_LinkedList.AddLast(value);
            var terminalNode = m_LinkedList.AddLast(default(TValue));

            m_Dictionary.Add(key, new GameFrameworkLinkedListRange<TValue>(firstNode, terminalNode));
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

            for (var current = range.First; current != null && current != range.Terminal; current = current.Next)
            {
                if (!current.Value.Equals(value)) continue;

                if (current == range.First)
                {
                    var next = current.Next;
                    if (next == range.Terminal)
                    {
                        m_LinkedList.Remove(next);
                        m_Dictionary.Remove(key);
                    }
                    else
                        m_Dictionary[key] = new GameFrameworkLinkedListRange<TValue>(next, range.Terminal);
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
                var next = current != range.Terminal ? current.Next : null;
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
        IEnumerator<KeyValuePair<TKey, GameFrameworkLinkedListRange<TValue>>> IEnumerable<KeyValuePair<TKey, GameFrameworkLinkedListRange<TValue>>>.GetEnumerator()
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
        public struct Enumerator : IEnumerator<KeyValuePair<TKey, GameFrameworkLinkedListRange<TValue>>>
        {
            private Dictionary<TKey, GameFrameworkLinkedListRange<TValue>>.Enumerator m_Enumerator;

            internal Enumerator(Dictionary<TKey, GameFrameworkLinkedListRange<TValue>> dictionary)
            {
                if (dictionary == null)
                {
                    throw new GameFrameworkException("Dictionary is invalid.");
                }

                m_Enumerator = dictionary.GetEnumerator();
            }

            /// <summary>
            /// 获取当前结点。
            /// </summary>
            public KeyValuePair<TKey, GameFrameworkLinkedListRange<TValue>> Current => m_Enumerator.Current;

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
                ((IEnumerator<KeyValuePair<TKey, GameFrameworkLinkedListRange<TValue>>>)m_Enumerator).Reset();
            }
        }
    }
}