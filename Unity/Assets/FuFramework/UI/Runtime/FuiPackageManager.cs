using System;
using FairyGUI;
using UnityEngine;
using System.Threading;
using GameFrameX.Runtime;
using Cysharp.Threading.Tasks;
using GameFrameX.Asset.Runtime;
using System.Collections.Generic;

namespace FuFramework.UI.Runtime
{
    /// <summary>
    /// FGui的包管理器，
    /// 主要处理包的资源加载，缓存，卸载等
    /// </summary>
    public class FuiPackageManager : GameFrameworkMonoSingleton<FuiPackageManager>
    {
        /// 缓存已加载的包的字典，key:包名，value：包
        private readonly Dictionary<string, UIPackage> m_loadedPkgDict = new();

        /// 正在异步加载的包的字典，key:包名，value：异步加载任务
        private readonly Dictionary<string, UniTask<UIPackage>> m_loadingTasks = new();

        /// 包对应的资源加载器字典，key:包名，value：资源加载器，一个包对应一个资源加载器，用于加载包的描述文件和资源文件
        private readonly Dictionary<string, AssetLoadRegister> m_pkgAssetLoaderDict = new();

        /// 缓存包的引用计数，key:包名，value：引用数量，一个包可能被界面引用，也可能被其他包引用，当引用计数为0时，释放包
        private readonly Dictionary<string, int> m_pkgRefCountDict = new();

        /// 从Resources中加载的包名
        private readonly List<string> m_fromResourcesPackages = new() { "Launcher" };

        /// 不会被释放的包名
        private readonly List<string> m_notReleasePackages = new() { "Common" };


        /// <summary>
        /// 初始化
        /// </summary>
        protected override void Init()
        {
            UIPackage.unloadBundleByFGUI = false; // 手动管理资源
        }

        /// <summary>
        /// 清空所有包
        /// </summary>
        protected override void Dispose() => ReleaseAll();

        /// <summary>
        /// 是否存在指定包
        /// </summary>
        /// <param name="packageName"></param>
        /// <returns></returns>
        public bool HasPackage(string packageName) => m_loadedPkgDict.ContainsKey(packageName);

        /// <summary>
        /// 异步加载指定包
        /// </summary>
        /// <param name="pkgName"></param>
        /// <returns></returns>
        public UniTask<UIPackage> AddPackageAsync(string pkgName)
        {
            // 已经加载过的包直接返回
            if (m_loadedPkgDict.TryGetValue(pkgName, out var loadedPackage))
                return UniTask.FromResult(loadedPackage);

            // 如果已有正在加载的任务（无论是否加载完成），直接返回任务
            if (m_loadingTasks.TryGetValue(pkgName, out var loadingTask))
                return loadingTask;

            Log.Info($"[FuiPackageManager]添加UIPackage包: {pkgName}");

            // 没有缓存时，创建新的加载任务（延迟执行）
            var newTask = UniTask.Defer(async () =>
            {
                try
                {
                    var package = await LoadPackageAsync_(pkgName);
                    m_loadedPkgDict[pkgName] = package; // 缓存结果
                    package.ReloadAssets();
                    return package;
                }
                finally
                {
                    m_loadingTasks.Remove(pkgName); // 加载完成后移除任务记录
                }
            });

            m_loadingTasks[pkgName] = newTask; // 记录正在加载的任务
            return newTask;
        }

        /// <summary>
        /// 异步加载指定包和所有依赖包
        /// </summary>
        /// <param name="pkgName"></param>
        /// <returns></returns>
        private async UniTask<UIPackage> LoadPackageAsync_(string pkgName)
        {
            var package = await LoadPackageAsync__(pkgName);
            await AddPackageDepAsync(package);
            return package;
        }

        /// <summary>
        /// 异步加载指定包
        /// </summary>
        /// <param name="pkgName">包名</param>
        /// <returns></returns>
        private async UniTask<UIPackage> LoadPackageAsync__(string pkgName)
        {
            // 加载Resources中的包
            if (IsFromResources(pkgName))
            {
                UIPackage.AddPackage($"UI/{pkgName}/{pkgName}");
                return UIPackage.GetByName(pkgName);
            }

            // 加载包的描述文件
            var pkgDesc = await LoadDesc(pkgName);

            // 加载完成后，添加到UIPackage中，并加载pkg中的资源
            var loadedPackage = UIPackage.AddPackage(pkgDesc.bytes, string.Empty, (assetName, extension, type, packageItem) => { LoadResAsync(assetName, extension, type, packageItem).Forget(); });

            return loadedPackage;
        }

        /// <summary>
        /// 加载指定包的依赖包
        /// </summary>
        /// <param name="package">包</param>
        private async UniTask AddPackageDepAsync(UIPackage package)
        {
            var tasks = new List<UniTask>();
            foreach (var dep in package.dependencies)
            {
                if (dep.TryGetValue("name", out var depPkgName))
                {
                    tasks.Add(AddPackageAsync(depPkgName));
                    AddRef(depPkgName);
                }
            }

            await UniTask.WhenAll(tasks); // 并行加载所有依赖
        }

