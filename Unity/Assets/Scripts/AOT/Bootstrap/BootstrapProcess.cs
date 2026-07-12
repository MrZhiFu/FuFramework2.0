using System;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using YooAsset;
using HybridCLR;
using FuFramework.Core.Runtime;
using FuFramework.ModuleSetting.Runtime;
using Launcher.Bootstrap;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable once CheckNamespace
namespace Launcher
{
    /// <summary>
    /// AOT 启动引导流程。
    /// 功能：显示加载界面 → 资源更新（远端配置/版本/清单/下载）→ 加载 AOT 元数据与 Hotfix.dll → 移交热更入口。
    /// </summary>
    public static class BootstrapProcess
    {
        private const string RemoteUpdateConfigName = "RemoteUpdateConfig.json";
        private const string HotfixDllName          = "Hotfix";

        private static WinLauncher s_View;

        /// <summary>运行引导流程。onHotfixEntry：加载完 Hotfix 程序集后由外部执行热更入口调用。</summary>
        public static async UniTask RunAsync(Func<WinLauncher, UniTask> onHotfixEntry)
        {
            FuLogger.LogInfo("<color=#43f656>------进入启动引导流程------</color>");
            s_View = await WinLauncher.CreateAsync();

            var playMode = ModuleSetting.Instance.AssetSetting.PlayMode;
            RemoteUpdateConfig updateConfig = null;

            // 联机/Web 模式：请求远端更新配置（含强更判断）
            if (playMode is not (EPlayMode.EditorSimulateMode or EPlayMode.OfflinePlayMode))
            {
                updateConfig = await ReqRemoteUpdateConfigWithRetry();
                if (updateConfig.ForceUpdate)
                {
                    s_View.ShowUpdateDialog(updateConfig.UpdateAnnouncement, () => Application.OpenURL(updateConfig.AppDownloadUrl));
                    return; // 强更中止后续流程
                }
            }

            // 初始化资源包
            s_View.SetTip("InitPackage...");
            if (updateConfig == null)
            {
                await BootstrapAssetHelper.InitPackageAsync();
            }
            else
            {
                var version   = $"v{Utility.Version.MajorMinorVersion}";
                var url       = string.Format(updateConfig.ResDownloadUrl,       version);
                var backupUrl = string.Format(updateConfig.ResDownloadBackupUrl, version);
                await BootstrapAssetHelper.InitPackageAsync(url, backupUrl);
            }

            // 获取版本号（失败重试）
            s_View.SetTip("GetVersion...");
            string packageVersion;
            while ((packageVersion = await BootstrapAssetHelper.RequestVersionAsync()) == null)
            {
                s_View.SetTip("获取版本号失败，正在重试...");
                await UniTask.WaitForSeconds(3);
            }

            // 更新资源清单（失败重试）
            s_View.SetTip("UpdateManifest...");
            while (!await BootstrapAssetHelper.UpdateManifestAsync(packageVersion))
            {
                s_View.SetTip("更新清单失败，正在重试...");
                await UniTask.WaitForSeconds(3);
            }

            // 联机模式：创建下载器并下载（编辑器/离线跳过）
            if (playMode is not (EPlayMode.EditorSimulateMode or EPlayMode.OfflinePlayMode))
                await CreateAndDownload(updateConfig);

            // 资源更新完毕
            s_View.SetDownloading(false);
            s_View.SetTip(string.Empty);

            // 加载 AOT 补充元数据 + Hotfix.dll，移交热更入口
            await LoadHotfixAndHandoff(onHotfixEntry);
        }

        /// <summary>请求远端更新配置（失败重试）。</summary>
        private static async UniTask<RemoteUpdateConfig> ReqRemoteUpdateConfigWithRetry()
        {
            var assetSetting = ModuleSetting.Instance.AssetSetting;
            var configUrl    = $"{assetSetting.ResCdnRootRootURL}{Utility.Application.PlatformName}/{RemoteUpdateConfigName}";
            while (true)
            {
                try
                {
                    using var request = UnityWebRequest.Get(configUrl);
                    request.timeout = 5;
                    await request.SendWebRequest();
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var cfg = Utility.Json.ToObject<RemoteUpdateConfig>(request.downloadHandler.text);
                        if (cfg != null) return cfg;
                    }
                }
                catch (Exception e)
                {
                    FuLogger.LogError($"[Bootstrap] 获取远端更新配置异常：{e.Message}");
                }
                s_View.SetTip("资源服务器错误，正在重试...");
                await UniTask.WaitForSeconds(3);
            }
        }

