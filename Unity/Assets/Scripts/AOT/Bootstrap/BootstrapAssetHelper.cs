using System.Collections.Generic;
using YooAsset;
using UnityEngine;
using Cysharp.Threading.Tasks;
using AOT.Framework.Core.Log;
using AOT.Framework.ModuleSetting.Runtime;

// ReSharper disable once CheckNamespace
namespace AOT.Bootstrap
{
    /// <summary>
    /// AOT 资源引导助手。
    /// 功能：引导期直连 YooAsset 完成「初始化默认包 / 请求版本号 / 更新清单 / 创建下载器 / 加载原始字节」等流程。
    ///      不依赖 AssetModule。初始化完成后写入 BootstrapContext 供热更侧 AssetModule 复用。
    /// </summary>
    public static class BootstrapAssetHelper
    {
        /// <summary>
        /// 最大下载数。
        /// </summary>
        private static int m_DownloadingMaxNum;

        /// <summary>
        /// 失败重试次数。
        /// </summary>
        private static int m_FailedTryAgainNum;

        /// <summary>
        /// 资源运行模式。
        /// </summary>
        public static EPlayMode PlayMode { get; private set; }

        /// <summary>
        /// 默认资源包名称。
        /// </summary>
        public static string DefaultPackageName { get; private set; }

        /// <summary>
        /// 默认资源包。
        /// </summary>
        public static ResourcePackage DefaultPackage { get; private set; }


        /// <summary>
        /// 初始化 YooAsset 与默认资源包。
        /// <param name="url"> 远端资源服务器地址。</param>
        /// <param name="backupUrl"> 备用资源服务器地址。</param>
        /// <returns>初始化操作结果。</returns>
        /// </summary>
        public static async UniTask InitPackageAsync(string url = null, string backupUrl = null)
        {
            PlayMode            = GameSetting.Instance.PlayMode;
            DefaultPackageName  = GameSetting.Instance.DefaultPackageName;
            m_DownloadingMaxNum = GameSetting.Instance.DownloadingMaxNum;
            m_FailedTryAgainNum = GameSetting.Instance.FailedTryAgainNum;

            // 初始化 YooAsset 与默认资源包。
            if (!BootstrapContext.YooAssetInitialized)
            {
                YooAssets.Initialize();
                YooAssets.SetAsyncOperationMaxTimeSlice(GameSetting.Instance.AsyncSystemMaxSlicePerFrame);

                // 记入引导上下文
                BootstrapContext.YooAssetInitialized = true;
            }

            // 获取或创建默认资源包（v3 移除了 SetDefaultPackage，改为自行持有引用）。
            DefaultPackage = YooAssets.TryGetPackage(DefaultPackageName, out var existingPackage) ? existingPackage : YooAssets.CreatePackage(DefaultPackageName);

            // 根据运行模式初始化资源包
            var initOperation = InitByPlayMode(url, backupUrl);
            if (initOperation == null)
            {
                FuLogger.LogError($"[Bootstrap] 未知的资源运行模式: {PlayMode}");
                return;
            }

            // 返回初始化操作结果。
            await initOperation;
            if (initOperation.Status != EOperationStatus.Succeeded)
            {
                FuLogger.LogError($"[Bootstrap] 资源包初始化失败: {initOperation.Error}");
            }
        }

        /// <summary>
        /// 根据运行模式初始化资源包。
        /// <param name="url"> 远端资源服务器地址。</param>
        /// <param name="backupUrl"> 备用资源服务器地址。</param>
        /// <returns>初始化操作。</returns>
        /// </summary>
        private static InitializePackageOperation InitByPlayMode(string url, string backupUrl)
        {
            switch (PlayMode)
            {
                // 编辑器模拟模式：模拟打包并创建文件系统。
                case EPlayMode.EditorSimulateMode:
                {
                    var buildResult = EditorSimulateBuildInvoker.Build(DefaultPackageName, (int)EBundleType.VirtualAssetBundle);
                    var fs          = FileSystemParameters.CreateDefaultEditorFileSystemParameters(buildResult.PackageRootDirectory);
                    return DefaultPackage.InitializePackageAsync(new EditorSimulateModeOptions { EditorFileSystemParameters = fs });
                }

                // 离线模式：使用内置文件系统(StreamingAssets)。
                case EPlayMode.OfflinePlayMode:
                {
                    var fs = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters();
                    return DefaultPackage.InitializePackageAsync(new OfflinePlayModeOptions { BuiltinFileSystemParameters = fs });
                }

                // 主机模式：使用远程服务器文件系统(资源CDN服务器)。
                case EPlayMode.HostPlayMode:
                {
                    IRemoteService remote     = new RemoteServices(url, backupUrl);
                    var            cacheFs   = FileSystemParameters.CreateDefaultSandboxFileSystemParameters(remote);
                    var            buildInFs = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters();
                    return DefaultPackage.InitializePackageAsync(new HostPlayModeOptions { BuiltinFileSystemParameters = buildInFs, CacheFileSystemParameters = cacheFs });
                }

                // Web 模式：使用 Web 服务器文件系统（资源服务器）。
                case EPlayMode.WebPlayMode:
                {
                    FileSystemParameters webFs = null;

#if UNITY_WEBGL
    #if ENABLE_DOUYIN_MINI_GAME
                    // 创建抖音小游戏文件系统
                    if (string.IsNullOrWhiteSpace(url))
                        webFs = ByteGameFileSystemCreater.CreateByteGameFileSystemParameters();
                    else
                        webFs = ByteGameFileSystemCreater.CreateByteGameFileSystemParameters(url);
    #elif ENABLE_WECHAT_MINI_GAME
                    // 创建微信小游戏文件系统
                    WeChatWASM.WXBase.PreloadConcurrent(10);
                    if (string.IsNullOrWhiteSpace(url))
                        webFs = WechatFileSystemCreater.CreateWechatFileSystemParameters();
                    else
                        webFs = WechatFileSystemCreater.CreateWechatPathFileSystemParameters(url);
    #else
                    // 创建默认WebGL文件系统
                    webFs = FileSystemParameters.CreateDefaultWebFileSystemParameters();
    #endif
#else
                    // 非 WebGL 平台回退默认 Web 服务器文件系统
                    webFs = FileSystemParameters.CreateDefaultWebServerFileSystemParameters();
#endif
                    return DefaultPackage.InitializePackageAsync(new WebPlayModeOptions { WebServerFileSystemParameters = webFs });
                }
                case EPlayMode.CustomPlayMode:
                default: return null;
            }
        }

