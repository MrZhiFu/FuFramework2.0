using System;
using System.Linq;
using FuFramework.SuperSocket.Server.Abstractions.Connections;
using FuFramework.SuperSocket.Server.Abstractions.Host;
using FuFramework.SuperSocket.Server.Abstractions.Middleware;
using FuFramework.SuperSocket.Server.Abstractions.Session;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace FuFramework.SuperSocket.Udp;

public static class UdpServerHostBuilderExtensions
{
	public static ISuperSocketHostBuilder UseUdp(this ISuperSocketHostBuilder hostBuilder)
	{
		return (hostBuilder.ConfigureServices(delegate(HostBuilderContext context, IServiceCollection services)
		{
			services.AddSingleton<IConnectionListenerFactory, UdpConnectionListenerFactory>();
			services.AddSingleton<IConnectionFactoryBuilder, UdpConnectionFactoryBuilder>();
		}) as ISuperSocketHostBuilder).ConfigureSupplementServices(delegate(HostBuilderContext context, IServiceCollection services)
		{
			if (!services.Any((ServiceDescriptor s) => s.ServiceType == typeof(IUdpSessionIdentifierProvider)))
			{
				services.AddSingleton<IUdpSessionIdentifierProvider, IPAddressUdpSessionIdentifierProvider>();
			}
			if (!services.Any((ServiceDescriptor s) => s.ServiceType == typeof(IAsyncSessionContainer)))
			{
				services.TryAddEnumerable(ServiceDescriptor.Singleton<IMiddleware, InProcSessionContainerMiddleware>((IServiceProvider s) => s.GetRequiredService<InProcSessionContainerMiddleware>()));
				services.AddSingleton<InProcSessionContainerMiddleware>();
				services.AddSingleton((Func<IServiceProvider, ISessionContainer>)((IServiceProvider s) => s.GetRequiredService<InProcSessionContainerMiddleware>()));
				services.AddSingleton((IServiceProvider s) => s.GetRequiredService<ISessionContainer>().ToAsyncSessionContainer());
			}
		});
	}

	public static ISuperSocketHostBuilder<TReceivePackage> UseUdp<TReceivePackage>(this ISuperSocketHostBuilder<TReceivePackage> hostBuilder)
	{
		return ((ISuperSocketHostBuilder)hostBuilder).UseUdp() as ISuperSocketHostBuilder<TReceivePackage>;
	}
}
