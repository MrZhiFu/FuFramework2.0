using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace FuFramework.Utility;

/// <summary>
/// 网络帮助类
/// </summary>
public static class NetHelper
{
	/// <summary>
	/// 判断IP地址是否合法
	/// </summary>
	/// <param name="ipAddress">IP地址字符串</param>
	/// <param name="value">解析成功的IPAddress对象</param>
	/// <returns>如果IP地址合法，返回true；否则返回false</returns>
	public static bool IsValidIpAddress(string ipAddress, out IPAddress value)
	{
		return IPAddress.TryParse(ipAddress, out value);
	}

	/// <summary>
	/// 获取第一个可用的端口号
	/// </summary>
	/// <param name="startPort">起始端口号，默认为667</param>
	/// <param name="maxPort">结束端口号，默认为65535</param>
	/// <returns>第一个可用的端口号，如果没有可用端口号则返回-1</returns>
	public static int GetFirstAvailablePort(int startPort = 667, int maxPort = 65535)
	{
		for (int i = startPort; i < maxPort; i++)
		{
			if (PortIsAvailable(i))
			{
				return i;
			}
		}
		return -1;
	}

	/// <summary>
	/// 获取操作系统已用的端口号
	/// </summary>
	/// <returns>包含已用端口号的列表</returns>
	public static List<int> PortIsUsed()
	{
		IPGlobalProperties iPGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
		IPEndPoint[] activeTcpListeners = iPGlobalProperties.GetActiveTcpListeners();
		IPEndPoint[] activeUdpListeners = iPGlobalProperties.GetActiveUdpListeners();
		TcpConnectionInformation[] activeTcpConnections = iPGlobalProperties.GetActiveTcpConnections();
		List<int> list = new List<int>();
		IPEndPoint[] array = activeTcpListeners;
		foreach (IPEndPoint iPEndPoint in array)
		{
			list.Add(iPEndPoint.Port);
		}
		array = activeUdpListeners;
		foreach (IPEndPoint iPEndPoint2 in array)
		{
			list.Add(iPEndPoint2.Port);
		}
		TcpConnectionInformation[] array2 = activeTcpConnections;
		foreach (TcpConnectionInformation tcpConnectionInformation in array2)
		{
			list.Add(tcpConnectionInformation.LocalEndPoint.Port);
		}
		return list;
	}

	/// <summary>
	/// 检查指定端口是否可用
	/// </summary>
	/// <param name="port">要检查的端口号</param>
	/// <returns>如果端口未被使用，返回true；否则返回false</returns>
	public static bool PortIsAvailable(int port)
	{
		bool result = true;
		foreach (int item in PortIsUsed())
		{
			if (item == port)
			{
				result = false;
				break;
			}
		}
		return result;
	}

	/// <summary>
	/// 获取本地IP地址列表
	/// </summary>
	/// <param name="addressFamily">IP地址类型,默认为IPv4</param>
	/// <returns>本地IP地址列表</returns>
	public static List<string> GetLocalIpList(AddressFamily addressFamily = AddressFamily.InterNetwork)
	{
		List<string> list = new List<string>();
		try
		{
			NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
			foreach (NetworkInterface networkInterface in allNetworkInterfaces)
			{
				if (networkInterface.OperationalStatus != OperationalStatus.Up || networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
				{
					continue;
				}
				IPInterfaceProperties iPProperties = networkInterface.GetIPProperties();
				GatewayIPAddressInformationCollection gatewayAddresses = iPProperties.GatewayAddresses;
				if (gatewayAddresses == null || gatewayAddresses.Count == 0)
				{
					continue;
				}
				foreach (UnicastIPAddressInformation unicastAddress in iPProperties.UnicastAddresses)
				{
					if (unicastAddress.Address.AddressFamily == addressFamily)
					{
						list.Add(unicastAddress.Address.ToString());
					}
				}
			}
			return list;
		}
		catch (Exception)
		{
			return new List<string>();
		}
	}
}
