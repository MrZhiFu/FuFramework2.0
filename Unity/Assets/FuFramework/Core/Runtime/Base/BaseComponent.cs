using System;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 基础组件。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/Base")]
    [DefaultExecutionOrder(-500)]
    public sealed class BaseComponent : FuComponent
    {
        /// 屏幕每英寸点数 默认为windows dpi
        private const int DefaultDpi = 96;

        /// 游戏暂停之前的速度
        private float m_GameSpeedBeforePause = 1f;


        /// 默认文本辅助器全名称
        [SerializeField] private string m_TextHelperTypeName = "FuFramework.Core.Runtime.DefaultTextHelper";

        /// 默认版本号辅助器全名称
        [SerializeField] private string m_VersionHelperTypeName = "FuFramework.Core.Runtime.DefaultVersionHelper";

        /// 默认日志辅助器全名称
        [SerializeField] private string m_LogHelperTypeName = "FuFramework.Core.Runtime.DefaultLogHelper";

        /// 默认压缩辅助器全名称
        [SerializeField] private string m_CompressionHelperTypeName = "FuFramework.Core.Runtime.DefaultCompressionHelper";

        /// 默认Json辅助器全名称
        [SerializeField] private string m_JsonHelperTypeName = "FuFramework.Core.Runtime.DefaultJsonHelper";


        /// 游戏帧率
        [SerializeField] private int m_FrameRate = 30;

        /// 游戏速度。
        [SerializeField] private float m_GameSpeed = 1f;

        /// 是否允许后台运行。
        [SerializeField] private bool m_RunInBackground = true;

        /// 是否禁止休眠。
        [SerializeField] private bool m_NeverSleep = true;


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
        /// 游戏框架组件初始化。
        /// </summary>
        protected override void Awake()
        {
            IsAutoRegister = false;
            base.Awake();

            DontDestroyOnLoad(this);

            // 初始化相关辅助器
            InitTextHelper();
            InitVersionHelper();
            InitLogHelper();
            InitCompressionHelper();
            InitJsonHelper();

            Log.Info("游戏版本号: {0}, Unity版本号: {1}", Version.GameVersion, Application.unityVersion);

            // 设置工具类Converter的屏幕dpi, 方便进行屏幕像素和厘米与英寸的转换方法实现
            Utility.Converter.ScreenDpi = Screen.dpi;
            if (Utility.Converter.ScreenDpi <= 0)
                Utility.Converter.ScreenDpi = DefaultDpi;

            // 设置游戏速度，屏幕休眠，帧率，后台运行等
            Time.timeScale              =  m_GameSpeed;
            Screen.sleepTimeout         =  m_NeverSleep ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
            Application.targetFrameRate =  m_FrameRate;
            Application.runInBackground =  m_RunInBackground;
            Application.lowMemory       += OnLowMemory;
        }

        /// <summary>
        /// 帧更新，驱动框架入口GFGameEntry更新
        /// </summary>
        private void Update()
        {
            FuEntry.Update(Time.deltaTime, Time.unscaledDeltaTime);
        }

        /// <summary>
        /// 退出游戏。
        /// </summary>
        private void OnApplicationQuit()
        {
            Application.lowMemory -= OnLowMemory;
            StopAllCoroutines();
        }

        /// <summary>
        /// 销毁。
        /// </summary>
        private void OnDestroy() => FuEntry.Shutdown();

        /// <summary>
        /// 低内存回调
        /// </summary>
        private void OnLowMemory()
        {
            Log.Info("低内存警告, 释放对象池资源...");

            // 释放对象池中所有未使用的资源
            var objectPoolComponent = GameEntry.GetComponent<ObjectPoolComponent>();
            if (objectPoolComponent != null)
                objectPoolComponent.ReleaseAllUnused();
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
        /// 重置为正常游戏速度。
        /// </summary>
        public void ResetNormalGameSpeed()
        {
            if (IsNormalGameSpeed) return;
            GameSpeed = 1f;
        }

        /// <summary>
        /// 关闭游戏框架组件。
        /// </summary>
        internal void Shutdown() => Destroy(gameObject);


        /// <summary>
        /// 初始化文本辅助器
        /// </summary>
        private void InitTextHelper()
        {
            if (string.IsNullOrEmpty(m_TextHelperTypeName)) return;

            var textHelperType = Utility.Assembly.GetType(m_TextHelperTypeName);
            if (textHelperType == null)
            {
                Log.Error("找不到文本辅助器类型'{0}'.", m_TextHelperTypeName);
                return;
            }

            var textHelper = (Utility.Text.ITextHelper)Activator.CreateInstance(textHelperType);
            if (textHelper == null)
            {
                Log.Error("创建文本辅助器实例'{0}'失败.", m_TextHelperTypeName);
                return;
            }

            Utility.Text.SetTextHelper(textHelper);
        }

        /// <summary>
        /// 初始化版本辅助器
        /// </summary>
        private void InitVersionHelper()
        {
            if (string.IsNullOrEmpty(m_VersionHelperTypeName)) return;

            var versionHelperType = Utility.Assembly.GetType(m_VersionHelperTypeName);
            if (versionHelperType == null)
                throw new FuException(Utility.Text.Format("不能找到版本辅助器类型'{0}'.", m_VersionHelperTypeName));

            var versionHelper = (Version.IVersionHelper)Activator.CreateInstance(versionHelperType);
            if (versionHelper == null)
                throw new FuException(Utility.Text.Format("创建版本辅助器实例'{0}'失败.", m_VersionHelperTypeName));

            Version.SetVersionHelper(versionHelper);
        }

        /// <summary>
        /// 初始化日志辅助器
        /// </summary>
        private void InitLogHelper()
        {
            if (string.IsNullOrEmpty(m_LogHelperTypeName)) return;

            var logHelperType = Utility.Assembly.GetType(m_LogHelperTypeName);
            if (logHelperType == null)
                throw new FuException(Utility.Text.Format("不能找到日志辅助器类型'{0}'.", m_LogHelperTypeName));

            var logHelper = (FuLog.ILogHelper)Activator.CreateInstance(logHelperType);
            if (logHelper == null)
                throw new FuException(Utility.Text.Format("创建日志辅助器实例'{0}'失败.", m_LogHelperTypeName));

            FuLog.SetLogHelper(logHelper);
        }

        /// <summary>
        /// 初始化压缩辅助器
        /// </summary>
        private void InitCompressionHelper()
        {
            if (string.IsNullOrEmpty(m_CompressionHelperTypeName)) return;

            var compressionHelperType = Utility.Assembly.GetType(m_CompressionHelperTypeName);
            if (compressionHelperType == null)
            {
                Log.Error("不能找到压缩辅助器类型'{0}'.", m_CompressionHelperTypeName);
                return;
            }

            var compressionHelper = (Utility.Compression.ICompressionHelper)Activator.CreateInstance(compressionHelperType);
            if (compressionHelper == null)
            {
                Log.Error("创建压缩辅助器实例'{0}'失败.", m_CompressionHelperTypeName);
                return;
            }

            Utility.Compression.SetCompressionHelper(compressionHelper);
        }

        /// <summary>
        /// 初始化Json辅助器
        /// </summary>
        private void InitJsonHelper()
        {
            if (string.IsNullOrEmpty(m_JsonHelperTypeName)) return;

            var jsonHelperType = Utility.Assembly.GetType(m_JsonHelperTypeName);
            if (jsonHelperType == null)
            {
                Log.Error("不能找到Json辅助器类型'{0}'.", m_JsonHelperTypeName);
                return;
            }

            var jsonHelper = (Utility.Json.IJsonHelper)Activator.CreateInstance(jsonHelperType);
            if (jsonHelper == null)
            {
                Log.Error("创建Json辅助器实例'{0}'失败.", m_JsonHelperTypeName);
                return;
            }

            Utility.Json.SetJsonHelper(jsonHelper);
        }
    }
}