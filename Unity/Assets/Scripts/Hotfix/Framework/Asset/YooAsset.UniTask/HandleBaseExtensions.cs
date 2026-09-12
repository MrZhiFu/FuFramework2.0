// 来源：YooAsset 3.0.5 Samples~/UniTask Sample/UniTask/Runtime/External/YooAsset/HandleBaseExtensions.cs
// 本工程改造（相对上游的差异，升级时需逐项比对，不可整份覆盖）：
//   1. 剥离 #if YOOASSET_UNITASK_SUPPORT 守卫 —— 本工程刻意启用该集成；
//   2. 移除 GetAwaiter / WithCancellation —— 前者被 YooAsset 的 HandleBase.GetAwaiter() 实例方法遮蔽
//      （C# 中实例方法优先于扩展方法，该扩展永不会被编译器选中），后者全工程无调用点，二者均为死代码；
//   3. 移除 HandleBaseConfiguredSource.cancelImmediately 字段及其赋值 —— 上游该字段写而不读，属死代码。
//      注：UniTask 原版会在 GetResult 中读它，在「cancelImmediately 且令牌已取消」路径上跳过「重置状态 +
//      注销取消注册 + 归还对象池」三步（TaskTracker.RemoveTracking 两条路径都会做，非差异点），实例交由 GC
//      回收；YooAsset 复制时把 GetResult 简化为无条件 TryReturn，该读取随之丢失。本工程只删死字段
//      （行为与上游一致），未恢复 UniTask 的这条收尾跳过；
//   4. 补充中文 XML 文档注释。
// 其余逻辑与上游一致；标识符沿用上游命名，便于与上游比对同步。

using System;
using System.Threading;
using YooAsset;

// ReSharper disable once CheckNamespace
namespace Cysharp.Threading.Tasks
{
    /// <summary>
    /// YooAsset 资源句柄（<see cref="HandleBase"/>）的 UniTask 适配扩展。
    /// 仅补充 YooAsset 原生 OperationAwaiter 缺失的能力：带取消令牌的等待。
    /// 本扩展是 Asset 模块「取消令牌必传 + 与模块令牌 linked 竞速」取消语义的实现基础，
    /// 详见 Asset/README.md 的「取消语义边界」。
    /// </summary>
    public static class HandleBaseExtensions
    {
        /// <summary>
        /// 将资源句柄的加载过程转换为可等待的 UniTask，支持进度上报与取消。
        /// </summary>
        /// <param name="handle">资源句柄（AssetHandle / SceneHandle 等 HandleBase 派生类型）。</param>
        /// <param name="progress">加载进度回调（0~1），由 PlayerLoop 每帧上报一次；不需要进度时传 null。</param>
        /// <param name="timing">进度上报与完成检测所挂载的 PlayerLoop 时机。</param>
        /// <param name="cancellationToken">调用方生命周期取消令牌；令牌已取消时立即返回已取消任务。</param>
        /// <param name="cancelImmediately">为 true 时令牌取消会立即完成 await 并抛 OperationCanceledException，不等 YooAsset 加载结束。</param>
        /// <returns>句柄加载完成或被取消后可等待的任务；句柄已失效或已完成时返回已完成任务。</returns>
        public static UniTask ToUniTask(this HandleBase handle, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken cancellationToken = default,
                                        bool cancelImmediately = false)
        {
            if (cancellationToken.IsCancellationRequested)
                return UniTask.FromCanceled(cancellationToken);

            if (!handle.IsValid || handle.IsDone)
                return UniTask.CompletedTask;

            return new UniTask(HandleBaseConfiguredSource.Create(handle, timing, progress, cancellationToken, cancelImmediately, out var token), token);
        }

        /// <summary>
        /// 句柄等待所用的 UniTask 源。
        /// 经 TaskPool 池化复用，由 PlayerLoop 逐帧驱动（轮询句柄完成状态并上报进度），
        /// 在完成、取消或句柄失效时结束并归还池。
        /// </summary>
        private sealed class HandleBaseConfiguredSource : IUniTaskSource, IPlayerLoopItem, ITaskPoolNode<HandleBaseConfiguredSource>
        {
            /// <summary>
            /// 本类型实例的对象池。
            /// </summary>
            static TaskPool<HandleBaseConfiguredSource> pool;

            /// <summary>
            /// 对象池侵入式单链表的下一节点。
            /// </summary>
            HandleBaseConfiguredSource nextNode;

