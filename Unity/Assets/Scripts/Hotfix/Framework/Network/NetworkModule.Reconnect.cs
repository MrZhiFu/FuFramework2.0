using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 重连参数：目标地址 + 退避序列（数组长度即最大重试次数）。
	/// </summary>
	public sealed class ReconnectOptions
	{
		/// <summary>
		/// 目标服务器地址
		/// </summary>
		public Uri Address { get; set; }

		/// <summary>
		/// 逐次退避间隔（秒），数组长度即最大重试次数。默认 {2,3,5,8,13}s × 5 次。
		/// 实例化后请勿修改（ReconnectAsync 按索引读取）。
		/// </summary>
		public int[] BackoffScheduleSeconds { get; set; } = { 2, 3, 5, 8, 13 };
	}

	/// <summary>
	/// 网络管理模块——重连分部：退避重拨编排（销毁旧频道 → 重建 → ConnectAsync，直至拨通或重试耗尽）。
	/// 仅适配异常断线（Lost）后的自动重连；主动登出（Active）不应调用。
	/// </summary>
	public sealed partial class NetworkModule : ModuleBase
	{
		/// <summary>
		/// 按退避序列重拨：每轮先等退避间隔，再销毁旧频道、重建并拨号，拨通返回新频道。
		/// 重试全部失败后抛 <see cref="TimeoutException"/>（调用方决定收尾——弹失败框或放弃）。
		/// 取消语义：cancellationToken 取消时立即停止（不再发起新一轮），进行中的拨号随之取消。
		/// </summary>
		/// <param name="options">目标地址与退避序列</param>
		/// <param name="cancellationToken">取消令牌</param>
		/// <returns>拨通的新频道</returns>
		public async UniTask<INetworkChannel> ReconnectAsync(ReconnectOptions options, CancellationToken cancellationToken)
		{
			options.NotNull(nameof(options));
			options.Address.NotNull(nameof(options.Address));
			cancellationToken.ThrowIfCancellationRequested();

			for (var attempt = 0; attempt < options.BackoffScheduleSeconds.Length; attempt++)
			{
				var backoffSeconds = options.BackoffScheduleSeconds[attempt];
				await UniTask.Delay(backoffSeconds * 1000, cancellationToken: cancellationToken);
				cancellationToken.ThrowIfCancellationRequested();

				// 旧频道整个销毁（Dispose 走 Close(Active)，不触发重连），装配状态干净
				DestroyChannel();
				FuLogger.LogInfo($"重连第 {attempt + 1}/{options.BackoffScheduleSeconds.Length} 次（退避 {backoffSeconds}s）→ {options.Address}");

				try
				{
					return await ConnectAsync(options.Address, cancellationToken);
				}
				catch (OperationCanceledException)
				{
					throw; // 取消不吞，交给调用方处理
				}
				catch (Exception e)
				{
					FuLogger.LogWarning($"重连第 {attempt + 1} 次失败：{e.Message}");
					// 进入下一轮退避；次数耗尽则跳出循环按失败处理
				}
			}

			DestroyChannel();
			throw new TimeoutException($"重连失败：已重试 {options.BackoffScheduleSeconds.Length} 次（退避 {string.Join("/", options.BackoffScheduleSeconds)}s）。");
		}
	}
}