        /// <summary>
        /// 请求资源版本号，失败返回 null。
        /// </summary>
        public static async UniTask<string> RequestVersionAsync()
        {
            var reqOperation = DefaultPackage.RequestPackageVersionAsync();
            await reqOperation;
            return reqOperation.Status == EOperationStatus.Succeeded ? reqOperation.PackageVersion : null;
        }

        /// <summary>
        /// 更新资源清单，返回是否成功。
        /// </summary>
        public static async UniTask<bool> UpdateManifestAsync(string version)
        {
            var updateOperation = DefaultPackage.LoadPackageManifestAsync(new LoadPackageManifestOptions(version, 60));
            await updateOperation;
            return updateOperation.Status == EOperationStatus.Succeeded;
        }

        /// <summary>
        /// 创建资源下载器。
        /// </summary>
        public static ResourceDownloaderOperation CreateDownloader() =>
            DefaultPackage.CreateResourceDownloader(new ResourceDownloaderOptions(m_DownloadingMaxNum, m_FailedTryAgainNum));

        /// <summary>
        /// 加载程序集字节文件(用于 AOT/Hotfix DLL)。
        /// 本项目将 DLL/AOT 以 .bytes(TextAsset) 形式打包，故通过 LoadAssetAsync&lt;Object&gt; 加载后取 TextAsset.bytes，
        /// <param name="location"> 资源路径。</param>
        /// <returns>加载字节操作。</returns>
        /// </summary>
        public static UniTask<byte[]> LoadDllBytesAsync(string location)
        {
            var tcs    = new UniTaskCompletionSource<byte[]>();
            var handle = DefaultPackage.LoadAssetAsync<Object>(location);
            handle.Completed += h =>
            {
                // 加载失败：记录错误并以 null 结果完成，避免回调抛异常导致 await 永久挂起。
                if (h.Status != EOperationStatus.Succeeded)
                {
                    FuLogger.LogError($"[Bootstrap] 加载原始文件失败: {location}, 错误信息: {h.Error}");
                    tcs.TrySetResult(null);
                    return;
                }

                // 资源非 TextAsset（GetAssetObject 返回 null）：记录错误并以 null 结果完成。
                var textAsset = h.GetAssetObject<TextAsset>();
                if (textAsset == null)
                {
                    FuLogger.LogError($"[Bootstrap] 加载的资源不是 TextAsset 或为空: {location}");
                    tcs.TrySetResult(null);
                    return;
                }

                tcs.TrySetResult(textAsset.bytes);
            };
            return tcs.Task;
        }

        /// <summary>
        /// 远端资源服务器定义，用于提供远端资源的下载地址。
        /// </summary>
        private class RemoteServices : IRemoteService
        {
            /// <summary>
            /// 远端资源服务器地址。
            /// </summary>
            private string HostServer { get; }

            /// <summary>
            /// 备用资源服务器地址。
            /// </summary>
            private string FallbackHostServer { get; }

            /// <summary>
            /// 初始化远端资源服务器定义。
            /// </summary>
            /// <param name="hostServer"> 远端资源服务器地址。</param>
            /// <param name="fallbackHostServer"> 备用资源服务器地址。</param>
            public RemoteServices(string hostServer, string fallbackHostServer)
            {
                HostServer         = hostServer;
                FallbackHostServer = fallbackHostServer;
            }

            /// <summary>
            /// 获取指定文件的所有远端候选地址，按优先级排序。
            /// </summary>
            /// <param name="fileName"> 资源文件名。</param>
            /// <returns>按优先级排序的远端候选地址列表。</returns>
            public IReadOnlyList<string> GetRemoteUrls(string fileName)
            {
                var urls = new List<string>(2);
                if (!string.IsNullOrEmpty(HostServer))
                    urls.Add(HostServer + fileName);
                if (!string.IsNullOrEmpty(FallbackHostServer))
                    urls.Add(FallbackHostServer + fileName);
                return urls;
            }
        }
    }
}