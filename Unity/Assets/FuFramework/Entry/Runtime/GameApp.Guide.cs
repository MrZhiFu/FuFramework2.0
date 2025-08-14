#if ENABLE_GAME_FRAME_X_GUIDE
using GameFrameX.Runtime;
using GameFrameX.Guide.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entry.Runtime
{
    public static partial class GameApp
    {
        private static GuideComponent _guide;

        /// <summary>
        /// 获取引导组件。
        /// </summary>
        public static GuideComponent Guide
        {
            get
            {
                if (!_guide) _guide = GameEntry.GetComponent<GuideComponent>();
                return _guide;
            }
        }
    }
}
#endif