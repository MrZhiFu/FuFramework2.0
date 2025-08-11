using FuFramework.Core.Runtime;
using GameFrameX.Fsm.Runtime;

public static partial class GameApp
{
    /// <summary>
    /// 获取有限状态机组件。
    /// </summary>
    public static FsmComponent Fsm
    {
        get
        {
            if (_fsm == null)
            {
                _fsm = GameEntry.GetComponent<FsmComponent>();
            }

            return _fsm;
        }
    }

    private static FsmComponent _fsm;
}