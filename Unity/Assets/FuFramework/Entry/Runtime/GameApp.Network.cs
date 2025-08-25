using FuFramework.Core.Runtime;
using FuFramework.Network.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entry.Runtime
{
    public static partial class GameApp
    {
        private static NetworkComponent _network;

        /// <summary>
        /// 获取网络组件。
        /// </summary>
        public static NetworkComponent Network
        {
            get
            {
                if (!_network) _network = GameEntry.GetComponent<NetworkComponent>();
                return _network;
            }
        }
    }
}