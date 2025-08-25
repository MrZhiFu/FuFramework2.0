using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Sound.Runtime
{
   
    public class GameFrameXSoundCroppingHelper : MonoBehaviour
    {
       
        private void Start()
        {
            _ = typeof(SoundManager);
            _ = typeof(SoundParams);
            _ = typeof(SoundParams3D);
            _ = typeof(EPlaySoundErrorCode);
            _ = typeof(PlaySoundFailureEventArgs);
            _ = typeof(PlaySoundSuccessEventArgs);
        }
    }
}