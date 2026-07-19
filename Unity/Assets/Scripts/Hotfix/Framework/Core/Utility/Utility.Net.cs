using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Core
{
    public static partial class Utility
    {
        /// <summary>
        /// 网络相关的实用函数。
        /// 功能：
        ///     1. 获取第一个可用的端口号。
        ///     2. 获取操作系统已用的端口号。
        ///     3. 获取本机IP地址。
        ///     4. 获取域名的IP地址。
        /// </summary>
        public static class Net
        {
            /// <summary>
            /// 获取第一个可用的端口号(默认从667开始)。
            /// </summary>
            /// <param name="startPort">起始端口号</param>
            /// <param name="maxPort">结束端口号</param>
            /// <returns>返回第一个可用的端口号，如果没有可用端口则返回-1</returns>
            public static int GetFirstAvailablePort(int startPort = 667, int maxPort = 65535)
            {
                for (var i = startPort; i < maxPort; i++)
                {
                    if (PortIsAvailable(i)) return i;
                }

                return -1;
            }

            /// <summary>
            /// 获取操作系统已用的端口号。
            /// </summary>
            /// <returns>返回一个包含所有已用端口号的列表</returns>
            public static List<int> PortIsUsed()
            {
                //获取本地计算机的网络连接和通信统计数据的信息
                var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();

                //返回本地计算机上的所有Tcp监听程序
                var ipsTcp = ipGlobalProperties.GetActiveTcpListeners();

                //返回本地计算机上的所有UDP监听程序
                var ipsUDP = ipGlobalProperties.GetActiveUdpListeners();

                //返回本地计算机上的Internet协议版本4(IPV4 传输控制协议(TCP)连接的信息。
                var tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();

                var allPorts = new List<int>();
                foreach (var ep in ipsTcp)
                {
                    allPorts.Add(ep.Port);
                }

                foreach (var ep in ipsUDP)
                {
                    allPorts.Add(ep.Port);
                }

                foreach (var conn in tcpConnInfoArray)
                {
                    allPorts.Add(conn.LocalEndPoint.Port);
                }

                return allPorts;
            }

            /// <summary>
            /// 检查指定端口是否已用。
            /// </summary>
            /// <param name="port">要检查的端口号</param>
            /// <returns>如果端口可用则返回true，否则返回false</returns>
            public static bool PortIsAvailable(int port)
            {
                var portUsed = PortIsUsed();
                foreach (var p in portUsed)
                {
                    if (p != port) continue;
                    return false;
                }

                return true;
            }

            /// <summary>
            /// 获取域名的IpV4 地址。
            /// </summary>
            /// <param name="domainName">域名</param>
            /// <returns>返回域名的IPv4地址，如果没有则返回空字符串</returns>
            public static string GetHostIPv4(string domainName)
            {
                var iPHostEntry = Dns.GetHostEntry(domainName);
                foreach (var address in iPHostEntry.AddressList)
                {
                    if (address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return address.ToString();
                    }
                }

                return string.Empty;
            }

            /// <summary>
            /// 获取域名的IpV6 地址。
            /// </summary>
            /// <param name="domainName">域名</param>
            /// <returns>返回域名的IPv6地址，如果没有则返回空字符串</returns>
            public static string GetHostIPv6(string domainName)
            {
                var iPHostEntry = Dns.GetHostEntry(domainName);
                foreach (var address in iPHostEntry.AddressList)
                {
                    if (address.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        return address.ToString();
                    }
                }

                return string.Empty;
            }

            /// <summary>
            /// 获取本机ipv4地址。
            /// </summary>
            /// <returns>返回本机的IPv4地址，如果没有则返回空字符串</returns>
            public static string GetIP()
            {
                var hostName    = Dns.GetHostName();
                var iPHostEntry = Dns.GetHostEntry(hostName);
                foreach (var address in iPHostEntry.AddressList)
                {
                    if (address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return address.ToString();
                    }
                }

                return string.Empty;
            }

            /// <summary>
            /// 获取本机IPv6地址。
            /// </summary>
            /// <param name="host">主机名</param>
            /// <returns>返回本机的IPv6地址，如果没有则返回空字符串</returns>
            public static (AddressFamily, string) GetIPv6Address(string host)
            {
                var addresses = Dns.GetHostAddresses(host);

                foreach (var ipAddress in addresses)
                {
                    if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        return (AddressFamily.InterNetworkV6, ipAddress.ToString());
                    }
                }

                foreach (var ipAddress in addresses)
                {
                    if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return (AddressFamily.InterNetwork, ipAddress.ToString());
                    }
                }

                return (AddressFamily.InterNetwork, host);
            }

            /// <summary>
            /// 获取本地的所有IP地址列表。
            /// </summary>
            /// <returns>返回本地的所有IP地址列表</returns>
            public static string[] GetAddressIPs()
            {
                //获取本地的IP地址
                var list       = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
                var addressIPs = new string[list.Length];
                for (var index = 0; index < list.Length; index++)
                {
                    IPAddress address = list[index];
                    addressIPs[index] = address.ToString();
                }

                return addressIPs;
            }

            /// <summary>
            /// 是否有网络。
            /// </summary>
            /// <returns>返回是否有网络</returns>
            public static bool IsReachable()
            {
                return UnityEngine.Application.internetReachability != NetworkReachability.NotReachable;
            }

            /// <summary>
            /// 是否是WIFI网络。
            /// </summary>
            /// <returns>返回是否是WIFI网络</returns>
            public static bool IsWifi()
            {
                return UnityEngine.Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork;
            }

            /// <summary>
            /// 是否是移动网络。
            /// </summary>
            /// <returns>返回是否是移动网络</returns>
            public static bool IsViaCarrierData()
            {
                return UnityEngine.Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork;
            }
        }
    }
}
