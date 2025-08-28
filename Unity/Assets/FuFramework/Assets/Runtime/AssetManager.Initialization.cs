using FuFramework.Core.Runtime;
using YooAsset;

// ReSharper disable once CheckNamespace
namespace FuFramework.Asset.Runtime
{
    /// <summary>
    /// 初始化加载模式
    /// </summary>
    public partial class AssetManager
    {
        /// <summary>
        /// 根据运行模式创建初始化句柄
        /// </summary>
        /// <returns></returns>
        private InitializationOperation CreateInitHandler(ResourcePackage resourcePackage, string downloadURL, string fallbackDownloadURL)
        {
            return PlayMode switch
            {
                EPlayMode.EditorSimulateMode => InitInEditorSimulateMode(resourcePackage),                             // 编辑器下的模拟模式
                EPlayMode.OfflinePlayMode    => InitInOfflinePlayMode(resourcePackage),                                // 单机运行模式
                EPlayMode.HostPlayMode       => InitInHostPlayMode(resourcePackage, downloadURL, fallbackDownloadURL), // 联机运行模式
                EPlayMode.WebPlayMode        => InitInWebPlayMode(resourcePackage, downloadURL, fallbackDownloadURL),  // WebGL运行模式
                _                            => null
            };
        }

        /// <summary>
        /// 初始化为编辑器下模拟模式
        /// </summary>
        /// <param name="resourcePackage"></param>
        /// <returns></returns>
        private InitializationOperation InitInEditorSimulateMode(ResourcePackage resourcePackage)
        {
            FuGuard.NotNull(resourcePackage, nameof(resourcePackage));
            var simulateBuildResult = EditorSimulateModeHelper.SimulateBuild(DefaultPackageName);
            var packageRoot = simulateBuildResult.PackageRootDirectory;
            var editorFileSystem  = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot);
            var initParameters = new EditorSimulateModeParameters
            {
                EditorFileSystemParameters = editorFileSystem
            };
            return resourcePackage.InitializeAsync(initParameters);
        }

        /// <summary>
        /// 初始化为单机运行模式
        /// </summary>
        /// <param name="resourcePackage"></param>
        /// <returns></returns>
        private InitializationOperation InitInOfflinePlayMode(ResourcePackage resourcePackage)
        {
            FuGuard.NotNull(resourcePackage, nameof(resourcePackage));
            var buildInFileSystem = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
            var initParameters = new OfflinePlayModeParameters
            {
                BuildinFileSystemParameters = buildInFileSystem
            };
            return resourcePackage.InitializeAsync(initParameters);
        }

        /// <summary>
        /// 初始化为联机运行模式
        /// </summary>
        /// <param name="resourcePackage"></param>
        /// <param name="downloadURL"></param>
        /// <param name="fallbackDownloadURL"></param>
        /// <returns></returns>
        private InitializationOperation InitInHostPlayMode(ResourcePackage resourcePackage, string downloadURL, string fallbackDownloadURL)
        {
            FuGuard.NotNull(resourcePackage,     nameof(resourcePackage));
            FuGuard.NotNull(downloadURL,         nameof(downloadURL));
            FuGuard.NotNull(fallbackDownloadURL, nameof(fallbackDownloadURL));
            
            IRemoteServices remoteServices = new RemoteServices(downloadURL, fallbackDownloadURL);

            var cacheFileSystem   = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices);
            var buildInFileSystem = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();

            var initParameters = new HostPlayModeParameters
            {
                BuildinFileSystemParameters = buildInFileSystem,
                CacheFileSystemParameters   = cacheFileSystem
            };
            return resourcePackage.InitializeAsync(initParameters);
        }

        /// <summary>
        /// 初始化为Web运行模式
        /// </summary>
        /// <param name="resourcePackage"></param>
        /// <param name="downloadURL"></param>
        /// <param name="fallbackDownloadURL"></param>
        /// <returns></returns>
        private InitializationOperation InitInWebPlayMode(ResourcePackage resourcePackage, string downloadURL, string fallbackDownloadURL)
        {
            FuGuard.NotNull(resourcePackage,     nameof(resourcePackage));
            FuGuard.NotNull(downloadURL,         nameof(downloadURL));
            FuGuard.NotNull(fallbackDownloadURL, nameof(fallbackDownloadURL));
            
            var initParameters = new WebPlayModeParameters();
            FileSystemParameters webFileSystem = null;

#if UNITY_WEBGL
    #if ENABLE_DOUYIN_MINI_GAME
            // 创建字节小游戏文件系统
            if (downloadURL.IsNullOrWhiteSpace())
                webFileSystem = ByteGameFileSystemCreater.CreateByteGameFileSystemParameters();
            else
                webFileSystem = ByteGameFileSystemCreater.CreateByteGameFileSystemParameters(downloadURL);
    #elif ENABLE_WECHAT_MINI_GAME
            // 创建微信小游戏文件系统
            WeChatWASM.WXBase.PreloadConcurrent(10);
            if (downloadURL.IsNullOrWhiteSpace())
                webFileSystem = WechatFileSystemCreater.CreateWechatFileSystemParameters();
            else
                webFileSystem = WechatFileSystemCreater.CreateWechatPathFileSystemParameters(downloadURL);
    #else
            // 创建默认WebGL文件系统
            webFileSystem = FileSystemParameters.CreateDefaultWebFileSystemParameters();
    #endif
#else
            webFileSystem = FileSystemParameters.CreateDefaultWebServerFileSystemParameters();
#endif
            initParameters.WebServerFileSystemParameters = webFileSystem;
            return resourcePackage.InitializeAsync(initParameters);
        }
    }
}