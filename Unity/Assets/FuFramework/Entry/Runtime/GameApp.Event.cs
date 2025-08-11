using FuFramework.Core.Runtime;
using GameFrameX.Event.Runtime;

public static partial class GameApp
{
    /// <summary>
    /// 获取事件组件。
    /// </summary>
    public static EventComponent Event
    {
        get
        {
            if (_event == null)
            {
                _event = GameEntry.GetComponent<EventComponent>();
            }

            return _event;
        }
    }

    private static EventComponent _event;
}