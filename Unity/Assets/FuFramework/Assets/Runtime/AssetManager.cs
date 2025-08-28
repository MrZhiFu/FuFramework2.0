using System;
using YooAsset;
using Cysharp.Threading.Tasks;
using FuFramework.Core.Runtime;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace FuFramework.Asset.Runtime
{
    /// <summary>
    /// 资源管理器。
    /// 功能：
    /// 1. 封装了YooAsset的资源管理接口，提供更高级的UniTask异步接口。
    /// 2. 统一从资源配置(AssetSetting.scriptableObject)中读取相关参数配置，传入YooAsset，方便管理。
    /// </summary>
    public partial class AssetManager : MonoSingleton<AssetManager>
    {
        /// <summary>
        /// 资源运行模式。
        /// </summary>
        public EPlayMode PlayMode { get; private set; }

        /// <summary>
        /// 默认资源包名称
        /// </summary>
        public string DefaultPackageName { get; private set; }

        /// <summary>
        /// 资源下载最大并发数量
        /// </summary>
        public int DownloadingMaxNum { get; private set; }

        /// <summary>
        /// 资源下载失败重试次数
        /// </summary>
        public int FailedTryAgainNum { get; private set; }

        /// <summary>
        /// YooAsset异步系统参数-每帧执行消耗的最大时间切片（单位：毫秒）
        /// </summary>
        public int AsyncSystemMaxSlicePerFrame { get; private set; }
        
        /// <summary>
        /// 初始化
        /// </summary>
        protected override void Init()
        {
            // 获取资源模块配置数据
            var assetSetting = ModuleSetting.Runtime.ModuleSetting.Instance.AssetSetting;
            if (!assetSetting) throw new FuException("资源模块配置数据为空!");

            PlayMode                    = assetSetting.PlayMode;
            DefaultPackageName          = assetSetting.DefaultPackageName;
            DownloadingMaxNum           = assetSetting.DownloadingMaxNum;
            FailedTryAgainNum           = assetSetting.FailedTryAgainNum;
            AsyncSystemMaxSlicePerFrame = assetSetting.AsyncSystemMaxSlicePerFrame;

#if !UNITY_EDITOR
            if (PlayMode == EPlayMode.EditorSimulateMode)
            {
                PlayMode = EPlayMode.HostPlayMode;
            }
#if UNITY_WEBGL
            PlayMode = EPlayMode.WebPlayMode;
#endif
#endif
            Log.Info($"资源系统运行模式：{PlayMode}");

            BetterStreamingAssets.Initialize();
            
            YooAssets.Initialize();
            YooAssets.SetOperationSystemMaxTimeSlice(AsyncSystemMaxSlicePerFrame); // 设置异步系统参数，每帧执行消耗的最大时间切片（单位：毫秒）
     
            Log.Info("资源系统初始化完毕！");
        }
        
        #region 异步加载资源

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <returns></returns>
        public UniTask<AssetHandle> LoadAssetAsync(AssetInfo assetInfo)
        {
            var taskCompletionSource = new UniTaskCompletionSource<AssetHandle>();
            var assetHandle          = YooAssets.LoadAssetAsync(assetInfo);
            assetHandle.Completed += handle => { taskCompletionSource.TrySetResult(handle); };
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="type">资源类型</param>
        /// <returns></returns>
        public UniTask<AssetHandle> LoadAssetAsync(string path, Type type)
        {
            var taskCompletionSource = new UniTaskCompletionSource<AssetHandle>();
            var assetHandle          = YooAssets.LoadAssetAsync(path, type);
            assetHandle.Completed += handle => { taskCompletionSource.TrySetResult(handle); };
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// 异步加载全部资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        public UniTask<AllAssetsHandle> LoadAllAssetsAsync<T>(string path) where T : Object
        {
            var taskCompletionSource = new UniTaskCompletionSource<AllAssetsHandle>();
            var assetHandle          = YooAssets.LoadAllAssetsAsync<T>(path);
            assetHandle.Completed += handle => { taskCompletionSource.TrySetResult(handle); };
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// 异步加载全部资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="type">资源类型</param>
        /// <returns></returns>
        public UniTask<AllAssetsHandle> LoadAllAssetsAsync(string path, Type type)
        {
            var taskCompletionSource = new UniTaskCompletionSource<AllAssetsHandle>();
            var assetHandle          = YooAssets.LoadAllAssetsAsync(path, type);
            assetHandle.Completed += handle => { taskCompletionSource.TrySetResult(handle); };
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// 异步加载资源包内所有资源对象
        /// </summary>
        /// <param name="path">资源的定位地址</param>
        public UniTask<AllAssetsHandle> LoadAllAssetsAsync(string path)
        {
            var taskCompletionSource = new UniTaskCompletionSource<AllAssetsHandle>();
            var assetHandle          = YooAssets.LoadAllAssetsAsync(path);
            assetHandle.Completed += handle => { taskCompletionSource.TrySetResult(handle); };
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// 异步加载资源包内所有资源对象
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        public UniTask<AllAssetsHandle> LoadAllAssetsAsync(AssetInfo assetInfo)
        {
            var taskCompletionSource = new UniTaskCompletionSource<AllAssetsHandle>();
            var assetHandle          = YooAssets.LoadAllAssetsAsync(assetInfo);
            assetHandle.Completed += handle => { taskCompletionSource.TrySetResult(handle); };
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// 异步加载子资源对象
        /// </summary>
        /// <param name="path">资源的定位地址</param>
        public SubAssetsHandle LoadSubAssetsAsync(string path) => YooAssets.LoadSubAssetsAsync(path);


        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        public UniTask<AssetHandle> LoadAssetAsync(string path)
        {
            var taskCompletionSource = new UniTaskCompletionSource<AssetHandle>();
            var assetHandle          = YooAssets.LoadAssetAsync(path);
            assetHandle.Completed += handle => { taskCompletionSource.TrySetResult(handle); };
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <typeparam name="T">资源类型</typeparam>
        /// <returns></returns>
        public UniTask<AssetHandle> LoadAssetAsync<T>(string path) where T : Object
        {
            var taskCompletionSource = new UniTaskCompletionSource<AssetHandle>();
            var assetHandle          = YooAssets.LoadAssetAsync<T>(path);

            void OnAssetHandleOnCompleted(AssetHandle handle)
            {
                taskCompletionSource.TrySetResult(handle);
            }

            assetHandle.Completed += OnAssetHandleOnCompleted;
            return taskCompletionSource.Task;
        }

        #endregion

        #region 同步加载资源

        /// <summary>
        /// 同步加载资源包内所有资源对象
        /// </summary>
        /// <param name="path">资源的定位地址</param>
        public AllAssetsHandle LoadAllAssetsSync(string path) => YooAssets.LoadAllAssetsSync(path);

        /// <summary>
        /// 同步加载资源包内所有资源对象
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="path">资源的定位地址</param>
        public AllAssetsHandle LoadAllAssetsSync<T>(string path) where T : Object => YooAssets.LoadAllAssetsSync<T>(path);

        /// <summary>
        /// 同步加载资源包内所有资源对象
        /// </summary>
        /// <param name="path">资源的定位地址</param>
        /// <param name="type">子对象类型</param>
        public AllAssetsHandle LoadAllAssetsSync(string path, Type type) => YooAssets.LoadAllAssetsSync(path, type);

        /// <summary>
        /// 同步加载包内全部资源对象
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <returns></returns>
        public AllAssetsHandle LoadAllAssetsSync(AssetInfo assetInfo) => YooAssets.LoadAllAssetsSync(assetInfo);

        /// <summary>
        /// 同步加载子资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        public SubAssetsHandle LoadSubAssetSync(string path) => YooAssets.LoadSubAssetsSync(path);

        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        public AssetHandle LoadAssetSync(string path) => YooAssets.LoadAssetSync(path);

        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="type"></param>
        /// <returns></returns>
        public AssetHandle LoadAssetSync(string path, Type type) => YooAssets.LoadAssetSync(path, type);

        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <returns></returns>
        public AssetHandle LoadAssetSync(AssetInfo assetInfo) => YooAssets.LoadAssetSync(assetInfo);

        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        public AssetHandle LoadAssetSync<T>(string path) where T : Object => YooAssets.LoadAssetSync<T>(path);

        #endregion

        #region 加载场景

        /// <summary>
        /// 异步加载场景
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="sceneMode">场景模式</param>
        /// <param name="activateOnLoad">是否加载完成自动激活</param>
        /// <returns></returns>
        public UniTask<SceneHandle> LoadSceneAsync(string path, LoadSceneMode sceneMode, bool activateOnLoad = true)
        {
            var taskCompletionSource = new UniTaskCompletionSource<SceneHandle>();
            var sceneHandle          = YooAssets.LoadSceneAsync(path, sceneMode, LocalPhysicsMode.None, !activateOnLoad);
            sceneHandle.Completed += handle => { taskCompletionSource.TrySetResult(handle); };
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// 异步加载场景
        /// </summary>
        /// <param name="assetInfo">资源路径</param>
        /// <param name="sceneMode">场景模式</param>
        /// <param name="activateOnLoad">是否加载完成自动激活</param>
        /// <returns></returns>
        public UniTask<SceneHandle> LoadSceneAsync(AssetInfo assetInfo, LoadSceneMode sceneMode, bool activateOnLoad = true)
        {
            var taskCompletionSource = new UniTaskCompletionSource<SceneHandle>();
            var sceneHandle          = YooAssets.LoadSceneAsync(assetInfo, sceneMode, LocalPhysicsMode.None, !activateOnLoad);
            sceneHandle.Completed += handle => { taskCompletionSource.TrySetResult(handle); };
            return taskCompletionSource.Task;
        }

        #endregion

        #region 异步加载子资源对象

        /// <summary>
        /// 异步加载子资源对象
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <returns></returns>
        public UniTask<SubAssetsHandle> LoadSubAssetsAsync(AssetInfo assetInfo)
        {
            var taskCompletionSource = new UniTaskCompletionSource<SubAssetsHandle>();
            var assetHandle          = YooAssets.LoadSubAssetsAsync(assetInfo);
            assetHandle.Completed += handle => { taskCompletionSource.TrySetResult(handle); };
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// 异步加载子资源对象
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="type"></param>
        /// <returns></returns>
        public UniTask<SubAssetsHandle> LoadSubAssetsAsync(string path, Type type)
        {
            var taskCompletionSource = new UniTaskCompletionSource<SubAssetsHandle>();
            var assetHandle          = YooAssets.LoadSubAssetsAsync(path, type);
            assetHandle.Completed += handle => { taskCompletionSource.TrySetResult(handle); };
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// 异步加载子资源对象
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        public UniTask<SubAssetsHandle> LoadSubAssetsAsync<T>(string path) where T : Object
        {
            var taskCompletionSource = new UniTaskCompletionSource<SubAssetsHandle>();
            var assetHandle          = YooAssets.LoadSubAssetsAsync<T>(path);
            assetHandle.Completed += handle => { taskCompletionSource.TrySetResult(handle); };
            return taskCompletionSource.Task;
        }

        #endregion

        #region 异步加载子资源对象

        /// <summary>
        /// 同步加载子资源对象
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <returns></returns>
        public SubAssetsHandle LoadSubAssetSync(AssetInfo assetInfo) => YooAssets.LoadSubAssetsSync(assetInfo);

        /// <summary>
        /// 同步加载子资源对象
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="type"></param>
        /// <returns></returns>
        public SubAssetsHandle LoadSubAssetSync(string path, Type type) => YooAssets.LoadSubAssetsSync(path, type);

        /// <summary>
        /// 同步加载子资源对象
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        public SubAssetsHandle LoadSubAssetSync<T>(string path) where T : Object => YooAssets.LoadSubAssetsSync<T>(path);

        #endregion

        #region 异步加载原生文件

        /// <summary>
        /// 异步加载原生文件
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <returns></returns>
        public UniTask<RawFileHandle> LoadRawFileAsync(AssetInfo assetInfo)
        {
            var taskCompletionSource = new UniTaskCompletionSource<RawFileHandle>();
            var assetHandle          = YooAssets.LoadRawFileAsync(assetInfo);
            assetHandle.Completed += handle => { taskCompletionSource.TrySetResult(handle); };
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// 异步加载原生文件
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        public UniTask<RawFileHandle> LoadRawFileAsync(string path)
        {
            var taskCompletionSource = new UniTaskCompletionSource<RawFileHandle>();
            var assetHandle          = YooAssets.LoadRawFileAsync(path);
            assetHandle.Completed += handle => { taskCompletionSource.TrySetResult(handle); };
            return taskCompletionSource.Task;
        }

        #endregion

        #region 同步加载原生文件

        /// <summary>
        /// 同步加载原生文件
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <returns></returns>
        public RawFileHandle LoadRawFileSync(AssetInfo assetInfo) => YooAssets.LoadRawFileSync(assetInfo);

        /// <summary>
        /// 同步加载原生文件
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        public RawFileHandle LoadRawFileSync(string path) => YooAssets.LoadRawFileSync(path);

        #endregion
        
        #region 资源包

        /// <summary>
        /// 初始化资源包。
        /// </summary>
        /// <param name="packageName">包名称</param>
        /// <param name="downloadURL">热更资源包下载URL</param>
        /// <param name="fallbackDownloadURL">备用热更资源包下载URL</param>
        /// <param name="isDefaultPackage">是否是默认包，默认为true</param>
        /// <returns></returns>
        public UniTask<bool> InitPackageAsync(string packageName, string downloadURL = null, string fallbackDownloadURL = null, bool isDefaultPackage = true)
        {
            FuGuard.NotNull(packageName, nameof(packageName));
            
            // 创建默认的资源包
            var resourcePackage = TryGetPackage(packageName);
            if (resourcePackage == null)
            {
                resourcePackage = CreatePackage(packageName);
                if (isDefaultPackage) 
                    SetDefaultPackage(resourcePackage);// 设置该资源包为默认的资源包
            }

            // 新建一个任务，包装初始化操作
            var taskCompletionSource = new UniTaskCompletionSource<bool>();
            var initHandler = CreateInitHandler(resourcePackage, downloadURL, fallbackDownloadURL);
            if (initHandler == null) throw new FuException($"初始化资源包失败：{packageName}");

            initHandler.Completed += asyncOperationBase =>
            {
                if (asyncOperationBase.Error == null && asyncOperationBase.Status == EOperationStatus.Succeed && asyncOperationBase.IsDone)
                    taskCompletionSource.TrySetResult(true);
                else
                    taskCompletionSource.TrySetException(new Exception(asyncOperationBase.Error));
            };
            return taskCompletionSource.Task;
        }
        
        /// <summary>
        /// 创建资源包
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        /// <returns></returns>
        public ResourcePackage CreatePackage(string packageName) => YooAssets.CreatePackage(packageName);

        /// <summary>
        /// 尝试获取资源包
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        /// <returns></returns>
        public ResourcePackage TryGetPackage(string packageName) => YooAssets.TryGetPackage(packageName);

        /// <summary>
        /// 检查资源包是否存在
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        /// <returns></returns>
        public bool HasPackage(string packageName) => YooAssets.TryGetPackage(packageName) != null;

        /// <summary>
        /// 获取资源包
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        /// <returns></returns>
        public ResourcePackage GetPackage(string packageName) => YooAssets.GetPackage(packageName);
        
        /// <summary>
        /// 设置默认资源包
        /// </summary>
        /// <param name="resourcePackage">资源信息</param>
        /// <returns></returns>
        public void SetDefaultPackage(ResourcePackage resourcePackage) => YooAssets.SetDefaultPackage(resourcePackage);

        /// <summary>
        /// 设置默认资源包
        /// </summary>
        /// <returns></returns>
        public ResourceDownloaderOperation CreateResourceDownloader() => YooAssets.CreateResourceDownloader(DownloadingMaxNum, FailedTryAgainNum);
        
        #endregion

        #region 卸载资源

        /// <summary>
        /// 卸载资源
        /// </summary>
        /// <param name="assetPath">资源路径</param>
        public void UnloadAsset(string assetPath)
        {
            FuGuard.NotNull(assetPath, nameof(assetPath));
            var package = YooAssets.GetPackage(DefaultPackageName);
            package.TryUnloadUnusedAsset(assetPath);
        }

        /// <summary>
        /// 卸载资源
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        /// <param name="assetPath">资源路径</param>
        public void UnloadAsset(string packageName, string assetPath)
        {
            FuGuard.NotNull(packageName, nameof(packageName));
            FuGuard.NotNull(assetPath,   nameof(assetPath));
            var package = YooAssets.GetPackage(packageName);
            package.TryUnloadUnusedAsset(assetPath);
        }
        
        /// <summary>
        /// 强制回收所有资源
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        public void UnloadAllAssetsAsync(string packageName)
        {
            FuGuard.NotNull(packageName, nameof(packageName));
            var package = YooAssets.GetPackage(packageName);
            package.UnloadAllAssetsAsync();
        }

        /// <summary>
        /// 卸载无用资源
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        public void UnloadUnusedAssetsAsync(string packageName)
        {
            FuGuard.NotNull(packageName, nameof(packageName));
            var package = YooAssets.GetPackage(packageName);
            package.UnloadUnusedAssetsAsync();
        }

        /// <summary>
        /// 清理所有资源
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        public void ClearAllBundleFilesAsync(string packageName)
        {
            FuGuard.NotNull(packageName, nameof(packageName));
            var package = YooAssets.GetPackage(packageName);
            package.ClearCacheFilesAsync(EFileClearMode.ClearAllBundleFiles);
        }

        /// <summary>
        /// 清理无用资源
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        public void ClearUnusedBundleFilesAsync(string packageName)
        {
            FuGuard.NotNull(packageName, nameof(packageName));
            var package = YooAssets.GetPackage(packageName);
            package.ClearCacheFilesAsync(EFileClearMode.ClearUnusedBundleFiles);
        }

        #endregion

        #region Get

        /// <summary>
        /// 是否需要下载
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <returns></returns>
        public bool IsNeedDownload(AssetInfo assetInfo) => YooAssets.IsNeedDownloadFromRemote(assetInfo);

        /// <summary>
        /// 是否需要下载
        /// </summary>
        /// <param name="path">资源地址</param>
        /// <returns></returns>
        public bool IsNeedDownload(string path) => YooAssets.IsNeedDownloadFromRemote(path);

        /// <summary>
        /// 获取资源信息
        /// </summary>
        /// <param name="assetTags">资源标签列表</param>
        /// <returns></returns>
        public AssetInfo[] GetAssetInfos(string[] assetTags) => YooAssets.GetAssetInfos(assetTags);

        /// <summary>
        /// 获取资源信息
        /// </summary>
        /// <param name="assetTag">资源标签</param>
        /// <returns></returns>
        public AssetInfo[] GetAssetInfos(string assetTag) => YooAssets.GetAssetInfos(assetTag);

        /// <summary>
        /// 获取资源信息
        /// </summary>
        public AssetInfo GetAssetInfo(string path) => YooAssets.GetAssetInfo(path);

        /// <summary>
        /// 检查指定的资源路径是否有效。
        /// </summary>
        /// <param name="path">要检查的资源路径。</param>
        /// <returns>如果资源路径有效，则返回 true；否则返回 false。</returns>
        public bool HasAssetPath(string path) => YooAssets.CheckLocationValid(path);
        
        #endregion
    }
}