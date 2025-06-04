using UnityEngine;
using UnityEngine.Scripting;

namespace GameFrameX.Timer.Runtime
{
    [Preserve]
    public class GameFrameXTimerCroppingHelper : MonoBehaviour
    {
        [Preserve]
        private void Start()
        {
            _ = typeof(TimerComponent);
            _ = typeof(ITimerManager);
            _ = typeof(TimerManager);
        }
    }
}