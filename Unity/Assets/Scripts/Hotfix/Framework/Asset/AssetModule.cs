using System;
using YooAsset;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Core;
using AOT.Bootstrap;
using AOT.Framework.ModuleSetting.Runtime.Asset;
using AOT.Framework.ModuleSetting.Runtime;
using AOT.Framework.Core.Log;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Hotfix.Framework.Asset
{
    /// <summary>
    /// 资源管理模块。
    /// 功能：
    ///     1. 封装了YooAsset的资源管理接口，提供更高级的UniTask异步资源加载相关接口。
    ///     2. 统一从资源配置(AssetSetting.scriptableObject)中读取相关参数配置，传入YooAsset，方便管理。
    /// </summary>
    public partial class AssetModule : ModuleBase
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
        /// 资源包是否已经初始化过
        /// </summary>
        private bool PackageInited { get; set; } = false;

        /// <summary>
        /// 初始化
        /// </summary>
        protected internal override void OnInit()
        {
            // 获取资源管理模块配置数据
            var assetSetting = ModuleSetting.Instance.AssetSetting;
            if (!assetSetting) throw new InvalidOperationException("[AssetModule]资源管理模块配置数据为空!");

            PlayMode                    = assetSetting.PlayMode;
            DefaultPackageName          = assetSetting.DefaultPackageName;
            DownloadingMaxNum           = assetSetting.DownloadingMaxNum;
            FailedTryAgainNum           = assetSetting.FailedTryAgainNum;
            AsyncSystemMaxSlicePerFrame = assetSetting.AsyncSystemMaxSlicePerFrame;

            FuLogger.LogInfo($"[AssetModule]资源系统运行模式：{PlayMode}");

            // 初始化YooAsset（引导阶段 BootstrapAssetHelper 可能已初始化，避免二次初始化）
            if (BootstrapContext.YooAssetInitialized)
            {
                // 引导流程已初始化 YooAsset 并创建/初始化默认包，标记为已初始化，避免热更侧重复初始化默认包
                PackageInited = true;
            }
            else
            {
                YooAssets.Initialize();

                // 设置异步系统参数，每帧执行消耗的最大时间切片（单位：毫秒）
                YooAssets.SetAsyncOperationMaxTimeSlice(AsyncSystemMaxSlicePerFrame);

                BootstrapContext.YooAssetInitialized = true;
                BootstrapContext.DefaultPackageName  = DefaultPackageName;
            }

            FuLogger.LogInfo("[AssetModule]资源系统初始化完毕！");
        }

        /// <summary>
        /// 释放
        /// </summary>
        protected internal override void OnDispose()
        {
            UnloadAllAssetsAsync(DefaultPackageName).Forget();
        }


        #region 异步加载资源(推荐使用)

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        public UniTask<AssetHandle> LoadAssetAsync(string path)
        {
            var taskCompletionSource = new UniTaskCompletionSource<AssetHandle>();
            var assetHandle          = GetDefaultPackage().LoadAssetAsync(path);
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
            var assetHandle          = GetDefaultPackage().LoadAssetAsync<T>(path);

            assetHandle.Completed += OnAssetHandleOnCompleted;
            return taskCompletionSource.Task;

            void OnAssetHandleOnCompleted(AssetHandle handle) => taskCompletionSource.TrySetResult(handle);
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
            var assetHandle          = GetDefaultPackage().LoadAssetAsync(path, type);
            assetHandle.Completed += handle => { taskCompletionSource.TrySetResult(handle); };
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <returns></returns>
        public UniTask<AssetHandle> LoadAssetAsync(AssetInfo assetInfo)
        {
            var taskCompletionSource = new UniTaskCompletionSource<AssetHandle>();
            var assetHandle          = GetDefaultPackage().LoadAssetAsync(assetInfo);
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
            var assetHandle          = GetDefaultPackage().LoadAllAssetsAsync<T>(path);
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
            var assetHandle          = GetDefaultPackage().LoadAllAssetsAsync(path, type);
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
            var assetHandle          = GetDefaultPackage().LoadAllAssetsAsync(path);
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
            var assetHandle          = GetDefaultPackage().LoadAllAssetsAsync(assetInfo);
            assetHandle.Completed += handle => { taskCompletionSource.TrySetResult(handle); };
            return taskCompletionSource.Task;
        }

        #endregion

        #region 同步加载资源(不推荐使用)

        /// <summary>
        /// 同步加载资源包内所有资源对象
        /// </summary>
        /// <param name="path">资源的定位地址</param>
        public AllAssetsHandle LoadAllAssetsSync(string path) => GetDefaultPackage().LoadAllAssetsSync(path);

        /// <summary>
        /// 同步加载资源包内所有资源对象
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="path">资源的定位地址</param>
        public AllAssetsHandle LoadAllAssetsSync<T>(string path) where T : Object => GetDefaultPackage().LoadAllAssetsSync<T>(path);

        /// <summary>
        /// 同步加载资源包内所有资源对象
        /// </summary>
        /// <param name="path">资源的定位地址</param>
        /// <param name="type">子对象类型</param>
        public AllAssetsHandle LoadAllAssetsSync(string path, Type type) => GetDefaultPackage().LoadAllAssetsSync(path, type);

        /// <summary>
        /// 同步加载包内全部资源对象
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <returns></returns>
        public AllAssetsHandle LoadAllAssetsSync(AssetInfo assetInfo) => GetDefaultPackage().LoadAllAssetsSync(assetInfo);

        /// <summary>
        /// 同步加载子资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        public SubAssetsHandle LoadSubAssetSync(string path) => GetDefaultPackage().LoadSubAssetsSync(path);

        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        public AssetHandle LoadAssetSync(string path) => GetDefaultPackage().LoadAssetSync(path);

        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="type"></param>
        /// <returns></returns>
        public AssetHandle LoadAssetSync(string path, Type type) => GetDefaultPackage().LoadAssetSync(path, type);

        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <returns></returns>
        public AssetHandle LoadAssetSync(AssetInfo assetInfo) => GetDefaultPackage().LoadAssetSync(assetInfo);

        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        public AssetHandle LoadAssetSync<T>(string path) where T : Object => GetDefaultPackage().LoadAssetSync<T>(path);

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
            var sceneHandle          = GetDefaultPackage().LoadSceneAsync(path, sceneMode, LocalPhysicsMode.None, !activateOnLoad);
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
            var sceneHandle          = GetDefaultPackage().LoadSceneAsync(assetInfo, sceneMode, LocalPhysicsMode.None, !activateOnLoad);
            sceneHandle.Completed += handle => { taskCompletionSource.TrySetResult(handle); };
            return taskCompletionSource.Task;
        }

        #endregion

        #region 异步加载子资源对象

        /// <summary>
        /// 异步加载子资源对象
        /// </summary>
        /// <param name="path">资源的定位地址</param>
        public SubAssetsHandle LoadSubAssetsAsync(string path) => GetDefaultPackage().LoadSubAssetsAsync(path);

        /// <summary>
        /// 异步加载子资源对象
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        public UniTask<SubAssetsHandle> LoadSubAssetsAsync<T>(string path) where T : Object
        {
            var taskCompletionSource = new UniTaskCompletionSource<SubAssetsHandle>();
            var assetHandle          = GetDefaultPackage().LoadSubAssetsAsync<T>(path);
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
            var assetHandle          = GetDefaultPackage().LoadSubAssetsAsync(path, type);
            assetHandle.Completed += handle => { taskCompletionSource.TrySetResult(handle); };
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// 异步加载子资源对象
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <returns></returns>
        public UniTask<SubAssetsHandle> LoadSubAssetsAsync(AssetInfo assetInfo)
        {
            var taskCompletionSource = new UniTaskCompletionSource<SubAssetsHandle>();
            var assetHandle          = GetDefaultPackage().LoadSubAssetsAsync(assetInfo);
            assetHandle.Completed += handle => { taskCompletionSource.TrySetResult(handle); };
            return taskCompletionSource.Task;
        }

        #endregion

        #region 异步加载原生文件

        /// <summary>
        /// 异步加载原生文件
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <returns></returns>
        public UniTask<BundleFileHandle> LoadRawFileAsync(AssetInfo assetInfo)
        {
            var taskCompletionSource = new UniTaskCompletionSource<BundleFileHandle>();
            var assetHandle          = GetDefaultPackage().LoadBundleFileAsync(assetInfo);
            assetHandle.Completed += handle => { taskCompletionSource.TrySetResult(handle); };
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// 异步加载原生文件
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        public UniTask<BundleFileHandle> LoadRawFileAsync(string path)
        {
            var taskCompletionSource = new UniTaskCompletionSource<BundleFileHandle>();
            var assetHandle          = GetDefaultPackage().LoadBundleFileAsync(path);
            assetHandle.Completed += handle => { taskCompletionSource.TrySetResult(handle); };
            return taskCompletionSource.Task;
        }

        #endregion

        #region 同步加载原生文件(不推荐使用)

        /// <summary>
        /// 同步加载原生文件
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <returns></returns>
        public BundleFileHandle LoadRawFileSync(AssetInfo assetInfo) => GetDefaultPackage().LoadBundleFileSync(assetInfo);

        /// <summary>
        /// 同步加载原生文件
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        public BundleFileHandle LoadRawFileSync(string path) => GetDefaultPackage().LoadBundleFileSync(path);

        #endregion

        #region 实例化游戏物体

        /// <summary>
        /// 异步实例化实体(推荐使用)。
        /// <param name="path">资源路径</param>
        /// </summary>
        /// <returns>实例化后的实体。</returns>
        public async UniTask<GameObject> InstantiateAsync(string path)
        {
            var assetHandle          = await LoadAssetAsync(path);
            var instantiateOperation = assetHandle.InstantiateAsync();
            await instantiateOperation;
            return instantiateOperation.Result;
        }

        /// <summary>
        /// 同步实例化实体(不推荐使用)。
        /// <param name="path">资源路径</param>
        /// </summary>
        /// <returns>实例化后的实体。</returns>
        public GameObject InstantiateSync(string path)
        {
            var assetHandle = LoadAssetSync(path);
            return assetHandle.InstantiateSync();
        }

        #endregion

        #region 资源包

        /// <summary>
        /// 初始化默认资源包。
        /// </summary>
        /// <param name="downloadURL">热更资源包下载URL</param>
        /// <param name="downloadBackupURL">备用热更资源包下载URL</param>
        /// <param name="isDefaultPackage">是否是默认包，默认为true</param>
        /// <returns></returns>
        public UniTask<bool> InitDefaultPackageAsync(string downloadURL = null, string downloadBackupURL = null, bool isDefaultPackage = true)
        {
            return InitPackageAsync(DefaultPackageName, downloadURL, downloadBackupURL, isDefaultPackage);
        }

        /// <summary>
        /// 初始化资源包。
        /// </summary>
        /// <param name="packageName">包名称</param>
        /// <param name="downloadURL">热更资源包下载URL</param>
        /// <param name="downloadBackupURL">备用热更资源包下载URL</param>
        /// <param name="isDefaultPackage">是否是默认包，默认为true</param>
        /// <returns></returns>
        public UniTask<bool> InitPackageAsync(string packageName, string downloadURL = null, string downloadBackupURL = null, bool isDefaultPackage = true)
        {
            packageName.NotNull(nameof(packageName));

            if (PackageInited)
            {
                return UniTask.FromResult(true);
            }

            PackageInited = true;

            // 创建默认的资源包
            var resourcePackage = TryGetPackage(packageName);
            if (resourcePackage == null)
            {
                resourcePackage = CreatePackage(packageName);
                // v3 移除了全局默认包概念，只需创建包即可，通过包名访问
            }

            // 新建一个任务，包装初始化操作
            var taskCompletionSource = new UniTaskCompletionSource<bool>();
            var initHandler          = InitPackage(resourcePackage, downloadURL, downloadBackupURL);
            if (initHandler == null) throw new InvalidOperationException($"初始化资源包失败：{packageName}");

            initHandler.Completed += asyncOperationBase =>
            {
                if (asyncOperationBase.Error == null && asyncOperationBase.Status == EOperationStatus.Succeeded && asyncOperationBase.IsDone)
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
        public ResourcePackage TryGetPackage(string packageName) => YooAssets.TryGetPackage(packageName, out var package) ? package : null;

        /// <summary>
        /// 检查资源包是否存在
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        /// <returns></returns>
        public bool HasPackage(string packageName) => YooAssets.TryGetPackage(packageName, out _);

        /// <summary>
        /// 获取默认资源包
        /// </summary>
        /// <returns></returns>
        public ResourcePackage GetDefaultPackage() => YooAssets.GetPackage(DefaultPackageName);

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
        [System.Obsolete("YooAsset v3 已移除全局默认包概念，此方法不再需要调用。")]
        public void SetDefaultPackage(ResourcePackage resourcePackage) { /* v3 已移除 YooAssets.SetDefaultPackage */ }

        /// <summary>
        /// 设置默认资源包
        /// </summary>
        /// <returns></returns>
        public ResourceDownloaderOperation CreateResourceDownloader() => GetDefaultPackage().CreateResourceDownloader(new ResourceDownloaderOptions(DownloadingMaxNum, FailedTryAgainNum));

        #endregion

        #region 卸载资源

        /// <summary>
        /// 卸载指定资源。
        /// 注意：如果该资源还在被使用，该方法会无效
        /// </summary>
        /// <param name="assetPath">资源路径</param>
        public void UnloadAsset(string assetPath)
        {
            assetPath.NotNull(nameof(assetPath));
            var package = YooAssets.GetPackage(DefaultPackageName);
            package.TryUnloadUnusedAsset(assetPath);
        }

        /// <summary>
        /// 卸载指定资源包下的指定资源。
        /// 注意：如果该资源还在被使用，该方法会无效
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        /// <param name="assetPath">资源路径</param>
        public void UnloadAsset(string packageName, string assetPath)
        {
            packageName.NotNull(nameof(packageName));
            assetPath.NotNull(  nameof(assetPath));
            var package = YooAssets.GetPackage(packageName);
            package.TryUnloadUnusedAsset(assetPath);
        }

        /// <summary>
        /// 卸载所有无用资源。
        /// 注意：该方法会卸载所有引用计数为零的资源包，可以在切换场景之后调用资源释放方法或者写定时器间隔时间去释放。
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        public async UniTaskVoid UnloadUnusedAssetsAsync(string packageName)
        {
            packageName.NotNull(nameof(packageName));
            var package = YooAssets.GetPackage(packageName);
            await package.UnloadUnusedAssetsAsync();
        }

        /// <summary>
        /// 强制卸载所有资源。
        /// 注意：该方法请在合适的时机调用。Package在销毁的时候也会自动调用该方法。
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        public async UniTaskVoid UnloadAllAssetsAsync(string packageName)
        {
            packageName.NotNull(nameof(packageName));
            if (!YooAssets.IsInitialized) return;
            if (!YooAssets.TryGetPackage(packageName, out var package)) return;
            await package.UnloadAllAssetsAsync();
        }

        /// <summary>
        /// 清理YooAssets文件系统所有的缓存资源文件。
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        public async UniTaskVoid ClearAllBundleFilesAsync(string packageName)
        {
            packageName.NotNull(nameof(packageName));
            var package = YooAssets.GetPackage(packageName);
            await package.ClearCacheAsync(new ClearCacheOptions("ClearAllBundleFiles"));
        }

        /// <summary>
        /// 清理YooAssets文件系统未使用的缓存资源文件。
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        public async UniTaskVoid ClearUnusedBundleFilesAsync(string packageName)
        {
            packageName.NotNull(nameof(packageName));
            var package = YooAssets.GetPackage(packageName);
            await package.ClearCacheAsync(new ClearCacheOptions("ClearUnusedBundleFiles"));
        }

        #endregion

        #region Get

        /// <summary>
        /// 是否需要下载
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <returns></returns>
        public bool IsNeedDownload(AssetInfo assetInfo) => GetDefaultPackage().GetDownloadSize(assetInfo) > 0;

        /// <summary>
        /// 是否需要下载
        /// </summary>
        /// <param name="path">资源地址</param>
        /// <returns></returns>
        public bool IsNeedDownload(string path) => GetDefaultPackage().GetDownloadSize(path) > 0;

        /// <summary>
        /// 获取资源信息
        /// </summary>
        /// <param name="assetTags">资源标签列表</param>
        /// <returns></returns>
        public AssetInfo[] GetAssetInfos(string[] assetTags) => GetDefaultPackage().GetAssetInfos(assetTags);

        /// <summary>
        /// 获取资源信息
        /// </summary>
        /// <param name="assetTag">资源标签</param>
        /// <returns></returns>
        public AssetInfo[] GetAssetInfos(string assetTag) => GetDefaultPackage().GetAssetInfos(assetTag);

        /// <summary>
        /// 获取资源信息
        /// </summary>
        public AssetInfo GetAssetInfo(string path) => GetDefaultPackage().GetAssetInfo(path);

        /// <summary>
        /// 检查指定的资源路径是否有效。
        /// </summary>
        /// <param name="path">要检查的资源路径。</param>
        /// <returns>如果资源路径有效，则返回 true；否则返回 false。</returns>
        public bool HasAssetPath(string path) => GetDefaultPackage().IsLocationValid(path);

        #endregion
    }
}
