using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Core;
using Hotfix.Framework.Network;
using Hotfix.Framework.Scene;
using Hotfix.Game.Proto;
using Hotfix.Game.UI;
using Hotfix.Game.Manager_ToDelete;
using UtilityAOT = AOT.Framework.Core.Utility.UtilityAOT;

namespace Hotfix.Game.Network
{
	/// <summary>
	/// 游戏会话流：会话建立（进游戏）与销毁（登出）的对称操作，登录窗口与重连编排共用。
	/// 进游戏：挂起推送派发 → Call(ReqPlayerLogin) 建立全量基线 → 恢复派发；失败收尾（清缓冲+解除挂起）在内，异常上抛。
	/// 登出：终止重连 → 清玩家状态 → 清推送缓冲 → 关频道（Active，不触发自动重连）→ 卸载主场景 → 关全部窗口 → 回登录界面。
	/// </summary>
	public static class GameSessionFlow
	{
		/// <summary>
		/// 是否正在执行登录事务（同一时间只允许一个登录事务）
		/// </summary>
		private static bool s_isEntering;

		/// <summary>
		/// 进游戏事务取消源：事务 Token 供 <b>登录成功之后</b>的后续链使用（加载主场景、打开主界面等）——
		/// 这些步骤的寿命比触发它的选角窗口长（窗口在事务中途 Close，其 Token 随之取消），故不能沿用窗口令牌。
		/// 每次事务开始（LoginFlowAsync）Recreate：新事务开启新生命周期，旧事务未完成的链路随之作废。
		/// </summary>
		private static readonly LifecycleCancellationSource sr_transactionCancellation = new();

		/// <summary>
		/// 进游戏事务令牌：登录成功后的后续异步链（场景加载等）统一使用，与窗口生命周期解耦。
		/// </summary>
		public static CancellationToken TransactionToken => sr_transactionCancellation.Token;

		/// <summary>
		/// 进游戏：执行登录事务。
		/// </summary>
		/// <param name="playerId">角色 ID</param>
		/// <param name="cancellationToken">取消令牌（铁律 5：调用方生命周期持有者传入）</param>
		/// <returns>登录响应（含 PlayerInfo）</returns>
		public static async UniTask<RespPlayerLogin> LoginFlowAsync(long playerId, CancellationToken cancellationToken)
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

		/// <summary>
		/// 登出：终止重连 → 清玩家状态 → 清推送缓冲 → 关频道（Active，不触发自动重连）→ 卸载主场景 → 关全部窗口 → 回登录界面。
		/// 【返回登录】/ 被顶号 / 主动登出共用。
		/// </summary>
		public static async UniTask LogoutFlowAsync()
		{
			// 终止可能进行中的重连事务（否则断线判定可能抢先新建频道）
			ReconnectManager.Instance.CancelReconnect();

			// 清玩家本地状态（断线重连认领的玩家身份随之废弃）
			PlayerManager.Instance.PlayerInfo = null;

			// 清推送缓冲：旧会话脏数据不带到下一会话
			NetworkModule.Instance.ClearNotifyBuffer();

			// 关闭频道（Dispose → Close(Active)，不会触发自动重连）
			NetworkModule.Instance.DestroyChannel();

			// 卸载主场景：登出回到"未进游戏"状态。不卸载则再登录时 LoadScene 因"已加载"直接抛异常；
			// 幂等防御：场景未加载（异常时序）时跳过，UnloadScene 对未加载场景同样抛异常。
			var mainScenePath = UtilityAOT.AssetPath.GetScenePath("Main");
			if (SceneModule.Instance.IsLoaded(mainScenePath))
				SceneModule.Instance.UnloadScene(mainScenePath);

			// 关闭全部窗口（含主界面），回账号登录界面
			GlobalModule.UIModule.CloseAll();

			// 打开账号登录界面
			await GlobalModule.UIModule.OpenAsync<WinLogin>();
		}
	}
}