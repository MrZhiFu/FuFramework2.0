using System;
using System.Net;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// 网络频道基类——连接分部：连接流程、域名解析与地址族校验。
	/// </summary>
	internal abstract partial class NetworkChannelBase : INetworkChannel, IDisposable
	{
		/// <summary>
		/// 网络地址类型
		/// </summary>
		protected EAddressFamily m_EAddressFamily;

		/// <summary>
		/// 是否验证地址
		/// </summary>
		protected bool m_IsVerifyAddress = true;

		/// <summary>
		/// 是否需要由本类解析主机名。默认需要；子类若自行处理主机名（如 WebSocket）可覆写为 false。
		/// </summary>
		protected virtual bool NeedResolveHost => true;

		/// <summary>
		/// 链接目标地址
		/// </summary>
		protected IPEndPoint m_ConnectEndPoint;

		/// <summary>
		/// 是否正在连接中
		/// </summary>
		protected bool m_IsConnecting = false;

		/// <summary>
		/// 连接到远程主机。
		/// </summary>
		/// <param name="address">远程主机的地址。</param>
		/// <param name="userData">用户自定义数据。</param>
		public virtual void Connect(Uri address, object userData = null)
		{
			if (m_Socket != null)
			{
				Close();
				m_Socket = null;
			}

			m_IsVerifyAddress = true;
			m_ConnectEndPoint = null;

			if (IPAddress.TryParse(address.Host, out var ipAddress))
			{
				m_ConnectEndPoint = new IPEndPoint(ipAddress, address.Port);
				CompleteConnectAddress(userData);
				return;
			}

			if (!NeedResolveHost)
			{
				// 子类自行处理主机名（例如 WebSocket 的 URL 由底层客户端解析）。
				CompleteConnectAddress(userData);
				return;
			}

			// 域名解析（Dns.GetHostEntry）是同步阻塞调用，放在主线程会卡帧，
			// 因此丢到线程池执行，解析完成后再回到主线程继续连接流程。
			ResolveHostAsync(address, userData).Forget();
		}

		/// <summary>
		/// 在线程池解析域名，随后在主线程完成连接流程。
		/// </summary>
		/// <param name="address">远程主机地址</param>
		/// <param name="userData">用户自定义数据</param>
		private async UniTaskVoid ResolveHostAsync(Uri address, object userData)
		{
			var host = address.Host;
			var port = address.Port;
			try
			{
				var ipHost = await UniTask.RunOnThreadPool(() => Utility.Net.GetHostIPv4(host));
				if (IPAddress.TryParse(ipHost, out var ipAddress))
				{
					m_ConnectEndPoint = new IPEndPoint(ipAddress, port);
				}
				else
				{
					// 获取IP失败
					FuLogger.LogError($"IP 地址无效。{host}");
					m_IsVerifyAddress = false;
					Close();
					m_Socket = null;
				}
			}
			catch (Exception e)
			{
				FuLogger.LogError($"IP 地址无效。{host} {e.Message}");
				m_IsVerifyAddress = false;
				Close();
				m_Socket = null;
			}

			CompleteConnectAddress(userData);
		}

		/// <summary>
		/// 地址解析完成后校验地址族并通知子类创建 Socket 发起连接。
		/// </summary>
		/// <param name="userData">用户自定义数据</param>
		private void CompleteConnectAddress(object userData)
		{
			if (m_IsVerifyAddress && m_ConnectEndPoint != null)
			{
				switch (m_ConnectEndPoint.AddressFamily)
				{
					case AddressFamily.InterNetwork:
						m_EAddressFamily = EAddressFamily.IPv4;
						break;

					case AddressFamily.InterNetworkV6:
						m_EAddressFamily = EAddressFamily.IPv6;
						break;

					default:
						var errorMessage = $"不支持的地址族 '{m_ConnectEndPoint.AddressFamily}'。";
						if (m_NetworkChannelError == null) throw new InvalidOperationException(errorMessage);
						EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
						                      errorCode: ENetworkErrorCode.AddressFamilyError,
						                      socketError: SocketError.Success,
						                      errorMessage: errorMessage);
						return;
				}
			}

			m_SendState.Reset();
			m_ReceiveState.PrepareForPacketHeader();

			OnConnectEndPointReady(userData);
		}

		/// <summary>
		/// 连接目标地址已解析完成（或已确认解析失败）时由子类实现：创建 Socket 并发起连接。
		/// </summary>
		/// <param name="userData">用户自定义数据</param>
		protected abstract void OnConnectEndPointReady(object userData);
	}
}
