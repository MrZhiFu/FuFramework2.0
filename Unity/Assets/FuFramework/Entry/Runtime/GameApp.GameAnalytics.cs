#if ENABLE_GAME_FRAME_X_GAME_ANALYTICS
using GameFrameX.GameAnalytics.Runtime;
using GameFrameX.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entry.Runtime
{
    public static partial class GameApp
    {
        private static GameAnalyticsComponent _gameAnalytics;

        /// <summary>
        /// 获取游戏分析组件。
        /// </summary>
        public static GameAnalyticsComponent GameAnalytics
        {
            get
            {
                if (!_gameAnalytics) _gameAnalytics = GameEntry.GetComponent<GameAnalyticsComponent>();
                return _gameAnalytics;
            }
        }
    }
}
#endif