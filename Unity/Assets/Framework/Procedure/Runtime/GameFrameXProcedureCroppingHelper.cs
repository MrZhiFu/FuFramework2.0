using UnityEngine;
using UnityEngine.Scripting;

namespace GameFrameX.Procedure.Runtime
{
    [Preserve]
    public class GameFrameXProcedureCroppingHelper : MonoBehaviour
    {
        [Preserve]
        private void Start()
        {
            _ = typeof(IProcedureManager);
            _ = typeof(ProcedureBase);
            _ = typeof(ProcedureManager);
            _ = typeof(ProcedureComponent);
        }
    }
}