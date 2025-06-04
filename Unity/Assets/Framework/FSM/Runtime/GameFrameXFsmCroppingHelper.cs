using UnityEngine;
using UnityEngine.Scripting;

namespace GameFrameX.Fsm.Runtime
{
    [Preserve]
    public class GameFrameXFsmCroppingHelper : MonoBehaviour
    {
        [Preserve]
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