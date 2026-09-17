using System;
using System.Threading;
using System.Threading.Tasks;
using FuFramework.SuperSocket.Command;
using FuFramework.SuperSocket.Server.Abstractions;
using FuFramework.SuperSocket.Server.Abstractions.Session;
using Microsoft.Extensions.Options;

namespace FuFramework.SuperSocket.WebSocket.Server;

internal sealed class CommandSubProtocolHandler<TPackageInfo> : SubProtocolHandlerBase
{
	private readonly IPackageHandler<WebSocketPackage> _commandMiddleware;

	public CommandSubProtocolHandler(string name, IServiceProvider serviceProvider, IOptions<CommandOptions> commandOptions, IPackageMapper<WebSocketPackage, TPackageInfo> mapper)
		: base(name)
	{
		Type keyType = CommandMiddlewareExtensions.GetKeyType<TPackageInfo>();
		Type type = typeof(WebSocketCommandMiddleware<, >).MakeGenericType(keyType, typeof(TPackageInfo));
		_commandMiddleware = Activator.CreateInstance(type, serviceProvider, commandOptions, mapper) as IPackageHandler<WebSocketPackage>;
	}

	public override async ValueTask Handle(IAppSession session, WebSocketPackage package, CancellationToken cancellationToken)
	{
		await _commandMiddleware.Handle(session, package, cancellationToken);
	}
}
