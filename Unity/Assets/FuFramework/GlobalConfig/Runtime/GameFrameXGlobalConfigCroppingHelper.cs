using UnityEngine;
using UnityEngine.Scripting;

namespace GameFrameX.GlobalConfig.Runtime
{
   
    public class GameFrameXGlobalConfigCroppingHelper : MonoBehaviour
    {
       
        private void Start()
        {
            _ = typeof(GlobalConfigComponent);
            _ = typeof(ResponseGameAppVersion);
            _ = typeof(ResponseGlobalInfo);
            _ = typeof(ResponseGameAssetPackageVersion);
        }
    }
}