using System;
using System.Net;

namespace FuFramework.SuperSocket.ClientEngine.Proxy;

public class Socks4aConnector : Socks4Connector
{
	private static Random m_Random = new Random();

	public Socks4aConnector(EndPoint proxyEndPoint, string userID)
		: base(proxyEndPoint, userID)
	{
	}

	public override void Connect(EndPoint remoteEndPoint)
	{
		if (!(remoteEndPoint is DnsEndPoint state))
		{
			OnCompleted(new ProxyEventArgs(new Exception("The argument 'remoteEndPoint' must be a DnsEndPoint")));
			return;
		}
		try
		{
			base.ProxyEndPoint.ConnectAsync(null, ProcessConnect, state);
		}
		catch (Exception innerException)
		{
			OnException(new Exception("Failed to connect proxy server", innerException));
		}
	}

	protected override byte[] GetSendingBuffer(EndPoint targetEndPoint, out int actualLength)
	{
		DnsEndPoint dnsEndPoint = targetEndPoint as DnsEndPoint;
		byte[] array = new byte[Math.Max(8, ((!string.IsNullOrEmpty(base.UserID)) ? ProxyConnectorBase.ASCIIEncoding.GetMaxByteCount(base.UserID.Length) : 0) + 5 + 4 + ProxyConnectorBase.ASCIIEncoding.GetMaxByteCount(dnsEndPoint.Host.Length) + 1)];
		array[0] = 4;
		array[1] = 1;
		array[2] = (byte)(dnsEndPoint.Port / 256);
		array[3] = (byte)(dnsEndPoint.Port % 256);
		array[4] = 0;
		array[5] = 0;
		array[6] = 0;
		array[7] = (byte)m_Random.Next(1, 255);
		actualLength = 8;
		if (!string.IsNullOrEmpty(base.UserID))
		{
			actualLength += ProxyConnectorBase.ASCIIEncoding.GetBytes(base.UserID, 0, base.UserID.Length, array, actualLength);
		}
		array[actualLength++] = 0;
		actualLength += ProxyConnectorBase.ASCIIEncoding.GetBytes(dnsEndPoint.Host, 0, dnsEndPoint.Host.Length, array, actualLength);
		array[actualLength++] = 0;
		return array;
	}

	protected override void HandleFaultStatus(byte status)
	{
		string empty = string.Empty;
		OnException(status switch
		{
			91 => "request rejected or failed", 
			92 => "request failed because client is not running identd (or not reachable from the server)", 
			93 => "request failed because client's identd could not confirm the user ID string in the reques", 
			_ => "request rejected for unknown error", 
		});
	}
}
