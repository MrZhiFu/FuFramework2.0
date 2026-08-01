using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
// ReSharper disable InconsistentlySynchronizedField
namespace Hotfix.Framework.ReferencePool
{
    public sealed partial class ReferencePoolModule
    {
        /// <summary>
        /// 引用集合(使用栈存储)，即一个引用类型对应一个引用信息集合。
        /// 功能：
        ///     1. 管理指定类型下的所有引用，包括闲置的、正在使用的、已获取过的、释放归还的、新增的、被移除的。
        ///     2. 提供获取、归还、增加、移除等操作。
        /// </summary>
        private sealed class ReferenceCollection
        {
            /// <summary>
            /// 引用池内的引用类型。
            /// </summary>
            public Type RefType { get; }

            /// <summary>
            /// 引用池栈, 存储闲置的引用对象。
            /// </summary>
            private readonly Stack<IReference> m_FreeStack = new();

            /// <summary>
            /// 正在使用的引用数量(从引用池中获取的 + 引用池中不存在时new创建的引用数量 - 释放归还的引用数量)。
            /// </summary>
            public int UsingReferenceCount { get; private set; }

            /// <summary>
            /// 已获取的引用数量(从引用池中获取的 + 引用池中不存在时new创建的引用数量）。
            /// </summary>
            public int AcquireReferenceCount { get; private set; }

            /// <summary>
            /// 释放(归还)的引用数量。
            /// </summary>
            public int ReleaseReferenceCount { get; private set; }

            /// <summary>
            /// 新增的引用数量。
            /// </summary>
            public int AddReferenceCount { get; private set; }

            /// <summary>
            /// 被移除的引用数量。
            /// </summary>
            public int RemoveReferenceCount { get; private set; }

            /// <summary>
            /// 闲置未使用的引用数量(即引用池中的元素数量)。
            /// </summary>
            public int UnusedReferenceCount => m_FreeStack.Count;

            /// <summary>
            /// 初始化引用集合的新实例。
            /// </summary>
            /// <param name="refType">引用类型。</param>
            public ReferenceCollection(Type refType)
            {
                RefType = refType;
            }

            /// <summary>
            /// 从引用池获取引用对象(没有则使用new T()创建)。
            /// </summary>
            /// <typeparam name="T">引用类型。</typeparam>
            /// <returns>引用对象。</returns>
            public T Acquire<T>() where T : class, IReference, new()
            {
                if (typeof(T) != RefType) throw new InvalidOperationException("[ReferencePoolModule.ReferenceCollection] 引用获取失败，引用类型无效.");

                lock (m_FreeStack)
                {
                    UsingReferenceCount++;
                    AcquireReferenceCount++;

                    if (m_FreeStack.Count > 0)
                        return m_FreeStack.Pop() as T;

                    AddReferenceCount++;
                }

                // 对象创建在锁外，避免阻塞其他线程的获取/释放
                return new T();
            }

            /// <summary>
            /// 释放引用, 将引用归还到引用池中。
            /// </summary>
            /// <param name="reference">要释放的引用。</param>
            public void Release(IReference reference)
            {
                if (reference == null) throw new InvalidOperationException("[ReferencePoolModule.ReferenceCollection] 引用释放失败，引用对象为空.");

                lock (m_FreeStack)
                {
                    // 重复释放检测：无条件保留，杜绝同一对象被同时交给多个持有者
                    if (m_FreeStack.Contains(reference))
                        throw new InvalidOperationException($"[ReferencePoolModule.ReferenceCollection] 引用实例{reference.GetType().Name}释放失败，该对象已经被释放.");

                    // 清理引用，清除数据后方便重用该对象
                    reference.Clear();
                    m_FreeStack.Push(reference);

                    ReleaseReferenceCount++;

                    if (UsingReferenceCount <= 0)
                        throw new InvalidOperationException($"[ReferencePoolModule.ReferenceCollection] 引用实例{reference.GetType().Name}释放失败，使用计数已为零，存在未通过池获取的 Release 调用.");

                    UsingReferenceCount--;
                }
            }

            /// <summary>
            /// 向引用池中添加指定数量的引用(使用new T()创建)。
            /// </summary>
            /// <typeparam name="T">引用类型。</typeparam>
            /// <param name="count">添加数量。</param>
            public void Add<T>(int count) where T : class, IReference, new()
            {
                if (typeof(T) != RefType) throw new InvalidOperationException($"[ReferencePoolModule.ReferenceCollection] 添加引用失败，类型{typeof(T).Name}不是引用池类型.");

                lock (m_FreeStack)
                {
                    AddReferenceCount += count;
                    while (count-- > 0)
                    {
                        var reference = new T();
                        m_FreeStack.Push(reference);
                    }
                }
            }

            /// <summary>
            /// 从引用池中移除指定数量的引用。
            /// </summary>
            /// <param name="count">移除数量。</param>
            public void Remove(int count)
            {
                lock (m_FreeStack)
                {
                    if (count > m_FreeStack.Count)
                        count = m_FreeStack.Count;

                    RemoveReferenceCount += count;
                    while (count-- > 0)
                    {
                        m_FreeStack.Pop();
                    }
                }
            }

            /// <summary>
            /// 从引用池中移除所有的引用。
            /// </summary>
            public void RemoveAll()
            {
                lock (m_FreeStack)
                {
                    RemoveReferenceCount += m_FreeStack.Count;
                    m_FreeStack.Clear();
                }
            }
        }
    }
}
