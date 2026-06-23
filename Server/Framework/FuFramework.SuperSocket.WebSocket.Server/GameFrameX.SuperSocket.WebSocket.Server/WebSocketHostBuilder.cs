using FuFramework.SuperSocket.Server;
using FuFramework.SuperSocket.Server.Abstractions;
using FuFramework.SuperSocket.Server.Abstractions.Session;
using FuFramework.SuperSocket.Server.Host;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace FuFramework.SuperSocket.WebSocket.Server;

public class WebSocketHostBuilder : SuperSocketHostBuilder<WebSocketPackage>
{
	internal WebSocketHostBuilder()
		: this((string[])null)
	{
	}

	internal WebSocketHostBuilder(IHostBuilder hostBuilder)
		: base(hostBuilder)
	{
	}

	internal WebSocketHostBuilder(string[] args)
		: base(args)
	{
		ConfigureSupplementServices(ValidateHostBuilder);
	}

	protected override void RegisterDefaultServices(HostBuilderContext builderContext, IServiceCollection servicesInHost, IServiceCollection services)
	{
		services.TryAddSingleton<ISessionFactory, GenericSessionFactory<WebSocketSession>>();
		base.RegisterDefaultServices(builderContext, servicesInHost, services);
	}

	public static WebSocketHostBuilder Create()
	{
		return Create((string[])null);
	}

	public static WebSocketHostBuilder Create(string[] args)
	{
		return Create(new WebSocketHostBuilder(args));
	}

	public static WebSocketHostBuilder Create(IHostBuilder hostBuilder)
	{
		return Create(new WebSocketHostBuilder(hostBuilder));
	}

	public static WebSocketHostBuilder Create(SuperSocketHostBuilder<WebSocketPackage> hostBuilder)
	{
		return hostBuilder.UsePipelineFilter<WebSocketPipelineFilter>().UseWebSocketMiddleware().ConfigureServices(delegate(HostBuilderContext ctx, IServiceCollection services)
		{
			services.AddSingleton<IPackageHandler<WebSocketPackage>, WebSocketPackageHandler>();
		}) as WebSocketHostBuilder;
	}

	internal static void ValidateHostBuilder(HostBuilderContext builderCtx, IServiceCollection services)
	{
	}
}
