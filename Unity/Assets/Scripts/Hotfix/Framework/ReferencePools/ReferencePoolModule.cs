using System;
using System.Collections.Generic;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.ReferencePools
{
    /// <summary>
    /// 引用池管理模块。
    /// 功能：
    ///     1. 从引用池获取引用。
    ///     2. 将引用归还引用池。
    ///     3. 获取引用池的数量。
    /// </summary>
    public sealed partial class ReferencePoolModule : ModuleBase
    {
        /// <summary>
        /// 记录指定类型下的引用对象集合的字典, key:指定类型--Value:该类型下的引用对象信息集合
        /// </summary>
        private readonly Dictionary<Type, ReferenceCollection> m_ReferenceCollectionDict = new();

        /// <summary>
        /// 获取引用池的数量。
        /// </summary>
        // ReSharper disable once InconsistentlySynchronizedField
        public int Count => m_ReferenceCollectionDict.Count;

        /// <summary>
        /// 释放。
        /// </summary>
        protected internal override void OnDispose()
        {
            ClearAll();
        }

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
        public void Release(IReference reference)
        {
            if (reference == null) throw new InvalidOperationException("[ReferencePoolModule] 要归还的引用对象为空.");

            var refType = reference.GetType();
            GetReferenceCollection(refType).Release(reference);
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
        /// 从指定类型的引用池中移除指定数量的引用。
        /// </summary>
        /// <typeparam name="T">引用类型。</typeparam>
        /// <param name="count">移除数量。</param>
        public void Remove<T>(int count) where T : class, IReference
        {
            GetReferenceCollection(typeof(T)).Remove(count);
        }

        /// <summary>
        /// 从指定类型的引用池中移除所有的引用。
        /// </summary>
        /// <typeparam name="T">引用类型。</typeparam>
        public void RemoveAll<T>() where T : class, IReference
        {
            GetReferenceCollection(typeof(T)).RemoveAll();
        }

        /// <summary>
        /// 清除所有引用池。
        /// </summary>
        public void ClearAll()
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

        /// <summary>
        /// 获取指定类型下的引用信息集合。
        /// </summary>
        /// <param name="refType">引用类型。</param>
        /// <returns>引用信息集合。</returns>
        private ReferenceCollection GetReferenceCollection(Type refType)
        {
            if (refType == null) throw new InvalidOperationException("[ReferencePoolModule] 引用类型为空.");

            ReferenceCollection referenceCollection;
            lock (m_ReferenceCollectionDict)
            {
                if (m_ReferenceCollectionDict.TryGetValue(refType, out referenceCollection)) return referenceCollection;
                referenceCollection = new ReferenceCollection(refType);
                m_ReferenceCollectionDict.Add(refType, referenceCollection);
            }

            return referenceCollection;
        }
    }
}
