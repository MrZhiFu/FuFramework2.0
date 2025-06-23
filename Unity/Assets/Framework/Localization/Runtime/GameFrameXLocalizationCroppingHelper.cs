using UnityEngine;
using UnityEngine.Scripting;

namespace GameFrameX.Localization.Runtime
{
   
    public class GameFrameXLocalizationCroppingHelper : MonoBehaviour
    {
       
        private void Start()
        {
            _ = typeof(ILocalizationHelper);
            _ = typeof(ILocalizationManager);
            _ = typeof(Language);
            _ = typeof(LoadDictionaryFailureEventArgs);
            _ = typeof(LoadDictionarySuccessEventArgs);
            _ = typeof(LoadDictionaryUpdateEventArgs);
            _ = typeof(LocalizationManager);
            _ = typeof(DefaultLocalizationHelper);
            _ = typeof(LocalizationComponent);
            _ = typeof(LocalizationHelperBase);
        }
    }
}