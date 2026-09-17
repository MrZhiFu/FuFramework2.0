using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using FuFramework.SuperSocket.Connection;
using FuFramework.SuperSocket.Primitives;
using FuFramework.SuperSocket.ProtoBase;
using FuFramework.SuperSocket.Server.Abstractions;
using FuFramework.SuperSocket.Server.Abstractions.Connections;
using FuFramework.SuperSocket.Server.Abstractions.Host;
using FuFramework.SuperSocket.Server.Abstractions.Session;
using FuFramework.SuperSocket.Server.Connection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FuFramework.SuperSocket.Server.Host;

public static class HostBuilderExtensions
{
	public static ISuperSocketHostBuilder<TReceivePackage> AsSuperSocketHostBuilder<TReceivePackage>(this IHostBuilder hostBuilder)
	{
		if (hostBuilder is ISuperSocketHostBuilder<TReceivePackage> result)
		{
			return result;
		}
		return new SuperSocketHostBuilder<TReceivePackage>(hostBuilder);
	}

	public static ISuperSocketHostBuilder<TReceivePackage> AsSuperSocketHostBuilder<TReceivePackage, TPipelineFilter>(this IHostBuilder hostBuilder) where TPipelineFilter : IPipelineFilter<TReceivePackage>, new()
	{
		if (hostBuilder is ISuperSocketHostBuilder<TReceivePackage> result)
		{
			return result;
		}
		return new SuperSocketHostBuilder<TReceivePackage>(hostBuilder).UsePipelineFilter<TPipelineFilter>();
	}

	public static ISuperSocketHostBuilder<TReceivePackage> UsePipelineFilterFactory<TReceivePackage>(this ISuperSocketHostBuilder<TReceivePackage> hostBuilder, Func<object, IPipelineFilter<TReceivePackage>> filterFactory)
	{
		hostBuilder.ConfigureServices(delegate(HostBuilderContext hostCtx, IServiceCollection services)
		{
			services.AddSingleton(filterFactory);
		});
		return hostBuilder.UsePipelineFilterFactory<DelegatePipelineFilterFactory<TReceivePackage>>();
	}

	public static ISuperSocketHostBuilder<TReceivePackage> UseClearIdleSession<TReceivePackage>(this ISuperSocketHostBuilder<TReceivePackage> hostBuilder)
	{
		return hostBuilder.UseMiddleware<ClearIdleSessionMiddleware>();
	}

	public static ISuperSocketHostBuilder<TReceivePackage> UseSessionHandler<TReceivePackage>(this ISuperSocketHostBuilder<TReceivePackage> hostBuilder, Func<IAppSession, ValueTask> onConnected = null, Func<IAppSession, CloseEventArgs, ValueTask> onClosed = null)
	{
		return hostBuilder.ConfigureServices(delegate(HostBuilderContext hostCtx, IServiceCollection services)
		{
			services.AddSingleton(new SessionHandlers
			{
				Connected = onConnected,
				Closed = onClosed
			});
		});
	}

	public static ISuperSocketHostBuilder<TReceivePackage> ConfigureSuperSocket<TReceivePackage>(this ISuperSocketHostBuilder<TReceivePackage> hostBuilder, Action<ServerOptions> configurator)
	{
		return hostBuilder.ConfigureServices(delegate(HostBuilderContext hostCtx, IServiceCollection services)
		{
			services.Configure(configurator);
		});
	}

	public static ISuperSocketHostBuilder<TReceivePackage> ConfigureSocketOptions<TReceivePackage>(this ISuperSocketHostBuilder<TReceivePackage> hostBuilder, Action<Socket> socketOptionsSetter) where TReceivePackage : class
	{
		return hostBuilder.ConfigureServices(delegate(HostBuilderContext hostCtx, IServiceCollection services)
		{
			services.AddSingleton(new SocketOptionsSetter(socketOptionsSetter));
		});
	}

	public static IServer BuildAsServer(this IHostBuilder hostBuilder)
	{
		return hostBuilder.Build().AsServer();
	}

	public static IServer AsServer(this IHost host)
	{
		return host.Services.GetService<IEnumerable<IHostedService>>().OfType<IServer>().FirstOrDefault();
	}

	public static ISuperSocketHostBuilder<TReceivePackage> ConfigureErrorHandler<TReceivePackage>(this ISuperSocketHostBuilder<TReceivePackage> hostBuilder, Func<IAppSession, PackageHandlingException<TReceivePackage>, ValueTask<bool>> errorHandler)
	{
		return hostBuilder.ConfigureServices(delegate(HostBuilderContext hostCtx, IServiceCollection services)
		{
			services.AddSingleton(errorHandler);
		});
	}

	public static ISuperSocketHostBuilder<TReceivePackage> UsePackageHandler<TReceivePackage>(this ISuperSocketHostBuilder<TReceivePackage> hostBuilder, Func<IAppSession, TReceivePackage, ValueTask> packageHandler, Func<IAppSession, PackageHandlingException<TReceivePackage>, ValueTask<bool>> errorHandler = null)
	{
		return hostBuilder.ConfigureServices(delegate(HostBuilderContext hostCtx, IServiceCollection services)
		{
			if (packageHandler != null)
			{
				services.AddSingleton((IPackageHandler<TReceivePackage>)new DelegatePackageHandler<TReceivePackage>(packageHandler));
			}
			if (errorHandler != null)
			{
				services.AddSingleton(errorHandler);
			}
		});
	}

	public static MultipleServerHostBuilder AsMultipleServerHostBuilder(this IHostBuilder hostBuilder)
	{
		return new MultipleServerHostBuilder(hostBuilder);
	}

	/// <summary>
	/// Converts an <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to a <see cref="T:FuFramework.SuperSocket.Server.Host.SuperSocketWebApplicationBuilder" />.
	/// </summary>
	/// <param name="hostApplicationBuilder">The host application builder to convert.</param>
	/// <param name="configureServerHostBuilder">The action to configure the server host builder.</param>
	/// <returns>An instance of <see cref="T:FuFramework.SuperSocket.Server.Host.SuperSocketWebApplicationBuilder" />.</returns>
	public static SuperSocketWebApplicationBuilder AsSuperSocketWebApplicationBuilder(this IHostApplicationBuilder hostApplicationBuilder, Action<MultipleServerHostBuilder> configureServerHostBuilder)
	{
		SuperSocketWebApplicationBuilder superSocketWebApplicationBuilder = new SuperSocketWebApplicationBuilder(hostApplicationBuilder);
		MultipleServerHostBuilder multipleServerHostBuilder = new MultipleServerHostBuilder(superSocketWebApplicationBuilder.Host);
		configureServerHostBuilder(multipleServerHostBuilder);
		multipleServerHostBuilder.AsMinimalApiHostBuilder().ConfigureHostBuilder();
		return superSocketWebApplicationBuilder;
	}

	public static IMinimalApiHostBuilder AsMinimalApiHostBuilder(this ISuperSocketHostBuilder hostBuilder)
	{
		return hostBuilder;
	}

	public static ISuperSocketHostBuilder UseGZip(this ISuperSocketHostBuilder hostBuilder)
	{
		return hostBuilder.ConfigureServices(delegate(HostBuilderContext hostCtx, IServiceCollection services)
		{
			services.AddSingleton((IConnectionStreamInitializersFactory)new DefaultConnectionStreamInitializersFactory(CompressionLevel.Optimal));
		}) as ISuperSocketHostBuilder;
	}
}
