using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Diagnostics;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using Debug = UnityEngine.Debug;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Editor
{
    /// <summary>
    /// 构建产品帮助类。
    /// 主要用于发布游戏的各个平台的产品
    /// </summary>
    public static class BuildProductHelper
    {
        /// <summary>
        /// 构建导出根目录
        /// </summary>
        private static string BuildRootPath => $"{GetProjectPath()}/Builds";

        /// <summary>
        /// 构建导出路径
        /// </summary>
        private static string _buildPath;

        /// <summary>
        /// 构建时间
        /// </summary>
        private static string _buildTime;

        static BuildProductHelper()
        {
            _buildPath = string.Empty;
            UpdateBuildTime();
        }


        /// <summary>
        /// 发布WindowsX64平台
        /// </summary>
        [MenuItem("FuFramework/Build/Windows X64", false, 100)]
        public static void BuildToWindows64()
        {
            PlayerSettings.SplashScreen.show = false;
            Debug.Log(EditorUserBuildSettings.activeBuildTarget);
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
            {
                Debug.LogError("当前构建目标平台不是 Windows, 请先手动切换到 Windows 平台!");
                return;
            }

            try
            {
                // 标记HotFix.asmdef程序集仅在非Editor环境(运行时)下使用
                HotFixEditorCompilerHelper.AddEditorInExcludePlatforms();

                // 构建相关设置
                PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
                PlayerSettings.defaultScreenHeight = 720;
                PlayerSettings.defaultScreenWidth = 1280;
                EditorUserBuildSettings.selectedStandaloneTarget = BuildTarget.StandaloneWindows64;
                AssetDatabase.SaveAssets();

                // 更新构建时间
                UpdateBuildTime();

                // 构建输出路径
                var outputPath = BuildOutputPath() + Path.DirectorySeparatorChar;
                var exePath = outputPath + PlayerSettings.productName + ".exe";
                
                // 执行构建
                var buildReport = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, exePath , EditorUserBuildSettings.activeBuildTarget, BuildOptions.None);
                if (buildReport.summary.result != BuildResult.Succeeded) return;

                // 删除 BackUpThisFolder_ButDontShipItWithYourGame备份文件夹。
                // 该文件夹是IL2CPP中中间生成结果，包含C#翻译成的cpp文件，以及cpp文件编译后生成的dll文件，存在只是为了下次打包时减少编译时间
                var buildDirectory = new DirectoryInfo(outputPath);
                foreach (var directoryInfo in buildDirectory.GetDirectories())
                {
                    if (directoryInfo.Name.Contains("BackUpThisFolder_ButDontShipItWithYourGame"))
                    {
                        directoryInfo.Delete(true);
                        break;
                    }
                }

                // 复制 Steam AppId.txt 配置 到 构建目录
                CopySteamWorksConfig(buildDirectory);

                // 压缩文件
                // var pathName = Path.GetDirectoryName(resultDirectory);
                // ZipHelper.CompressDirectory(resultDirectory, pathName + ".zip");
                
                Debug.Log("构建成功:" + exePath);
                
                // 构建完成后自动打开文件夹
                EditorUtility.RevealInFinder(exePath);
            }
            finally
            {
                HotFixEditorCompilerHelper.RemoveEditorInExcludePlatforms();
            }
        }

        /// <summary>
        /// 发布WindowsX32平台
        /// </summary>
        [MenuItem("FuFramework/Build/Windows X32", false, 100)]
        public static void BuildToWindows32()
        {
            PlayerSettings.SplashScreen.show = false;
            Debug.Log(EditorUserBuildSettings.activeBuildTarget);
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows)
            {
                Debug.LogError("当前构建目标平台不是 Windows, 请先手动切换到 Windows 平台!");
                return;
            }

            try
            {
                // 标记HotFix.asmdef程序集仅在非Editor环境(运行时)下使用
                HotFixEditorCompilerHelper.AddEditorInExcludePlatforms();

                // 构建相关设置
                PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
                PlayerSettings.defaultScreenHeight = 720;
                PlayerSettings.defaultScreenWidth = 1280;
                EditorUserBuildSettings.selectedStandaloneTarget = BuildTarget.StandaloneWindows;
                AssetDatabase.SaveAssets();

                // 更新构建时间
                UpdateBuildTime();

                // 构建输出路径
                var outputPath = BuildOutputPath() + Path.DirectorySeparatorChar;
                var exePath = outputPath + PlayerSettings.productName + ".exe";
                
                // 执行构建
                var buildReport = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, exePath, EditorUserBuildSettings.activeBuildTarget, BuildOptions.None);
                if (buildReport.summary.result != BuildResult.Succeeded) return;

                // 删除 BackUpThisFolder_ButDontShipItWithYourGame备份文件夹。
                // 该文件夹是IL2CPP中中间生成结果，包含C#翻译成的cpp文件，以及cpp文件编译后生成的dll文件，存在只是为了下次打包时减少编译时间
                var buildDirectory = new DirectoryInfo(outputPath);
                foreach (var directoryInfo in buildDirectory.GetDirectories())
                {
                    if (directoryInfo.Name.Contains("BackUpThisFolder_ButDontShipItWithYourGame"))
                    {
                        directoryInfo.Delete(true);
                        break;
                    }
                }

                // 复制 Steam AppId.txt 配置 到 构建目录
                CopySteamWorksConfig(buildDirectory);

                // 压缩文件
                // var pathName = Path.GetDirectoryName(resultDirectory);
                // ZipHelper.CompressDirectory(resultDirectory, pathName + ".zip");
                
                Debug.Log("构建成功:" + outputPath);

                // 构建完成后自动打开文件夹
                EditorUtility.RevealInFinder(exePath);
            }
            finally
            {
                HotFixEditorCompilerHelper.RemoveEditorInExcludePlatforms();
            }
        }

        /// <summary>
        /// 发布MacOS平台
        /// </summary>
        [MenuItem("FuFramework/Build/MacOS", false, 200)]
        public static void BuildToMacOS()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneOSX)
            {
                Debug.LogError("当前构建目标平台不是 MacOS, 请先手动切换到 MacOS 平台!");
                return;
            }

            try
            {
                // 标记HotFix.asmdef程序集仅在非Editor环境(运行时)下使用
                HotFixEditorCompilerHelper.AddEditorInExcludePlatforms();

                // 构建相关设置
                PlayerSettings.SplashScreen.show = false;
                PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
                PlayerSettings.defaultScreenHeight = 720;
                PlayerSettings.defaultScreenWidth = 1280;
                AssetDatabase.SaveAssets();

                // 更新构建时间
                UpdateBuildTime();

                // 构建输出路径
                var outputPath = BuildOutputPath() + Path.DirectorySeparatorChar;
                var appPath = outputPath + PlayerSettings.productName + ".app";
                
                // 执行构建
                var buildReport = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, appPath, EditorUserBuildSettings.activeBuildTarget, BuildOptions.None);
                if (buildReport.summary.result != BuildResult.Succeeded) return;

                // 删除 BackUpThisFolder_ButDontShipItWithYourGame备份文件夹。
                // 该文件夹是IL2CPP中中间生成结果，包含C#翻译成的cpp文件，以及cpp文件编译后生成的dll文件，存在只是为了下次打包时减少编译时间
                var buildDirectory = new DirectoryInfo(outputPath);
                foreach (var directoryInfo in buildDirectory.GetDirectories())
                {
                    if (directoryInfo.Name.Contains("BackUpThisFolder_ButDontShipItWithYourGame"))
                    {
                        directoryInfo.Delete(true);
                        break;
                    }
                }

                // 复制 Steam AppId.txt 配置 到 构建目录
                CopySteamWorksConfig(buildDirectory);

                // var pathName = Path.GetDirectoryName(resultDirectory);
                // ZipHelper.CompressDirectory(resultDirectory, pathName + ".zip");
                Debug.Log("构建成功:" + appPath);
                
                // 构建完成后自动打开文件夹
                EditorUtility.RevealInFinder(appPath);
            }
            finally
            {
                HotFixEditorCompilerHelper.RemoveEditorInExcludePlatforms();
            }
        }

        /// <summary>
        /// 发布 APK
        /// </summary>
        [MenuItem("FuFramework/Build/Apk", false, 250)]
        private static void BuildPlayerToAndroid()
        {
            PlayerSettings.SplashScreen.show = false;
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Debug.LogError("当前构建目标平台不是 Android, 请先手动切换到 Android 平台!");
                return;
            }

            if (string.IsNullOrEmpty(PlayerSettings.Android.keystoreName)
                || string.IsNullOrEmpty(PlayerSettings.Android.keyaliasName)
                || string.IsNullOrEmpty(PlayerSettings.Android.keyaliasPass)
                || string.IsNullOrEmpty(PlayerSettings.Android.keystorePass))
            {
                Debug.LogError("没有设置签名密钥,取消打包APK");
                return;
            }
            
            try
            {
                // 标记HotFix.asmdef程序集仅在非Editor环境(运行时)下使用
                HotFixEditorCompilerHelper.AddEditorInExcludePlatforms();
                
                // 更新构建时间
                UpdateBuildTime();
                
                // 构建相关设置
                EditorUserBuildSettings.buildAppBundle = false;
                EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
                AssetDatabase.SaveAssets();
                
                // 构建输出路径
                _buildPath = BuildOutputPath();
                var apkPath = $"{_buildPath}.apk";
                
                // 执行构建
                BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, apkPath, BuildTarget.Android, BuildOptions.None);
                Debug.Log("构建成功:" + apkPath);
                
                // 构建完成后自动打开文件夹
                EditorUtility.RevealInFinder(_buildPath);
            }
            finally
            {
                // 构建完成后恢复标记
                HotFixEditorCompilerHelper.RemoveEditorInExcludePlatforms();
            }
        }

        /// <summary>
        /// 发布 AAB
        /// </summary>
        [MenuItem("FuFramework/Build/AAB", false, 250)]
        private static void BuildAppBundleForAndroid()
        {
            PlayerSettings.SplashScreen.show = false;
            
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Debug.LogError("当前构建目标平台不是 Android, 请先手动切换到 Android 平台!");
                return;
            }
            
            if (string.IsNullOrEmpty(PlayerSettings.Android.keystoreName)
                || string.IsNullOrEmpty(PlayerSettings.Android.keyaliasName)
                || string.IsNullOrEmpty(PlayerSettings.Android.keyaliasPass)
                || string.IsNullOrEmpty(PlayerSettings.Android.keystorePass))
            {
                Debug.LogError("没有设置签名密钥,取消打包AAB");
                return;
            }

            try
            {
                // 标记HotFix.asmdef程序集仅在非Editor环境(运行时)下使用
                HotFixEditorCompilerHelper.AddEditorInExcludePlatforms();
                
                // 更新构建时间
                UpdateBuildTime();
                
                // 构建相关设置
                EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
                EditorUserBuildSettings.buildAppBundle = true;
                EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Public; // 开启符号表的输出
                AssetDatabase.SaveAssets();
                
                // 构建输出路径
                _buildPath = BuildOutputPath();
                var aapPath = $"{_buildPath}.aab";
                
                // 执行构建
                BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, aapPath, BuildTarget.Android, BuildOptions.None);
                Debug.Log("构建成功:" + aapPath);
                
                // 构建完成后自动打开文件夹
                EditorUtility.RevealInFinder(_buildPath);
            }
            finally
            {
                // 构建完成后恢复标记
                HotFixEditorCompilerHelper.RemoveEditorInExcludePlatforms();
            }
        }

        /// <summary>
        /// 发布 WebGL
        /// </summary>
        [MenuItem("FuFramework/Build/WebGL", false, 300)]
        private static void BuildPlayerToWebGL()
        {
            PlayerSettings.SplashScreen.show = false;

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                Debug.LogError("当前构建目标平台不是 WebGL, 请先手动切换到 WebGL 平台!");
                return;
            }

            try
            {
                // 标记HotFix.asmdef程序集仅在非Editor环境(运行时)下使用
                HotFixEditorCompilerHelper.AddEditorInExcludePlatforms();
                AssetDatabase.SaveAssets();

                // 更新构建时间
                UpdateBuildTime();

                // 构建输出路径
                _buildPath = BuildOutputPath();
                
                // 执行构建
                BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, _buildPath, BuildTarget.WebGL, BuildOptions.None);
                Debug.Log("构建成功:" + _buildPath);
                
                // 构建完成后自动打开文件夹
                EditorUtility.RevealInFinder(_buildPath);
            }
            finally
            {
                // 构建完成后恢复标记
                HotFixEditorCompilerHelper.RemoveEditorInExcludePlatforms();
            }
        }

