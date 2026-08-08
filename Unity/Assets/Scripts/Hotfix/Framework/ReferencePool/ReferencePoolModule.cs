using System;
using System.Collections.Generic;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.ReferencePool
{
    /// <summary>
    /// 引用池管理模块。
    /// 功能：
    ///     1. 管理各引用类型下的引用集合字典。
    ///     2. 提供模块生命周期管理。
    /// </summary>
    public sealed partial class ReferencePoolModule : ModuleBase
    {
        /// <summary>
        /// 记录指定类型下的引用对象集合的字典, key:指定类型--Value:该类型下的引用对象信息集合
        /// </summary>
        private readonly Dictionary<Type, ReferenceCollection> m_ReferenceCollectionDict = new();

        /// <summary>
        /// 释放。
        /// </summary>
        protected internal override void OnDispose()
        {
            RemoveAllPools();
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
