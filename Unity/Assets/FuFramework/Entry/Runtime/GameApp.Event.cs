using FuFramework.Core.Runtime;
using GameFrameX.Event.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entry.Runtime
{
    public static partial class GameApp
    {
        private static EventComponent _event;

        /// <summary>
        /// 获取事件组件。
        /// </summary>
        public static EventComponent Event
        {
            get
            {
                if (!_event) _event = GameEntry.GetComponent<EventComponent>();
                return _event;
            }
        }
    }
}