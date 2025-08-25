using System;
using System.Net.Sockets;
using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Network.Runtime
{
    public sealed partial class NetworkManager
    {
        /// <summary>
        /// TCP 网络频道。
        /// 功能：
        /// 1. 实现TCP网络通讯功能。
        /// 2. 实现消息的发送和接收。
        /// </summary>
        private sealed class SystemTcpNetworkChannel : NetworkChannelBase
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
                if (IsVerifyAddress)
                {
                    m_SystemNetSocket = new SystemNetSocket(ConnectEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    PSocket = m_SystemNetSocket;
                }

                if (PSocket == null)
                {
                    const string errorMessage = "Initialize network channel failure.";
                    if (NetworkChannelError == null) throw new FuException(errorMessage);
                    NetworkChannelError(this, NetworkErrorCode.SocketError, SocketError.Success, errorMessage);
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
                    NetworkChannelError(this, NetworkErrorCode.ReceiveError, socketException?.SocketErrorCode ?? SocketError.Success, exception.ToString());
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
                    NetworkChannelError(this, NetworkErrorCode.ReceiveError, socketException?.SocketErrorCode ?? SocketError.Success, exception.ToString());
                    return;

                }

                if (bytesReceived <= 0)
                {
                    Close();
                    return;
                }

                lock (PHeartBeatState)
                {
                    PHeartBeatState.Reset(PResetHeartBeatElapseSecondsWhenReceivePacket);
                }

                PReceiveState.Stream.Position += bytesReceived;
                if (PReceiveState.Stream.Position < PReceiveState.Stream.Length)
                {
                    ReceiveAsync();
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
                        ReceiveAsync();
                        return;
                    }
                }

                if (processSuccess) 
                    ReceiveAsync();
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
                var bodyLength = (int)(PacketReceiveHeaderHandler.PacketLength - PacketReceiveHeaderHandler.PacketHeaderLength);
                PReceiveState.Reset(bodyLength, PacketReceiveHeaderHandler);
                return processSuccess;
            }

            /// <summary>
            /// 解析消息内容
            /// </summary>
            /// <returns></returns>
            private bool ProcessPackBody()
            {
                var bodyLength = (int)(PReceiveState.PacketHeader.PacketLength - PReceiveState.PacketHeader.PacketHeaderLength);
                var buffer = new byte[bodyLength];
                _ = PReceiveState.Stream.Read(buffer, 0, bodyLength);

                if (PReceiveState.PacketHeader.ZipFlag != 0)
                {
                    // 解压
                    FuGuard.NotNull(MessageDecompressHandler, nameof(MessageDecompressHandler));
                    buffer = MessageDecompressHandler.Handler(buffer);
                }

                var processSuccess = PNetworkChannelHelper.DeserializePacketBody(buffer, PacketReceiveHeaderHandler.Id, out var messageObject);
                if (processSuccess)
                {
                    messageObject.SetUpdateUniqueId(PacketReceiveHeaderHandler.UniqueId);
                }

                DebugReceiveLog(messageObject);
                
                // 将收到的消息加入到链表最后
                m_ExecutionMessageLinkedList.AddLast(messageObject);

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
                    NetworkChannelError?.Invoke(this, NetworkErrorCode.SocketError, SocketError.Disconnecting, "Network channel is closing.");
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
                    NetworkChannelError(this, NetworkErrorCode.SendError, socketException?.SocketErrorCode ?? SocketError.Success, exception.ToString());
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
                    NetworkChannelError(this, NetworkErrorCode.SendError, socketException?.SocketErrorCode ?? SocketError.Success, exception.ToString());
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
                    NetworkChannelError(this, NetworkErrorCode.ConnectError, socketException?.SocketErrorCode ?? SocketError.Success, exception.ToString());
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
                    NetworkChannelError?.Invoke(this, NetworkErrorCode.ConnectError, socketException?.SocketErrorCode ?? SocketError.Success, exception.ToString());
                    Close();
                    return;
                }

                PSentPacketCount = 0;
                PReceivedPacketCount = 0;

                lock (PSendPacketPool) PSendPacketPool.Clear();
                lock (PHeartBeatState) PHeartBeatState.Reset(true);

                NetworkChannelConnected?.Invoke(this, m_ConnectState.UserData);
                PActive = true;
                ReceiveAsync();
            }

            #endregion
        }
    }
}