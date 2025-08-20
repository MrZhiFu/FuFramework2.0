using FuFramework.Core.Runtime;
using UnityEngine;
using UnityEngine.Serialization;

namespace FuFramework.ModuleSetting.Runtime
{
    /// <summary>
    /// 系统模块配置
    /// </summary>
    public class ModuleSetting : MonoSingleton<ModuleSetting>
    {
        [FormerlySerializedAs("soundSetting")]
        [Header("音频系统配置")]
        [SerializeField] private SoundSetting m_SoundSetting;

        /// <summary>
        /// 获取音频系统配置
        /// </summary>
        public SoundSetting SoundSetting => m_SoundSetting;
    }
}