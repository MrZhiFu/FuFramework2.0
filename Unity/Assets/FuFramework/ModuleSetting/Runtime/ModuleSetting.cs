using UnityEngine;
using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.ModuleSetting.Runtime
{
    /// <summary>
    /// 系统模块配置
    /// </summary>
    public class ModuleSetting : MonoSingleton<ModuleSetting>
    {
        /// 游戏帧率
        [SerializeField] private int m_FrameRate = 30;

        /// 游戏速度。
        [SerializeField] private float m_GameSpeed = 1f;

        /// 是否允许后台运行。
        [SerializeField] private bool m_RunInBackground = true;

        /// 是否禁止休眠。
        [SerializeField] private bool m_NeverSleep = true;

        [Header("音频系统配置")]
        [SerializeField] private SoundSetting m_SoundSetting;

        [Header("资源系统配置")]
        [SerializeField] private AssetSetting m_AssetSetting;
        
        [Header("实体系统配置")]
        [SerializeField] private EntitySetting m_EntitySetting;
        
        [Header("本地数据存储系统配置")]
        [SerializeField] private DataSaveSetting m_DataSaveSetting;

        /// <summary>
        /// 获取音频系统配置
        /// </summary>
        public SoundSetting SoundSetting => m_SoundSetting;
        
        /// <summary>
        /// 获取资源系统配置
        /// </summary>
        public AssetSetting AssetSetting => m_AssetSetting;
        
        /// <summary>
        /// 获取实体系统配置
        /// </summary>
        public EntitySetting EntitySetting => m_EntitySetting;
        
        /// <summary>
        /// 获取本地存储系统配置
        /// </summary>
        public DataSaveSetting DataSaveSetting => m_DataSaveSetting;
        

        /// 游戏暂停之前的速度
        private float m_GameSpeedBeforePause = 1f;
        
        /// <summary>
        /// 获取或设置游戏帧率。
        /// </summary>
        public int FrameRate
        {
            get => m_FrameRate;
            set => Application.targetFrameRate = m_FrameRate = value;
        }

        /// <summary>
        /// 获取或设置游戏速度。
        /// </summary>
        public float GameSpeed
        {
            get => m_GameSpeed;
            set => Time.timeScale = m_GameSpeed = value >= 0f ? value : 0f;
        }

        /// <summary>
        /// 获取游戏是否暂停。
        /// </summary>
        public bool IsGamePaused => m_GameSpeed <= 0f;

        /// <summary>
        /// 获取是否正常游戏速度。
        /// </summary>
        public bool IsNormalGameSpeed => Mathf.Approximately(m_GameSpeed, 1f);

        /// <summary>
        /// 获取或设置是否允许后台运行。
        /// </summary>
        public bool RunInBackground
        {
            get => m_RunInBackground;
            set => Application.runInBackground = m_RunInBackground = value;
        }

        /// <summary>
        /// 获取或设置是否禁止休眠。
        /// </summary>
        public bool NeverSleep
        {
            get => m_NeverSleep;
            set
            {
                m_NeverSleep = value;
                Screen.sleepTimeout = value ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
            }
        }

        /// <summary>
        /// 初始化。
        /// </summary>
        protected override void Init()
        {
            FuLog.Info($"游戏版本号: {Application.version}, Unity版本号: {Application.unityVersion}");

            // 设置游戏速度，屏幕休眠，帧率，后台运行等
            Time.timeScale = m_GameSpeed;
            Screen.sleepTimeout = m_NeverSleep ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
            Application.targetFrameRate = m_FrameRate;
            Application.runInBackground = m_RunInBackground;
        }

        /// <summary>
        /// 暂停游戏。
        /// </summary>
        public void PauseGame()
        {
            if (IsGamePaused) return;
            m_GameSpeedBeforePause = GameSpeed;
            GameSpeed = 0f;
        }

        /// <summary>
        /// 恢复游戏。
        /// </summary>
        public void ResumeGame()
        {
            if (!IsGamePaused) return;
            GameSpeed = m_GameSpeedBeforePause;
        }

        /// <summary>
        /// 重置为正常游戏速度。
        /// </summary>
        public void ResetNormalGameSpeed()
        {
            if (IsNormalGameSpeed) return;
            GameSpeed = 1f;
        }
    }
}