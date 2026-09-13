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
        /// 移除所有引用池中的闲置引用（保留类型条目与计数，使用中的引用不受影响）。
        /// </summary>
        /// <remarks>
        /// 保留类型条目与计数、仅释放闲置引用；不取消任何在途异步，也不回收使用中的引用。
        /// 因此本方法不依赖调用时机：类型条目与其「使用中」计数被保留（而非连条目一起清空），
        /// 在途异步结束后的迟到 Recycle 仍会落回原集合并与该计数自洽，不会落到重建的零计数空集合上而误报「使用计数已为零」，
        /// 也就不会把异常逃逸到无 try/catch 的 <c>ModuleManager.Update</c> 中断整帧。
        /// </remarks>
        public static void ClearAll()
        {
            lock (m_ReferenceCollectionDict)
            {
                foreach (var (_, refCollection) in m_ReferenceCollectionDict)
                {
                    refCollection.RemoveAll();
                }
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
