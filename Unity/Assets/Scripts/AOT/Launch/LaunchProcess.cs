using System;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using YooAsset;
using HybridCLR;
using AOT.Framework.Core.Log;
using AOT.Framework.ModuleSetting.Runtime;
using AOT.Launch.UpdateConfig;
using UtilityAOT = AOT.Framework.Core.Utility.UtilityAOT;

// ReSharper disable once CheckNamespace
namespace AOT.Launch
{
    /// <summary>
    /// AOT 启动引导流程。
    /// 功能：显示加载界面 → 资源更新（远端配置/版本/清单/下载）→ 加载 AOT 元数据与 Hotfix.dll → 移交热更入口。
    /// </summary>
    public static class LaunchProcess
    {
        /// <summary>
        /// 远端更新配置文件名，从远端资源服务器获取。
        /// </summary>
        private const string RemoteUpdateConfigName = "RemoteUpdateConfig.json";

        /// <summary>
        /// 热更 dll 名称。
        /// </summary>
        private const string HotfixDllName = "Hotfix";

        /// <summary>
        /// 加载界面视图。
        /// </summary>
        private static ILaunchView m_LaunchView;

        /// <summary>
        /// 运行引导流程。加载完 Hotfix 程序集后直接反射进入热更入口。
        /// </summary>
        /// <returns>异步流程</returns>
        public static async UniTask RunAsync()
        {
            FuLogger.LogInfo("<color=#43f656>------进入启动引导流程------</color>");

            // 显示加载界面
            m_LaunchView = await LaunchView.CreateAsync();

            var playMode = GameSetting.Instance.PlayMode;

            RemoteUpdateConfig updateConfig = null;

            // 联机/Web 模式：请求远端更新配置（含强更判断）
            if (playMode is not (EPlayMode.EditorSimulateMode or EPlayMode.OfflinePlayMode))
            {
                updateConfig = await ReqRemoteUpdateConfigWithRetry();
                if (updateConfig.ForceUpdate)
                {
                    // 强更中止后续流程
                    m_LaunchView.ShowUpdateDialog(updateConfig.UpdateAnnouncement, () => Application.OpenURL(updateConfig.AppDownloadUrl));
                    return;
                }
            }

            // 初始化资源包
            m_LaunchView.SetTip("InitPackage...");
            if (updateConfig == null)
            {
                await LaunchAssetHelper.InitPackageAsync();
            }
            else
            {
                var version   = $"v{UtilityAOT.Version.MajorMinorVersion}";
                var url       = string.Format(updateConfig.ResDownloadUrl,       version);
                var backupUrl = string.Format(updateConfig.ResDownloadBackupUrl, version);
                await LaunchAssetHelper.InitPackageAsync(url, backupUrl);
            }

            // 获取版本号（失败重试）
            m_LaunchView.SetTip("GetVersion...");
            string packageVersion;
            while ((packageVersion = await LaunchAssetHelper.RequestVersionAsync()) == null)
            {
                m_LaunchView.SetTip("获取版本号失败，正在重试...");
                await UniTask.WaitForSeconds(3);
            }

            // 更新资源清单（失败重试）
            m_LaunchView.SetTip("UpdateManifest...");
            while (!await LaunchAssetHelper.UpdateManifestAsync(packageVersion))
            {
                m_LaunchView.SetTip("更新清单失败，正在重试...");
                await UniTask.WaitForSeconds(3);
            }

            // 联机模式：创建下载器并下载（编辑器/离线跳过）
            if (playMode is not (EPlayMode.EditorSimulateMode or EPlayMode.OfflinePlayMode))
                await CreateAndDownload(updateConfig);

            // 资源更新完毕
            m_LaunchView.SetDownloading(false);
            m_LaunchView.SetTip(string.Empty);

            // 加载 AOT 补充元数据 + Hotfix.dll，移交热更入口
            await LoadHotfixAndHandoff();
        }

        /// <summary>
        /// 请求远端更新配置（失败重试）。
        /// </summary>
        private static async UniTask<RemoteUpdateConfig> ReqRemoteUpdateConfigWithRetry()
        {
            var configUrl = $"{GameSetting.Instance.ResCdnRootRootURL}{UtilityAOT.Application.PlatformName}/{RemoteUpdateConfigName}";
            while (true)
            {
                try
                {
                    using var request = UnityWebRequest.Get(configUrl);
                    request.timeout = 5;
                    await request.SendWebRequest();
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var cfg = UtilityAOT.Json.ToObject<RemoteUpdateConfig>(request.downloadHandler.text);
                        if (cfg != null) return cfg;
                    }
                }
                catch (Exception e)
                {
                    FuLogger.LogError($"[Launch] 获取远端更新配置异常：{e.Message}");
                }

                m_LaunchView.SetTip("资源服务器错误，正在重试...");
                await UniTask.WaitForSeconds(3);
            }
        }

