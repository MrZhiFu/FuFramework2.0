using FuFramework.Core.Runtime;
using FuFramework.Timer.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entry.Runtime
{
    public static partial class GameApp
    {
        private static TimerComponent _timer;

        /// <summary>
        /// 获取定时器组件。
        /// </summary>
        public static TimerComponent Timer
        {
            get
            {
                if (!_timer) _timer = GameEntry.GetComponent<TimerComponent>();
                return _timer;
            }
        }
    }
}