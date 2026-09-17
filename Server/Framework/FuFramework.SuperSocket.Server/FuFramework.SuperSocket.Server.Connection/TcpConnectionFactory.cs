using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using FuFramework.SuperSocket.Connection;
using FuFramework.SuperSocket.Connection.Sockets;
using FuFramework.SuperSocket.Server.Abstractions;
using FuFramework.SuperSocket.Server.Abstractions.Connections;
using Microsoft.Extensions.ObjectPool;

namespace FuFramework.SuperSocket.Server.Connection;

/// <summary>
/// Factory for creating TCP connections with optional stream initializers and socket options.
/// </summary>
public class TcpConnectionFactory : TcpConnectionFactoryBase
{
	private readonly ObjectPool<SocketSender> _socketSenderPool;

	/// <summary>
	/// Initializes a new instance of the <see cref="T:FuFramework.SuperSocket.Server.Connection.TcpConnectionFactory" /> class.
	/// </summary>
	/// <param name="listenOptions">The options for the listener.</param>
	/// <param name="connectionOptions">The options for the connection.</param>
	/// <param name="socketOptionsSetter">An action to configure socket options.</param>
	/// <param name="connectionStreamInitializersFactory">The factory for creating connection stream initializers.</param>
	public TcpConnectionFactory(ListenOptions listenOptions, ConnectionOptions connectionOptions, Action<Socket> socketOptionsSetter, IConnectionStreamInitializersFactory connectionStreamInitializersFactory)
		: base(listenOptions, connectionOptions, socketOptionsSetter, connectionStreamInitializersFactory)
	{
		Dictionary<string, string> values = connectionOptions.Values;
		if (values == null || !values.TryGetValue("socketSenderPoolSize", out var value) || !int.TryParse(value, out var result))
		{
			result = 1000;
		}
		_socketSenderPool = new DefaultObjectPool<SocketSender>(new DefaultPooledObjectPolicy<SocketSender>(), result);
	}

	/// <summary>
	/// Creates a new connection asynchronously.
	/// </summary>
	/// <param name="connection">The connection object, typically a socket.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the created connection.</returns>
	public override async Task<IConnection> CreateConnection(object connection, CancellationToken cancellationToken)
	{
		Socket socket = connection as Socket;
		ApplySocketOptions(socket);
		IEnumerable<IConnectionStreamInitializer> connectionStreamInitializers = base.ConnectionStreamInitializers;
		if (connectionStreamInitializers != null && connectionStreamInitializers.Any())
		{
			Stream stream = null;
			foreach (IConnectionStreamInitializer item in connectionStreamInitializers)
			{
				stream = await item.InitializeAsync(socket, stream, cancellationToken);
			}
			return new StreamPipeConnection(stream, socket.RemoteEndPoint, socket.LocalEndPoint, base.ConnectionOptions);
		}
		return new TcpPipeConnection(socket, base.ConnectionOptions, _socketSenderPool);
	}
}
