using System;
using System.Collections.Generic;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
    /// <summary>
    /// 网络管理模块的公共 API。
    /// 功能：
    ///     1. 提供网络频道的创建、获取、查询、销毁接口。
    /// </summary>
    public sealed partial class NetworkModule : ModuleBase
    {
        /// <summary>
        /// 获取网络频道数量。
        /// </summary>
        public int NetworkChannelCount => m_NetworkChannelDict.Count;

        #region 查询网络频道

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
            var index   = 0;
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
            results.NotNull(nameof(results));

            results.Clear();
            foreach (var networkChannel in m_NetworkChannelDict)
            {
                results.Add(networkChannel.Value);
            }
        }

        #endregion

        #region 创建网络频道

        /// <summary>
        /// 创建网络频道。
        /// </summary>
        /// <param name="channelName">网络频道名称。</param>
        /// <param name="networkChannelHelper">网络频道辅助器。</param>
        /// <param name="rpcTimeout">RPC超时时间，默认5000毫秒。</param>
        /// <returns>要创建的网络频道。</returns>
        public INetworkChannel CreateNetworkChannel(string channelName, INetworkChannelHelper networkChannelHelper, int rpcTimeout = 5000)
        {
            channelName.NotNullOrEmpty(nameof(channelName));
            networkChannelHelper.NotNull(nameof(networkChannelHelper));

            if (HasNetworkChannel(channelName))
            {
                throw new InvalidOperationException($"[NetworkModule]网络频道已存在: '{channelName ?? string.Empty}'.");
            }
#if (ENABLE_GAME_FRAME_X_WEB_SOCKET && UNITY_WEBGL) || FORCE_ENABLE_WEB_SOCKET
            NetworkChannelBase networkChannel = new WebSocketNetworkChannel(channelName, networkChannelHelper, rpcTimeout);
#else
            NetworkChannelBase networkChannel = new SystemTcpNetworkChannel(channelName, networkChannelHelper, rpcTimeout);
#endif
            networkChannel.NetworkChannelConnected     += OnNetworkChannelConnected;
            networkChannel.NetworkChannelClosed        += OnNetworkChannelClosed;
            networkChannel.NetworkChannelMissHeartBeat += OnNetworkChannelMissHeartBeat;
            networkChannel.NetworkChannelError         += OnNetworkChannelError;
            m_NetworkChannelDict.Add(channelName, networkChannel);
            return networkChannel;
        }

        #endregion

        #region 销毁网络频道

        /// <summary>
        /// 销毁网络频道。
        /// </summary>
        /// <param name="channelName">网络频道名称。</param>
        /// <returns>是否销毁网络频道成功。</returns>
        public bool DestroyNetworkChannel(string channelName)
        {
            channelName.NotNullOrEmpty(nameof(channelName));
            if (!m_NetworkChannelDict.TryGetValue(channelName ?? string.Empty, out var networkChannel)) return false;
            networkChannel.NetworkChannelConnected     -= OnNetworkChannelConnected;
            networkChannel.NetworkChannelClosed        -= OnNetworkChannelClosed;
            networkChannel.NetworkChannelMissHeartBeat -= OnNetworkChannelMissHeartBeat;
            networkChannel.NetworkChannelError         -= OnNetworkChannelError;
            networkChannel.Shutdown();
            return channelName != null && m_NetworkChannelDict.Remove(channelName);
        }

        #endregion
    }
}
