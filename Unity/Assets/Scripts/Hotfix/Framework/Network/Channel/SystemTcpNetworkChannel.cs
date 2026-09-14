using System;
using System.Net.Sockets;
using AOT.Framework.Core.Log;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
    /// <summary>
    /// TCP 网络频道。
    /// 功能：
    ///     1. 实现TCP网络通讯功能。
    ///     2. 实现消息的发送和接收。
    /// </summary>
    internal sealed class SystemTcpNetworkChannel : NetworkChannelBase
    {
        private ConnectState m_ConnectState;
        private SystemNetSocket m_SystemNetSocket;

        /// <summary>
        /// 初始化网络频道的新实例。
        /// </summary>
        /// <param name="name">网络频道名称。</param>
        /// <param name="networkChannelHelper">网络频道辅助器。</param>
        /// <param name="rpcTimeout">RPC超时时间</param>
        public SystemTcpNetworkChannel(string name, INetworkChannelHelper networkChannelHelper, int rpcTimeout) : base(name, networkChannelHelper, rpcTimeout) { }

        /// <summary>
        /// 连接到远程主机。
        /// </summary>
        /// <param name="address">远程主机的地址。</param>
        /// <param name="userData">用户自定义数据。</param>
        public override void Connect(Uri address, object userData = null)
        {
            if (PIsConnecting) return;

            base.Connect(address, userData);
        }

        /// <summary>
        /// 目标地址解析完成（或确认解析失败）后创建 Socket 并发起连接。
        /// </summary>
        /// <param name="userData">用户自定义数据</param>
        protected override void OnConnectEndPointReady(object userData)
        {
            if (IsVerifyAddress)
            {
                m_SystemNetSocket = new SystemNetSocket(ConnectEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                PSocket = m_SystemNetSocket;
            }

            if (PSocket == null)
            {
                const string errorMessage = "Initialize network channel failure.";
                if (NetworkChannelError == null) throw new InvalidOperationException(errorMessage);
                NetworkChannelError(this, ENetworkErrorCode.SocketError, SocketError.Success, errorMessage);
                return;
            }

            PNetworkChannelHelper.PrepareForConnecting();

            ConnectAsync(userData);
        }

        protected override bool ProcessSend()
        {
            if (!base.ProcessSend()) return false;
            SendAsync();
            return true;

        }

        #region Receive

        private void ReceiveAsync()
        {
            try
            {
                var position = (int)PReceiveState.Stream.Position;
                var length = (int)(PReceiveState.Stream.Length - PReceiveState.Stream.Position);
                m_SystemNetSocket.BeginReceive(PReceiveState.Stream.GetBuffer(), position, length, SocketFlags.None, ReceiveCallback, m_SystemNetSocket);
            }
            catch (Exception exception)
            {
                PActive = false;
                if (NetworkChannelError == null) throw;
                var socketException = exception as SocketException;
                NetworkChannelError(this, ENetworkErrorCode.ReceiveError, socketException?.SocketErrorCode ?? SocketError.Success, exception.ToString());
            }
        }

        private void ReceiveCallback(IAsyncResult asyncResult)
        {
            var systemNetSocket = (SystemNetSocket)asyncResult.AsyncState;
            if (!systemNetSocket.IsConnected) return;

            int bytesReceived;
            try
            {
                bytesReceived = systemNetSocket.EndReceive(asyncResult);
            }
            catch (Exception exception)
            {
                PActive = false;
                if (NetworkChannelError == null) throw;
                var socketException = exception as SocketException;
                NetworkChannelError(this, ENetworkErrorCode.ReceiveError, socketException?.SocketErrorCode ?? SocketError.Success, exception.ToString());
                return;

            }

            if (bytesReceived <= 0)
            {
                Close();
                return;
            }

            lock (PHeartBeatLock)
            {
                PHeartBeatState.Reset(PResetHeartBeatElapseSecondsWhenReceivePacket);
            }

            try
            {
                ProcessReceivedBytes(bytesReceived);
            }
            catch (Exception exception)
            {
                // 回包处理（畸形包头、未注册 messageId 等）抛出的异常绝不能在**线程池线程**上逃逸：
                // 原实现只包住了 EndReceive，解析阶段的异常会直接抛出，且抛出前没有续接 ReceiveAsync，
                // 连接会静默卡死。这里统一转为 NetworkChannelError 事件。
                var socketException = exception as SocketException;
                NetworkChannelError?.Invoke(this, ENetworkErrorCode.DeserializePacketError, socketException?.SocketErrorCode ?? SocketError.Success, exception.ToString());
            }
            finally
            {
                // 无论成功、解析失败还是异常，都必须续接接收，避免连接静默卡死。
                if (PActive && PSocket != null)
                {
                    ReceiveAsync();
                }
            }
        }

        /// <summary>
        /// 处理本次收到的字节。
        /// </summary>
        /// <param name="bytesReceived">本次收到的字节数</param>
        private void ProcessReceivedBytes(int bytesReceived)
        {
            PReceiveState.Stream.Position += bytesReceived;
            if (PReceiveState.Stream.Position < PReceiveState.Stream.Length)
            {
                return;
            }

            PReceiveState.Stream.Position = 0L;
            bool processSuccess;
            if (PReceiveState.PacketHeader != null)
            {
                processSuccess = ProcessPackBody();
            }
            else
            {
                processSuccess = ProcessPackHeader();
                if (PReceiveState.IsEmptyBody)
                {
                    // 如果是空消息,直接返回
                    ProcessPackBody();
                    return;
                }
            }

            if (!processSuccess)
            {
                // 兼容原有语义：解析失败不抛异常，只记录日志（接收续接由调用方的 finally 保证）。
                FuLogger.LogWarning($"[{Name}] 数据包解析失败，已丢弃。");
            }
        }

        /// <summary>
        /// 解析消息头
        /// </summary>
        /// <returns></returns>
        private bool ProcessPackHeader()
        {
            var headerLength = PacketReceiveHeaderHandler.PacketHeaderLength;
            var buffer = new byte[headerLength];
            _ = PReceiveState.Stream.Read(buffer, 0, headerLength);
            var processSuccess = PNetworkChannelHelper.DeserializePacketHeader(buffer);
            if (!processSuccess)
            {
                // 包头解析失败：恢复为“等待包头”的状态，避免流状态与网络数据错位。
                PReceiveState.PrepareForPacketHeader();
                return false;
            }

            var bodyLength = ValidateAndGetPacketBodyLength(PacketReceiveHeaderHandler);
            PReceiveState.Reset(bodyLength, PacketReceiveHeaderHandler);
            return true;
        }

        /// <summary>
        /// 解析消息内容
        /// </summary>
        /// <returns></returns>
        private bool ProcessPackBody()
        {
            var bodyLength = ValidateAndGetPacketBodyLength(PReceiveState.PacketHeader);
            var buffer = new byte[bodyLength];
            _ = PReceiveState.Stream.Read(buffer, 0, bodyLength);

            if (PReceiveState.PacketHeader.ZipFlag != 0)
            {
                // 解压
                MessageDecompressHandler.NotNull(nameof(MessageDecompressHandler));
                buffer = MessageDecompressHandler.Handler(buffer);
            }

            var processSuccess = PNetworkChannelHelper.DeserializePacketBody(buffer, PacketReceiveHeaderHandler.Id, out var messageObject);
            if (processSuccess)
            {
                messageObject.SetUpdateUniqueId(PacketReceiveHeaderHandler.UniqueId);
            }

            DebugReceiveLog(messageObject);

            // 将收到的消息加入到链表最后（接收回调在线程池线程，需与主线程消费互斥）
            lock (PExecutionMessageLock)
            {
                m_ExecutionMessageLinkedList.AddLast(messageObject);
            }

            PReceivedPacketCount++;
            PReceiveState.PrepareForPacketHeader();
            return processSuccess;
        }

        #endregion

        #region Sender

        protected override bool ProcessSendMessage(MessageObject messageObject)
        {
            if (PActive == false)
            {
                PActive = false;
                NetworkChannelError?.Invoke(this, ENetworkErrorCode.SocketError, SocketError.Disconnecting, "Network channel is closing.");
                return false;
            }

            var serializeResult = base.ProcessSendMessage(messageObject);
            if (serializeResult) return true;
            const string errorMessage = "Serialized packet failure.";
            throw new InvalidOperationException(errorMessage);
        }


        /// <summary>
        /// 实际发送异步数据
        /// </summary>
        private void SendAsync()
        {
            try
            {
                m_SystemNetSocket.BeginSend(PSendState.Stream.GetBuffer(), (int)PSendState.Stream.Position,
                    (int)(PSendState.Stream.Length - PSendState.Stream.Position), SocketFlags.None, SendCallback, m_SystemNetSocket);
            }
            catch (Exception exception)
            {
                PActive = false;
                if (NetworkChannelError == null) throw;
                var socketException = exception as SocketException;
                NetworkChannelError(this, ENetworkErrorCode.SendError, socketException?.SocketErrorCode ?? SocketError.Success, exception.ToString());
            }
        }

        private void SendCallback(IAsyncResult asyncResult)
        {
            var systemNetSocket = (SystemNetSocket)asyncResult.AsyncState;
            if (!systemNetSocket.IsConnected) return;

            int bytesSent;
            try
            {
                bytesSent = systemNetSocket.EndSend(asyncResult, out _);
            }
            catch (Exception exception)
            {
                PActive = false;
                if (NetworkChannelError == null) return;
                var socketException = exception as SocketException;
                NetworkChannelError(this, ENetworkErrorCode.SendError, socketException?.SocketErrorCode ?? SocketError.Success, exception.ToString());
                return;
            }

            PSendState.Stream.Position += bytesSent;
            if (PSendState.Stream.Position < PSendState.Stream.Length)
            {
                SendAsync();
                return;
            }

            PSentPacketCount++;
            PSendState.Reset();
        }

        #endregion

        #region Connect

        private void ConnectAsync(object userData)
        {
            try
            {
                PIsConnecting = true;
                m_ConnectState = new ConnectState(m_SystemNetSocket, userData);
                ((SystemNetSocket)PSocket).BeginConnect(ConnectEndPoint.Address, ConnectEndPoint.Port, ConnectCallback, m_ConnectState);
            }
            catch (Exception exception)
            {
                if (NetworkChannelError == null) throw;
                var socketException = exception as SocketException;
                NetworkChannelError(this, ENetworkErrorCode.ConnectError, socketException?.SocketErrorCode ?? SocketError.Success, exception.ToString());
            }
        }

        private void ConnectCallback(IAsyncResult asyncResult)
        {
            PIsConnecting = false;
            var connectState = (ConnectState)asyncResult.AsyncState;
            var systemNetSocket = (SystemNetSocket)connectState.Socket;
            try
            {
                systemNetSocket.EndConnect(asyncResult);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception exception)
            {
                var socketException = exception as SocketException;
                NetworkChannelError?.Invoke(this, ENetworkErrorCode.ConnectError, socketException?.SocketErrorCode ?? SocketError.Success, exception.ToString());
                Close();
                return;
            }

            PSentPacketCount = 0;
            PReceivedPacketCount = 0;

            lock (PSendPacketPool) PSendPacketPool.Clear();
            lock (PHeartBeatLock) PHeartBeatState.Reset(true);

            NetworkChannelConnected?.Invoke(this, m_ConnectState.UserData);
            PActive = true;
            ReceiveAsync();
        }

        #endregion
    }
}
