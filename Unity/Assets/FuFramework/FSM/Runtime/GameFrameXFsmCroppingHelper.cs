using UnityEngine;
using UnityEngine.Scripting;

namespace FuFramework.Fsm.Runtime
{
   
    public class GameFrameXFsmCroppingHelper : MonoBehaviour
    {
       
        private void Start()
        {
            _ = typeof(IFsmManager);
            _ = typeof(IFsm<>);
            _ = typeof(FsmStateBase<>);
            _ = typeof(FsmBase);
            _ = typeof(FsmManager);
            _ = typeof(FsmComponent);
        }
    }
}