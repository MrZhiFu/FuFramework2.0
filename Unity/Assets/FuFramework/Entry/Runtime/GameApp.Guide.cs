#if ENABLE_GAME_FRAME_X_GUIDE
using GameFrameX.Runtime;
using GameFrameX.Guide.Runtime;

public static partial class GameApp
{
    /// <summary>
    /// 获取引导组件。
    /// </summary>
    public static GuideComponent Guide
    {
        get
        {
            if (_guide == null)
            {
                _guide = GameEntry.GetComponent<GuideComponent>();
            }

            return _guide;
        }
    }

    private static GuideComponent _guide;
}
#endif