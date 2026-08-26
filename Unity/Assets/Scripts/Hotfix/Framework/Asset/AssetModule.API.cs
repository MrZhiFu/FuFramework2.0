using System;
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
        /// <returns>资源句柄，使用完毕必须调用 Release()。</returns>
        public async UniTask<AssetHandle> LoadAssetAsync(string path)
        {
            m_Scope.Token.ThrowIfCancellationRequested(); // 入口：取消后拒绝新操作，防新在途使排水计数失效
            using (m_Scope.Begin()) // 登记在途：OnDispose 排水时等待本操作清理完毕
            {
                var package = GetReadyDefaultPackage();
                var handle  = package.LoadAssetAsync(path);
                try
                {
                    // YooAsset 官方 UniTask 集成：cancelImmediately=true 时 Token 取消立即完成 await 并抛 OperationCanceledException
                    await handle.ToUniTask(cancellationToken: m_Scope.Token, cancelImmediately: true);
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

        /// <summary>
        /// 异步加载资源。
        /// 注意1：YooAsset 的 await 不抛异常——资源加载失败时本方法仍返回句柄，其 Status 为 Failed、AssetObject 为 null，
        /// 调用方必须先检查 handle.Status == EOperationStatus.Succeeded 再使用资源。
        /// 注意2：返回的句柄无论成败均须调用 Release()（失败不释放则 provider 引用计数不归零、资源永不卸载）。
        /// 注意3：模块销毁（OnDispose）时本方法会抛 OperationCanceledException（句柄已自动释放并卸载），调用方可按需捕获。
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <typeparam name="T">资源类型</typeparam>
        /// <returns>资源句柄，使用完毕必须调用 Release()。</returns>
        public async UniTask<AssetHandle> LoadAssetAsync<T>(string path) where T : Object
        {
            m_Scope.Token.ThrowIfCancellationRequested(); // 入口：取消后拒绝新操作，防新在途使排水计数失效
            using (m_Scope.Begin()) // 登记在途：OnDispose 排水时等待本操作清理完毕
            {
                var package = GetReadyDefaultPackage();
                var handle  = package.LoadAssetAsync<T>(path);
                try
                {
                    // YooAsset 官方 UniTask 集成：cancelImmediately=true 时 Token 取消立即完成 await 并抛 OperationCanceledException
                    await handle.ToUniTask(cancellationToken: m_Scope.Token, cancelImmediately: true);
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

        /// <summary>
        /// 异步加载资源。
        /// 注意1：YooAsset 的 await 不抛异常——资源加载失败时本方法仍返回句柄，其 Status 为 Failed、AssetObject 为 null，
        /// 调用方必须先检查 handle.Status == EOperationStatus.Succeeded 再使用资源。
        /// 注意2：返回的句柄无论成败均须调用 Release()（失败不释放则 provider 引用计数不归零、资源永不卸载）。
        /// 注意3：模块销毁（OnDispose）时本方法会抛 OperationCanceledException（句柄已自动释放并卸载），调用方可按需捕获。
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="type">资源类型</param>
        /// <returns>资源句柄，使用完毕必须调用 Release()。</returns>
        public async UniTask<AssetHandle> LoadAssetAsync(string path, Type type)
        {
            m_Scope.Token.ThrowIfCancellationRequested(); // 入口：取消后拒绝新操作，防新在途使排水计数失效
            using (m_Scope.Begin()) // 登记在途：OnDispose 排水时等待本操作清理完毕
            {
                var package = GetReadyDefaultPackage();
                var handle  = package.LoadAssetAsync(path, type);
                try
                {
                    // YooAsset 官方 UniTask 集成：cancelImmediately=true 时 Token 取消立即完成 await 并抛 OperationCanceledException
                    await handle.ToUniTask(cancellationToken: m_Scope.Token, cancelImmediately: true);
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

        #endregion

        #region 异步加载场景

        /// <summary>
        /// 异步加载场景（加载完成自动激活）。
        /// LocalPhysicsMode 固定为 None，未开放自定义。
        /// onProgress：加载进度回调（0~1），每帧上报一次直至加载完成；不需要进度时传 null。
        /// 加载失败时内部释放句柄并抛异常，调用方无需也无法释放失败句柄。
        /// 返回的 SceneHandle 使用完毕后必须调用 Release()，否则资源永不卸载。
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="sceneMode">场景模式</param>
        /// <param name="onProgress">加载进度回调（可选）。</param>
        /// <returns>场景句柄，使用完毕须调用 Release。</returns>
        public async UniTask<SceneHandle> LoadSceneAsync(string path, LoadSceneMode sceneMode, Action<float> onProgress = null)
        {
            m_Scope.Token.ThrowIfCancellationRequested();
            using (m_Scope.Begin())
            {
                var package = GetReadyDefaultPackage();
                var handle  = package.LoadSceneAsync(path, sceneMode, LocalPhysicsMode.None, true);
                try
                {
                    // 可选进度上报：每帧轮询 handle.Progress 直到加载完成；同时观察取消令牌
                    if (onProgress != null)
                    {
                        while (!handle.IsDone)
                        {
                            m_Scope.Token.ThrowIfCancellationRequested();
                            TryReportProgress(onProgress, handle.Progress);
                            await UniTask.Yield();
                        }
                        TryReportProgress(onProgress, handle.Progress);
                    }

                    await handle.ToUniTask(cancellationToken: m_Scope.Token, cancelImmediately: true);
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

        /// <summary>
        /// 上报场景加载进度；回调异常记录日志但不抛出（防止回调异常导致句柄无法返回而泄漏）。
        /// </summary>
        private static void TryReportProgress(Action<float> onProgress, float progress)
        {
            try
            {
                onProgress(progress);
            }
            catch (Exception e)
            {
                FuLogger.LogError($"[AssetModule]onProgress 回调异常：{e.Message}");
            }
        }

        #endregion

        #region 异步实例化游戏物体

        /// <summary>
        /// 异步实例化实体。
        /// 句柄按路径缓存并引用计数：同一 prefab 多实例共享句柄，实例销毁时调用 ReleaseInstantiate 释放。
        /// 返回 InstantiateResult（携带实例与创建时生命周期代数）：热更重载（OnDispose/ReInit）后
        /// 旧生命周期存活的实例调用 ReleaseInstantiate 会被代际校验识别并忽略，杜绝误释放新生命周期同路径引用。
        /// 注意：同步/异步首次实例化请勿混用同一路径（首次加载去重仅覆盖异步路径）。
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns>实例化结果，实例销毁时调用 ReleaseInstantiate(result) 释放引用。</returns>
        public async UniTask<InstantiateResult> InstantiateAsync(string path)
        {
            if (m_IsDisposed) throw new ObjectDisposedException(nameof(AssetModule));
            var lifecycleEpoch = m_LifecycleEpoch;

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
                    sharedSource = new UniTaskCompletionSource<AssetHandle>();
                    m_InstantiateLoadingTasks[path] = sharedSource;
                    LoadAsyncForInstantiate(path, sharedSource, lifecycleEpoch).Forget();
                }

                assetHandle = await sharedSource.Task;

                // 模块销毁/生命周期变更后：句柄可能已被释放，不得再写回引用字典
                if (m_IsDisposed || lifecycleEpoch != m_LifecycleEpoch)
                {
                    if (assetHandle != null && assetHandle.IsValid)
                    {
                        assetHandle.Release();
                        // 跨生命周期中止：加载成功的句柄仅 Release 在 AutoUnloadBundleWhenUnused=false 下不卸载 bundle，
                        // 配对 UnloadAsset 防该 prefab 的 bundle 常驻（新生命周期不再加载同路径时永不释放）
                        UnloadAsset(path);
                    }
                    throw new ObjectDisposedException(nameof(AssetModule));
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
                // 模块销毁/生命周期变更后：句柄已随 OnDispose 释放，不返回孤儿实例；销毁已实例化的对象，抛 ObjectDisposedException（catch 中 ReleaseInstantiate 兜底回滚）
                if (m_IsDisposed || lifecycleEpoch != m_LifecycleEpoch)
                {
                    if (instantiateOperation.Result != null)
                        Object.Destroy(instantiateOperation.Result);
                    throw new ObjectDisposedException(nameof(AssetModule));
                }
                if (instantiateOperation.Result == null)
                    throw new InvalidOperationException($"[AssetModule]实例化资源{path}失败");

                // 携带当前生命周期代数返回：销毁时经 ReleaseInstantiate(result) 校验代际，热更重载后旧实例释放被识别并忽略
                return new InstantiateResult
                {
                    Instance = instantiateOperation.Result,
                    Path = path,
                    LifecycleEpoch = lifecycleEpoch,
                };
            }
            catch
            {
                // 生命周期已变更（热更重载）时不得回滚引用：引用字典已被 OnDispose 清空，
                // 且可能已存在新生命周期同路径条目，误回滚会卸载新生命周期存活的实例资源；仅在本代际时回滚本次占用
                if (lifecycleEpoch == m_LifecycleEpoch)
                    ReleaseInstantiateInternal(path);
                throw;
            }
        }

        /// <summary>
        /// 首次实例化共享加载：同一路径多个并发调用方 await 同一完成源，共享单个句柄。
        /// 加载完成或失败后向完成源写入结果并移除去重项；模块销毁/生命周期变更时释放句柄并向等待方抛异常。
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="sharedSource">共享完成源</param>
        /// <param name="lifecycleEpoch">发起时的模块生命周期代际</param>
        private async UniTaskVoid LoadAsyncForInstantiate(string path, UniTaskCompletionSource<AssetHandle> sharedSource, int lifecycleEpoch)
        {
            AssetHandle handle = null;
            try
            {
                handle = await LoadAssetAsync(path);
                // YooAsset await 不抛异常：资源加载失败时返回的是 Failed 句柄，必须显式校验 Status 并释放，
                // 否则 Failed 句柄流入引用字典（依赖下游 InstantiateAsync 失败才兜底释放，绕路且隐蔽）。
                if (handle == null || handle.Status != EOperationStatus.Succeeded)
                {
                    if (handle != null && handle.IsValid) handle.Release();
                    sharedSource.TrySetException(new InvalidOperationException($"[AssetModule]资源{path}加载失败"));
                    return;
                }

                if (m_IsDisposed || lifecycleEpoch != m_LifecycleEpoch)
                {
                    if (handle != null && handle.IsValid)
                    {
                        handle.Release();
                        // 跨生命周期中止：加载成功的句柄仅 Release 在 AutoUnloadBundleWhenUnused=false 下不卸载 bundle，配对卸载防残留
                        UnloadAsset(path);
                    }
                    sharedSource.TrySetException(new ObjectDisposedException(nameof(AssetModule)));
                    return;
                }

                sharedSource.TrySetResult(handle);
            }
            catch (Exception e)
            {
                if (handle != null && handle.IsValid) handle.Release();
                sharedSource.TrySetException(e);
            }
            finally
            {
                // 跨生命周期防护：仅当共享源仍是本任务注册的条目时才移除，防止旧生命周期在途任务的
                // finally 误删新生命周期（ReInit 后）同路径刚注册的去重项，导致后续并发请求重复发起加载
                if (m_InstantiateLoadingTasks.TryGetValue(path, out var current) && ReferenceEquals(current, sharedSource))
                    m_InstantiateLoadingTasks.Remove(path);
            }
        }

        /// <summary>
        /// 释放实例化资源的句柄引用。实例对象销毁后调用，传入 InstantiateAsync 返回的 InstantiateResult。
        /// 引用计数归零时 Release 句柄并移除，让资源可被卸载。
        /// 热更重载（OnDispose/ReInit）后旧生命周期存活的实例携带旧代际结果调用本方法时会被代际校验识别并忽略，
        /// 杜绝误命中新生命周期同路径条目、导致仍存活的实例资源被静默卸载。
        /// </summary>
        /// <param name="result">InstantiateAsync 返回的实例化结果。</param>
        public void ReleaseInstantiate(InstantiateResult result)
        {
            if (m_IsDisposed) return; // 模块已销毁：引用字典已清空，直接忽略（含 null 参数，teardown 防御不抛）
            if (result == null) throw new ArgumentNullException(nameof(result)); // 存活模块传入 null 属调用方 bug，快速失败
            if (result.LifecycleEpoch != m_LifecycleEpoch)
            {
                // 跨生命周期释放：旧代际实例在热更重载后误调用，直接忽略防误卸载新生命周期同路径引用；
                // 属调用方违反"重载后不得对旧实例释放"契约，告警便于定位
                FuLogger.LogWarning($"[AssetModule]忽略跨生命周期实例化释放：{result.Path}（结果代际 {result.LifecycleEpoch}，当前代际 {m_LifecycleEpoch}）。热更重载后请勿对旧生命周期实例调用 ReleaseInstantiate。");
                return;
            }

            ReleaseInstantiateInternal(result.Path);
        }

        /// <summary>
        /// 按路径释放实例化引用（引用计数归零时释放句柄并移除，让资源可被卸载）。
        /// 供本模块内部（ReleaseInstantiate / 实例化失败回滚）使用，不校验生命周期代际。
        /// </summary>
        /// <param name="path">资源路径。</param>
        private void ReleaseInstantiateInternal(string path)
        {
            if (!m_InstantiateRefDict.TryGetValue(path, out var entry)) return;
            if (entry.RefCount   <= 0) return;
            if (--entry.RefCount > 0) return;

            entry.Handle.Release();
            m_InstantiateRefDict.Remove(path);

            // 引用归零后显式卸载：句柄 Release 在 AutoUnloadBundleWhenUnused=false 下不会卸载 bundle，
            // 需 UnloadAsset 才能真正释放，否则该 prefab 的 bundle 永久残留（内存只增不减）。
            // 若其他系统仍持有同一资源句柄，TryUnloadUnusedAsset 会因引用计数 >0 而跳过，共享安全。
            UnloadAsset(path);
        }

        #endregion

        #region 卸载资源

        /// <summary>
        /// 卸载指定资源。
        /// 注意：如果该资源还在被使用，该方法会无效。
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

        #region Get

        /// <summary>
        /// 获取资源信息。
        /// 注意：默认包未就绪（含初始化未完成/失败）时返回 null，避免同步抛异常。
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns>资源信息，默认包未就绪时返回 null。</returns>
        public AssetInfo GetAssetInfo(string path)
        {
            // 默认包未就绪返回 null（调用方已判空），避免同步抛异常
            if (!TryGetReadyPackage(out var package)) return null;
            return package.GetAssetInfo(path);
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
            if (!TryGetReadyPackage(out var package)) return false;
            return package.IsLocationValid(path);
        }

        #endregion
    }
}
