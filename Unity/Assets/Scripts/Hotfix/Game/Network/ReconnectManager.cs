using System;
using Cysharp.Threading.Tasks;
using AOT.Framework.Core.Log;
using Hotfix.Framework.Core;
using Hotfix.Framework.Event;
using Hotfix.Framework.Network;
using Hotfix.Game.UI;
using Hotfix.Game.Proto;
using Hotfix.Game.Manager_ToDelete;

namespace Hotfix.Game.Network
{
	/// <summary>
	/// 网络重连编排者（游戏侧）：监听断线事件分流 + 被顶标记 + 重连事务入口。
	/// 分流顺序（敏感）：
	///     非 Lost（主动关闭）→ 忽略；未进游戏（PlayerInfo 为空）→ 忽略；
	///     被顶标记 → 弹提示回登录（不自动重连）；
	///     否则 → 启动重连事务（StartReconnectFlow，Task 7 填充完整实现）。
	/// </summary>
	public class ReconnectManager : Singleton<ReconnectManager>
	{
		/// <summary>
		/// 被顶标记：收到 NotifyPrompt(Type=5) 置位，随后到达的 Closed(Lost) 不触发自动重连（防顶号乒乓）
		/// </summary>
		private bool m_isKickedByTopLogin;

		/// <summary>
		/// 被顶提示内容（服务器随 NotifyPrompt 下发，兜底文案见下）
		/// </summary>
		private string m_kickContent = string.Empty;

		/// <summary>
		/// 是否正处于重连事务中（重入防护：重连过程中再次断线不再开新事务）
		/// </summary>
		private bool m_isReconnecting;

		/// <summary>
		/// 重连事务取消源（Task 7 的 StartReconnectFlow 使用其 Token；CancelReconnect 供登出/取消按钮调用）
		/// </summary>
		private readonly LifecycleCancellationSource m_reconnectCancellation = new();

		/// <summary>
		/// 是否已初始化（Initialize 幂等标记，防重复订阅）
		/// </summary>
		private bool m_initialized;

		/// <summary>
		/// 初始化：订阅网络关闭事件（登出/销毁之外的异常断线由此进入重连分流）。
		/// 由 HotfixLauncher 在进入游戏时调用；幂等——重复初始化不重复订阅（防双重回调）。
		/// </summary>
		public void Initialize()
		{
			if (m_initialized) return;
			m_initialized = true;
			GlobalModule.EventModule.Subscribe(NetworkClosedEventArgs.EventId, OnNetworkClosed);
			FuLogger.LogInfo(nameof(ReconnectManager) + " 已初始化。");
		}

		/// <summary>
		/// 取消进行中的重连事务（登出流程 / 转圈框取消按钮调用）
		/// </summary>
		public void CancelReconnect() => m_reconnectCancellation.Cancel();

		#region 事件处理

		/// <summary>
		/// 被顶提示：Type=5（弹窗并回登录）。仅置标记，随后的 Closed 由 OnNetworkClosed 读取标记分流。
		/// 其余 Type（提示/跑马灯等）本模块不处理（留给专门的提示系统）。
		/// </summary>
		[NotifyMessageHandler(typeof(NotifyPrompt), nameof(OnNotifyPrompt))]
		internal void OnNotifyPrompt(NotifyPrompt msg)
		{
			if (msg.Type != 5) return;
			m_isKickedByTopLogin = true;
			m_kickContent        = msg.Content ?? string.Empty;
		}

		/// <summary>
		/// 网络关闭事件分流（见类注释的顺序）
		/// </summary>
		private void OnNetworkClosed(object sender, GameEventArgs e)
		{
			if (e is not NetworkClosedEventArgs ne) return;
			if (ne.CloseReason                    != ENetworkCloseReason.Lost) return; // 主动关闭（登出/销毁）不重连
			if (PlayerManager.Instance.PlayerInfo.Id <= 0) return;                     // 未进游戏（登录期）不自动重连，由按钮兜底（PlayerInfo 由构造初始化为空对象，判空无效，用 Id 判定"已登录"）

			if (m_isKickedByTopLogin)
			{
				// 被顶：不自动重连，弹提示回登录界面
				m_isKickedByTopLogin = false;

				var content = string.IsNullOrEmpty(m_kickContent)
					? "您的账号已在其他设备登录。"
					: m_kickContent;

				m_kickContent = string.Empty;

				ShowKickedPrompt(content);
				GameSessionFlow.LogoutAsync().Forget();
				return;
			}

			if (m_isReconnecting) return; // 重入防护：已有重连事务在跑
			_ = StartReconnectFlow(new Uri($"tcp://{WinPlayerList.serverIp}:{WinPlayerList.serverPort}"));
		}

		#endregion

		/// <summary>
		/// 启动重连事务：重开取消源 → 弹"重连中"转圈框 → ReconnectAsync 退避重拨 →
		/// 拨通后静默重登（PlayerLoginFlow）→ 关全部窗口回主界面原地复活；
		/// 失败 → "重连失败"框（再试一次=从拨号重头 / 返回登录）；取消 → 走登出。
		/// 重入防护：m_isReconnecting 在分流处检查；本方法负责置位/复位。
		/// </summary>
		private async UniTask StartReconnectFlow(Uri address)
		{
			m_isReconnecting = true;
			m_reconnectCancellation.Recreate(); // 每轮新 Token：CancelReconnect 只取消本轮
			try
			{
				var playerId = PlayerManager.Instance.PlayerInfo.Id; // 断线未登出，PlayerInfo 仍在

				await GlobalModule.UIModule.OpenAsync<WinReconnecting>();   // 转圈框在所有出口经 CloseAll 关闭，无需持有引用

				try
				{
					await NetworkModule.Instance.ReconnectAsync(new ReconnectOptions { Address = address }, m_reconnectCancellation.Token);
					await GameSessionFlow.EnterGameAsync(playerId, m_reconnectCancellation.Token);

					// 原地复活：关全部窗口（含转圈框）重建主界面——不弹回选角界面
					GlobalModule.UIModule.CloseAll();
					await GlobalModule.UIModule.OpenAsync<WinMain>();
				}
				catch (OperationCanceledException)
				{
					// 玩家取消（转圈框取消按钮）/ 登出竞态：回登录界面
					await GameSessionFlow.LogoutAsync();
				}
				catch (Exception)
				{
					// 重连失败（退避耗尽或重登失败）：交玩家手选
					GlobalModule.UIModule.CloseAll();
					var dialogData = new WinDialogMessageBox.DialogMessageBoxData("重连失败，请重试或返回登录。",
					                                                              () => StartReconnectFlow(address).Forget(), // 再试一次：重头再来，计数天然重置
					                                                              () => GameSessionFlow.LogoutAsync().Forget());   // 返回登录
					await GlobalModule.UIModule.OpenAsync<WinDialogMessageBox>(dialogData);
				}
			}
			finally
			{
				m_isReconnecting = false;
			}
		}

		/// <summary>
		/// 被顶提示框：确认即回登录（复用现成两按钮对话框，确认按钮=返回登录，取消按钮=仅关窗）
		/// </summary>
		private void ShowKickedPrompt(string content)
		{
			// 被顶是强制离开：仅显示「确定」单按钮，"回登录"是唯一出口（取消只关窗会留玩家在断线死局）
			var dialogData = new WinDialogMessageBox.DialogMessageBoxData(content, () => GameSessionFlow.LogoutAsync().Forget(), null, WinDialogMessageBox.EBtnType.ShowOk);
			GlobalModule.UIModule.Open<WinDialogMessageBox>(dialogData);
		}
	}
}