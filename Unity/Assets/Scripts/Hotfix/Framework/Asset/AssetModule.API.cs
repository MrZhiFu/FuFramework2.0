using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using YooAsset;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Hotfix.Framework.Asset
{
    /// <summary>
    /// 资源管理模块的公共 API。
    /// 功能：
    ///     1. 异步加载资源/场景，异步实例化游戏物体。
    ///     2. 资源卸载与查询。
    /// </summary>
    public partial class AssetModule : ModuleBase
    {
        #region 异步加载资源

        /// <summary>
        /// 异步加载资源。
        /// 注意1：YooAsset 的 await 不抛异常——资源加载失败时本方法仍返回句柄，其 Status 为 Failed、AssetObject 为 null，
        /// 调用方必须先检查 handle.Status == EOperationStatus.Succeeded 再使用资源。
        /// 注意2：返回的句柄无论成败均须调用 Release()（失败不释放则 provider 引用计数不归零、资源永不卸载）。
        /// 注意3：模块销毁（OnDispose）时本方法会抛 OperationCanceledException（句柄已自动释放并卸载），调用方可按需捕获。
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="token">取消令牌</param>
        /// <returns>资源句柄，使用完毕必须调用 Release()。</returns>
        public async UniTask<AssetHandle> LoadAssetAsync(string path, CancellationToken token)
        {
            m_Scope.Token.ThrowIfCancellationRequested(); // 入口：模块已销毁则拒绝
            token.ThrowIfCancellationRequested();         // 调用方已取消（如窗口关闭）则拒绝
            using (m_Scope.Begin())                       // 登记在途：OnDispose 取消时等待本操作清理完毕
            {
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(m_Scope.Token, token)) // 模块或调用方任一取消即中止
                {
                    var package = GetReadyDefaultPackage();
                    var handle  = package.LoadAssetAsync(path);
                    try
                    {
                        // YooAsset 官方 UniTask 集成：cancelImmediately=true 时 Token 取消立即完成 await 并抛 OperationCanceledException
                        await handle.ToUniTask(cancellationToken: linkedCts.Token, cancelImmediately: true);
                        return handle;
                    }
                    catch (OperationCanceledException)
                    {
                        // 取消路径必须配对：Release 句柄 + UnloadAsset 卸载 bundle（AutoUnloadBundleWhenUnused=false 下仅 Release 不卸载）
                        handle.Release();
                        UnloadAsset(path);
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// 异步加载资源。
        /// 注意1：YooAsset 的 await 不抛异常——资源加载失败时本方法仍返回句柄，其 Status 为 Failed、AssetObject 为 null，
        /// 调用方必须先检查 handle.Status == EOperationStatus.Succeeded 再使用资源。
        /// 注意2：返回的句柄无论成败均须调用 Release()（失败不释放则 provider 引用计数不归零、资源永不卸载）。
        /// 注意3：模块销毁（OnDispose）时本方法会抛 OperationCanceledException（句柄已自动释放并卸载），调用方可按需捕获。
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="token">取消令牌</param>
        /// <typeparam name="T">资源类型</typeparam>
        /// <returns>资源句柄，使用完毕必须调用 Release()。</returns>
        public async UniTask<AssetHandle> LoadAssetAsync<T>(string path, CancellationToken token) where T : Object
        {
            m_Scope.Token.ThrowIfCancellationRequested(); // 入口：模块已销毁则拒绝
            token.ThrowIfCancellationRequested();         // 调用方已取消（如窗口关闭）则拒绝
            using (m_Scope.Begin())                       // 登记在途：OnDispose 取消时等待本操作清理完毕
            {
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(m_Scope.Token, token)) // 模块或调用方任一取消即中止
                {
                    var package = GetReadyDefaultPackage();
                    var handle  = package.LoadAssetAsync<T>(path);
                    try
                    {
                        // YooAsset 官方 UniTask 集成：cancelImmediately=true 时 Token 取消立即完成 await 并抛 OperationCanceledException
                        await handle.ToUniTask(cancellationToken: linkedCts.Token, cancelImmediately: true);
                        return handle;
                    }
                    catch (OperationCanceledException)
                    {
                        // 取消路径必须配对：Release 句柄 + UnloadAsset 卸载 bundle（AutoUnloadBundleWhenUnused=false 下仅 Release 不卸载）
                        handle.Release();
                        UnloadAsset(path);
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// 异步加载资源。
        /// 注意1：YooAsset 的 await 不抛异常——资源加载失败时本方法仍返回句柄，其 Status 为 Failed、AssetObject 为 null，
        /// 调用方必须先检查 handle.Status == EOperationStatus.Succeeded 再使用资源。
        /// 注意2：返回的句柄无论成败均须调用 Release()（失败不释放则 provider 引用计数不归零、资源永不卸载）。
        /// 注意3：模块销毁（OnDispose）时本方法会抛 OperationCanceledException（句柄已自动释放并卸载），调用方可按需捕获。
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="type">资源类型</param>
        /// <param name="token">取消令牌</param>
        /// <returns>资源句柄，使用完毕必须调用 Release()。</returns>
        public async UniTask<AssetHandle> LoadAssetAsync(string path, Type type, CancellationToken token)
        {
            m_Scope.Token.ThrowIfCancellationRequested(); // 入口：模块已销毁则拒绝
            token.ThrowIfCancellationRequested();         // 调用方已取消（如窗口关闭）则拒绝
            using (m_Scope.Begin())                       // 登记在途：OnDispose 取消时等待本操作清理完毕
            {
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(m_Scope.Token, token)) // 模块或调用方任一取消即中止
                {
                    var package = GetReadyDefaultPackage();
                    var handle  = package.LoadAssetAsync(path, type);
                    try
                    {
                        // YooAsset 官方 UniTask 集成：cancelImmediately=true 时 Token 取消立即完成 await 并抛 OperationCanceledException
                        await handle.ToUniTask(cancellationToken: linkedCts.Token, cancelImmediately: true);
                        return handle;
                    }
                    catch (OperationCanceledException)
                    {
                        // 取消路径必须配对：Release 句柄 + UnloadAsset 卸载 bundle（AutoUnloadBundleWhenUnused=false 下仅 Release 不卸载）
                        handle.Release();
                        UnloadAsset(path);
                        throw;
                    }
                }
            }
        }

        #endregion

        #region 异步加载场景

        /// <summary>
        /// 异步加载场景（加载完成自动激活）。
        /// onProgress：加载进度回调（0~1），每帧上报一次直至加载完成；不需要进度时传 null。
        /// 加载失败时内部释放句柄并抛异常，调用方无需也无法释放失败句柄。
        /// 注意：模块销毁（OnDispose）时本方法会抛 OperationCanceledException（场景句柄已自动释放），调用方可按需捕获。
        /// 返回的 SceneHandle 使用完毕后必须调用 Release()，否则资源永不卸载。
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="sceneMode">场景模式</param>
        /// <param name="token">取消令牌</param>
        /// <param name="onProgress">加载进度回调（可选）。</param>
        /// <returns>场景句柄，使用完毕须调用 Release。</returns>
        public async UniTask<SceneHandle> LoadSceneAsync(string path, LoadSceneMode sceneMode, CancellationToken token, Action<float> onProgress = null)
        {
            m_Scope.Token.ThrowIfCancellationRequested();
            token.ThrowIfCancellationRequested();
            using (m_Scope.Begin())
            {
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(m_Scope.Token, token)) // 模块或调用方任一取消即中止
                {
                    var package = GetReadyDefaultPackage();
                    var handle  = package.LoadSceneAsync(path, sceneMode);
                    try
                    {
                        // 可选进度上报：每帧轮询 handle.Progress 直到加载完成；同时观察取消令牌
                        if (onProgress != null)
                        {
                            while (!handle.IsDone)
                            {
                                linkedCts.Token.ThrowIfCancellationRequested();
                                TryReportProgress(onProgress, handle.Progress);
                                await UniTask.Yield();
                            }

                            TryReportProgress(onProgress, handle.Progress);
                        }

                        await handle.ToUniTask(cancellationToken: linkedCts.Token, cancelImmediately: true);
                        if (handle.Status != EOperationStatus.Succeeded)
                        {
                            handle.Release();
                            throw new InvalidOperationException($"[AssetModule]场景加载失败：{path}");
                        }

                        return handle;
                    }
                    catch (OperationCanceledException)
                    {
                        // 取消：释放场景句柄（场景走 SceneManager 生命周期，不调 UnloadAsset）
                        handle.Release();
                        throw;
                    }
                }
            }
        }

        #endregion

        #region 异步实例化游戏物体

        /// <summary>
        /// 异步实例化游戏物体。
        /// 句柄按路径缓存并引用计数：同一 prefab 多实例共享句柄，实例销毁时调用 ReleaseInstantiate 释放。
        /// 返回 InstantiateResult（携带实例与创建时生命周期代数）：重启（OnDispose/重新初始化）后
        /// 旧生命周期存活的实例调用 ReleaseInstantiate 会被代际校验识别并忽略，杜绝误释放新生命周期同路径引用。
        /// 注意：同步/异步首次实例化请勿混用同一路径（首次加载去重仅覆盖异步路径）。
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="token">取消令牌</param>
        /// <returns>实例化结果，实例销毁时调用 ReleaseInstantiate(result) 释放引用。</returns>
        public async UniTask<InstantiateResult> InstantiateAsync(string path, CancellationToken token)
        {
            m_Scope.Token.ThrowIfCancellationRequested(); // 入口：模块已销毁（Token 取消）则拒绝，抛 OperationCanceledException
            token.ThrowIfCancellationRequested();         // 调用方已取消（如窗口关闭）则拒绝
            var capturedToken = m_Scope.Token;            // 在途守卫 + 结果标记：捕获本生命周期 Token（旧 Token 被取消或更换即识别为旧生命周期）

            AssetHandle assetHandle;
            if (m_InstantiateRefDict.TryGetValue(path, out var entry))
            {
                entry.RefCount++;
                assetHandle = entry.Handle;
            }
            else
            {
                // 并发首次加载去重：共享完成源（UniTaskCompletionSource.Task 可被多个调用方 await）
                if (!m_InstantiateLoadingTasks.TryGetValue(path, out var sharedSource))
                {
                    sharedSource                    = new UniTaskCompletionSource<AssetHandle>();
                    m_InstantiateLoadingTasks[path] = sharedSource;
                    LoadAsyncForInstantiate(path, sharedSource, capturedToken, token).Forget();
                }

                assetHandle = await sharedSource.Task;

                // 模块销毁/生命周期变更/调用方取消后：句柄可能已被释放，不得再写回引用字典
                if (capturedToken.IsCancellationRequested || capturedToken != m_Scope.Token || token.IsCancellationRequested)
                {
                    if (assetHandle is { IsValid: true })
                    {
                        assetHandle.Release();

                        // 中止路径（调用方取消/跨生命周期）：加载成功的句柄仅 Release 在 AutoUnloadBundleWhenUnused=false 下不卸载 bundle，
                        // 配对 UnloadAsset 防该 prefab 的 bundle 常驻（新生命周期不再加载同路径时永不释放）
                        UnloadAsset(path);
                    }

                    throw new OperationCanceledException(capturedToken);
                }

                // 加载完成后写入引用；若期间被并发请求写入则复用
                if (m_InstantiateRefDict.TryGetValue(path, out var existing))
                {
                    existing.RefCount++;
                    // 复用并发写入的条目：若句柄不同（共享源已移除后被重建，见 LoadAsyncForInstantiate finally 移除），
                    // 释放本次加载的句柄，避免其 provider 引用计数永久残留
                    if (!ReferenceEquals(existing.Handle, assetHandle))
                    {
                        assetHandle.Release();
                    }

                    assetHandle = existing.Handle;
                }
                else
                {
                    m_InstantiateRefDict[path] = new InstantiateRef { Handle = assetHandle, RefCount = 1 };
                }
            }

            try
            {
                var instantiateOperation = assetHandle.InstantiateAsync();
                await instantiateOperation;

                // 模块销毁/生命周期变更/调用方取消后：句柄已随 OnDispose 释放，不返回孤儿实例；销毁已实例化的对象，抛 OperationCanceledException（catch 中 ReleaseInstantiateInternal 兜底回滚）
                if (capturedToken.IsCancellationRequested || capturedToken != m_Scope.Token || token.IsCancellationRequested)
                {
                    if (instantiateOperation.Result != null)
                        Object.Destroy(instantiateOperation.Result);
                    throw new OperationCanceledException(capturedToken);
                }

                if (instantiateOperation.Result == null)
                    throw new InvalidOperationException($"[AssetModule]实例化资源{path}失败");

                // 携带捕获的生命周期 Token 返回：销毁时经 ReleaseInstantiate(result) 校验 Token，重启后旧实例释放被识别并忽略
                return new InstantiateResult
                {
                    Instance = instantiateOperation.Result,
                    Path     = path,
                    Token    = capturedToken,
                };
            }
            catch
            {
                // 生命周期已变更（重启）时不得回滚引用：引用字典已被 OnDispose 清空，
                // 且可能已存在新生命周期同路径条目，误回滚会卸载新生命周期存活的实例资源；仅在本代际时回滚本次占用
                if (capturedToken == m_Scope.Token)
                    ReleaseInstantiateInternal(path);
                throw;
            }
        }

        /// <summary>
        /// 释放实例化资源的句柄引用。实例对象销毁后调用，传入 InstantiateAsync 返回的 InstantiateResult。
        /// 引用计数归零时 Release 句柄并移除，让资源可被卸载。
        /// 重启（OnDispose/重新初始化）后旧生命周期存活的实例携带旧代际结果调用本方法时会被代际校验识别并忽略，
        /// 杜绝误命中新生命周期同路径条目、导致仍存活的实例资源被静默卸载。
        /// </summary>
        /// <param name="result">InstantiateAsync 返回的实例化结果。</param>
        public void ReleaseInstantiate(InstantiateResult result)
        {
            if (Token.IsCancellationRequested) return;                           // 模块已销毁：引用字典已清空，直接忽略（含 null 参数，teardown 防御不抛）
            if (result == null) throw new ArgumentNullException(nameof(result)); // 存活模块传入 null 属调用方 bug，快速失败
            if (result.Token != m_Scope.Token)
            {
                // 跨生命周期释放：旧生命周期结果携带的 Token 与当前不同，直接忽略防误卸载新生命周期同路径引用；
                // 属调用方违反"重启后不得对旧实例释放"契约，告警便于定位
                FuLogger.LogWarning($"[AssetModule]忽略跨生命周期实例化释放：{result.Path}（结果 Token 属于旧生命周期）。重启后请勿对旧生命周期实例调用 ReleaseInstantiate。");
                return;
            }

            ReleaseInstantiateInternal(result.Path);
        }

        #endregion

        #region 卸载资源

        /// <summary>
        /// 卸载指定资源。
        /// 注意：该资源仍被其他句柄持有时（provider 引用计数 >0），卸载不会生效。
        /// 默认包未就绪（含初始化未完成/失败）时不抛异常，避免 YooAsset CheckInitialized 抛 YooPackageInvalidException。
        /// </summary>
        /// <param name="assetPath">资源路径</param>
        public void UnloadAsset(string assetPath)
        {
            assetPath.NotNull(nameof(assetPath));
            if (!TryGetReadyPackage(out var package)) return; // 防御：包未初始化/不存在/未就绪不抛
            package.TryUnloadUnusedAsset(assetPath);
        }

        #endregion

        #region 资源查询

        /// <summary>
        /// 获取资源信息。
        /// 注意：默认包未就绪（含初始化未完成/失败）时返回 null，避免同步抛异常。
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns>资源信息，默认包未就绪时返回 null。</returns>
        public AssetInfo GetAssetInfo(string path)
        {
            return !TryGetReadyPackage(out var package) ? null : package.GetAssetInfo(path);
        }

        /// <summary>
        /// 检查指定的资源路径是否有效。
        /// 注意：默认包未初始化/不存在/未就绪时返回 false，避免同步抛异常。
        /// IsLocationValid 依赖已初始化的资源清单，故用 TryGetReadyPackage 校验包初始化状态（EOperationStatus.Succeeded）。
        /// </summary>
        /// <param name="path">要检查的资源路径。</param>
        /// <returns>如果资源路径有效，则返回 true；否则返回 false。</returns>
        public bool HasAssetPath(string path)
        {
            return TryGetReadyPackage(out var package) && package.IsLocationValid(path);
        }

        #endregion
    }
}