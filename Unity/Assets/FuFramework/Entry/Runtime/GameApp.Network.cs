using FuFramework.Core.Runtime;
using GameFrameX.Network.Runtime;

public static partial class GameApp
{
    /// <summary>
    /// 获取网络组件。
    /// </summary>
    public static NetworkComponent Network
    {
        get
        {
            if (_network == null)
            {
                _network = GameEntry.GetComponent<NetworkComponent>();
            }

            return _network;
        }
    }

    private static NetworkComponent _network;
}