            /// <summary>
            /// 对象池侵入式单链表的下一节点（ITaskPoolNode 契约）。
            /// </summary>
            public ref HandleBaseConfiguredSource NextNode => ref nextNode;

            /// <summary>
            /// 向 UniTask 诊断面板注册本类型对象池的容量查询器。
            /// </summary>
            static HandleBaseConfiguredSource()
            {
                TaskPool.RegisterSizeGetter(typeof(HandleBaseConfiguredSource), () => pool.Size);
            }

            /// <summary>
            /// 被等待的资源句柄。
            /// </summary>
            HandleBase handle;

            /// <summary>
            /// 调用方生命周期取消令牌。
            /// </summary>
            CancellationToken cancellationToken;

            /// <summary>
            /// 令牌取消回调的注册句柄，归还池时释放。
            /// </summary>
            CancellationTokenRegistration cancellationTokenRegistration;

            /// <summary>
            /// 加载进度回调；为 null 表示不关心进度。
            /// </summary>
            IProgress<float> progress;

            /// <summary>
            /// 本等待是否已结束（完成或取消），防止重复置位结果。
            /// </summary>
            bool completed;

            /// <summary>
            /// UniTask 源的核心状态与结果容器。
            /// </summary>
            UniTaskCompletionSourceCore<AsyncUnit> core;

            /// <summary>
            /// 由对象池创建实例；各字段在 <see cref="Create"/> 中完成赋值，构造时不持有任何状态。
            /// </summary>
            HandleBaseConfiguredSource() { }

            /// <summary>
            /// 从对象池取出或新建一个等待源并完成初始化。
            /// </summary>
            /// <param name="handle">被等待的资源句柄。</param>
            /// <param name="timing">进度上报与完成检测所挂载的 PlayerLoop 时机。</param>
            /// <param name="progress">加载进度回调；可为 null。</param>
            /// <param name="cancellationToken">调用方生命周期取消令牌。</param>
            /// <param name="cancelImmediately">是否在令牌取消时立即完成 await。</param>
            /// <param name="token">输出：本等待源的核心版本号，用于后续状态校验。</param>
            /// <returns>可等待的 UniTask 源；令牌已取消时返回一个已取消的源。</returns>
            public static IUniTaskSource Create(HandleBase handle, PlayerLoopTiming timing, IProgress<float> progress, CancellationToken cancellationToken, bool cancelImmediately, out short token)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return AutoResetUniTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
                }

                if (!pool.TryPop(out var result))
                {
                    result = new HandleBaseConfiguredSource();
                }

                result.handle            = handle;
                result.progress          = progress;
                result.cancellationToken = cancellationToken;
                result.completed         = false;

                if (cancelImmediately && cancellationToken.CanBeCanceled)
                {
                    result.cancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(state =>
                    {
                        var source = (HandleBaseConfiguredSource)state;
                        source.core.TrySetCanceled(source.cancellationToken);
                    }, result);
                }

                TaskTracker.TrackActiveTask(result, 3);
                PlayerLoopHelper.AddAction(timing, result);

                // 注意：统一用强类型回调订阅 Handle.Completed，修复 IL2CPP 逆变委托崩溃
                switch (handle)
                {
                    case AssetHandle asset_handle:
                        asset_handle.Completed += result.AssetContinuation;
                        break;
                    case SceneHandle scene_handle:
                        scene_handle.Completed += result.SceneContinuation;
                        break;
                    case SubAssetsHandle sub_asset_handle:
                        sub_asset_handle.Completed += result.SubContinuation;
                        break;
                    case BundleFileHandle bundle_file_handle:
                        bundle_file_handle.Completed += result.BundleFileContinuation;
                        break;
                    case AllAssetsHandle all_assets_handle:
                        all_assets_handle.Completed += result.AllAssetsContinuation;
                        break;
                }

                token = result.core.Version;
                return result;
            }

            /// <summary>
            /// AssetHandle 完成回调（强类型，规避 IL2CPP 逆变委托崩溃）。
            /// </summary>
            private void AssetContinuation(AssetHandle _) => HandleCompleted(null);

            /// <summary>
            /// SceneHandle 完成回调（强类型，规避 IL2CPP 逆变委托崩溃）。
            /// </summary>
            private void SceneContinuation(SceneHandle _) => HandleCompleted(null);

