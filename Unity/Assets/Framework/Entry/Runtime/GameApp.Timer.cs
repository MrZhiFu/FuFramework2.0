using GameFrameX.Runtime;
using GameFrameX.Timer.Runtime;

public static partial class GameApp
{
    /// <summary>
    /// 获取定时器组件。
    /// </summary>
    public static TimerComponent Timer
    {
        get
        {
            if (_timer == null)
            {
                _timer = GameEntry.GetComponent<TimerComponent>();
            }

            return _timer;
        }
    }

    private static TimerComponent _timer;
}