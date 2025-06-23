using UnityEngine;
using UnityEngine.Scripting;

namespace GameFrameX.Mono.Runtime
{
   
    public class GameFrameXMonoCroppingHelper : MonoBehaviour
    {
       
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