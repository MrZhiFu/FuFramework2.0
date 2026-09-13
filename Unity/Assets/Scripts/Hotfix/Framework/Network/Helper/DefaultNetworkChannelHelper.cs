using System;
using System.Collections.Generic;
using System.IO;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Extension;
using AOT.Framework.Core.Utility;
using AOT.Framework.Core.Log;
using UtilityAOT = AOT.Framework.Core.Utility.UtilityAOT;
using Hotfix.Framework.Event;
using Utility = Hotfix.Framework.Core.Utility;


// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
    /// <summary>
    /// 默认网络频道帮助器。
    /// 功能：
    ///     1. 初始化网络频道帮助器。
    ///     2. 反射注册包和包处理函数。
    ///     3. 发送默认的包和包处理函数。
    /// </summary>
    public class DefaultNetworkChannelHelper : INetworkChannelHelper, IReference
    {
        /// <summary>
        /// 网络频道
        /// </summary>
        private INetworkChannel m_NetworkChannel;

        /// <summary>
        /// 获取事件组件。
        /// </summary>
        public static EventModule Event
        {
            get
            {
                if (m_Event == null) m_Event = ModuleManager.GetModule<EventModule>();
                return m_Event;
            }
        }

        private static EventModule m_Event;

        /// <summary>
        /// 包处理器类型缓存。
        /// 说明：原实现每次创建频道都会做一次全程序集扫描 + 逐个类型做接口判断，
        /// 这里把「类型发现」降级为进程内一次性执行并缓存结果；处理器的实例仍按频道创建
        /// （处理器内部各自持有解析状态，不能跨频道共享）。
        /// 受限于「不引入代码生成器」的约束，类型发现本身仍基于反射，属于铁律 4 的降级处理：
        /// 理想方案是由代码生成器产出静态映射表，彻底消除运行时反射。
        /// </summary>
        private static class PacketHandlerTypeCache
        {
            internal static List<Type> ReceiveHeaderTypes;
            internal static List<Type> ReceiveBodyTypes;
            internal static List<Type> SendHeaderTypes;
            internal static List<Type> SendBodyTypes;
            internal static List<Type> HeartBeatTypes;
            internal static List<Type> CompressTypes;
            internal static List<Type> DecompressTypes;
        }

        private static readonly object s_HandlerTypeCacheLock = new();
        private static          bool   s_HandlerTypeCacheReady;

        /// <summary>
        /// 一次性扫描程序集并缓存各类包处理器类型。
        /// </summary>
        private static void EnsureHandlerTypeCache()
        {
            if (s_HandlerTypeCacheReady) return;
            lock (s_HandlerTypeCacheLock)
            {
                if (s_HandlerTypeCacheReady) return;

                var receiveHeaderTypes = new List<Type>();
                var receiveBodyTypes   = new List<Type>();
                var sendHeaderTypes    = new List<Type>();
                var sendBodyTypes      = new List<Type>();
                var heartBeatTypes     = new List<Type>();
                var compressTypes      = new List<Type>();
                var decompressTypes    = new List<Type>();

                var packetReceiveHeaderHandlerBaseType = typeof(IPacketReceiveHeaderHandler);
                var packetReceiveBodyHandlerBaseType   = typeof(IPacketReceiveBodyHandler);
                var packetSendHeaderHandlerBaseType    = typeof(IPacketSendHeaderHandler);
                var packetSendBodyHandlerBaseType      = typeof(IPacketSendBodyHandler);
                var packetHeartBeatHandlerBaseType     = typeof(IPacketHeartBeatHandler);
                var messageCompressHandlerBaseType     = typeof(IMessageCompressHandler);
                var messageDecompressHandlerBaseType   = typeof(IMessageDecompressHandler);
                var packetHandlerBaseType              = typeof(IPacketHandler);

                var types = UtilityAOT.Assembly.GetTypes();
                foreach (var type in types)
                {
                    if (!type.IsClass || type.IsAbstract) continue;
                    if (!type.IsImplWithInterface(packetHandlerBaseType)) continue;

                    if (type.IsImplWithInterface(packetReceiveHeaderHandlerBaseType))
                    {
                        receiveHeaderTypes.Add(type);
                    }
                    else if (type.IsImplWithInterface(packetReceiveBodyHandlerBaseType))
                    {
                        receiveBodyTypes.Add(type);
                    }
                    else if (type.IsImplWithInterface(packetSendHeaderHandlerBaseType))
                    {
                        sendHeaderTypes.Add(type);
                    }
                    else if (type.IsImplWithInterface(packetSendBodyHandlerBaseType))
                    {
                        sendBodyTypes.Add(type);
                    }
                    else if (type.IsImplWithInterface(packetHeartBeatHandlerBaseType))
                    {
                        heartBeatTypes.Add(type);
                    }
                    else if (type.IsImplWithInterface(messageCompressHandlerBaseType))
                    {
                        compressTypes.Add(type);
                    }
                    else if (type.IsImplWithInterface(messageDecompressHandlerBaseType))
                    {
                        decompressTypes.Add(type);
                    }
                }

                PacketHandlerTypeCache.ReceiveHeaderTypes = receiveHeaderTypes;
                PacketHandlerTypeCache.ReceiveBodyTypes   = receiveBodyTypes;
                PacketHandlerTypeCache.SendHeaderTypes    = sendHeaderTypes;
                PacketHandlerTypeCache.SendBodyTypes      = sendBodyTypes;
                PacketHandlerTypeCache.HeartBeatTypes     = heartBeatTypes;
                PacketHandlerTypeCache.CompressTypes      = compressTypes;
                PacketHandlerTypeCache.DecompressTypes    = decompressTypes;

                s_HandlerTypeCacheReady = true;
            }
        }

        private void RegisterReceiveHeaderHandlers(List<Type> types)
        {
            for (var i = 0; i < types.Count; i++)
            {
                m_NetworkChannel.RegisterHandler((IPacketReceiveHeaderHandler)Activator.CreateInstance(types[i]));
            }
        }

        private void RegisterReceiveBodyHandlers(List<Type> types)
        {
            for (var i = 0; i < types.Count; i++)
            {
                m_NetworkChannel.RegisterHandler((IPacketReceiveBodyHandler)Activator.CreateInstance(types[i]));
            }
        }

        private void RegisterSendHeaderHandlers(List<Type> types)
        {
            for (var i = 0; i < types.Count; i++)
            {
                m_NetworkChannel.RegisterHandler((IPacketSendHeaderHandler)Activator.CreateInstance(types[i]));
            }
        }

        private void RegisterSendBodyHandlers(List<Type> types)
        {
            for (var i = 0; i < types.Count; i++)
            {
                m_NetworkChannel.RegisterHandler((IPacketSendBodyHandler)Activator.CreateInstance(types[i]));
            }
        }

        private void RegisterHeartBeatHandlers(List<Type> types)
        {
            for (var i = 0; i < types.Count; i++)
            {
                m_NetworkChannel.RegisterHeartBeatHandler((IPacketHeartBeatHandler)Activator.CreateInstance(types[i]));
            }
        }

        private void RegisterCompressHandlers(List<Type> types)
        {
            for (var i = 0; i < types.Count; i++)
            {
                m_NetworkChannel.RegisterMessageCompressHandler((IMessageCompressHandler)Activator.CreateInstance(types[i]));
            }
        }

        private void RegisterDecompressHandlers(List<Type> types)
        {
            for (var i = 0; i < types.Count; i++)
            {
                m_NetworkChannel.RegisterMessageDecompressHandler((IMessageDecompressHandler)Activator.CreateInstance(types[i]));
            }
        }

        /// <summary>
        /// 初始化网络频道帮助器。
        /// </summary>
        /// <param name="netChannel"></param>
        public void Initialize(INetworkChannel netChannel)
        {
            m_NetworkChannel = netChannel;

            // 注册包和包处理函数（类型发现只在首次执行，结果被缓存）。
            EnsureHandlerTypeCache();

            RegisterReceiveHeaderHandlers(PacketHandlerTypeCache.ReceiveHeaderTypes);
            RegisterReceiveBodyHandlers(PacketHandlerTypeCache.ReceiveBodyTypes);
            RegisterSendHeaderHandlers(PacketHandlerTypeCache.SendHeaderTypes);
            RegisterSendBodyHandlers(PacketHandlerTypeCache.SendBodyTypes);
            RegisterHeartBeatHandlers(PacketHandlerTypeCache.HeartBeatTypes);
            RegisterCompressHandlers(PacketHandlerTypeCache.CompressTypes);
            RegisterDecompressHandlers(PacketHandlerTypeCache.DecompressTypes);

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
            m_NetworkChannel.NotNull(nameof(m_NetworkChannel));
            m_NetworkChannel.PacketSendHeaderHandler.NotNull(nameof(m_NetworkChannel.PacketSendHeaderHandler));
            messageObject.NotNull(nameof(messageObject));
            destination.NotNull(nameof(destination));

            return m_NetworkChannel.PacketSendHeaderHandler.Handler(messageObject, m_NetworkChannel.MessageCompressHandler, destination,
                out messageBodyBuffer);
        }

        public bool SerializePacketBody(byte[] messageBodyBuffer, MemoryStream destination)
        {
            m_NetworkChannel.NotNull(nameof(m_NetworkChannel));
            m_NetworkChannel.PacketSendHeaderHandler.NotNull(nameof(m_NetworkChannel.PacketSendHeaderHandler));
            m_NetworkChannel.PacketSendBodyHandler.NotNull(nameof(m_NetworkChannel.PacketSendBodyHandler));
            messageBodyBuffer.NotNull(nameof(messageBodyBuffer));
            destination.NotNull(nameof(destination));

            return m_NetworkChannel.PacketSendBodyHandler.Handler(messageBodyBuffer, destination);
        }

        public bool DeserializePacketHeader(byte[] source)
        {
            source.NotNull(nameof(source));

            return m_NetworkChannel.PacketReceiveHeaderHandler.Handler(source);
        }

        public bool DeserializePacketBody(byte[] source, int messageId, out MessageObject messageObject)
        {
            source.NotNull(nameof(source));

            return m_NetworkChannel.PacketReceiveBodyHandler.Handler(source, messageId, out messageObject);
        }

        public void Clear()
        {
            // 仅重置字段：本类型始终由 new 直接构造、从不经引用池获取，Clear 不会被引用池调用。
            // 原实现在此调用 m_NetworkChannel?.Close()（带副作用的“清理”）与引用池 Clear 的纯重置约定不符，故移除；
            // 频道的关闭由 NetworkChannelBase.Shutdown 等频道自身生命周期负责。
            m_NetworkChannel = null;
        }

        private void OnNetworkConnectedEventArgs(object sender, GameEventArgs e)
        {
            if (e is not NetworkConnectedEventArgs ne || ne.NetworkChannel != m_NetworkChannel) return;
            FuLogger.LogInfo($"网络连接成功......{ne.NetworkChannel.Name}");
        }

        private void OnNetworkClosedEventArgs(object sender, GameEventArgs e)
        {
            if (e is not NetworkClosedEventArgs ne || ne.NetworkChannel != m_NetworkChannel) return;
            FuLogger.LogInfo($"网络连接关闭......{ne.NetworkChannel.Name}");
        }

        private void OnNetworkMissHeartBeatEventArgs(object sender, GameEventArgs e)
        {
            if (e is not NetworkMissHeartBeatEventArgs ne || ne.NetworkChannel != m_NetworkChannel) return;
            FuLogger.LogWarning($"Network channel '{ne.NetworkChannel.Name}' miss heart beat '{ ne.MissCount}' times.");
        }

        private void OnNetworkErrorEventArgs(object sender, GameEventArgs e)
        {
            if (e is not NetworkErrorEventArgs ne || ne.NetworkChannel != m_NetworkChannel) return;
            FuLogger.LogError($"Network channel '{ne.NetworkChannel.Name}' error, error code is '{ne.ErrorCode}', error message is '{ne.ErrorMessage}'.");
            ne.NetworkChannel.Close();
        }
    }
}
