using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FairyGUI;
using GameFrameX.Asset.Runtime;
using GameFrameX.Runtime;
using UnityEngine;

namespace GameFrameX.UI.FairyGUI.Runtime
{
    /// <summary>
    /// FGUI的包管理器，
    /// 主要处理包的资源加载
    /// </summary>
    public class FUIPackageMgr : GameFrameworkMonoSingleton<FUIPackageMgr>
    {
        /// 缓存已加载的包的字典，key:包名，value：包
        private readonly Dictionary<string, UIPackage> _loadedPkgDic = new();

        /// 缓存包的引用计数，key:包名，value：引用数量
        private readonly Dictionary<string, int> _pkgRefCountDic = new();
        
        /// 资源管理器
        private AssetComponent _assetManager;

        /// <summary>
        /// 初始化
        /// </summary>
        protected override void Init()
        {
            base.Init();

            _assetManager = GameEntry.GetComponent<AssetComponent>();
            
            //手动管理资源
            UIPackage.unloadBundleByFGUI = false;
        }
        
        /// <summary>
        /// 清空所有包
        /// </summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            foreach (var pkgName in _loadedPkgDic.Keys)
            {
                UIPackage.RemovePackage(pkgName);
            }

            _pkgRefCountDic.Clear();
            _loadedPkgDic.Clear();

            UIPackage.RemoveAllPackages();
        }


        /// <summary>
        /// 是否存在指定包
        /// </summary>
        /// <param name="packageName"></param>
        /// <returns></returns>
        public bool HasPackage(string packageName)
        {
            return _loadedPkgDic.ContainsKey(packageName);
        }
        
        /// <summary>
        /// 移除指定包
        /// </summary>
        /// <param name="pkgName">包名</param>
        private void RemovePackage(string pkgName)
        {
            if (!_loadedPkgDic.Remove(pkgName, out var package)) return;

            UIPackage.RemovePackage(pkgName);

            var pkgPath = Utility.Asset.Path.GetUIPackagePath(pkgName);
            var descPath = $"{pkgPath}_fui";
            var resPath  = $"{pkgPath}_atlas";
            _assetManager.UnloadAsset(descPath);
            _assetManager.UnloadAsset(resPath);

            // 绑定该包下绑定的所有自定义组件
            if (!FUIConfig.CompBinderDic.TryGetValue(pkgName, out var compBinder)) return;
            {
                compBinder?.RemoveBindComp();
            }
        }

        /// <summary>
        /// 加载指定包和依赖
        /// </summary>
        /// <param name="pkgName">包名</param>
        /// <param name="isFromResources">是否从Resources目录加载</param>
        public async UniTask<UIPackage> AddPackageAsync(string pkgName, bool isFromResources = false)
        {
            // 已经加载过的包直接返回
            if (_loadedPkgDic.TryGetValue(pkgName, out var loadedPackage)) return loadedPackage;

            Log.Info($"添加UI包: {pkgName}", pkgName);

            UIPackage package;

            if (isFromResources)
                package = UIPackage.AddPackage($"Assets/Resources/UI/{pkgName}/{pkgName}");
            else
                package = await LoadPackageAsync(pkgName);
            
            _loadedPkgDic.Add(pkgName, package);
            
            // 绑定该包下的所有自定义组件
            if (FUIConfig.CompBinderDic.TryGetValue(pkgName, out var compBinder))
                compBinder?.BindComp();

            return await AddDependenciesPkgAsync(package);
        }

        /// <summary>
        /// 加载指定包的依赖包
        /// </summary>
        /// <param name="package">包</param>
        private async UniTask<UIPackage> AddDependenciesPkgAsync(UIPackage package)
        {
            foreach (var depPkgDict in package.dependencies)
            {
                foreach (var (_, depPkgName) in depPkgDict)
                {
                    if (depPkgName != "name") continue;
                    await AddPackageAsync(depPkgName);
                    Retain(depPkgName);
                }
            }

            return package;
        }

