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
		/// 创建 RPC 状态
		/// </summary>
		/// <param name="timeout">RPC 超时时间。单位毫秒，不能小于 3000</param>
		public RpcState(int timeout)
		{
			m_rpcTimeout = timeout;
			if (m_rpcTimeout < 3000)
			{
				throw new ArgumentOutOfRangeException(nameof(timeout), "RPC超时时间不能小于3000毫秒");
			}
		}

		/// <summary>
		/// 终结所有挂起的 RPC 请求。
		/// 注意：本方法可重复调用（断线后重连会再次触发）。
		/// 断线/销毁时必须让所有挂起的请求以异常结束，否则 await 会永久悬挂（原实现只做了字典 Clear）。
		/// </summary>
		public void Dispose()
		{
			foreach (var (_, rpcMessageData) in m_waitingReplyDict)
			{
				rpcMessageData.Cancel();
			}

			m_waitingReplyDict.Clear();
			m_timeoutReplyIdSet.Clear();
		}

		/// <summary>
		/// 处理 RPC 回复消息：按 UniqueId 配对等待中的请求并唤醒其 await。
		/// </summary>
		/// <param name="message">要处理的消息对象，必须实现IResponseMessage接口。</param>
		/// <returns>如果成功处理回复消息，则返回true；否则返回false。</returns>
		public bool TryReply(MessageObject message)
		{
			// 非响应消息（Notify/Req）直接跳过配对：模式匹配编译为 isinst 指令，零分配，无需类型缓存
			if (message is not IResponseMessage responseMessage) return false;
			if (!m_waitingReplyDict.TryRemove(message.UniqueId, out var rpcMessageData)) return false;

			rpcMessageData.Reply(responseMessage);
			return true;
		}

		/// <summary>
		/// 调用RPC并等待返回结果。
		/// 此方法会发送一个请求消息并返回一个任务，任务将在收到响应时完成。
		/// 可能会引发超时异常。
		/// </summary>
		/// <param name="messageObject">要发送的消息对象，必须实现IRequestMessage接口。</param>
		/// <returns>返回一个任务，该任务在收到响应时完成，并返回IResponseMessage。</returns>
		public UniTask<IResponseMessage> Call(MessageObject messageObject)
		{
			if (m_waitingReplyDict.TryGetValue(messageObject.UniqueId, out var existing))
			{
				return existing.Task;
			}

			var rpcMessageData = RpcMessageData.Create(messageObject as IRequestMessage, m_rpcTimeout);
			m_waitingReplyDict.TryAdd(messageObject.UniqueId, rpcMessageData);
			return rpcMessageData.Task;
		}

		/// <summary>
		/// 逻辑更新，处理计时和超时移除任务
		/// </summary>
		/// <param name="deltaTime"></param>
		public void Update(float deltaTime)
		{
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

			if (m_timeoutReplyIdSet.Count > 0)
			{
				foreach (var objectId in m_timeoutReplyIdSet)
				{
					m_waitingReplyDict.TryRemove(objectId, out _);
				}

				m_timeoutReplyIdSet.Clear();
			}
		}
	}
}