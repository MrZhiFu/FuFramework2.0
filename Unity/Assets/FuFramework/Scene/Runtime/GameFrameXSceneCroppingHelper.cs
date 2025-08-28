using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Scene.Runtime
{
    public class GameFrameXSceneCroppingHelper : MonoBehaviour
    {
        private void Start()
        {
            _ = typeof(ActiveSceneChangedEventArgs);
            _ = typeof(GameSceneManager);
            _ = typeof(LoadSceneFailureEventArgs);
            _ = typeof(LoadSceneSuccessEventArgs);
            _ = typeof(LoadSceneUpdateEventArgs);
            _ = typeof(UnloadSceneFailureEventArgs);
            _ = typeof(UnloadSceneSuccessEventArgs);
        }
    }
}