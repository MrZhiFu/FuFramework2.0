using FuFramework.SuperSocket.Connection;

namespace FuFramework.SuperSocket.Server.Abstractions.Connections;

public interface IConnectionFactoryBuilder
{
	IConnectionFactory Build(ListenOptions listenOptions, ConnectionOptions connectionOptions);
}
