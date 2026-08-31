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
        /// 当前生命周期取消令牌。在途异步操作观察它并中止。
        /// </summary>
        public CancellationToken Token => m_Cts?.Token ?? default;

        /// <summary>
        /// 触发当前生命周期取消（不释放源，已注册的观察者仍可读到取消状态）。
        /// </summary>
        public void Cancel() => m_Cts?.Cancel();

        /// <summary>
        /// 开启新生命周期：取消并释放旧源，创建新 Token。
        /// 旧生命周期的在途操作据此识别（旧 Token 已取消）并中止，不再写回新生命周期。
        /// </summary>
        public void Recreate()
        {
            var old = m_Cts;
            m_Cts = new CancellationTokenSource();
            
            // 先取消再释放
            old?.Cancel(); 
            old?.Dispose();
        }

        /// <summary>
        /// 永久释放（对象不再复用时调用；复用对象用 Recreate 重建）。
        /// </summary>
        public void Dispose()
        {
            m_Cts?.Cancel();
            m_Cts?.Dispose();
            m_Cts = null;
        }
    }
}