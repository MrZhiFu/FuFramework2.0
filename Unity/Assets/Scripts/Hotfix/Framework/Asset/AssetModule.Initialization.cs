using Hotfix.Framework.Core;
using YooAsset;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Asset
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
        private InitializePackageOperation InitPackage(ResourcePackage resPackage, string downloadURL, string downloadBackupURL)
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
        private InitializePackageOperation InitInEditorSimulateMode(ResourcePackage resPackage)
        {
            resPackage.NotNull(nameof(resPackage));
            var simulateBuildResult = EditorSimulateBuildInvoker.Build(DefaultPackageName, (int)EBundleType.VirtualAssetBundle);
            var packageRoot         = simulateBuildResult.PackageRootDirectory;
            var editorFileSystem    = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot);
            var initOptions = new EditorSimulateModeOptions
            {
                EditorFileSystemParameters = editorFileSystem
            };
            return resPackage.InitializePackageAsync(initOptions);
        }

        /// <summary>
        /// 初始化为离线单机运行模式。
        /// DefaultBuildinFile => StreamingAssets目录。
        /// </summary>
        /// <param name="resPackage">资源包</param>
        /// <returns></returns>
        private InitializePackageOperation InitInOfflinePlayMode(ResourcePackage resPackage)
        {
            resPackage.NotNull(nameof(resPackage));
            var buildInFileSystem = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters();
            var initOptions = new OfflinePlayModeOptions
            {
                BuiltinFileSystemParameters = buildInFileSystem
            };
            return resPackage.InitializePackageAsync(initOptions);
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
        private InitializePackageOperation InitInHostPlayMode(ResourcePackage resPackage, string downloadURL, string downloadBackupURL)
        {
            resPackage.NotNull(nameof(resPackage));
            downloadURL.NotNull(nameof(downloadURL));
            downloadBackupURL.NotNull(nameof(downloadBackupURL));

            IRemoteService remoteService = new RemoteServices(downloadURL, downloadBackupURL);

            var cacheFileSystem   = FileSystemParameters.CreateDefaultSandboxFileSystemParameters(remoteService);
            var buildInFileSystem = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters();

            var initOptions = new HostPlayModeOptions
            {
                BuiltinFileSystemParameters = buildInFileSystem,
                CacheFileSystemParameters   = cacheFileSystem
            };
            return resPackage.InitializePackageAsync(initOptions);
        }

        /// <summary>
        /// 初始化为Web运行模式
        /// </summary>
        /// <param name="resPackage">资源包</param>
        /// <param name="downloadURL">资源下载地址</param>
        /// <param name="downloadBackupURL">资源备用下载地址</param>
        /// <returns></returns>
        private InitializePackageOperation InitInWebPlayMode(ResourcePackage resPackage, string downloadURL, string downloadBackupURL)
        {
            resPackage.NotNull(nameof(resPackage));
            downloadURL.NotNull(nameof(downloadURL));
            downloadBackupURL.NotNull(nameof(downloadBackupURL));

            var                  initOptions   = new WebPlayModeOptions();
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
            initOptions.WebServerFileSystemParameters = webFileSystem;
            return resPackage.InitializePackageAsync(initOptions);
        }
    }
}