        /// <summary>
        /// 添加依赖包引用
        /// </summary>
        /// <param name="pkgName">包名</param>
        public void Retain(string pkgName)
        {
            if (!_pkgRefCountDic.TryAdd(pkgName, 1))
                _pkgRefCountDic[pkgName] += 1;
        }

        /// <summary>
        /// 释放指定包
        /// </summary>
        /// <param name="pkgName">包名</param>
        public void Release(string pkgName)
        {
            if (!_pkgRefCountDic.ContainsKey(pkgName)) return;

            _pkgRefCountDic[pkgName] -= 1;

            if (_pkgRefCountDic[pkgName] > 0) return;

            if (!_loadedPkgDic.TryGetValue(pkgName, out var pkg)) return;
            foreach (var depPkgDict in pkg.dependencies)
            {
                foreach (var (_, depPkgName) in depPkgDict)
                {
                    if (depPkgName != "name") continue;
                    Release(depPkgName);
                }
            }

            RemovePackage(pkgName);
        }


        /// <summary>
        /// 异步加载UI包
        /// </summary>
        /// <param name="pkgName">包名</param>
        /// <returns></returns>
        private async UniTask<UIPackage> LoadPackageAsync(string pkgName)
        {
            if (_loadedPkgDic.TryGetValue(pkgName, out var loadedPackage)) ;
            {
                if (loadedPackage != null)
                {
                    loadedPackage.ReloadAssets();
                    return loadedPackage;
                }
            }

            const string rootPath = "Assets/Bundles/UI/";
            
            // 加载描述文件
            var pkgDesc = await LoadDesc(pkgName);
            
            // 加载完成后，添加到UIPackage中
            loadedPackage = UIPackage.AddPackage(pkgDesc.bytes, string.Empty, (assetName, extension, type, packageItem) =>
            {
                LoadResAsync(rootPath, assetName, extension, type, packageItem).Forget();
            });
            
            return loadedPackage;
        }

        /// <summary>
        /// 加载UI包描述数据
        /// </summary>
        /// <param name="pkgName"></param>
        /// <returns></returns>
        /// <exception cref="GameFrameworkException"></exception>
        private async UniTask<TextAsset> LoadDesc(string pkgName)
        {
            if (string.IsNullOrEmpty(pkgName)) throw new GameFrameworkException("Asset name is invalid.");

            const string rootPath = "Assets/Bundles/UI/";
            var       resPath  = Utility.Text.Format("{0}{1}/{2}_fui.bytes", rootPath, pkgName, pkgName);

            // 等待描述文件加载完成
            var assetHandle = await _assetManager.LoadAssetAsync(resPath);
            var isSuccess   = assetHandle != null && assetHandle.AssetObject != null;

            if (!isSuccess)
                throw new GameFrameworkException(Utility.Text.Format("Asset name '{0}' is invalid.", pkgName));

            var pkgDesc = assetHandle.GetAssetObject<TextAsset>();

            if (pkgDesc == null)
                throw new GameFrameworkException(Utility.Text.Format("Asset name '{0}' is invalid.", pkgName));

            return pkgDesc;
        }

        /// <summary>
        /// 加载UI包资源
        /// </summary>
        /// <param name="rootPath"></param>
        /// <param name="assetName"></param>
        /// <param name="extension"></param>
        /// <param name="type"></param>
        /// <param name="packageItem"></param>
        private async UniTaskVoid LoadResAsync(string rootPath, string assetName, string extension, System.Type type, PackageItem packageItem)
        {
            var extPath = $"{rootPath}{packageItem.owner.name}/{packageItem.owner.name}_{assetName}{extension}";

            // 等待资源文件加载完成
            var assetHandle = await _assetManager.LoadAssetAsync(extPath, type);
            var isSuccess   = assetHandle != null && assetHandle.AssetObject != null;
            
            if (!isSuccess)
                throw new GameFrameworkException(Utility.Text.Format("Asset name '{0}' is invalid.", assetName));

            packageItem.owner.SetItemAsset(packageItem, assetHandle.AssetObject, DestroyMethod.Custom);
        }
    }
}