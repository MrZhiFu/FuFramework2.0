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
    /// FUI包管理器：管理 FairyGUI UI 包（UIPackage）的加载、缓存、引用计数与资源生命周期。
    ///
    /// 【核心资源模型】
    /// 包 = 元数据（_fui.bytes 解析出的 UIPackage，常驻）+ 纹理/音频资源（随引用计数释放/恢复）。
    ///   引用归零 → UnloadAssets（释放纹理/音频，元数据保留）+ UnloadAll（释放 YooAsset 句柄，AssetBundle 可卸载）
    ///   引用 0→1 → ReloadAssets（重新加载纹理/音频）
    ///
    /// 【关键保障】
    /// 1. 加载去重：m_LoadingTasks 保证同一包仅一个加载任务，并发请求共享结果。
    /// 2. 循环依赖防护：AddPkgRef/SubPkgRef 用递归栈防栈溢出；加载用 m_LoadingTasks 防死锁。
    /// 3. 取消语义：RemovePkg 取消加载中任务（cts.Cancel 不移除字典），任务在任意 await 边界观察取消并清理。
    /// 4. 资源恢复：m_UnloadedAssetPkgSet 标记已卸载资源包，缓存命中仅对其 ReloadAssets，避免无谓遍历。
    /// 5. 完全移除：RemovePkg/RemoveAllPkg 彻底删除（元数据+资源+FGUI 全局缓存），用于退出或显式清理。
    ///
    /// 【与 YooAsset 集成】经 AssetLoadRegister 加载资源，UnloadAll 释放句柄让 AssetBundle 可卸载，防句柄悬挂泄漏。
    /// </summary>
    public class FuiPkgManager
    {
        /// <summary>
        /// 缓存已加载的包的字典，key:包名，value：包
        /// </summary>
        private readonly Dictionary<string, UIPackage> m_LoadedPkgDict = new();

        /// <summary>
        /// 加载任务去重字典，key:包名，value：加载任务。
        /// 同一包仅一个加载任务，并发请求共享同一任务；任务完成后由 finally 移除。
        /// </summary>
        private readonly Dictionary<string, UniTask<UIPackage>> m_LoadingTasks = new();

        /// <summary>
        /// 加载任务取消令牌源字典，key:包名，value：CTS。
        /// 注意：RemovePkg 取消时仅 Cancel 不移除——任务续体需检查到已取消 token 才能正确中断，
        /// 条目由 LoadPkgTaskAsync 的 finally 统一移除并 Dispose。
        /// </summary>
        private readonly Dictionary<string, CancellationTokenSource> m_LoadingCts = new();

        /// <summary>
        /// 包对应的资源加载器字典，key:包名，value：资源加载器，一个包对应一个资源加载器，用于加载包的描述文件和资源文件
        /// </summary>
        private readonly Dictionary<string, AssetLoadRegister> m_PkgAssetLoaderDict = new();

        /// <summary>
        /// 包引用计数，key:包名，value：存活实例数（引用该包的 Win 实例总数）。
        /// 语义：引用绑定"实例存活"而非"界面显示"——Win 在 OnInit 加引用、Dispose 减引用，
        /// 界面开关不影响计数。归零时 UnloadAssets + UnloadAll，0→1 时 ReloadAssets。
        /// AddPkgRef/SubPkgRef 每次递归处理依赖包（对称），循环依赖由递归栈防护。
        /// </summary>
        private readonly Dictionary<string, int> m_PkgRefCountDict = new();

        /// <summary>
        /// 资源已卸载（UnloadAssets）的包名集合。
        /// 缓存命中时仅对标记包 ReloadAssets 恢复资源（恢复后即移除标记），
        /// 正常包缓存命中零遍历开销。
        /// </summary>
        private readonly HashSet<string> m_UnloadedAssetPkgSet = new();

        /// <summary>
        /// AddPkgRef 递归栈，防止循环依赖（A↔B）下递增无截断导致无限递归栈溢出。
        /// 已在栈中则跳过，保证循环依赖整环仅增/减一次。
        /// </summary>
        private readonly HashSet<string> m_AddPkgRefStack = new();

        /// <summary>
        /// SubPkgRef 递归栈，防止循环依赖下冗余递归。
        /// </summary>
        private readonly HashSet<string> m_SubPkgRefStack = new();

        /// <summary>
        /// 包是否已加载。
        /// </summary>
        /// <param name="pkgName">包名。</param>
        /// <returns>包已加载返回 true，否则返回 false。</returns>
        public bool IsLoadedPkg(string pkgName) => m_LoadedPkgDict.ContainsKey(pkgName);

        /// <summary>
        /// 异步加载指定包（含依赖包）。
        /// 去重：已加载 → 直接返回缓存；正在加载 → 返回同一任务（共享结果，不重复加载）。
        /// 缓存命中时仅对曾 UnloadAssets 的包（m_UnloadedAssetPkgSet 标记）ReloadAssets 恢复资源，
        /// 正常包 O(1) 判断零遍历开销。
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
        /// 执行包加载任务（含依赖），完成后缓存。
        /// 立即执行（async 同步到第一个 await，续体在 PlayerLoop 调度）：即使调用方丢弃任务也会跑完，
        /// finally 统一清理 m_LoadingTasks/m_LoadingCts 并 Dispose CTS——不会因丢弃任务而残留。
        /// 取消语义：任意 await 边界检查 cts 取消则抛异常，不写入 m_LoadedPkgDict。
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
        /// 并行加载指定包的所有依赖包。
        /// 已加载的跳过；正在加载的说明是循环依赖，也跳过（防死锁）。
        /// WhenAll 后检查取消：AddPackage 后若包被取消则中断，防残留窗口。
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
        /// 添加包引用（Win 实例存活时调用，OnInit 处）。
        /// 计数 0→1 时 ReloadAssets 恢复纹理/音频并清除卸载标记；递归递增依赖包引用（对称于 SubPkgRef）。
        /// 递归栈（m_AddPkgRefStack）防护循环依赖：已在栈中则跳过，循环依赖整环仅增一次。
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
        /// 减少包引用（Win 实例销毁时调用，Dispose 处）。
        /// 每次递归递减依赖包引用（对称于 AddPkgRef）；计数归零时：
        ///   UnloadAssets 释放纹理/音频（元数据保留）+ 标记 m_UnloadedAssetPkgSet + UnloadAll 释放 YooAsset 句柄。
        /// 递归栈防护循环依赖：整环一起递减，最后一个引用释放整环卸载。
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
        /// 两阶段：先取消所有加载中的包（cts.Cancel），再移除所有已加载的包。
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
        /// 与引用计数归零的区别：归零只 UnloadAssets（释放资源、保留元数据、可恢复）；
        /// RemovePkg 是彻底删除，之后需重新 LoadPkgAsync 加载。
        /// 加载中取消：仅 cts.Cancel（不移除字典），任务续体观察取消后由 finally 清理。
        /// 移除时按引用数递归递减依赖包计数（A 的每个引用对依赖贡献 1）。
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
                assetLoader.UnloadAll(); // 先释放 YooAsset 句柄让 AssetBundle 可卸载（Release 仅归还池，不清理句柄）
                assetLoader.Release();   // 再归还引用池
                FuLogger.LogInfo($"[FuiPkgManager] 释放UIPackage-{pkgName}内的资源完成.");
            }

            // 7.移除引用计数和卸载标记
            m_PkgRefCountDict.Remove(pkgName);
            m_UnloadedAssetPkgSet.Remove(pkgName);
        }
    }
}