using System;
using System.IO;
using System.Threading;
using System.Net.Sockets;
using AOT.Framework.Core.Log;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
    /// <summary>
    /// Web Socket 网络频道。
    /// </summary>
    internal sealed class WebSocketNetworkChannel : NetworkChannelBase
    {
        /// <summary>
        /// WebSocket 连接超时时间（毫秒）
        /// </summary>
        private const int ConnectTimeoutMilliseconds = 15000;

        /// <summary>
        /// 取消令牌源。每次连接都会重建（见 OnConnectEndPointReady），
        /// 原实现为 readonly 且只 Cancel 不重建，导致 Cancel 之后频道永远无法重连。
        /// </summary>
        private CancellationTokenSource m_CancellationTokenSource = new();

        /// <summary>
        /// 最近一次连接的地址，用于在地址解析完成后创建 WebSocket 客户端。
        /// </summary>
        private Uri m_LastAddress;

        /// <summary>
        /// 初始化网络频道的新实例。
        /// </summary>
        /// <param name="name">网络频道名称。</param>
        /// <param name="networkChannelHelper">网络频道辅助器。</param>
        /// <param name="rpcTimeout">RPC超时时间</param>
        public WebSocketNetworkChannel(string name, INetworkChannelHelper networkChannelHelper, int rpcTimeout) : base(name, networkChannelHelper, rpcTimeout) { }

        /// <summary>
        /// WebSocket 的 URL 由底层客户端自行解析，无需在这里做 DNS 解析。
        /// </summary>
        protected override bool NeedResolveHost => false;

        /// <summary>
        /// 连接到远程主机。
        /// </summary>
        /// <param name="address">远程主机的地址。</param>
        /// <param name="userData">用户自定义数据。</param>
        public override void Connect(Uri address, object userData = null)
        {
            if (PIsConnecting) return;

            m_LastAddress = address;
            base.Connect(address, userData);
        }

        /// <summary>
        /// 地址解析完成后创建 WebSocket 客户端并发起连接。
        /// </summary>
        /// <param name="userData">用户自定义数据</param>
        protected override void OnConnectEndPointReady(object userData)
        {
            // 旧 CTS 可能已在 Close 中被 Cancel，这里必须重建，否则新连接会立刻被判定为已取消。
            m_CancellationTokenSource?.Dispose();
            m_CancellationTokenSource = new CancellationTokenSource();

            PSocket = new WebSocketNetSocket(m_LastAddress.ToString(), ReceiveCallback, CloseCallback);
            if (PSocket == null)
            {
                const string errorMessage = "Initialize network channel failure.";
                if (NetworkChannelError == null) throw new InvalidOperationException(errorMessage);
                EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
                    errorCode: ENetworkErrorCode.SocketError,
                    socketError: SocketError.Success,
                    errorMessage: errorMessage);
                return;
            }

            PNetworkChannelHelper.PrepareForConnecting();
            ConnectAsync(userData).Forget();
        }

        private void CloseCallback(string errorMessage) => Close();

        public override void Close()
        {
            base.Close();
            m_CancellationTokenSource?.Cancel();
        }

        private bool IsClose()
        {
            return PSocket != null && !PSocket.IsConnected && m_CancellationTokenSource != null && m_CancellationTokenSource.IsCancellationRequested;
        }

        protected override bool ProcessSend()
        {
            lock (PSendPacketPool)
            {
                if (PSendPacketPool.Count <= 0) return false;

                while (PSendPacketPool.First != null)
                {
                    var messageObject = PSendPacketPool.First.Value;

                    bool serializeResult;
                    try
                    {
                        serializeResult = ProcessSendMessage(messageObject);
                        DebugSendLog(messageObject);
                    }
                    catch (Exception exception)
                    {
                        PActive = false;
                        if (NetworkChannelError == null) throw;
                        var socketException = exception as SocketException;
                        EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
                            errorCode: ENetworkErrorCode.SerializeError,
                            socketError: socketException?.SocketErrorCode ?? SocketError.Success,
                            errorMessage: exception.ToString());
                        return false;
                    }
                    finally
                    {
                        PSendPacketPool.RemoveFirst();
                    }

                    if (!serializeResult)
                    {
                        const string errorMessage = "Serialized packet failure.";
                        if (NetworkChannelError == null) throw new InvalidOperationException(errorMessage);
                        EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
                            errorCode: ENetworkErrorCode.SerializeError,
                            socketError: SocketError.Success,
                            errorMessage: errorMessage);
                        return false;
                    }

                    PSendState.Reset();
                }

                return true;
            }
        }

        /// <summary>
        /// 处理发送消息对象
        /// </summary>
        /// <param name="messageObject">消息对象</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        protected override bool ProcessSendMessage(MessageObject messageObject)
        {
            if (IsClose())
            {
                PActive = false;
                EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
                    errorCode: ENetworkErrorCode.SocketError,
                    socketError: SocketError.Disconnecting,
                    errorMessage: "Network channel is closing.");
                return false;
            }

            var serializeResult = base.ProcessSendMessage(messageObject);
            if (serializeResult)
            {
                var webSocketClientNetSocket = (WebSocketNetSocket)PSocket;

                var buffer = new byte[PSendState.Stream.Length];
                PSendState.Stream.Seek(0, SeekOrigin.Begin);
                _ = PSendState.Stream.Read(buffer, 0, buffer.Length);

                webSocketClientNetSocket.Client.SendAsync(buffer);
            }
            else
            {
                const string errorMessage = "Serialized packet failure.";
                throw new InvalidOperationException(errorMessage);
            }

            return true;
        }

        private async UniTaskVoid ConnectAsync(object userData)
        {
            var cancellationTokenSource = m_CancellationTokenSource;
            try
            {
                PIsConnecting = true;
                var socketClient = (WebSocketNetSocket)PSocket;

                // 连接超时/取消由 CTS 控制：原实现是 async void，既无超时也无取消，失败还可能抛出。
                cancellationTokenSource.CancelAfter(ConnectTimeoutMilliseconds);

                await socketClient.ConnectAsync(cancellationTokenSource.Token);
                ConnectCallback(new ConnectState(PSocket, userData));
            }
            catch (OperationCanceledException)
            {
                PIsConnecting = false;
                PActive       = false;
                EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
                    errorCode: ENetworkErrorCode.ConnectError,
                    socketError: SocketError.TimedOut,
                    errorMessage: $"WebSocket connect canceled or timeout after {ConnectTimeoutMilliseconds}ms.");
            }
            catch (Exception exception)
            {
                PIsConnecting = false;
                var socketException = exception as SocketException;
                if (NetworkChannelError == null)
                {
                    // UniTaskVoid 中禁止抛出，否则异常无人接管。
                    FuLogger.LogError(exception.ToString());
                    return;
                }

                EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
                    errorCode: ENetworkErrorCode.ConnectError,
                    socketError: socketException?.SocketErrorCode ?? SocketError.Success,
                    errorMessage: exception.ToString());
            }
        }

        private void ConnectCallback(ConnectState connectState)
        {
            PIsConnecting = false;
            try
            {
                var socketUserData = (WebSocketNetSocket)PSocket;
                if (!socketUserData.IsConnected)
                    throw new SocketException((int)ENetworkErrorCode.ConnectError);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception exception)
            {
                PActive = false;
                if (NetworkChannelError == null) throw;
                var socketException = exception as SocketException;
                EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
                    errorCode: ENetworkErrorCode.ConnectError,
                    socketError: socketException?.SocketErrorCode ?? SocketError.Success,
                    errorMessage: exception.ToString());
                return;
            }

            PSentPacketCount     = 0;
            PReceivedPacketCount = 0;

            lock (PSendPacketPool) PSendPacketPool.Clear();
            lock (PHeartBeatLock) PHeartBeatState.Reset(true);
            EnqueueLifecycleEvent(EChannelLifecycleEventType.Connected, connectState.UserData);
            PActive = true;
        }

        private void ReceiveCallback(byte[] buffer)
        {
            try
            {
                lock (PHeartBeatLock)
                {
                    PHeartBeatState.Reset(PResetHeartBeatElapseSecondsWhenReceivePacket);
                }

                PReceivedPacketCount++;

                if (buffer.Length < PacketReceiveHeaderHandler.PacketHeaderLength) return;

                var processSuccess = PNetworkChannelHelper.DeserializePacketHeader(buffer);
                if (processSuccess)
                {
                    var bodyLength = ValidateAndGetPacketBodyLength(PacketReceiveHeaderHandler);
                    PReceiveState.Reset(bodyLength, PacketReceiveHeaderHandler);
                    if (buffer.Length < bodyLength) return;

                    var body = buffer.ReadBytes(PacketReceiveHeaderHandler.PacketHeaderLength, bodyLength);
                    if (PReceiveState.PacketHeader.ZipFlag != 0)
                    {
                        // 解压
                        MessageDecompressHandler.NotNull(nameof(MessageDecompressHandler));
                        body = MessageDecompressHandler.Handler(body);
                    }

                    // 反序列化数据
                    processSuccess = PNetworkChannelHelper.DeserializePacketBody(body, PacketReceiveHeaderHandler.Id, out var messageObject);
                    if (processSuccess)
                    {
                        messageObject.SetUpdateUniqueId(PacketReceiveHeaderHandler.UniqueId);
                    }

                    DebugReceiveLog(messageObject);
                    if (!processSuccess)
                    {
                        if (NetworkChannelError != null)
                        {
                            EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
                                errorCode: ENetworkErrorCode.DeserializePacketError,
                                errorMessage: "Packet body is invalid.");
                            return;
                        }
                    }

                    // 将收到的消息加入到链表最后（与主线程消费互斥）
                    lock (PExecutionMessageLock)
                    {
                        m_ExecutionMessageLinkedList.AddLast(messageObject);
                    }

                    PReceivedPacketCount++;
                }
                else
                {
                    EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
                        errorCode: ENetworkErrorCode.DeserializePacketHeaderError,
                        errorMessage: "Packet header is invalid.");
                }
            }
            catch (Exception e)
            {
                EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
                    errorCode: ENetworkErrorCode.DeserializePacketError,
                    errorMessage: "Packet body is invalid." + e.Message + "\n" + e.StackTrace);
            }
        }
    }
}
