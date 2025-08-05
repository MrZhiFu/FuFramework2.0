using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    public static partial class ReferencePool
    {
        /// <summary>
        /// 指定类型下的引用信息集合(使用队列存储)，即一个引用类型对应一个引用信息集合。
        /// 功能：
        /// 1. 管理指定类型下的所有引用，包括闲置的、正在使用的、已获取过的、释放归还的、新增的、被移除的。
        /// 2. 提供获取、归还、增加、移除等操作。
        /// 3. 管理引用的生命周期，自动回收引用。
        /// </summary>
        private sealed class ReferenceCollection
        {
            /// 引用池类型
            public Type RefType { get; }

            /// 引用池队列
            private readonly Queue<IReference> m_RefQueue;

            /// 闲置未使用的引用数量(即引用池中的元素数量)
            public int UnusedReferenceCount => m_RefQueue.Count;

            /// 正在使用的引用数量(从引用池中获取的 + 引用池中不存在时new创建的引用数量 - 释放归还的引用数量)
            public int UsingReferenceCount { get; private set; }

            /// 已获取的引用数量(从引用池中获取的 + 引用池中不存在时new创建的引用数量）
            public int AcquireReferenceCount { get; private set; }

            /// 释放(归还)的引用数量
            public int ReleaseReferenceCount { get; private set; }

            /// 新增的引用数量
            public int AddReferenceCount { get; private set; }

            /// 被移除的引用数量
            public int RemoveReferenceCount { get; private set; }

            public ReferenceCollection(Type refType)
            {
                RefType    = refType;
                m_RefQueue = new Queue<IReference>();

                UsingReferenceCount   = 0;
                AcquireReferenceCount = 0;
                ReleaseReferenceCount = 0;
                AddReferenceCount     = 0;
                RemoveReferenceCount  = 0;
            }

            /// <summary>
            /// 从引用池获取引用对象(没有则使用泛型new()创建)
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <returns></returns>
            public T Acquire<T>() where T : class, IReference, new()
            {
                if (typeof(T) != RefType)
                    throw new GameFrameworkException("类型无效.");

                UsingReferenceCount++;
                AcquireReferenceCount++;

                lock (m_RefQueue)
                {
                    if (m_RefQueue.Count > 0)
                        return (T)m_RefQueue.Dequeue();
                }

                AddReferenceCount++;
                return new T();
            }

            /// <summary>
            /// 从引用池获取引用对象(使用没有则使用Activator.CreateInstance()创建)
            /// </summary>
            /// <returns></returns>
            public IReference Acquire()
            {
                UsingReferenceCount++;
                AcquireReferenceCount++;
                lock (m_RefQueue)
                {
                    if (m_RefQueue.Count > 0)
                        return m_RefQueue.Dequeue();
                }

                AddReferenceCount++;
                return (IReference)Activator.CreateInstance(RefType);
            }

            /// <summary>
            /// 释放引用, 将引用归还引用池
            /// </summary>
            /// <param name="reference"></param>
            public void Release(IReference reference)
            {
                // 清理引用，清除数据后重用该对象
                reference.Clear();

                lock (m_RefQueue)
                {
                    if (EnableStrictCheck && m_RefQueue.Contains(reference))
                        throw new GameFrameworkException("该引用对象已经被释放.");

                    m_RefQueue.Enqueue(reference);
                }

                ReleaseReferenceCount++;
                UsingReferenceCount--;
            }

            /// <summary>
            /// 向引用池中追加指定数量的引用
            /// </summary>
            /// <param name="count"></param>
            /// <typeparam name="T"></typeparam>
            public void Add<T>(int count) where T : class, IReference, new()
            {
                if (typeof(T) != RefType)
                    throw new GameFrameworkException("类型无效，该类型不是引用池类型.");

                lock (m_RefQueue)
                {
                    AddReferenceCount += count;
                    while (count-- > 0)
                    {
                        m_RefQueue.Enqueue(new T());
                    }
                }
            }

            /// <summary>
            /// 向引用池中追加指定数量的引用
            /// </summary>
            /// <param name="count"></param>
            public void Add(int count)
            {
                lock (m_RefQueue)
                {
                    AddReferenceCount += count;
                    while (count-- > 0)
                    {
                        m_RefQueue.Enqueue((IReference)Activator.CreateInstance(RefType));
                    }
                }
            }

            /// <summary>
            /// 从引用池中移除指定数量的引用
            /// </summary>
            /// <param name="count"></param>
            public void Remove(int count)
            {
                lock (m_RefQueue)
                {
                    if (count > m_RefQueue.Count)
                        count = m_RefQueue.Count;

                    RemoveReferenceCount += count;
                    while (count-- > 0)
                    {
                        m_RefQueue.Dequeue();
                    }
                }
            }

            /// <summary>
            /// 从引用池中移除所有的引用
            /// </summary>
            public void RemoveAll()
            {
                lock (m_RefQueue)
                {
                    RemoveReferenceCount += m_RefQueue.Count;
                    m_RefQueue.Clear();
                }
            }
        }
    }
}