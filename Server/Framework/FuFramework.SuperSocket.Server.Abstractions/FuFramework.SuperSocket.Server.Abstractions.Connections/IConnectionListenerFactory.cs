using FuFramework.SuperSocket.Connection;
using Microsoft.Extensions.Logging;

namespace FuFramework.SuperSocket.Server.Abstractions.Connections;

public interface IConnectionListenerFactory
{
	IConnectionListener CreateConnectionListener(ListenOptions options, ConnectionOptions connectionOptions, ILoggerFactory loggerFactory);
}
