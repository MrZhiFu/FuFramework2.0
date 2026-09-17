using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace FuFramework.SuperSocket.ClientEngine;

public abstract class TcpClientSession : ClientSession
{
	private bool m_InConnecting;

	private IBatchQueue<ArraySegment<byte>> m_SendingQueue;

	private PosList<ArraySegment<byte>> m_SendingItems;

	private int m_IsSending;

	protected string HostName { get; private set; }

	public override EndPoint LocalEndPoint
	{
		get
		{
			return base.LocalEndPoint;
		}
		set
		{
			if (m_InConnecting || base.IsConnected)
			{
				throw new Exception("You cannot set LocalEdnPoint after you start the connection.");
			}
			base.LocalEndPoint = value;
		}
	}

	public override int ReceiveBufferSize
	{
		get
		{
			return base.ReceiveBufferSize;
		}
		set
		{
			if (base.Buffer.Array != null)
			{
				throw new Exception("ReceiveBufferSize cannot be set after the socket has been connected!");
			}
			base.ReceiveBufferSize = value;
		}
	}

	protected bool IsSending => m_IsSending == 1;

	public bool IsInConnecting => m_InConnecting;

	public TcpClientSession()
	{
	}

	protected virtual bool IsIgnorableException(Exception e)
	{
		if (!(e is ObjectDisposedException))
		{
			return e is NullReferenceException;
		}
		return true;
	}

	protected bool IsIgnorableSocketError(int errorCode)
	{
		if (errorCode != 10058 && errorCode != 10053 && errorCode != 10054)
		{
			return errorCode == 995;
		}
		return true;
	}

	protected abstract void SocketEventArgsCompleted(object sender, SocketAsyncEventArgs e);

	public override void Connect(EndPoint remoteEndPoint)
	{
		if (remoteEndPoint == null)
		{
			throw new ArgumentNullException("remoteEndPoint");
		}
		if (remoteEndPoint is DnsEndPoint dnsEndPoint)
		{
			string host = dnsEndPoint.Host;
			if (!string.IsNullOrEmpty(host))
			{
				HostName = host;
			}
		}
		if (m_InConnecting)
		{
			throw new Exception("The socket is connecting, cannot connect again!");
		}
		if (base.Client != null)
		{
			throw new Exception("The socket is connected, you needn't connect again!");
		}
		if (base.Proxy != null)
		{
			base.Proxy.Completed += Proxy_Completed;
			base.Proxy.Connect(remoteEndPoint);
			m_InConnecting = true;
		}
		else
		{
			m_InConnecting = true;
			remoteEndPoint.ConnectAsync(LocalEndPoint, ProcessConnect, null);
		}
	}

	private void Proxy_Completed(object sender, ProxyEventArgs e)
	{
		base.Proxy.Completed -= Proxy_Completed;
		if (e.Connected)
		{
			SocketAsyncEventArgs socketAsyncEventArgs = null;
			if (e.TargetHostName != null)
			{
				socketAsyncEventArgs = new SocketAsyncEventArgs();
				socketAsyncEventArgs.RemoteEndPoint = new DnsEndPoint(e.TargetHostName, 0);
			}
			ProcessConnect(e.Socket, null, socketAsyncEventArgs, null);
		}
		else
		{
			OnError(new Exception("proxy error", e.Exception));
			m_InConnecting = false;
		}
	}

	protected void ProcessConnect(Socket socket, object state, SocketAsyncEventArgs e, Exception exception)
	{
		if (exception != null)
		{
			m_InConnecting = false;
			OnError(exception);
			e?.Dispose();
			return;
		}
		if (e != null && e.SocketError != 0)
		{
			m_InConnecting = false;
			OnError(new SocketException((int)e.SocketError));
			e.Dispose();
			return;
		}
		if (socket == null)
		{
			m_InConnecting = false;
			OnError(new SocketException(10053));
			return;
		}
		if (!socket.Connected)
		{
			m_InConnecting = false;
			SocketError errorCode = SocketError.HostUnreachable;
			OnError(new SocketException((int)errorCode));
			return;
		}
		if (e == null)
		{
			e = new SocketAsyncEventArgs();
		}
		e.Completed += SocketEventArgsCompleted;
		base.Client = socket;
		m_InConnecting = false;
		try
		{
			LocalEndPoint = socket.LocalEndPoint;
		}
		catch
		{
		}
		EndPoint endPoint = ((e.RemoteEndPoint != null) ? e.RemoteEndPoint : socket.RemoteEndPoint);
		if (string.IsNullOrEmpty(HostName))
		{
			HostName = GetHostOfEndPoint(endPoint);
		}
		else if (endPoint is DnsEndPoint dnsEndPoint)
		{
			string host = dnsEndPoint.Host;
			if (!string.IsNullOrEmpty(host) && !HostName.Equals(host, StringComparison.OrdinalIgnoreCase))
			{
				HostName = host;
			}
		}
		OnGetSocket(e);
	}

