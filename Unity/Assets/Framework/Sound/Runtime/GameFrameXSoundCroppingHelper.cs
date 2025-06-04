using UnityEngine;
using UnityEngine.Scripting;

namespace GameFrameX.Sound.Runtime
{
    [Preserve]
    public class GameFrameXSoundCroppingHelper : MonoBehaviour
    {
        [Preserve]
        private void Start()
        {
            _ = typeof(DefaultSoundAgentHelper);
            _ = typeof(DefaultSoundGroupHelper);
            _ = typeof(DefaultSoundHelper);
            _ = typeof(PlaySoundInfo);
            _ = typeof(SoundAgentHelperBase);
            _ = typeof(SoundComponent);
            _ = typeof(SoundGroupHelperBase);
            _ = typeof(SoundHelperBase);
            _ = typeof(Constant);
            _ = typeof(ISoundAgent);
            _ = typeof(ISoundAgentHelper);
            _ = typeof(ISoundGroup);
            _ = typeof(ISoundGroupHelper);
            _ = typeof(ISoundHelper);
            _ = typeof(ISoundManager);
            _ = typeof(PlaySoundErrorCode);
            _ = typeof(PlaySoundFailureEventArgs);
            _ = typeof(PlaySoundParams);
            _ = typeof(PlaySoundSuccessEventArgs);
            _ = typeof(PlaySoundUpdateEventArgs);
            _ = typeof(ResetSoundAgentEventArgs);
            _ = typeof(SoundManager);
        }
    }
}