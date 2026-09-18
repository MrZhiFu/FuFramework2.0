using System;
using System.Collections.Generic;
using System.IO;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;
using Hotfix.Framework.Event;


// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
    /// <summary>
    /// 默认网络频道帮助器。
    /// 功能：
    ///     1. 初始化网络频道帮助器。
    ///     2. 显式装配包和包处理函数（不再反射扫描程序集，见 <see cref="RegisterDefaultHandlers"/>）。
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
        /// 自定义包处理器注册委托集合（显式注册入口）。
        ///
        /// 说明（项目铁律 4：运行时杜绝反射）：
        ///     原实现通过 <c>UtilityAOT.Assembly.GetTypes()</c> 扫描全部已加载程序集 + 接口判断 +
        ///     <c>Activator.CreateInstance</c> 来发现并创建包处理器，现已改为
        ///     「显式装配框架自带的 7 个处理器」（见 <see cref="RegisterDefaultHandlers"/>）。
        ///     因此，**新增自定义处理器必须显式注册**，不再靠反射扫描自动发现：
        ///         1) 调用 <see cref="AddCustomHandlerRegistrar"/> 注册装配委托；或
        ///         2) 派生本类并重写 <see cref="RegisterCustomHandlers"/>。
        ///     处理器实例按频道创建（处理器内部各自持有解析状态，不能跨频道共享）。
        ///
        /// 仅限启动期调用，之后不再变更。
        /// </summary>
        private static readonly List<Action<INetworkChannel>> s_CustomHandlerRegistrars = new();

        private static readonly object s_CustomHandlerRegistrarsLock = new();

        /// <summary>
        /// 显式注册一个自定义包处理器装配委托。
        /// 委托会在每个频道 Initialize 时、框架默认处理器装配完成之后被调用。
        /// </summary>
        /// <param name="registrar">装配委托，参数为待装配的网络频道</param>
        public static void AddCustomHandlerRegistrar(Action<INetworkChannel> registrar)
        {
            registrar.NotNull(nameof(registrar));
            lock (s_CustomHandlerRegistrarsLock)
            {
                s_CustomHandlerRegistrars.Add(registrar);
            }
        }

        /// <summary>
        /// 装配框架自带的默认包处理器。
        ///
        /// 说明（项目铁律 4）：原实现由反射扫描全部已加载程序集发现 IPacketHandler 实现并逐个实例化，
        /// 现改为显式列出。下表即原扫描结果中的框架侧实现类（框架外的实现由生成物追加注册并覆盖，见下）：
        ///     接收包头 → DefaultPacketReceiveHeaderHandler
        ///     接收包体 → DefaultPacketReceiveBodyHandler
        ///     发送包头 → DefaultPacketSendHeaderHandler
        ///     发送包体 → DefaultPacketSendBodyHandler
        ///     心跳     → BasePacketHeartBeatHandler（兜底，可被框架外实现覆盖）
        ///     压缩     → DefaultMessageCompressHandler
        ///     解压     → DefaultMessageDecompressHandler
        ///
        /// 心跳处理器：此处装配的是框架基类 BasePacketHeartBeatHandler，仅作为**兜底默认**。
        ///     原实现靠反射扫描，会同时命中基类与该基类的游戏侧派生类
        ///     Hotfix.Game.Network.DefaultPacketHeartBeatHandler；由于 RegisterHeartBeatHandler 是
        ///     「后注册覆盖」，而编译产物中基类排在派生类之后，框架扫描的结果是**基类生效**
        ///     （基类 Handler() 仅抛 NotImplementedException）。
        ///     此前运行期之所以正常，是因为游戏侧 WinPlayerList.OnBtnLoginClick 在
        ///     CreateNetworkChannel（内部即触发本方法）之后又手动注册了一次该派生类。
        ///     现由生成物 Generated/ProtoMessageRegistry.g.cs 登记装配委托，在本方法之后追加注册
        ///     框架外的具体处理器（见 <see cref="AddCustomHandlerRegistrar"/>），
        ///     使框架侧自洽、不再依赖游戏侧的手动补注册；最终生效者仍是游戏侧实现（后注册者生效）。
        /// </summary>
        protected virtual void RegisterDefaultHandlers()
        {
            var channel = m_NetworkChannel;

            channel.RegisterHandler(new DefaultPacketReceiveHeaderHandler());
            channel.RegisterHandler(new DefaultPacketReceiveBodyHandler());
            channel.RegisterHandler(new DefaultPacketSendHeaderHandler());
            channel.RegisterHandler(new DefaultPacketSendBodyHandler());
            channel.RegisterHeartBeatHandler(new BasePacketHeartBeatHandler());
            channel.RegisterMessageCompressHandler(new DefaultMessageCompressHandler());
            channel.RegisterMessageDecompressHandler(new DefaultMessageDecompressHandler());
        }

        /// <summary>
        /// 自定义处理器装配钩子。派生类可重写以补充或替换包处理器。
        /// 在框架默认处理器装配之后调用。
        /// </summary>
        protected virtual void RegisterCustomHandlers()
        {
        }

        /// <summary>
        /// 初始化网络频道帮助器。
        /// </summary>
        /// <param name="netChannel"></param>
        public void Initialize(INetworkChannel netChannel)
        {
            m_NetworkChannel = netChannel;

            // 显式装配包处理器：框架默认 7 个 → 派生类钩子 → 显式注册的自定义委托。
            // 新增自定义处理器必须显式注册，不再依赖反射扫描程序集。
            RegisterDefaultHandlers();
            RegisterCustomHandlers();

            lock (s_CustomHandlerRegistrarsLock)
            {
                for (var i = 0; i < s_CustomHandlerRegistrars.Count; i++)
                {
                    s_CustomHandlerRegistrars[i]?.Invoke(m_NetworkChannel);
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
