using System;
using System.Net.Sockets;
using AOT.Framework.Core.Log;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// TCP 网络频道。
	/// 功能：
	///     1. 实现TCP网络通讯功能。
	///     2. 实现消息的发送和接收。
	/// </summary>
	internal sealed class SystemTcpNetworkChannel : NetworkChannelBase
	{
		private ConnectState m_connectState;
		private SystemNetSocket m_systemNetSocket;

		/// <summary>
		/// 初始化网络频道的新实例。
		/// </summary>
		/// <param name="name">网络频道名称。</param>
		/// <param name="networkChannelHelper">网络频道辅助器。</param>
		/// <param name="rpcTimeout">RPC超时时间</param>
		public SystemTcpNetworkChannel(string name, INetworkChannelHelper networkChannelHelper, int rpcTimeout) : base(name, networkChannelHelper, rpcTimeout) { }

		/// <summary>
		/// 连接到远程主机。
		/// </summary>
		/// <param name="address">远程主机的地址。</param>
		/// <param name="userData">用户自定义数据。</param>
		public override void Connect(Uri address, object userData = null)
		{
			if (m_IsConnecting) return;

			base.Connect(address, userData);
		}

		/// <summary>
		/// 目标地址解析完成（或确认解析失败）后创建 Socket 并发起连接。
		/// </summary>
		/// <param name="userData">用户自定义数据</param>
		protected override void OnConnectEndPointReady(object userData)
		{
			if (m_IsVerifyAddress)
			{
				m_systemNetSocket = new SystemNetSocket(m_ConnectEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
				m_Socket = m_systemNetSocket;
			}

			if (m_Socket == null)
			{
				const string errorMessage = "Initialize network channel failure.";
				if (m_NetworkChannelError == null) throw new InvalidOperationException(errorMessage);
				EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
					errorCode: ENetworkErrorCode.SocketError,
					socketError: SocketError.Success,
					errorMessage: errorMessage);
				return;
			}

			m_NetworkChannelHelper.PrepareForConnecting();

			ConnectAsync(userData);
		}

		protected override bool ProcessSend()
		{
			if (!base.ProcessSend()) return false;
			SendAsync();
			return true;

		}

		#region Receive

		private void ReceiveAsync()
		{
			try
			{
				var position = (int)m_ReceiveState.Stream.Position;
				var length = (int)(m_ReceiveState.Stream.Length - m_ReceiveState.Stream.Position);
				m_systemNetSocket.BeginReceive(m_ReceiveState.Stream.GetBuffer(), position, length, SocketFlags.None, ReceiveCallback, m_systemNetSocket);
			}
			catch (Exception exception)
			{
				PActive = false;
				if (m_NetworkChannelError == null) throw;
				var socketException = exception as SocketException;
				EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
					errorCode: ENetworkErrorCode.ReceiveError,
					socketError: socketException?.SocketErrorCode ?? SocketError.Success,
					errorMessage: exception.ToString());
			}
		}

		private void ReceiveCallback(IAsyncResult asyncResult)
		{
			var systemNetSocket = (SystemNetSocket)asyncResult.AsyncState;
			if (!systemNetSocket.IsConnected) return;

			int bytesReceived;
			try
			{
				bytesReceived = systemNetSocket.EndReceive(asyncResult);
			}
			catch (Exception exception)
			{
				PActive = false;
				if (m_NetworkChannelError == null) throw;
				var socketException = exception as SocketException;
				EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
					errorCode: ENetworkErrorCode.ReceiveError,
					socketError: socketException?.SocketErrorCode ?? SocketError.Success,
					errorMessage: exception.ToString());
				return;
			}

			if (bytesReceived <= 0)
			{
				Close();
				return;
			}

			lock (m_HeartBeatLock)
			{
				m_HeartBeatState.Reset(m_ResetHeartBeatElapseSecondsWhenReceivePacket);
			}

			try
			{
				ProcessReceivedBytes(bytesReceived);
			}
			catch (Exception exception)
			{
				// 回包处理（畸形包头、未注册 messageId 等）抛出的异常绝不能在**线程池线程**上逃逸：
				// 原实现只包住了 EndReceive，解析阶段的异常会直接抛出，且抛出前没有续接 ReceiveAsync，
				// 连接会静默卡死。这里统一转为 NetworkChannelError 事件（封送主线程触发）。
				var socketException = exception as SocketException;
				EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
					errorCode: ENetworkErrorCode.DeserializePacketError,
					socketError: socketException?.SocketErrorCode ?? SocketError.Success,
					errorMessage: exception.ToString());
			}
			finally
			{
				// 无论成功、解析失败还是异常，都必须续接接收，避免连接静默卡死。
				if (PActive && m_Socket != null)
				{
					ReceiveAsync();
				}
			}
		}

		/// <summary>
		/// 处理本次收到的字节。
		/// </summary>
		/// <param name="bytesReceived">本次收到的字节数</param>
		private void ProcessReceivedBytes(int bytesReceived)
		{
			m_ReceiveState.Stream.Position += bytesReceived;
			if (m_ReceiveState.Stream.Position < m_ReceiveState.Stream.Length)
			{
				return;
			}

			m_ReceiveState.Stream.Position = 0L;
			bool processSuccess;
			if (m_ReceiveState.PacketHeader != null)
			{
				processSuccess = ProcessPackBody();
			}
			else
			{
				processSuccess = ProcessPackHeader();
				if (m_ReceiveState.IsEmptyBody)
				{
					// 如果是空消息,直接返回
					ProcessPackBody();
					return;
				}
			}

			if (!processSuccess)
			{
				// 兼容原有语义：解析失败不抛异常，只记录日志（接收续接由调用方的 finally 保证）。
				FuLogger.LogWarning($"[{Name}] 数据包解析失败，已丢弃。");
			}
		}

		/// <summary>
		/// 解析消息头
		/// </summary>
		/// <returns></returns>
		private bool ProcessPackHeader()
		{
			var headerLength = PacketReceiveHeaderHandler.PacketHeaderLength;
			var buffer = new byte[headerLength];
			_ = m_ReceiveState.Stream.Read(buffer, 0, headerLength);
			var processSuccess = m_NetworkChannelHelper.DeserializePacketHeader(buffer);
			if (!processSuccess)
			{
				// 包头解析失败：恢复为“等待包头”的状态，避免流状态与网络数据错位。
				m_ReceiveState.PrepareForPacketHeader();
				return false;
			}

			var bodyLength = ValidateAndGetPacketBodyLength(PacketReceiveHeaderHandler);
			m_ReceiveState.Reset(bodyLength, PacketReceiveHeaderHandler);
			return true;
		}

		/// <summary>
		/// 解析消息内容
		/// </summary>
		/// <returns></returns>
		private bool ProcessPackBody()
		{
			var bodyLength = ValidateAndGetPacketBodyLength(m_ReceiveState.PacketHeader);
			var buffer = new byte[bodyLength];
			_ = m_ReceiveState.Stream.Read(buffer, 0, bodyLength);

			if (m_ReceiveState.PacketHeader.ZipFlag != 0)
			{
				// 解压
				MessageDecompressHandler.NotNull(nameof(MessageDecompressHandler));
				buffer = MessageDecompressHandler.Handler(buffer);
			}

			var processSuccess = m_NetworkChannelHelper.DeserializePacketBody(buffer, PacketReceiveHeaderHandler.Id, out var messageObject);
			if (processSuccess)
			{
				messageObject.SetUpdateUniqueId(PacketReceiveHeaderHandler.UniqueId);
			}

			DebugReceiveLog(messageObject);

			// 将收到的消息加入到链表最后（接收回调在线程池线程，需与主线程消费互斥）
			lock (m_ExecutionMessageLock)
			{
				m_executionMessageLinkedList.AddLast(messageObject);
			}

			m_ReceivedPacketCount++;
			m_ReceiveState.PrepareForPacketHeader();
			return processSuccess;
		}

		#endregion

		#region Sender

		protected override bool ProcessSendMessage(MessageObject messageObject)
		{
			if (PActive == false)
			{
				PActive = false;
				EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
					errorCode: ENetworkErrorCode.SocketError,
					socketError: SocketError.Disconnecting,
					errorMessage: "Network channel is closing.");
				return false;
			}

			var serializeResult = base.ProcessSendMessage(messageObject);
			if (serializeResult) return true;
			const string errorMessage = "Serialized packet failure.";
			throw new InvalidOperationException(errorMessage);
		}


		/// <summary>
		/// 实际发送异步数据
		/// </summary>
		private void SendAsync()
		{
			try
			{
				m_systemNetSocket.BeginSend(m_SendState.Stream.GetBuffer(), (int)m_SendState.Stream.Position,
					(int)(m_SendState.Stream.Length - m_SendState.Stream.Position), SocketFlags.None, SendCallback, m_systemNetSocket);
			}
			catch (Exception exception)
			{
				PActive = false;
				if (m_NetworkChannelError == null) throw;
				var socketException = exception as SocketException;
				EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
					errorCode: ENetworkErrorCode.SendError,
					socketError: socketException?.SocketErrorCode ?? SocketError.Success,
					errorMessage: exception.ToString());
			}
		}

		private void SendCallback(IAsyncResult asyncResult)
		{
			var systemNetSocket = (SystemNetSocket)asyncResult.AsyncState;
			if (!systemNetSocket.IsConnected) return;

			int bytesSent;
			try
			{
				bytesSent = systemNetSocket.EndSend(asyncResult, out _);
			}
			catch (Exception exception)
			{
				PActive = false;
				if (m_NetworkChannelError == null) return;
				var socketException = exception as SocketException;
				EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
					errorCode: ENetworkErrorCode.SendError,
					socketError: socketException?.SocketErrorCode ?? SocketError.Success,
					errorMessage: exception.ToString());
				return;
			}

			m_SendState.Stream.Position += bytesSent;
			if (m_SendState.Stream.Position < m_SendState.Stream.Length)
			{
				SendAsync();
				return;
			}

			m_SentPacketCount++;
			m_SendState.Reset();
		}

		#endregion

		#region Connect

		private void ConnectAsync(object userData)
		{
			try
			{
				m_IsConnecting = true;
				m_connectState = new ConnectState(m_systemNetSocket, userData);
				((SystemNetSocket)m_Socket).BeginConnect(m_ConnectEndPoint.Address, m_ConnectEndPoint.Port, ConnectCallback, m_connectState);
			}
			catch (Exception exception)
			{
				if (m_NetworkChannelError == null) throw;
				var socketException = exception as SocketException;
				EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
					errorCode: ENetworkErrorCode.ConnectError,
					socketError: socketException?.SocketErrorCode ?? SocketError.Success,
					errorMessage: exception.ToString());
			}
		}

		private void ConnectCallback(IAsyncResult asyncResult)
		{
			m_IsConnecting = false;
			var connectState = (ConnectState)asyncResult.AsyncState;
			var systemNetSocket = (SystemNetSocket)connectState.Socket;
			try
			{
				systemNetSocket.EndConnect(asyncResult);
			}
			catch (ObjectDisposedException)
			{
				return;
			}
			catch (Exception exception)
			{
				var socketException = exception as SocketException;
				EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
					errorCode: ENetworkErrorCode.ConnectError,
					socketError: socketException?.SocketErrorCode ?? SocketError.Success,
					errorMessage: exception.ToString());
				Close();
				return;
			}

			m_SentPacketCount = 0;
			m_ReceivedPacketCount = 0;

			lock (m_SendPacketPool) m_SendPacketPool.Clear();
			lock (m_HeartBeatLock) m_HeartBeatState.Reset(true);

			EnqueueLifecycleEvent(EChannelLifecycleEventType.Connected, m_connectState.UserData);
			PActive = true;
			ReceiveAsync();
		}

		#endregion
	}
}
