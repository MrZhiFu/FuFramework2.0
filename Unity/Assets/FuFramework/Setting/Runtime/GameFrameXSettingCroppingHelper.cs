using UnityEngine;
using UnityEngine.Scripting;

namespace GameFrameX.Setting.Runtime
{
   
    public class GameFrameXSettingCroppingHelper : MonoBehaviour
    {
       
        private void Start()
        {
            _ = typeof(DefaultSetting);
            _ = typeof(DefaultSettingHelper);
            _ = typeof(DefaultSettingSerializer);
            _ = typeof(PlayerPrefsSettingHelper);
            _ = typeof(SettingComponent);
            _ = typeof(SettingHelperBase);
            _ = typeof(ISettingHelper);
            _ = typeof(ISettingManager);
            _ = typeof(SettingManager);
        }
    }
}