using System.Net.Sockets;

namespace FuFramework.SuperSocket.Connection;

public static class SocketExtensions
{
	public static bool IsIgnorableSocketException(this SocketException se)
	{
		SocketError socketErrorCode = se.SocketErrorCode;
		if (socketErrorCode == SocketError.OperationAborted || (uint)(socketErrorCode - 10052) <= 2u || socketErrorCode == SocketError.TimedOut)
		{
			return true;
		}
		return false;
	}
}
