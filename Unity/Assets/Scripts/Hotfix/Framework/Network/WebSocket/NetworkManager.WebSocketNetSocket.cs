using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;
using UnityWebSocket;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
    public partial class NetworkModule
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

            private UniTaskCompletionSource<bool> m_connectTask = new();

            private readonly Action<byte[]> m_onReceiveAction;
            private readonly Action<string> m_onCloseAction;

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

                FuLogger.LogError(e.Message);
                m_connectTask.TrySetResult(false);
            }

            private void OnOpen(object sender, OpenEventArgs e)
            {
                m_isConnecting = false;
                m_connectTask.TrySetResult(true);
            }


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

            public IWebSocket Client => m_client;

            public bool IsConnected => m_client.IsConnected;

            public bool IsClosed { get; private set; }

            public EndPoint LocalEndPoint => null;

            public EndPoint RemoteEndPoint => null;

            public int ReceiveBufferSize { get; set; }
            public int SendBufferSize    { get; set; }

            public void Shutdown()
            {
                if (IsClosed) return;
                IsClosed = true;

                // 退订全部回调：否则 m_client 会一直持有本对象（含 2×64KB 接收缓冲等），
                // 频道销毁后仍无法被回收。
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

            public void Close()
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
}
