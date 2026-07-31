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
    /// 目标：用于自行管理FUI包的描述文件和资源加载，缓存，卸载等操作。
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
        /// 缓存包的引用计数，key:包名，value：该包被引用的次数。
        /// 归零时自动卸载 纹理/音频资源（UnloadAssets），保留包元数据。
        /// 再次引用时自动恢复资源（ReloadAssets）。
        /// </summary>
        private readonly Dictionary<string, int> m_PkgRefCountDict = new();

        /// <summary>
        /// 已卸载资源（UnloadAssets）的包名集合。
        /// 缓存命中时仅对这些包 ReloadAssets 恢复资源，避免对正常包做无谓遍历。
        /// </summary>
        private readonly HashSet<string> m_UnloadedAssetPkgSet = new();

        /// <summary>
        /// AddPkgRef 递归调用栈中的包集合，用于防止循环依赖导致无限递归（栈溢出）。
        /// </summary>
        private readonly HashSet<string> m_AddPkgRefStack = new();

        /// <summary>
        /// SubPkgRef 递归调用栈中的包集合，用于防止循环依赖导致冗余递归。
        /// </summary>
        private readonly HashSet<string> m_SubPkgRefStack = new();

        /// <summary>
        /// 包是否已加载。
        /// </summary>
        /// <param name="pkgName">包名。</param>
        /// <returns>包已加载返回 true，否则返回 false。</returns>
        public bool IsLoadedPkg(string pkgName) => m_LoadedPkgDict.ContainsKey(pkgName);

        /// <summary>
        /// 异步加载指定包（含依赖包）。已加载或正在加载时直接返回，不重复加载。
        /// 缓存命中时仅对曾 UnloadAssets（资源已卸载）的包 ReloadAssets 恢复资源。
        /// </summary>
        /// <param name="pkgName">包名。</param>
        /// <returns>加载完成的 UI 包实例。</returns>
        public UniTask<UIPackage> LoadPkgAsync(string pkgName)
        {
            // 已经加载过的包直接返回；仅当资源曾卸载（UnloadAssets 状态）时恢复，避免 UI 空白
            if (m_LoadedPkgDict.TryGetValue(pkgName, out var loadedPkg))
            {
                if (m_UnloadedAssetPkgSet.Remove(pkgName)) // 恢复后清除标记，避免反复遍历
                {
                    loadedPkg.ReloadAssets();
                }
                return UniTask.FromResult(loadedPkg);
            }

            // 如果已有正在加载的任务，直接返回任务
            if (m_LoadingTasks.TryGetValue(pkgName, out var loadingTask))
            {
                return loadingTask;
            }

            FuLogger.LogInfo($"[FuiPkgManager] 添加UIPackage包: {pkgName}");

            // 创建取消令牌源
            var cts = new CancellationTokenSource();
            m_LoadingCts[pkgName] = cts;

            // 立即启动加载任务（async 同步执行到第一个 await，不 await 也会继续并最终清理）
            var newTask = LoadPkgTaskAsync(pkgName, cts);

            // 记录正在加载的任务
            m_LoadingTasks[pkgName] = newTask;
            return newTask;
        }

        /// <summary>
        /// 执行包加载任务（含依赖），完成后缓存。即使调用方不 await 任务，也会在 PlayerLoop 中继续并最终清理。
        /// </summary>
        /// <param name="pkgName">包名。</param>
        /// <param name="cts">取消令牌源。</param>
        /// <returns>加载完成的 UI 包实例。</returns>
        private async UniTask<UIPackage> LoadPkgTaskAsync(string pkgName, CancellationTokenSource cts)
        {
            try
            {
                // 检查是否被取消
                cts.Token.ThrowIfCancellationRequested();

                // 等待加载包和其依赖包
                var pkg = await LoadPkgAndDepPkgAsync(pkgName);

                // 缓存结果
                m_LoadedPkgDict[pkgName] = pkg;

                return pkg;
            }
            finally
            {
                // 加载完成后移除任务和取消令牌源，并释放 CTS（防止丢弃任务时残留）
                m_LoadingTasks.Remove(pkgName);
                m_LoadingCts.Remove(pkgName);
                cts.Dispose();
            }
        }

        /// <summary>
        /// 异步加载指定包和所有依赖包（自身 + 依赖，无缓存检查）。
        /// </summary>
        /// <param name="pkgName">包名。</param>
        /// <returns>加载完成的 UI 包实例。</returns>
        private async UniTask<UIPackage> LoadPkgAndDepPkgAsync(string pkgName)
        {
            var pkg = await LoadPkgInternalAsync(pkgName);
            await LoadDepPkgAsync(pkg);
            return pkg;
        }

        /// <summary>
        /// 异步加载单个包自身（不含依赖包）。
        /// </summary>
        /// <param name="pkgName">包名。</param>
        /// <returns>加载完成的 UI 包实例。</returns>
        private async UniTask<UIPackage> LoadPkgInternalAsync(string pkgName)
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
        /// 并行加载指定包的所有依赖包（已加载或正在加载的跳过，防止循环依赖死锁）。
        /// </summary>
        /// <param name="pkg">已加载的包，读取其依赖列表。</param>
        private async UniTask LoadDepPkgAsync(UIPackage pkg)
        {
            if (pkg == null) return; // 防御：AddPackage 解析失败可能返回 null（仅编辑器非运行时）

            var tasks = new List<UniTask>();
            foreach (var dep in pkg.dependencies)
            {
                if (dep.TryGetValue("name", out var depPkgName))
                {
                    // 已加载的跳过；正在加载的说明是循环依赖，也跳过加载
                    if (!m_LoadedPkgDict.ContainsKey(depPkgName) && !m_LoadingTasks.ContainsKey(depPkgName))
                    {
                        tasks.Add(LoadPkgAsync(depPkgName));
                    }
                }
            }

            // 并行加载所有依赖
            await UniTask.WhenAll(tasks);

            // 依赖加载完成后，若包已被取消则中断（防止 AddPackage 后取消不被观察的残留窗口）
            if (m_LoadingCts.TryGetValue(pkg.name, out var cts))
                cts.Token.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// 加载包的描述文件（_fui.bytes）。
        /// </summary>
        /// <param name="pkgName">包名。</param>
        /// <returns>包描述文件 TextAsset。</returns>
        /// <exception cref="InvalidOperationException">包名为空时抛出。</exception>
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
        /// 异步加载包中的单个资源文件（图集、音频等），并绑定到包内资源项。
        /// </summary>
        /// <param name="assetName">资源名（不含扩展名）。</param>
        /// <param name="extension">资源扩展名，如 .png、.ogg。</param>
        /// <param name="type">资源类型，如 Texture、AudioClip。</param>
        /// <param name="pkgItem">包内资源项，加载完成后绑定资源。</param>
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
        /// 添加包引用。引用计数从 0 变为 1 时自动恢复 纹理/音频资源，并递归递增依赖包引用。
        /// </summary>
        /// <param name="pkgName">包名。</param>
        public void AddPkgRef(string pkgName)
        {
            if (!m_AddPkgRefStack.Add(pkgName)) return; // 已在递归栈中（循环依赖），跳过

            try
            {
                var wasZero = !m_PkgRefCountDict.TryGetValue(pkgName, out var count) || count == 0;

                // 已存在且未归零 → 自增；不存在或已归零 → 初始化为 1
                m_PkgRefCountDict[pkgName] = wasZero ? 1 : count + 1;

                FuLogger.LogInfo($"[FuiPkgManager] 增加UIPackage包资源引用: {pkgName}，当前引用计数: {m_PkgRefCountDict[pkgName]}");

                // 0→1 时恢复纹理/音频资源 + 递归递增依赖包引用
                if (m_LoadedPkgDict.TryGetValue(pkgName, out var pkg))
                {
                    if (wasZero)
                    {
                        pkg.ReloadAssets();
                        m_UnloadedAssetPkgSet.Remove(pkgName); // 资源已恢复，清除标记
                    }

                    foreach (var dep in pkg.dependencies)
                    {
                        if (dep.TryGetValue("name", out var depPkgName))
                            AddPkgRef(depPkgName);
                    }
                }
            }
            finally
            {
                m_AddPkgRefStack.Remove(pkgName);
            }
        }

        /// <summary>
        /// 减少包引用。递归递减依赖包引用；计数归零时卸载 纹理/音频资源（UnloadAssets）并释放 YooAsset 句柄（UnloadAll），保留包元数据。
        /// </summary>
        /// <param name="pkgName">包名。</param>
        public void SubPkgRef(string pkgName)
        {
            if (!m_SubPkgRefStack.Add(pkgName)) return; // 已在递归栈中（循环依赖），跳过

            try
            {
                if (!m_PkgRefCountDict.TryGetValue(pkgName, out var count)) return;
                if (count <= 0) return; // 已归零：防止循环依赖导致重复卸载

                count = --m_PkgRefCountDict[pkgName];
                FuLogger.LogInfo($"[FuiPkgManager] 减少UIPackage包资源引用: {pkgName}，当前引用计数: {count}");

                // 递归递减依赖包引用（对称于 AddPkgRef）+ 归零时卸载资源
                if (m_LoadedPkgDict.TryGetValue(pkgName, out var pkg))
                {
                    foreach (var dep in pkg.dependencies)
                    {
                        if (dep.TryGetValue("name", out var depPkgName))
                            SubPkgRef(depPkgName);
                    }

                    if (count == 0)
                    {
                        pkg.UnloadAssets();
                        m_UnloadedAssetPkgSet.Add(pkgName); // 标记资源已卸载，缓存命中时需恢复
                        FuLogger.LogInfo($"[FuiPkgManager] 卸载UIPackage资源: {pkgName}（包元数据保留）");

                        // 释放 YooAsset 资源句柄，让 AssetBundle 得以卸载（避免句柄悬挂导致内存泄漏）
                        if (m_PkgAssetLoaderDict.TryGetValue(pkgName, out var loader))
                            loader.UnloadAll();
                    }
                }
            }
            finally
            {
                m_SubPkgRefStack.Remove(pkgName);
            }
        }

        /// <summary>
        /// 完全移除所有包（元数据 + 纹理/音频资源 + FGUI 全局缓存），用于游戏退出。
        /// </summary>
        public void RemoveAllPkg()
        {
            // 先取消所有正在加载的包
            List<string> loadingPkgNames = new(m_LoadingCts.Keys);
            foreach (var pkgName in loadingPkgNames)
            {
                RemovePkg(pkgName);
            }

            // 再移除所有已加载的包（先复制Keys避免遍历时修改集合）
            List<string> loadedPkgNames = new(m_LoadedPkgDict.Keys);
            foreach (var pkgName in loadedPkgNames)
            {
                RemovePkg(pkgName);
            }
        }

        /// <summary>
        /// 完全移除指定包：元数据 + 纹理/音频资源 + 从 FGUI 全局缓存移除。彻底删除，无法恢复。
        /// 日常引用计数归零时不会调用此方法，而是调用 UnloadAssets（仅释放 纹理/音频资源，元数据保留）。
        /// 移除时按引用数递归递减依赖包计数。
        /// </summary>
        /// <param name="pkgName">要完全移除的包名。</param>
        public void RemovePkg(string pkgName)
        {
            // 1.如果是正在加载的包，取消正在加载的任务
            //   （加载中的包 GetByName 为 null，必须先于第 2 步处理）
            //   注意：只 Cancel 不移除 m_LoadingCts——任务续体需检查到已取消的 token 才能正确中断；
            //   m_LoadingCts/m_LoadingTasks 由 LoadPkgTaskAsync 的 finally 清理，此处移除会使取消检查失效。
            if (m_LoadingCts.TryGetValue(pkgName, out var cts))
            {
                cts.Cancel();
                FuLogger.LogInfo($"[FuiPkgManager] 取消正在加载的UIPackage: {pkgName}");
                return;
            }

            // 2.记录引用数，用于递减依赖包（A 的每个引用都对依赖贡献 1）
            var refCount = m_PkgRefCountDict.GetValueOrDefault(pkgName, 0);

            // 3.FUI移除UIPackage包（先取依赖表，RemovePackage 后依赖信息可能失效）
            var pkgToRemove = UIPackage.GetByName(pkgName);
            if (pkgToRemove == null) return;
            UIPackage.RemovePackage(pkgName);

            // 4.递归递减依赖包的引用计数
            foreach (var dep in pkgToRemove.dependencies)
            {
                if (dep.TryGetValue("name", out var depPkgName))
                {
                    for (var i = 0; i < refCount; i++)
                        SubPkgRef(depPkgName);
                }
            }

            // 5.从已加载字典移除
            if (!m_LoadedPkgDict.Remove(pkgName, out _)) return;

            // 6.释放包的描述文件资源和资源，包括atlas图集资源，音频资源，spine动画资源等
            if (m_PkgAssetLoaderDict.Remove(pkgName, out var assetLoader))
            {
                assetLoader.Release();
                FuLogger.LogInfo($"[FuiPkgManager] 释放UIPackage-{pkgName}内的资源完成.");
            }

            // 7.移除引用计数和卸载标记
            m_PkgRefCountDict.Remove(pkgName);
            m_UnloadedAssetPkgSet.Remove(pkgName);
        }
    }
}