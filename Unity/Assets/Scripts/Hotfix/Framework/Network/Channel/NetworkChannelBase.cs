using System;
using System.Collections.Generic;
using System.Net.Sockets;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 网络频道基类。
	/// 核心分部：构造、帧调度（Update）、连接关闭（Close）、销毁（Dispose）与生命周期事件队列。
	/// 其余职责见 Send / Receive / HeartBeat / Connection / API 各分部文件。
	/// </summary>
	internal abstract partial class NetworkChannelBase : INetworkChannel, IDisposable
	{
		/// <summary>
		/// 网络频道帮助器（负责装配处理器与序列化收发包）
		/// </summary>
		protected readonly INetworkChannelHelper m_NetworkChannelHelper;

		/// <summary>
		/// 网络Socket 对象
		/// </summary>
		protected INetworkSocket m_Socket;

		/// <summary>
		/// 是否已释放
		/// </summary>
		private bool m_disposed;

		/// <summary>
		/// 关闭流程的专用锁对象（替代原先的 lock(this)，避免与外部对频道实例的加锁产生交叉锁序）。
		/// </summary>
		private readonly object m_closeLock = new();

		/// <summary>
		/// 网络频道是否已激活(已连接至服务器)
		/// </summary>
		protected bool PActive { get; set; }

		/// <summary>
		/// 频道生命周期事件类型。
		/// 仅含跨线程触发的三类；MissHeartBeat 由主线程 Update 的心跳检查触发，无需封送。
		/// protected 而非 private：作为 protected 方法 EnqueueLifecycleEvent 的参数类型，
		/// private 会报 CS0051，且子类触发点需要引用本类型。
		/// </summary>
		protected enum EChannelLifecycleEventType
		{
			Connected,
			Closed,
			Error
		}

		/// <summary>
		/// 待主线程派发的生命周期事件记录。
		/// 生命周期事件频率极低（每连接至多 Connected/Closed 各一次、Error 罕见），直接 new，不入池。
		/// </summary>
		private sealed class LifecycleEvent
		{
			/// <summary>
			/// 生命周期事件类型
			/// </summary>
			public EChannelLifecycleEventType m_Type;

			/// <summary>
			/// 用户自定义数据
			/// </summary>
			public object m_UserData;

			/// <summary>
			/// 网络错误码
			/// </summary>
			public ENetworkErrorCode m_ErrorCode;

			/// <summary>
			/// 套接字错误码
			/// </summary>
			public SocketError m_SocketError;

			/// <summary>
			/// 错误消息
			/// </summary>
			public string m_ErrorMessage;
		}

		/// <summary>
		/// 生命周期事件队列：Socket 回调线程入队，主线程 Update 排水后触发。
		/// 互斥复用 m_ExecutionMessageLock（与数据包接收队列同族：接收线程生产、主线程消费）。
		/// 排水位置在 Update 活跃检查之前——保证 Close() 后入队的 Closed 事件必达。
		/// 频道销毁后 Update 不再被调，未派发事件随本对象一并回收（丢弃，无泄漏）。
		/// </summary>
		private readonly Queue<LifecycleEvent> m_lifecycleEventQueue = new();

		/// <summary>
		/// 将生命周期事件入队，由主线程 Update 排水时触发（封送，替代跨线程直接 Invoke）。
		/// 调用方保留「订阅者为 null 时抛异常」的既有行为，本方法不做判空。
		/// </summary>
		protected void EnqueueLifecycleEvent(EChannelLifecycleEventType type, object userData = null,
		                                     ENetworkErrorCode errorCode = ENetworkErrorCode.SocketError,
		                                     SocketError socketError = SocketError.Success,
		                                     string errorMessage = null)
		{
			var lifecycleEvent = new LifecycleEvent
			{
				m_Type         = type,
				m_UserData     = userData,
				m_ErrorCode    = errorCode,
				m_SocketError  = socketError,
				m_ErrorMessage = errorMessage,
			};

			lock (m_ExecutionMessageLock)
			{
				m_lifecycleEventQueue.Enqueue(lifecycleEvent);
			}
		}

		/// <summary>
		/// 排水生命周期事件队列：锁内逐条出队、锁外触发（写法同 ProcessReceivedMessage 的出队-派发分离，
		/// handler 内同步 Close 频道只会继续入队，不会破坏本循环）。
		/// 触发的是现有公共委托字段，NetworkModule 等订阅方零改动。
		/// </summary>
		private void DrainLifecycleEvents()
		{
			while (true)
			{
				LifecycleEvent lifecycleEvent;
				lock (m_ExecutionMessageLock)
				{
					if (m_lifecycleEventQueue.Count == 0) break;
					lifecycleEvent = m_lifecycleEventQueue.Dequeue();
				}

				switch (lifecycleEvent.m_Type)
				{
					case EChannelLifecycleEventType.Connected:
						m_NetworkChannelConnected?.Invoke(this, lifecycleEvent.m_UserData);
						break;
					case EChannelLifecycleEventType.Closed:
						m_NetworkChannelClosed?.Invoke(this);
						break;
					case EChannelLifecycleEventType.Error:
						m_NetworkChannelError?.Invoke(this, lifecycleEvent.m_ErrorCode, lifecycleEvent.m_SocketError, lifecycleEvent.m_ErrorMessage);
						break;
				}
			}
		}

		/// <summary>
		/// 连接成功回调（参数：频道、用户自定义数据）
		/// </summary>
		public Action<NetworkChannelBase, object> m_NetworkChannelConnected;

		/// <summary>
		/// 连接关闭回调（参数：频道）
		/// </summary>
		public Action<NetworkChannelBase> m_NetworkChannelClosed;

		/// <summary>
		/// 心跳丢失回调（参数：频道、连续丢失次数）
		/// </summary>
		public Action<NetworkChannelBase, int> m_NetworkChannelMissHeartBeat;

		/// <summary>
		/// 网络错误回调（参数：频道、错误码、套接字错误码、错误消息）
		/// </summary>
		public Action<NetworkChannelBase, ENetworkErrorCode, SocketError, string> m_NetworkChannelError;

		/// <summary>
		/// 初始化网络频道基类的新实例。
		/// </summary>
		/// <param name="name">网络频道名称。</param>
		/// <param name="networkChannelHelper">网络频道辅助器。</param>
		/// <param name="rpcTimeout">RPC超时时间</param>
		public NetworkChannelBase(string name, INetworkChannelHelper networkChannelHelper, int rpcTimeout)
		{
			Name                                           = name ?? string.Empty;
			m_SendPacketPool                               = new FuLinkedList<MessageObject>();
			m_NetworkChannelHelper                         = networkChannelHelper;
			m_EAddressFamily                               = EAddressFamily.Unknown;
			m_HeartBeatInterval                            = DEFAULT_HEART_BEAT_INTERVAL;
			m_MissHeartBeatCountByClose                    = DEFAULT_MISS_HEART_BEAT_COUNT_BY_CLOSE;
			m_ResetHeartBeatElapseSecondsWhenReceivePacket = false;
			m_Socket                                       = null;
			m_SendState                                    = new SendState();
			m_ReceiveState                                 = new ReceiveState();
			m_HeartBeatState                               = new HeartBeatState();
			m_RpcState                                     = new RpcState(rpcTimeout);
			m_SentPacketCount                              = 0;
			m_ReceivedPacketCount                          = 0;
			PActive                                        = false;
			m_IsConnecting                                 = false;
			m_disposed                                     = false;
			m_NetworkChannelConnected                      = null;
			m_NetworkChannelClosed                         = null;
			m_NetworkChannelMissHeartBeat                  = null;
			m_NetworkChannelError                          = null;

			networkChannelHelper.Initialize(this);
		}

		/// <summary>
		/// 获取网络频道名称。
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// 获取网络频道所使用的 Socket。
		/// </summary>
		public INetworkSocket Socket => m_Socket;

		/// <summary>
		/// 获取是否已连接。
		/// </summary>
		public bool Connected => m_Socket is { IsConnected: true };

		/// <summary>
		/// 获取网络地址类型。
		/// </summary>
		public EAddressFamily EAddressFamily => m_EAddressFamily;

		/// <summary>
		/// 网络频道轮询。
		/// </summary>
		/// <param name="deltaTime">帧间隔时间。</param>
		/// <param name="unscaledDeltaTime">无缩放的帧间隔时间。</param>
		public virtual void Update(float deltaTime, float unscaledDeltaTime)
		{
			// 先排水生命周期事件，再做活跃检查：频道 Close 后 PActive=false，若排水在检查之后，
			// 已入队的 Closed 事件将永远滞留队列（Update 不再被有效执行）。
			DrainLifecycleEvents();

			if (m_Socket == null || !PActive) return;

			ProcessSend();
			ProcessReceive();

			if (m_Socket == null || !PActive) return;

			ProcessHeartBeat(unscaledDeltaTime);
			ProcessReceivedMessage();
			m_RpcState.Update(deltaTime, unscaledDeltaTime);
		}

		/// <summary>
		/// 关闭连接并释放所有相关资源。
		/// 语义：断开连接（可重连）——重连时将重建 Socket；彻底销毁频道请调 Dispose。
		/// </summary>
		public virtual void Close()
		{
			lock (m_closeLock)
			{
				if (m_Socket == null) return;
				PActive = false;

				try
				{
					m_Socket.Dispose();
				}
				catch
				{
					// ignored
				}
				finally
				{
					m_Socket = null;
					// 原为 NetworkChannelClosed?.Invoke(this)（可能运行在 Socket 回调线程）——改为入队封送。
					// m_Socket==null 的早退保证重复 Close 不会重复入队；事件队列不被 Close 清空。
					EnqueueLifecycleEvent(EChannelLifecycleEventType.Closed);
				}

				m_SentPacketCount     = 0;
				m_ReceivedPacketCount = 0;
			}

			// 以下清理放在 m_closeLock 之外：ProcessSend 会先持有 m_SendPacketPool，
			// 其中的错误回调可能再次进入 Close，若这里持 m_closeLock 再抢 m_SendPacketPool，
			// 两条路径锁序相反会形成 ABBA 死锁。
			lock (m_SendPacketPool) m_SendPacketPool.Clear();
			lock (m_HeartBeatLock) m_HeartBeatState.Reset(true);

			// 断线/销毁时终结所有挂起的 RPC 请求，否则 await 会永久悬挂。
			m_RpcState.Dispose();
			lock (m_ExecutionMessageLock) m_executionMessageLinkedList.Clear();
		}

		/// <summary>
		/// 销毁网络频道（终态：断开连接 + 释放全部状态与辅助器，频道不可复用）。
		/// 仅断开连接请调 Close（可重连：重连时将重建 Socket）。
		/// </summary>
		public void Dispose()
		{
			if (m_disposed) return;

			Close();
			m_SendState.Dispose();
			m_ReceiveState.Dispose();
			m_NetworkChannelHelper.Dispose();

			m_disposed = true;
		}
	}
}