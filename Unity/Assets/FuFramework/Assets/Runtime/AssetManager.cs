using System;
using YooAsset;
using Cysharp.Threading.Tasks;
using FuFramework.Core.Runtime;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using FuFramework.ModuleSetting.Runtime;

// ReSharper disable once CheckNamespace
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace FuFramework.Asset.Runtime
{
    /// <summary>
    /// 资源管理器。
    /// </summary>
    public partial class AssetManager : MonoSingleton<AssetManager>
    {
        /// <summary>
        /// 默认资源包名称
        /// </summary>
        public string DefaultPackageName { get; private set; } = "DefaultPackage";

        /// <summary>
        /// 获取或设置运行模式。
        /// </summary>
        public EPlayMode PlayMode { get; private set; }


        /// <summary>
        /// 初始化
        /// </summary>
        protected override void Init()
        {
            // 获取资源模块配置数据
            var assetSetting = ModuleSetting.Runtime.ModuleSetting.Instance.AssetSetting;
            if (!assetSetting) throw new FuException("资源模块配置数据为空!");

            PlayMode = assetSetting.PlayMode;
            DefaultPackageName = assetSetting.DefaultPackage.PackageName;

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
            YooAssets.SetOperationSystemMaxTimeSlice(30); // 设置异步系统参数，每帧执行消耗的最大时间切片（单位：毫秒）

            Log.Info("资源系统初始化完毕！");
        }

        /// <summary>
        /// 异步初始化资源包。
        /// </summary>
        /// <param name="packageInfo">资源包信息，包括包名称，下载地址，备用下载地址等</param>
        /// <returns></returns>
        public UniTask InitPackageAsync(AssetPackageInfo packageInfo)
        {
            FuGuard.NotNull(packageInfo, nameof(packageInfo));

            // 创建默认的资源包
            var resourcePackage = YooAssets.TryGetPackage(packageInfo.PackageName);
            if (resourcePackage == null)
            {
                resourcePackage = YooAssets.CreatePackage(packageInfo.PackageName);
                if (packageInfo.IsDefaultPackage)
                    YooAssets.SetDefaultPackage(resourcePackage); // 设置资源包默认资源包
            }

            return CreateInitPackageTask(resourcePackage, packageInfo.DownloadURL, packageInfo.FallbackDownloadURL).ToUniTask();
        }

        #region 异步加载子资源对象

        /// <summary>
        /// 异步加载子资源对象
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <returns></returns>
        public UniTask<SubAssetsHandle> LoadSubAssetsAsync(AssetInfo assetInfo)
        {
            var taskCompletionSource = new UniTaskCompletionSource<SubAssetsHandle>();
            var assetHandle = YooAssets.LoadSubAssetsAsync(assetInfo);
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
            var assetHandle = YooAssets.LoadSubAssetsAsync(path, type);
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
            var assetHandle = YooAssets.LoadSubAssetsAsync<T>(path);
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
        public SubAssetsHandle LoadSubAssetSync(AssetInfo assetInfo)
        {
            return YooAssets.LoadSubAssetsSync(assetInfo);
        }

        /// <summary>
        /// 同步加载子资源对象
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="type"></param>
        /// <returns></returns>
        public SubAssetsHandle LoadSubAssetSync(string path, Type type)
        {
            return YooAssets.LoadSubAssetsSync(path, type);
        }

        /// <summary>
        /// 同步加载子资源对象
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        public SubAssetsHandle LoadSubAssetSync<T>(string path) where T : Object
        {
            return YooAssets.LoadSubAssetsSync<T>(path);
        }

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
            var assetHandle = YooAssets.LoadRawFileAsync(assetInfo);
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
            var assetHandle = YooAssets.LoadRawFileAsync(path);
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
        public RawFileHandle LoadRawFileSync(AssetInfo assetInfo)
        {
            return YooAssets.LoadRawFileSync(assetInfo);
        }

        /// <summary>
        /// 同步加载原生文件
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        public RawFileHandle LoadRawFileSync(string path)
        {
            return YooAssets.LoadRawFileSync(path);
        }

        #endregion

        #region 异步加载资源

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <returns></returns>
        public UniTask<AssetHandle> LoadAssetAsync(AssetInfo assetInfo)
        {
            var taskCompletionSource = new UniTaskCompletionSource<AssetHandle>();
            var assetHandle = YooAssets.LoadAssetAsync(assetInfo);
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
            var assetHandle = YooAssets.LoadAssetAsync(path, type);
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
            var assetHandle = YooAssets.LoadAllAssetsAsync<T>(path);
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
            var assetHandle = YooAssets.LoadAllAssetsAsync(path, type);
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
            var assetHandle = YooAssets.LoadAllAssetsAsync(path);
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
            var assetHandle = YooAssets.LoadAllAssetsAsync(assetInfo);
            assetHandle.Completed += handle => { taskCompletionSource.TrySetResult(handle); };
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// 异步加载子资源对象
        /// </summary>
        /// <param name="path">资源的定位地址</param>
        public SubAssetsHandle LoadSubAssetsAsync(string path)
        {
            return YooAssets.LoadSubAssetsAsync(path);
        }


        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        public UniTask<AssetHandle> LoadAssetAsync(string path)
        {
            var taskCompletionSource = new UniTaskCompletionSource<AssetHandle>();
            var assetHandle = YooAssets.LoadAssetAsync(path);
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
            var assetHandle = YooAssets.LoadAssetAsync<T>(path);

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
        public AllAssetsHandle LoadAllAssetsSync(string path)
        {
            return YooAssets.LoadAllAssetsSync(path);
        }

        /// <summary>
        /// 同步加载资源包内所有资源对象
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="path">资源的定位地址</param>
        public AllAssetsHandle LoadAllAssetsSync<T>(string path) where T : Object
        {
            return YooAssets.LoadAllAssetsSync<T>(path);
        }

        /// <summary>
        /// 同步加载资源包内所有资源对象
        /// </summary>
        /// <param name="path">资源的定位地址</param>
        /// <param name="type">子对象类型</param>
        public AllAssetsHandle LoadAllAssetsSync(string path, Type type)
        {
            return YooAssets.LoadAllAssetsSync(path, type);
        }

        /// <summary>
        /// 同步加载包内全部资源对象
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <returns></returns>
        public AllAssetsHandle LoadAllAssetsSync(AssetInfo assetInfo)
        {
            return YooAssets.LoadAllAssetsSync(assetInfo);
        }

        /// <summary>
        /// 同步加载子资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        public SubAssetsHandle LoadSubAssetSync(string path)
        {
            return YooAssets.LoadSubAssetsSync(path);
        }

        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        public AssetHandle LoadAssetSync(string path)
        {
            return YooAssets.LoadAssetSync(path);
        }

        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="type"></param>
        /// <returns></returns>
        public AssetHandle LoadAssetSync(string path, Type type)
        {
            return YooAssets.LoadAssetSync(path, type);
        }

        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <returns></returns>
        public AssetHandle LoadAssetSync(AssetInfo assetInfo)
        {
            return YooAssets.LoadAssetSync(assetInfo);
        }

        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        public AssetHandle LoadAssetSync<T>(string path) where T : Object
        {
            return YooAssets.LoadAssetSync<T>(path);
        }

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
            var sceneHandle = YooAssets.LoadSceneAsync(path, sceneMode, LocalPhysicsMode.None, !activateOnLoad);
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
            var sceneHandle = YooAssets.LoadSceneAsync(assetInfo, sceneMode, LocalPhysicsMode.None, !activateOnLoad);
            sceneHandle.Completed += handle => { taskCompletionSource.TrySetResult(handle); };
            return taskCompletionSource.Task;
        }

        #endregion

        #region 资源包

        /// <summary>
        /// 创建资源包
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        /// <returns></returns>
        public ResourcePackage CreateAssetsPackage(string packageName) => YooAssets.CreatePackage(packageName);

        /// <summary>
        /// 尝试获取资源包
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        /// <returns></returns>
        public ResourcePackage TryGetAssetsPackage(string packageName) => YooAssets.TryGetPackage(packageName);

        /// <summary>
        /// 检查资源包是否存在
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        /// <returns></returns>
        public bool HasAssetsPackage(string packageName) => YooAssets.TryGetPackage(packageName) != null;

        /// <summary>
        /// 获取资源包
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        /// <returns></returns>
        public ResourcePackage GetAssetsPackage(string packageName) => YooAssets.GetPackage(packageName);

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
            FuGuard.NotNull(assetPath, nameof(assetPath));
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

        /// <summary>
        /// 设置默认资源包
        /// </summary>
        /// <param name="resourcePackage">资源信息</param>
        /// <returns></returns>
        public void SetDefaultAssetsPackage(ResourcePackage resourcePackage) => YooAssets.SetDefaultPackage(resourcePackage);

        #endregion
    }
}