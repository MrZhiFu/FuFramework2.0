using System;
using System.Threading;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Core
{
	/// <summary>
	/// 生命周期取消源：给"会反复开关/复用的对象"管令牌用的。
	/// 典型对象：UI 窗口——每次打开都是一段新生命，关闭时这段生命里发起的请求全部作废，下次打开再领新令牌。
	/// 怎么用：
	///     1. 对象持有本类实例（通常一个，readonly）；
	///     2. 开启新生命周期时调 Recreate()（旧令牌随即取消，在途请求自动中止）；
	///     3. 异步操作里 await 的地方传 Token——生命周期结束一取消，这些 await 各自中止；
	///     4. 对象彻底销毁时调 Dispose()。
	/// 与 CancellationScope 的区别：本类只管"发令牌、能取消、能换新"，不记录在途操作、
	/// 也不支持"等所有在途操作清完再继续"——需要那种排队能力的用 CancellationScope。
	/// 取消后的收尾（释放资源等）由被取消的操作自己在取消路径处理，本类不管。
	/// </summary>
	public sealed class LifecycleCancellationSource : IDisposable
	{
		/// <summary>
		/// 当前生命周期的取消令牌源（Recreate 重建、Dispose 释放后置空）。
		/// </summary>
		private CancellationTokenSource m_cts = new();

		/// <summary>
		/// 已释放状态下对外暴露的「已取消」令牌快照。
		/// CancellationTokenSource 被 Dispose 后再访问其 Token 会抛 ObjectDisposedException，
		/// 因此释放前先取出已取消的 Token 缓存于此，保证「Token 永不返回 default（永不取消语义）」。
		/// </summary>
		private CancellationToken m_releasedToken;

		/// <summary>
		/// 是否已 Dispose（幂等标记）。
		/// </summary>
		private bool m_released;

		/// <summary>
		/// 当前生命周期取消令牌。在途异步操作观察它并中止。
		/// 对象已释放时返回「已取消」的令牌，而非 default(CancellationToken)（后者永不取消，会让在途操作静默跑完）。
		/// </summary>
		public CancellationToken Token => m_released ? m_releasedToken : m_cts.Token;

		/// <summary>
		/// 触发当前生命周期取消（不释放源，已注册的观察者仍可读到取消状态）。
		/// </summary>
		public void Cancel()
		{
			if (m_released) return;
			m_cts?.Cancel();
		}

		/// <summary>
		/// 开启新生命周期：取消并释放旧源，创建新 Token。
		/// 旧生命周期的在途操作据此识别（旧 Token 已取消）并中止，不再写回新生命周期。
		/// 已 Dispose 的对象调用本方法会重新激活（兜底对象复用场景）。
		/// </summary>
		public void Recreate()
		{
			var old     = m_cts;
			m_cts       = new CancellationTokenSource();
			m_released  = false;

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
			if (m_released) return;
			m_released = true;

			if (m_cts == null) return;

			m_cts.Cancel();

			// 先取 Token 再释放源：源被 Dispose 后访问 Token 会抛 ObjectDisposedException
			m_releasedToken = m_cts.Token;
			m_cts.Dispose();
			m_cts = null;
		}
	}
}