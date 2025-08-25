using System;
using System.Net;
using System.Threading.Tasks;
using FuFramework.Core.Runtime;
using UnityWebSocket;

// ReSharper disable once CheckNamespace
namespace FuFramework.Network.Runtime
{
    public partial class NetworkManager
    {
        /// <summary>
        /// WebSocket 网络套接字
        /// </summary>
        private sealed class WebSocketNetSocket : INetworkSocket
        {
            private readonly IWebSocket m_client;

            /// <summary>
            /// 是否正在连接
            /// </summary>
            private bool m_isConnecting;

            private TaskCompletionSource<bool> m_connectTask = new(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly Action<byte[]> m_onReceiveAction;
            private readonly Action<string> m_onCloseAction;

            public WebSocketNetSocket(string url, Action<byte[]> onReceiveAction, Action<string> onCloseAction)
            {
                m_client = new WebSocket(url);
                m_onReceiveAction = onReceiveAction;
                m_onCloseAction = onCloseAction;
                m_client.OnOpen += OnOpen;
                m_client.OnError += OnError;
                m_client.OnClose += OnClose;
                m_client.OnMessage += OnMessage;
            }

            private void OnMessage(object sender, MessageEventArgs e)
            {
                if (e.IsBinary)
                    m_onReceiveAction.Invoke(e.RawData);
            }

            private void OnClose(object sender, CloseEventArgs e)
            {
                m_onCloseAction?.Invoke(e.Reason + " " + e.Code);
            }

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

                Log.Error(e.Message);
                m_connectTask.TrySetResult(false);
            }

            private void OnOpen(object sender, OpenEventArgs e)
            {
                m_isConnecting = false;
                m_connectTask.TrySetResult(true);
            }


            public async Task ConnectAsync()
            {
                m_isConnecting = true;
                m_connectTask = new TaskCompletionSource<bool>();
                m_client.ConnectAsync();
                await m_connectTask.Task;
            }

            public IWebSocket Client => m_client;

            public bool IsConnected => m_client.IsConnected;

            public bool IsClosed { get; private set; }

            public EndPoint LocalEndPoint => null;

            public EndPoint RemoteEndPoint => null;

            public int ReceiveBufferSize { get; set; }
            public int SendBufferSize { get; set; }

            public void Shutdown()
            {
                if (IsClosed) return;
                m_client.CloseAsync();
            }

            public void Close()
            {
                if (IsClosed) return;
                m_client.CloseAsync();
                IsClosed = true;
            }
        }
    }
}