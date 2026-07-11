using YooAsset;
using UnityEngine;
using Cysharp.Threading.Tasks;
using FuFramework.Core.Runtime;
using FuFramework.ModuleSetting.Runtime;

// ReSharper disable once CheckNamespace
namespace Launcher
{
    /// <summary>
    /// AOT 资源引导助手。
    /// 功能：引导期直连 YooAsset 完成「初始化默认包 / 请求版本号 / 更新清单 / 创建下载器 / 加载原始字节」，
    ///      不依赖 AssetModule。初始化完成后写入 BootstrapContext 供热更侧 AssetModule 复用。
    /// </summary>
    public static class BootstrapAssetHelper
    {
        private static ResourcePackage s_DefaultPackage;
        private static int             s_DownloadingMaxNum;
        private static int             s_FailedTryAgainNum;

        /// <summary>资源运行模式。</summary>
        public static EPlayMode PlayMode { get; private set; }

        /// <summary>默认资源包名称。</summary>
        public static string DefaultPackageName { get; private set; }

        /// <summary>默认资源包。</summary>
        public static ResourcePackage DefaultPackage => s_DefaultPackage;

        /// <summary>初始化 YooAsset 与默认资源包。</summary>
        public static async UniTask InitPackageAsync(string url = null, string backupUrl = null)
        {
            var assetSetting = ModuleSetting.Instance.AssetSetting;
            PlayMode            = assetSetting.PlayMode;
            DefaultPackageName  = assetSetting.DefaultPackageName;
            s_DownloadingMaxNum = assetSetting.DownloadingMaxNum;
            s_FailedTryAgainNum = assetSetting.FailedTryAgainNum;

            if (!BootstrapContext.YooAssetInitialized)
            {
                YooAssets.Initialize();
                YooAssets.SetOperationSystemMaxTimeSlice(assetSetting.AsyncSystemMaxSlicePerFrame);
                BootstrapContext.YooAssetInitialized = true;
                BootstrapContext.DefaultPackageName  = DefaultPackageName;
            }

            s_DefaultPackage = YooAssets.TryGetPackage(DefaultPackageName) ?? YooAssets.CreatePackage(DefaultPackageName);
            YooAssets.SetDefaultPackage(s_DefaultPackage);

            var initOp = InitByPlayMode(url, backupUrl);
            if (initOp == null)
            {
                FuLogger.LogError($"[Bootstrap] 未知的资源运行模式: {PlayMode}");
                return;
            }

            // 既有流程（ProcedureGetPackageVersion 等）均直接 await YooAsset 操作，此处保持一致。
            await initOp;
            if (initOp.Status != EOperationStatus.Succeed)
                FuLogger.LogError($"[Bootstrap] 资源包初始化失败: {initOp.Error}");
        }

        /// <summary>根据运行模式初始化资源包。</summary>
        private static InitializationOperation InitByPlayMode(string url, string backupUrl)
        {
            switch (PlayMode)
            {
                case EPlayMode.EditorSimulateMode:
                {
                    var buildResult = EditorSimulateModeHelper.SimulateBuild(DefaultPackageName);
                    var fs          = FileSystemParameters.CreateDefaultEditorFileSystemParameters(buildResult.PackageRootDirectory);
                    return s_DefaultPackage.InitializeAsync(new EditorSimulateModeParameters { EditorFileSystemParameters = fs });
                }
                case EPlayMode.OfflinePlayMode:
                {
                    var fs = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
                    return s_DefaultPackage.InitializeAsync(new OfflinePlayModeParameters { BuildinFileSystemParameters = fs });
                }
                case EPlayMode.HostPlayMode:
                {
                    IRemoteServices remote = new RemoteServices(url, backupUrl);
                    var cacheFs   = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remote);
                    var buildInFs = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
                    return s_DefaultPackage.InitializeAsync(new HostPlayModeParameters { BuildinFileSystemParameters = buildInFs, CacheFileSystemParameters = cacheFs });
                }
                case EPlayMode.WebPlayMode:
                {
                    var webFs = FileSystemParameters.CreateDefaultWebServerFileSystemParameters();
                    return s_DefaultPackage.InitializeAsync(new WebPlayModeParameters { WebServerFileSystemParameters = webFs });
                }
                default:
                    return null;
            }
        }

        /// <summary>请求资源版本号，失败返回 null。</summary>
        public static async UniTask<string> RequestVersionAsync()
        {
            var op = s_DefaultPackage.RequestPackageVersionAsync();
            await op;
            return op.Status == EOperationStatus.Succeed ? op.PackageVersion : null;
        }

        /// <summary>更新资源清单，返回是否成功。</summary>
        public static async UniTask<bool> UpdateManifestAsync(string version)
        {
            var op = s_DefaultPackage.UpdatePackageManifestAsync(version);
            await op;
            return op.Status == EOperationStatus.Succeed;
        }

        /// <summary>创建资源下载器。</summary>
        public static ResourceDownloaderOperation CreateDownloader()
            => s_DefaultPackage.CreateResourceDownloader(s_DownloadingMaxNum, s_FailedTryAgainNum);

        /// <summary>
        /// 加载原始文件字节（用于 DLL/AOT 元数据）。
        /// 本项目将 DLL/AOT 以 .bytes(TextAsset) 形式打包，故通过 LoadAssetAsync&lt;Object&gt; 加载后取 TextAsset.bytes，
        /// 与原 ProcedureCodeHotfix.LoadDll 的加载方式保持一致。
        /// </summary>
        public static UniTask<byte[]> LoadRawFileBytesAsync(string location)
        {
            var tcs    = new UniTaskCompletionSource<byte[]>();
            var handle = s_DefaultPackage.LoadAssetAsync<Object>(location);
            handle.Completed += h => tcs.TrySetResult(h.GetAssetObject<TextAsset>().bytes);
            return tcs.Task;
        }

        /// <summary>
        /// 远端资源服务器定义，用于提供远端资源的下载地址。
        /// 说明：AssetModule 内的 RemoteServices 为其私有嵌套类型，引导期无法访问，故在此复刻一份等价实现。
        /// </summary>
        private class RemoteServices : IRemoteServices
        {
            private string HostServer         { get; }
            private string FallbackHostServer { get; }

            public RemoteServices(string hostServer, string fallbackHostServer)
            {
                HostServer         = hostServer;
                FallbackHostServer = fallbackHostServer;
            }

            public string GetRemoteMainURL(string fileName)     => HostServer + fileName;
            public string GetRemoteFallbackURL(string fileName) => FallbackHostServer + fileName;
        }
    }
}
