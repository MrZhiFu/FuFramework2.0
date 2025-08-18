using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.GlobalConfig.Runtime
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