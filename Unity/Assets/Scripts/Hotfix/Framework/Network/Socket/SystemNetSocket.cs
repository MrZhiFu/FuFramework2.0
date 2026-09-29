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
		private readonly Socket m_socket;

		public SystemNetSocket(AddressFamily ipAddressAddressFamily, SocketType socketType, ProtocolType protocolType)
		{
			m_socket = new Socket(ipAddressAddressFamily, socketType, protocolType);
		}

		public bool IsConnected => m_socket.Connected;

		public bool IsClosed { get; private set; }

		public Socket Socket => m_socket;

		public EndPoint LocalEndPoint => m_socket.LocalEndPoint;

		public EndPoint RemoteEndPoint => m_socket.RemoteEndPoint;

		public int Available => m_socket.Available;

		public int ReceiveBufferSize
		{
			get => m_socket.ReceiveBufferSize;
			set
			{
				if (value <= 0)
					throw new ArgumentException("Receive buffer size is invalid.", nameof(value));
				m_socket.ReceiveBufferSize = value;
			}
		}

		public int SendBufferSize
		{
			get => m_socket.SendBufferSize;
			set
			{
				if (value <= 0)
					throw new ArgumentException("Send buffer size is invalid.", nameof(value));
				m_socket.SendBufferSize = value;
			}
		}

		public void Shutdown()
		{
			if (IsClosed) return;
			m_socket.Shutdown(SocketShutdown.Both);
		}

		public void Close()
		{
			if (IsClosed) return;
			m_socket.Close();
			m_socket.Dispose();
			IsClosed = true;
		}


		public IAsyncResult BeginSend(byte[] getBuffer, int streamPosition, int streamLength, SocketFlags none, AsyncCallback mSendCallback,
									  INetworkSocket mSocket)
		{
			return m_socket.BeginSend(getBuffer, streamPosition, streamLength, none, mSendCallback, mSocket);
		}

		public int EndSend(IAsyncResult asyncResult, out SocketError error)
		{
			return m_socket.EndSend(asyncResult, out error);
		}


		public void BeginConnect(IPAddress ipAddress, int port, AsyncCallback mConnectCallback, ConnectState connectState)
		{
			m_socket.BeginConnect(ipAddress, port, mConnectCallback, connectState);
		}

		public void EndConnect(IAsyncResult ar)
		{
			m_socket.EndConnect(ar);
		}

		public void BeginReceive(byte[] getBuffer, int streamPosition, int streamLength, SocketFlags none, AsyncCallback mReceiveCallback,
								 INetworkSocket mSocket)
		{
			m_socket.BeginReceive(getBuffer, streamPosition, streamLength, none, mReceiveCallback, mSocket);
		}

		public int EndReceive(IAsyncResult asyncResult)
		{
			return m_socket.EndReceive(asyncResult);
		}
	}
}