	private string GetHostOfEndPoint(EndPoint endPoint)
	{
		if (endPoint is DnsEndPoint dnsEndPoint)
		{
			return dnsEndPoint.Host;
		}
		if (endPoint is IPEndPoint { Address: not null } iPEndPoint)
		{
			return iPEndPoint.Address.ToString();
		}
		return string.Empty;
	}

	protected abstract void OnGetSocket(SocketAsyncEventArgs e);

	protected bool EnsureSocketClosed()
	{
		return EnsureSocketClosed(null);
	}

	protected bool EnsureSocketClosed(Socket prevClient)
	{
		Socket socket = base.Client;
		if (socket == null)
		{
			return false;
		}
		bool result = true;
		if (prevClient != null && prevClient != socket)
		{
			socket = prevClient;
			result = false;
		}
		else
		{
			base.Client = null;
			m_IsSending = 0;
		}
		try
		{
			socket.Shutdown(SocketShutdown.Both);
		}
		catch
		{
		}
		finally
		{
			try
			{
				socket.Dispose();
			}
			catch
			{
			}
		}
		return result;
	}

	private bool DetectConnected()
	{
		if (base.Client != null)
		{
			return true;
		}
		OnError(new SocketException(10057));
		return false;
	}

	private IBatchQueue<ArraySegment<byte>> GetSendingQueue()
	{
		if (m_SendingQueue != null)
		{
			return m_SendingQueue;
		}
		lock (this)
		{
			if (m_SendingQueue != null)
			{
				return m_SendingQueue;
			}
			m_SendingQueue = new ConcurrentBatchQueue<ArraySegment<byte>>(Math.Max(base.SendingQueueSize, 1024), (ArraySegment<byte> t) => t.Array == null || t.Count == 0);
			return m_SendingQueue;
		}
	}

	private PosList<ArraySegment<byte>> GetSendingItems()
	{
		if (m_SendingItems == null)
		{
			m_SendingItems = new PosList<ArraySegment<byte>>();
		}
		return m_SendingItems;
	}

	public override bool TrySend(ArraySegment<byte> segment)
	{
		if (segment.Array == null || segment.Count == 0)
		{
			throw new Exception("The data to be sent cannot be empty.");
		}
		if (!DetectConnected())
		{
			return true;
		}
		bool result = GetSendingQueue().Enqueue(segment);
		if (Interlocked.CompareExchange(ref m_IsSending, 1, 0) != 0)
		{
			return result;
		}
		DequeueSend();
		return result;
	}

	public override bool TrySend(IList<ArraySegment<byte>> segments)
	{
		if (segments == null || segments.Count == 0)
		{
			throw new ArgumentNullException("segments");
		}
		for (int i = 0; i < segments.Count; i++)
		{
			if (segments[i].Count == 0)
			{
				throw new Exception("The data piece to be sent cannot be empty.");
			}
		}
		if (!DetectConnected())
		{
			return true;
		}
		bool result = GetSendingQueue().Enqueue(segments);
		if (Interlocked.CompareExchange(ref m_IsSending, 1, 0) != 0)
		{
			return result;
		}
		DequeueSend();
		return result;
	}

	private void DequeueSend()
	{
		PosList<ArraySegment<byte>> sendingItems = GetSendingItems();
		if (!m_SendingQueue.TryDequeue(sendingItems))
		{
			m_IsSending = 0;
		}
		else
		{
			SendInternal(sendingItems);
		}
	}

	protected abstract void SendInternal(PosList<ArraySegment<byte>> items);

	protected void OnSendingCompleted()
	{
		PosList<ArraySegment<byte>> sendingItems = GetSendingItems();
		sendingItems.Clear();
		sendingItems.Position = 0;
		if (!m_SendingQueue.TryDequeue(sendingItems))
		{
			m_IsSending = 0;
		}
		else
		{
			SendInternal(sendingItems);
		}
	}

	public override void Close()
	{
		if (EnsureSocketClosed())
		{
			OnClosed();
		}
	}
}
