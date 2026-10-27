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
		private void ProcessReceived()
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

					// 执行RPC匹配，如果匹配成功，执行RPC响应；否则派发通知消息。
					if (m_rpcState.TryReply(msgObj)) continue;

					// 派发通知消息
					DispatchNotify(msgObj);
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
		private void DispatchNotify(MessageObject msgObj)
		{
			if (!NotifyMessageHandlerRegistry.TryDispatch(msgObj))
			{
				FuLogger.LogWarning($"没有找到消息处理器的消息类型：{msgObj.GetType().Name}");
			}
		}

		/// <summary>
		/// 接收调试日志（仅 ENABLE_NETWORK_RSP_LOG 宏开启时输出）
		/// </summary>
		/// <param name="msgObj">已接收的消息对象</param>
		protected void DebugReceiveLog(MessageObject msgObj)
		{
#if ENABLE_NETWORK_RSP_LOG
			if (!m_IgnoreReceiveIds.Contains(m_NetworkChannelHelper.LastReceivedMessageId))
			{
				FuLogger.LogInfo($"收到消息:[{msgObj.GetType().Name}({m_NetworkChannelHelper.LastReceivedMessageId}),{msgObj.UniqueId}] 消息内容:{UtilityAOT.Json.ToJson(msgObj)}");
			}
#endif
		}
	}
}