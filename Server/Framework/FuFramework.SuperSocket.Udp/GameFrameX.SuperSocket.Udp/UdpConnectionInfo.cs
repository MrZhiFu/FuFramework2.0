using System.Net;
using System.Net.Sockets;
using FuFramework.SuperSocket.Connection;

namespace FuFramework.SuperSocket.Udp;

internal struct UdpConnectionInfo
{
	public Socket Socket { get; set; }

	public ConnectionOptions ConnectionOptions { get; set; }

	public string SessionIdentifier { get; set; }

	public IPEndPoint RemoteEndPoint { get; set; }
}
