using UnityEngine;
using UnityEngine.Scripting;

namespace GameFrameX.Asset.Runtime
{
   
    public class GameFrameXAssetCroppingHelper : MonoBehaviour
    {
       
        private void Start()
        {
            _ = typeof(AssetManager);
            _ = typeof(Constant);
            _ = typeof(IAssetManager);
            _ = typeof(AssetComponent);
        }
    }
}