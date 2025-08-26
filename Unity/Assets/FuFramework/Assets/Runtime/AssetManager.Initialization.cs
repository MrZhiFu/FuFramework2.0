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
        /// 根据运行模式创建初始化操作数据
        /// </summary>
        /// <returns></returns>
        private InitializationOperation CreateInitOperationHandler(ResourcePackage resourcePackage, string hostServerURL, string fallbackHostServerURL)
        {
            return PlayMode switch
            {
                EPlayMode.EditorSimulateMode => InitAsEditorSimulateMode(resourcePackage),                                 // 编辑器下的模拟模式
                EPlayMode.OfflinePlayMode    => InitAsOfflinePlayMode(resourcePackage),                                    // 单机运行模式
                EPlayMode.HostPlayMode       => InitAsHostPlayMode(resourcePackage, hostServerURL, fallbackHostServerURL), // 联机运行模式
                EPlayMode.WebPlayMode        => InitAsWebPlayMode(resourcePackage, hostServerURL, fallbackHostServerURL),  // WebGL运行模式
                _                            => null
            };
        }

        /// <summary>
        /// 初始化为编辑器下模拟模式
        /// </summary>
        /// <param name="resourcePackage"></param>
        /// <returns></returns>
        private InitializationOperation InitAsEditorSimulateMode(ResourcePackage resourcePackage)
        {
            var initParameters = new EditorSimulateModeParameters();
            var simulateBuildResult = EditorSimulateModeHelper.SimulateBuild(DefaultPackageName);
            var packageRoot = simulateBuildResult.PackageRootDirectory;
            var editorFileSystem  = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot);
            initParameters.EditorFileSystemParameters = editorFileSystem;
            return resourcePackage.InitializeAsync(initParameters);
        }

        /// <summary>
        /// 初始化为单机运行模式
        /// </summary>
        /// <param name="resourcePackage"></param>
        /// <returns></returns>
        private InitializationOperation InitAsOfflinePlayMode(ResourcePackage resourcePackage)
        {
            var buildInFileSystem = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
            var initParameters = new OfflinePlayModeParameters
            {
                BuildinFileSystemParameters = buildInFileSystem
            };
            return resourcePackage.InitializeAsync(initParameters);
        }

        /// <summary>
        /// 初始化为Web运行模式
        /// </summary>
        /// <param name="resourcePackage"></param>
        /// <param name="hostServerURL"></param>
        /// <param name="fallbackHostServerURL"></param>
        /// <returns></returns>
        private InitializationOperation InitAsWebPlayMode(ResourcePackage resourcePackage, string hostServerURL, string fallbackHostServerURL)
        {
            var initParameters = new WebPlayModeParameters();

            FileSystemParameters webFileSystem = null;

#if UNITY_WEBGL
#if ENABLE_DOUYIN_MINI_GAME
            // 创建字节小游戏文件系统
            if (hostServerURL.IsNullOrWhiteSpace())
            {
                webFileSystem = ByteGameFileSystemCreater.CreateByteGameFileSystemParameters();
            }
            else
            {
                webFileSystem = ByteGameFileSystemCreater.CreateByteGameFileSystemParameters(hostServerURL);
            }
#elif ENABLE_WECHAT_MINI_GAME
            WeChatWASM.WXBase.PreloadConcurrent(10);
            // 创建微信小游戏文件系统
            if (hostServerURL.IsNullOrWhiteSpace())
            {
                webFileSystem = WechatFileSystemCreater.CreateWechatFileSystemParameters();
            }
            else
            {
                webFileSystem = WechatFileSystemCreater.CreateWechatPathFileSystemParameters(hostServerURL);
            }
#else
            // 创建默认WebGL文件系统
            webFileSystem = FileSystemParameters.CreateDefaultWebFileSystemParameters();
#endif
#else
            // webFileSystem = FileSystemParameters.CreateDefaultWebFileSystemParameters(); // TODO
#endif
            // initParameters.WebFileSystemParameters = webFileSystem;
            return resourcePackage.InitializeAsync(initParameters);
        }

        /// <summary>
        /// 初始化为联机运行模式
        /// </summary>
        /// <param name="resourcePackage"></param>
        /// <param name="hostServerURL"></param>
        /// <param name="fallbackHostServerURL"></param>
        /// <returns></returns>
        private InitializationOperation InitAsHostPlayMode(ResourcePackage resourcePackage, string hostServerURL, string fallbackHostServerURL)
        {
            IRemoteServices remoteServices = new RemoteServices(hostServerURL, fallbackHostServerURL);

            var cacheFileSystem   = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices);
            var buildInFileSystem = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();

            var initParameters = new HostPlayModeParameters
            {
                BuildinFileSystemParameters = buildInFileSystem,
                CacheFileSystemParameters   = cacheFileSystem
            };
            return resourcePackage.InitializeAsync(initParameters);
        }
    }
}