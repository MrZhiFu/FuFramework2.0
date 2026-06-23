using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;

namespace FuFramework.SuperSocket.ClientEngine;

public abstract class AuthenticatedStreamTcpSession : TcpClientSession
{
	private class StreamAsyncState
	{
		public AuthenticatedStream Stream { get; set; }

		public Socket Client { get; set; }

		public PosList<ArraySegment<byte>> SendingItems { get; set; }
	}

	private AuthenticatedStream m_Stream;

	public SecurityOption Security { get; set; }

	public AuthenticatedStreamTcpSession()
	{
	}

	protected override void SocketEventArgsCompleted(object sender, SocketAsyncEventArgs e)
	{
		ProcessConnect(sender as Socket, null, e, null);
	}

	protected abstract void StartAuthenticatedStream(Socket client);

	protected override void OnGetSocket(SocketAsyncEventArgs e)
	{
		try
		{
			StartAuthenticatedStream(base.Client);
		}
		catch (Exception e2)
		{
			if (!IsIgnorableException(e2))
			{
				OnError(e2);
			}
		}
	}

	protected void OnAuthenticatedStreamConnected(AuthenticatedStream stream)
	{
		m_Stream = stream;
		OnConnected();
		if (base.Buffer.Array == null)
		{
			int num = ReceiveBufferSize;
			if (num <= 0)
			{
				num = 4096;
			}
			ReceiveBufferSize = num;
			base.Buffer = new ArraySegment<byte>(new byte[num]);
		}
		BeginRead();
	}

	private void BeginRead()
	{
		ReadAsync();
	}

	private async void ReadAsync()
	{
		while (base.IsConnected && base.Client != null && m_Stream != null)
		{
			ArraySegment<byte> buffer = base.Buffer;
			int num;
			try
			{
				num = await m_Stream.ReadAsync(buffer.Array, buffer.Offset, buffer.Count, CancellationToken.None);
			}
			catch (Exception e)
			{
				if (!IsIgnorableException(e))
				{
					OnError(e);
				}
				if (EnsureSocketClosed(base.Client))
				{
					OnClosed();
				}
				break;
			}
			if (num != 0)
			{
				OnDataReceived(buffer.Array, buffer.Offset, num);
				continue;
			}
			if (EnsureSocketClosed(base.Client))
			{
				OnClosed();
			}
			break;
		}
	}

	protected override bool IsIgnorableException(Exception e)
	{
		if (base.IsIgnorableException(e))
		{
			return true;
		}
		if (e is IOException)
		{
			if (e.InnerException is ObjectDisposedException)
			{
				return true;
			}
			if (e.InnerException is IOException && e.InnerException.InnerException is ObjectDisposedException)
			{
				return true;
			}
		}
		return false;
	}

	protected override void SendInternal(PosList<ArraySegment<byte>> items)
	{
		SendInternalAsync(items);
	}

	private async void SendInternalAsync(PosList<ArraySegment<byte>> items)
	{
		try
		{
			for (int i = items.Position; i < items.Count; i++)
			{
				ArraySegment<byte> arraySegment = items[i];
				await m_Stream.WriteAsync(arraySegment.Array, arraySegment.Offset, arraySegment.Count, CancellationToken.None);
			}
			m_Stream.Flush();
		}
		catch (Exception e)
		{
			if (!IsIgnorableException(e))
			{
				OnError(e);
			}
			if (EnsureSocketClosed(base.Client))
			{
				OnClosed();
			}
			return;
		}
		OnSendingCompleted();
	}

	public override void Close()
	{
		AuthenticatedStream stream = m_Stream;
		if (stream != null)
		{
			stream.Dispose();
			m_Stream = null;
		}
		base.Close();
	}
}
