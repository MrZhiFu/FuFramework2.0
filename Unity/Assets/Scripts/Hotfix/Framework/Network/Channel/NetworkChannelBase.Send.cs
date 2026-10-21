using System;
using System.Collections.Generic;
using System.Net.Sockets;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 网络频道基类——发送分部：发送队列、对外 Send、序列化发送管线与发送调试日志。
	/// </summary>
	internal abstract partial class NetworkChannelBase : INetworkChannel, IDisposable
	{
		/// <summary>
		/// 待发送消息包池（链表，按先进先出顺序发送）
		/// </summary>
		protected readonly FuLinkedList<MessageObject> m_SendPacketPool;

		/// <summary>
		/// 发送状态
		/// </summary>
		protected readonly SendState m_SendState;

		/// <summary>
		/// 发送消息ID忽略列表
		/// </summary>
		protected List<int> m_IgnoreSendIds = new();

		/// <summary>
		/// 发送数据包的数量
		/// </summary>
		protected int m_SentPacketCount;

		/// <summary>
		/// 向远程主机发送消息包(将消息对象加入发送队列，后续由 ProcessSend 处理发送逻辑)。
		/// </summary>
		/// <typeparam name="T">消息包类型。</typeparam>
		/// <param name="messageObject">要发送的消息包。</param>
		public void Send<T>(T messageObject) where T : MessageObject
		{
			messageObject.NotNull(nameof(messageObject));
			if (m_Socket == null)
			{
				const string errorMessage = "Socket为null，无法发送消息包。";
				if (m_NetworkChannelError == null) throw new InvalidOperationException(errorMessage);
				EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
				                      errorCode: ENetworkErrorCode.SendError,
				                      socketError: SocketError.Success,
				                      errorMessage: errorMessage);
				return;
			}

			if (!PActive)
			{
				const string errorMessage = "网络频道未激活(未连接至服务器)，无法发送消息包。";
				if (m_NetworkChannelError == null) throw new InvalidOperationException(errorMessage);
				EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
				                      errorCode: ENetworkErrorCode.SendError,
				                      socketError: SocketError.Success,
				                      errorMessage: errorMessage);
				return;
			}

			if (messageObject == null)
			{
				const string errorMessage = "消息包无效。";
				if (m_NetworkChannelError == null) throw new InvalidOperationException(errorMessage);
				EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
				                      errorCode: ENetworkErrorCode.SendError,
				                      socketError: SocketError.Success,
				                      errorMessage: errorMessage);
				return;
			}

			lock (m_SendPacketPool)
			{
				m_SendPacketPool.AddLast(messageObject);
			}
		}

		/// <summary>
		/// 处理消息发送
		/// </summary>
		/// <returns> 是否成功处理消息发送 </returns>
		/// <exception cref="InvalidOperationException"></exception>
		protected virtual bool ProcessSend()
		{
			lock (m_SendPacketPool)
			{
				// 闸门：同一时刻只允许一批消息在途。
				// ① Stream.Length > 0：上一批还没发完（发完才 Reset 清零），不能再灌新消息，避免两批混在一个流里；
				// ② 发送队列已空：没有可发的消息。两种情况都无需装货，直接返回（子类据此不会发起新一轮发送）。
				if (m_SendState.Stream.Length > 0 || m_SendPacketPool.Count <= 0) return false;

				// 处理待发送的消息对象，直到遇到错误或发送完毕为止。
				while (m_SendPacketPool.First != null)
				{
					var  messageObject = m_SendPacketPool.First.Value;
					bool serializeResult;
					try
					{
						// 处理发送消息对象(序列化包头和包体，并将序列化后的包头和包体写进stream中)。
						serializeResult = ProcessSendMessage(messageObject);
						DebugSendLog(messageObject);
					}
					catch (Exception exception)
					{
						PActive = false;
						if (m_NetworkChannelError == null) throw;
						var socketException = exception as SocketException;
						EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
						                      errorCode: ENetworkErrorCode.SerializeError,
						                      socketError: socketException?.SocketErrorCode ?? SocketError.Success,
						                      errorMessage: exception.ToString());
						return false;
					}
					finally
					{
						m_SendPacketPool.RemoveFirst();
					}

					// 消息序列化处理成功，继续处理下一个消息对象
					if (serializeResult) continue;

					// 处理发送消息对象失败，返回false，停止整帧发送
					const string errorMessage = "序列化消息失败.";
					if (m_NetworkChannelError == null) throw new InvalidOperationException(errorMessage);
					EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
					                      errorCode: ENetworkErrorCode.SerializeError,
					                      socketError: SocketError.Success,
					                      errorMessage: errorMessage);
					return false;
				}

				m_SendState.Stream.Position = 0L;
				return true;
			}
		}

		/// <summary>
		/// 处理发送消息对象(序列化包头和包体，并将序列化后的包头和包体写进stream中)。
		/// </summary>
		/// <param name="messageObject">消息对象</param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		protected virtual bool ProcessSendMessage(MessageObject messageObject)
		{
			var serializeResult = m_NetworkChannelHelper.SerializePacketHeader(messageObject, m_SendState.Stream, out var messageBodyBuffer);
			if (serializeResult)
			{
				serializeResult = m_NetworkChannelHelper.SerializePacketBody(messageBodyBuffer, m_SendState.Stream);
			}
			else
			{
				const string errorMessage = "序列化消息失败。";
				throw new InvalidOperationException(errorMessage);
			}

			return serializeResult;
		}

		/// <summary>
		/// 发送调试日志（仅 ENABLE_NETWORK_REQ_LOG 宏开启时输出）
		/// </summary>
		/// <param name="messageObject">已发送的消息对象</param>
		protected void DebugSendLog(MessageObject messageObject)
		{
#if ENABLE_NETWORK_REQ_LOG
			if (!m_IgnoreSendIds.Contains(PacketSendHeaderHandler.Id))
			{
				FuLogger.LogInfo($"发送消息 ID:[{PacketSendHeaderHandler.Id},{messageObject.UniqueId},{messageObject.GetType().Name}] 消息内容:{UtilityAOT.Json.ToJson(messageObject)}");
			}
#endif
		}
	}
}