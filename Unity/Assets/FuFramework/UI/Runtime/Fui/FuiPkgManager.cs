using System;
using FairyGUI;
using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;
using FuFramework.Core.Runtime;
using FuFramework.Asset.Runtime;
using System.Collections.Generic;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable once CheckNamespace
namespace FuFramework.UI.Runtime
{
    /// <summary>
    /// FUI包管理器。
    /// 职责：用于自行管理FUI包的资源加载，缓存，卸载等操作。
    /// 核心功能:
    /// 1. 异步加载FUI包。
    /// 2. 缓存已加载的FUI包。
    /// 3. 卸载FUI包。
    /// 4. 支持通过事件系统通知包加载完成。
    /// </summary>
    public class FuiPkgManager
    {
        /// <summary>
        /// 缓存已加载的包的字典，key:包名，value：包
        /// </summary>
        private readonly Dictionary<string, UIPackage> m_LoadedPkgDict = new();

        /// <summary>
        /// 正在异步加载的包的字典，key:包名，value：异步加载任务
        /// </summary>
        private readonly Dictionary<string, UniTask<UIPackage>> m_LoadingTasks = new();

        /// <summary>
        /// 包加载的取消令牌源字典，用于正确取消加载任务
        /// </summary>
        private readonly Dictionary<string, CancellationTokenSource> m_LoadingCts = new();

        /// <summary>
        /// 包对应的资源加载器字典，key:包名，value：资源加载器，一个包对应一个资源加载器，用于加载包的描述文件和资源文件
        /// </summary>
        private readonly Dictionary<string, AssetLoadRegister> m_PkgAssetLoaderDict = new();

        /// <summary>
        /// 缓存包的引用计数，key:包名，value：引用数量，一个包可能被界面引用，也可能被其他包引用，当引用计数为0时，释放包
        /// </summary>
        private readonly Dictionary<string, int> m_PkgRefCountDict = new();

        /// <summary>
        /// 从Resources中加载的包名列表
        /// </summary>
        private readonly List<string> m_FromResourcesPackages = new() { "Launcher" };

        public FuiPkgManager()
        {
            // 手动管理资源
            UIPackage.unloadBundleByFGUI = false;
        }

        /// <summary>
        /// 是否存在指定包
        /// </summary>
        /// <param name="packageName"></param>
        /// <returns></returns>
        public bool HasPackage(string packageName) => m_LoadedPkgDict.ContainsKey(packageName);

