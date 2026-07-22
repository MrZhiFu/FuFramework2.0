using UnityEngine;
using YooAsset;

// ReSharper disable once CheckNamespace
namespace AOT.Framework.ModuleSetting.Runtime
{
    /// <summary>
    /// 游戏全局配置。
    /// 功能：
    ///     1. 管理游戏基本设置，包括帧率、游戏速度、后台运行、禁止休眠等。
    ///     2. 管理资源系统配置（YooAsset 运行模式、下载参数、CDN 地址等）。
    ///     3. 管理本地数据存储配置（自动保存、加密等）。
    ///     4. 提供游戏暂停、恢复、重置正常速度等功能。
    ///
    /// 注意：
    ///     1. 该类为单例，挂载到首个初始化场景的 GameObject 上，并且 DontDestroyOnLoad。
    /// </summary>
    public class GameSetting : MonoBehaviour
    {
        /// <summary>
        /// 单例实例。
        /// </summary>
        public static GameSetting Instance { get; private set; }

        #region 游戏基本设置

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

        #endregion

        #region 资源系统配置

        /// <summary>
        /// 资源运行模式。
        /// </summary>
        [SerializeField] private EPlayMode m_PlayMode = EPlayMode.EditorSimulateMode;

        /// <summary>
        /// 默认资源包名称。
        /// </summary>
        [SerializeField] private string m_DefaultPackageName = "DefaultPackage";

        /// <summary>
        /// 资源下载最大并发数量。
        /// </summary>
        [SerializeField] private int m_DownloadingMaxNum = 10;

        /// <summary>
        /// 资源下载失败重试次数。
        /// </summary>
        [SerializeField] private int m_FailedTryAgainNum = 3;

        /// <summary>
        /// YooAsset 异步系统每帧最大时间切片（毫秒）。
        /// </summary>
        [SerializeField] private int m_AsyncSystemMaxSlicePerFrame = 30;

        /// <summary>
        /// 资源 CDN 根地址。
        /// </summary>
        [SerializeField] private string m_ResCdnRootURL = "http://localhost:8080/CDN/";

        #endregion

        #region 本地数据存储系统配置

        /// <summary>
        /// 是否启用自动保存。
        /// </summary>
        [SerializeField] private bool m_EnableAutoSave = true;

        /// <summary>
        /// 自动保存间隔（秒）。
        /// </summary>
        [SerializeField] private float m_AutoSaveInterval = 300f;

        /// <summary>
        /// 是否启用加密。
        /// </summary>
        [SerializeField] private bool m_EnableEncrypt = false;

        /// <summary>
        /// 加密密钥。
        /// </summary>
        [SerializeField] private string m_EncryptKey = "FuFrameworkStorageKey";

        #endregion

        /// <summary>
        /// 游戏暂停之前的速度。
        /// </summary>
        private float m_GameSpeedBeforePause = 1f;

        #region 游戏基本设置属性

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

        #endregion

        #region 资源系统配置属性

        /// <summary>
        /// 资源运行模式。
        /// </summary>
        public EPlayMode PlayMode => m_PlayMode;

        /// <summary>
        /// 默认资源包名称。
        /// </summary>
        public string DefaultPackageName => m_DefaultPackageName;

        /// <summary>
        /// 资源下载最大并发数量。
        /// </summary>
        public int DownloadingMaxNum => m_DownloadingMaxNum;

        /// <summary>
        /// 资源下载失败重试次数。
        /// </summary>
        public int FailedTryAgainNum => m_FailedTryAgainNum;

        /// <summary>
        /// YooAsset 异步系统每帧最大时间切片（毫秒）。
        /// </summary>
        public int AsyncSystemMaxSlicePerFrame => m_AsyncSystemMaxSlicePerFrame;

        /// <summary>
        /// 资源 CDN 根地址。
        /// </summary>
        public string ResCdnRootRootURL => m_ResCdnRootURL;

        #endregion

        #region 本地数据存储系统配置属性

        /// <summary>
        /// 是否启用自动保存。
        /// </summary>
        public bool EnableAutoSave => m_EnableAutoSave;

        /// <summary>
        /// 自动保存间隔（秒）。
        /// </summary>
        public float AutoSaveInterval => m_AutoSaveInterval;

        /// <summary>
        /// 是否启用加密。
        /// </summary>
        public bool EnableEncrypt => m_EnableEncrypt;

        /// <summary>
        /// 加密密钥。
        /// </summary>
        public string EncryptKey => m_EncryptKey;

        #endregion

        #region 生命周期

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Time.timeScale              = m_GameSpeed;
            Screen.sleepTimeout         = m_NeverSleep ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
            Application.targetFrameRate = m_FrameRate;
            Application.runInBackground = m_RunInBackground;
        }

        #endregion

        #region 游戏速度控制

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
        /// 重置为正常游戏速度。
        /// </summary>
        public void ResetNormalGameSpeed()
        {
            if (IsNormalGameSpeed) return;
            GameSpeed = 1f;
        }

        #endregion
    }
}