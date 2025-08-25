// ReSharper disable once CheckNamespace

namespace FuFramework.Network.Runtime
{
    public sealed partial class NetworkManager
    {
        /// <summary>
        /// 网络连接状态
        /// </summary>
        public sealed class ConnectState
        {
            /// <summary>
            /// Socket
            /// </summary>
            public INetworkSocket Socket { get; }

            /// <summary>
            /// 用户自定义数据
            /// </summary>
            public object UserData { get; }

            public ConnectState(INetworkSocket socket, object userData)
            {
                Socket = socket;
                UserData = userData;
            }
        }
    }
}