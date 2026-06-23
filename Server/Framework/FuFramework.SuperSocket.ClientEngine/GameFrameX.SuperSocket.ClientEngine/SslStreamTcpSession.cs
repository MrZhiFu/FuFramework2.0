using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace FuFramework.SuperSocket.ClientEngine;

public class SslStreamTcpSession : AuthenticatedStreamTcpSession
{
	protected override void StartAuthenticatedStream(Socket client)
	{
		if (base.Security == null)
		{
			throw new Exception("securityOption was not configured");
		}
		AuthenticateAsClientAsync(new SslStream(new NetworkStream(client), leaveInnerStreamOpen: false, ValidateRemoteCertificate), base.Security);
	}

	private async void AuthenticateAsClientAsync(SslStream sslStream, SecurityOption securityOption)
	{
		try
		{
			await sslStream.AuthenticateAsClientAsync(base.HostName, securityOption.Certificates, securityOption.EnabledSslProtocols, checkCertificateRevocation: false);
		}
		catch (Exception e)
		{
			EnsureSocketClosed();
			OnError(e);
			return;
		}
		OnAuthenticatedStreamConnected(sslStream);
	}

	private bool ValidateRemoteCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
	{
		if (sslPolicyErrors == SslPolicyErrors.None)
		{
			return true;
		}
		if (base.Security.AllowNameMismatchCertificate)
		{
			sslPolicyErrors &= ~SslPolicyErrors.RemoteCertificateNameMismatch;
		}
		if (base.Security.AllowCertificateChainErrors)
		{
			sslPolicyErrors &= ~SslPolicyErrors.RemoteCertificateChainErrors;
		}
		if (sslPolicyErrors == SslPolicyErrors.None)
		{
			return true;
		}
		if (!base.Security.AllowUnstrustedCertificate)
		{
			OnError(new Exception(sslPolicyErrors.ToString()));
			return false;
		}
		if (sslPolicyErrors != 0 && sslPolicyErrors != SslPolicyErrors.RemoteCertificateChainErrors)
		{
			OnError(new Exception(sslPolicyErrors.ToString()));
			return false;
		}
		if (chain != null && chain.ChainStatus != null)
		{
			X509ChainStatus[] chainStatus = chain.ChainStatus;
			for (int i = 0; i < chainStatus.Length; i++)
			{
				X509ChainStatus x509ChainStatus = chainStatus[i];
				if ((!(certificate.Subject == certificate.Issuer) || x509ChainStatus.Status != X509ChainStatusFlags.UntrustedRoot) && x509ChainStatus.Status != 0)
				{
					OnError(new Exception(sslPolicyErrors.ToString()));
					return false;
				}
			}
		}
		return true;
	}
}
