using UnityEngine;
using UnityEngine.Scripting;

namespace GameFrameX.Mono.Runtime
{
    [Preserve]
    public class GameFrameXMonoCroppingHelper : MonoBehaviour
    {
        [Preserve]
        private void Start()
        {
            _ = typeof(IMonoManager);
            _ = typeof(MonoManager);
            _ = typeof(OnApplicationFocusChangedEventArgs);
            _ = typeof(OnApplicationPauseChangedEventArgs);
            _ = typeof(MonoComponent);
        }
    }
}