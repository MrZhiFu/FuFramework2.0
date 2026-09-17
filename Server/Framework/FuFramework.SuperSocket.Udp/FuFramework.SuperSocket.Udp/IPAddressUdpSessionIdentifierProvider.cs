using System;
using System.Net;

namespace FuFramework.SuperSocket.Udp;

internal class IPAddressUdpSessionIdentifierProvider : IUdpSessionIdentifierProvider
{
	public string GetSessionIdentifier(IPEndPoint remoteEndPoint, ArraySegment<byte> data)
	{
		return remoteEndPoint.Address.ToString() + ":" + remoteEndPoint.Port;
	}
}
