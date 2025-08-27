using System.Collections;
using YooAsset;
using Cysharp.Threading.Tasks;
using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Asset.Runtime
{
    /// <summary>
    /// 初始化加载模式
    /// </summary>
    public partial class AssetManager
    {
        /// <summary>
        /// 根据运行模式创建初始化任务
        /// </summary>
        /// <returns></returns>
        private IEnumerator CreateInitPackageTask(ResourcePackage resourcePackage, string hostServerURL, string fallbackHostServerURL)
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
        private IEnumerator InitAsEditorSimulateMode(ResourcePackage resourcePackage)
        {
            Log.Info("初始化为编辑器下模拟模式");
            var initParameters = new EditorSimulateModeParameters();
            var simulateBuildResult = EditorSimulateModeHelper.SimulateBuild(DefaultPackageName);
            var packageRoot = simulateBuildResult.PackageRootDirectory;
            var editorFileSystemParams  = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot);
            initParameters.EditorFileSystemParameters = editorFileSystemParams;
            yield return resourcePackage.InitializeAsync(initParameters);
        }

        /// <summary>
        /// 初始化为单机运行模式
        /// </summary>
        /// <param name="resourcePackage"></param>
        /// <returns></returns>
        private IEnumerator InitAsOfflinePlayMode(ResourcePackage resourcePackage)
        {
            Log.Info("初始化为单机运行模式");
            var buildInFileSystemParams = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
            var initParameters = new OfflinePlayModeParameters
            {
                BuildinFileSystemParameters = buildInFileSystemParams
            };
            yield return resourcePackage.InitializeAsync(initParameters);
        }

        /// <summary>
        /// 初始化为联机运行模式
        /// </summary>
        /// <param name="resourcePackage"></param>
        /// <param name="hostServerURL"></param>
        /// <param name="fallbackHostServerURL"></param>
        /// <returns></returns>
        private IEnumerator InitAsHostPlayMode(ResourcePackage resourcePackage, string hostServerURL, string fallbackHostServerURL)
        {
            FuGuard.NotNullOrEmpty(hostServerURL, nameof(hostServerURL));
            FuGuard.NotNullOrEmpty(fallbackHostServerURL, nameof(fallbackHostServerURL));
           
            Log.Info("初始化为联机运行模式");
            
            IRemoteServices remoteServices = new RemoteServices(hostServerURL, fallbackHostServerURL);

            var cacheFileSystemParams   = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices);
            var buildInFileSystemParams = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();

            var initParameters = new HostPlayModeParameters
            {
                BuildinFileSystemParameters = buildInFileSystemParams,
                CacheFileSystemParameters   = cacheFileSystemParams
            };
            yield return resourcePackage.InitializeAsync(initParameters);
        }

        /// <summary>
        /// 初始化为Web运行模式
        /// </summary>
        /// <param name="resourcePackage"></param>
        /// <param name="hostServerURL"></param>
        /// <param name="fallbackHostServerURL"></param>
        /// <returns></returns>
        private IEnumerator InitAsWebPlayMode(ResourcePackage resourcePackage, string hostServerURL, string fallbackHostServerURL)
        { 
            FuGuard.NotNullOrEmpty(hostServerURL, nameof(hostServerURL));
            FuGuard.NotNullOrEmpty(fallbackHostServerURL, nameof(fallbackHostServerURL));
            
            Log.Info("初始化为Web运行模式");
            IRemoteServices remoteServices = new RemoteServices(hostServerURL, fallbackHostServerURL);
            var webServerFileSystemParams = FileSystemParameters.CreateDefaultWebServerFileSystemParameters();
            var webRemoteFileSystemParams = FileSystemParameters.CreateDefaultWebRemoteFileSystemParameters(remoteServices); //支持跨域下载
    
            var initParameters = new WebPlayModeParameters
            {
                WebServerFileSystemParameters = webServerFileSystemParams,
                WebRemoteFileSystemParameters = webRemoteFileSystemParams
            };

            yield return resourcePackage.InitializeAsync(initParameters);
        }
    }
}