using System;
using System.Net.Sockets;

namespace FuFramework.SuperSocket.Server.Abstractions.Connections;

public class SocketOptionsSetter
{
	public Action<Socket> Setter { get; private set; }

	public SocketOptionsSetter(Action<Socket> setter)
	{
		Setter = setter;
	}
}
