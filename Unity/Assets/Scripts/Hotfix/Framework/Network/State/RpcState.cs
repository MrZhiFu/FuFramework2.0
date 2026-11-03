using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// RPC 配对状态：按 UniqueId 登记等待中的请求，TryReply 配对回包唤醒 await，Update 超时清理，Dispose 终结全部挂起。
	/// </summary>
	internal sealed class RpcState : IDisposable
	{
		/// <summary>
		/// 等待回复的请求字典（UniqueId → 等待载体 RpcMessageData）
		/// </summary>
		private readonly ConcurrentDictionary<long, RpcMessageData> m_waitingReplyDict = new();

		/// <summary>
		/// 因超时待移除的请求 ID 列表（Update 中先收集、后统一从等待字典移除）
		/// </summary>
		private readonly HashSet<long> m_timeoutReplyIdSet = new();

		/// <summary>
		/// RPC 超时时间。单位毫秒
		/// </summary>
		private readonly int m_rpcTimeout;

		/// <summary>
		/// 自上次消费以来是否发生过 RPC 超时（供频道联动立即探活；读取即清零）
		/// </summary>
		private bool m_hasTimeout;

		/// <summary>
		/// 读取并清零"发生过 RPC 超时"标志（频道据此立即拉满心跳流逝，下一帧补发心跳体检链路，不等下一个心跳周期）
		/// </summary>
		public bool ConsumeTimeoutFlag()
		{
			var has = m_hasTimeout;
			m_hasTimeout = false;
			return has;
		}

		/// <summary>
		/// 创建 RPC 状态（超时取 <see cref="ChannelConst.RPC_TIMEOUT_DEFAULT"/>）
		/// </summary>
		public RpcState()
		{
			m_rpcTimeout = ChannelConst.RPC_TIMEOUT_DEFAULT;
			if (m_rpcTimeout < ChannelConst.RPC_TIMEOUT_MIN)
			{
				throw new InvalidOperationException("RPC 超时配置低于下限（ChannelConst.RPC_TIMEOUT_MIN）");
			}
		}

		/// <summary>
		/// 终结所有挂起的 RPC 请求。
		/// 注意：本方法可重复调用（断线后重连会再次触发）。
		/// 断线/销毁时必须让所有挂起的请求以异常结束，否则 await 会永久悬挂（原实现只做了字典 Clear）。
		/// </summary>
		public void Dispose()
		{
			foreach (var (uniqueId, rpcMessageData) in m_waitingReplyDict)
			{
				rpcMessageData.Cancel();
#if UNITY_EDITOR
				// 网络调试面板终点回写：断线/销毁取消的请求记录状态置为取消；
				// 字典 Key 虽为 long，但值源自 int UniqueId，显式收窄无损
				NetworkDebug.OnRpcSettled((int)uniqueId, ENetworkMessageStatus.Canceled, rpcMessageData.ElapseTime);
#endif
			}

			m_waitingReplyDict.Clear();
			m_timeoutReplyIdSet.Clear();
		}

		/// <summary>
		/// 处理 RPC 回复消息：按 UniqueId 配对等待中的请求并唤醒其 await。
		/// </summary>
		/// <param name="msgObj">要处理的消息对象，必须实现IResponseMessage接口。</param>
		/// <returns>如果成功处理回复消息，则返回true；否则返回false。</returns>
		public bool TryReply(MessageObject msgObj)
		{
			if (msgObj is not IResponseMessage responseMessage) return false;
			if (!m_waitingReplyDict.TryRemove(msgObj.UniqueId, out var rpcMessageData)) return false;

#if UNITY_EDITOR
			// 网络调试面板终点回写：请求记录状态置为已回并记录耗时
			NetworkDebug.OnRpcSettled(msgObj.UniqueId, ENetworkMessageStatus.Replied, rpcMessageData.ElapseTime);
#endif

			rpcMessageData.Reply(responseMessage);
			return true;
		}

		/// <summary>
		/// 调用RPC并等待返回结果。
		/// 此方法会发送一个请求消息并返回一个任务，任务将在收到响应时完成。
		/// 可能会引发超时异常。
		/// </summary>
		/// <param name="msgObj">要发送的消息对象，必须实现IRequestMessage接口。</param>
		/// <returns>返回一个任务，该任务在收到响应时完成，并返回IResponseMessage。</returns>
		public UniTask<IResponseMessage> Call(MessageObject msgObj)
		{
			if (m_waitingReplyDict.TryGetValue(msgObj.UniqueId, out var existing))
			{
				return existing.Task;
			}

			var rpcMessageData = RpcMessageData.Create(msgObj as IRequestMessage);
			m_waitingReplyDict.TryAdd(msgObj.UniqueId, rpcMessageData);
			return rpcMessageData.Task;
		}

		/// <summary>
		/// 逻辑更新，处理消息计时和超时移除消息
		/// </summary>
		/// <param name="deltaTime"></param>
		public void Update(float deltaTime)
		{
			// 处理等待回复的消息
			if (m_waitingReplyDict.Count > 0)
			{
				var elapseSecondsTime = (long)(deltaTime * 1000);
				m_timeoutReplyIdSet.Clear();
				foreach (var (id, rpcMessageData) in m_waitingReplyDict)
				{
					var isTimeout = rpcMessageData.IncrementalElapseTime(elapseSecondsTime);
					if (!isTimeout) continue;
					m_timeoutReplyIdSet.Add(id);
				}
			}

			// 移除超时任务
			if (m_timeoutReplyIdSet.Count > 0)
			{
				foreach (var objectId in m_timeoutReplyIdSet)
				{
#if UNITY_EDITOR
					// 网络调试面板终点回写：请求记录状态置为超时（移除前取值，避免竞态）；
					// 字典 Key 虽为 long，但值源自 int UniqueId，显式收窄无损
					if (m_waitingReplyDict.TryGetValue(objectId, out var timedOutData))
					{
						NetworkDebug.OnRpcSettled((int)objectId, ENetworkMessageStatus.Timeout, timedOutData.ElapseTime);
					}
#endif
					m_waitingReplyDict.TryRemove(objectId, out _);
				}

				m_timeoutReplyIdSet.Clear();
				m_hasTimeout = true; // 超时可能意味着链路异常：通知频道立即探活
			}
		}
	}
}