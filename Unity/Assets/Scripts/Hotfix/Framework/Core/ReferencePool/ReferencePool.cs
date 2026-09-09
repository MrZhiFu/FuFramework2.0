using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Core
{
    /// <summary>
    /// 引用池。
    /// 功能：
    ///     1. 管理各引用类型下的引用集合字典。
    ///     2. 提供引用池的全局清理。
    /// </summary>
    public static partial class ReferencePool
    {
        /// <summary>
        /// 记录指定类型下的引用对象集合的字典, key:指定类型--Value:该类型下的引用对象信息集合
        /// </summary>
        private static readonly Dictionary<Type, ReferenceCollection> m_ReferenceCollectionDict = new();

        /// <summary>
        /// 移除所有引用池：清空各类型的闲置引用并删除全部类型条目（引用池数量归零，使用中的引用不受影响）。
        /// </summary>
        public static void ClearAll()
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
        /// 获取指定类型下的引用信息集合。
        /// </summary>
        /// <param name="refType">引用类型。</param>
        /// <returns>引用信息集合。</returns>
        private static ReferenceCollection GetReferenceCollection(Type refType)
        {
            if (refType == null) throw new InvalidOperationException("[ReferencePool] 引用类型为空.");

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
