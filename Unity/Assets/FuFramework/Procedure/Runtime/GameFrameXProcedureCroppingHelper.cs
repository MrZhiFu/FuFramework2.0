using UnityEngine;
using UnityEngine.Scripting;

namespace GameFrameX.Procedure.Runtime
{
   
    public class GameFrameXProcedureCroppingHelper : MonoBehaviour
    {
       
        private void Start()
        {
            _ = typeof(IProcedureManager);
            _ = typeof(ProcedureBase);
            _ = typeof(ProcedureManager);
            _ = typeof(ProcedureComponent);
        }
    }
}