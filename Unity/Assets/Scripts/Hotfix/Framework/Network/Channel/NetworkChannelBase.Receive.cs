using System;
using System.Collections.Generic;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 网络频道基类——接收派发分部：执行链表、主线程派发、包体长度校验与接收调试日志。
	/// </summary>
	internal abstract partial class NetworkChannelBase : INetworkChannel, IDisposable
	{
		/// <summary>
		/// 接收状态
		/// </summary>
		protected readonly ReceiveState m_ReceiveState;

		/// <summary>
		/// 接收数据包数量
		/// </summary>
		protected int m_ReceivedPacketCount;

		/// <summary>
		/// 接收消息ID忽略列表
		/// </summary>
		protected List<int> m_IgnoreReceiveIds = new();

		/// <summary>
		/// 待执行消息链表的专用锁对象。
		/// 说明：接收回调运行在线程池线程（AddLast），而主线程会进行 First/RemoveFirst/Clear，
		/// FuLinkedList 并非线程安全，必须加锁互斥。
		/// </summary>
		protected readonly object m_ExecutionMessageLock = new();

		/// <summary>
		/// 待执行消息链表（接收线程投递、主线程派发）
		/// </summary>
		protected readonly FuLinkedList<MessageObject> m_executionMessageLinkedList = new();

		/// <summary>
		/// 消息派发时复用的处理器列表，避免每收一包都新建 List 造成 GC。
		/// </summary>
		private readonly List<MessageHandlerAttribute> m_handlerBuffer = new(8);

		/// <summary>
		/// 复用列表是否正在使用中（用于处理派发重入，重入时回退为新建列表）。
		/// </summary>
		private bool m_handlerBufferBusy;

		/// <summary>
		/// 处理接收到的消息
		/// </summary>
		private void ProcessReceivedMessage()
		{
			while (true)
			{
				MessageObject messageObject;

				// 接收回调运行在线程池线程（AddLast），此处与其互斥出队；
				// 同时在锁内校验 First 是否为空，避免链表被清空后 RemoveFirst 抛异常冲出 ModuleManager.Update。
				lock (m_ExecutionMessageLock)
				{
					var first = m_executionMessageLinkedList.First;
					if (first == null) break;

					messageObject = first.Value;
					m_executionMessageLinkedList.RemoveFirst();
				}

				try
				{
					// 执行RPC匹配
					if (m_RpcState.TryReply(messageObject)) continue;

					// 执行通知消息（锁外派发，避免长时间持锁阻塞接收线程）
					DispatchMessage(messageObject);
				}
				catch (Exception e)
				{
					FuLogger.LogFatal(e);
				}
			}
		}

		/// <summary>
		/// 将消息派发给已注册的消息处理器
		/// </summary>
		/// <param name="messageObject">消息对象</param>
		private void DispatchMessage(MessageObject messageObject)
		{
			// 处理器列表会被复制到复用缓冲区后再派发：派发期间用户代码可能注册/注销处理器，
			// 直接遍历内部列表会抛 InvalidOperationException；复用缓冲区避免每包分配 List。
			var                           reuseBuffer = !m_handlerBufferBusy;
			List<MessageHandlerAttribute> handlers;
			if (reuseBuffer)
			{
				m_handlerBufferBusy = true;
				handlers            = m_handlerBuffer;
				ProtoMessageHandlerRegistry.GetHandlers(messageObject.GetType(), handlers);
			}
			else
			{
				// 重入时不能复用缓冲区，退化为新建列表。
				handlers = new List<MessageHandlerAttribute>(8);
				ProtoMessageHandlerRegistry.GetHandlers(messageObject.GetType(), handlers);
			}

			try
			{
				foreach (var handler in handlers)
				{
					handler.SetMessageObject(messageObject);
					try
					{
						handler.Invoke();
					}
					catch (Exception e)
					{
						FuLogger.LogFatal(e);
					}
				}
			}
			finally
			{
				if (reuseBuffer)
				{
					handlers.Clear();
					m_handlerBufferBusy = false;
				}
			}
		}

		/// <summary>
		/// 处理接收数据的占位实现（当前接收在子类回调中直接处理，保留扩展点）
		/// </summary>
		protected void ProcessReceive() { }

		/// <summary>
		/// 单个数据包的包体长度上限（默认 1 MiB，<b>可按协议需要调整</b>）。
		/// 包头中的 PacketLength 直接来自网络报文，若不设上限，畸形包会导致超大数组分配/内存耗尽。
		/// 默认值依据：本项目 proto 未使用 bytes 大字段（仅 repeated/map 的常规消息），
		/// 实际消息体在 KB 量级，1 MiB 是宽松的健全性上限；若后续协议引入大消息，
		/// 由具体通道实现（TCP / WebSocket）在合适时机调高本值即可。
		/// </summary>
		protected static int MaxPacketBodyLength { get; set; } = 1024 * 1024;

		/// <summary>
		/// 校验包头中的包长度并换算包体长度。
		/// </summary>
		/// <param name="header">已解析的包头</param>
		/// <returns>包体长度</returns>
		/// <exception cref="InvalidOperationException">包长度非法或超出上限时抛出</exception>
		protected static int ValidateAndGetPacketBodyLength(IPacketReceiveHeaderHandler header)
		{
			var packetLength = header.PacketLength;
			var headerLength = header.PacketHeaderLength;
			if (packetLength < headerLength)
			{
				throw new InvalidOperationException($"包长度非法。packetLength:{packetLength}, headerLength:{headerLength}");
			}

			var bodyLength = (long)packetLength - headerLength;
			if (bodyLength > MaxPacketBodyLength)
			{
				throw new InvalidOperationException($"包体长度超出上限。bodyLength:{bodyLength}, limit:{MaxPacketBodyLength}");
			}

			return (int)bodyLength;
		}

		/// <summary>
		/// 接收调试日志（仅 ENABLE_NETWORK_RSP_LOG 宏开启时输出）
		/// </summary>
		/// <param name="messageObject">已接收的消息对象</param>
		protected void DebugReceiveLog(MessageObject messageObject)
		{
#if ENABLE_NETWORK_RSP_LOG
			if (!m_IgnoreReceiveIds.Contains(PacketReceiveHeaderHandler.Id))
			{
				FuLogger.LogInfo($"收到消息 ID:[{PacketReceiveHeaderHandler.Id},{messageObject.UniqueId},{messageObject.GetType().Name}] 消息内容:{UtilityAOT.Json.ToJson(messageObject)}");
			}
#endif
		}
	}
}