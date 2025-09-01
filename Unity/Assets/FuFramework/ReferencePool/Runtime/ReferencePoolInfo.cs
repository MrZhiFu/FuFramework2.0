using System;
using System.Runtime.InteropServices;

// ReSharper disable once CheckNamespace
namespace FuFramework.ReferencePool.Runtime
{
    /// <summary>
    /// 引用池信息。
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public readonly struct ReferencePoolInfo
    {
        /// 引用池类型。
        public Type Type { get; }

        /// 闲置未使用的引用数量。
        public int UnusedReferenceCount { get; }

        /// 正在使用的引用数量。(从引用池中获取的 + 引用池中不存在时new创建的引用数量 - 释放归还的引用数量)
        public int UsingReferenceCount { get; }

        /// 已获取的引用数量。(从引用池中获取的 + 引用池中不存在时new创建的引用数量）
        public int AcquireReferenceCount { get; }

        /// 释放(归还)的引用数量。
        public int ReleaseReferenceCount { get; }

        /// 新增的引用数量。
        public int AddReferenceCount { get; }

        /// 被移除的引用数量。
        public int RemoveReferenceCount { get; }

        /// <summary>
        /// 初始化引用池信息的新实例。
        /// </summary>
        /// <param name="type">引用池类型。</param>
        /// <param name="unusedReferenceCount">未使用引用数量。</param>
        /// <param name="usingReferenceCount">正在使用引用数量。</param>
        /// <param name="acquireReferenceCount">已获取的引用数量。</param>
        /// <param name="releaseReferenceCount">归还的引用数量。</param>
        /// <param name="addReferenceCount">增加的引用数量。</param>
        /// <param name="removeReferenceCount">移除的引用数量。</param>
        public ReferencePoolInfo(Type type, int unusedReferenceCount, int usingReferenceCount, int acquireReferenceCount, int releaseReferenceCount, int addReferenceCount, int removeReferenceCount)
        {
            Type                  = type;
            AddReferenceCount     = addReferenceCount;
            UsingReferenceCount   = usingReferenceCount;
            UnusedReferenceCount  = unusedReferenceCount;
            RemoveReferenceCount  = removeReferenceCount;
            AcquireReferenceCount = acquireReferenceCount;
            ReleaseReferenceCount = releaseReferenceCount;
        }
    }
}