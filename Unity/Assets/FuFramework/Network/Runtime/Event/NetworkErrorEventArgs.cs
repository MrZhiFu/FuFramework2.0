using System.Net.Sockets;
using FuFramework.Event.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Network.Runtime
{
    /// <summary>
    /// 网络错误事件。
    /// </summary>
    public sealed class NetworkErrorEventArgs : GameEventArgs
    {
        /// <summary>
        /// 获取网络错误事件编号。
        /// </summary>
        public override string Id => EventId;

        /// <summary>
        /// 网络错误事件编号。
        /// </summary>
        public static readonly string EventId = typeof(NetworkErrorEventArgs).FullName;

        /// <summary>
        /// 获取网络频道。
        /// </summary>
        public INetworkChannel NetworkChannel { get; private set; }

        /// <summary>
        /// 获取错误码。
        /// </summary>
        public NetworkErrorCode ErrorCode { get; private set; } = NetworkErrorCode.Unknown;

        /// <summary>
        /// 获取 Socket 错误码。
        /// </summary>
        public SocketError SocketErrorCode { get; private set; } = SocketError.Success;

        /// <summary>
        /// 获取错误信息。
        /// </summary>
        public string ErrorMessage { get; private set; }

        /// <summary>
        /// 创建网络错误事件。
        /// </summary>
        /// <param name="networkChannel">网络频道。</param>
        /// <param name="errorCode">错误码。</param>
        /// <param name="socketErrorCode">Socket 错误码。</param>
        /// <param name="errorMessage">错误信息。</param>
        /// <returns>创建的网络错误事件。</returns>
        public static NetworkErrorEventArgs Create(INetworkChannel networkChannel, NetworkErrorCode errorCode, SocketError socketErrorCode,
                                                   string errorMessage)
        {
            var networkErrorEventArgs = ReferencePool.Runtime.ReferencePool.Acquire<NetworkErrorEventArgs>();
            networkErrorEventArgs.NetworkChannel  = networkChannel;
            networkErrorEventArgs.ErrorCode       = errorCode;
            networkErrorEventArgs.SocketErrorCode = socketErrorCode;
            networkErrorEventArgs.ErrorMessage    = errorMessage;
            return networkErrorEventArgs;
        }

        /// <summary>
        /// 清理网络错误事件。
        /// </summary>
        public override void Clear()
        {
            NetworkChannel  = null;
            ErrorMessage    = null;
            ErrorCode       = NetworkErrorCode.Unknown;
            SocketErrorCode = SocketError.Success;
        }
    }
}