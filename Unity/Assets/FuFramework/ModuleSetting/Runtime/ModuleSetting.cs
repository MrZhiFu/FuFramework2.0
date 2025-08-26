using FuFramework.Core.Runtime;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.ModuleSetting.Runtime
{
    /// <summary>
    /// 系统模块配置
    /// </summary>
    public class ModuleSetting : MonoSingleton<ModuleSetting>
    {
        [Header("音频系统配置")]
        [SerializeField] private SoundSetting m_SoundSetting;

        [Header("资源系统配置")]
        [SerializeField] private AssetSetting m_AssetSetting;

        /// <summary>
        /// 获取音频系统配置
        /// </summary>
        public SoundSetting SoundSetting => m_SoundSetting;
        
        /// <summary>
        /// 获取资源系统配置
        /// </summary>
        public AssetSetting AssetSetting => m_AssetSetting;
    }
}