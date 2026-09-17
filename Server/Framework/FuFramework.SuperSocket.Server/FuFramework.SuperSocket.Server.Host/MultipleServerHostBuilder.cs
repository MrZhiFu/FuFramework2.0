using System;
using System.Collections.Generic;
using FuFramework.SuperSocket.ProtoBase;
using FuFramework.SuperSocket.Server.Abstractions.Host;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FuFramework.SuperSocket.Server.Host;

/// <summary>
/// Provides a builder for configuring and managing multiple SuperSocket servers.
/// </summary>
public class MultipleServerHostBuilder : HostBuilderAdapter<MultipleServerHostBuilder>, IMinimalApiHostBuilder
{
	private List<IServerHostBuilderAdapter> _hostBuilderAdapters = new List<IServerHostBuilderAdapter>();

	/// <summary>
	/// Initializes a new instance of the <see cref="T:FuFramework.SuperSocket.Server.Host.MultipleServerHostBuilder" /> class with default settings.
	/// </summary>
	private MultipleServerHostBuilder()
		: this((string[])null)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:FuFramework.SuperSocket.Server.Host.MultipleServerHostBuilder" /> class with the specified arguments.
	/// </summary>
	/// <param name="args">The command-line arguments for the host builder.</param>
	private MultipleServerHostBuilder(string[] args)
		: base(args)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:FuFramework.SuperSocket.Server.Host.MultipleServerHostBuilder" /> class with the specified host builder.
	/// </summary>
	/// <param name="hostBuilder">The host builder to adapt.</param>
	internal MultipleServerHostBuilder(IHostBuilder hostBuilder)
		: base(hostBuilder)
	{
	}

	/// <summary>
	/// Configures the servers with the specified host builder context and services.
	/// </summary>
	/// <param name="context">The context of the host builder.</param>
	/// <param name="hostServices">The collection of services for the host.</param>
	protected virtual void ConfigureServers(HostBuilderContext context, IServiceCollection hostServices)
	{
		foreach (IServerHostBuilderAdapter hostBuilderAdapter in _hostBuilderAdapters)
		{
			hostBuilderAdapter.ConfigureServer(context, hostServices);
		}
	}

	/// <summary>
	/// Builds the host and configures multiple servers.
	/// </summary>
	/// <returns>The built host.</returns>
	public override IHost Build()
	{
		ConfigureServices(ConfigureServers);
		IHost host = base.Build();
		IServiceProvider services = host.Services;
		AdaptMultipleServerHost(services);
		return host;
	}

	/// <summary>
	/// Adapts the services to support multiple servers.
	/// </summary>
	/// <param name="services">The service provider for the host.</param>
	internal void AdaptMultipleServerHost(IServiceProvider services)
	{
		foreach (IServerHostBuilderAdapter hostBuilderAdapter in _hostBuilderAdapters)
		{
			hostBuilderAdapter.ConfigureServiceProvider(services);
		}
	}

	/// <summary>
	/// Creates a new instance of the <see cref="T:FuFramework.SuperSocket.Server.Host.MultipleServerHostBuilder" /> class.
	/// </summary>
	/// <returns>A new instance of <see cref="T:FuFramework.SuperSocket.Server.Host.MultipleServerHostBuilder" />.</returns>
	public static MultipleServerHostBuilder Create()
	{
		return Create(null);
	}

	/// <summary>
	/// Creates a new instance of the <see cref="T:FuFramework.SuperSocket.Server.Host.MultipleServerHostBuilder" /> class with the specified arguments.
	/// </summary>
	/// <param name="args">The command-line arguments for the host builder.</param>
	/// <returns>A new instance of <see cref="T:FuFramework.SuperSocket.Server.Host.MultipleServerHostBuilder" />.</returns>
	public static MultipleServerHostBuilder Create(string[] args)
	{
		return new MultipleServerHostBuilder(args);
	}

	private ServerHostBuilderAdapter<TReceivePackage> CreateServerHostBuilder<TReceivePackage>(Action<SuperSocketHostBuilder<TReceivePackage>> hostBuilderDelegate) where TReceivePackage : class
	{
		ServerHostBuilderAdapter<TReceivePackage> serverHostBuilderAdapter = new ServerHostBuilderAdapter<TReceivePackage>(this);
		hostBuilderDelegate(serverHostBuilderAdapter);
		return serverHostBuilderAdapter;
	}

