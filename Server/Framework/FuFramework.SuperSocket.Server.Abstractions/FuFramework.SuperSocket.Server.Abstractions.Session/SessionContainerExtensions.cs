using System;
using System.Linq;
using FuFramework.SuperSocket.Server.Abstractions.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace FuFramework.SuperSocket.Server.Abstractions.Session;

public static class SessionContainerExtensions
{
	public static ISessionContainer ToSyncSessionContainer(this IAsyncSessionContainer asyncSessionContainer)
	{
		return new AsyncToSyncSessionContainerWrapper(asyncSessionContainer);
	}

	public static IAsyncSessionContainer ToAsyncSessionContainer(this ISessionContainer syncSessionContainer)
	{
		return new SyncToAsyncSessionContainerWrapper(syncSessionContainer);
	}

	[Obsolete("Please use the method server.GetSessionContainer() instead.")]
	public static ISessionContainer GetSessionContainer(this IServiceProvider serviceProvider)
	{
		ISessionContainer sessionContainer = serviceProvider.GetServices<IMiddleware>().OfType<ISessionContainer>().FirstOrDefault();
		if (sessionContainer != null)
		{
			return sessionContainer;
		}
		return serviceProvider.GetServices<IMiddleware>().OfType<IAsyncSessionContainer>().FirstOrDefault()?.ToSyncSessionContainer();
	}

	[Obsolete("Please use the method server.GetSessionContainer() instead.")]
	public static IAsyncSessionContainer GetAsyncSessionContainer(this IServiceProvider serviceProvider)
	{
		IAsyncSessionContainer asyncSessionContainer = serviceProvider.GetServices<IMiddleware>().OfType<IAsyncSessionContainer>().FirstOrDefault();
		if (asyncSessionContainer != null)
		{
			return asyncSessionContainer;
		}
		return serviceProvider.GetServices<IMiddleware>().OfType<ISessionContainer>().FirstOrDefault()?.ToAsyncSessionContainer();
	}

	public static ISessionContainer GetSessionContainer(this IServerInfo server)
	{
		return server.ServiceProvider.GetSessionContainer();
	}

	public static IAsyncSessionContainer GetAsyncSessionContainer(this IServerInfo server)
	{
		return server.ServiceProvider.GetAsyncSessionContainer();
	}
}
