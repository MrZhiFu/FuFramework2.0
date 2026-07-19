using System;
using System.Collections.Generic;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
// ReSharper disable InconsistentlySynchronizedField
namespace Hotfix.Framework.ReferencePools
{
    public static partial class ReferencePool
    {
        /// <summary>
        /// 引用集合(使用队列存储)，即一个引用类型对应一个引用信息集合。
        /// 功能：
        ///     1. 管理指定类型下的所有引用，包括闲置的、正在使用的、已获取过的、释放归还的、新增的、被移除的。
        ///     2. 提供获取、归还、增加、移除等操作。
        ///     3. 管理引用的生命周期，自动回收引用。
        /// </summary>
        private sealed class ReferenceCollection
        {
            /// 引用池内的引用类型
            public Type RefType { get; }

            /// 引用池队列, 存储闲置的引用对象
            private readonly Queue<IReference> m_FreeQueue = new();


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

            /// 闲置未使用的引用数量(即引用池中的元素数量)
            public int UnusedReferenceCount => m_FreeQueue?.Count ?? 0;

            
            public ReferenceCollection(Type refType)
            {
                RefType = refType;
            }

            /// <summary>
            /// 从引用池获取引用对象(没有则使用new T()创建)
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <returns></returns>
            // ReSharper disable once MemberHidesStaticFromOuterClass
            public T Acquire<T>() where T : class, IReference, new()
            {
                if (typeof(T) != RefType) throw new InvalidOperationException("[ReferencePool.ReferenceCollection] 引用获取失败，引用类型无效.");

                UsingReferenceCount++;
                AcquireReferenceCount++;

                lock (m_FreeQueue)
                {
                    if (m_FreeQueue.Count > 0)
                        return m_FreeQueue.Dequeue() as T;
                }

                AddReferenceCount++;
                return new T();
            }

            /// <summary>
            /// 从引用池获取引用对象(没有则使用Activator.CreateInstance()创建)
            /// </summary>
            /// <returns></returns>
            public IReference Acquire()
            {
                UsingReferenceCount++;
                AcquireReferenceCount++;
                
                lock (m_FreeQueue)
                {
                    if (m_FreeQueue.Count > 0)
                        return m_FreeQueue.Dequeue();
                }

                AddReferenceCount++;
                return Activator.CreateInstance(RefType) as IReference;
            }

            /// <summary>
            /// 释放引用, 将引用归还到引用池中
            /// </summary>
            /// <param name="reference"></param>
            // ReSharper disable once MemberHidesStaticFromOuterClass
            public void Release(IReference reference)
            {
                if (reference == null) throw new InvalidOperationException("[ReferencePool.ReferenceCollection] 引用释放失败，引用对象为空.");

                // 清理引用，清除数据后方便重用该对象
                reference.Clear();

                lock (m_FreeQueue)
                {
                    if (m_FreeQueue.Contains(reference))
                        throw new InvalidOperationException($"[ReferencePool.ReferenceCollection] 引用实例{reference.GetType().Name}释放失败，该对象已经被释放.");

                    m_FreeQueue.Enqueue(reference);
                }

                ReleaseReferenceCount++;
                UsingReferenceCount--;
            }

            /// <summary>
            /// 向引用池中添加指定数量的引用(使用new T()创建)
            /// </summary>
            /// <param name="count"></param>
            /// <typeparam name="T"></typeparam>
            // ReSharper disable once MemberHidesStaticFromOuterClass
            public void Add<T>(int count) where T : class, IReference, new()
            {
                if (typeof(T) != RefType) throw new InvalidOperationException($"[ReferencePool.ReferenceCollection] 添加引用失败，类型{typeof(T).Name}不是引用池类型.");

                lock (m_FreeQueue)
                {
                    AddReferenceCount += count;
                    while (count-- > 0)
                    {
                        var reference = new T();
                        m_FreeQueue.Enqueue(reference);
                    }
                }
            }

            /// <summary>
            /// 向引用池中添加指定数量的引用(使用Activator.CreateInstance()创建)
            /// </summary>
            /// <param name="count"></param>
            public void Add(int count)
            {
                lock (m_FreeQueue)
                {
                    AddReferenceCount += count;
                    while (count-- > 0)
                    {
                        var reference = Activator.CreateInstance(RefType) as IReference;
                        m_FreeQueue.Enqueue(reference);
                    }
                }
            }

            /// <summary>
            /// 从引用池中移除指定数量的引用
            /// </summary>
            /// <param name="count"></param>
            public void Remove(int count)
            {
                lock (m_FreeQueue)
                {
                    if (count > m_FreeQueue.Count)
                        count = m_FreeQueue.Count;

                    RemoveReferenceCount += count;
                    while (count-- > 0)
                    {
                        m_FreeQueue.Dequeue();
                    }
                }
            }

            /// <summary>
            /// 从引用池中移除所有的引用
            /// </summary>
            public void RemoveAll()
            {
                lock (m_FreeQueue)
                {
                    RemoveReferenceCount += m_FreeQueue.Count;
                    m_FreeQueue.Clear();
                }
            }
        }
    }
}