        /// <summary>
        /// 加载包的描述文件
        /// </summary>
        /// <param name="pkgName"></param>
        /// <returns></returns>
        /// <exception cref="GameFrameworkException"></exception>
        private async UniTask<TextAsset> LoadDesc(string pkgName)
        {
            if (string.IsNullOrEmpty(pkgName)) throw new GameFrameworkException("[FuiPackageManager]包名不能为空.");

            //"Assets/Bundles/UI/";
            var rootPath = Utility.Asset.Path.GetUIRootPath();
            var descPath = $"{rootPath}{pkgName}/{pkgName}_fui.bytes";

            m_pkgAssetLoaderDict.TryGetValue(pkgName, out var descLoader);
            if (descLoader == null)
            {
                descLoader                    = AssetLoadRegister.Create();
                m_pkgAssetLoaderDict[pkgName] = descLoader;
            }

            // 等待描述文件加载完成
            return await descLoader.Load<TextAsset>(descPath);
        }

        /// <summary>
        /// 加载包中的资源文件
        /// </summary>
        /// <param name="assetName">资源名</param>
        /// <param name="extension">资源扩展名</param>
        /// <param name="type">资源类型</param>
        /// <param name="packageItem">包内资源项</param>
        private async UniTaskVoid LoadResAsync(string assetName, string extension, Type type, PackageItem packageItem)
        {
            var pkgName  = packageItem.owner.name;
            var rootPath = Utility.Asset.Path.GetUIRootPath(); //"Assets/Bundles/UI/";
            var itemPath = $"{rootPath}{pkgName}/{pkgName}_{assetName}";
            var extPath  = $"{itemPath}{extension}";

            // 等待资源文件加载完成
            m_pkgAssetLoaderDict.TryGetValue(pkgName, out var resLoader);
            if (resLoader == null)
            {
                resLoader                     = AssetLoadRegister.Create();
                m_pkgAssetLoaderDict[pkgName] = resLoader;
            }

            var assetObj = await resLoader.Load(extPath, type);

            // 绑定资源到包内资源项
            packageItem.owner.SetItemAsset(packageItem, assetObj, DestroyMethod.Unload);
        }

        /// <summary>
        /// 添加依赖包引用
        /// </summary>
        /// <param name="pkgName">包名</param>
        public void AddRef(string pkgName)
        {
            if (!m_pkgRefCountDict.TryAdd(pkgName, 1))
            {
                m_pkgRefCountDict[pkgName] += 1;
            }

            Log.Info($"[FuiPackageManager]增加UIPackage包资源引用: {pkgName}，当前引用计数: {m_pkgRefCountDict[pkgName]}");
        }

        /// <summary>
        /// 减少依赖包引用
        /// </summary>
        /// <param name="pkgName">包名</param>
        public void SubRef(string pkgName)
        {
            if (m_pkgRefCountDict.ContainsKey(pkgName))
            {
                m_pkgRefCountDict[pkgName] -= 1;
                Log.Info($"[FuiPackageManager]减少UIPackage包资源引用: {pkgName}，当前引用计数: {m_pkgRefCountDict[pkgName]}");
                if (m_pkgRefCountDict[pkgName] > 0) return; // 引用计数大于0，不释放
            }

            if (!m_loadedPkgDict.TryGetValue(pkgName, out var pkg)) return;

            // 减少该包依赖的其他包引用
            foreach (var depPkgDict in pkg.dependencies)
            {
                foreach (var (_, depPkgName) in depPkgDict)
                {
                    if (depPkgName != "name") continue;
                    SubRef(depPkgName);
                }
            }

            ReleasePackage(pkgName);
        }

        /// <summary>
        /// 释放指定包。
        /// </summary>
        /// <param name="pkgName">要移除的包名</param>
        public void ReleasePackage(string pkgName)
        {
            // 1.如果是不会被释放的包，直接返回
            if (m_notReleasePackages.Contains(pkgName)) return;

            // 2.FUI移除UIPackage包
            if (UIPackage.GetByName(pkgName) == null) return;
            UIPackage.RemovePackage(pkgName);

            // 3.如果是从Resources中加载的包，直接移除包，Resources加载的包会在UIPackage.RemovePackage中自动释放
            if (IsFromResources(pkgName))
            {
                Log.Info($"[FuiPackageManager]释放从Resources中加载的UIPackage包: {pkgName}.");
                return;
            }

            // 4.如果是正在加载的包，取消正在加载的任务
            if (m_loadingTasks.TryGetValue(pkgName, out _))
            {
                var cts = new CancellationTokenSource();
                cts.Cancel();
                m_loadingTasks[pkgName] = UniTask.FromCanceled<UIPackage>(cts.Token);
                Log.Info($"[FuiPackageManager]取消正在加载的UIPackage: {pkgName}");
                return;
            }

            // 5.从已加载字典移除
            if (!m_loadedPkgDict.Remove(pkgName, out _)) return;

            // 6.释包的描述文件资源和资源，包括atlas图集资源，音频资源，spine动画资源等
            if (m_pkgAssetLoaderDict.TryGetValue(pkgName, out var assetLoader))
            {
                assetLoader.Release();
                Log.Info($"[FuiPackageManager]释放UIPackage-{pkgName}内的资源完成.");
            }

            // 7. 移除引用计数
            m_pkgRefCountDict.Remove(pkgName);
        }

        /// <summary>
        /// 释放所有包
        /// </summary>
        public void ReleaseAll()
        {
            var packagesToRelease = new List<string>(m_loadedPkgDict.Keys);
            foreach (var pkgName in packagesToRelease)
            {
                ReleasePackage(pkgName);
            }

            m_loadingTasks.Clear();
            m_pkgRefCountDict.Clear();
            m_loadedPkgDict.Clear();
        }

        /// <summary>
        /// 是否是从Resources中加载的包
        /// </summary>
        private bool IsFromResources(string packageName)
        {
            return m_fromResourcesPackages.Contains(packageName);
        }
    }
}