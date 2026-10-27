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
		/// <summary>
		/// 连接状态
		/// </summary>
		private ConnectState m_connectState;

		/// <summary>
		/// 系统 Socket 封装
		/// </summary>
		private SystemNetSocket m_systemNetSocket;

		/// <summary>
		/// 初始化网络频道的新实例。
		/// </summary>
		/// <param name="networkChannelHelper">网络频道辅助器。</param>
		/// <param name="rpcTimeout">RPC超时时间</param>
		public SystemTcpNetworkChannel(NetworkChannelHelper networkChannelHelper, int rpcTimeout) : base(networkChannelHelper, rpcTimeout) { }

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
				m_Socket          = m_systemNetSocket;
			}

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

			ConnectAsync(userData);
		}

		/// <summary>
		/// 处理发送：序列化完成后发起异步发送
		/// </summary>
		/// <returns>是否成功发起发送</returns>
		protected override bool ProcessSend()
		{
			if (!base.ProcessSend()) return false;
			SendAsync();
			return true;
		}

		#region 收包

		/// <summary>
		/// 发起异步接收（写入接收缓冲区）
		/// </summary>
		private void ReceiveAsync()
		{
			try
			{
				var position = (int)m_ReceiveState.Stream.Position;
				var length   = (int)(m_ReceiveState.Stream.Length - m_ReceiveState.Stream.Position);
				m_systemNetSocket.BeginReceive(m_ReceiveState.Stream.GetBuffer(), position, length, SocketFlags.None, ReceiveCallback, m_systemNetSocket);
			}
			catch (Exception exception)
			{
				IsOpen = false;
				if (m_NetworkChannelError == null) throw;
				var socketException = exception as SocketException;
				EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
				                      errorCode: ENetworkErrorCode.ReceiveError,
				                      socketError: socketException?.SocketErrorCode ?? SocketError.Success,
				                      errorMessage: exception.ToString());
			}
		}

		/// <summary>
		/// 异步接收完成回调：投递数据到接收缓冲并继续发起下一次接收
		/// </summary>
		/// <param name="asyncResult">异步操作结果</param>
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
				IsOpen = false;
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
				// 收包仅清零丢失计数，不重置流逝时长
				m_HeartBeatState.Reset(false);
			}

			try
			{
				// 处理本次收到的字节
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
				if (IsOpen && m_Socket != null)
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

			// 如果每凑够一个完整的数据包，就返回等待继续接收
			if (m_ReceiveState.Stream.Position < m_ReceiveState.Stream.Length) return;

			// 完整的包，从头开始处理
			m_ReceiveState.Stream.Position = 0L;
			bool processSuccess;
			if (m_ReceiveState.IsAwaitingBody)
			{
				// 解析包体
				processSuccess = ProcessPackBody();
			}
			else
			{
				// 解析包头
				processSuccess = ProcessPackHeader();
				if (m_ReceiveState.IsEmptyBody)
				{
					// 空消息（包体长度 = 0）：包头即整条消息，没有包体可等攒，
					// 当帧调 ProcessPackBody 解析入队并复位状态机后再返回（不能省略，否则消息丢失且状态机残留错位）。
					ProcessPackBody();
					return;
				}
			}

			if (!processSuccess)
			{
				// 兼容原有语义：解析失败不抛异常，只记录日志（接收续接由调用方的 finally 保证）。
				FuLogger.LogWarning("数据包解析失败，已丢弃。");
			}
		}

		/// <summary>
		/// 解析消息头
		/// </summary>
		/// <returns></returns>
		private bool ProcessPackHeader()
		{
			var headerLength = ReceiveState.PACKET_HEADER_LENGTH;
			var buffer       = new byte[headerLength];
			_ = m_ReceiveState.Stream.Read(buffer, 0, headerLength);
			var processSuccess = m_NetworkChannelHelper.DeserializePacketHeader(buffer);
			if (!processSuccess)
			{
				// 包头解析失败：恢复为“等待包头”的状态，避免流状态与网络数据错位。
				m_ReceiveState.PrepareForPacketHeader();
				return false;
			}

			var bodyLength = m_NetworkChannelHelper.ValidateAndGetPacketBodyLength();
			m_ReceiveState.ZipFlag      = m_NetworkChannelHelper.LastReceivedZipFlag;
			m_ReceiveState.Reset(bodyLength);
			m_ReceiveState.IsAwaitingBody = true;
			return true;
		}

		/// <summary>
		/// 解析消息体
		/// </summary>
		/// <returns></returns>
		private bool ProcessPackBody()
		{
			var bodyLength = (int)m_ReceiveState.Stream.Length;
			var buffer     = new byte[bodyLength];
			_ = m_ReceiveState.Stream.Read(buffer, 0, bodyLength);

			if (m_ReceiveState.ZipFlag != 0)
			{
				// 解压
				buffer = Utility.Zip.Decompress(buffer);
			}

			var processSuccess = m_NetworkChannelHelper.DeserializePacketBody(buffer, m_NetworkChannelHelper.LastReceivedMessageId, out var msgObj);
			if (processSuccess)
			{
				msgObj.SetUniqueId(m_NetworkChannelHelper.LastReceivedUniqueId);
			}

#if ENABLE_NETWORK_RSP_LOG
			DebugReceiveLog(msgObj);
#endif

			// 将收到的消息加入到链表最后（接收回调在线程池线程，需与主线程消费互斥）
			lock (m_handleMsgLock)
			{
				m_waitingHandleMsgLkList.AddLast(msgObj);
			}

			m_ReceiveState.PrepareForPacketHeader();
			return processSuccess;
		}

		#endregion

		#region 发包

		/// <summary>
		/// 处理待发送消息：频道未激活时报错，序列化失败则抛出异常
		/// </summary>
		/// <param name="msgObj">待发送的消息对象</param>
		/// <returns>是否序列化成功</returns>
		protected override bool ProcessSendMessage(MessageObject msgObj)
		{
			if (IsOpen == false)
			{
				IsOpen = false;
				EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
				                      errorCode: ENetworkErrorCode.SocketError,
				                      socketError: SocketError.Disconnecting,
				                      errorMessage: "网络频道正在关闭。");
				return false;
			}

			var serializeResult = base.ProcessSendMessage(msgObj);
			if (serializeResult) return true;
			const string errorMessage = "序列化消息失败。";
			throw new InvalidOperationException(errorMessage);
		}


		/// <summary>
		/// 实际发送异步数据
		/// </summary>
		private void SendAsync()
		{
			try
			{
				// 每次都是从"进度指针"往后发，所以续发永远不会把已发的字节重发一遍
				m_systemNetSocket.BeginSend(m_SendState.CacheStream.GetBuffer(), (int)m_SendState.CacheStream.Position,
				                            (int)(m_SendState.CacheStream.Length - m_SendState.CacheStream.Position), SocketFlags.None, SendCallback, m_systemNetSocket);
			}
			catch (Exception exception)
			{
				IsOpen = false;
				if (m_NetworkChannelError == null) throw;
				var socketException = exception as SocketException;
				EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
				                      errorCode: ENetworkErrorCode.SendError,
				                      socketError: socketException?.SocketErrorCode ?? SocketError.Success,
				                      errorMessage: exception.ToString());
			}
		}

		/// <summary>
		/// 异步发送完成回调：未发完则续发，发完则重置发送状态
		/// </summary>
		/// <param name="asyncResult">异步操作结果</param>
		private void SendCallback(IAsyncResult asyncResult)
		{
			var systemNetSocket = (SystemNetSocket)asyncResult.AsyncState;
			if (!systemNetSocket.IsConnected) return;

			// 这次发送实际发出字节数
			int bytesSent;
			try
			{
				bytesSent = systemNetSocket.EndSend(asyncResult, out _);
			}
			catch (Exception exception)
			{
				IsOpen = false;
				if (m_NetworkChannelError == null) return;
				var socketException = exception as SocketException;
				EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
				                      errorCode: ENetworkErrorCode.SendError,
				                      socketError: socketException?.SocketErrorCode ?? SocketError.Success,
				                      errorMessage: exception.ToString());
				return;
			}

			// 发送进度往前推
			m_SendState.CacheStream.Position += bytesSent;

			// 如果未发完则续发，否则重置发送状态
			if (m_SendState.CacheStream.Position < m_SendState.CacheStream.Length)
			{
				SendAsync();
				return;
			}

			m_SendState.Reset();
		}

		#endregion

		#region 连接

		/// <summary>
		/// 发起异步连接
		/// </summary>
		/// <param name="userData">用户自定义数据</param>
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

		/// <summary>
		/// 异步连接完成回调：连接成功后初始化频道并启动接收
		/// </summary>
		/// <param name="asyncResult">异步操作结果</param>
		private void ConnectCallback(IAsyncResult asyncResult)
		{
			m_IsConnecting = false;
			var connectState    = (ConnectState)asyncResult.AsyncState;
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

			m_SendState.Reset();

			lock (m_SendPacketPool) m_SendPacketPool.Clear();
			lock (m_HeartBeatLock) m_HeartBeatState.Reset(true);

			EnqueueLifecycleEvent(EChannelLifecycleEventType.Connected, m_connectState.UserData);
			IsOpen = true;
			ReceiveAsync();
		}

		#endregion
	}
}