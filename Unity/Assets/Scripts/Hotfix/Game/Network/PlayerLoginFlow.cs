using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Core;
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
		/// 进游戏事务取消源：事务 Token 供 <b>登录成功之后</b>的后续链使用（加载主场景、打开主界面等）——
		/// 这些步骤的寿命比触发它的选角窗口长（窗口在事务中途 Close，其 Token 随之取消），故不能沿用窗口令牌。
		/// 每次事务开始（EnterGameAsync）Recreate：新事务开启新生命周期，旧事务未完成的链路随之作废。
		/// </summary>
		private static readonly LifecycleCancellationSource sr_transactionCancellation = new();

		/// <summary>
		/// 进游戏事务令牌：登录成功后的后续异步链（场景加载等）统一使用，与窗口生命周期解耦。
		/// </summary>
		public static CancellationToken TransactionToken => sr_transactionCancellation.Token;

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
			sr_transactionCancellation.Recreate(); // 新事务开启新生命周期：旧事务未完成的后续链路随之取消

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