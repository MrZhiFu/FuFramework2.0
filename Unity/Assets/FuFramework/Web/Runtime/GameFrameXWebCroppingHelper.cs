using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Web.Runtime
{
    public class GameFrameXWebCroppingHelper : MonoBehaviour
    {
        private void Start()
        {
            _ = typeof(WebComponent);
            _ = typeof(IWebManager);
            _ = typeof(WebManager);
            _ = typeof(WebStringResult);
            _ = typeof(WebBufferResult);
        }
    }
}