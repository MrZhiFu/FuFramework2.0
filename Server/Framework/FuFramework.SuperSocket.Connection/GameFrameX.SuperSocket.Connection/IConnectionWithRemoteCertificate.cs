using System.Security.Cryptography.X509Certificates;

namespace FuFramework.SuperSocket.Connection;

public interface IConnectionWithRemoteCertificate
{
	X509Certificate RemoteCertificate { get; }
}
