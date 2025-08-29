using System.Collections.Generic;
using FuFramework.Core.Runtime;
using FuFramework.Event.Runtime;
using UnityEngine;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable once CheckNamespace
namespace FuFramework.Network.Runtime
{
    /// <summary>
    /// 网络组件。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/Network")]
    public sealed class NetworkComponent : FuComponent
    {
        private INetworkManager m_NetworkManager; // 网络管理器。
        private EventManager    m_EventComponent; // 事件组件。

        /// <summary>
        /// 忽略发送的网络消息ID的日志打印
        /// </summary>
        [SerializeField] private List<int> m_IgnoredSendNetworkIds = new();

        /// <summary>
        /// 忽略接收的网络消息ID的日志打印
        /// </summary>
        [SerializeField] private List<int> m_IgnoredReceiveNetworkIds = new();

        /// <summary>
        /// RPC超时时间，以毫秒为单位,默认为5秒
        /// </summary>
        [SerializeField] private int m_rpcTimeout = 5000;

        /// <summary>
        /// 获取网络频道数量。
        /// </summary>
        public int NetworkChannelCount => m_NetworkManager.NetworkChannelCount;

        protected override void OnInit()
        {
            m_NetworkManager = FuEntry.GetModule<INetworkManager>();
            if (m_NetworkManager == null)
            {
                Log.Fatal("Network manager is invalid.");
                return;
            }

            m_NetworkManager.NetworkConnected += OnNetworkConnected;
            m_NetworkManager.NetworkClosed += OnNetworkClosed;
            m_NetworkManager.NetworkMissHeartBeat += OnNetworkMissHeartBeat;
            m_NetworkManager.NetworkError += OnNetworkError;
            
            m_EventComponent = ModuleManager.GetModule<EventManager>();
            if (!m_EventComponent) Log.Fatal("Event component is invalid.");
        }
        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
        }
        protected override void OnShutdown(ShutdownType shutdownType)
        {
        }

        /// <summary>
        /// 检查是否存在网络频道。
        /// </summary>
        /// <param name="channelName">网络频道名称。</param>
        /// <returns>是否存在网络频道。</returns>
        public bool HasNetworkChannel(string channelName)
        {
            FuGuard.NotNullOrEmpty(channelName, nameof(channelName));
            return m_NetworkManager.HasNetworkChannel(channelName);
        }

        /// <summary>
        /// 获取网络频道。
        /// </summary>
        /// <param name="channelName">网络频道名称。</param>
        /// <returns>要获取的网络频道。</returns>
        public INetworkChannel GetNetworkChannel(string channelName)
        {
            FuGuard.NotNullOrEmpty(channelName, nameof(channelName));
            return m_NetworkManager.GetNetworkChannel(channelName);
        }

        /// <summary>
        /// 获取所有网络频道。
        /// </summary>
        /// <returns>所有网络频道。</returns>
        public INetworkChannel[] GetAllNetworkChannels() => m_NetworkManager.GetAllNetworkChannels();

        /// <summary>
        /// 获取所有网络频道。
        /// </summary>
        /// <param name="results">所有网络频道。</param>
        public void GetAllNetworkChannels(List<INetworkChannel> results) => m_NetworkManager.GetAllNetworkChannels(results);

        /// <summary>
        /// 创建网络频道。
        /// </summary>
        /// <param name="channelName">网络频道名称。</param>
        /// <param name="networkChannelHelper">网络频道辅助器。</param>
        /// <returns>要创建的网络频道。</returns>
        public INetworkChannel CreateNetworkChannel(string channelName, INetworkChannelHelper networkChannelHelper)
        {
            FuGuard.NotNullOrEmpty(channelName, nameof(channelName));
            var networkChannel = m_NetworkManager.CreateNetworkChannel(channelName, networkChannelHelper, m_rpcTimeout);
            networkChannel.SetIgnoreLogNetworkIds(m_IgnoredSendNetworkIds, m_IgnoredReceiveNetworkIds);
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
            return m_NetworkManager.DestroyNetworkChannel(channelName);
        }

        /// <summary>
        /// 网络连接成功回调。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void OnNetworkConnected(object sender, NetworkConnectedEventArgs eventArgs) => m_EventComponent.Fire(this, eventArgs);

        /// <summary>
        /// 网络连接关闭回调。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void OnNetworkClosed(object sender, NetworkClosedEventArgs eventArgs) => m_EventComponent.Fire(this, eventArgs);

        /// <summary>
        /// 网络心跳包丢失回调。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void OnNetworkMissHeartBeat(object sender, NetworkMissHeartBeatEventArgs eventArgs) => m_EventComponent.Fire(this, eventArgs);

        /// <summary>
        /// 网络错误回调。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void OnNetworkError(object sender, NetworkErrorEventArgs eventArgs) => m_EventComponent.Fire(this, eventArgs);
    }
}