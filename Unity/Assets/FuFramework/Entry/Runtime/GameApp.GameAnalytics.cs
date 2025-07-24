#if ENABLE_GAME_FRAME_X_GAME_ANALYTICS
using GameFrameX.GameAnalytics.Runtime;
using GameFrameX.Runtime;

public static partial class GameApp
{
    /// <summary>
    /// 获取游戏分析组件。
    /// </summary>
    public static GameAnalyticsComponent GameAnalytics
    {
        get
        {
            if (_gameAnalytics == null)
            {
                _gameAnalytics = GameEntry.GetComponent<GameAnalyticsComponent>();
            }

            return _gameAnalytics;
        }
    }

    private static GameAnalyticsComponent _gameAnalytics;
}
#endif