#if ENABLE_WX_MINI_GAME
        /// <summary>
        /// 发布 微信小游戏 WebGL
        /// </summary>
        [MenuItem("FuFramework/Build/WeChat MiniGame WebGL", false, 300)]
        private static void BuildPlayerToWeChatMiniGameWebGL()
        {
            PlayerSettings.SplashScreen.show = false;

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                Debug.LogError("当前构建目标平台不是 WebGL, 请先手动切换到 WebGL 平台!");
                return;
            }

            try
            {
                // 标记HotFix.asmdef程序集仅在非Editor环境(运行时)下使用 
                HotFixEditorCompilerHelper.AddEditor();

                // 更新构建时间
                UpdateBuildTime();
                WeChatWASM.WXConvertCore.config.ProjectConf.DST = BuildOutputPath();
                AssetDatabase.SaveAssets();
                WeChatWASM.WXConvertCore.DoExport();
                Debug.Log("构建成功:" + BuildOutputPath());
                
                // 构建完成后自动打开文件夹
                EditorUtility.RevealInFinder(BuildOutputPath());
            }
            finally
            {
                // 构建完成后恢复标记
                HotFixEditorCompilerHelper.RemoveEditor();
            }
        }
#endif

        /// <summary>
        /// 发布 Xcode Debug 版本
        /// </summary>
        [MenuItem("FuFramework/Build/Xcode Project Debug", false, 400)]
        private static void ExportToXcodeToDevelop()
        {
            try
            {
                // 标记HotFix.asmdef程序集仅在非Editor环境(运行时)下使用
                HotFixEditorCompilerHelper.AddEditorInExcludePlatforms();
                PlayerSettings.SplashScreen.show = false;
                AssetDatabase.SaveAssets();
                
                // 更新构建时间
                UpdateBuildTime();
                
                // 构建输出路径
                _buildPath = BuildOutputPath();
                EditorUserBuildSettings.development = true;
                
                // 执行构建
                BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, _buildPath, BuildTarget.iOS, BuildOptions.None);
                Process.Start(_buildPath);
                Debug.Log("构建成功:" + _buildPath);
                
                // 构建完成后自动打开文件夹
                EditorUtility.RevealInFinder(BuildOutputPath());
            }
            finally
            {
                // 构建完成后恢复标记
                HotFixEditorCompilerHelper.RemoveEditorInExcludePlatforms();
            }
        }

        /// <summary>
        /// 发布 Xcode Release 版本
        /// </summary>
        [MenuItem("FuFramework/Build/Xcode Project Release", false, 400)]
        private static void ExportToXcodeToRelease()
        {
            try
            {
                // 标记HotFix.asmdef程序集仅在非Editor环境(运行时)下使用
                HotFixEditorCompilerHelper.AddEditorInExcludePlatforms();
                PlayerSettings.SplashScreen.show = false;
                
                // 构建相关设置
                EditorUserBuildSettings.development = false;
                AssetDatabase.SaveAssets();

                // 更新构建时间
                UpdateBuildTime();

                // 构建输出路径
                _buildPath = BuildOutputPath();
                
                // 执行构建
                BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, _buildPath, BuildTarget.iOS, BuildOptions.None);
                Process.Start(_buildPath);
                Debug.Log("构建成功:" + _buildPath);
                
                // 构建完成后自动打开文件夹
                EditorUtility.RevealInFinder(_buildPath);
            }
            finally
            {
                // 构建完成后恢复标记
                HotFixEditorCompilerHelper.RemoveEditorInExcludePlatforms();
            }
        }

        /// <summary>
        /// 获取工程路径
        /// </summary>
        /// <returns></returns>
        private static string GetProjectPath() => Application.dataPath.Replace("Assets", string.Empty);

        /// <summary>
        /// 更新构建时间
        /// </summary>
        private static void UpdateBuildTime() => _buildTime = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");

        /// <summary>
        /// 获取发布导出路径：Builds/目标平台/应用标识Identifier/Version/BuildTime_v_BundleVersion
        /// </summary>
        /// <returns></returns>
        private static string BuildOutputPath()
        {
            var pathName = $"{Application.identifier}_{_buildTime}_v_{PlayerSettings.bundleVersion}";
            switch (EditorUserBuildSettings.activeBuildTarget)
            {
                case BuildTarget.Android:
                    pathName = $"{_buildTime}_v_{PlayerSettings.bundleVersion}_code_{PlayerSettings.Android.bundleVersionCode}";
                    break;
                case BuildTarget.iOS:
                case BuildTarget.StandaloneOSX:
                    pathName = $"{_buildTime}_v_{PlayerSettings.bundleVersion}_code_{PlayerSettings.iOS.buildNumber}";
                    break;
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    pathName = $"{_buildTime}";
                    break;
                case BuildTarget.WebGL:
                    pathName = $"{_buildTime}_v_{PlayerSettings.bundleVersion}";
                    break;
            }

            var path = Path.Combine(BuildRootPath, EditorUserBuildSettings.activeBuildTarget.ToString(), Application.identifier, Application.version, pathName);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            return path;
        }

        /// <summary>
        /// 复制 SteamWorks 配置
        /// </summary>
        /// <param name="buildDirectory">目标目录</param>
        private static void CopySteamWorksConfig(DirectoryInfo buildDirectory)
        {
#if STEAMWORKS_NET
            DirectoryInfo projectDirectoryInfo = new DirectoryInfo(Application.dataPath);
            var steamAppidPath = PathHelper.Combine(projectDirectoryInfo.Parent.FullName, "steam_appid.txt");
            if (File.Exists(steamAppidPath))
            {
                File.Copy(steamAppidPath, PathHelper.Combine(buildDirectory.FullName, "steam_appid.txt"), true);
            }
#endif
        }

        /// <summary>
        /// 发布后版本号自动更新
        /// </summary>
        /// <param name="target"></param>
        /// <param name="path"></param>
        [PostProcessBuild(999)]
        public static void OnPostProcessBuild(BuildTarget target, string path)
        {
            if (target == BuildTarget.Android)
            {
                // Update Build Version Code
                PlayerSettings.Android.bundleVersionCode = Convert.ToInt32(PlayerSettings.Android.bundleVersionCode) + 1;
            }

            if (target == BuildTarget.iOS)
            {
                // Update Build Version Code
                PlayerSettings.iOS.buildNumber = (Convert.ToInt32(PlayerSettings.iOS.buildNumber) + 1).ToString();
            }
        }
    }
}