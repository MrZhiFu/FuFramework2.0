using UnityEngine;
using UnityEngine.Scripting;

namespace FuFramework.Timer.Runtime
{
   
    public class GameFrameXTimerCroppingHelper : MonoBehaviour
    {
       
        private void Start()
        {
            _ = typeof(TimerComponent);
            _ = typeof(ITimerManager);
            _ = typeof(TimerManager);
        }
    }
}