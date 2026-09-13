using System;
using System.Threading;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Core
{
    /// <summary>
    /// 生命周期取消源：为「按生命周期重建取消令牌」的对象提供轻量 CTS 封装。
    /// 适用对象：UI 窗口、实体等一切「可发起异步请求且生命周期可复用/重建」的对象。
    /// 每次 Recreate 生成新 Token（旧 Token 已取消 = 旧生命周期），Cancel 触发当前生命周期取消。
    /// 与 CancellationScope 的区别：本类只提供「令牌 + 取消 + 重建」，不做在途计数与排水等待——
    /// 无需 await 清理的对象用它即可（取消清理由消费的异步 API 在取消路径负责）。
    /// </summary>
    public sealed class LifecycleCancellationSource : IDisposable
    {
        /// <summary>
        /// 当前生命周期的取消令牌源（Recreate 重建、Dispose 释放后置空）。
        /// </summary>
        private CancellationTokenSource m_Cts = new();

        /// <summary>
        /// 已释放状态下对外暴露的「已取消」令牌快照。
        /// CancellationTokenSource 被 Dispose 后再访问其 Token 会抛 ObjectDisposedException，
        /// 因此释放前先取出已取消的 Token 缓存于此，保证「Token 永不返回 default（永不取消语义）」。
        /// </summary>
        private CancellationToken m_ReleasedToken;

        /// <summary>
        /// 是否已 Dispose（幂等标记）。
        /// </summary>
        private bool m_Released;

        /// <summary>
        /// 当前生命周期取消令牌。在途异步操作观察它并中止。
        /// 对象已释放时返回「已取消」的令牌，而非 default(CancellationToken)（后者永不取消，会让在途操作静默跑完）。
        /// </summary>
        public CancellationToken Token => m_Released ? m_ReleasedToken : m_Cts.Token;

        /// <summary>
        /// 触发当前生命周期取消（不释放源，已注册的观察者仍可读到取消状态）。
        /// </summary>
        public void Cancel()
        {
            if (m_Released) return;
            m_Cts?.Cancel();
        }

        /// <summary>
        /// 开启新生命周期：取消并释放旧源，创建新 Token。
        /// 旧生命周期的在途操作据此识别（旧 Token 已取消）并中止，不再写回新生命周期。
        /// 已 Dispose 的对象调用本方法会重新激活（兜底对象复用场景）。
        /// </summary>
        public void Recreate()
        {
            var old     = m_Cts;
            m_Cts       = new CancellationTokenSource();
            m_Released  = false;

            // 先取消再释放
            old?.Cancel();
            old?.Dispose();
        }

        /// <summary>
        /// 永久释放（对象不再复用时调用；复用对象用 Recreate 重建）。可重入、幂等。
        /// 释放后 Token 仍返回一个「已取消」的令牌，不会退化为永不取消的 default 值。
        /// </summary>
        public void Dispose()
        {
            if (m_Released) return;
            m_Released = true;

            if (m_Cts == null) return;

            m_Cts.Cancel();

            // 先取 Token 再释放源：源被 Dispose 后访问 Token 会抛 ObjectDisposedException
            m_ReleasedToken = m_Cts.Token;
            m_Cts.Dispose();
            m_Cts = null;
        }
    }
}