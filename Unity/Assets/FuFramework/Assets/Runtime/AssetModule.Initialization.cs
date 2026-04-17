using FuFramework.Core.Runtime;
using YooAsset;

// ReSharper disable once CheckNamespace
namespace FuFramework.Asset.Runtime
{
    /// <summary>
    /// 初始化加载模式
    /// </summary>
    public partial class AssetModule
    {
        /// <summary>
        /// 根据运行模式初始化资源包。
        /// </summary>
        /// <param name="resPackage">资源包</param>
        /// <param name="downloadURL">资源下载地址</param>
        /// <param name="downloadBackupURL">资源备用下载地址</param>
        /// <returns></returns>
        private InitializationOperation InitPackage(ResourcePackage resPackage, string downloadURL, string downloadBackupURL)
        {
            return PlayMode switch
            {
                EPlayMode.EditorSimulateMode => InitInEditorSimulateMode(resPackage),                           // 编辑器下的模拟模式
                EPlayMode.OfflinePlayMode    => InitInOfflinePlayMode(resPackage),                              // 单机运行模式
                EPlayMode.HostPlayMode       => InitInHostPlayMode(resPackage, downloadURL, downloadBackupURL), // 联机运行模式
                EPlayMode.WebPlayMode        => InitInWebPlayMode(resPackage, downloadURL, downloadBackupURL),  // WebGL运行模式
                _                            => null
            };
        }

        /// <summary>
        /// 初始化为编辑器下模拟模式。
        /// DefaultEditorFile => Application.dataPath目录
        /// </summary>
        /// <param name="resPackage">资源包</param>
        /// <returns></returns>
        private InitializationOperation InitInEditorSimulateMode(ResourcePackage resPackage)
        {
            FuGuard.NotNull(resPackage, nameof(resPackage));
            var simulateBuildResult = EditorSimulateModeHelper.SimulateBuild(DefaultPackageName);
            var packageRoot         = simulateBuildResult.PackageRootDirectory;
            var editorFileSystem    = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot);
            var initParameters = new EditorSimulateModeParameters
            {
                EditorFileSystemParameters = editorFileSystem
            };
            return resPackage.InitializeAsync(initParameters);
        }

        /// <summary>
        /// 初始化为离线单机运行模式。
        /// DefaultBuildinFile => StreamingAssets目录。
        /// </summary>
        /// <param name="resPackage">资源包</param>
        /// <returns></returns>
        private InitializationOperation InitInOfflinePlayMode(ResourcePackage resPackage)
        {
            FuGuard.NotNull(resPackage, nameof(resPackage));
            var buildInFileSystem = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
            var initParameters = new OfflinePlayModeParameters
            {
                BuildinFileSystemParameters = buildInFileSystem
            };
            return resPackage.InitializeAsync(initParameters);
        }

        /// <summary>
        /// 初始化为联机运行模式。
        /// DefaultCacheFile => PC/Linux(Application.dataPath), Mac/Android/iOS(Application.persistentDataPath)。
        /// DefaultBuildinFile => StreamingAssets目录。
        /// </summary>
        /// <param name="resPackage">资源包</param>
        /// <param name="downloadURL">资源下载地址</param>
        /// <param name="downloadBackupURL">资源备用下载地址</param>
        /// <returns></returns>
        private InitializationOperation InitInHostPlayMode(ResourcePackage resPackage, string downloadURL, string downloadBackupURL)
        {
            FuGuard.NotNull(resPackage,        nameof(resPackage));
            FuGuard.NotNull(downloadURL,       nameof(downloadURL));
            FuGuard.NotNull(downloadBackupURL, nameof(downloadBackupURL));

            IRemoteServices remoteServices = new RemoteServices(downloadURL, downloadBackupURL);

            var cacheFileSystem   = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices);
            var buildInFileSystem = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();

            var initParameters = new HostPlayModeParameters
            {
                BuildinFileSystemParameters = buildInFileSystem,
                CacheFileSystemParameters   = cacheFileSystem
            };
            return resPackage.InitializeAsync(initParameters);
        }

        /// <summary>
        /// 初始化为Web运行模式
        /// </summary>
        /// <param name="resPackage">资源包</param>
        /// <param name="downloadURL">资源下载地址</param>
        /// <param name="downloadBackupURL">资源备用下载地址</param>
        /// <returns></returns>
        private InitializationOperation InitInWebPlayMode(ResourcePackage resPackage, string downloadURL, string downloadBackupURL)
        {
            FuGuard.NotNull(resPackage,        nameof(resPackage));
            FuGuard.NotNull(downloadURL,       nameof(downloadURL));
            FuGuard.NotNull(downloadBackupURL, nameof(downloadBackupURL));

            var                  initParameters = new WebPlayModeParameters();
            FileSystemParameters webFileSystem  = null;

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
            return resPackage.InitializeAsync(initParameters);
        }
    }
}