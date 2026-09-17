using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace FuFramework.SuperSocket.ClientEngine;

public class NegotiateStreamTcpSession : AuthenticatedStreamTcpSession
{
	protected override void StartAuthenticatedStream(Socket client)
	{
		SecurityOption security = base.Security;
		if (security == null)
		{
			throw new Exception("securityOption was not configured");
		}
		NegotiateStream stream = new NegotiateStream(new NetworkStream(client));
		NetworkCredential credential = security.Credential;
		if (credential == null)
		{
			credential = (NetworkCredential)CredentialCache.DefaultCredentials;
		}
		Task.Run(async delegate
		{
			try
			{
				await stream.AuthenticateAsClientAsync(credential, base.HostName);
			}
			catch (Exception e)
			{
				EnsureSocketClosed();
				OnError(e);
				return;
			}
			OnAuthenticatedStreamConnected(stream);
		});
	}
}
