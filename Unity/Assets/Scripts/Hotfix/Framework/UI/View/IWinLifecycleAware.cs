// ReSharper disable once CheckNamespace 禁用命名空间检查

namespace Hotfix.Framework.UI
{
	/// <summary>
	/// 窗口生命周期广播接口：让窗口组件树上的组件感知开关生命周期——关闭时取消自身在途异步任务、
	/// 打开时重建自身令牌签发器。
	/// 触发点：由 WinBase._OnClose/_OnOpen 遍历 WinUI 组件树广播（晚于窗口自身 Cancel/Recreate），
	/// 实现方应在此取消/重建自持签发器；另可订阅 onAddedToStage/onRemovedFromStage
	/// 兜底列表项(GList item)进池/复用场景（窗口广播覆盖不到出池期间的 item）。
	/// </summary>
	public interface IWinLifecycleAware
	{
		/// <summary>
		/// 窗口关闭通知：实现方取消自身令牌签发器，在途异步处理器于业务 await 点静默中止。
		/// </summary>
		void OnWinClosed();

		/// <summary>
		/// 窗口打开通知：实现方重建自身令牌签发器，开启新生命周期（复用窗口的组件随此获得新令牌）。
		/// </summary>
		void OnWinOpened();
	}
}