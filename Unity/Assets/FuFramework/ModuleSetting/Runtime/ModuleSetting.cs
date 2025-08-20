using FuFramework.ModuleSetting.Runtime;
using UnityEngine;
using UnityEngine.Serialization;

// ReSharper disable once CheckNamespace
namespace FuFramework.GlobalSetting.Runtime
{
    /// <summary>
    /// 全局模块配置
    /// </summary>
    public class ModuleSetting : MonoBehaviour
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