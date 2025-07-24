using GameFrameX.Runtime;

public static partial class GameApp
{
    /// <summary>
    /// 获取游戏基础组件。
    /// </summary>
    public static BaseComponent Base
    {
        get
        {
            if (_base == null)
            {
                _base = GameEntry.GetComponent<BaseComponent>();
            }

            return _base;
        }
    }

    private static BaseComponent _base;
}