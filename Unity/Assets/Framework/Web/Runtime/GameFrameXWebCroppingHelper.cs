using UnityEngine;
using UnityEngine.Scripting;

namespace GameFrameX.Web.Runtime
{
    [Preserve]
    public class GameFrameXWebCroppingHelper : MonoBehaviour
    {
        [Preserve]
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