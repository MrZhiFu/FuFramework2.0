using System;
using System.Collections.Generic;
using System.Linq;
using FuFramework.SuperSocket.Primitives;
using FuFramework.SuperSocket.ProtoBase;
using FuFramework.SuperSocket.Server.Abstractions;
using FuFramework.SuperSocket.Server.Abstractions.Connections;
using FuFramework.SuperSocket.Server.Abstractions.Host;
using FuFramework.SuperSocket.Server.Abstractions.Middleware;
using FuFramework.SuperSocket.Server.Abstractions.Session;
using FuFramework.SuperSocket.Server.Connection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace FuFramework.SuperSocket.Server.Host;

public class SuperSocketHostBuilder<TReceivePackage> : HostBuilderAdapter<SuperSocketHostBuilder<TReceivePackage>>, ISuperSocketHostBuilder<TReceivePackage>, ISuperSocketHostBuilder, IHostBuilder, IMinimalApiHostBuilder
{
	private Func<HostBuilderContext, IConfiguration, IConfiguration> _serverOptionsReader;

	protected List<Action<HostBuilderContext, IServiceCollection>> ConfigureSupplementServicesActions = new List<Action<HostBuilderContext, IServiceCollection>>();

	protected List<Action<HostBuilderContext, IServiceCollection>> ConfigureServicesActions { get; private set; } = new List<Action<HostBuilderContext, IServiceCollection>>();

	public SuperSocketHostBuilder(IHostBuilder hostBuilder)
		: base(hostBuilder)
	{
	}

	public SuperSocketHostBuilder()
		: this((string[])null)
	{
	}

	public SuperSocketHostBuilder(string[] args)
		: base(args)
	{
	}

	private void ConfigureHostBuilder()
	{
		base.HostBuilder.ConfigureServices(delegate(HostBuilderContext ctx, IServiceCollection services)
		{
			RegisterBasicServices(ctx, services, services);
		}).ConfigureServices(delegate(HostBuilderContext ctx, IServiceCollection services)
		{
			foreach (Action<HostBuilderContext, IServiceCollection> configureServicesAction in ConfigureServicesActions)
			{
				configureServicesAction(ctx, services);
			}
			foreach (Action<HostBuilderContext, IServiceCollection> configureSupplementServicesAction in ConfigureSupplementServicesActions)
			{
				configureSupplementServicesAction(ctx, services);
			}
		}).ConfigureServices(delegate(HostBuilderContext ctx, IServiceCollection services)
		{
			RegisterDefaultServices(ctx, services, services);
		});
	}

	void IMinimalApiHostBuilder.ConfigureHostBuilder()
	{
		ConfigureHostBuilder();
	}

	public override IHost Build()
	{
		ConfigureHostBuilder();
		return base.HostBuilder.Build();
	}

	public ISuperSocketHostBuilder<TReceivePackage> ConfigureSupplementServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
	{
		ConfigureSupplementServicesActions.Add(configureDelegate);
		return this;
	}

	ISuperSocketHostBuilder ISuperSocketHostBuilder.ConfigureSupplementServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
	{
		return ConfigureSupplementServices(configureDelegate);
	}

	protected virtual void RegisterBasicServices(HostBuilderContext builderContext, IServiceCollection servicesInHost, IServiceCollection services)
	{
		Func<HostBuilderContext, IConfiguration, IConfiguration> func = _serverOptionsReader;
		if (func == null)
		{
			func = (HostBuilderContext ctx, IConfiguration config) => config;
		}
		services.AddOptions();
		IConfigurationSection section = builderContext.Configuration.GetSection("serverOptions");
		IConfiguration config2 = func(builderContext, section);
		services.Configure<ServerOptions>(config2);
	}

