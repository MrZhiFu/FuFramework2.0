using UnityEngine.Audio;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Sound.Runtime
{
    /// <summary>
    /// 默认声音组辅助器。
    /// </summary>
    public class DefaultSoundGroupHelper : MonoBehaviour
    {
        /// <summary>
        /// 声音组辅助器所在的混音组。
        /// </summary>
        [SerializeField] private AudioMixerGroup m_AudioMixerGroup;

        /// <summary>
        /// 获取或设置声音组辅助器所在的混音组。
        /// </summary>
        public AudioMixerGroup AudioMixerGroup
        {
            get => m_AudioMixerGroup;
            set => m_AudioMixerGroup = value;
        }
    }
}