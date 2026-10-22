using System;
using System.IO;
using System.Threading;
using System.Net.Sockets;
using AOT.Framework.Core.Log;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// Web Socket 网络频道。
	/// </summary>
	internal sealed class WebSocketNetworkChannel : NetworkChannelBase
	{
		/// <summary>
		/// WebSocket 连接超时时间（毫秒）
		/// </summary>
		private const int CONNECT_TIMEOUT_MILLISECONDS = 15000;

		/// <summary>
		/// 最近一次连接的地址，用于在地址解析完成后创建 WebSocket 客户端。
		/// </summary>
		private Uri m_lastAddress;

		/// <summary>
		/// 取消令牌源。每次连接都会重建（见 OnConnectEndPointReady），
		/// 原实现为 readonly 且只 Cancel 不重建，导致 Cancel 之后频道永远无法重连。
		/// </summary>
		private CancellationTokenSource m_cancellationTokenSource = new();

		/// <summary>
		/// 初始化网络频道的新实例。
		/// </summary>
		/// <param name="name">网络频道名称。</param>
		/// <param name="networkChannelHelper">网络频道辅助器。</param>
		/// <param name="rpcTimeout">RPC超时时间</param>
		public WebSocketNetworkChannel(DefaultNetworkChannelHelper networkChannelHelper, int rpcTimeout) : base(networkChannelHelper, rpcTimeout) { }

		/// <summary>
		/// WebSocket 的 URL 由底层客户端自行解析，无需在这里做 DNS 解析。
		/// </summary>
		protected override bool NeedResolveHost => false;

		/// <summary>
		/// 连接到远程主机。
		/// </summary>
		/// <param name="address">远程主机的地址。</param>
		/// <param name="userData">用户自定义数据。</param>
		public override void Connect(Uri address, object userData = null)
		{
			if (m_IsConnecting) return;

			m_lastAddress = address;
			base.Connect(address, userData);
		}

		/// <summary>
		/// 地址解析完成后创建 WebSocket 客户端并发起连接。
		/// </summary>
		/// <param name="userData">用户自定义数据</param>
		protected override void OnConnectEndPointReady(object userData)
		{
			// 旧 CTS 可能已在 Close 中被 Cancel，这里必须重建，否则新连接会立刻被判定为已取消。
			m_cancellationTokenSource?.Dispose();
			m_cancellationTokenSource = new CancellationTokenSource();

			m_Socket = new WebSocketNetSocket(m_lastAddress.ToString(), ReceiveCallback, CloseCallback);
			if (m_Socket == null)
			{
				const string errorMessage = "初始化网络频道失败。";
				if (m_NetworkChannelError == null) throw new InvalidOperationException(errorMessage);
				EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
				                      errorCode: ENetworkErrorCode.SocketError,
				                      socketError: SocketError.Success,
				                      errorMessage: errorMessage);
				return;
			}

			m_NetworkChannelHelper.PrepareForConnecting();
			ConnectAsync(userData).Forget();
		}

		/// <summary>
		/// 底层连接关闭回调，触发频道关闭
		/// </summary>
		/// <param name="errorMessage">关闭原因</param>
		private void CloseCallback(string errorMessage) => Close();

		/// <summary>
		/// 关闭频道并取消进行中的连接
		/// </summary>
		public override void Close()
		{
			base.Close();
			m_cancellationTokenSource?.Cancel();
		}

		/// <summary>
		/// 判断频道是否已关闭
		/// </summary>
		/// <returns>Socket 断开且连接令牌已取消时返回 true</returns>
		private bool IsClose()
		{
			return m_Socket != null && !m_Socket.IsConnected && m_cancellationTokenSource != null && m_cancellationTokenSource.IsCancellationRequested;
		}

		/// <summary>
		/// 处理发送：序列化待发消息并交由底层 WebSocket 发出
		/// </summary>
		/// <returns>是否发送成功</returns>
		protected override bool ProcessSend()
		{
			lock (m_SendPacketPool)
			{
				if (m_SendPacketPool.Count <= 0) return false;

				while (m_SendPacketPool.First != null)
				{
					var messageObject = m_SendPacketPool.First.Value;

					bool serializeResult;
					try
					{
						serializeResult = ProcessSendMessage(messageObject);
#if ENABLE_NETWORK_REQ_LOG
						DebugSendLog(messageObject);
#endif
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

					if (!serializeResult)
					{
						const string errorMessage = "序列化消息失败。";
						if (m_NetworkChannelError == null) throw new InvalidOperationException(errorMessage);
						EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
						                      errorCode: ENetworkErrorCode.SerializeError,
						                      socketError: SocketError.Success,
						                      errorMessage: errorMessage);
						return false;
					}

					m_SendState.Reset();
				}

				return true;
			}
		}

		/// <summary>
		/// 处理发送消息对象
		/// </summary>
		/// <param name="messageObject">消息对象</param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		protected override bool ProcessSendMessage(MessageObject messageObject)
		{
			if (IsClose())
			{
				PActive = false;
				EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
				                      errorCode: ENetworkErrorCode.SocketError,
				                      socketError: SocketError.Disconnecting,
				                      errorMessage: "网络频道正在关闭。");
				return false;
			}

			var serializeResult = base.ProcessSendMessage(messageObject);
			if (serializeResult)
			{
				var webSocketClientNetSocket = (WebSocketNetSocket)m_Socket;

				var buffer = new byte[m_SendState.Stream.Length];
				m_SendState.Stream.Seek(0, SeekOrigin.Begin);
				_ = m_SendState.Stream.Read(buffer, 0, buffer.Length);

				webSocketClientNetSocket.Client.SendAsync(buffer);
			}
			else
			{
				const string errorMessage = "序列化消息失败。";
				throw new InvalidOperationException(errorMessage);
			}

			return true;
		}

		/// <summary>
		/// 异步连接远程主机（超时与取消由 CancellationTokenSource 控制）
		/// </summary>
		/// <param name="userData">用户自定义数据</param>
		private async UniTaskVoid ConnectAsync(object userData)
		{
			var cancellationTokenSource = m_cancellationTokenSource;
			try
			{
				m_IsConnecting = true;
				var socketClient = (WebSocketNetSocket)m_Socket;

				// 连接超时/取消由 CTS 控制：原实现是 async void，既无超时也无取消，失败还可能抛出。
				cancellationTokenSource.CancelAfter(CONNECT_TIMEOUT_MILLISECONDS);

				await socketClient.ConnectAsync(cancellationTokenSource.Token);
				ConnectCallback(new ConnectState(m_Socket, userData));
			}
			catch (OperationCanceledException)
			{
				m_IsConnecting = false;
				PActive        = false;
				EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
				                      errorCode: ENetworkErrorCode.ConnectError,
				                      socketError: SocketError.TimedOut,
				                      errorMessage: $"WebSocket 连接已取消或超时（{CONNECT_TIMEOUT_MILLISECONDS}ms）。");
			}
			catch (Exception exception)
			{
				m_IsConnecting = false;
				var socketException = exception as SocketException;
				if (m_NetworkChannelError == null)
				{
					// UniTaskVoid 中禁止抛出，否则异常无人接管。
					FuLogger.LogError(exception.ToString());
					return;
				}

				EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
				                      errorCode: ENetworkErrorCode.ConnectError,
				                      socketError: socketException?.SocketErrorCode ?? SocketError.Success,
				                      errorMessage: exception.ToString());
			}
		}

		/// <summary>
		/// 连接完成回调：校验连接状态，成功后重置计数并广播连接成功事件
		/// </summary>
		/// <param name="connectState">连接状态</param>
		private void ConnectCallback(ConnectState connectState)
		{
			m_IsConnecting = false;
			try
			{
				var socketUserData = (WebSocketNetSocket)m_Socket;
				if (!socketUserData.IsConnected)
					throw new SocketException((int)ENetworkErrorCode.ConnectError);
			}
			catch (ObjectDisposedException)
			{
				return;
			}
			catch (Exception exception)
			{
				PActive = false;
				if (m_NetworkChannelError == null) throw;
				var socketException = exception as SocketException;
				EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
				                      errorCode: ENetworkErrorCode.ConnectError,
				                      socketError: socketException?.SocketErrorCode ?? SocketError.Success,
				                      errorMessage: exception.ToString());
				return;
			}

			m_SendState.Reset();

			lock (m_SendPacketPool) m_SendPacketPool.Clear();
			lock (m_HeartBeatLock) m_HeartBeatState.Reset(true);
			EnqueueLifecycleEvent(EChannelLifecycleEventType.Connected, connectState.UserData);
			PActive = true;
		}

		/// <summary>
		/// 底层消息回调：投递数据到接收缓冲并驱动心跳重置
		/// </summary>
		/// <param name="buffer">收到的原始字节</param>
		private void ReceiveCallback(byte[] buffer)
		{
			try
			{
				lock (m_HeartBeatLock)
				{
					// 收包仅清零丢失计数，不重置流逝时长
					m_HeartBeatState.Reset(false);
				}

				if (buffer.Length < PacketReceiveHeaderHandler.PacketHeaderLength) return;

				var processSuccess = m_NetworkChannelHelper.DeserializePacketHeader(buffer);
				if (processSuccess)
				{
					var bodyLength = ValidateAndGetPacketBodyLength(PacketReceiveHeaderHandler);
					m_ReceiveState.Reset(bodyLength, PacketReceiveHeaderHandler);
					if (buffer.Length < bodyLength) return;

					var body = buffer.ReadBytes(PacketReceiveHeaderHandler.PacketHeaderLength, bodyLength);
					if (m_ReceiveState.PacketHeader.ZipFlag != 0)
					{
						// 解压
						MessageDecompressHandler.NotNull(nameof(MessageDecompressHandler));
						body = MessageDecompressHandler.Handler(body);
					}

					// 反序列化数据
					processSuccess = m_NetworkChannelHelper.DeserializePacketBody(body, PacketReceiveHeaderHandler.Id, out var messageObject);
					if (processSuccess)
					{
						messageObject.SetUpdateUniqueId(PacketReceiveHeaderHandler.UniqueId);
					}

#if ENABLE_NETWORK_RSP_LOG
					DebugReceiveLog(messageObject);
#endif
					if (!processSuccess)
					{
						if (m_NetworkChannelError != null)
						{
							EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
							                      errorCode: ENetworkErrorCode.DeserializePacketError,
							                      errorMessage: "包体无效。");
							return;
						}
					}

					// 将收到的消息加入到链表最后（与主线程消费互斥）
					lock (m_ExecutionMessageLock)
					{
						m_executionMessageLinkedList.AddLast(messageObject);
					}
				}
				else
				{
					EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
					                      errorCode: ENetworkErrorCode.DeserializePacketHeaderError,
					                      errorMessage: "包头无效。");
				}
			}
			catch (Exception e)
			{
				EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
				                      errorCode: ENetworkErrorCode.DeserializePacketError,
				                      errorMessage: "包体无效。" + e.Message + "\n" + e.StackTrace);
			}
		}
	}
}