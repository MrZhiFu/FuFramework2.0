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
        /// </summary>
        /// <param name="reference">要归还的引用。</param>
        public static void Recycle(IReference reference)
        {
            if (reference == null) throw new InvalidOperationException("[ReferencePool] 要归还的引用对象为空.");

            var refType = reference.GetType();
            GetReferenceCollection(refType).Recycle(reference);
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
        /// </summary>
        /// <typeparam name="T">引用类型。</typeparam>
        /// <param name="count">移除数量，超过闲置总数时移除全部闲置引用。</param>
        public static void RemoveUnused<T>(int count) where T : class, IReference
        {
            GetReferenceCollection(typeof(T)).Remove(count);
        }

        /// <summary>
        /// 移除指定类型引用池中的所有闲置引用（使用中的引用不受影响，该类型条目保留在字典中）。
        /// </summary>
        /// <typeparam name="T">引用类型。</typeparam>
        public static void RemoveAllUnused<T>() where T : class, IReference
        {
            GetReferenceCollection(typeof(T)).RemoveAll();
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
