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
		/// 接收消息ID忽略列表
		/// </summary>
		protected List<int> m_IgnoreReceiveIds = new();

		/// <summary>
		/// 待处理消息链表的专用锁对象。
		/// 说明：接收回调运行在线程池线程（AddLast），而主线程会进行 First/RemoveFirst/Clear，
		/// FuLinkedList 并非线程安全，必须加锁互斥。
		/// </summary>
		protected readonly object m_handleMsgLock = new();

		/// <summary>
		/// 待处理消息链表（接收线程投递、主线程派发）
		/// </summary>
		protected readonly FuLinkedList<MessageObject> m_waitingHandleMsgLkList = new();

		/// <summary>
		/// 处理接收到的消息
		/// </summary>
		private void ProcessReceivedMessage()
		{
			while (true)
			{
				MessageObject msgObj;

				// 接收回调运行在线程池线程（AddLast），此处与其互斥出队；
				// 同时在锁内校验 First 是否为空，避免链表被清空后 RemoveFirst 抛异常冲出 ModuleManager.Update。
				lock (m_handleMsgLock)
				{
					var first = m_waitingHandleMsgLkList.First;
					if (first == null) break;

					msgObj = first.Value;
					m_waitingHandleMsgLkList.RemoveFirst();
				}

				try
				{
					// 心跳回包是保活侧信道，不参与业务派发（角色由 IHeartBeatMessage 接口自描述）
					if (msgObj is IHeartBeatMessage) continue;

					// 执行RPC匹配
					if (m_rpcState.TryReply(msgObj)) continue;

					// 执行通知消息（锁外派发，避免长时间持锁阻塞接收线程）
					DispatchNotifyMessage(msgObj);
				}
				catch (Exception e)
				{
					FuLogger.LogFatal(e);
				}
			}
		}

		/// <summary>
		/// 将推送消息派发给已绑定的处理委托（声明即生效，无手动注册；单个委托异常就地隔离）
		/// </summary>
		/// <param name="msgObj">推送消息对象</param>
		private void DispatchNotifyMessage(MessageObject msgObj)
		{
			// 委托表仅在启动期 RegisterAll 写入一次、之后只读，运行期无增删，无需快照缓冲。
			if (!NotifyMessageHandlerRegistry.TryDispatch(msgObj))
			{
				FuLogger.LogWarning($"没有找到消息处理器的消息类型：{msgObj.GetType().Name}");
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
		protected static int ValidateAndGetPacketBodyLength(PacketReceiveHeaderHandler header)
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
		/// <param name="msgObj">已接收的消息对象</param>
		protected void DebugReceiveLog(MessageObject msgObj)
		{
#if ENABLE_NETWORK_RSP_LOG
			if (!m_IgnoreReceiveIds.Contains(PacketReceiveHeaderHandler.Id))
			{
				FuLogger.LogInfo($"收到消息:[{msgObj.GetType().Name}({PacketReceiveHeaderHandler.Id}),{msgObj.UniqueId}] 消息内容:{UtilityAOT.Json.ToJson(msgObj)}");
			}
#endif
		}
	}
}