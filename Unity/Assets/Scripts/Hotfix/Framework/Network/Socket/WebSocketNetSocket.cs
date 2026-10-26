using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Cysharp.Threading.Tasks;
using AOT.Framework.Core.Log;
using UnityWebSocket;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
	/// <summary>
	/// WebSocket 网络套接字
	/// </summary>
	internal sealed class WebSocketNetSocket : INetworkSocket
	{
		/// <summary>
		/// 底层 WebSocket 客户端
		/// </summary>
		private readonly IWebSocket m_client;

		/// <summary>
		/// 是否正在连接
		/// </summary>
		private bool m_isConnecting;

		/// <summary>
		/// 连接任务完成源
		/// </summary>
		private UniTaskCompletionSource<bool> m_connectTask = new();

		/// <summary>
		/// 收到底层消息的回调
		/// </summary>
		private readonly Action<byte[]> m_onReceiveAction;

		/// <summary>
		/// 底层连接关闭的回调
		/// </summary>
		private readonly Action<string> m_onCloseAction;

		/// <summary>
		/// 底层 WebSocket 客户端实例
		/// </summary>
		public IWebSocket Client => m_client;

		/// <summary>
		/// 是否已连接
		/// </summary>
		public bool IsConnected => m_client.IsConnected;

		/// <summary>
		/// 是否已关闭
		/// </summary>
		public bool IsClosed { get; private set; }

		/// <summary>
		/// 本地端点（IP 地址 + 端口；WebSocket 不提供，恒为 null）
		/// </summary>
		public EndPoint LocalEndPoint => null;

		/// <summary>
		/// 远程端点（IP 地址 + 端口；WebSocket 不提供，恒为 null）
		/// </summary>
		public EndPoint RemoteEndPoint => null;

		/// <summary>
		/// 接收缓冲区大小。单位字节
		/// </summary>
		public int ReceiveBufferSize { get; set; }

		/// <summary>
		/// 发送缓冲区大小。单位字节
		/// </summary>
		public int SendBufferSize { get; set; }


		/// <summary>
		/// 创建 WebSocket 套接字
		/// </summary>
		/// <param name="url">连接地址</param>
		/// <param name="onReceiveAction">收到消息的回调</param>
		/// <param name="onCloseAction">连接关闭的回调</param>
		public WebSocketNetSocket(string url, Action<byte[]> onReceiveAction, Action<string> onCloseAction)
		{
			m_client           =  new WebSocket(url);
			m_onReceiveAction  =  onReceiveAction;
			m_onCloseAction    =  onCloseAction;
			m_client.OnOpen    += OnOpen;
			m_client.OnError   += OnError;
			m_client.OnClose   += OnClose;
			m_client.OnMessage += OnMessage;
		}

		/// <summary>
		/// 底层消息回调
		/// </summary>
		/// <param name="sender">事件源</param>
		/// <param name="e">消息参数</param>
		private void OnMessage(object sender, MessageEventArgs e)
		{
			if (e.IsBinary)
				m_onReceiveAction.Invoke(e.RawData);
		}

		/// <summary>
		/// 底层连接关闭回调
		/// </summary>
		/// <param name="sender">事件源</param>
		/// <param name="e">关闭参数</param>
		private void OnClose(object sender, CloseEventArgs e)
		{
			m_onCloseAction?.Invoke(e.Reason + " " + e.Code);
		}

		/// <summary>
		/// 底层错误回调
		/// </summary>
		/// <param name="sender">事件源</param>
		/// <param name="e">错误参数</param>
		private void OnError(object sender, ErrorEventArgs e)
		{
			if (m_isConnecting)
			{
				// 连接错误
			}
			else
			{
				// 非连接错误
			}

			FuLogger.LogError(e.Message);
			m_connectTask.TrySetResult(false);
		}

		/// <summary>
		/// 底层连接打开回调
		/// </summary>
		/// <param name="sender">事件源</param>
		/// <param name="e">打开参数</param>
		private void OnOpen(object sender, OpenEventArgs e)
		{
			m_isConnecting = false;
			m_connectTask.TrySetResult(true);
		}
		
		/// <summary>
		/// 异步连接远程主机
		/// </summary>
		/// <param name="cancellationToken">取消令牌（超时或主动断开时取消）</param>
		public async UniTask ConnectAsync(CancellationToken cancellationToken)
		{
			m_isConnecting = true;
			m_connectTask  = new UniTaskCompletionSource<bool>();
			m_client.ConnectAsync();

			// 等待打开/失败；取消（超时或主动断开）时抛出 OperationCanceledException 由调用方处理。
			await m_connectTask.Task.AttachExternalCancellation(cancellationToken);
			if (!m_client.IsConnected)
			{
				throw new SocketException((int)ENetworkErrorCode.ConnectError);
			}
		}

		/// <summary>
		/// 关闭连接并释放底层客户端（退订回调 + 关闭连接）。
		/// </summary>
		public void Dispose()
		{
			if (IsClosed) return;
			IsClosed = true;

			Unsubscribe();
			try
			{
				m_client.CloseAsync();
			}
			catch (Exception e)
			{
				FuLogger.LogError(e.Message);
			}
		}

		/// <summary>
		/// 退订底层客户端的全部事件回调
		/// </summary>
		private void Unsubscribe()
		{
			m_client.OnOpen    -= OnOpen;
			m_client.OnError   -= OnError;
			m_client.OnClose   -= OnClose;
			m_client.OnMessage -= OnMessage;
		}
	}
}