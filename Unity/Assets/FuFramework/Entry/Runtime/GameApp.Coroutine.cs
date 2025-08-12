using FuFramework.Core.Runtime;
using FuFramework.Coroutine.Runtime;

public static partial class GameApp
{
    /// <summary>
    /// 获取协程组件。
    /// </summary>
    public static CoroutineComponent Coroutine
    {
        get
        {
            if (_coroutine == null)
            {
                _coroutine = GameEntry.GetComponent<CoroutineComponent>();
            }

            return _coroutine;
        }
    }

    private static CoroutineComponent _coroutine;
}