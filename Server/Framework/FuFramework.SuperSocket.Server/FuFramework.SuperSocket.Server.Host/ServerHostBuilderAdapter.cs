using System;
using System.Collections.Generic;
using FuFramework.SuperSocket.Server.Abstractions;
using FuFramework.SuperSocket.Server.Abstractions.Host;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FuFramework.SuperSocket.Server.Host;

/// <summary>
/// Represents a server host builder adapter for SuperSocket.
/// </summary>
/// <typeparam name="TReceivePackage">The type of the received package.</typeparam>
public class ServerHostBuilderAdapter<TReceivePackage> : SuperSocketHostBuilder<TReceivePackage>, IServerHostBuilderAdapter
{
	private IHostBuilder _hostBuilder;

	private IServiceCollection _currentServices = new ServiceCollection();

	private IServiceProvider _serviceProvider;

	private IServiceProvider _hostServiceProvider;

	private Func<HostBuilderContext, IServiceCollection, IServiceProvider> _serviceProviderBuilder;

	private List<IConfigureContainerAdapter> _configureContainerActions = new List<IConfigureContainerAdapter>();

	/// <summary>
	/// Initializes a new instance of the <see cref="T:FuFramework.SuperSocket.Server.Host.ServerHostBuilderAdapter`1" /> class.
	/// </summary>
	/// <param name="hostBuilder">The host builder to adapt.</param>
	public ServerHostBuilderAdapter(IHostBuilder hostBuilder)
		: base(hostBuilder)
	{
		_hostBuilder = hostBuilder;
	}

	/// <summary>
	/// Configures the server with the specified host builder context and service collection.
	/// </summary>
	/// <param name="context">The host builder context.</param>
	/// <param name="hostServices">The service collection for the host.</param>
	void IServerHostBuilderAdapter.ConfigureServer(HostBuilderContext context, IServiceCollection hostServices)
	{
		ConfigureServer(context, hostServices);
	}

	/// <summary>
	/// Configures the server with the specified host builder context and service collection.
	/// </summary>
	/// <param name="context">The host builder context.</param>
	/// <param name="hostServices">The service collection for the host.</param>
	protected void ConfigureServer(HostBuilderContext context, IServiceCollection hostServices)
	{
		IServiceCollection currentServices = _currentServices;
		CopyGlobalServices(hostServices, currentServices);
		RegisterBasicServices(context, hostServices, currentServices);
		foreach (Action<HostBuilderContext, IServiceCollection> configureServicesAction in base.ConfigureServicesActions)
		{
			configureServicesAction(context, currentServices);
		}
		foreach (Action<HostBuilderContext, IServiceCollection> configureSupplementServicesAction in ConfigureSupplementServicesActions)
		{
			configureSupplementServicesAction(context, currentServices);
		}
		RegisterDefaultServices(context, hostServices, currentServices);
		if (_serviceProviderBuilder == null)
		{
			DefaultServiceProviderFactory defaultServiceProviderFactory = new DefaultServiceProviderFactory();
			IServiceCollection containerBuilder = defaultServiceProviderFactory.CreateBuilder(currentServices);
			ConfigureContainerBuilder(context, containerBuilder);
			_serviceProvider = defaultServiceProviderFactory.CreateServiceProvider(containerBuilder);
		}
		else
		{
			_serviceProvider = _serviceProviderBuilder(context, currentServices);
		}
	}

	/// <summary>
	/// Configures the container builder with the specified context and container builder.
	/// </summary>
	/// <param name="context">The host builder context.</param>
	/// <param name="containerBuilder">The container builder to configure.</param>
	private void ConfigureContainerBuilder(HostBuilderContext context, object containerBuilder)
	{
		foreach (IConfigureContainerAdapter configureContainerAction in _configureContainerActions)
		{
			configureContainerAction.ConfigureContainer(context, containerBuilder);
		}
	}

	/// <summary>
	/// Copies global services from the host services to the specified service collection.
	/// </summary>
	/// <param name="hostServices">The host services.</param>
	/// <param name="services">The target service collection.</param>
	private void CopyGlobalServices(IServiceCollection hostServices, IServiceCollection services)
	{
		foreach (ServiceDescriptor hostService in hostServices)
		{
			if (!(hostService.ServiceType == typeof(IHostedService)))
			{
				CopyGlobalServiceDescriptor(hostServices, services, hostService);
			}
		}
	}

	/// <summary>
	/// Copies a global service descriptor from the host services to the specified service collection.
	/// </summary>
	/// <param name="hostServices">The host services.</param>
	/// <param name="services">The target service collection.</param>
	/// <param name="sd">The service descriptor to copy.</param>
	private void CopyGlobalServiceDescriptor(IServiceCollection hostServices, IServiceCollection services, ServiceDescriptor sd)
	{
		if (sd.ImplementationInstance != null)
		{
			services.Add(new ServiceDescriptor(sd.ServiceType, sd.ImplementationInstance));
		}
		else if (sd.ImplementationFactory != null)
		{
			services.Add(new ServiceDescriptor(sd.ServiceType, (IServiceProvider sp) => GetServiceFromHost(sd.ImplementationFactory), sd.Lifetime));
		}
		else
		{
			if (!(sd.ImplementationType != null))
			{
				return;
			}
			if (!sd.ServiceType.IsGenericTypeDefinition && sd.Lifetime == ServiceLifetime.Singleton)
			{
				services.Add(new ServiceDescriptor(sd.ServiceType, (IServiceProvider sp) => _hostServiceProvider.GetService(sd.ServiceType), ServiceLifetime.Singleton));
			}
			else
			{
				services.Add(sd);
			}
		}
	}

