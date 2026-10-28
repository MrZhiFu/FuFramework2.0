using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Network;
using Hotfix.Game.Proto;
using Hotfix.Game.Manager_ToDelete;

namespace Hotfix.Game.Network
{
	/// <summary>
	/// 进游戏登录事务（登录窗口与重连入口共用）：
	/// 挂起推送派发 → Call(ReqPlayerLogin) 建立全量基线 → 恢复派发。
	/// 失败收尾：清空缓冲并解除挂起（防脏数据回放 + 防后续会话"聋"），异常原样上抛。
	/// 重入防护：同一时间只允许一个登录事务。
	/// </summary>
	public static class PlayerLoginFlow
	{
		/// <summary>
		/// 是否正在执行登录事务（同一时间只允许一个登录事务）
		/// </summary>
		private static bool s_isEntering;

		/// <summary>
		/// 执行进游戏事务。
		/// </summary>
		/// <param name="playerId">角色 ID</param>
		/// <param name="cancellationToken">取消令牌（铁律 5：调用方生命周期持有者传入）</param>
		/// <returns>登录响应（含 PlayerInfo）</returns>
		public static async UniTask<RespPlayerLogin> EnterGameAsync(long playerId, CancellationToken cancellationToken)
		{
			if (s_isEntering) throw new InvalidOperationException("已有登录事务进行中，请勿重入。");
			s_isEntering = true;

			// 挂起推送派发：全量基线建立前的增量推送进缓冲
			NetworkModule.Instance.SuspendNotifyDispatch();

			try
			{
				var reqPlayerLogin  = new ReqPlayerLogin { Id = playerId };
				var respPlayerLogin = await NetworkModule.Instance.Call(reqPlayerLogin).AttachExternalCancellation(cancellationToken);
				PlayerManager.Instance.PlayerInfo = respPlayerLogin.PlayerInfo;

				// 基线就绪：恢复派发，缓冲推送按到达顺序回放
				NetworkModule.Instance.ResumeNotifyDispatch();
				return respPlayerLogin;
			}
			catch
			{
				// 失败：基线未建立，缓冲中的增量全是脏数据——丢弃且解除挂起，保证下一个会话派发状态干净
				NetworkModule.Instance.ClearNotifyBuffer();
				NetworkModule.Instance.ResumeNotifyDispatch();
				throw;
			}
			finally
			{
				s_isEntering = false;
			}
		}
	}
}