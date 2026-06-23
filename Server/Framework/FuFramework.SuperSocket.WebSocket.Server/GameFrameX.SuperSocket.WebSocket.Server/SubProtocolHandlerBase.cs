using System.Threading;
using System.Threading.Tasks;
using FuFramework.SuperSocket.Server.Abstractions;
using FuFramework.SuperSocket.Server.Abstractions.Session;

namespace FuFramework.SuperSocket.WebSocket.Server;

internal abstract class SubProtocolHandlerBase : ISubProtocolHandler, IPackageHandler<WebSocketPackage>
{
	public string Name { get; }

	public SubProtocolHandlerBase(string name)
	{
		Name = name;
	}

	public abstract ValueTask Handle(IAppSession session, WebSocketPackage package, CancellationToken cancellationToken);
}