        /// <summary>
        /// 创建下载器并下载资源。
        /// </summary>
        /// <param name="updateConfig">远端更新配置。</param>
        private static async UniTask CreateAndDownload(RemoteUpdateConfig updateConfig)
        {
            while (true)
            {
                var downloader = LaunchAssetHelper.CreateDownloader();
                if (downloader.TotalDownloadCount == 0) return; // 无需下载

                // 需要确认更新
                if (updateConfig is { ShowUpdateTips: true })
                {
                    var confirmed = new UniTaskCompletionSource();
                    m_LaunchView.ShowUpdateDialog(updateConfig.UpdateAnnouncement, () =>
                    {
                        m_LaunchView.SetNeedUpgrade(false);
                        confirmed.TrySetResult();
                    });
                    await confirmed.Task;
                }

                downloader.DownloadProgressChanged += args =>
                {
                    var progress = args.CurrentDownloadBytes / (args.TotalDownloadBytes * 1f);
                    var cur      = UtilityAOT.File.GetBytesSizeWithUnit(args.CurrentDownloadBytes);
                    var tot      = UtilityAOT.File.GetBytesSizeWithUnit(args.TotalDownloadBytes);
                    m_LaunchView.SetProgress(progress, $"下载中：{cur}/{tot}");
                };
                var failed = false;
                downloader.DownloadError += _ => failed = true;

                // v3 API: 使用 StartDownload() 替代旧版 BeginDownload()
                downloader.StartDownload();
                await downloader;

                if (!failed && downloader.Status == EOperationStatus.Succeeded) return; // 下载成功
                m_LaunchView.SetTip("下载失败，正在重试...");
                await UniTask.WaitForSeconds(3); // 失败后重建下载器重试
            }
        }

        /// <summary>
        /// 加载 AOT 补充元数据与 Hotfix 程序集，随后反射进入热更入口。
        /// </summary>
        private static async UniTask LoadHotfixAndHandoff()
        {
            FuLogger.LogInfo("<color=#43f656>------进入代码热更流程------</color>");

            // 编辑器模拟模式：程序集已在域中，直接进入热更入口
            if (UtilityAOT.Application.IsEditor && LaunchAssetHelper.PlayMode == EPlayMode.EditorSimulateMode)
            {
                await EnterHotfixAsync();
                return;
            }

            // 补充 AOT 元数据
            foreach (var aotDll in AOTGenericReferences.PatchedAOTAssemblyList)
            {
                var aotPath = UtilityAOT.AssetPath.GetAOTCodePath(aotDll);
                var bytes   = await LaunchAssetHelper.LoadDllBytesAsync(aotPath);

                // 加载失败：LoadRawFileBytesAsync 返回 null，禁止把 null 传给 RuntimeApi，记录路径并中止移交。
                if (bytes == null)
                {
                    FuLogger.LogError($"[Launch] 加载 AOT 补充元数据失败，中止热更移交：{aotPath}");
                    m_LaunchView.SetTip("热更资源加载失败，请检查网络后重启游戏");
                    return;
                }

                RuntimeApi.LoadMetadataForAOTAssembly(bytes, HomologousImageMode.SuperSet);
                FuLogger.LogInfo($"[Launch] 补充 AOT 元数据：{aotDll}");
            }

            // 加载 Hotfix.dll 并反射进入热更入口
            var dllPath  = UtilityAOT.AssetPath.GetCodePath($"{HotfixDllName}.dll");
            var dllBytes = await LaunchAssetHelper.LoadDllBytesAsync(dllPath);

            // 加载失败：禁止把 null 传给 Assembly.Load，记录路径并中止移交。
            if (dllBytes == null)
            {
                FuLogger.LogError($"[Launch] 加载 Hotfix 程序集失败，中止热更移交：{dllPath}");
                m_LaunchView.SetTip("热更资源加载失败，请检查网络后重启游戏");
                return;
            }

            System.Reflection.Assembly.Load(dllBytes);
            FuLogger.LogInfo("[Launch] Hotfix 程序集加载完成");

            await EnterHotfixAsync();
        }

        /// <summary>
        /// 反射调用 HotfixLauncher.MainAsync() 进入热更逻辑。
        /// Hotfix 程序集已在域中（编辑器模拟模式直接可用，非编辑器模式由 LoadHotfixAndHandoff 加载到域）。
        /// </summary>
        private static async UniTask EnterHotfixAsync()
        {
            System.Reflection.Assembly hotfixAssembly = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name != HotfixDllName) continue;
                hotfixAssembly = assembly;
                break;
            }

            if (hotfixAssembly == null)
            {
                FuLogger.LogError("[Launch] 未找到已加载的 Hotfix 程序集，无法进入热更入口。");
                return;
            }

            var entryType  = hotfixAssembly.GetType("Hotfix.HotfixLauncher");
            var mainMethod = entryType?.GetMethod("MainAsync", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (mainMethod == null)
            {
                FuLogger.LogError("[Launch] 未找到热更入口 Hotfix.HotfixLauncher.MainAsync。");
                return;
            }

            await (UniTask)mainMethod.Invoke(null, new object[] { m_LaunchView });
        }
    }
}