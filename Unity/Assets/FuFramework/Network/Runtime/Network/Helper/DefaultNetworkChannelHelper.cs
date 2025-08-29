using System;
using System.IO;
using FuFramework.Core.Runtime;
using FuFramework.Event.Runtime;
using Utility = FuFramework.Core.Runtime.Utility;


// ReSharper disable once CheckNamespace
namespace FuFramework.Network.Runtime
{
    /// <summary>
    /// 默认网络频道帮助器
    /// </summary>
    public class DefaultNetworkChannelHelper : INetworkChannelHelper, IReference
    {
        private INetworkChannel m_NetworkChannel;

        /// <summary>
        /// 获取事件组件。
        /// </summary>
        public static EventComponent Event
        {
            get
            {
                if (!_event) _event = ModuleManager.GetModule<EventComponent>();
                return _event;
            }
        }

        private static EventComponent _event;

        /// <summary>
        /// 初始化网络频道帮助器。
        /// </summary>
        /// <param name="netChannel"></param>
        public void Initialize(INetworkChannel netChannel)
        {
            m_NetworkChannel = netChannel;
            
            // 反射注册包和包处理函数。
            var packetReceiveHeaderHandlerBaseType = typeof(IPacketReceiveHeaderHandler);
            var packetReceiveBodyHandlerBaseType = typeof(IPacketReceiveBodyHandler);
            var packetSendHeaderHandlerBaseType = typeof(IPacketSendHeaderHandler);
            var packetSendBodyHandlerBaseType = typeof(IPacketSendBodyHandler);
            var packetHeartBeatHandlerBaseType = typeof(IPacketHeartBeatHandler);
            var messageCompressHandlerBaseType = typeof(IMessageCompressHandler);
            var messageDecompressHandlerBaseType = typeof(IMessageDecompressHandler);
            var packetHandlerBaseType = typeof(IPacketHandler);

            var types = Utility.Assembly.GetTypes();
            foreach (var type in types)
            {
                if (!type.IsClass || type.IsAbstract) continue;
                if (!type.IsImplWithInterface(packetHandlerBaseType)) continue;

                if (type.IsImplWithInterface(packetReceiveHeaderHandlerBaseType))
                {
                    var handler = Activator.CreateInstance(type) as IPacketReceiveHeaderHandler;
                    m_NetworkChannel.RegisterHandler(handler);
                }
                else if (type.IsImplWithInterface(packetReceiveBodyHandlerBaseType))
                {
                    var handler = Activator.CreateInstance(type) as IPacketReceiveBodyHandler;
                    m_NetworkChannel.RegisterHandler(handler);
                }
                else if (type.IsImplWithInterface(packetSendHeaderHandlerBaseType))
                {
                    var handler = Activator.CreateInstance(type) as IPacketSendHeaderHandler;
                    m_NetworkChannel.RegisterHandler(handler);
                }
                else if (type.IsImplWithInterface(packetSendBodyHandlerBaseType))
                {
                    var handler = Activator.CreateInstance(type) as IPacketSendBodyHandler;
                    m_NetworkChannel.RegisterHandler(handler);
                }
                else if (type.IsImplWithInterface(packetHeartBeatHandlerBaseType))
                {
                    var handler = Activator.CreateInstance(type) as IPacketHeartBeatHandler;
                    m_NetworkChannel.RegisterHeartBeatHandler(handler);
                }
                else if (type.IsImplWithInterface(messageCompressHandlerBaseType))
                {
                    var handler = Activator.CreateInstance(type) as IMessageCompressHandler;
                    m_NetworkChannel.RegisterMessageCompressHandler(handler);
                }
                else if (type.IsImplWithInterface(messageDecompressHandlerBaseType))
                {
                    var handler = Activator.CreateInstance(type) as IMessageDecompressHandler;
                    m_NetworkChannel.RegisterMessageDecompressHandler(handler);
                }
            }

            Event.Subscribe(NetworkConnectedEventArgs.EventId, OnNetworkConnectedEventArgs);
            Event.Subscribe(NetworkClosedEventArgs.EventId, OnNetworkClosedEventArgs);
            Event.Subscribe(NetworkMissHeartBeatEventArgs.EventId, OnNetworkMissHeartBeatEventArgs);
            Event.Subscribe(NetworkErrorEventArgs.EventId, OnNetworkErrorEventArgs);
        }

