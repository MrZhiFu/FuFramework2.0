using Cysharp.Threading.Tasks;
using Hotfix.Framework.Core;
using Hotfix.Framework.Network;
using Hotfix.Game.UI;
using Hotfix.Game.Manager_ToDelete;

namespace Hotfix.Game.Network
{
	/// <summary>
	/// 最小登出流程（【返回登录】/ 被顶号 / 主动登出共用）：
	/// 终止重连事务 → 清玩家本地状态 → 清推送缓冲 → 关频道（Active，不触发重连）→ 关全部窗口 → 回账号登录界面。
	/// </summary>
	public static class LogoutFlow
	{
		/// <summary>
		/// 执行登出。
		/// </summary>
		public static async UniTask LogoutAsync()
		{
			// 终止可能进行中的重连事务（否则断线判定可能抢先新建频道）
			ReconnectManager.Instance.CancelReconnect();

			// 清玩家本地状态（断线重连认领的玩家身份随之废弃）
			PlayerManager.Instance.PlayerInfo = null;

			// 清推送缓冲：旧会话脏数据不带到下一会话
			NetworkModule.Instance.ClearNotifyBuffer();

			// 关闭频道（Dispose → Close(Active)，不会触发自动重连）
			NetworkModule.Instance.DestroyChannel();

			// 关闭全部窗口（含主界面），回账号登录界面
			GlobalModule.UIModule.CloseAll();
			await GlobalModule.UIModule.OpenAsync<WinLogin>();
		}
	}
}