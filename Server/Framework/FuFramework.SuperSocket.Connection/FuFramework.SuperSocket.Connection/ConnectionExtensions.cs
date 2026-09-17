using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace FuFramework.SuperSocket.Connection;

public static class ConnectionExtensions
{
	public static X509Certificate GetRemoteCertificate(this IConnection connection)
	{
		if (connection is IStreamConnection { Stream: SslStream { IsAuthenticated: not false } stream })
		{
			return stream.RemoteCertificate;
		}
		return null;
	}
}
