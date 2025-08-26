using System;
using YooAsset;
using UnityEngine;
using Cysharp.Threading.Tasks;
using FuFramework.Core.Runtime;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable once CheckNamespace
// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming
namespace FuFramework.Asset.Runtime
{
    /// <summary>
    /// 资源组件。
    /// 功能：封装资源管理器
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/Asset")]
    public sealed class AssetComponent : FuComponent
    {
        /// <summary>
        /// 资源的运行模式
        /// </summary>
        [Tooltip("当目标平台为Web平台时，将会强制设置为" + nameof(EPlayMode.WebPlayMode))] [SerializeField]
        private EPlayMode m_GamePlayMode;

        /// <summary>
        /// 资源的运行模式
        /// </summary>
        public EPlayMode GamePlayMode
        {
            get => m_GamePlayMode;
            set => m_GamePlayMode = value;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 资源包信息列表
        /// </summary>
        [SerializeField] private List<AssetResourcePackageInfo> m_assetResourcePackages = new();
#endif

        /// <summary>
        /// 默认包名称
        /// </summary>
        public const string BuildInPackageName = "DefaultPackage";

        /// <summary>
        /// 初始化操作句柄
        /// </summary>
        private InitializationOperation _initializationOperation;

        /// <summary>
        /// 资源管理器
        /// </summary>
        private IAssetManager _assetManager;

        protected override void Awake()
        {
#if !UNITY_EDITOR
            if (GamePlayMode == EPlayMode.EditorSimulateMode)
            {
                GamePlayMode = EPlayMode.HostPlayMode;
            }
#if UNITY_WEBGL
            GamePlayMode = EPlayMode.WebPlayMode;
#endif
#endif
            ImplComponentType = Utility.Assembly.GetType(componentType);
            InterfaceComponentType      = typeof(IAssetManager);

            base.Awake();

            _assetManager = FuEntry.GetModule<IAssetManager>();
            if (_assetManager == null)
            {
                Log.Fatal("Asset manager is invalid.");
                return;
            }

            _assetManager.SetPlayMode(GamePlayMode);
        }

        private void Start()
        {
            _assetManager.Initialize();
        }

        /// <summary>
        /// 初始化资源包
        /// </summary>
        /// <param name="packageName">包名称</param>
        /// <param name="host">主下载地址</param>
        /// <param name="fallbackHostServer">备用下载地址</param>
        /// <param name="isDefaultPackage">是否是默认包</param>
        public async UniTask<bool> InitPackageAsync(string packageName, string host, string fallbackHostServer, bool isDefaultPackage = false)
        {
#if UNITY_EDITOR
            var assetResourcePackageInfo = new AssetResourcePackageInfo
            {
                PackageName         = packageName,
                DownloadURL         = host,
                FallbackDownloadURL = fallbackHostServer
            };

            if (!m_assetResourcePackages.Exists(m => m.PackageName == packageName))
            {
                m_assetResourcePackages.Add(assetResourcePackageInfo);
            }
#endif
            return await _assetManager.InitPackageAsync(packageName, host, fallbackHostServer, isDefaultPackage);
        }

        #region 异步加载子资源对象

        /// <summary>
        /// 异步加载子资源对象
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <returns></returns>
        public UniTask<SubAssetsHandle> LoadSubAssetsAsync(AssetInfo assetInfo) => _assetManager.LoadSubAssetsAsync(assetInfo);

        /// <summary>
        /// 异步加载子资源对象
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="type"></param>
        /// <returns></returns>
        public UniTask<SubAssetsHandle> LoadSubAssetsAsync(string path, Type type) => _assetManager.LoadSubAssetsAsync(path, type);

        /// <summary>
        /// 异步加载子资源对象
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        public UniTask<SubAssetsHandle> LoadSubAssetsAsync<T>(string path) where T : Object => _assetManager.LoadSubAssetsAsync<T>(path);

        #endregion

        #region 同步加载子资源对象

        /// <summary>
        /// 同步加载子资源对象
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <returns></returns>
        public SubAssetsHandle LoadSubAssetSync(AssetInfo assetInfo) => _assetManager.LoadSubAssetSync(assetInfo);

        /// <summary>
        /// 同步加载子资源对象
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="type"></param>
        /// <returns></returns>
        public SubAssetsHandle LoadSubAssetSync(string path, Type type) => _assetManager.LoadSubAssetSync(path, type);

        /// <summary>
        /// 同步加载子资源对象
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        public SubAssetsHandle LoadSubAssetSync<T>(string path) where T : Object => _assetManager.LoadSubAssetSync<T>(path);

        #endregion

        #region 异步加载原生文件

        /// <summary>
        /// 异步加载原生文件
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <returns></returns>
        public UniTask<RawFileHandle> LoadRawFileAsync(AssetInfo assetInfo) => _assetManager.LoadRawFileAsync(assetInfo);

        /// <summary>
        /// 异步加载原生文件
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        public UniTask<RawFileHandle> LoadRawFileAsync(string path) => _assetManager.LoadRawFileAsync(path);

        #endregion

        #region 同步加载原生文件

        /// <summary>
        /// 同步加载原生文件
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <returns></returns>
        public RawFileHandle LoadRawFileSync(AssetInfo assetInfo) => _assetManager.LoadRawFileSync(assetInfo);

        /// <summary>
        /// 同步加载原生文件
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        public RawFileHandle LoadRawFileSync(string path) => _assetManager.LoadRawFileSync(path);

        #endregion

        #region 异步加载资源

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <returns></returns>
        public UniTask<AssetHandle> LoadAssetAsync(AssetInfo assetInfo) => _assetManager.LoadAssetAsync(assetInfo);

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="type">资源类型</param>
        /// <returns></returns>
        public UniTask<AssetHandle> LoadAssetAsync(string path, Type type) => _assetManager.LoadAssetAsync(path, type);

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <typeparam name="T">资源类型</typeparam>
        /// <returns></returns>
        public UniTask<AssetHandle> LoadAssetAsync<T>(string path) where T : Object => _assetManager.LoadAssetAsync<T>(path);

        /// <summary>
        /// 异步加载全部资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <typeparam name="T">资源类型</typeparam>
        /// <returns></returns>
        public UniTask<AllAssetsHandle> LoadAllAssetsAsync<T>(string path) where T : Object => _assetManager.LoadAllAssetsAsync<T>(path);

        /// <summary>
        /// 异步加载全部资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="type">资源类型</param>
        /// <returns></returns>
        public UniTask<AllAssetsHandle> LoadAllAssetsAsync(string path, Type type) => _assetManager.LoadAllAssetsAsync(path, type);

        /// <summary>
        /// 异步加载资源包内所有资源对象
        /// </summary>
        /// <param name="path">资源的定位地址</param>
        public UniTask<AllAssetsHandle> LoadAllAssetsAsync(string path) => _assetManager.LoadAllAssetsAsync(path);

        /// <summary>
        /// 异步加载资源包内所有资源对象
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        public UniTask<AllAssetsHandle> LoadAllAssetsAsync(AssetInfo assetInfo) => _assetManager.LoadAllAssetsAsync(assetInfo);

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        public UniTask<AssetHandle> LoadAssetAsync(string path) => _assetManager.LoadAssetAsync(path);

        /// <summary>
        /// 异步加载子资源对象
        /// </summary>
        /// <param name="path">资源的定位地址</param>
        public SubAssetsHandle LoadSubAssetsAsync(string path) => _assetManager.LoadSubAssetsAsync(path);

        #endregion

        #region 同步加载资源

        /// <summary>
        /// 同步加载资源包内所有资源对象
        /// </summary>
        /// <param name="path">资源的定位地址</param>
        public AllAssetsHandle LoadAllAssetsSync(string path) => _assetManager.LoadAllAssetsSync(path);

        /// <summary>
        /// 同步加载资源包内所有资源对象
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="path">资源的定位地址</param>
        public AllAssetsHandle LoadAllAssetsSync<T>(string path) where T : Object => _assetManager.LoadAllAssetsSync<T>(path);

        /// <summary>
        /// 同步加载资源包内所有资源对象
        /// </summary>
        /// <param name="path">资源的定位地址</param>
        /// <param name="type">子对象类型</param>
        public AllAssetsHandle LoadAllAssetsSync(string path, Type type) => _assetManager.LoadAllAssetsSync(path, type);

        /// <summary>
        /// 同步加载包内全部资源对象
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <returns></returns>
        public AllAssetsHandle LoadAllAssetsSync(AssetInfo assetInfo) => _assetManager.LoadAllAssetsSync(assetInfo);

        /// <summary>
        /// 同步加载子资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        public SubAssetsHandle LoadSubAssetsSync(string path) => _assetManager.LoadSubAssetSync(path);

        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        public AssetHandle LoadAssetsSync(string path) => _assetManager.LoadAssetSync(path);

        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="type"></param>
        /// <returns></returns>
        public AssetHandle LoadAssetSync(string path, Type type) => _assetManager.LoadAssetSync(path, type);

        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <returns></returns>
        public AssetHandle LoadAssetSync(AssetInfo assetInfo) => _assetManager.LoadAssetSync(assetInfo);

        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        public AssetHandle LoadAssetSync<T>(string path) where T : Object => _assetManager.LoadAssetSync<T>(path);

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
            => _assetManager.LoadSceneAsync(path, sceneMode, activateOnLoad);

        /// <summary>
        /// 异步加载场景
        /// </summary>
        /// <param name="assetInfo">资源路径</param>
        /// <param name="sceneMode">场景模式</param>
        /// <param name="activateOnLoad">是否加载完成自动激活</param>
        /// <returns></returns>
        public UniTask<SceneHandle> LoadSceneAsync(AssetInfo assetInfo, LoadSceneMode sceneMode, bool activateOnLoad = true)
            => _assetManager.LoadSceneAsync(assetInfo, sceneMode, activateOnLoad);

        #endregion

        #region 资源包

        /// <summary>
        /// 创建资源包
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        /// <returns></returns>
        public ResourcePackage CreateAssetsPackage(string packageName) => _assetManager.CreateAssetsPackage(packageName);

        /// <summary>
        /// 尝试获取资源包
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        /// <returns></returns>
        public ResourcePackage TryGetAssetsPackage(string packageName) => _assetManager.TryGetAssetsPackage(packageName);

        /// <summary>
        /// 检查资源包是否存在
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        /// <returns></returns>
        public bool HasAssetsPackage(string packageName) => _assetManager.HasAssetsPackage(packageName);

        /// <summary>
        /// 获取资源包
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        /// <returns></returns>
        public ResourcePackage GetAssetsPackage(string packageName) => _assetManager.GetAssetsPackage(packageName);

        #endregion

        #region 卸载资源

        /// <summary>
        /// 卸载资源
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        /// <param name="assetPath">资源路径</param>
        public void UnloadAsset(string packageName, string assetPath) => _assetManager.UnloadAsset(packageName, assetPath);

        /// <summary>
        /// 卸载资源
        /// </summary>
        /// <param name="assetPath">资源路径</param>
        public void UnloadAsset(string assetPath) => _assetManager.UnloadAsset(assetPath);

        /// <summary>
        /// 卸载无用资源
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        public void UnloadUnusedAssetsAsync(string packageName) => _assetManager.UnloadUnusedAssetsAsync(packageName);

        /// <summary>
        /// 强制回收所有资源
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        public void UnloadAllAssetsAsync(string packageName) => _assetManager.UnloadAllAssetsAsync(packageName);

        /// <summary>
        /// 清理所有资源
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        public void ClearAllBundleFilesAsync(string packageName) => _assetManager.ClearAllBundleFilesAsync(packageName);

        /// <summary>
        /// 清理无用资源
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        public void ClearUnusedBundleFilesAsync(string packageName) => _assetManager.ClearUnusedBundleFilesAsync(packageName);

        #endregion

        #region Get

        /// <summary>
        /// 是否需要下载
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <returns></returns>
        public bool IsNeedDownload(AssetInfo assetInfo) => _assetManager.IsNeedDownload(assetInfo);

        /// <summary>
        /// 是否需要下载
        /// </summary>
        /// <param name="path">资源地址</param>
        /// <returns></returns>
        public bool IsNeedDownload(string path) => _assetManager.IsNeedDownload(path);

        /// <summary>
        /// 获取资源信息
        /// </summary>
        /// <param name="assetTags">资源标签列表</param>
        /// <returns></returns>
        public AssetInfo[] GetAssetInfos(string[] assetTags) => _assetManager.GetAssetInfos(assetTags);

        /// <summary>
        /// 获取资源信息
        /// </summary>
        /// <param name="assetTag">资源标签</param>
        /// <returns></returns>
        public AssetInfo[] GetAssetInfos(string assetTag) => _assetManager.GetAssetInfos(assetTag);

        /// <summary>
        /// 获取资源信息
        /// </summary>
        public AssetInfo GetAssetInfo(string path) => _assetManager.GetAssetInfo(path);

        /// <summary>
        /// 检查指定的资源路径是否存在。
        /// </summary>
        /// <param name="assetPath">要检查的资源路径。</param>
        /// <returns>如果存在指定的资源路径，则返回 true；否则返回 false。</returns>
        public bool HasAssetPath(string assetPath) => _assetManager.HasAssetPath(assetPath);

        #endregion

        #region Set

        /// <summary>
        /// 设置默认资源包
        /// </summary>
        /// <param name="assetsPackage">资源信息</param>
        /// <returns></returns>
        public void SetDefaultAssetsPackage(ResourcePackage assetsPackage) => _assetManager.SetDefaultAssetsPackage(assetsPackage);

        #endregion
    }

#if UNITY_EDITOR
    /// <summary>
    /// 资源包信息
    /// </summary>
    [Serializable]
    public sealed class AssetResourcePackageInfo
    {
        [SerializeField] public string PackageName;
        [SerializeField] public string DownloadURL;
        [SerializeField] public string FallbackDownloadURL;
    }
#endif
}