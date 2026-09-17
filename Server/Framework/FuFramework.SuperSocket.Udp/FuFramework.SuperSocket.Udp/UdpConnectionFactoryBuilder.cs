using FuFramework.SuperSocket.Connection;
using FuFramework.SuperSocket.Server.Abstractions;
using FuFramework.SuperSocket.Server.Abstractions.Connections;

namespace FuFramework.SuperSocket.Udp;

public class UdpConnectionFactoryBuilder : IConnectionFactoryBuilder
{
	public IConnectionFactory Build(ListenOptions listenOptions, ConnectionOptions connectionOptions)
	{
		return new UdpConnectionFactory();
	}
}
