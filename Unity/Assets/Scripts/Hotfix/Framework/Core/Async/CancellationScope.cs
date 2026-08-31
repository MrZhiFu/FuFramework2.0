using System;
using System.Threading;
using Cysharp.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Core
{
    /// <summary>
    /// 可取消异步对象：实现者的所有异步操作随对象销毁而取消，且可被 await 等待清理完成。
    /// </summary>
    public interface ICancelAsync
    {
        /// <summary>
        /// 取消令牌。对象销毁（OnDispose/Dispose）时触发，在途操作观察它并中止。
        /// </summary>
        CancellationToken Token { get; }

        /// <summary>
        /// 触发取消并等待所有在途操作完成清理（释放句柄 + 卸载资源）后才返回。可重入、幂等。
        /// </summary>
        UniTask CancelAsync();
    }

    
    
    /// <summary>
    /// 取消范围登记：实现 ICancelAsync（可取消 + 可 await 排水等待），内部持有 CTS + 在途计数 + 「全部完成」TCS，
    /// 供模块/装载器组合复用。
    /// 每次生命周期重建（OnInit 新建），旧实例的 Token 已取消即标识旧生命周期。
    /// 主线程模型（PlayerLoop 驱动）下普通 ++/-- 即可，无需原子操作。
    /// </summary>
    public sealed class CancellationScope : ICancelAsync
    {
        /// <summary>
        /// 取消令牌源。Cancel/CancelAsync 触发取消，Token 供在途操作观察。
        /// </summary>
        private readonly CancellationTokenSource m_Cts = new();

        /// <summary>
        /// 在途操作计数。Begin 递增、BeginScope.Dispose 递减，归零表示全部在途操作已清理完毕。
        /// </summary>
        private int m_InFlightCount;

        /// <summary>
        /// 「全部完成」信号。在途计数归零时完成，唤醒等待 CancelAsync 的调用方；惰性创建。
        /// </summary>
        private UniTaskCompletionSource m_AllDoneTcs;

        /// <summary>
        /// 取消令牌。对象销毁（OnDispose/Dispose）时触发，在途操作观察它并中止。
        /// </summary>
        public CancellationToken Token => m_Cts.Token;

        /// <summary>
        /// 同步触发取消（供 OnDispose/Dispose 等同步销毁钩子调用）；取消清理等待由 CancelAsync 负责。
        /// </summary>
        public void Cancel() => m_Cts.Cancel();

        /// <summary>
        /// 触发取消并等待所有在途操作完成清理后才返回。可重入、幂等。
        /// </summary>
        public async UniTask CancelAsync()
        {
            m_Cts.Cancel();
            if (m_InFlightCount == 0) return;
            m_AllDoneTcs ??= new UniTaskCompletionSource();
            await m_AllDoneTcs.Task;
        }

        /// <summary>
        /// 在途操作入口调用；返回的 BeginScope 必须 Dispose（用 using）以归零计数。
        /// 返回 struct 而非 IDisposable 接口：using 直接调用 Dispose，零装箱零分配（勿通过 IDisposable 接口使用，否则装箱）。
        /// </summary>
        /// <returns>在途操作作用域，操作清理完成后必须 Dispose（用 using）。</returns>
        public BeginScope Begin()
        {
            m_InFlightCount++;
            return new BeginScope(this);
        }
        
        
        
        /// <summary>
        /// 在途操作作用域（struct 一次性释放器）。Dispose 时递减在途计数，归零时唤醒 CancelAsync 的等待。
        /// 共享同一 CancellationScope 引用，按值复制无堆分配。
        /// </summary>
        public readonly struct BeginScope : IDisposable
        {
            /// <summary>
            /// 所属的取消范围。Dispose 时经它递减在途计数并尝试完成「全部完成」信号。
            /// </summary>
            private readonly CancellationScope m_Owner;

            /// <summary>
            /// 创建在途操作作用域。
            /// </summary>
            /// <param name="owner">所属的取消范围。</param>
            internal BeginScope(CancellationScope owner)
            {
                m_Owner = owner;
            }

            /// <summary>
            /// 结束在途操作：递减所属范围的在途计数，归零时完成「全部完成」信号以唤醒 CancelAsync 的等待。
            /// 调用方应始终通过 using 释放本作用域，勿手动重复 Dispose。
            /// </summary>
            public void Dispose()
            {
                if (--m_Owner.m_InFlightCount == 0)
                {
                    m_Owner.m_AllDoneTcs?.TrySetResult();
                }
            }
        }
    }
}