	protected virtual void RegisterDefaultServices(HostBuilderContext builderContext, IServiceCollection servicesInHost, IServiceCollection services)
	{
		if (typeof(TReceivePackage) == typeof(StringPackageInfo))
		{
			services.TryAdd(ServiceDescriptor.Singleton<IPackageDecoder<StringPackageInfo>, DefaultStringPackageDecoder>());
		}
		services.TryAdd(ServiceDescriptor.Singleton<IPackageEncoder<string>, DefaultStringEncoderForDI>());
		services.TryAdd(ServiceDescriptor.Singleton<ISessionFactory, DefaultSessionFactory>());
		services.TryAdd(ServiceDescriptor.Singleton<IConnectionListenerFactory, TcpConnectionListenerFactory>());
		services.TryAdd(ServiceDescriptor.Singleton(new SocketOptionsSetter(delegate
		{
		})));
		services.TryAdd(ServiceDescriptor.Singleton<IConnectionFactoryBuilder, ConnectionFactoryBuilder>());
		services.TryAdd(ServiceDescriptor.Singleton<IConnectionStreamInitializersFactory, DefaultConnectionStreamInitializersFactory>());
		if (!CheckIfExistHostedService(services))
		{
			RegisterDefaultHostedService(servicesInHost);
		}
	}

	protected virtual bool CheckIfExistHostedService(IServiceCollection services)
	{
		return services.Any((ServiceDescriptor s) => s.ServiceType == typeof(IHostedService) && typeof(SuperSocketService<TReceivePackage>).IsAssignableFrom(GetImplementationType(s)));
	}

	private Type GetImplementationType(ServiceDescriptor serviceDescriptor)
	{
		if (serviceDescriptor.ImplementationType != null)
		{
			return serviceDescriptor.ImplementationType;
		}
		if (serviceDescriptor.ImplementationInstance != null)
		{
			return serviceDescriptor.ImplementationInstance.GetType();
		}
		if (serviceDescriptor.ImplementationFactory != null)
		{
			Type[] genericTypeArguments = serviceDescriptor.ImplementationFactory.GetType().GenericTypeArguments;
			if (genericTypeArguments.Length == 2)
			{
				return genericTypeArguments[1];
			}
		}
		return null;
	}

	protected virtual void RegisterDefaultHostedService(IServiceCollection servicesInHost)
	{
		RegisterHostedService<SuperSocketService<TReceivePackage>>(servicesInHost);
	}

	protected virtual void RegisterHostedService<THostedService>(IServiceCollection servicesInHost) where THostedService : class, IHostedService
	{
		servicesInHost.AddSingleton<THostedService, THostedService>();
		servicesInHost.AddSingleton((IServiceProvider s) => s.GetService<THostedService>() as IServerInfo);
		servicesInHost.AddHostedService((IServiceProvider s) => s.GetService<THostedService>());
	}

	public ISuperSocketHostBuilder<TReceivePackage> ConfigureServerOptions(Func<HostBuilderContext, IConfiguration, IConfiguration> serverOptionsReader)
	{
		_serverOptionsReader = serverOptionsReader;
		return this;
	}

	ISuperSocketHostBuilder<TReceivePackage> ISuperSocketHostBuilder<TReceivePackage>.ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
	{
		return ConfigureServices(configureDelegate);
	}

	public override SuperSocketHostBuilder<TReceivePackage> ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
	{
		ConfigureServicesActions.Add(configureDelegate);
		return this;
	}

	public virtual ISuperSocketHostBuilder<TReceivePackage> UsePipelineFilter<TPipelineFilter>() where TPipelineFilter : IPipelineFilter<TReceivePackage>, new()
	{
		return ConfigureServices(delegate(HostBuilderContext ctx, IServiceCollection services)
		{
			services.AddSingleton<IPipelineFilterFactory<TReceivePackage>, DefaultPipelineFilterFactory<TReceivePackage, TPipelineFilter>>();
			services.AddSingleton((IServiceProvider serviceProvider) => serviceProvider.GetRequiredService<IPipelineFilterFactory<TReceivePackage>>() as IPipelineFilterFactory);
		});
	}

	public virtual ISuperSocketHostBuilder<TReceivePackage> UsePipelineFilterFactory<TPipelineFilterFactory>() where TPipelineFilterFactory : class, IPipelineFilterFactory<TReceivePackage>
	{
		return ConfigureServices(delegate(HostBuilderContext ctx, IServiceCollection services)
		{
			services.AddSingleton<IPipelineFilterFactory<TReceivePackage>, TPipelineFilterFactory>();
			services.AddSingleton((IServiceProvider serviceProvider) => serviceProvider.GetRequiredService<IPipelineFilterFactory<TReceivePackage>>() as IPipelineFilterFactory);
		});
	}

	public virtual ISuperSocketHostBuilder<TReceivePackage> UseSession<TSession>() where TSession : IAppSession
	{
		return UseSessionFactory<GenericSessionFactory<TSession>>();
	}

