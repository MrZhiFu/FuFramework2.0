using FuFramework.Event.Runtime;
using ReferencePool = FuFramework.Core.Runtime.ReferencePool;

// ReSharper disable once CheckNamespace
namespace FuFramework.Network.Runtime
{
    /// <summary>
    /// 网络连接成功事件。
    /// </summary>
    public sealed class NetworkConnectedEventArgs : GameEventArgs
    {
        /// <summary>
        /// 网络连接成功事件编号。
        /// </summary>
        public static readonly string EventId = typeof(NetworkConnectedEventArgs).FullName;

        /// <summary>
        /// 获取网络连接成功事件编号。
        /// </summary>
        public override string Id => EventId;

        /// <summary>
        /// 获取网络频道。
        /// </summary>
        public INetworkChannel NetworkChannel { get; private set; }

        /// <summary>
        /// 获取用户自定义数据。
        /// </summary>
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public object UserData { get; private set; }

        /// <summary>
        /// 创建网络连接成功事件。
        /// </summary>
        /// <param name="networkChannel">网络频道。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>创建的网络连接成功事件。</returns>
        public static NetworkConnectedEventArgs Create(INetworkChannel networkChannel, object userData)
        {
            var networkConnectedEventArgs = ReferencePool.Acquire<NetworkConnectedEventArgs>();
            networkConnectedEventArgs.NetworkChannel = networkChannel;
            networkConnectedEventArgs.UserData = userData;
            return networkConnectedEventArgs;
        }

        /// <summary>
        /// 清理网络连接成功事件。
        /// </summary>
        public override void Clear()
        {
            NetworkChannel = null;
            UserData = null;
        }
    }
}