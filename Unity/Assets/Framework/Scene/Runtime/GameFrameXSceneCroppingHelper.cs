using UnityEngine;
using UnityEngine.Scripting;

namespace GameFrameX.Scene.Runtime
{
    [Preserve]
    public class GameFrameXSceneCroppingHelper : MonoBehaviour
    {
        [Preserve]
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