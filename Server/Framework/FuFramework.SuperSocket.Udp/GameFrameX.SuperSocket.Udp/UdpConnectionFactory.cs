using System.Threading;
using System.Threading.Tasks;
using FuFramework.SuperSocket.Connection;

namespace FuFramework.SuperSocket.Udp;

public class UdpConnectionFactory : IConnectionFactory
{
	public Task<IConnection> CreateConnection(object connection, CancellationToken cancellationToken)
	{
		UdpConnectionInfo udpConnectionInfo = (UdpConnectionInfo)connection;
		return Task.FromResult((IConnection)new UdpPipeConnection(udpConnectionInfo.Socket, udpConnectionInfo.ConnectionOptions, udpConnectionInfo.RemoteEndPoint, udpConnectionInfo.SessionIdentifier));
	}
}
