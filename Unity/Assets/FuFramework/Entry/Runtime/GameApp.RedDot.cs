#if ENABLE_GAME_FRAME_X_RED_DOT
using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entry.Runtime
{
    public static partial class GameApp
    {
        private static RedDotComponent _redDot;

        /// <summary>
        /// 获取红点组件。
        /// </summary>
        public static RedDotComponent RedDot
        {
            get
            {
                if (!_redDot) _redDot = GameEntry.GetComponent<RedDotComponent>();
                return _redDot;
            }
        }
    }
}
#endif