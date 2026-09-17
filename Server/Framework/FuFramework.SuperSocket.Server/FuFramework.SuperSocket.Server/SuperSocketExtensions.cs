using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using FuFramework.SuperSocket.Primitives;
using FuFramework.SuperSocket.Server.Abstractions;
using FuFramework.SuperSocket.Server.Abstractions.Connections;
using FuFramework.SuperSocket.Server.Abstractions.Session;
using FuFramework.SuperSocket.Server.Host;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FuFramework.SuperSocket.Server;

public static class SuperSocketExtensions
{
	/// <summary>
	/// Establishes an active connection to the specified remote endpoint and registers the connection with the server.
	/// </summary>
	/// <param name="server">The server to register the connection with.</param>
	/// <param name="remoteEndpoint">The remote endpoint to connect to.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains a value indicating whether the connection was successful.</returns>
	public static async ValueTask<bool> ActiveConnect(this IServer server, EndPoint remoteEndpoint)
	{
		Socket socket = new Socket(remoteEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
		try
		{
			await socket.ConnectAsync(remoteEndpoint);
			await (server as IConnectionRegister).RegisterConnection(socket);
			return true;
		}
		catch (Exception exception)
		{
			server.ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger("ActiveConnect").LogError(exception, $"Failed to connect to {remoteEndpoint}");
			return false;
		}
	}

	/// <summary>
	/// Gets the default logger associated with the session.
	/// </summary>
	/// <param name="session">The session to get the logger for.</param>
	/// <returns>The default logger for the session.</returns>
	public static ILogger GetDefaultLogger(this IAppSession session)
	{
		return (session.Server as ILoggerAccessor)?.Logger;
	}

	/// <summary>
	/// Adds a listener to the server options.
	/// </summary>
	/// <param name="serverOptions">The server options to add the listener to.</param>
	/// <param name="listener">The listener to add.</param>
	/// <returns>The updated server options.</returns>
	public static ServerOptions AddListener(this ServerOptions serverOptions, ListenOptions listener)
	{
		List<ListenOptions> list = serverOptions.Listeners;
		if (list == null)
		{
			List<ListenOptions> list3 = (serverOptions.Listeners = new List<ListenOptions>());
			list = list3;
		}
		list.Add(listener);
		return serverOptions;
	}

	/// <summary>
	/// Adapts the host to support multiple server hosting.
	/// </summary>
	/// <param name="host">The host to adapt.</param>
	public static void AdaptMultipleServerHost(this IHost host)
	{
		IServiceProvider services = host.Services;
		services.GetService<MultipleServerHostBuilder>()?.AdaptMultipleServerHost(services);
	}
}
