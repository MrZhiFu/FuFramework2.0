using System;
using System.Net;
using System.Net.Sockets;

namespace FuFramework.SuperSocket.ClientEngine;

public static class ConnectAsyncExtension
{
	private class ConnectToken
	{
		public object State { get; set; }

		public ConnectedCallback Callback { get; set; }
	}

	private static void SocketAsyncEventCompleted(object sender, SocketAsyncEventArgs e)
	{
		e.Completed -= SocketAsyncEventCompleted;
		ConnectToken connectToken = (ConnectToken)e.UserToken;
		e.UserToken = null;
		connectToken.Callback(sender as Socket, connectToken.State, e, null);
	}

	private static SocketAsyncEventArgs CreateSocketAsyncEventArgs(EndPoint remoteEndPoint, ConnectedCallback callback, object state)
	{
		SocketAsyncEventArgs socketAsyncEventArgs = new SocketAsyncEventArgs();
		socketAsyncEventArgs.UserToken = new ConnectToken
		{
			State = state,
			Callback = callback
		};
		socketAsyncEventArgs.RemoteEndPoint = remoteEndPoint;
		socketAsyncEventArgs.Completed += SocketAsyncEventCompleted;
		return socketAsyncEventArgs;
	}

	internal static bool PreferIPv4Stack()
	{
		return Environment.GetEnvironmentVariable("PREFER_IPv4_STACK") != null;
	}

	public static void ConnectAsync(this EndPoint remoteEndPoint, EndPoint localEndPoint, ConnectedCallback callback, object state)
	{
		SocketAsyncEventArgs e = CreateSocketAsyncEventArgs(remoteEndPoint, callback, state);
		if (localEndPoint != null)
		{
			Socket socket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			try
			{
				socket.ExclusiveAddressUse = false;
				socket.Bind(localEndPoint);
			}
			catch (Exception exception)
			{
				callback(null, state, null, exception);
				return;
			}
			socket.ConnectAsync(e);
		}
		else
		{
			Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, e);
		}
	}
}
