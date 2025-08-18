using UnityEngine;
using UnityEngine.Scripting;

namespace FuFramework.Scene.Runtime
{
   
    public class GameFrameXSceneCroppingHelper : MonoBehaviour
    {
       
        private void Start()
        {
            _ = typeof(SceneComponent);
            _ = typeof(ActiveSceneChangedEventArgs);
            _ = typeof(GameSceneManager);
            _ = typeof(IGameSceneManager);
            _ = typeof(LoadSceneFailureEventArgs);
            _ = typeof(LoadSceneSuccessEventArgs);
            _ = typeof(LoadSceneUpdateEventArgs);
            _ = typeof(UnloadSceneFailureEventArgs);
            _ = typeof(UnloadSceneSuccessEventArgs);
        }
    }
}