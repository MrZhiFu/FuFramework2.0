using GameFrameX.Runtime;
using GameFrameX.Procedure.Runtime;

public static partial class GameApp
{
    /// <summary>
    /// 获取流程组件。
    /// </summary>
    public static ProcedureComponent Procedure
    {
        get
        {
            if (_procedure == null)
            {
                _procedure = GameEntry.GetComponent<ProcedureComponent>();
            }

            return _procedure;
        }
    }

    private static ProcedureComponent _procedure;
}