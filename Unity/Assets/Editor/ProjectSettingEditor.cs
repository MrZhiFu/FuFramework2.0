using System.IO;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Unity.Editor
{
    /// <summary>
    /// 项目 ProjectSettingEditor，用于编辑器启动时自动统一项目设置。
    ///
    /// 设计意图：
    /// 虽然 ProjectSettings.asset 在版本控制中，但实际开发中经常有人在本地临时调整设置
    /// （如切换平台、改 Bundle ID 调试等），事后容易忘记还原，导致提交时带入了非预期的改动。
    /// 使用 [InitializeOnLoadMethod] 在每次编辑器启动时强制覆盖这些关键设置，作为一道安全网，
    /// 确保核心构建配置不会被意外修改。
    ///
    /// 此外还负责自动生成必要的 Bundle 相关目录，YooAsset构建管线依赖这些目录。
    ///
    /// 注意：这意味着如果你手动修改了以下任何设置，下次打开编辑器时会被重置。这是有意为之的约束。
    /// </summary>
    internal static class ProjectSettingEditor
    {
        /// <summary>
        /// 编辑器启动时自动执行，统一关键 PlayerSettings 并创建必需的 Bundle 目录
        /// </summary>
        [InitializeOnLoadMethod]
        private static void Start()
        {
            // ========== 应用标识 ==========
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.fustudio.frameworkDemo"); // Android 包名
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS,     "com.fustudio.frameworkDemo"); // iOS Bundle ID

            // ========== 启动画面 ==========
            PlayerSettings.SplashScreen.show          = false; // 关闭 Splash Screen（使用 FairyGUI 自定义启动界面）
            PlayerSettings.SplashScreen.showUnityLogo = false; // 隐藏 Unity 默认 Logo

            // ========== 项目信息 ==========
            PlayerSettings.productName = "FuFrameworkDemo"; // 应用名称
            PlayerSettings.companyName = "FuStudio";        // 公司名称

            // ========== 屏幕方向 ==========
            PlayerSettings.defaultInterfaceOrientation           = UIOrientation.AutoRotation; // 默认方向设为自动旋转
            PlayerSettings.allowedAutorotateToPortrait           = false;                      // 禁止竖屏
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;                      // 禁止倒置竖屏
            PlayerSettings.allowedAutorotateToLandscapeLeft      = true;                       // 允许左横屏
            PlayerSettings.allowedAutorotateToLandscapeRight     = true;                       // 允许右横屏

            // ========== 音频 ==========
            PlayerSettings.muteOtherAudioSources = false; // 不静音其他应用音频（允许后台音乐播放）

            // ========== 窗口与全屏 ==========
            PlayerSettings.statusBarHidden = true;                               // 隐藏系统状态栏
            PlayerSettings.fullScreenMode  = FullScreenMode.ExclusiveFullScreen; // 独占全屏模式（避免窗口化导致的输入延迟）

#if UNITY_ANDROID
            // ========== Android 平台设置 ==========
            PlayerSettings.Android.renderOutsideSafeArea = true;                                                 // 允许渲染到安全区之外（刘海屏适配）
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);// 脚本后端 IL2CPP
            PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Android, ApiCompatibilityLevel.NET_4_6);   // API 兼容级别 .NET 4.x
            if (EditorUserBuildSettings.development)
            {
                PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.Android, Il2CppCompilerConfiguration.Debug); // Development 模式 Debug 编译（便于排查）
            }
            else
            {
                PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.Android, Il2CppCompilerConfiguration.Release); // Release 模式 Release 编译（性能最优）
            }

            PlayerSettings.Android.targetArchitectures    = AndroidArchitecture.ARM64; // 目标 CPU 架构仅 ARM64
            PlayerSettings.Android.androidTVCompatibility = false;                     // 关闭 Android TV 兼容模式
#endif

#if UNITY_IOS
            // ========== iOS 平台设置 ==========
            PlayerSettings.iOS.appleDeveloperTeamID = "XXXXXX";      // Apple Developer Team ID（需替换为实际 ID）
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;   // 启用自动签名
            PlayerSettings.iOS.hideHomeButton = true;                // 隐藏 Home 指示条（全屏沉浸体验）
            PlayerSettings.SetArchitecture(BuildTargetGroup.iOS, 1); // CPU 架构 ARM64
#endif

            // ========== Bundle 目录创建(YooAsset构建管线依赖这些目录) ==========
            var folderList = new[]
            {
                "Assets/StreamingAssets",
                "Assets/Bundles/AOTCode",
                "Assets/Bundles/Code",
                "Assets/Bundles/Shader",
                "Assets/Bundles/Textures",
                "Assets/Bundles/Sprites",
                "Assets/Bundles/Config",
                "Assets/Bundles/Sound",
                "Assets/Bundles/UI"
            };
            foreach (var folder in folderList)
            {
                if (Directory.Exists(folder)) continue;
                Directory.CreateDirectory(folder);
            }

            // 保存设置，确保本次初始化的修改立即写入磁盘
            AssetDatabase.SaveAssets();
        }
    }
}