using AOT.Framework.ModuleSetting.Runtime.Asset;
using AOT.Framework.ModuleSetting.Runtime.DataSave;
using UnityEngine;
using UnityEngine.Serialization;

// ReSharper disable once CheckNamespace
namespace AOT.Framework.ModuleSetting.Runtime
{
    /// <summary>
    /// 系统模块配置。
    /// 功能：
    /// 1. 管理游戏基本设置信息，包括游戏帧率，游戏速度，是否允许后台运行，是否禁止休眠等。
    /// 2. 管理游戏基础模块的配置信息，包括音频模块设置，资源管理模块设置，实体模块设置，本地数据存储模块设置，红点模块模块设置等。
    /// 3. 提供游戏暂停，恢复，重置正常速度等功能。
    ///
    /// 注意：
    /// 1. 该类为单例类，请不要在代码中创建多个实例。
    /// 2. 该类需要挂载到首个初始化场景的 GameObject 上，否则其他模块无法正确初始化。
    /// </summary>
    public class ModuleSetting : MonoBehaviour
    {
        /// <summary>
        /// 单例实例。由 Awake 赋值，依赖 Launcher 的 DontDestroyOnLoad 保证跨场景存活。
        /// </summary>
        public static ModuleSetting Instance { get; private set; }
        /// <summary>
        /// 游戏帧率。
        /// </summary>
        [SerializeField] private int m_FrameRate = 30;

        /// <summary>
        /// 游戏速度。
        /// </summary>
        [SerializeField] private float m_GameSpeed = 1f;

        /// <summary>
        /// 是否允许后台运行。
        /// </summary>
        [SerializeField] private bool m_RunInBackground = true;

        /// <summary>
        /// 是否禁止休眠。
        /// </summary>
        [SerializeField] private bool m_NeverSleep = true;

        /// <summary>
        /// 是否开启引导。
        /// </summary>
        [SerializeField] private bool m_OpenGuide = true;


        [Header("资源系统配置")]
        [SerializeField] private AssetSetting m_AssetSetting;

        [Header("本地数据存储系统配置")]
        [SerializeField] private StorageSetting m_StorageSetting;


        /// <summary>
        /// 获取资源系统配置
        /// </summary>
        public AssetSetting AssetSetting => m_AssetSetting;

        /// <summary>
        /// 获取本地存储系统配置
        /// </summary>
        public StorageSetting StorageSetting => m_StorageSetting;


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
                m_NeverSleep        = value;
                Screen.sleepTimeout = value ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
            }
        }

        /// <summary>
        /// 获取或设置是否开启引导。
        /// </summary>
        public bool OpenGuide
        {
            get => m_OpenGuide;
            set => m_OpenGuide = value;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            // DontDestroyOnLoad 由挂载在同一 GameObject 上的 Launcher 统一处理

            // 设置游戏速度，屏幕休眠，帧率，后台运行等
            Time.timeScale              = m_GameSpeed;
            Screen.sleepTimeout         = m_NeverSleep ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
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
            GameSpeed              = 0f;
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
        /// 重置为正常游戏速度(1倍速)。
        /// </summary>
        public void ResetNormalGameSpeed()
        {
            if (IsNormalGameSpeed) return;
            GameSpeed = 1f;
        }
    }
}