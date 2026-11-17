using System;
using System.Threading;
using Cysharp.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Core
{
	/// <summary>
	/// 可取消异步对象：实现者的所有异步操作随对象销毁而取消，且可以"等清理完再继续"。
	/// </summary>
	public interface ICancelAsync
	{
		/// <summary>
		/// 取消令牌。对象销毁（OnDispose/Dispose）时触发，在途操作观察它并中止。
		/// </summary>
		CancellationToken Token { get; }

		/// <summary>
		/// 取消所有在途操作，并等它们都收尾完成后才返回。
		/// 用途：销毁/重启前必须确认"旧操作都收拾干净了"才能继续的场景。可重复调用、重复调用无副作用。
		/// </summary>
		UniTask CancelAsync();
	}



	/// <summary>
	/// 取消范围：一个能"记账"的取消源——它记住当前有多少个异步操作正在跑，
	/// 并且支持"取消之后，等这些操作全部收尾完毕再返回"。
	/// 典型场景是框架重启：重启前必须等各模块把在途的加载、请求都收拾完，才能安全重建。
	/// 这时每个模块持有本类，重启流程逐个 await 模块的 CancelAsync。
	/// 怎么用：
	///     1. 模块持有本类实例，发起异步操作时用 using (m_scope.Begin()) 包住——开始时计数 +1，结束自动 -1；
	///     2. 销毁/重启时调 CancelAsync()——先触发取消（在途操作各自中止），再等计数归零才返回；
	///     3. 生命周期重建用"新建实例"表达（旧实例的 Token 已取消 = 旧生命周期）。
	/// 与 CancellationTokenIssuer 怎么选：窗口开关那种"取消就走、不用等谁"的场景用 Issuer 就够；
	/// 只有"取消之后还要等全部收完才能往下走"的场景才需要本类。
	/// 计数用普通 int：主线程单线程模型，无需原子操作。
	/// </summary>
	public sealed class CancellationBarrier : ICancelAsync
	{
		/// <summary>
		/// 取消令牌源。Cancel/CancelAsync 触发取消，Token 供在途操作观察。
		/// </summary>
		private readonly CancellationTokenSource m_cts = new();

		/// <summary>
		/// 「全部完成」信号。在途计数归零时完成并立即置空（复位），唤醒等待 CancelAsync 的调用方；惰性创建。
		/// 必须复位：UniTaskCompletionSource 完成后 await 会立即返回，
		/// 若不复位，二次 CancelAsync（此时又有了在途操作）会立刻返回、排水失效。
		/// </summary>
		private UniTaskCompletionSource m_allDoneTcs;

		/// <summary>
		/// 在途操作计数。Begin 递增、BeginScope.Dispose 递减，归零表示全部在途操作已清理完毕。
		/// </summary>
		private int m_inFlightCount;

		/// <summary>
		/// 取消令牌。对象销毁（OnDispose/Dispose）时触发，在途操作观察它并中止。
		/// </summary>
		public CancellationToken Token => m_cts.Token;

		/// <summary>
		/// 同步触发取消（供 OnDispose/Dispose 等同步销毁钩子调用）；取消清理等待由 CancelAsync 负责。
		/// </summary>
		public void Cancel() => m_cts.Cancel();

		/// <summary>
		/// 触发取消并等待所有在途操作完成清理后才返回。可重入、幂等。
		/// </summary>
		public async UniTask CancelAsync()
		{
			m_cts.Cancel();
			if (m_inFlightCount == 0) return;

			// 归零时 m_allDoneTcs 已被置空，此处按需新建；已完成/已取消的 TCS 不得重复等待
			m_allDoneTcs ??= new UniTaskCompletionSource();
			await m_allDoneTcs.Task;
		}

		/// <summary>
		/// 在途操作入口调用；返回的 BeginScope 必须 Dispose（用 using）以归零计数。
		/// 返回 struct 而非 IDisposable 接口：using 直接调用 Dispose，零装箱零分配（勿通过 IDisposable 接口使用，否则装箱）。
		/// </summary>
		/// <returns>在途操作作用域，操作清理完成后必须 Dispose（用 using）。</returns>
		public BeginScope Begin()
		{
			m_inFlightCount++;
			return new BeginScope(this);
		}



		/// <summary>
		/// 在途操作作用域（struct 一次性释放器）。Dispose 时递减在途计数，归零时唤醒 CancelAsync 的等待。
		/// 共享同一 CancellationBarrier 引用，按值复制无堆分配。
		/// </summary>
		public struct BeginScope : IDisposable
		{
			/// <summary>
			/// 所属的取消范围。Dispose 时经它递减在途计数并尝试完成「全部完成」信号。
			/// </summary>
			private readonly CancellationBarrier m_owner;

			/// <summary>
			/// 是否已释放（幂等标记）：防止重复 Dispose 把在途计数减成负数，导致后续 CancelAsync 永久挂起。
			/// </summary>
			private bool m_disposed;

			/// <summary>
			/// 创建在途操作作用域。
			/// </summary>
			/// <param name="owner">所属的取消范围。</param>
			internal BeginScope(CancellationBarrier owner)
			{
				m_owner    = owner;
				m_disposed = false;
			}

			/// <summary>
			/// 结束在途操作：递减所属范围的在途计数，归零时完成「全部完成」信号以唤醒 CancelAsync 的等待。
			/// 可重入、幂等（重复 Dispose 无效）；调用方应始终通过 using 释放本作用域。
			/// </summary>
			public void Dispose()
			{
				if (m_disposed || m_owner == null) return;
				m_disposed = true;

				if (--m_owner.m_inFlightCount == 0)
				{
					// 先取下引用并复位，再完成信号：完成回调里若再次 CancelAsync/Begin 也能拿到干净状态
					var allDoneTcs = m_owner.m_allDoneTcs;
					m_owner.m_allDoneTcs = null;
					allDoneTcs?.TrySetResult();
				}
			}
		}
	}
}