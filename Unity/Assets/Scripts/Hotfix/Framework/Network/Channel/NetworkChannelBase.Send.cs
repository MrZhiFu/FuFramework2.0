using System;
using System.Collections.Generic;
using System.Net.Sockets;
using Hotfix.Framework.Core;

#if ENABLE_NETWORK_REQ_LOG
using AOT.Framework.Core.Log;
#endif

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
		/// 向远程主机单向发送消息包（发完即忘，不等待服务器回复）。
		/// 说明：
		///     1. 仅校验状态后将消息加入发送队列，本方法立即返回；
		///        序列化与实际发出由后续帧的 ProcessSend/ProcessSendMessage 完成；
		///     2. 不等待服务器回复；需要一问一答请使用 Call（内部第一步就是调用本方法）。
		/// 详细说明与示例见 <see cref="INetworkChannel.Send{T}"/>。
		/// </summary>
		/// <typeparam name="T">消息包类型。</typeparam>
		/// <param name="msgObj">要发送的消息包。</param>
		public void Send<T>(T msgObj) where T : MessageObject
		{
			msgObj.NotNull(nameof(msgObj));
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

			if (!IsOpen)
			{
				const string errorMessage = "网络频道未激活(未连接至服务器)，无法发送消息包。";
				if (m_NetworkChannelError == null) throw new InvalidOperationException(errorMessage);
				EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
				                      errorCode: ENetworkErrorCode.SendError,
				                      socketError: SocketError.Success,
				                      errorMessage: errorMessage);
				return;
			}

			if (msgObj == null)
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
				m_SendPacketPool.AddLast(msgObj);
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
				if (m_SendState.CacheStream.Length > 0 || m_SendPacketPool.Count <= 0) return false;

				// 处理待发送的消息对象，直到遇到错误或发送完毕为止。
				while (m_SendPacketPool.First != null)
				{
					var  msgObj = m_SendPacketPool.First.Value;
					bool serializeResult;
					try
					{
						// 处理发送消息对象(序列化包头和包体，并将序列化后的包头和包体写进stream中)。
						serializeResult = ProcessSendMessage(msgObj);

#if ENABLE_NETWORK_REQ_LOG
						DebugSendLog(msgObj);
#endif
					}
					catch (Exception exception)
					{
						IsOpen = false;
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

				m_SendState.CacheStream.Position = 0L;
				return true;
			}
		}

		/// <summary>
		/// 处理发送消息对象(序列化包头和包体，并将序列化后的包头和包体写进stream中)。
		/// </summary>
		/// <param name="msgObj">消息对象</param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		protected virtual bool ProcessSendMessage(MessageObject msgObj)
		{
			var serializeSuccess = m_NetworkChannelHelper.SerializeAndWriteToCacheStream(msgObj, m_SendState.CacheStream, out var msgBodyBuffer);
			if (serializeSuccess)
			{
				serializeSuccess = m_NetworkChannelHelper.WritePacketBodyToCacheStream(msgBodyBuffer, m_SendState.CacheStream);
			}
			else
			{
				const string errorMessage = "序列化消息失败。";
				throw new InvalidOperationException(errorMessage);
			}

			return serializeSuccess;
		}

		/// <summary>
		/// 发送调试日志（仅 ENABLE_NETWORK_REQ_LOG 宏开启时输出）
		/// </summary>
		/// <param name="msgObj">已发送的消息对象</param>
		protected void DebugSendLog(MessageObject msgObj)
		{
#if ENABLE_NETWORK_REQ_LOG
			if (!m_IgnoreSendIds.Contains(PacketSendHeaderHandler.Id))
			{
				FuLogger.LogInfo($"发送消息:[{msgObj.GetType().Name}({PacketSendHeaderHandler.Id}),{msgObj.UniqueId}] 消息内容:{UtilityAOT.Json.ToJson(msgObj)}");
			}
#endif
		}
	}
}