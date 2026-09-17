using System;
using FuFramework.SuperSocket.Server.Abstractions.Connections;
using FuFramework.SuperSocket.Server.Abstractions.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace FuFramework.SuperSocket.Server.Abstractions.Host;

public static class HostBuilderExtensions
{
	public static ISuperSocketHostBuilder AsSuperSocketBuilder(this IHostBuilder hostBuilder)
	{
		return hostBuilder as ISuperSocketHostBuilder;
	}

	public static ISuperSocketHostBuilder UseMiddleware<TMiddleware>(this ISuperSocketHostBuilder builder) where TMiddleware : class, IMiddleware
	{
		return builder.ConfigureServices(delegate(HostBuilderContext ctx, IServiceCollection services)
		{
			services.TryAddEnumerable(ServiceDescriptor.Singleton<IMiddleware, TMiddleware>());
		}).AsSuperSocketBuilder();
	}

	public static ISuperSocketHostBuilder UseMiddleware<TMiddleware>(this ISuperSocketHostBuilder builder, Func<IServiceProvider, TMiddleware> implementationFactory) where TMiddleware : class, IMiddleware
	{
		return builder.ConfigureServices(delegate(HostBuilderContext ctx, IServiceCollection services)
		{
			services.TryAddEnumerable(ServiceDescriptor.Singleton<IMiddleware, TMiddleware>(implementationFactory));
		}).AsSuperSocketBuilder();
	}

	public static ISuperSocketHostBuilder UseTcpConnectionListenerFactory<TConnectionListenerFactory>(this ISuperSocketHostBuilder builder) where TConnectionListenerFactory : class, IConnectionListenerFactory
	{
		return builder.ConfigureServices(delegate(HostBuilderContext ctx, IServiceCollection services)
		{
			services.AddSingleton<IConnectionListenerFactory, TConnectionListenerFactory>();
		}).AsSuperSocketBuilder();
	}
}