            /// <summary>
            /// SubAssetsHandle 完成回调（强类型，规避 IL2CPP 逆变委托崩溃）。
            /// </summary>
            private void SubContinuation(SubAssetsHandle _) => HandleCompleted(null);

            /// <summary>
            /// BundleFileHandle 完成回调（强类型，规避 IL2CPP 逆变委托崩溃）。
            /// </summary>
            private void BundleFileContinuation(BundleFileHandle _) => HandleCompleted(null);

            /// <summary>
            /// AllAssetsHandle 完成回调（强类型，规避 IL2CPP 逆变委托崩溃）。
            /// </summary>
            private void AllAssetsContinuation(AllAssetsHandle _) => HandleCompleted(null);

            /// <summary>
            /// 句柄完成时的统一收尾：先退订回调，再按是否已取消置位等待结果。
            /// </summary>
            /// <param name="_">未使用的完成回调参数。</param>
            private void HandleCompleted(HandleBase _)
            {
                RemoveCompleted();

                if (completed) return;

                completed = true;
                if (cancellationToken.IsCancellationRequested)
                {
                    core.TrySetCanceled(cancellationToken);
                }
                else
                {
                    core.TrySetResult(AsyncUnit.Default);
                }
            }

            /// <summary>
            /// 退订当前句柄上的强类型完成回调；句柄已失效时直接返回（此时无从退订）。
            /// </summary>
            private void RemoveCompleted()
            {
                if (handle == null || !handle.IsValid) return;

                switch (handle)
                {
                    case AssetHandle asset_handle:
                        asset_handle.Completed -= AssetContinuation;
                        break;
                    case SceneHandle scene_handle:
                        scene_handle.Completed -= SceneContinuation;
                        break;
                    case SubAssetsHandle sub_asset_handle:
                        sub_asset_handle.Completed -= SubContinuation;
                        break;
                    case BundleFileHandle bundle_file_handle:
                        bundle_file_handle.Completed -= BundleFileContinuation;
                        break;
                    case AllAssetsHandle all_assets_handle:
                        all_assets_handle.Completed -= AllAssetsContinuation;
                        break;
                }
            }

            /// <summary>
            /// 取回等待结果并归还对象池。
            /// </summary>
            /// <param name="token">等待源的核心版本号。</param>
            public void GetResult(short token)
            {
                try
                {
                    core.GetResult(token);
                }
                finally
                {
                    TryReturn();
                }
            }

            /// <summary>
            /// 获取当前等待状态。
            /// </summary>
            /// <param name="token">等待源的核心版本号。</param>
            /// <returns>当前等待状态。</returns>
            public UniTaskStatus GetStatus(short token) => core.GetStatus(token);

            /// <summary>
            /// 不校验版本号地获取当前等待状态（仅供 UniTask 内部快速判定）。
            /// </summary>
            /// <returns>当前等待状态。</returns>
            public UniTaskStatus UnsafeGetStatus() => core.UnsafeGetStatus();

            /// <summary>
            /// 注册 await 的后续回调。
            /// </summary>
            /// <param name="continuation">后续执行的回调。</param>
            /// <param name="state">回调状态对象。</param>
            /// <param name="token">等待源的核心版本号。</param>
            public void OnCompleted(Action<object> continuation, object state, short token) => core.OnCompleted(continuation, state, token);

            /// <summary>
            /// PlayerLoop 每帧驱动：检测取消与句柄完成状态，并上报加载进度。
            /// </summary>
            /// <returns>仍需继续驱动返回 true；本帧已结束等待返回 false。</returns>
            public bool MoveNext()
            {
                if (completed) return false;

                if (cancellationToken.IsCancellationRequested)
                {
                    completed = true;
                    core.TrySetCanceled(cancellationToken);
                    return false;
                }

                if (handle == null || !handle.IsValid || handle.IsDone)
                {
                    completed = true;
                    core.TrySetResult(AsyncUnit.Default);
                    return false;
                }

                if (progress != null && handle.IsValid)
                {
                    progress.Report(handle.Progress);
                }

                return true;
            }

            /// <summary>
            /// 清理引用（解除追踪、重置状态、释放取消注册）并归还对象池。
            /// </summary>
            /// <returns>归还成功返回 true。</returns>
            private bool TryReturn()
            {
                TaskTracker.RemoveTracking(this);
                core.Reset();
                handle            = default;
                progress          = default;
                cancellationToken = default;
                cancellationTokenRegistration.Dispose();
                return pool.TryPush(this);
            }
        }
    }
}