	/// <summary>
	/// Gets a service from the host using the specified factory.
	/// </summary>
	/// <param name="factory">The factory to create the service.</param>
	/// <returns>The created service.</returns>
	private object GetServiceFromHost(Func<IServiceProvider, object> factory)
	{
		return factory(_hostServiceProvider);
	}

	/// <summary>
	/// Configures the service provider with the specified host service provider.
	/// </summary>
	/// <param name="hostServiceProvider">The host service provider.</param>
	void IServerHostBuilderAdapter.ConfigureServiceProvider(IServiceProvider hostServiceProvider)
	{
		_hostServiceProvider = hostServiceProvider;
	}

	/// <summary>
	/// Registers a hosted service of the specified type.
	/// </summary>
	/// <typeparam name="THostedService">The type of the hosted service.</typeparam>
	protected void RegisterHostedService<THostedService>() where THostedService : class, IHostedService
	{
		base.HostBuilder.ConfigureServices(delegate(HostBuilderContext context, IServiceCollection services)
		{
			RegisterHostedService<THostedService>(services);
		});
	}

	/// <summary>
	/// Registers a hosted service of the specified type in the provided service collection.
	/// </summary>
	/// <typeparam name="THostedService">The type of the hosted service.</typeparam>
	/// <param name="servicesInHost">The service collection to register the hosted service in.</param>
	protected override void RegisterHostedService<THostedService>(IServiceCollection servicesInHost)
	{
		_currentServices.AddSingleton<IHostedService, THostedService>();
		_currentServices.AddSingleton((IServiceProvider s) => s.GetService<IHostedService>() as IServerInfo);
		servicesInHost.AddHostedService((IServiceProvider s) => GetHostedService<THostedService>());
	}

	protected override void RegisterDefaultHostedService(IServiceCollection servicesInHost)
	{
		RegisterHostedService<SuperSocketService<TReceivePackage>>(servicesInHost);
	}

	/// <summary>
	/// Gets the hosted service of the specified type.
	/// </summary>
	/// <typeparam name="THostedService">The type of the hosted service.</typeparam>
	/// <returns>The hosted service.</returns>
	private THostedService GetHostedService<THostedService>()
	{
		return (THostedService)_serviceProvider.GetService<IHostedService>();
	}

	/// <summary>
	/// Configures the host builder to use a hosted service of the specified type.
	/// </summary>
	/// <typeparam name="THostedService">The type of the hosted service.</typeparam>
	/// <returns>The current instance of <see cref="T:FuFramework.SuperSocket.Server.Abstractions.Host.ISuperSocketHostBuilder`1" />.</returns>
	public override ISuperSocketHostBuilder<TReceivePackage> UseHostedService<THostedService>()
	{
		RegisterHostedService<THostedService>();
		return this;
	}

	/// <summary>
	/// Builds the host. This method is not supported.
	/// </summary>
	/// <returns>Throws a <see cref="T:System.NotSupportedException" />.</returns>
	public override IHost Build()
	{
		throw new NotSupportedException();
	}

	/// <summary>
	/// Configures the container with the specified delegate.
	/// </summary>
	/// <typeparam name="TContainerBuilder">The type of the container builder.</typeparam>
	/// <param name="configureDelegate">The delegate to configure the container.</param>
	/// <returns>The current instance of <see cref="T:FuFramework.SuperSocket.Server.Host.SuperSocketHostBuilder`1" />.</returns>
	public override SuperSocketHostBuilder<TReceivePackage> ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate)
	{
		_configureContainerActions.Add(new ConfigureContainerAdapter<TContainerBuilder>(configureDelegate));
		return this;
	}

	/// <summary>
	/// Configures the host builder to use the specified service provider factory.
	/// </summary>
	/// <typeparam name="TContainerBuilder">The type of the container builder.</typeparam>
	/// <param name="factory">The service provider factory to use.</param>
	/// <returns>The current instance of <see cref="T:FuFramework.SuperSocket.Server.Host.SuperSocketHostBuilder`1" />.</returns>
	public override SuperSocketHostBuilder<TReceivePackage> UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory)
	{
		_serviceProviderBuilder = delegate(HostBuilderContext context, IServiceCollection services)
		{
			TContainerBuilder val = factory.CreateBuilder(services);
			ConfigureContainerBuilder(context, val);
			return factory.CreateServiceProvider(val);
		};
		return this;
	}

	/// <summary>
	/// Configures the host builder to use the specified service provider factory.
	/// </summary>
	/// <typeparam name="TContainerBuilder">The type of the container builder.</typeparam>
	/// <param name="factory">The factory function to create the service provider factory.</param>
	/// <returns>The current instance of <see cref="T:FuFramework.SuperSocket.Server.Host.SuperSocketHostBuilder`1" />.</returns>
	public override SuperSocketHostBuilder<TReceivePackage> UseServiceProviderFactory<TContainerBuilder>(Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory)
	{
		_serviceProviderBuilder = delegate(HostBuilderContext context, IServiceCollection services)
		{
			IServiceProviderFactory<TContainerBuilder> serviceProviderFactory = factory(context);
			TContainerBuilder val = serviceProviderFactory.CreateBuilder(services);
			ConfigureContainerBuilder(context, val);
			return serviceProviderFactory.CreateServiceProvider(val);
		};
		return this;
	}
}
