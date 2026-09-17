using System;
using System.Net;

namespace FuFramework.SuperSocket.Server.Abstractions;

public class ListenOptions
{
	public string Ip { get; set; }

	public int Port { get; set; }

	public string Path { get; set; }

	public int BackLog { get; set; }

	public bool NoDelay { get; set; }

	/// <summary>
	/// Gets or sets the authentication options for the listener.
	/// </summary>
	public ServerAuthenticationOptions AuthenticationOptions { get; set; }

	public TimeSpan ConnectionAcceptTimeOut { get; set; } = TimeSpan.FromSeconds(5.0);

	public bool UdpExclusiveAddressUse { get; set; } = true;

	public IPEndPoint ToEndPoint()
	{
		string ip = Ip;
		int port = Port;
		IPAddress address = ("any".Equals(ip, StringComparison.OrdinalIgnoreCase) ? IPAddress.Any : ((!"IpV6Any".Equals(ip, StringComparison.OrdinalIgnoreCase)) ? IPAddress.Parse(ip) : IPAddress.IPv6Any));
		return new IPEndPoint(address, port);
	}

	public override string ToString()
	{
		return $"{"Ip"}={Ip}, {"Port"}={Port}, {"AuthenticationOptions"}={AuthenticationOptions}, {"Path"}={Path}, {"BackLog"}={BackLog}, {"NoDelay"}={NoDelay}";
	}
}
