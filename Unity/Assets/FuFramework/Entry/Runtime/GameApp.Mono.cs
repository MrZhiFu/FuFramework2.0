using GameFrameX.Runtime;
using GameFrameX.Mono.Runtime;

public static partial class GameApp
{
    /// <summary>
    /// 获取Mono组件。
    /// </summary>
    public static MonoComponent Mono
    {
        get
        {
            if (_mono == null)
            {
                _mono = GameEntry.GetComponent<MonoComponent>();
            }

            return _mono;
        }
    }

    private static MonoComponent _mono;
}