using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
// ReSharper disable InconsistentlySynchronizedField
namespace Hotfix.Framework.Core
{
    /// <summary>
    /// 引用集合(使用栈存储)，即一个引用类型对应一个引用信息集合。
    /// 功能：
    ///     1. 管理指定类型下的所有引用，包括闲置的、正在使用的、已获取过的、释放归还的、新增的、被移除的。
    ///     2. 提供获取、归还、增加、移除等操作。
    /// </summary>
    internal sealed class ReferenceCollection
    {
        /// <summary>
        /// 引用池内的引用类型。
        /// </summary>
        public Type RefType { get; }

        /// <summary>
        /// 引用池栈, 存储闲置的引用对象，使用栈结构对缓存友好。
        /// </summary>
        private readonly Stack<IReference> m_FreeStack = new();

        /// <summary>
        /// 闲置引用对象的归属索引，与 m_FreeStack 中的元素严格保持一一对应。
        /// 用途：将 Recycle 的重复归还检测由栈的线性扫描(O(n))降为哈希查找(O(1))。
        /// 采用默认相等比较器，与原 Stack&lt;T&gt;.Contains 的判定口径完全一致。
        /// </summary>
        private readonly HashSet<IReference> m_InPoolSet = new();

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
            if (typeof(T) != RefType) throw new InvalidOperationException("[ReferencePool.ReferenceCollection] 引用获取失败，引用类型无效.");

            lock (m_FreeStack)
            {
                if (m_FreeStack.Count > 0)
                {
                    // 先 Peek 校验类型、后 Pop/自增：类型不匹配时直接抛出，池内结构（栈、归属索引）与四个计数
                    // 全部保持原样，异常路径不改变任何状态。
                    // 旧写法先 Pop + m_InPoolSet.Remove + 计数自增、之后才校验并抛出，会让 UsingReferenceCount/
                    // AcquireReferenceCount 虚高，且对象已出栈（既不在池中、也不被任何持有者引用）而丢失。
                    var reference = m_FreeStack.Peek();
                    if (reference is not T result)
                        throw new InvalidOperationException($"[ReferencePool.ReferenceCollection] 引用获取失败，池中对象类型不匹配，期望 '{RefType.Name}'，实际 '{reference.GetType().Name}'.");

                    m_FreeStack.Pop();
                    m_InPoolSet.Remove(reference);
                    UsingReferenceCount++;
                    AcquireReferenceCount++;
                    return result;
                }

                AddReferenceCount++;
                UsingReferenceCount++;
                AcquireReferenceCount++;
            }

            // 对象创建在锁外，避免阻塞其他线程的获取/释放
            return new T();
        }

        /// <summary>
        /// 将引用归还到引用池中。
        /// </summary>
        /// <param name="reference">要释放的引用。</param>
        public void Recycle(IReference reference)
        {
            if (reference == null) throw new InvalidOperationException("[ReferencePool.ReferenceCollection] 引用释放失败，引用对象为空.");

            // 重复释放检测：O(1) 哈希查找，无条件保留，杜绝同一对象被同时交给多个持有者。
            // 单独置于 Clear 之前：池中已有该对象时立即失败，避免误清空一个闲置对象的数据
            lock (m_FreeStack)
            {
                if (m_InPoolSet.Contains(reference))
                    throw new InvalidOperationException($"[ReferencePool.ReferenceCollection] 引用实例{reference.GetType().Name}释放失败，该对象已经被释放.");
            }

            // 清理引用在锁外执行：
            // 1) Clear() 的实现可能回调 ReferencePool.Recycle，锁内调用会形成跨集合的嵌套取锁(锁序反转)；
            // 2) 此时对象尚未入栈(m_FreeStack/m_InPoolSet 均未改动)、UsingReferenceCount 也尚未自减，对象对其它线程不可见，
            //    因此锁外清理安全；若 Clear() 抛异常，则异常直接向上抛出：池内结构保持一致(未入栈、计数未改)，
            //    代价是该对象不再归还池(仍计入"使用中"，等同本次 Release 失效)，而不是"对象滞留于已计数未入栈的中间态"。
            reference.Clear();

            lock (m_FreeStack)
            {
                // 计数校验与递减必须同临界区：若拆到锁外，并发归还不同对象时两个线程会同时通过校验并把计数减成负数
                if (UsingReferenceCount <= 0)
                    throw new InvalidOperationException($"[ReferencePool.ReferenceCollection] 引用实例{reference.GetType().Name}释放失败，使用计数已为零，存在未通过池获取的 Release 调用.");

                // 再次以 Add 结果判定重复：Clear 期间可能有并发归还同一对象。校验全部完成后才改状态(Clear 已在外完成，此处只剩 Push/计数)
                if (!m_InPoolSet.Add(reference))
                    throw new InvalidOperationException($"[ReferencePool.ReferenceCollection] 引用实例{reference.GetType().Name}释放失败，该对象已经被释放.");

                m_FreeStack.Push(reference);
                ReleaseReferenceCount++;
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
            if (typeof(T) != RefType) throw new InvalidOperationException($"[ReferencePool.ReferenceCollection] 添加引用失败，类型{typeof(T).Name}不是引用池类型.");

            // count <= 0 时直接返回：否则 AddReferenceCount += count 会累加非正数，而入栈循环按实际创建数(0)执行，计数与实际池内容不符
            if (count <= 0) return;

            // 对象创建可能较重，放在锁外批量完成，避免持锁期间阻塞其它线程的获取/归还；
            // 创建全部成功后再进锁统一入栈与计数，保持 AddReferenceCount 语义不变。
            var references = new List<IReference>(count);
            for (var i = 0; i < count; i++)
            {
                references.Add(new T());
            }

            lock (m_FreeStack)
            {
                AddReferenceCount += count;
                for (var i = 0; i < references.Count; i++)
                {
                    var reference = references[i];
                    m_FreeStack.Push(reference);
                    m_InPoolSet.Add(reference);
                }
            }
        }

        /// <summary>
        /// 从引用池中移除指定数量的闲置引用。
        /// </summary>
        /// <param name="count">移除数量，超过闲置总数时移除全部闲置引用。</param>
        public void Remove(int count)
        {
            // count <= 0 时直接返回：否则 RemoveReferenceCount += count 会累加非正数把计数减成负数，而移除循环按 0 次执行
            if (count <= 0) return;

            lock (m_FreeStack)
            {
                if (count > m_FreeStack.Count)
                    count = m_FreeStack.Count;

                RemoveReferenceCount += count;
                while (count-- > 0)
                {
                    var reference = m_FreeStack.Pop();
                    m_InPoolSet.Remove(reference);
                }
            }
        }

        /// <summary>
        /// 从引用池中移除所有的闲置引用。
        /// </summary>
        public void RemoveAll()
        {
            lock (m_FreeStack)
            {
                RemoveReferenceCount += m_FreeStack.Count;
                m_FreeStack.Clear();
                m_InPoolSet.Clear();
            }
        }
    }
}
