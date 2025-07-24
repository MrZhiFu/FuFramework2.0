using UnityEngine;
using UnityEngine.Scripting;

namespace GameFrameX.Fsm.Runtime
{
   
    public class GameFrameXFsmCroppingHelper : MonoBehaviour
    {
       
        private void Start()
        {
            _ = typeof(IFsmManager);
            _ = typeof(IFsm<>);
            _ = typeof(FsmState<>);
            _ = typeof(FsmBase);
            _ = typeof(FsmManager);
            _ = typeof(FsmComponent);
        }
    }
}