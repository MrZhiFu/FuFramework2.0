using System;
using FuFramework.SuperSocket.Command;
using FuFramework.SuperSocket.ProtoBase;
using FuFramework.SuperSocket.Server.Abstractions.Middleware;
using Microsoft.Extensions.Options;

namespace FuFramework.SuperSocket.WebSocket.Server;

/// <summary>
/// Represents middleware for handling WebSocket commands.
/// </summary>
/// <typeparam name="TKey">The type of the command key.</typeparam>
/// <typeparam name="TPackageInfo">The type of the package information.</typeparam>
public class WebSocketCommandMiddleware<TKey, TPackageInfo> : CommandMiddleware<TKey, WebSocketPackage, TPackageInfo>, IWebSocketCommandMiddleware, IMiddleware where TPackageInfo : class, IKeyedPackageInfo<TKey>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="T:FuFramework.SuperSocket.WebSocket.Server.WebSocketCommandMiddleware`2" /> class.
	/// </summary>
	/// <param name="serviceProvider">The service provider.</param>
	/// <param name="commandOptions">The command options.</param>
	public WebSocketCommandMiddleware(IServiceProvider serviceProvider, IOptions<CommandOptions> commandOptions)
		: base(serviceProvider, commandOptions)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:FuFramework.SuperSocket.WebSocket.Server.WebSocketCommandMiddleware`2" /> class with a package mapper.
	/// </summary>
	/// <param name="serviceProvider">The service provider.</param>
	/// <param name="commandOptions">The command options.</param>
	/// <param name="mapper">The package mapper.</param>
	public WebSocketCommandMiddleware(IServiceProvider serviceProvider, IOptions<CommandOptions> commandOptions, IPackageMapper<WebSocketPackage, TPackageInfo> mapper)
		: base(serviceProvider, commandOptions, mapper)
	{
	}
}
