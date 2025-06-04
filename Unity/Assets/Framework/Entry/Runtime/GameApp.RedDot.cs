#if ENABLE_GAME_FRAME_X_RED_DOT
using GameFrameX.Runtime;
using GameFrameX.RedDot.Runtime;

public static partial class GameApp
{
    /// <summary>
    /// 获取红点组件。
    /// </summary>
    public static RedDotComponent RedDot
    {
        get
        {
            if (_redDot == null)
            {
                _redDot = GameEntry.GetComponent<RedDotComponent>();
            }

            return _redDot;
        }
    }

    private static RedDotComponent _redDot;
}
#endif