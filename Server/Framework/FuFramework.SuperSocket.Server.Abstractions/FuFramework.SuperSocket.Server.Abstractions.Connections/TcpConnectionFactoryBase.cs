using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using FuFramework.SuperSocket.Connection;
using Microsoft.Extensions.Logging;

namespace FuFramework.SuperSocket.Server.Abstractions.Connections;

public abstract class TcpConnectionFactoryBase : IConnectionFactory
{
	protected ListenOptions ListenOptions { get; }

	protected ConnectionOptions ConnectionOptions { get; }

	protected Action<Socket> SocketOptionsSetter { get; }

	protected ILogger Logger { get; }

	protected IEnumerable<IConnectionStreamInitializer> ConnectionStreamInitializers { get; }

	public TcpConnectionFactoryBase(ListenOptions listenOptions, ConnectionOptions connectionOptions, Action<Socket> socketOptionsSetter, IConnectionStreamInitializersFactory connectionStreamInitializersFactory)
	{
		ListenOptions = listenOptions;
		ConnectionOptions = connectionOptions;
		SocketOptionsSetter = socketOptionsSetter;
		Logger = connectionOptions.Logger;
		ConnectionStreamInitializers = connectionStreamInitializersFactory?.Create(listenOptions);
	}

	public abstract Task<IConnection> CreateConnection(object connection, CancellationToken cancellationToken);

	protected virtual void ApplySocketOptions(Socket socket)
	{
		try
		{
			if (ListenOptions.NoDelay)
			{
				socket.NoDelay = true;
			}
		}
		catch (Exception exception)
		{
			Logger.LogWarning(exception, "Failed to set NoDelay for the socket.");
		}
		try
		{
			if (ConnectionOptions.ReceiveBufferSize > 0)
			{
				socket.ReceiveBufferSize = ConnectionOptions.ReceiveBufferSize;
			}
		}
		catch (Exception exception2)
		{
			Logger.LogWarning(exception2, "Failed to set ReceiveBufferSize for the socket.");
		}
		try
		{
			if (ConnectionOptions.SendBufferSize > 0)
			{
				socket.SendBufferSize = ConnectionOptions.SendBufferSize;
			}
		}
		catch (Exception exception3)
		{
			Logger.LogWarning(exception3, "Failed to set SendBufferSize for the socket.");
		}
		try
		{
			if (ConnectionOptions.ReceiveTimeout > 0)
			{
				socket.ReceiveTimeout = ConnectionOptions.ReceiveTimeout;
			}
		}
		catch (Exception exception4)
		{
			Logger.LogWarning(exception4, "Failed to set ReceiveTimeout for the socket.");
		}
		try
		{
			if (ConnectionOptions.SendTimeout > 0)
			{
				socket.SendTimeout = ConnectionOptions.SendTimeout;
			}
		}
		catch (Exception exception5)
		{
			Logger.LogWarning(exception5, "Failed to set SendTimeout for the socket.");
		}
		try
		{
			SocketOptionsSetter?.Invoke(socket);
		}
		catch (Exception exception6)
		{
			Logger.LogWarning(exception6, "Failed to run socketOptionSetter for the socket.");
		}
	}
}
