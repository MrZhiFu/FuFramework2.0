using System;
using System.Collections.Generic;
using System.Net.Sockets;
using FuFramework.Core.Runtime;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable once CheckNamespace
namespace FuFramework.Network.Runtime
{
    /// <summary>
    /// 网络管理器。
    /// </summary>
    public sealed partial class NetworkManager : FuModule, INetworkManager
    {
        private readonly Dictionary<string, NetworkChannelBase> m_NetworkChannels = new();
        private EventHandler<NetworkConnectedEventArgs> m_NetworkConnectedEventHandler = null;
        private EventHandler<NetworkClosedEventArgs> m_NetworkClosedEventHandler = null;
        private EventHandler<NetworkMissHeartBeatEventArgs> m_NetworkMissHeartBeatEventHandler = null;
        private EventHandler<NetworkErrorEventArgs> m_NetworkErrorEventHandler = null;

        /// <summary>
        /// 获取网络频道数量。
        /// </summary>
        public int NetworkChannelCount => m_NetworkChannels.Count;

        /// <summary>
        /// 网络连接成功事件。
        /// </summary>
        public event EventHandler<NetworkConnectedEventArgs> NetworkConnected
        {
            add => m_NetworkConnectedEventHandler += value;
            remove => m_NetworkConnectedEventHandler -= value;
        }

        /// <summary>
        /// 网络连接关闭事件。
        /// </summary>
        public event EventHandler<NetworkClosedEventArgs> NetworkClosed
        {
            add => m_NetworkClosedEventHandler += value;
            remove => m_NetworkClosedEventHandler -= value;
        }

        /// <summary>
        /// 网络心跳包丢失事件。
        /// </summary>
        public event EventHandler<NetworkMissHeartBeatEventArgs> NetworkMissHeartBeat
        {
            add => m_NetworkMissHeartBeatEventHandler += value;
            remove => m_NetworkMissHeartBeatEventHandler -= value;
        }

        /// <summary>
        /// 网络错误事件。
        /// </summary>
        public event EventHandler<NetworkErrorEventArgs> NetworkError
        {
            add => m_NetworkErrorEventHandler += value;
            remove => m_NetworkErrorEventHandler -= value;
        }

        /// <summary>
        /// 网络管理器轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        protected override void Update(float elapseSeconds, float realElapseSeconds)
        {
            foreach (var networkChannel in m_NetworkChannels)
            {
                networkChannel.Value.Update(elapseSeconds, realElapseSeconds);
            }
        }

        /// <summary>
        /// 关闭并清理网络管理器。
        /// </summary>
        protected override void Shutdown()
        {
            foreach (var networkChannel in m_NetworkChannels)
            {
                var networkChannelBase = networkChannel.Value;
                networkChannelBase.NetworkChannelConnected -= OnNetworkChannelConnected;
                networkChannelBase.NetworkChannelClosed -= OnNetworkChannelClosed;
                networkChannelBase.NetworkChannelMissHeartBeat -= OnNetworkChannelMissHeartBeat;
                networkChannelBase.NetworkChannelError -= OnNetworkChannelError;
                networkChannelBase.Shutdown();
            }

            m_NetworkChannels.Clear();
        }

        /// <summary>
        /// 检查是否存在网络频道。
        /// </summary>
        /// <param name="channelName">网络频道名称。</param>
        /// <returns>是否存在网络频道。</returns>
        public bool HasNetworkChannel(string channelName)
        {
            return m_NetworkChannels.ContainsKey(channelName ?? string.Empty);
        }

        /// <summary>
        /// 获取网络频道。
        /// </summary>
        /// <param name="channelName">网络频道名称。</param>
        /// <returns>要获取的网络频道。</returns>
        public INetworkChannel GetNetworkChannel(string channelName)
        {
            return m_NetworkChannels.GetValueOrDefault(channelName ?? string.Empty);
        }

        /// <summary>
        /// 获取所有网络频道。
        /// </summary>
        /// <returns>所有网络频道。</returns>
        public INetworkChannel[] GetAllNetworkChannels()
        {
            var index = 0;
            var results = new INetworkChannel[m_NetworkChannels.Count];
            foreach (var networkChannel in m_NetworkChannels)
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
            foreach (var networkChannel in m_NetworkChannels)
            {
                results.Add(networkChannel.Value);
            }
        }

        /// <summary>
        /// 创建网络频道。
        /// </summary>
        /// <param name="channelName">网络频道名称。</param>
        /// <param name="networkChannelHelper">网络频道辅助器。</param>
        /// <param name="rpcTimeout">RPC超时时间</param>
        /// <returns>要创建的网络频道。</returns>
        public INetworkChannel CreateNetworkChannel(string channelName, INetworkChannelHelper networkChannelHelper, int rpcTimeout)
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
            m_NetworkChannels.Add(channelName, networkChannel);
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
            if (!m_NetworkChannels.TryGetValue(channelName ?? string.Empty, out var networkChannel)) return false;
            networkChannel.NetworkChannelConnected -= OnNetworkChannelConnected;
            networkChannel.NetworkChannelClosed -= OnNetworkChannelClosed;
            networkChannel.NetworkChannelMissHeartBeat -= OnNetworkChannelMissHeartBeat;
            networkChannel.NetworkChannelError -= OnNetworkChannelError;
            networkChannel.Shutdown();
            return channelName != null && m_NetworkChannels.Remove(channelName);
        }

        private void OnNetworkChannelConnected(NetworkChannelBase networkChannel, object userData)
        {
            if (m_NetworkConnectedEventHandler == null) return;
            lock (m_NetworkConnectedEventHandler)
            {
                var networkConnectedEventArgs = NetworkConnectedEventArgs.Create(networkChannel, userData);
                m_NetworkConnectedEventHandler(this, networkConnectedEventArgs);
            }
        }

        private void OnNetworkChannelClosed(NetworkChannelBase networkChannel)
        {
            if (m_NetworkClosedEventHandler == null) return;
            lock (m_NetworkClosedEventHandler)
            {
                var networkClosedEventArgs = NetworkClosedEventArgs.Create(networkChannel);
                m_NetworkClosedEventHandler(this, networkClosedEventArgs);
            }
        }

        private void OnNetworkChannelMissHeartBeat(NetworkChannelBase networkChannel, int missHeartBeatCount)
        {
            if (m_NetworkMissHeartBeatEventHandler == null) return;
            lock (m_NetworkMissHeartBeatEventHandler)
            {
                var networkMissHeartBeatEventArgs = NetworkMissHeartBeatEventArgs.Create(networkChannel, missHeartBeatCount);
                m_NetworkMissHeartBeatEventHandler(this, networkMissHeartBeatEventArgs);
            }
        }

        private void OnNetworkChannelError(NetworkChannelBase networkChannel, NetworkErrorCode errorCode, SocketError socketErrorCode,
            string errorMessage)
        {
            if (m_NetworkErrorEventHandler == null) return;
            lock (m_NetworkErrorEventHandler)
            {
                var networkErrorEventArgs = NetworkErrorEventArgs.Create(networkChannel, errorCode, socketErrorCode, errorMessage);
                m_NetworkErrorEventHandler(this, networkErrorEventArgs);
            }
        }
    }
}