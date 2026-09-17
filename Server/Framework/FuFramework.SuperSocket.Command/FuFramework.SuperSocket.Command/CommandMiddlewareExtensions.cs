using System;
using System.Collections.Generic;
using System.Linq;
using FuFramework.SuperSocket.ProtoBase;
using FuFramework.SuperSocket.Server.Abstractions.Host;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FuFramework.SuperSocket.Command;

/// <summary>
/// Provides extension methods for configuring and using command middleware in a SuperSocket application.
/// </summary>
public static class CommandMiddlewareExtensions
{
	/// <summary>
	/// Gets the key type from the specified package type.
	/// </summary>
	/// <typeparam name="TPackageInfo">The type of the package.</typeparam>
	/// <returns>The key type of the package.</returns>
	/// <exception cref="T:System.Exception">Thrown if the package type does not implement <see cref="T:FuFramework.SuperSocket.ProtoBase.IKeyedPackageInfo`1" />.</exception>
	public static Type GetKeyType<TPackageInfo>()
	{
		Type? type = typeof(TPackageInfo).GetInterfaces().FirstOrDefault((Type i) => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IKeyedPackageInfo<>));
		if (type == null)
		{
			throw new Exception($"The package type {"TPackageInfo"} should implement the interface {typeof(IKeyedPackageInfo<>).Name}.");
		}
		return type.GetGenericArguments().FirstOrDefault();
	}

	/// <summary>
	/// Configures command options for the SuperSocket host builder.
	/// </summary>
	/// <param name="builder">The SuperSocket host builder.</param>
	/// <returns>The configured host builder.</returns>
	private static ISuperSocketHostBuilder ConfigureCommand(this ISuperSocketHostBuilder builder)
	{
		return builder.ConfigureServices(delegate(HostBuilderContext hostCxt, IServiceCollection services)
		{
			services.Configure<CommandOptions>(hostCxt.Configuration?.GetSection("serverOptions")?.GetSection("commands"));
		}) as ISuperSocketHostBuilder;
	}

	/// <summary>
	/// Adds command middleware to the SuperSocket host builder.
	/// </summary>
	/// <typeparam name="TPackageInfo">The type of the package.</typeparam>
	/// <param name="builder">The SuperSocket host builder.</param>
	/// <returns>The configured host builder.</returns>
	public static ISuperSocketHostBuilder<TPackageInfo> UseCommand<TPackageInfo>(this ISuperSocketHostBuilder<TPackageInfo> builder) where TPackageInfo : class
	{
		Type keyType = GetKeyType<TPackageInfo>();
		return (typeof(CommandMiddlewareExtensions).GetMethod("UseCommand", new Type[1] { typeof(ISuperSocketHostBuilder) }).MakeGenericMethod(keyType, typeof(TPackageInfo)).Invoke(null, new object[1] { builder }) as ISuperSocketHostBuilder).ConfigureCommand() as ISuperSocketHostBuilder<TPackageInfo>;
	}

	/// <summary>
	/// Adds command middleware to the SuperSocket host builder with a configurator.
	/// </summary>
	/// <typeparam name="TPackageInfo">The type of the package.</typeparam>
	/// <param name="builder">The SuperSocket host builder.</param>
	/// <param name="configurator">The configurator for command options.</param>
	/// <returns>The configured host builder.</returns>
	public static ISuperSocketHostBuilder<TPackageInfo> UseCommand<TPackageInfo>(this ISuperSocketHostBuilder<TPackageInfo> builder, Action<CommandOptions> configurator) where TPackageInfo : class
	{
		return builder.UseCommand().ConfigureServices(delegate(HostBuilderContext hostCtx, IServiceCollection services)
		{
			services.Configure(configurator);
		});
	}

	/// <summary>
	/// Adds command middleware to the SuperSocket host builder with a configurator and a key comparer.
	/// </summary>
	/// <typeparam name="TKey">The type of the command key.</typeparam>
	/// <typeparam name="TPackageInfo">The type of the package.</typeparam>
	/// <param name="builder">The SuperSocket host builder.</param>
	/// <param name="configurator">The configurator for command options.</param>
	/// <param name="comparer">The comparer for command keys.</param>
	/// <returns>The configured host builder.</returns>
	public static ISuperSocketHostBuilder<TPackageInfo> UseCommand<TKey, TPackageInfo>(this ISuperSocketHostBuilder<TPackageInfo> builder, Action<CommandOptions> configurator, IEqualityComparer<TKey> comparer) where TPackageInfo : class, IKeyedPackageInfo<TKey>
	{
		return builder.UseCommand(configurator).ConfigureServices(delegate(HostBuilderContext hostCtx, IServiceCollection services)
		{
			services.AddSingleton(comparer);
		});
	}

	/// <summary>
	/// Adds command middleware to the SuperSocket host builder with a specific key type.
	/// </summary>
	/// <typeparam name="TKey">The type of the command key.</typeparam>
	/// <typeparam name="TPackageInfo">The type of the package.</typeparam>
	/// <param name="builder">The SuperSocket host builder.</param>
	/// <returns>The configured host builder.</returns>
	public static ISuperSocketHostBuilder<TPackageInfo> UseCommand<TKey, TPackageInfo>(this ISuperSocketHostBuilder builder) where TPackageInfo : class, IKeyedPackageInfo<TKey>
	{
		return builder.UseMiddleware<CommandMiddleware<TKey, TPackageInfo>>().ConfigureCommand() as ISuperSocketHostBuilder<TPackageInfo>;
	}

	/// <summary>
	/// Adds command middleware to the SuperSocket host builder with a specific key type and a configurator.
	/// </summary>
	/// <typeparam name="TKey">The type of the command key.</typeparam>
	/// <typeparam name="TPackageInfo">The type of the package.</typeparam>
	/// <param name="builder">The SuperSocket host builder.</param>
	/// <param name="configurator">The configurator for command options.</param>
	/// <returns>The configured host builder.</returns>
	public static ISuperSocketHostBuilder<TPackageInfo> UseCommand<TKey, TPackageInfo>(this ISuperSocketHostBuilder builder, Action<CommandOptions> configurator) where TPackageInfo : class, IKeyedPackageInfo<TKey>
	{
		return builder.UseCommand<TKey, TPackageInfo>().ConfigureServices(delegate(HostBuilderContext hostCtx, IServiceCollection services)
		{
			services.Configure(configurator);
		});
	}

	/// <summary>
	/// Adds command middleware to the SuperSocket host builder with a specific key type, a configurator, and a key comparer.
	/// </summary>
	/// <typeparam name="TKey">The type of the command key.</typeparam>
	/// <typeparam name="TPackageInfo">The type of the package.</typeparam>
	/// <param name="builder">The SuperSocket host builder.</param>
	/// <param name="configurator">The configurator for command options.</param>
	/// <param name="comparer">The comparer for command keys.</param>
	/// <returns>The configured host builder.</returns>
	public static ISuperSocketHostBuilder<TPackageInfo> UseCommand<TKey, TPackageInfo>(this ISuperSocketHostBuilder builder, Action<CommandOptions> configurator, IEqualityComparer<TKey> comparer) where TPackageInfo : class, IKeyedPackageInfo<TKey>
	{
		return builder.UseCommand<TKey, TPackageInfo>(configurator).ConfigureServices(delegate(HostBuilderContext hostCtx, IServiceCollection services)
		{
			services.AddSingleton(comparer);
		});
	}
}