        public void Shutdown()
        {
            Event.Unsubscribe(NetworkConnectedEventArgs.EventId, OnNetworkConnectedEventArgs);
            Event.Unsubscribe(NetworkClosedEventArgs.EventId, OnNetworkClosedEventArgs);
            Event.Unsubscribe(NetworkMissHeartBeatEventArgs.EventId, OnNetworkMissHeartBeatEventArgs);
            Event.Unsubscribe(NetworkErrorEventArgs.EventId, OnNetworkErrorEventArgs);
            m_NetworkChannel = null;
        }

        public void PrepareForConnecting()
        {
            m_NetworkChannel.Socket.ReceiveBufferSize = 1024 * 64 - 1;
            m_NetworkChannel.Socket.SendBufferSize = 1024 * 64 - 1;
        }

        public bool SendHeartBeat()
        {
            var message = m_NetworkChannel.PacketHeartBeatHandler.Handler();
            m_NetworkChannel.Send(message);
            return true;
        }

        public bool SerializePacketHeader<T>(T messageObject, MemoryStream destination, out byte[] messageBodyBuffer) where T : MessageObject
        {
            FuGuard.NotNull(m_NetworkChannel, nameof(m_NetworkChannel));
            FuGuard.NotNull(m_NetworkChannel.PacketSendHeaderHandler, nameof(m_NetworkChannel.PacketSendHeaderHandler));
            FuGuard.NotNull(messageObject, nameof(messageObject));
            FuGuard.NotNull(destination, nameof(destination));

            return m_NetworkChannel.PacketSendHeaderHandler.Handler(messageObject, m_NetworkChannel.MessageCompressHandler, destination,
                out messageBodyBuffer);
        }

        public bool SerializePacketBody(byte[] messageBodyBuffer, MemoryStream destination)
        {
            FuGuard.NotNull(m_NetworkChannel, nameof(m_NetworkChannel));
            FuGuard.NotNull(m_NetworkChannel.PacketSendHeaderHandler, nameof(m_NetworkChannel.PacketSendHeaderHandler));
            FuGuard.NotNull(m_NetworkChannel.PacketSendBodyHandler, nameof(m_NetworkChannel.PacketSendBodyHandler));
            FuGuard.NotNull(messageBodyBuffer, nameof(messageBodyBuffer));
            FuGuard.NotNull(destination, nameof(destination));

            return m_NetworkChannel.PacketSendBodyHandler.Handler(messageBodyBuffer, destination);
        }

        public bool DeserializePacketHeader(byte[] source)
        {
            FuGuard.NotNull(source, nameof(source));

            return m_NetworkChannel.PacketReceiveHeaderHandler.Handler(source);
        }

        public bool DeserializePacketBody(byte[] source, int messageId, out MessageObject messageObject)
        {
            FuGuard.NotNull(source, nameof(source));

            return m_NetworkChannel.PacketReceiveBodyHandler.Handler(source, messageId, out messageObject);
        }

        public void Clear()
        {
            m_NetworkChannel?.Close();
            m_NetworkChannel = null;
        }

        private void OnNetworkConnectedEventArgs(object sender, GameEventArgs e)
        {
            if (e is not NetworkConnectedEventArgs ne || ne.NetworkChannel != m_NetworkChannel) return;
            Log.Debug($"网络连接成功......{ne.NetworkChannel.Name}");
        }

        private void OnNetworkClosedEventArgs(object sender, GameEventArgs e)
        {
            if (e is not NetworkClosedEventArgs ne || ne.NetworkChannel != m_NetworkChannel) return;
            Log.Debug($"网络连接关闭......{ne.NetworkChannel.Name}");
        }

        private void OnNetworkMissHeartBeatEventArgs(object sender, GameEventArgs e)
        {
            if (e is not NetworkMissHeartBeatEventArgs ne || ne.NetworkChannel != m_NetworkChannel) return;
            Log.Warning(Utility.Text.Format("Network channel '{0}' miss heart beat '{1}' times.", ne.NetworkChannel.Name, ne.MissCount));
        }

        private void OnNetworkErrorEventArgs(object sender, GameEventArgs e)
        {
            if (e is not NetworkErrorEventArgs ne || ne.NetworkChannel != m_NetworkChannel) return;
            Log.Error(Utility.Text.Format("Network channel '{0}' error, error code is '{1}', error message is '{2}'.", ne.NetworkChannel.Name,
                ne.ErrorCode, ne.ErrorMessage));
            ne.NetworkChannel.Close();
        }
    }
}