        /// <summary>创建下载器并下载资源。</summary>
        private static async UniTask CreateAndDownload(RemoteUpdateConfig updateConfig)
        {
            while (true)
            {
                var downloader = BootstrapAssetHelper.CreateDownloader();
                if (downloader.TotalDownloadCount == 0) return; // 无需下载

                // 需要确认更新
                if (updateConfig is { ShowUpdateTips: true })
                {
                    var confirmed = new UniTaskCompletionSource();
                    s_View.ShowUpdateDialog(updateConfig.UpdateAnnouncement, () =>
                    {
                        s_View.SetNeedUpgrade(false);
                        confirmed.TrySetResult();
                    });
                    await confirmed.Task;
                }

                downloader.DownloadUpdateCallback = data =>
                {
                    var progress = data.CurrentDownloadBytes / (data.TotalDownloadBytes * 1f);
                    var cur      = Utility.File.GetBytesSizeWithUnit(data.CurrentDownloadBytes);
                    var tot      = Utility.File.GetBytesSizeWithUnit(data.TotalDownloadBytes);
                    s_View.SetProgress(progress, $"下载中：{cur}/{tot}");
                };
                var failed = false;
                downloader.DownloadErrorCallback = _ => failed = true;

                // 既有流程（ProcedureDownloadPackage）直接 await YooAsset 操作，此处保持一致。
                downloader.BeginDownload();
                await downloader;

                if (!failed && downloader.Status == EOperationStatus.Succeed) return; // 下载成功
                s_View.SetTip("下载失败，正在重试...");
                await UniTask.WaitForSeconds(3); // 失败后重建下载器重试
            }
        }

        /// <summary>加载 AOT 补充元数据与 Hotfix 程序集，并移交热更入口。</summary>
        private static async UniTask LoadHotfixAndHandoff(Func<WinLauncher, UniTask> onHotfixEntry)
        {
            FuLogger.LogInfo("<color=#43f656>------进入代码热更流程------</color>");

            // 编辑器模拟模式：程序集已在域中，直接移交
            if (Utility.Application.IsEditor && BootstrapAssetHelper.PlayMode == EPlayMode.EditorSimulateMode)
            {
                await onHotfixEntry(s_View);
                return;
            }

            // 补充 AOT 元数据
            foreach (var aotDll in AOTGenericReferences.PatchedAOTAssemblyList)
            {
                var aotPath = Utility.AssetPath.GetAOTCodePath(aotDll);
                var bytes   = await BootstrapAssetHelper.LoadRawFileBytesAsync(aotPath);
                // 加载失败：LoadRawFileBytesAsync 返回 null，禁止把 null 传给 RuntimeApi，记录路径并中止移交。
                if (bytes == null)
                {
                    FuLogger.LogError($"[Bootstrap] 加载 AOT 补充元数据失败，中止热更移交：{aotPath}");
                    s_View.SetTip("热更资源加载失败，请检查网络后重启游戏");
                    return;
                }
                RuntimeApi.LoadMetadataForAOTAssembly(bytes, HomologousImageMode.SuperSet);
                FuLogger.LogInfo($"[Bootstrap] 补充 AOT 元数据：{aotDll}");
            }

            // 加载 Hotfix.dll（域内自动注册程序集，供 onHotfixEntry 反射调用入口）
            var dllPath  = Utility.AssetPath.GetCodePath($"{HotfixDllName}.dll");
            var dllBytes = await BootstrapAssetHelper.LoadRawFileBytesAsync(dllPath);
            // 加载失败：禁止把 null 传给 Assembly.Load，记录路径并中止移交。
            if (dllBytes == null)
            {
                FuLogger.LogError($"[Bootstrap] 加载 Hotfix 程序集失败，中止热更移交：{dllPath}");
                s_View.SetTip("热更资源加载失败，请检查网络后重启游戏");
                return;
            }
            System.Reflection.Assembly.Load(dllBytes);
            FuLogger.LogInfo("[Bootstrap] Hotfix 程序集加载完成");

            await onHotfixEntry(s_View);
        }
    }
}
