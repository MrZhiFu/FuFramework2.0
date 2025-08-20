using UnityEngine;
using FuFramework.Sound.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.GlobalSetting.Runtime
{
    /// <summary>
    /// 全局模块配置
    /// </summary>
    public class GlobalModuleSetting : MonoBehaviour
    {
        [Header("音频系统配置")]
        [SerializeField] private SoundSetting soundSetting;
    }
}