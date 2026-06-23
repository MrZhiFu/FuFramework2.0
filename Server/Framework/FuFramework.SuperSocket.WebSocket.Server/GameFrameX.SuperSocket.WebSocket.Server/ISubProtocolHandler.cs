using FuFramework.SuperSocket.Server.Abstractions;

namespace FuFramework.SuperSocket.WebSocket.Server;

internal interface ISubProtocolHandler : IPackageHandler<WebSocketPackage>
{
	string Name { get; }
}
