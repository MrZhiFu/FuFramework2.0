using System;
using System.Net;
using System.Net.Sockets;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 系统套接字实现
	/// </summary>
	internal sealed class SystemNetSocket : INetworkSocket
	{
		/// <summary>
		/// 底层系统 Socket
		/// </summary>
		private readonly Socket m_socket;

		/// <summary>
		/// 创建系统套接字
		/// </summary>
		/// <param name="ipAddressAddressFamily">地址族</param>
		/// <param name="socketType">套接字类型</param>
		/// <param name="protocolType">协议类型</param>
		public SystemNetSocket(AddressFamily ipAddressAddressFamily, SocketType socketType, ProtocolType protocolType)
		{
			m_socket = new Socket(ipAddressAddressFamily, socketType, protocolType);
		}

		/// <summary>
		/// 是否已连接
		/// </summary>
		public bool IsConnected => m_socket.Connected;

		/// <summary>
		/// 是否已关闭
		/// </summary>
		public bool IsClosed { get; private set; }

		/// <summary>
		/// 底层系统 Socket 实例
		/// </summary>
		public Socket Socket => m_socket;

		/// <summary>
		/// 本地端点
		/// </summary>
		public EndPoint LocalEndPoint => m_socket.LocalEndPoint;

		/// <summary>
		/// 远程端点
		/// </summary>
		public EndPoint RemoteEndPoint => m_socket.RemoteEndPoint;

		/// <summary>
		/// 当前可从网络读取的数据量。单位字节
		/// </summary>
		public int Available => m_socket.Available;

		/// <summary>
		/// 接收缓冲区大小。单位字节
		/// </summary>
		public int ReceiveBufferSize
		{
			get => m_socket.ReceiveBufferSize;
			set
			{
				if (value <= 0)
					throw new ArgumentException("接收缓冲区大小无效。", nameof(value));
				m_socket.ReceiveBufferSize = value;
			}
		}

		/// <summary>
		/// 发送缓冲区大小。单位字节
		/// </summary>
		public int SendBufferSize
		{
			get => m_socket.SendBufferSize;
			set
			{
				if (value <= 0)
					throw new ArgumentException("发送缓冲区大小无效。", nameof(value));
				m_socket.SendBufferSize = value;
			}
		}

		/// <summary>
		/// 禁用收发，但不释放底层资源
		/// </summary>
		public void Shutdown()
		{
			if (IsClosed) return;
			m_socket.Shutdown(SocketShutdown.Both);
		}

		/// <summary>
		/// 关闭并释放底层 Socket
		/// </summary>
		public void Close()
		{
			if (IsClosed) return;
			m_socket.Close();
			m_socket.Dispose();
			IsClosed = true;
		}


		/// <summary>
		/// 开始异步发送
		/// </summary>
		/// <param name="getBuffer">数据缓冲区</param>
		/// <param name="streamPosition">起始偏移</param>
		/// <param name="streamLength">数据长度</param>
		/// <param name="none">Socket 行为标志</param>
		/// <param name="mSendCallback">异步回调</param>
		/// <param name="mSocket">状态对象（发起发送的 Socket）</param>
		/// <returns>异步操作结果</returns>
		public IAsyncResult BeginSend(byte[] getBuffer, int streamPosition, int streamLength, SocketFlags none, AsyncCallback mSendCallback,
		                              INetworkSocket mSocket)
		{
			return m_socket.BeginSend(getBuffer, streamPosition, streamLength, none, mSendCallback, mSocket);
		}

		/// <summary>
		/// 结束异步发送
		/// </summary>
		/// <param name="asyncResult">异步操作结果</param>
		/// <param name="error">套接字错误码</param>
		/// <returns>实际发送的字节数</returns>
		public int EndSend(IAsyncResult asyncResult, out SocketError error)
		{
			return m_socket.EndSend(asyncResult, out error);
		}


		/// <summary>
		/// 开始异步连接远程主机
		/// </summary>
		/// <param name="ipAddress">远程主机 IP</param>
		/// <param name="port">远程主机端口</param>
		/// <param name="mConnectCallback">异步回调</param>
		/// <param name="connectState">状态对象（连接状态）</param>
		public void BeginConnect(IPAddress ipAddress, int port, AsyncCallback mConnectCallback, ConnectState connectState)
		{
			m_socket.BeginConnect(ipAddress, port, mConnectCallback, connectState);
		}

		/// <summary>
		/// 结束异步连接
		/// </summary>
		/// <param name="ar">异步操作结果</param>
		public void EndConnect(IAsyncResult ar)
		{
			m_socket.EndConnect(ar);
		}

		/// <summary>
		/// 开始异步接收
		/// </summary>
		/// <param name="getBuffer">接收缓冲区</param>
		/// <param name="streamPosition">写入起始偏移</param>
		/// <param name="streamLength">最大接收长度</param>
		/// <param name="none">Socket 行为标志</param>
		/// <param name="mReceiveCallback">异步回调</param>
		/// <param name="mSocket">状态对象（发起接收的 Socket）</param>
		public void BeginReceive(byte[] getBuffer, int streamPosition, int streamLength, SocketFlags none, AsyncCallback mReceiveCallback,
		                         INetworkSocket mSocket)
		{
			m_socket.BeginReceive(getBuffer, streamPosition, streamLength, none, mReceiveCallback, mSocket);
		}

		/// <summary>
		/// 结束异步接收
		/// </summary>
		/// <param name="asyncResult">异步操作结果</param>
		/// <returns>实际接收的字节数</returns>
		public int EndReceive(IAsyncResult asyncResult)
		{
			return m_socket.EndReceive(asyncResult);
		}
	}
}