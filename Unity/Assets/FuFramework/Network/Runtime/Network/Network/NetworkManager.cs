using System;
using System.Collections.Generic;
using System.Net.Sockets;
using FuFramework.Core.Runtime;
using FuFramework.Event.Runtime;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable once CheckNamespace
namespace FuFramework.Network.Runtime
{
    /// <summary>
    /// 网络管理器。
    /// </summary>
    public sealed partial class NetworkManager : FuComponent
    {
        /// <summary>
        /// 获取游戏框架模块优先级。
        /// </summary>
        /// <remarks>优先级较高的模块会优先轮询，并且关闭操作会后进行。</remarks>
        protected override int Priority => ModulePriority.Core;
        
        /// <summary>
        /// 所有网络频道的字典，Key为网络频道名称，Value为网络频道对象。
        /// </summary>
        private readonly Dictionary<string, NetworkChannelBase> m_NetworkChannelDict = new();

        /// <summary>
        /// 事件组件。
        /// </summary>
        private EventManager m_EventManager;

        /// <summary>
        /// 获取网络频道数量。
        /// </summary>
        public int NetworkChannelCount => m_NetworkChannelDict.Count;

        /// <summary>
        /// 初始化
        /// </summary>
        protected override void OnInit()
        {
            m_EventManager = ModuleManager.Instance.GetModule<EventManager>();
        }

        /// <summary>
        /// 网络管理器轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            foreach (var networkChannel in m_NetworkChannelDict)
            {
                networkChannel.Value.Update(elapseSeconds, realElapseSeconds);
            }
        }

        /// <summary>
        /// 关闭
        /// </summary>
        /// <param name="shutdownType"></param>
        protected override void OnShutdown(ShutdownType shutdownType)
        {
            foreach (var networkChannel in m_NetworkChannelDict)
            {
                var networkChannelBase = networkChannel.Value;
                networkChannelBase.NetworkChannelConnected -= OnNetworkChannelConnected;
                networkChannelBase.NetworkChannelClosed -= OnNetworkChannelClosed;
                networkChannelBase.NetworkChannelMissHeartBeat -= OnNetworkChannelMissHeartBeat;
                networkChannelBase.NetworkChannelError -= OnNetworkChannelError;
                networkChannelBase.Shutdown();
            }

            m_NetworkChannelDict.Clear();
        }

        /// <summary>
        /// 检查是否存在网络频道。
        /// </summary>
        /// <param name="channelName">网络频道名称。</param>
        /// <returns>是否存在网络频道。</returns>
        public bool HasNetworkChannel(string channelName)
        {
            return m_NetworkChannelDict.ContainsKey(channelName ?? string.Empty);
        }

        /// <summary>
        /// 获取网络频道。
        /// </summary>
        /// <param name="channelName">网络频道名称。</param>
        /// <returns>要获取的网络频道。</returns>
        public INetworkChannel GetNetworkChannel(string channelName)
        {
            return m_NetworkChannelDict.GetValueOrDefault(channelName ?? string.Empty);
        }

        /// <summary>
        /// 获取所有网络频道。
        /// </summary>
        /// <returns>所有网络频道。</returns>
        public INetworkChannel[] GetAllNetworkChannels()
        {
            var index = 0;
            var results = new INetworkChannel[m_NetworkChannelDict.Count];
            foreach (var networkChannel in m_NetworkChannelDict)
            {
                results[index++] = networkChannel.Value;
            }

            return results;
        }

        /// <summary>
        /// 获取所有网络频道。
        /// </summary>
        /// <param name="results">所有网络频道。</param>
        public void GetAllNetworkChannels(List<INetworkChannel> results)
        {
            FuGuard.NotNull(results, nameof(results));

            results.Clear();
            foreach (var networkChannel in m_NetworkChannelDict)
            {
                results.Add(networkChannel.Value);
            }
        }

