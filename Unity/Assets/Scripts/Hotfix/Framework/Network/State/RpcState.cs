using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Core;
using AOT.Framework.Extension;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// RPC状态类
	/// </summary>
	internal sealed class RpcState : IDisposable
	{
		/// <summary>
		/// 等待回复处理对象字典
		/// </summary>
		private readonly ConcurrentDictionary<long, RpcMessageData> m_waitingReplyHandlingObjects = new();

		/// <summary>
		/// 删除等待中的处理器ID列表,由于超时导致的删除
		/// </summary>
		private readonly HashSet<long> m_removeReplyHandlingObjectIds = new();

		/// <summary>
		/// RPC 超时时间。单位毫秒
		/// </summary>
		private readonly int m_rpcTimeout;

		/// <summary>
		/// 消息类型是否为响应类型的缓存。
		/// 说明：原实现每收一包都调用 Type.IsImplWithInterface（内部会分配 GetInterfaces 数组），
		/// 这里按类型缓存结果，热路径上不再重复做接口扫描。
		/// </summary>
		private static readonly ConcurrentDictionary<Type, bool> sr_responseMessageTypeCache = new();

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
			foreach (var (_, rpcMessageData) in m_waitingReplyHandlingObjects)
			{
				rpcMessageData.Cancel();
			}

			m_waitingReplyHandlingObjects.Clear();
			m_removeReplyHandlingObjectIds.Clear();
		}

		/// <summary>
		/// 处理RPC回复消息，并触发相应的结束处理程序。
		/// </summary>
		/// <param name="message">要处理的消息对象，必须实现IResponseMessage接口。</param>
		/// <returns>如果成功处理回复消息，则返回true；否则返回false。</returns>
		public bool TryReply(MessageObject message)
		{
			if (!sr_responseMessageTypeCache.GetOrAdd(message.GetType(), static type => type.IsImplWithInterface(typeof(IResponseMessage)))) return false;
			if (!m_waitingReplyHandlingObjects.TryRemove(message.UniqueId, out var messageActorObject)) return false;

			messageActorObject.Reply(message as IResponseMessage);
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
			if (m_waitingReplyHandlingObjects.TryGetValue(messageObject.UniqueId, out var messageActorObject))
			{
				return messageActorObject.Task;
			}

			var rpcMessageData = RpcMessageData.Create(messageObject as IRequestMessage, m_rpcTimeout);
			m_waitingReplyHandlingObjects.TryAdd(messageObject.UniqueId, rpcMessageData);
			return rpcMessageData.Task;
		}

		/// <summary>
		/// 逻辑更新，处理计时和超时移除任务
		/// </summary>
		/// <param name="deltaTime"></param>
		public void Update(float deltaTime)
		{
			if (m_waitingReplyHandlingObjects.Count > 0)
			{
				var elapseSecondsTime = (long)(deltaTime * 1000);
				m_removeReplyHandlingObjectIds.Clear();
				foreach (var handlingObject in m_waitingReplyHandlingObjects)
				{
					var isTimeout = handlingObject.Value.IncrementalElapseTime(elapseSecondsTime);
					if (!isTimeout) continue;
					m_removeReplyHandlingObjectIds.Add(handlingObject.Key);
				}
			}

			if (m_removeReplyHandlingObjectIds.Count > 0)
			{
				foreach (var objectId in m_removeReplyHandlingObjectIds)
				{
					m_waitingReplyHandlingObjects.TryRemove(objectId, out _);
				}

				m_removeReplyHandlingObjectIds.Clear();
			}
		}
	}
}