	/// <summary>
	/// Adds a server to the host builder with the specified configuration.
	/// </summary>
	/// <typeparam name="TReceivePackage">The type of the package received by the server.</typeparam>
	/// <param name="hostBuilderDelegate">The action to configure the server host builder.</param>
	/// <returns>The updated host builder.</returns>
	public MultipleServerHostBuilder AddServer<TReceivePackage>(Action<ISuperSocketHostBuilder<TReceivePackage>> hostBuilderDelegate) where TReceivePackage : class
	{
		ServerHostBuilderAdapter<TReceivePackage> item = CreateServerHostBuilder(hostBuilderDelegate);
		_hostBuilderAdapters.Add(item);
		return this;
	}

	/// <summary>
	/// Adds a server to the host builder with the specified configuration and pipeline filter.
	/// </summary>
	/// <typeparam name="TReceivePackage">The type of the package received by the server.</typeparam>
	/// <typeparam name="TPipelineFilter">The type of the pipeline filter.</typeparam>
	/// <param name="hostBuilderDelegate">The action to configure the server host builder.</param>
	/// <returns>The updated host builder.</returns>
	public MultipleServerHostBuilder AddServer<TReceivePackage, TPipelineFilter>(Action<ISuperSocketHostBuilder<TReceivePackage>> hostBuilderDelegate) where TReceivePackage : class where TPipelineFilter : IPipelineFilter<TReceivePackage>, new()
	{
		ServerHostBuilderAdapter<TReceivePackage> serverHostBuilderAdapter = CreateServerHostBuilder(hostBuilderDelegate);
		_hostBuilderAdapters.Add(serverHostBuilderAdapter);
		serverHostBuilderAdapter.UsePipelineFilter<TPipelineFilter>();
		return this;
	}

	/// <summary>
	/// Adds a server to the host builder using the specified server host builder adapter.
	/// </summary>
	/// <param name="hostBuilderAdapter">The server host builder adapter to add.</param>
	/// <returns>The updated host builder.</returns>
	public MultipleServerHostBuilder AddServer(IServerHostBuilderAdapter hostBuilderAdapter)
	{
		_hostBuilderAdapters.Add(hostBuilderAdapter);
		return this;
	}

	/// <summary>
	/// Adds a server to the host builder with the specified service, package, and pipeline filter types.
	/// </summary>
	/// <typeparam name="TSuperSocketService">The type of the SuperSocket service.</typeparam>
	/// <typeparam name="TReceivePackage">The type of the package received by the server.</typeparam>
	/// <typeparam name="TPipelineFilter">The type of the pipeline filter.</typeparam>
	/// <param name="hostBuilderDelegate">The action to configure the server host builder.</param>
	/// <returns>The updated host builder.</returns>
	public MultipleServerHostBuilder AddServer<TSuperSocketService, TReceivePackage, TPipelineFilter>(Action<SuperSocketHostBuilder<TReceivePackage>> hostBuilderDelegate) where TSuperSocketService : SuperSocketService<TReceivePackage> where TReceivePackage : class where TPipelineFilter : IPipelineFilter<TReceivePackage>, new()
	{
		ServerHostBuilderAdapter<TReceivePackage> serverHostBuilderAdapter = CreateServerHostBuilder(hostBuilderDelegate);
		_hostBuilderAdapters.Add(serverHostBuilderAdapter);
		serverHostBuilderAdapter.UsePipelineFilter<TPipelineFilter>().UseHostedService<TSuperSocketService>();
		return this;
	}

	/// <summary>
	/// Converts the host builder to a minimal API host builder.
	/// </summary>
	/// <returns>An instance of <see cref="T:FuFramework.SuperSocket.Server.Abstractions.Host.IMinimalApiHostBuilder" />.</returns>
	public IMinimalApiHostBuilder AsMinimalApiHostBuilder()
	{
		return this;
	}

	/// <summary>
	/// Configures the host builder for minimal API support.
	/// </summary>
	void IMinimalApiHostBuilder.ConfigureHostBuilder()
	{
		ConfigureServices(ConfigureServers);
		base.HostBuilder.ConfigureServices(delegate(HostBuilderContext _, IServiceCollection services)
		{
			services.AddSingleton(this);
		});
	}
}
