using System;
using System.Net;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Utility;
using AOT.Framework.Core.Log;
using UtilityAOT = AOT.Framework.Core.Utility.UtilityAOT;
using System.Collections.Generic;
using Utility = Hotfix.Framework.Core.Utility;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
    /// <summary>
    /// 网络频道基类。
    /// </summary>
    internal abstract class NetworkChannelBase : INetworkChannel, IDisposable
    {
        /// <summary>
        /// 默认心跳间隔
        /// </summary>
        private const float DefaultHeartBeatInterval = 30f;

        /// <summary>
        /// 默认心跳丢失断开次数
        /// </summary>
        private const int DefaultMissHeartBeatCountByClose = 10;

        protected readonly FuLinkedList<MessageObject> PSendPacketPool;
        protected readonly INetworkChannelHelper       PNetworkChannelHelper;
        protected          EAddressFamily              PEAddressFamily;

        /// <summary>
        /// 当收到数据包时是否重置心跳流逝时长
        /// </summary>
        protected bool PResetHeartBeatElapseSecondsWhenReceivePacket;

        /// <summary>
        /// 心跳间隔
        /// </summary>
        protected float PHeartBeatInterval;

        /// <summary>
        /// 心跳丢失次数
        /// </summary>
        protected int MissHeartBeatCountByClose;

        /// <summary>
        /// 发送消息ID忽略列表
        /// </summary>
        protected List<int> IgnoreSendIds = new();

        /// <summary>
        /// 接收消息ID忽略列表
        /// </summary>
        protected List<int> IgnoreReceiveIds = new();

        /// <summary>
        /// 网络Socket 对象
        /// </summary>
        protected INetworkSocket PSocket;

        protected readonly SendState      PSendState;
        protected readonly ReceiveState   PReceiveState;
        protected readonly HeartBeatState PHeartBeatState;
        protected readonly RpcState       PRpcState;

        /// <summary>
        /// 是否验证地址
        /// </summary>
        protected bool IsVerifyAddress = true;

        /// <summary>
        /// 是否需要由本类解析主机名。默认需要；子类若自行处理主机名（如 WebSocket）可覆写为 false。
        /// </summary>
        protected virtual bool NeedResolveHost => true;

        /// <summary>
        /// 链接目标地址
        /// </summary>
        protected IPEndPoint ConnectEndPoint;

        /// <summary>
        /// 发送数据包的数量
        /// </summary>
        protected int PSentPacketCount;

        /// <summary>
        /// 接收数据包数量
        /// </summary>
        protected int PReceivedPacketCount;

        /// <summary>
        /// 是否正在连接中
        /// </summary>
        protected bool PIsConnecting = false;

        private bool m_Disposed;
        private bool m_PActive;

        /// <summary>
        /// 网络是否激活
        /// </summary>
        protected bool PActive
        {
            get => m_PActive;
            set
            {
                if (m_PActive == value) return;
                m_PActive = value;
            }
        }

        private IPacketSendHeaderHandler    m_PacketSendHeaderHandler;
        private IPacketSendBodyHandler      m_PacketSendBodyHandler;
        private IPacketReceiveHeaderHandler m_PacketReceiveHeaderHandler;
        private IPacketReceiveBodyHandler   m_PacketReceiveBodyHandler;
        private IPacketHeartBeatHandler     m_PacketHeartBeatHandler;

        /// <summary>
        /// 心跳状态的专用锁对象。
        /// 说明：原先直接 lock(PHeartBeatState) 作为锁使用，而 Close() 在 lock(this) 内再次获取该锁、
        /// ProcessHeartBeat 又在持有该锁时调用 Close()，两条路径锁序相反会形成 ABBA 死锁面，
        /// 因此统一改为专用锁对象，并保证不在持有该锁时再获取关闭锁。
        /// </summary>
        protected readonly object PHeartBeatLock = new();

        /// <summary>
        /// 待执行消息链表的专用锁对象。
        /// 说明：接收回调运行在线程池线程（AddLast），而主线程会进行 First/RemoveFirst/Clear，
        /// FuLinkedList 并非线程安全，必须加锁互斥。
        /// </summary>
        protected readonly object PExecutionMessageLock = new();

        /// <summary>
        /// 关闭流程的专用锁对象（替代原先的 lock(this)，避免与外部对频道实例的加锁产生交叉锁序）。
        /// </summary>
        private readonly object m_CloseLock = new();

        protected readonly FuLinkedList<MessageObject> m_ExecutionMessageLinkedList = new();

        /// <summary>
        /// 消息派发时复用的处理器列表，避免每收一包都新建 List 造成 GC。
        /// </summary>
        private readonly List<MessageHandlerAttribute> m_HandlerBuffer = new(8);

        /// <summary>
        /// 复用列表是否正在使用中（用于处理派发重入，重入时回退为新建列表）。
        /// </summary>
        private bool m_HandlerBufferBusy;

        /// <summary>
        /// 频道生命周期事件类型。
        /// 仅含跨线程触发的三类；MissHeartBeat 由主线程 Update 的心跳检查触发，无需封送。
        /// protected 而非 private：作为 protected 方法 EnqueueLifecycleEvent 的参数类型，
        /// private 会报 CS0051，且子类触发点需要引用本类型。
        /// </summary>
        protected enum EChannelLifecycleEventType
        {
            Connected,
            Closed,
            Error
        }

        /// <summary>
        /// 待主线程派发的生命周期事件记录。
        /// 生命周期事件频率极低（每连接至多 Connected/Closed 各一次、Error 罕见），直接 new，不入池。
        /// </summary>
        private sealed class LifecycleEvent
        {
            public EChannelLifecycleEventType Type;
            public object            UserData;
            public ENetworkErrorCode ErrorCode;
            public SocketError       SocketError;
            public string            ErrorMessage;
        }

        /// <summary>
        /// 生命周期事件队列：Socket 回调线程入队，主线程 Update 排水后触发。
        /// 互斥复用 PExecutionMessageLock（与数据包接收队列同族：接收线程生产、主线程消费）。
        /// 排水位置在 Update 活跃检查之前——保证 Close() 后入队的 Closed 事件必达。
        /// 频道销毁后 Update 不再被调，未派发事件随本对象一并回收（丢弃，无泄漏）。
        /// </summary>
        private readonly Queue<LifecycleEvent> m_LifecycleEventQueue = new();

        /// <summary>
        /// 将生命周期事件入队，由主线程 Update 排水时触发（封送，替代跨线程直接 Invoke）。
        /// 调用方保留「订阅者为 null 时抛异常」的既有行为，本方法不做判空。
        /// </summary>
        protected void EnqueueLifecycleEvent(EChannelLifecycleEventType type, object userData = null,
                                             ENetworkErrorCode errorCode = ENetworkErrorCode.SocketError,
                                             SocketError socketError = SocketError.Success,
                                             string errorMessage = null)
        {
            var lifecycleEvent = new LifecycleEvent
            {
                Type         = type,
                UserData     = userData,
                ErrorCode    = errorCode,
                SocketError  = socketError,
                ErrorMessage = errorMessage,
            };

            lock (PExecutionMessageLock)
            {
                m_LifecycleEventQueue.Enqueue(lifecycleEvent);
            }
        }

        /// <summary>
        /// 排水生命周期事件队列：锁内逐条出队、锁外触发（写法同 ProcessReceivedMessage 的出队-派发分离，
        /// handler 内同步 Close 频道只会继续入队，不会破坏本循环）。
        /// 触发的是现有公共委托字段，NetworkModule 等订阅方零改动。
        /// </summary>
        private void DrainLifecycleEvents()
        {
            while (true)
            {
                LifecycleEvent lifecycleEvent;
                lock (PExecutionMessageLock)
                {
                    if (m_LifecycleEventQueue.Count == 0) break;
                    lifecycleEvent = m_LifecycleEventQueue.Dequeue();
                }

                switch (lifecycleEvent.Type)
                {
                    case EChannelLifecycleEventType.Connected:
                        NetworkChannelConnected?.Invoke(this, lifecycleEvent.UserData);
                        break;
                    case EChannelLifecycleEventType.Closed:
                        NetworkChannelClosed?.Invoke(this);
                        break;
                    case EChannelLifecycleEventType.Error:
                        NetworkChannelError?.Invoke(this, lifecycleEvent.ErrorCode, lifecycleEvent.SocketError, lifecycleEvent.ErrorMessage);
                        break;
                }
            }
        }

        public Action<NetworkChannelBase, object>                                NetworkChannelConnected;
        public Action<NetworkChannelBase>                                        NetworkChannelClosed;
        public Action<NetworkChannelBase, int>                                   NetworkChannelMissHeartBeat;
        public Action<NetworkChannelBase, ENetworkErrorCode, SocketError, string> NetworkChannelError;

        /// <summary>
        /// 初始化网络频道基类的新实例。
        /// </summary>
        /// <param name="name">网络频道名称。</param>
        /// <param name="networkChannelHelper">网络频道辅助器。</param>
        /// <param name="rpcTimeout">RPC超时时间</param>
        public NetworkChannelBase(string name, INetworkChannelHelper networkChannelHelper, int rpcTimeout)
        {
            Name                                          = name ?? string.Empty;
            PSendPacketPool                               = new FuLinkedList<MessageObject>();
            PNetworkChannelHelper                         = networkChannelHelper;
            PEAddressFamily                               = EAddressFamily.Unknown;
            PResetHeartBeatElapseSecondsWhenReceivePacket = false;
            PHeartBeatInterval                            = DefaultHeartBeatInterval;
            MissHeartBeatCountByClose                     = DefaultMissHeartBeatCountByClose;
            PSocket                                       = null;
            PSendState                                    = new SendState();
            PReceiveState                                 = new ReceiveState();
            PHeartBeatState                               = new HeartBeatState();
            PRpcState                                     = new RpcState(rpcTimeout);
            PSentPacketCount                              = 0;
            PReceivedPacketCount                          = 0;
            PActive                                       = false;
            PIsConnecting                                 = false;
            m_Disposed                                    = false;
            NetworkChannelConnected                       = null;
            NetworkChannelClosed                          = null;
            NetworkChannelMissHeartBeat                   = null;
            NetworkChannelError                           = null;

            networkChannelHelper.Initialize(this);
        }

        #region 属性

        /// <summary>
        /// 获取网络频道名称。
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 获取网络频道所使用的 Socket。
        /// </summary>
        public INetworkSocket Socket => PSocket;

        /// <summary>
        /// 获取是否已连接。
        /// </summary>
        public bool Connected => PSocket is { IsConnected: true };

        /// <summary>
        /// 获取网络地址类型。
        /// </summary>
        public EAddressFamily EAddressFamily => PEAddressFamily;

        /// <summary>
        /// 获取要发送的消息包数量。
        /// </summary>
        public int SendPacketCount
        {
            get
            {
                lock (PSendPacketPool)
                    return PSendPacketPool.Count;
            }
        }

        /// <summary>
        /// 获取累计发送的消息包数量。
        /// </summary>
        public int SentPacketCount => PSentPacketCount;

        /// <summary>
        /// 获取累计已接收的消息包数量。
        /// </summary>
        public int ReceivedPacketCount => PReceivedPacketCount;

        /// <summary>
        /// 获取或设置当收到消息包时是否重置心跳流逝时间。
        /// </summary>
        public bool ResetHeartBeatElapseSecondsWhenReceivePacket
        {
            get => PResetHeartBeatElapseSecondsWhenReceivePacket;
            set => PResetHeartBeatElapseSecondsWhenReceivePacket = value;
        }

        /// <summary>
        /// 获取丢失心跳的次数。
        /// </summary>
        public int MissHeartBeatCount
        {
            get
            {
                lock (PHeartBeatLock)
                    return PHeartBeatState.MissHeartBeatCount;
            }
        }

        /// <summary>
        /// 获取或设置心跳间隔时长，以秒为单位。
        /// </summary>
        public float HeartBeatInterval
        {
            get => PHeartBeatInterval;
            set => PHeartBeatInterval = value;
        }

        /// <summary>
        /// 获取心跳等待时长，以秒为单位。
        /// </summary>
        public float HeartBeatElapseSeconds
        {
            get
            {
                lock (PHeartBeatLock)
                {
                    return PHeartBeatState.HeartBeatElapseSeconds;
                }
            }
        }

        /// <summary>
        /// 消息发送包头处理器
        /// </summary>
        public IPacketSendHeaderHandler PacketSendHeaderHandler => m_PacketSendHeaderHandler;

        /// <summary>
        /// 消息发送内容处理器
        /// </summary>
        public IPacketSendBodyHandler PacketSendBodyHandler => m_PacketSendBodyHandler;

        /// <summary>
        /// 消息接收包头处理器
        /// </summary>
        public IPacketReceiveHeaderHandler PacketReceiveHeaderHandler => m_PacketReceiveHeaderHandler;

        /// <summary>
        /// 心跳消息处理器
        /// </summary>
        public IPacketHeartBeatHandler PacketHeartBeatHandler => m_PacketHeartBeatHandler;

        /// <summary>
        /// 消息接收内容处理器
        /// </summary>
        public IPacketReceiveBodyHandler PacketReceiveBodyHandler => m_PacketReceiveBodyHandler;

        /// <summary>
        /// 消息压缩处理器
        /// </summary>
        public IMessageCompressHandler MessageCompressHandler { get; private set; }

        /// <summary>
        /// 消息解压处理器
        /// </summary>
        public IMessageDecompressHandler MessageDecompressHandler { get; private set; }

        #endregion

        /// <summary>
        /// 网络频道轮询。
        /// </summary>
        /// <param name="deltaTime">帧间隔时间。</param>
        /// <param name="unscaledDeltaTime">无缩放的帧间隔时间。</param>
        public virtual void Update(float deltaTime, float unscaledDeltaTime)
        {
            // 先排水生命周期事件，再做活跃检查：频道 Close 后 PActive=false，若排水在检查之后，
            // 已入队的 Closed 事件将永远滞留队列（Update 不再被有效执行）。
            DrainLifecycleEvents();

            if (PSocket == null || !PActive) return;

            ProcessSend();
            ProcessReceive();

            if (PSocket == null || !PActive) return;

            ProcessHeartBeat(unscaledDeltaTime);
            ProcessReceivedMessage();
            PRpcState.Update(deltaTime, unscaledDeltaTime);
        }

        /// <summary>
        /// 处理接收到的消息
        /// </summary>
        private void ProcessReceivedMessage()
        {
            while (true)
            {
                MessageObject messageObject;
                // 接收回调运行在线程池线程（AddLast），此处与其互斥出队；
                // 同时在锁内校验 First 是否为空，避免链表被清空后 RemoveFirst 抛异常冲出 ModuleManager.Update。
                lock (PExecutionMessageLock)
                {
                    var first = m_ExecutionMessageLinkedList.First;
                    if (first == null) break;

                    messageObject = first.Value;
                    m_ExecutionMessageLinkedList.RemoveFirst();
                }

                try
                {
                    // 执行RPC匹配
                    if (PRpcState.TryReply(messageObject)) continue;

                    // 执行通知消息（锁外派发，避免长时间持锁阻塞接收线程）
                    DispatchMessage(messageObject);
                }
                catch (Exception e)
                {
                    FuLogger.LogFatal(e);
                }
            }
        }

        /// <summary>
        /// 将消息派发给已注册的消息处理器
        /// </summary>
        /// <param name="messageObject">消息对象</param>
        private void DispatchMessage(MessageObject messageObject)
        {
            // 处理器列表会被复制到复用缓冲区后再派发：派发期间用户代码可能注册/注销处理器，
            // 直接遍历内部列表会抛 InvalidOperationException；复用缓冲区避免每包分配 List。
            var reuseBuffer = !m_HandlerBufferBusy;
            List<MessageHandlerAttribute> handlers;
            if (reuseBuffer)
            {
                m_HandlerBufferBusy = true;
                handlers            = m_HandlerBuffer;
                ProtoMessageHandler.GetHandlers(messageObject.GetType(), handlers);
            }
            else
            {
                // 重入时不能复用缓冲区，退化为新建列表。
                handlers = new List<MessageHandlerAttribute>(8);
                ProtoMessageHandler.GetHandlers(messageObject.GetType(), handlers);
            }

            try
            {
                for (var i = 0; i < handlers.Count; i++)
                {
                    var handler = handlers[i];
                    handler.SetMessageObject(messageObject);
                    try
                    {
                        handler.Invoke();
                    }
                    catch (Exception e)
                    {
                        FuLogger.LogFatal(e);
                    }
                }
            }
            finally
            {
                if (reuseBuffer)
                {
                    handlers.Clear();
                    m_HandlerBufferBusy = false;
                }
            }
        }

        /// <summary>
        /// 处理心跳
        /// </summary>
        /// <param name="unscaledDeltaTime"></param>
        private void ProcessHeartBeat(float unscaledDeltaTime)
        {
            if (PHeartBeatInterval <= 0f) return;

            var sendHeartBeat      = false;
            var missHeartBeatCount = 0;
            lock (PHeartBeatLock)
            {
                if (PSocket == null || !PActive) return;

                PHeartBeatState.HeartBeatElapseSeconds += unscaledDeltaTime;
                if (PHeartBeatState.HeartBeatElapseSeconds >= PHeartBeatInterval)
                {
                    sendHeartBeat                          = true;
                    missHeartBeatCount                     = PHeartBeatState.MissHeartBeatCount;
                    PHeartBeatState.HeartBeatElapseSeconds = 0f;
                    PHeartBeatState.MissHeartBeatCount++;
                }
            }

            // 以下操作必须在心跳锁之外执行：SendHeartBeat 会获取发送包池锁，
            // Close 会获取关闭锁，若仍在心跳锁内执行会与 Close 内的加锁形成 ABBA 死锁。
            if (sendHeartBeat && PNetworkChannelHelper.SendHeartBeat())
            {
                if (missHeartBeatCount > 0 && NetworkChannelMissHeartBeat != null)
                {
                    NetworkChannelMissHeartBeat(this, missHeartBeatCount);
                }

                // PHeartBeatState.Reset(this.ResetHeartBeatElapseSecondsWhenReceivePacket);
                return;
            }

            bool shouldClose;
            lock (PHeartBeatLock)
            {
                shouldClose = PHeartBeatState.MissHeartBeatCount > MissHeartBeatCountByClose;
            }

            if (shouldClose)
            {
                // 心跳丢失达到上限。触发断开
                Close();
            }
        }

        /// <summary>
        /// 关闭网络频道。
        /// </summary>
        public virtual void Shutdown()
        {
            Close();
            PSendState.Reset();
            PNetworkChannelHelper.Shutdown();
        }

        /// <summary>
        /// 注册消息压缩处理器
        /// </summary>
        /// <param name="handler">处理器对象,当设置为空的时候，不启用消息压缩</param>
        public void RegisterMessageCompressHandler(IMessageCompressHandler handler)
        {
            MessageCompressHandler = handler;
        }

        /// <summary>
        /// 注册消息解压处理器
        /// </summary>
        /// <param name="handler">处理器对象,当设置为空的时候，不启用消息解压</param>
        public void RegisterMessageDecompressHandler(IMessageDecompressHandler handler)
        {
            MessageDecompressHandler = handler;
        }

        /// <summary>
        /// 注册网络消息包处理函数。
        /// </summary>
        /// <param name="handler">要注册的网络消息包处理函数。</param>
        public void RegisterHandler(IPacketSendHeaderHandler handler)
        {
            handler.NotNull(nameof(handler));
            m_PacketSendHeaderHandler = handler;
        }


        /// <summary>
        /// 注册网络消息包处理函数。
        /// </summary>
        /// <param name="handler">要注册的网络消息包处理函数。</param>
        public void RegisterHandler(IPacketSendBodyHandler handler)
        {
            handler.NotNull(nameof(handler));
            m_PacketSendBodyHandler = handler;
        }

        /// <summary>
        /// 注册网络消息包处理函数。
        /// </summary>
        /// <param name="handler">要注册的网络消息包处理函数。</param>
        public void RegisterHandler(IPacketReceiveHeaderHandler handler)
        {
            handler.NotNull(nameof(handler));
            m_PacketReceiveHeaderHandler = handler;
        }

        /// <summary>
        /// 注册网络消息包处理函数。
        /// </summary>
        /// <param name="handler">要注册的网络消息包处理函数。</param>
        public void RegisterHandler(IPacketReceiveBodyHandler handler)
        {
            handler.NotNull(nameof(handler));
            m_PacketReceiveBodyHandler = handler;
        }

        /// <summary>
        /// 注册网络消息心跳处理函数，用于处理心跳消息
        /// </summary>
        /// <param name="handler">要注册的网络消息包处理函数</param>
        [Obsolete("Use RegisterHeartBeatHandler instead")]
        public void RegisterHandler(IPacketHeartBeatHandler handler)
        {
            RegisterHeartBeatHandler(handler);
        }

        /// <summary>
        /// 注册网络消息心跳处理函数，用于处理心跳消息
        /// </summary>
        /// <param name="handler">要注册的网络消息包处理函数</param>
        public void RegisterHeartBeatHandler(IPacketHeartBeatHandler handler)
        {
            handler.NotNull(nameof(handler));
            m_PacketHeartBeatHandler = handler;
            if (handler.HeartBeatInterval > 0)
            {
                PHeartBeatInterval = handler.HeartBeatInterval;
            }

            if (handler.MissHeartBeatCountByClose > 0)
            {
                MissHeartBeatCountByClose = handler.MissHeartBeatCountByClose;
            }
        }

        /// <summary>
        /// 设置RPC 的 ErrorCode 不为 0 的时候的处理函数
        /// </summary>
        /// <param name="handler"></param>
        public void SetRPCErrorCodeHandler(EventHandler<MessageObject> handler)
        {
            handler.NotNull(nameof(handler));
            PRpcState.SetRPCErrorCodeHandler(handler);
        }

        /// <summary>
        /// 设置RPC错误的处理函数
        /// </summary>
        /// <param name="handler"></param>
        public void SetRPCErrorHandler(EventHandler<MessageObject> handler)
        {
            handler.NotNull(nameof(handler));
            PRpcState.SetRPCErrorHandler(handler);
        }

        /// <summary>
        /// 设置RPC开始的处理函数
        /// </summary>
        /// <param name="handler"></param>
        public void SetRPCStartHandler(EventHandler<MessageObject> handler)
        {
            handler.NotNull(nameof(handler));
            PRpcState.SetRPCStartHandler(handler);
        }

        /// <summary>
        /// 设置RPC结束的处理函数
        /// </summary>
        /// <param name="handler"></param>
        public void SetRPCEndHandler(EventHandler<MessageObject> handler)
        {
            handler.NotNull(nameof(handler));
            PRpcState.SetRPCEndHandler(handler);
        }


        /// <summary>
        /// 连接到远程主机。
        /// </summary>
        /// <param name="address">远程主机的地址。</param>
        /// <param name="userData">用户自定义数据。</param>
        public virtual void Connect(Uri address, object userData = null)
        {
            if (PSocket != null)
            {
                Close();
                PSocket = null;
            }

            IsVerifyAddress = true;
            ConnectEndPoint = null;

            if (IPAddress.TryParse(address.Host, out var ipAddress))
            {
                ConnectEndPoint = new IPEndPoint(ipAddress, address.Port);
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
                    ConnectEndPoint = new IPEndPoint(ipAddress, port);
                }
                else
                {
                    // 获取IP失败
                    FuLogger.LogError($"IP address is invalid.{host}");
                    IsVerifyAddress = false;
                    Close();
                    PSocket = null;
                }
            }
            catch (Exception e)
            {
                FuLogger.LogError($"IP address is invalid.{host} {e.Message}");
                IsVerifyAddress = false;
                Close();
                PSocket = null;
            }

            CompleteConnectAddress(userData);
        }

        /// <summary>
        /// 地址解析完成后校验地址族并通知子类创建 Socket 发起连接。
        /// </summary>
        /// <param name="userData">用户自定义数据</param>
        private void CompleteConnectAddress(object userData)
        {
            if (IsVerifyAddress && ConnectEndPoint != null)
            {
                switch (ConnectEndPoint.AddressFamily)
                {
                    case AddressFamily.InterNetwork:
                        PEAddressFamily = EAddressFamily.IPv4;
                        break;

                    case AddressFamily.InterNetworkV6:
                        PEAddressFamily = EAddressFamily.IPv6;
                        break;

                    default:
                        var errorMessage = $"Not supported address family '{ConnectEndPoint.AddressFamily}'.";
                        if (NetworkChannelError == null) throw new InvalidOperationException(errorMessage);
                        EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
                            errorCode: ENetworkErrorCode.AddressFamilyError,
                            socketError: SocketError.Success,
                            errorMessage: errorMessage);
                        return;
                }
            }

            PSendState.Reset();
            PReceiveState.PrepareForPacketHeader();

            OnConnectEndPointReady(userData);
        }

        /// <summary>
        /// 连接目标地址已解析完成（或已确认解析失败）时由子类实现：创建 Socket 并发起连接。
        /// </summary>
        /// <param name="userData">用户自定义数据</param>
        protected abstract void OnConnectEndPointReady(object userData);

        /// <summary>
        /// 关闭连接并释放所有相关资源。
        /// </summary>
        public virtual void Close()
        {
            lock (m_CloseLock)
            {
                if (PSocket == null) return;
                PActive = false;

                try
                {
                    PSocket.Shutdown();
                }
                catch
                {
                    // ignored
                }
                finally
                {
                    PSocket.Close();
                    PSocket = null;
                    // 原为 NetworkChannelClosed?.Invoke(this)（可能运行在 Socket 回调线程）——改为入队封送。
                    // PSocket==null 的早退保证重复 Close 不会重复入队；事件队列不被 Close 清空。
                    EnqueueLifecycleEvent(EChannelLifecycleEventType.Closed);
                }

                PSentPacketCount     = 0;
                PReceivedPacketCount = 0;
            }

            // 以下清理放在 m_CloseLock 之外：ProcessSend 会先持有 PSendPacketPool，
            // 其中的错误回调可能再次进入 Close，若这里持 m_CloseLock 再抢 PSendPacketPool，
            // 两条路径锁序相反会形成 ABBA 死锁。
            lock (PSendPacketPool) PSendPacketPool.Clear();
            lock (PHeartBeatLock) PHeartBeatState.Reset(true);

            // 断线/销毁时终结所有挂起的 RPC 请求，否则 await 会永久悬挂。
            PRpcState.Dispose();
            lock (PExecutionMessageLock) m_ExecutionMessageLinkedList.Clear();
        }

        /// <summary>
        /// 向远程主机发送消息包
        /// </summary>
        /// <param name="messageObject"></param>
        /// <typeparam name="TResult"></typeparam>
        public async UniTask<TResult> Call<TResult>(MessageObject messageObject) where TResult : MessageObject, IResponseMessage
        {
            messageObject.NotNull(nameof(messageObject));
            Send(messageObject);
            var result = await PRpcState.Call(messageObject);
            return result as TResult;
        }

        /// <summary>
        /// 向远程主机发送消息包。
        /// </summary>
        /// <typeparam name="T">消息包类型。</typeparam>
        /// <param name="messageObject">要发送的消息包。</param>
        public void Send<T>(T messageObject) where T : MessageObject
        {
            messageObject.NotNull(nameof(messageObject));
            if (PSocket == null)
            {
                const string errorMessage = "You must connect first.";
                if (NetworkChannelError == null) throw new InvalidOperationException(errorMessage);
                EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
                    errorCode: ENetworkErrorCode.SendError,
                    socketError: SocketError.Success,
                    errorMessage: errorMessage);
                return;
            }

            if (!PActive)
            {
                const string errorMessage = "Socket is not active.";
                if (NetworkChannelError == null) throw new InvalidOperationException(errorMessage);
                EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
                    errorCode: ENetworkErrorCode.SendError,
                    socketError: SocketError.Success,
                    errorMessage: errorMessage);
                return;
            }

            if (messageObject == null)
            {
                const string errorMessage = "Packet is invalid.";
                if (NetworkChannelError == null) throw new InvalidOperationException(errorMessage);
                EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
                    errorCode: ENetworkErrorCode.SendError,
                    socketError: SocketError.Success,
                    errorMessage: errorMessage);
                return;
            }

            lock (PSendPacketPool)
            {
                PSendPacketPool.AddLast(messageObject);
            }
        }

        /// <summary>
        /// 释放资源。
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源。
        /// </summary>
        /// <param name="disposing">释放资源标记。</param>
        private void Dispose(bool disposing)
        {
            if (m_Disposed) return;

            if (disposing)
            {
                Close();
                PSendState.Dispose();
                PReceiveState.Dispose();
            }

            m_Disposed = true;
        }

        /// <summary>
        /// 处理发送消息对象
        /// </summary>
        /// <param name="messageObject">消息对象</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        protected virtual bool ProcessSendMessage(MessageObject messageObject)
        {
            var serializeResult = PNetworkChannelHelper.SerializePacketHeader(messageObject, PSendState.Stream, out var messageBodyBuffer);
            if (serializeResult)
            {
                serializeResult = PNetworkChannelHelper.SerializePacketBody(messageBodyBuffer, PSendState.Stream);
            }
            else
            {
                const string errorMessage = "Serialized packet failure.";
                throw new InvalidOperationException(errorMessage);
            }

            return serializeResult;
        }

        /// <summary>
        /// 处理消息发送
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        protected virtual bool ProcessSend()
        {
            lock (PSendPacketPool)
            {
                if (PSendState.Stream.Length > 0 || PSendPacketPool.Count <= 0) return false;

                while (PSendPacketPool.First != null)
                {
                    var  messageObject = PSendPacketPool.First.Value;
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

                    if (serializeResult) continue;
                    const string errorMessage = "Serialized packet failure.";
                    if (NetworkChannelError == null) throw new InvalidOperationException(errorMessage);
                    EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
                        errorCode: ENetworkErrorCode.SerializeError,
                        socketError: SocketError.Success,
                        errorMessage: errorMessage);
                    return false;

                    // PSendState.Reset();
                }

                PSendState.Stream.Position = 0L;
                return true;
            }
        }

        protected void ProcessReceive() { }

        /// <summary>
        /// 单个数据包的包体长度上限（默认 1 MiB，<b>可按协议需要调整</b>）。
        /// 包头中的 PacketLength 直接来自网络报文，若不设上限，畸形包会导致超大数组分配/内存耗尽。
        /// 默认值依据：本项目 proto 未使用 bytes 大字段（仅 repeated/map 的常规消息），
        /// 实际消息体在 KB 量级，1 MiB 是宽松的健全性上限；若后续协议引入大消息，
        /// 由具体通道实现（TCP / WebSocket）在合适时机调高本值即可。
        /// </summary>
        protected static int MaxPacketBodyLength { get; set; } = 1024 * 1024;

        /// <summary>
        /// 校验包头中的包长度并换算包体长度。
        /// </summary>
        /// <param name="header">已解析的包头</param>
        /// <returns>包体长度</returns>
        /// <exception cref="InvalidOperationException">包长度非法或超出上限时抛出</exception>
        protected static int ValidateAndGetPacketBodyLength(IPacketReceiveHeaderHandler header)
        {
            var packetLength = header.PacketLength;
            var headerLength = header.PacketHeaderLength;
            if (packetLength < headerLength)
            {
                throw new InvalidOperationException($"Packet length is invalid. packetLength:{packetLength}, headerLength:{headerLength}");
            }

            var bodyLength = (long)packetLength - headerLength;
            if (bodyLength > MaxPacketBodyLength)
            {
                throw new InvalidOperationException($"Packet body length exceeds limit. bodyLength:{bodyLength}, limit:{MaxPacketBodyLength}");
            }

            return (int)bodyLength;
        }

        protected void DebugSendLog(MessageObject messageObject)
        {
#if ENABLE_NETWORK_REQ_LOG
            if (!IgnoreSendIds.Contains(PacketSendHeaderHandler.Id))
            {
                FuLogger.LogInfo($"发送消息 ID:[{PacketSendHeaderHandler.Id},{messageObject.UniqueId},{messageObject.GetType().Name}] 消息内容:{UtilityAOT.Json.ToJson(messageObject)}");
            }
#endif
        }

        protected void DebugReceiveLog(MessageObject messageObject)
        {
#if ENABLE_NETWORK_RSP_LOG
            if (!IgnoreReceiveIds.Contains(PacketReceiveHeaderHandler.Id))
            {
                FuLogger.LogInfo($"收到消息 ID:[{PacketReceiveHeaderHandler.Id},{messageObject.UniqueId},{messageObject.GetType().Name}] 消息内容:{UtilityAOT.Json.ToJson(messageObject)}");
            }
#endif
        }


        /// <summary>
        /// 设置忽略的消息打印列表
        /// </summary>
        /// <param name="sendIds">发送列表</param>
        /// <param name="receiveIds">接收列表</param>
        public void SetIgnoreLogNetworkIds(List<int> sendIds, List<int> receiveIds)
        {
            IgnoreSendIds    = sendIds;
            IgnoreReceiveIds = receiveIds;
        }
    }
}
