using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 游戏框架链表范围。用于指定一个链表在"指定的开始结点" 到 "指定的终结点"范围内的一段链表
    /// </summary>
    /// <typeparam name="T">指定链表范围的元素类型。</typeparam>
    [StructLayout(LayoutKind.Auto)]
    public readonly struct FuLinkedListRange<T> : IEnumerable<T>
    {
        /// <summary>
        /// 获取链表范围的开始结点。
        /// </summary>
        public LinkedListNode<T> First { get; }

        /// <summary>
        /// 获取链表范围的终结点。
        /// </summary>
        public LinkedListNode<T> End { get; }

        /// <summary>
        /// 初始化游戏框架链表范围的新实例。
        /// </summary>
        /// <param name="first">链表范围的开始结点。</param>
        /// <param name="end">链表范围的终结点。</param>
        public FuLinkedListRange(LinkedListNode<T> first, LinkedListNode<T> end)
        {
            if (first == null || end == null || first == end) throw new FuException("[FuLinkedListRange]链表范围无效!");
            First = first;
            End   = end;
        }

        /// <summary>
        /// 获取链表范围是否有效。
        /// </summary>
        public bool IsValid => First != null && End != null && First != End;

        /// <summary>
        /// 获取链表范围的结点数量。
        /// </summary>
        public int Count
        {
            get
            {
                if (!IsValid) return 0;

                var count = 0;
                for (var current = First; current != null && current != End; current = current.Next)
                {
                    count++;
                }

                return count;
            }
        }

        /// <summary>
        /// 检查是否包含指定值。
        /// </summary>
        /// <param name="value">要检查的值。</param>
        /// <returns>是否包含指定值。</returns>
        public bool Contains(T value)
        {
            for (var current = First; current != null && current != End; current = current.Next)
            {
                if (!current.Value.Equals(value)) continue;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 返回循环访问集合的枚举数。
        /// </summary>
        /// <returns>循环访问集合的枚举数。</returns>
        public Enumerator GetEnumerator() => new(this);

        /// <summary>
        /// 返回循环访问集合的枚举数。
        /// </summary>
        /// <returns>循环访问集合的枚举数。</returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// 返回循环访问集合的枚举数。
        /// </summary>
        /// <returns>循环访问集合的枚举数。</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// 循环访问集合的枚举数。
        /// </summary>
        [StructLayout(LayoutKind.Auto)]
        public struct Enumerator : IEnumerator<T>
        {
            /// <summary>
            /// 链表范围。
            /// </summary>
            private readonly FuLinkedListRange<T> m_Range;

            /// <summary>
            /// 当前结点。
            /// </summary>
            private LinkedListNode<T> m_Current;

            /// <summary>
            /// 当前结点的值。
            /// </summary>
            private T m_CurrentValue;

            internal Enumerator(FuLinkedListRange<T> range)
            {
                if (!range.IsValid) throw new FuException("[Enumerator]链表范围无效!");

                m_Range        = range;
                m_CurrentValue = default;
                m_Current      = m_Range.First;
            }

            /// <summary>
            /// 获取当前结点。
            /// </summary>
            public T Current => m_CurrentValue;

            /// <summary>
            /// 获取当前的枚举数。
            /// </summary>
            object IEnumerator.Current => m_CurrentValue;

            /// <summary>
            /// 清理枚举数。
            /// </summary>
            public void Dispose() { }

            /// <summary>
            /// 获取下一个结点。
            /// </summary>
            /// <returns>返回下一个结点。</returns>
            public bool MoveNext()
            {
                if (m_Current == null || m_Current == m_Range.End)
                    return false;

                m_CurrentValue = m_Current.Value;
                m_Current      = m_Current.Next;

                return true;
            }

            /// <summary>
            /// 重置枚举数。
            /// </summary>
            void IEnumerator.Reset()
            {
                m_Current      = m_Range.First;
                m_CurrentValue = default;
            }
        }
    }
}