        /// <summary>
        /// 创建网络频道。
        /// </summary>
        /// <param name="channelName">网络频道名称。</param>
        /// <param name="networkChannelHelper">网络频道辅助器。</param>
        /// <param name="rpcTimeout">RPC超时时间，默认5000毫秒。</param>
        /// <returns>要创建的网络频道。</returns>
        public INetworkChannel CreateNetworkChannel(string channelName, INetworkChannelHelper networkChannelHelper, int rpcTimeout = 5000)
        {
            FuGuard.NotNullOrEmpty(channelName, nameof(channelName));
            FuGuard.NotNull(networkChannelHelper, nameof(networkChannelHelper));

            if (HasNetworkChannel(channelName))
            {
                throw new FuException(Utility.Text.Format("Already exist network channel '{0}'.", channelName ?? string.Empty));
            }
#if (ENABLE_GAME_FRAME_X_WEB_SOCKET && UNITY_WEBGL) || FORCE_ENABLE_GAME_FRAME_X_WEB_SOCKET
            NetworkChannelBase networkChannel = new WebSocketNetworkChannel(channelName, networkChannelHelper, rpcTimeout);
#else
            NetworkChannelBase networkChannel = new SystemTcpNetworkChannel(channelName, networkChannelHelper, rpcTimeout);
#endif
            networkChannel.NetworkChannelConnected += OnNetworkChannelConnected;
            networkChannel.NetworkChannelClosed += OnNetworkChannelClosed;
            networkChannel.NetworkChannelMissHeartBeat += OnNetworkChannelMissHeartBeat;
            networkChannel.NetworkChannelError += OnNetworkChannelError;
            m_NetworkChannelDict.Add(channelName, networkChannel);
            return networkChannel;
        }

        /// <summary>
        /// 销毁网络频道。
        /// </summary>
        /// <param name="channelName">网络频道名称。</param>
        /// <returns>是否销毁网络频道成功。</returns>
        public bool DestroyNetworkChannel(string channelName)
        {
            FuGuard.NotNullOrEmpty(channelName, nameof(channelName));
            if (!m_NetworkChannelDict.TryGetValue(channelName ?? string.Empty, out var networkChannel)) return false;
            networkChannel.NetworkChannelConnected -= OnNetworkChannelConnected;
            networkChannel.NetworkChannelClosed -= OnNetworkChannelClosed;
            networkChannel.NetworkChannelMissHeartBeat -= OnNetworkChannelMissHeartBeat;
            networkChannel.NetworkChannelError -= OnNetworkChannelError;
            networkChannel.Shutdown();
            return channelName != null && m_NetworkChannelDict.Remove(channelName);
        }

        private void OnNetworkChannelConnected(NetworkChannelBase networkChannel, object userData)
        {
            var networkConnectedEventArgs = NetworkConnectedEventArgs.Create(networkChannel, userData);
            m_EventManager.Fire(this, networkConnectedEventArgs);
        }

        private void OnNetworkChannelClosed(NetworkChannelBase networkChannel)
        {
            var networkClosedEventArgs = NetworkClosedEventArgs.Create(networkChannel);
            m_EventManager.Fire(this, networkClosedEventArgs);
        }

        private void OnNetworkChannelMissHeartBeat(NetworkChannelBase networkChannel, int missHeartBeatCount)
        {
            var networkMissHeartBeatEventArgs = NetworkMissHeartBeatEventArgs.Create(networkChannel, missHeartBeatCount);
            m_EventManager.Fire(this, networkMissHeartBeatEventArgs);
        }

        private void OnNetworkChannelError(NetworkChannelBase networkChannel, NetworkErrorCode errorCode, SocketError socketErrorCode,
            string errorMessage)
        {
            var networkErrorEventArgs = NetworkErrorEventArgs.Create(networkChannel, errorCode, socketErrorCode, errorMessage);
            m_EventManager.Fire(this, networkErrorEventArgs);
        }
    }
}