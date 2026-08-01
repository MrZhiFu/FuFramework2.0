using Hotfix.Framework.Event;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{
    /// <summary>
    /// 网络心跳包丢失事件。
    /// </summary>
    public sealed class NetworkMissHeartBeatEventArgs : GameEventArgs
    {
        /// <summary>
        /// 获取网络心跳包丢失事件编号。
        /// </summary>
        public override string Id => EventId;

        /// <summary>
        /// 网络心跳包丢失事件编号。
        /// </summary>
        public static readonly string EventId = typeof(NetworkMissHeartBeatEventArgs).FullName;

        /// <summary>
        /// 获取网络频道。
        /// </summary>
        public INetworkChannel NetworkChannel { get; private set; }

        /// <summary>
        /// 获取心跳包已丢失次数。
        /// </summary>
        public int MissCount { get; private set; }

        /// <summary>
        /// 创建网络心跳包丢失事件。
        /// </summary>
        /// <param name="networkChannel">网络频道。</param>
        /// <param name="missCount">心跳包已丢失次数。</param>
        /// <returns>创建的网络心跳包丢失事件。</returns>
        public static NetworkMissHeartBeatEventArgs Create(INetworkChannel networkChannel, int missCount)
        {
            var networkMissHeartBeatEventArgs = GlobalModule.ReferencePoolModule.Acquire<NetworkMissHeartBeatEventArgs>();
            networkMissHeartBeatEventArgs.NetworkChannel = networkChannel;
            networkMissHeartBeatEventArgs.MissCount      = missCount;
            return networkMissHeartBeatEventArgs;
        }

        /// <summary>
        /// 清理网络心跳包丢失事件。
        /// </summary>
        public override void Clear()
        {
            NetworkChannel = null;
            MissCount      = 0;
        }
    }
}
