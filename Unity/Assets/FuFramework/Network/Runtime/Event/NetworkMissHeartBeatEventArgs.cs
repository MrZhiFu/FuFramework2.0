using FuFramework.Event.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Network.Runtime
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
            var networkMissHeartBeatEventArgs = ReferencePool.Runtime.ReferencePool.Acquire<NetworkMissHeartBeatEventArgs>();
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