        /// <summary>
        /// 异步加载指定包
        /// </summary>
        /// <param name="pkgName"></param>
        /// <returns></returns>
        public UniTask<UIPackage> AddPackageAsync(string pkgName)
        {
            // 已经加载过的包直接返回
            if (m_LoadedPkgDict.TryGetValue(pkgName, out var loadedPackage))
                return UniTask.FromResult(loadedPackage);

            // 如果已有正在加载的任务，直接返回任务
            if (m_LoadingTasks.TryGetValue(pkgName, out var loadingTask))
                return loadingTask;

            FuLogger.LogInfo($"[FuiPkgManager] 添加UIPackage包: {pkgName}");

            // 创建取消令牌源
            var cts = new CancellationTokenSource();
            m_LoadingCts[pkgName] = cts;

            // 创建新的加载任务（延迟执行）
            var newTask = UniTask.Defer(async () =>
            {
                try
                {
                    // 检查是否被取消
                    cts.Token.ThrowIfCancellationRequested();

                    var package = await LoadPackageAsync_(pkgName);
                    m_LoadedPkgDict[pkgName] = package; // 缓存结果
                    package.ReloadAssets();

                    return package;
                }
                finally
                {
                    m_LoadingTasks.Remove(pkgName); // 加载完成后移除任务记录
                    m_LoadingCts.Remove(pkgName);   // 移除取消令牌源
                }
            });

            m_LoadingTasks[pkgName] = newTask; // 记录正在加载的任务
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
            try
            {
                // 检查是否被取消
                if (m_LoadingCts.TryGetValue(pkgName, out var cts))
                    cts.Token.ThrowIfCancellationRequested();

                // 加载Resources中的包
                if (IsFromResources(pkgName))
                {
                    UIPackage.AddPackage($"UI/{pkgName}/{pkgName}");
                    return UIPackage.GetByName(pkgName);
                }

                // 加载包的描述文件
                var pkgDesc = await LoadDesc(pkgName);

                // 检查是否被取消
                if (m_LoadingCts.TryGetValue(pkgName, out cts))
                    cts.Token.ThrowIfCancellationRequested();

                // 加载完成后，添加到UIPackage中，并加载pkg中的资源
                var loadedPackage = UIPackage.AddPackage(pkgDesc.bytes, string.Empty, (assetName, extension, type, packageItem) => { LoadResAsync(assetName, extension, type, packageItem).Forget(); });

                return loadedPackage;
            }
            catch (OperationCanceledException)
            {
                FuLogger.LogInfo($"[FuiPkgManager] 包加载被取消: {pkgName}");
                throw;
            }
            catch (Exception ex)
            {
                FuLogger.LogError($"[FuiPkgManager] 加载包失败: {pkgName}, 错误: {ex.Message}");
                throw;
            }
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
        /// <exception cref="FuException"></exception>
        private async UniTask<TextAsset> LoadDesc(string pkgName)
        {
            if (string.IsNullOrEmpty(pkgName)) throw new FuException("[FuiPkgManager] 包名不能为空.");

            //"Assets/Bundles/UI/";
            var rootPath = Utility.AssetPath.GetUIRootPath();
            var descPath = $"{rootPath}{pkgName}/{pkgName}_fui.bytes";

            m_PkgAssetLoaderDict.TryGetValue(pkgName, out var descLoader);
            if (descLoader == null)
            {
                descLoader                    = AssetLoadRegister.Create();
                m_PkgAssetLoaderDict[pkgName] = descLoader;
            }

            // 等待描述文件加载完成
            return await descLoader.LoadAsync<TextAsset>(descPath);
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
            var rootPath = Utility.AssetPath.GetUIRootPath(); //"Assets/Bundles/UI/";
            var itemPath = $"{rootPath}{pkgName}/{pkgName}_{assetName}";
            var extPath  = $"{itemPath}{extension}";

            // 等待资源文件加载完成
            m_PkgAssetLoaderDict.TryGetValue(pkgName, out var resLoader);
            if (resLoader == null)
            {
                resLoader                     = AssetLoadRegister.Create();
                m_PkgAssetLoaderDict[pkgName] = resLoader;
            }

            var assetObj = await resLoader.LoadAsync(extPath, type);

            // 绑定资源到包内资源项
            packageItem.owner.SetItemAsset(packageItem, assetObj, DestroyMethod.Unload);
        }

        /// <summary>
        /// 添加依赖包引用
        /// </summary>
        /// <param name="pkgName">包名</param>
        public void AddRef(string pkgName)
        {
            if (!m_PkgRefCountDict.TryAdd(pkgName, 1))
            {
                m_PkgRefCountDict[pkgName] += 1;
            }

            FuLogger.LogInfo($"[FuiPkgManager] 增加UIPackage包资源引用: {pkgName}，当前引用计数: {m_PkgRefCountDict[pkgName]}");
        }

        /// <summary>
        /// 减少依赖包引用
        /// </summary>
        /// <param name="pkgName">包名</param>
        public void SubRef(string pkgName)
        {
            if (m_PkgRefCountDict.ContainsKey(pkgName))
            {
                m_PkgRefCountDict[pkgName] -= 1;
                FuLogger.LogInfo($"[FuiPkgManager] 减少UIPackage包资源引用: {pkgName}，当前引用计数: {m_PkgRefCountDict[pkgName]}");

                // 引用计数大于0，不释放
                if (m_PkgRefCountDict[pkgName] > 0) return;
            }

            if (!m_LoadedPkgDict.TryGetValue(pkgName, out var pkg)) return;

            // 减少该包依赖的其他包引用
            foreach (var dep in pkg.dependencies)
            {
                if (dep.TryGetValue("name", out var depPkgName))
                {
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
            // 1.FUI移除UIPackage包
            if (UIPackage.GetByName(pkgName) == null) return;
            UIPackage.RemovePackage(pkgName);

            // 2.如果是从Resources中加载的包，直接移除包，Resources加载的包会在UIPackage.RemovePackage中自动释放
            if (IsFromResources(pkgName))
            {
                FuLogger.LogInfo($"[FuiPkgManager] 释放从Resources中加载的UIPackage包: {pkgName}.");
                return;
            }

            // 3.如果是正在加载的包，取消正在加载的任务
            if (m_LoadingCts.TryGetValue(pkgName, out var cts))
            {
                cts.Cancel(); // 真正取消正在进行的加载任务
                m_LoadingCts.Remove(pkgName);
                FuLogger.LogInfo($"[FuiPkgManager] 取消正在加载的UIPackage: {pkgName}");
                return;
            }

            // 4.从已加载字典移除
            if (!m_LoadedPkgDict.Remove(pkgName, out _)) return;

            // 5.释包的描述文件资源和资源，包括atlas图集资源，音频资源，spine动画资源等
            if (m_PkgAssetLoaderDict.TryGetValue(pkgName, out var assetLoader))
            {
                assetLoader.Release();
                FuLogger.LogInfo($"[FuiPkgManager] 释放UIPackage-{pkgName}内的资源完成.");
            }

            // 6. 移除引用计数
            m_PkgRefCountDict.Remove(pkgName);
        }

        /// <summary>
        /// 释放所有包
        /// </summary>
        public void ReleaseAll()
        {
            // 释放所有已加载的包（先复制Keys避免遍历时修改集合）
            List<string> pkgNames = new(m_LoadedPkgDict.Keys);
            foreach (var pkgName in pkgNames)
            {
                ReleasePackage(pkgName);
            }
        }

        /// <summary>
        /// 是否是从Resources中加载的包
        /// </summary>
        private bool IsFromResources(string packageName)
        {
            return m_FromResourcesPackages.Contains(packageName);
        }
    }
}