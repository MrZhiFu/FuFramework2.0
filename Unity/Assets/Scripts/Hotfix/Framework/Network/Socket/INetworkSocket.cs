using System.Net;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 网络套接字接口。
	/// </summary>
	public interface INetworkSocket
	{
		/// <summary>
		/// 获取是否已连接。
		/// </summary>
		bool IsConnected { get; }

		/// <summary>
		/// 获取是否已关闭。
		/// </summary>
		bool IsClosed { get; }

		/// <summary>
		/// 获取本地端点（IP 地址 + 端口）。
		/// </summary>
		EndPoint LocalEndPoint { get; }

		/// <summary>
		/// 获取远程端点（IP 地址 + 端口）。
		/// </summary>
		EndPoint RemoteEndPoint { get; }

		/// <summary>
		/// 获取或设置接收缓冲区大小。
		/// </summary>
		int ReceiveBufferSize { get; set; }

		/// <summary>
		/// 获取或设置发送缓冲区大小。
		/// </summary>
		int SendBufferSize { get; set; }

		/// <summary>
		/// 关闭并释放网络套接字（禁用收发 + 释放句柄）。
		/// </summary>
		void Dispose();
	}
}