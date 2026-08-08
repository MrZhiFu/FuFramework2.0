using System;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.ReferencePool
{
    /// <summary>
    /// 引用池管理模块的公共 API。
    /// 功能：
    ///     1. 从引用池获取引用。
    ///     2. 将引用归还引用池。
    ///     3. 获取引用池的数量。
    /// </summary>
    public sealed partial class ReferencePoolModule : ModuleBase
    {
        /// <summary>
        /// 获取引用池的数量。
        /// </summary>
        // ReSharper disable once InconsistentlySynchronizedField
        public int Count => m_ReferenceCollectionDict.Count;

        /// <summary>
        /// 从引用池获取引用。
        /// </summary>
        /// <typeparam name="T">引用类型。</typeparam>
        /// <returns>引用。</returns>
        public T Acquire<T>() where T : class, IReference, new()
        {
            return GetReferenceCollection(typeof(T)).Acquire<T>();
        }

        /// <summary>
        /// 将引用归还引用池。
        /// </summary>
        /// <param name="reference">要归还的引用。</param>
        public void Recycle(IReference reference)
        {
            if (reference == null) throw new InvalidOperationException("[ReferencePoolModule] 要归还的引用对象为空.");

            var refType = reference.GetType();
            GetReferenceCollection(refType).Recycle(reference);
        }

        /// <summary>
        /// 向指定类型的引用池中追加指定数量的引用。
        /// </summary>
        /// <typeparam name="T">引用类型。</typeparam>
        /// <param name="count">追加数量。</param>
        public void Add<T>(int count) where T : class, IReference, new()
        {
            GetReferenceCollection(typeof(T)).Add<T>(count);
        }

        /// <summary>
        /// 从指定类型的引用池中移除指定数量的闲置引用（使用中的引用不受影响，被移除的对象直接丢弃）。
        /// </summary>
        /// <typeparam name="T">引用类型。</typeparam>
        /// <param name="count">移除数量，超过闲置总数时移除全部闲置引用。</param>
        public void RemoveUnused<T>(int count) where T : class, IReference
        {
            GetReferenceCollection(typeof(T)).Remove(count);
        }

        /// <summary>
        /// 移除指定类型引用池中的所有闲置引用（使用中的引用不受影响，该类型条目保留在字典中）。
        /// </summary>
        /// <typeparam name="T">引用类型。</typeparam>
        public void RemoveAllUnused<T>() where T : class, IReference
        {
            GetReferenceCollection(typeof(T)).RemoveAll();
        }

        /// <summary>
        /// 移除所有引用池：清空各类型的闲置引用并删除全部类型条目（引用池数量归零，使用中的引用不受影响）。
        /// </summary>
        public void RemoveAllPools()
        {
            lock (m_ReferenceCollectionDict)
            {
                foreach (var (_, refCollection) in m_ReferenceCollectionDict)
                {
                    refCollection.RemoveAll();
                }

                m_ReferenceCollectionDict.Clear();
            }
        }

        /// <summary>
        /// 获取所有引用池的信息。
        /// </summary>
        /// <returns>所有引用池的信息。</returns>
        public ReferencePoolInfo[] GetAllReferencePoolInfos()
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
