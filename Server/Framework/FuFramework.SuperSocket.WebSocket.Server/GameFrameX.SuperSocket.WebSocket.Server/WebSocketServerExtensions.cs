using System;
using System.Threading.Tasks;
using FuFramework.SuperSocket.Command;
using FuFramework.SuperSocket.Server;
using FuFramework.SuperSocket.Server.Abstractions.Host;
using FuFramework.SuperSocket.Server.Host;
using FuFramework.SuperSocket.WebSocket.Server.Extensions;
using FuFramework.SuperSocket.WebSocket.Server.Extensions.Compression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FuFramework.SuperSocket.WebSocket.Server;

public static class WebSocketServerExtensions
{
	internal static ISuperSocketHostBuilder<WebSocketPackage> UseWebSocketMiddleware(this ISuperSocketHostBuilder<WebSocketPackage> builder)
	{
		return builder.ConfigureServices(delegate(HostBuilderContext ctx, IServiceCollection services)
		{
			services.AddSingleton<IWebSocketServerMiddleware, WebSocketServerMiddleware>();
		}).UseMiddleware((IServiceProvider s) => s.GetService<IWebSocketServerMiddleware>() as WebSocketServerMiddleware) as ISuperSocketHostBuilder<WebSocketPackage>;
	}

	public static ISuperSocketHostBuilder<WebSocketPackage> UseWebSocketMessageHandler(this ISuperSocketHostBuilder<WebSocketPackage> builder, Func<WebSocketSession, WebSocketPackage, ValueTask> handler)
	{
		return builder.ConfigureServices(delegate(HostBuilderContext ctx, IServiceCollection services)
		{
			services.AddSingleton(handler);
		});
	}

	public static ISuperSocketHostBuilder<WebSocketPackage> UseWebSocketMessageHandler(this ISuperSocketHostBuilder<WebSocketPackage> builder, string protocol, Func<WebSocketSession, WebSocketPackage, ValueTask> handler)
	{
		return builder.ConfigureServices(delegate(HostBuilderContext ctx, IServiceCollection services)
		{
			services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(ISubProtocolHandler), new DelegateSubProtocolHandler(protocol, handler)));
		});
	}

	public static ISuperSocketHostBuilder<WebSocketPackage> UseCommand<TPackageInfo, TPackageMapper>(this ISuperSocketHostBuilder<WebSocketPackage> builder) where TPackageInfo : class where TPackageMapper : class, IPackageMapper<WebSocketPackage, TPackageInfo>
	{
		Type keyType = CommandMiddlewareExtensions.GetKeyType<TPackageInfo>();
		Type commandMiddlewareType = typeof(WebSocketCommandMiddleware<, >).MakeGenericType(keyType, typeof(TPackageInfo));
		return builder.ConfigureServices(delegate(HostBuilderContext ctx, IServiceCollection services)
		{
			services.AddSingleton(typeof(IWebSocketCommandMiddleware), commandMiddlewareType);
			services.AddSingleton<IPackageMapper<WebSocketPackage, TPackageInfo>, TPackageMapper>();
		}).ConfigureServices(delegate(HostBuilderContext ctx, IServiceCollection services)
		{
			services.Configure<CommandOptions>(ctx.Configuration?.GetSection("serverOptions")?.GetSection("commands"));
		});
	}

	public static ISuperSocketHostBuilder<WebSocketPackage> UseCommand<TPackageInfo, TPackageMapper>(this ISuperSocketHostBuilder<WebSocketPackage> builder, Action<CommandOptions> configurator) where TPackageInfo : class where TPackageMapper : class, IPackageMapper<WebSocketPackage, TPackageInfo>, new()
	{
		return builder.UseCommand<TPackageInfo, TPackageMapper>().ConfigureServices(delegate(HostBuilderContext ctx, IServiceCollection services)
		{
			services.Configure(configurator);
		});
	}

	public static ISuperSocketHostBuilder<WebSocketPackage> UseCommand<TPackageInfo, TPackageMapper>(this ISuperSocketHostBuilder<WebSocketPackage> builder, string protocol, Action<CommandOptions> commandOptionsAction = null) where TPackageInfo : class where TPackageMapper : class, IPackageMapper<WebSocketPackage, TPackageInfo>
	{
		return builder.ConfigureServices(delegate(HostBuilderContext ctx, IServiceCollection services)
		{
			CommandOptions commandOptions = new CommandOptions();
			ctx.Configuration?.GetSection("serverOptions")?.GetSection("commands")?.GetSection(protocol)?.Bind(commandOptions);
			commandOptionsAction?.Invoke(commandOptions);
			OptionsWrapper<CommandOptions> commandOptionsWrapper = new OptionsWrapper<CommandOptions>(commandOptions);
			services.TryAddEnumerable(ServiceDescriptor.Singleton<ISubProtocolHandler, CommandSubProtocolHandler<TPackageInfo>>(delegate(IServiceProvider sp)
			{
				TPackageMapper mapper = ActivatorUtilities.CreateInstance<TPackageMapper>(sp, Array.Empty<object>());
				return new CommandSubProtocolHandler<TPackageInfo>(protocol, sp, commandOptionsWrapper, mapper);
			}));
		});
	}

	public static ISuperSocketHostBuilder<WebSocketPackage> UsePerMessageCompression(this ISuperSocketHostBuilder<WebSocketPackage> builder)
	{
		return builder.ConfigureServices(delegate(HostBuilderContext ctx, IServiceCollection services)
		{
			services.TryAddEnumerable(ServiceDescriptor.Singleton<IWebSocketExtensionFactory, WebSocketPerMessageCompressionExtensionFactory>());
		});
	}

	public static MultipleServerHostBuilder AddWebSocketServer(this MultipleServerHostBuilder hostBuilder, Action<ISuperSocketHostBuilder<WebSocketPackage>> hostBuilderDelegate)
	{
		return hostBuilder.AddWebSocketServer<SuperSocketService<WebSocketPackage>>(hostBuilderDelegate);
	}

	public static MultipleServerHostBuilder AddWebSocketServer<TWebSocketService>(this MultipleServerHostBuilder hostBuilder, Action<ISuperSocketHostBuilder<WebSocketPackage>> hostBuilderDelegate) where TWebSocketService : SuperSocketService<WebSocketPackage>
	{
		WebSocketHostBuilderAdapter webSocketHostBuilderAdapter = new WebSocketHostBuilderAdapter(hostBuilder);
		webSocketHostBuilderAdapter.UseHostedService<TWebSocketService>();
		hostBuilderDelegate?.Invoke(webSocketHostBuilderAdapter);
		hostBuilder.AddServer(webSocketHostBuilderAdapter);
		return hostBuilder;
	}

	public static WebSocketHostBuilder AsWebSocketHostBuilder(this IHostBuilder hostBuilder)
	{
		return WebSocketHostBuilder.Create(hostBuilder);
	}
}