	public virtual ISuperSocketHostBuilder<TReceivePackage> UseSessionFactory<TSessionFactory>() where TSessionFactory : class, ISessionFactory
	{
		return ConfigureServices(delegate(HostBuilderContext hostCtx, IServiceCollection services)
		{
			services.AddSingleton<ISessionFactory, TSessionFactory>();
		});
	}

	public virtual ISuperSocketHostBuilder<TReceivePackage> UseHostedService<THostedService>() where THostedService : class, IHostedService
	{
		if (!typeof(SuperSocketService<TReceivePackage>).IsAssignableFrom(typeof(THostedService)))
		{
			throw new ArgumentException("The type parameter should be subclass of SuperSocketService", "THostedService");
		}
		return ConfigureServices(delegate(HostBuilderContext ctx, IServiceCollection services)
		{
			RegisterHostedService<THostedService>(services);
		});
	}

	public virtual ISuperSocketHostBuilder<TReceivePackage> UsePackageDecoder<TPackageDecoder>() where TPackageDecoder : class, IPackageDecoder<TReceivePackage>
	{
		return ConfigureServices(delegate(HostBuilderContext hostCtx, IServiceCollection services)
		{
			services.AddSingleton<IPackageDecoder<TReceivePackage>, TPackageDecoder>();
		});
	}

	public virtual ISuperSocketHostBuilder<TReceivePackage> UsePackageEncoder<TPackageEncoder>() where TPackageEncoder : class, IPackageEncoder<TReceivePackage>
	{
		return ConfigureServices(delegate(HostBuilderContext hostCtx, IServiceCollection services)
		{
			services.AddSingleton<IPackageEncoder<TReceivePackage>, TPackageEncoder>();
		});
	}

	public virtual ISuperSocketHostBuilder<TReceivePackage> UseMiddleware<TMiddleware>() where TMiddleware : class, IMiddleware
	{
		return ConfigureServices(delegate(HostBuilderContext ctx, IServiceCollection services)
		{
			services.TryAddEnumerable(ServiceDescriptor.Singleton<IMiddleware, TMiddleware>());
		});
	}

	public ISuperSocketHostBuilder<TReceivePackage> UsePackageHandlingScheduler<TPackageHandlingScheduler>() where TPackageHandlingScheduler : class, IPackageHandlingScheduler<TReceivePackage>
	{
		return ConfigureServices(delegate(HostBuilderContext hostCtx, IServiceCollection services)
		{
			services.AddSingleton<IPackageHandlingScheduler<TReceivePackage>, TPackageHandlingScheduler>();
		});
	}

	public ISuperSocketHostBuilder<TReceivePackage> UsePackageHandlingContextAccessor()
	{
		return ConfigureServices(delegate(HostBuilderContext hostCtx, IServiceCollection services)
		{
			services.AddSingleton<IPackageHandlingContextAccessor<TReceivePackage>, PackageHandlingContextAccessor<TReceivePackage>>();
		});
	}

	public ISuperSocketHostBuilder<TReceivePackage> UseGZip()
	{
		return HostBuilderExtensions.UseGZip(this) as ISuperSocketHostBuilder<TReceivePackage>;
	}
}
public static class SuperSocketHostBuilder
{
	public static ISuperSocketHostBuilder<TReceivePackage> Create<TReceivePackage>() where TReceivePackage : class
	{
		return Create<TReceivePackage>(null);
	}

	public static ISuperSocketHostBuilder<TReceivePackage> Create<TReceivePackage>(string[] args)
	{
		return new SuperSocketHostBuilder<TReceivePackage>(args);
	}

	public static ISuperSocketHostBuilder<TReceivePackage> Create<TReceivePackage, TPipelineFilter>() where TPipelineFilter : IPipelineFilter<TReceivePackage>, new()
	{
		return Create<TReceivePackage, TPipelineFilter>(null);
	}

	public static ISuperSocketHostBuilder<TReceivePackage> Create<TReceivePackage, TPipelineFilter>(string[] args) where TPipelineFilter : IPipelineFilter<TReceivePackage>, new()
	{
		return new SuperSocketHostBuilder<TReceivePackage>(args).UsePipelineFilter<TPipelineFilter>();
	}
}
