using System;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Core
{
    /// <summary>
    /// 引用池的公共 API。
    /// 功能：
    ///     1. 从引用池获取引用。
    ///     2. 将引用归还引用池。
    ///     3. 获取引用池的数量。
    /// </summary>
    public static partial class ReferencePool
    {
        /// <summary>
        /// 获取引用池的数量。
        /// </summary>
        // ReSharper disable once InconsistentlySynchronizedField
        public static int Count => m_ReferenceCollectionDict.Count;

        /// <summary>
        /// 从引用池获取引用。
        /// </summary>
        /// <typeparam name="T">引用类型。</typeparam>
        /// <returns>引用。</returns>
        public static T Acquire<T>() where T : class, IReference, new()
        {
            return GetReferenceCollection(typeof(T)).Acquire<T>();
        }

        /// <summary>
        /// 将引用归还引用池。
        /// 注意：始终按引用的**运行时类型**定位集合（类型由 <see cref="object.GetType"/> 取得），
        /// 与 <see cref="Acquire{T}"/> 的入池键一致；不可改为按编译期静态类型定位，
        /// 否则以基类静态类型传入（如 GameEventArgs）会归错集合。
        /// </summary>
        /// <param name="reference">要归还的引用。</param>
        public static void Recycle(IReference reference)
        {
            if (reference == null) throw new InvalidOperationException("[ReferencePool] 要归还的引用对象为空.");

            // 多态入口：静态类型不可知，只能按运行时真实类型定位集合
            GetReferenceCollection(reference.GetType()).Recycle(reference);
        }

        /// <summary>
        /// 向指定类型的引用池中追加指定数量的引用。
        /// </summary>
        /// <typeparam name="T">引用类型。</typeparam>
        /// <param name="count">追加数量。</param>
        public static void Add<T>(int count) where T : class, IReference, new()
        {
            GetReferenceCollection(typeof(T)).Add<T>(count);
        }

        /// <summary>
        /// 从指定类型的引用池中移除指定数量的闲置引用（使用中的引用不受影响，被移除的对象直接丢弃）。
        /// 该类型从未被获取/追加过（集合不存在）时不创建空集合，直接返回。
        /// </summary>
        /// <typeparam name="T">引用类型。</typeparam>
        /// <param name="count">移除数量，超过闲置总数时移除全部闲置引用。</param>
        public static void RemoveUnused<T>(int count) where T : class, IReference
        {
            // 查询类接口一律走“不创建”的查找：否则仅查询就会隐式新建并登记空集合，
            // 使 ReferencePool.Count 与本次查询动作本身产生副作用。
            if (TryGetReferenceCollection(typeof(T), out var referenceCollection))
                referenceCollection.Remove(count);
        }

        /// <summary>
        /// 移除指定类型引用池中的所有闲置引用（使用中的引用不受影响，该类型条目保留在字典中）。
        /// 该类型从未被获取/追加过（集合不存在）时不创建空集合，直接返回。
        /// </summary>
        /// <typeparam name="T">引用类型。</typeparam>
        public static void RemoveAllUnused<T>() where T : class, IReference
        {
            if (TryGetReferenceCollection(typeof(T), out var referenceCollection))
                referenceCollection.RemoveAll();
        }

        /// <summary>
        /// 尝试获取指定类型下的引用信息集合，<b>不存在时不会创建</b>。
        /// 与 GetReferenceCollection 的区别仅在于“不存在时不登记新集合”：Acquire/Add/Recycle 这类
        /// 需要落地的操作仍必须用 GetReferenceCollection（不存在则创建），只有查询类接口使用本方法。
        /// </summary>
        /// <param name="refType">引用类型。</param>
        /// <param name="referenceCollection">引用信息集合，不存在时为 null。</param>
        /// <returns>是否存在该类型的引用信息集合。</returns>
        private static bool TryGetReferenceCollection(Type refType, out ReferenceCollection referenceCollection)
        {
            if (refType == null) throw new InvalidOperationException("[ReferencePool] 引用类型为空.");

            // 与 ReferencePool.cs 中的 m_ReferenceCollectionDict 同属一个 partial 类，可直接访问其私有字段
            lock (m_ReferenceCollectionDict)
            {
                return m_ReferenceCollectionDict.TryGetValue(refType, out referenceCollection);
            }
        }

        /// <summary>
        /// 获取所有引用池的信息。
        /// </summary>
        /// <returns>所有引用池的信息。</returns>
        public static ReferencePoolInfo[] GetAllReferencePoolInfos()
        {
            var index = 0;

            ReferencePoolInfo[] results;

            lock (m_ReferenceCollectionDict)
            {
                results = new ReferencePoolInfo[m_ReferenceCollectionDict.Count];
                foreach (var (type, refCollection) in m_ReferenceCollectionDict)
                {
                    results[index++] = new ReferencePoolInfo(type, refCollection.UnusedReferenceCount, refCollection.UsingReferenceCount,
                        refCollection.AcquireReferenceCount, refCollection.ReleaseReferenceCount,
                        refCollection.AddReferenceCount, refCollection.RemoveReferenceCount);
                }
            }

            return results;
        }
    }
}
