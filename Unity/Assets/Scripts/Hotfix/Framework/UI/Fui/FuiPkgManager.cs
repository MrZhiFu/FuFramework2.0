using System;
using FairyGUI;
using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;
using AOT.Framework.Core.Log;
using UtilityAOT = AOT.Framework.Core.Utility.UtilityAOT;
using Hotfix.Framework.Asset;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.UI
{
    /// <summary>
    /// FUI包管理器。
    /// 目标：用于自行管理FUI包的资源加载，缓存，卸载等操作。
    /// 功能：
    ///     1. 异步加载FUI包。
    ///     2. 缓存已加载的FUI包。
    ///     3. 卸载FUI包。
    ///     4. 支持通过事件系统通知包加载完成。
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
        /// 缓存包的引用计数，key:包名，value：引用数量。
        /// 归零时自动卸载 纹理/音频资源（UnloadAssets），保留包元数据。
        /// 再次引用时自动恢复资源（ReloadAssets）。
        /// </summary>
        private readonly Dictionary<string, int> m_PkgRefCountDict = new();

        /// <summary>
        /// 是否存在指定包
        /// </summary>
        /// <param name="pkgName"></param>
        /// <returns></returns>
        public bool HasPackage(string pkgName) => m_LoadedPkgDict.ContainsKey(pkgName);

        /// <summary>
        /// 异步加载指定包
        /// </summary>
        /// <param name="pkgName"></param>
        /// <returns></returns>
        public UniTask<UIPackage> LoadPkgAsync(string pkgName)
        {
            // 已经加载过的包直接返回
            if (m_LoadedPkgDict.TryGetValue(pkgName, out var loadedPkg))
                return UniTask.FromResult(loadedPkg);

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

                    // 等待加载包和其依赖包
                    var pkg = await LoadSelfPkgAndDepPkgAsync(pkgName);

                    // 缓存结果
                    m_LoadedPkgDict[pkgName] = pkg;
                    pkg.ReloadAssets();

                    return pkg;
                }
                finally
                {
                    // 加载完成后移除任务和取消令牌源
                    m_LoadingTasks.Remove(pkgName);
                    m_LoadingCts.Remove(pkgName);
                }
            });

            // 记录正在加载的任务
            m_LoadingTasks[pkgName] = newTask;
            return newTask;
        }

        /// <summary>
        /// 异步加载指定包和所有依赖包
        /// </summary>
        /// <param name="pkgName"></param>
        /// <returns></returns>
        private async UniTask<UIPackage> LoadSelfPkgAndDepPkgAsync(string pkgName)
        {
            var pkg = await LoadSelfPkgAsync(pkgName);
            await LoadDepPkgAsync(pkg);
            return pkg;
        }

        /// <summary>
        /// 异步加载指定包
        /// </summary>
        /// <param name="pkgName">包名</param>
        /// <returns></returns>
        private async UniTask<UIPackage> LoadSelfPkgAsync(string pkgName)
        {
            try
            {
                // 检查是否被取消
                if (m_LoadingCts.TryGetValue(pkgName, out var cts))
                    cts.Token.ThrowIfCancellationRequested();

                // 加载包的描述文件
                var pkgDesc = await LoadDescAsync(pkgName);

                // 检查是否被取消
                if (m_LoadingCts.TryGetValue(pkgName, out cts))
                    cts.Token.ThrowIfCancellationRequested();

                // 加载完成后，添加到UIPackage中，并加载pkg中的资源
                var loadedPkg = UIPackage.AddPackage(pkgDesc.bytes, string.Empty, (assetName, extension, type, pkgItem) => { LoadResAsync(assetName, extension, type, pkgItem).Forget(); });

                return loadedPkg;
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
        /// <param name="pkg">包</param>
        private async UniTask LoadDepPkgAsync(UIPackage pkg)
        {
            var tasks = new List<UniTask>();
            foreach (var dep in pkg.dependencies)
            {
                if (dep.TryGetValue("name", out var depPkgName))
                {
                    // 已加载的跳过；正在加载的说明是循环依赖，也跳过加载，但引用计数仍需增加
                    if (!m_LoadedPkgDict.ContainsKey(depPkgName) && !m_LoadingTasks.ContainsKey(depPkgName))
                    {
                        tasks.Add(LoadPkgAsync(depPkgName));
                    }

                    AddPkgRef(depPkgName);
                }
            }

            // 并行加载所有依赖
            await UniTask.WhenAll(tasks);
        }

        /// <summary>
        /// 加载包的描述文件
        /// </summary>
        /// <param name="pkgName"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private async UniTask<TextAsset> LoadDescAsync(string pkgName)
        {
            if (string.IsNullOrEmpty(pkgName)) throw new InvalidOperationException("[FuiPkgManager] 包名不能为空.");

            //"Assets/Bundles/UI/";
            var rootPath = UtilityAOT.AssetPath.GetUIRootPath();
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
        /// <param name="pkgItem">包内资源项</param>
        private async UniTaskVoid LoadResAsync(string assetName, string extension, Type type, PackageItem pkgItem)
        {
            var pkgName  = pkgItem.owner.name;
            var rootPath = UtilityAOT.AssetPath.GetUIRootPath(); //"Assets/Bundles/UI/";
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
            pkgItem.owner.SetItemAsset(pkgItem, assetObj, DestroyMethod.Unload);
        }

        /// <summary>
        /// 添加包引用。引用计数从 0 变为 1 时自动恢复 纹理/音频资源。
        /// </summary>
        /// <param name="pkgName">包名</param>
        public void AddPkgRef(string pkgName)
        {
            var wasZero = !m_PkgRefCountDict.TryGetValue(pkgName, out var count) || count == 0;

            if (!m_PkgRefCountDict.TryAdd(pkgName, 1))
                m_PkgRefCountDict[pkgName] += 1;

            FuLogger.LogInfo($"[FuiPkgManager] 增加UIPackage包资源引用: {pkgName}，当前引用计数: {m_PkgRefCountDict[pkgName]}");

            // 引用计数从 0 变为 1，恢复纹理/音频资源
            if (wasZero && m_LoadedPkgDict.TryGetValue(pkgName, out var pkg))
            {
                pkg.ReloadAssets();
            }
        }

        /// <summary>
        /// 减少包引用。引用计数归零时自动卸载 纹理/音频资源，保留包元数据。
        /// </summary>
        /// <param name="pkgName">包名</param>
        public void SubPkgRef(string pkgName)
        {
            if (!m_PkgRefCountDict.TryGetValue(pkgName, out var count)) return;
            if (count <= 0) return; // 已归零：防止循环依赖导致重复卸载

            count = --m_PkgRefCountDict[pkgName];
            FuLogger.LogInfo($"[FuiPkgManager] 减少UIPackage包资源引用: {pkgName}，当前引用计数: {count}");

            if (count > 0) return;

            if (!m_LoadedPkgDict.TryGetValue(pkgName, out var pkg)) return;

            // 归零：卸载 纹理/音频资源，保留包元数据
            pkg.UnloadAssets();
            FuLogger.LogInfo($"[FuiPkgManager] 卸载UIPackage资源: {pkgName}（包元数据保留）");

            // 递归处理依赖包
            foreach (var dep in pkg.dependencies)
            {
                if (dep.TryGetValue("name", out var depPkgName))
                    SubPkgRef(depPkgName);
            }
        }

        /// <summary>
        /// 完全卸载所有包（元数据 + 纹理/音频资源 + FGUI 全局缓存），用于游戏退出。
        /// </summary>
        public void ReleaseAllPkg()
        {
            // 释放所有已加载的包（先复制Keys避免遍历时修改集合）
            List<string> pkgNames = new(m_LoadedPkgDict.Keys);
            foreach (var pkgName in pkgNames)
            {
                ReleasePkg(pkgName);
            }
        }

        /// <summary>
        /// 完全卸载指定包：元数据 + 纹理/音频资源 + 从 FGUI 全局缓存移除。彻底删除，无法恢复。
        /// 日常引用计数归零时不会调用此方法，而是调用 UnloadAssets（仅释放 纹理/音频资源，元数据保留）。
        /// </summary>
        public void ReleasePkg(string pkgName)
        {
            // 1.FUI移除UIPackage包
            if (UIPackage.GetByName(pkgName) == null) return;
            UIPackage.RemovePackage(pkgName);

            // 2.如果是正在加载的包，取消正在加载的任务
            if (m_LoadingCts.TryGetValue(pkgName, out var cts))
            {
                cts.Cancel();
                m_LoadingCts.Remove(pkgName);
                FuLogger.LogInfo($"[FuiPkgManager] 取消正在加载的UIPackage: {pkgName}");
                return;
            }

            // 3.从已加载字典移除
            if (!m_LoadedPkgDict.Remove(pkgName, out _)) return;

            // 4.释放包的描述文件资源和资源，包括atlas图集资源，音频资源，spine动画资源等
            if (m_PkgAssetLoaderDict.Remove(pkgName, out var assetLoader))
            {
                assetLoader.Release();
                FuLogger.LogInfo($"[FuiPkgManager] 释放UIPackage-{pkgName}内的资源完成.");
            }

            // 5.移除引用计数
            m_PkgRefCountDict.Remove(pkgName);
        }
    }
}