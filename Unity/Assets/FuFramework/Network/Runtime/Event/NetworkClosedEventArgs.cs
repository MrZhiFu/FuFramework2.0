using FuFramework.Event.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Network.Runtime
{
    /// <summary>
    /// 网络连接关闭事件。
    /// </summary>
    
    public sealed class NetworkClosedEventArgs : GameEventArgs
    {
        /// <summary>
        /// 获取网络连接关闭事件编号。
        /// </summary>
        public override string Id => EventId;

        /// <summary>
        /// 网络连接关闭事件编号。
        /// </summary>
        public static readonly string EventId = typeof(NetworkClosedEventArgs).FullName;

        /// <summary>
        /// 获取网络频道。
        /// </summary>
        public INetworkChannel NetworkChannel { get; private set; }

        /// <summary>
        /// 创建网络连接关闭事件。
        /// </summary>
        /// <param name="networkChannel">网络频道。</param>
        /// <returns>创建的网络连接关闭事件。</returns>
        public static NetworkClosedEventArgs Create(INetworkChannel networkChannel)
        {
            var networkClosedEventArgs = ReferencePool.Runtime.ReferencePool.Acquire<NetworkClosedEventArgs>();
            networkClosedEventArgs.NetworkChannel = networkChannel;
            return networkClosedEventArgs;
        }

        /// <summary>
        /// 清理网络连接关闭事件。
        /// </summary>
        public override void Clear() => NetworkChannel = null;
    }
}