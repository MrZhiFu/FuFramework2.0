using System;
using System.Threading;
using Cysharp.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Core
{
    /// <summary>
    /// 取消范围簿记：内部持有 CTS + 在途计数 + 「全部完成」TCS，供模块/装载器组合复用。
    /// 每次生命周期重建（OnInit 新建），旧实例的 Token 已取消即标识旧生命周期。
    /// 主线程模型（PlayerLoop 驱动）下普通 ++/-- 即可，无需原子操作。
    /// </summary>
    public sealed class CancellationScope
    {
        private CancellationTokenSource m_Cts = new();
        private int m_InFlightCount;
        private UniTaskCompletionSource m_AllDoneTcs;

        /// <summary>
        /// 取消令牌。
        /// </summary>
        public CancellationToken Token => m_Cts.Token;

        /// <summary>
        /// 同步触发取消（供 OnDispose/Dispose 等同步销毁钩子调用）；排水等待由 CancelAsync 负责。
        /// </summary>
        public void Cancel() => m_Cts.Cancel();

        /// <summary>
        /// 触发取消并等待所有在途操作完成清理后才返回。可重入、幂等。
        /// </summary>
        public async UniTask CancelAsync()
        {
            m_Cts.Cancel();
            if (m_InFlightCount == 0) return; // 主线程模型，普通读即可
            m_AllDoneTcs ??= new UniTaskCompletionSource();
            await m_AllDoneTcs.Task;
        }

        /// <summary>
        /// 在途操作入口调用；返回的 BeginScope 必须 Dispose（用 using）以归零计数。
        /// 返回 struct 而非 IDisposable 接口：using 直接调用 Dispose，零装箱零分配（勿通过 IDisposable 接口使用，否则装箱）。
        /// </summary>
        public BeginScope Begin()
        {
            m_InFlightCount++; // 主线程模型，普通递增即可
            return new BeginScope(this);
        }

        /// <summary>
        /// 在途操作作用域（struct 一次性释放器）。Dispose 时递减在途计数，归零时唤醒 CancelAsync 的等待。
        /// 共享同一 CancellationScope 引用，按值复制无堆分配。
        /// </summary>
        public readonly struct BeginScope : IDisposable
        {
            private readonly CancellationScope m_Owner;
            internal BeginScope(CancellationScope owner) { m_Owner = owner; }

            public void Dispose()
            {
                if (--m_Owner.m_InFlightCount == 0) // 主线程模型，普通递减即可
                    m_Owner.m_AllDoneTcs?.TrySetResult();
            }
        }
    }
}
