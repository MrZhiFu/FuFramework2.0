using FuFramework.SuperSocket.Server;
using FuFramework.SuperSocket.Server.Abstractions;
using FuFramework.SuperSocket.Server.Abstractions.Connections;
using FuFramework.SuperSocket.Server.Abstractions.Session;
using FuFramework.SuperSocket.Server.Connection;
using FuFramework.SuperSocket.Server.Host;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace FuFramework.SuperSocket.WebSocket.Server;

internal class WebSocketHostBuilderAdapter : ServerHostBuilderAdapter<WebSocketPackage>
{
	public WebSocketHostBuilderAdapter(IHostBuilder hostBuilder)
		: base(hostBuilder)
	{
		UsePipelineFilter<WebSocketPipelineFilter>();
		this.UseWebSocketMiddleware();
		ConfigureServices(delegate(HostBuilderContext ctx, IServiceCollection services)
		{
			services.AddSingleton<IPackageHandler<WebSocketPackage>, WebSocketPackageHandler>();
		});
		ConfigureSupplementServices(WebSocketHostBuilder.ValidateHostBuilder);
	}

	protected override void RegisterDefaultServices(HostBuilderContext builderContext, IServiceCollection servicesInHost, IServiceCollection services)
	{
		services.TryAddSingleton<ISessionFactory, GenericSessionFactory<WebSocketSession>>();
		services.TryAddSingleton<IConnectionListenerFactory, TcpConnectionListenerFactory>();
		services.TryAddSingleton(new SocketOptionsSetter(delegate
		{
		}));
		services.TryAddSingleton<IConnectionFactoryBuilder, ConnectionFactoryBuilder>();
		services.TryAddSingleton<IConnectionStreamInitializersFactory, DefaultConnectionStreamInitializersFactory>();
	}
}
