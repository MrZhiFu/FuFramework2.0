using FuFramework.Core.Runtime;
using GameFrameX.Asset.Runtime;

public static partial class GameApp
{
    /// <summary>
    /// 获取Asset组件。
    /// </summary>
    public static AssetComponent Asset
    {
        get
        {
            if (_asset == null)
            {
                _asset = GameEntry.GetComponent<AssetComponent>();
            }

            return _asset;
        }
    }

    private static AssetComponent _asset;
}