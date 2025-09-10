using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Localization.Runtime
{
   
    public class GameFrameXLocalizationCroppingHelper : MonoBehaviour
    {
       
        private void Start()
        {
            _ = typeof(ELanguage);
            _ = typeof(LocalizationManager);
        }
    }
}