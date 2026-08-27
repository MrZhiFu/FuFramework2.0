using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using YooAsset;
using Hotfix.Framework.Core;
using AOT.Framework.ModuleSetting.Runtime;
using AOT.Framework.Core.Log;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Asset
{
    /// <summary>
    /// 资源管理模块。
    /// 功能：
    ///     1. 封装了YooAsset的资源管理接口，提供更高级的UniTask异步资源加载相关接口。
    ///     2. 提供默认资源包的资源加载、卸载与查询能力。
    /// </summary>
    public partial class AssetModule : ModuleBase, ICancelAsync
    {
        /// <summary>
        /// 默认资源包名称
        /// </summary>
        private string DefaultPackageName { get; set; }

        /// <summary>
        /// 是否已销毁。防止销毁后在途的 InstantiateAsync 任务把已失效句柄写回引用字典。
        /// </summary>
        private bool m_IsDisposed;

        /// <summary>
        /// 取消范围：内部 CTS + 在途计数 + 全部完成信号。每次 OnInit 重建（新生命周期 = 新 Token）。
        /// OnDispose 时 Cancel，所有在途异步操作随之取消；框架 ReInit 前经 DrainCancelledAsync 等待排水。
        /// </summary>
        private CancellationScope m_Scope = new();

        /// <summary>
        /// 实例化资源引用管理，key:资源路径，value:句柄 + 引用计数。
        /// 实例化对象共享资源引用，调用方在实例销毁时通过 ReleaseInstantiate 释放。
        /// </summary>
        private readonly Dictionary<string, InstantiateRef> m_InstantiateRefDict = new();

        /// <summary>
        /// 实例化首次加载去重字典，key:资源路径，value:共享完成源。
        /// 同一路径并发首次实例化共享完成源（UniTaskCompletionSource.Task 可被多个调用方 await），
        /// 防止同一 pending 任务被二次 await 抛 "Already continuation registered"，
        /// 也保证同一路径仅加载一次、仅产生一个句柄。
        /// </summary>
        private readonly Dictionary<string, UniTaskCompletionSource<AssetHandle>> m_InstantiateLoadingTasks = new();

        /// <summary>
        /// 模块生命周期代数。OnDispose 递增，使旧生命周期在途的 InstantiateAsync
        /// 在 ReInit（OnInit 重置 m_IsDisposed）后仍能识别并拒绝把旧句柄写回新生命周期。
        /// </summary>
        private int m_LifecycleEpoch;

        /// <summary>
        /// 实例化引用：句柄 + 引用计数。
        /// 同一路径多个实例共享一个句柄，引用计数跟踪活跃实例数，
        /// 计数归零时释放句柄（见 AssetModule.ReleaseInstantiate），让资源可被卸载。
        /// </summary>
        private sealed class InstantiateRef
        {
            /// <summary>
            /// 资源句柄，持有 YooAsset 的资源引用。
            /// </summary>
            public AssetHandle Handle;

            /// <summary>
            /// 引用计数，即该路径当前活跃的实例化对象数。
            /// </summary>
            public int RefCount;
        }

        /// <summary>
        /// 实例化结果。携带实例对象、资源路径与创建时的模块生命周期代数。
        /// 实例销毁时调用 AssetModule.ReleaseInstantiate(result) 释放引用；
        /// 热更重载（OnDispose/ReInit）后旧代际结果会被代际校验识别并忽略，避免误释放新生命周期同路径引用。
        /// </summary>
        public sealed class InstantiateResult
        {
            /// <summary>
            /// 实例化出的 GameObject 对象。
            /// </summary>
            public GameObject Instance { get; internal set; }

            /// <summary>
            /// 实例来源的资源路径。
            /// </summary>
            public string Path { get; internal set; }

            /// <summary>
            /// 创建本结果时的模块生命周期代数。
            /// </summary>
            public int LifecycleEpoch { get; internal set; }
        }

        /// <summary>
        /// 取消令牌：模块销毁（OnDispose）后触发，在途操作观察它并中止。
        /// </summary>
        public CancellationToken Token => m_Scope.Token;

        /// <summary>
        /// 触发取消并等待在途操作完成清理（释放句柄 + 卸载资源）后才返回。供框架热更重载排水。
        /// </summary>
        public UniTask CancelAsync() => m_Scope.CancelAsync();

        /// <summary>
        /// 初始化
        /// </summary>
        protected internal override void OnInit()
        {
            // 热更重载场景下 OnDispose 后可能再次 OnInit（ModuleManager.ReInit），重置销毁标记
            m_IsDisposed = false;

            // 新生命周期 = 新 Token：旧 Token 已被 OnDispose 取消，在途旧任务据此识别中止
            m_Scope = new CancellationScope();

            // 默认包初始化由 AOT 启动流程 LaunchAssetHelper 完成，此处仅缓存默认包名
            DefaultPackageName = GameSetting.Instance.DefaultPackageName;

            FuLogger.LogInfo($"[AssetModule]资源系统运行模式：{GameSetting.Instance.PlayMode}");
            FuLogger.LogInfo("[AssetModule]资源系统初始化完毕！");
        }

        /// <summary>
        /// 释放
        /// </summary>
        protected internal override void OnDispose()
        {
            m_IsDisposed = true;
            m_LifecycleEpoch++;

            m_Scope.Cancel(); // 随模块销毁取消所有在途异步操作

            // 释放所有实例化句柄（否则实例化引用泄漏），并逐 path 显式卸载 bundle
            // （AutoUnloadBundleWhenUnused=false 下仅 Release 不会卸载；TryUnloadUnusedAsset 对仍被其他系统持有的共享 provider 安全跳过）。
            foreach (var kvp in m_InstantiateRefDict)
            {
                kvp.Value.Handle.Release();
                UnloadAsset(kvp.Key);
            }
            m_InstantiateRefDict.Clear();

            // 清理在途实例化加载任务（模块已销毁，任务完成回调会经 m_IsDisposed 检查自行释放句柄，不得再写回引用字典）。
            // 注意：此处不做整包 UnloadAllAssetsAsync——它是强制销毁全部 provider（含其他模块 Sound/Scene/Entity 仍持有的活句柄），
            // 且热更重载时 fire-and-forget 会误伤新生命周期刚创建的 provider。各模块应自行释放自己持有的句柄。
            m_InstantiateLoadingTasks.Clear();
        }

        /// <summary>
        /// 获取默认资源包；YooAssets 未初始化或默认包不存在时抛异常。
        /// 在 async 方法中调用时异常会被捕获为 faulted UniTask，保持"不同步抛"契约。
        /// </summary>
        private ResourcePackage GetReadyDefaultPackage()
        {
            if (!YooAssets.IsInitialized || !YooAssets.TryGetPackage(DefaultPackageName, out var package)
                                         || package.InitializeStatus != EOperationStatus.Succeeded)
                throw new InvalidOperationException($"[AssetModule]默认资源包未就绪：{DefaultPackageName}");
            return package;
        }

        /// <summary>
        /// 获取已成功初始化的默认包；未初始化/不存在返回 false（避免同步查询方法抛异常）。
        /// </summary>
        private bool TryGetReadyPackage(out ResourcePackage package)
        {
            package = null;
            if (!YooAssets.IsInitialized) return false; // YooAssets 未初始化（全局销毁后），防御不抛
            if (!YooAssets.TryGetPackage(DefaultPackageName, out package)) return false;
            return package.InitializeStatus == EOperationStatus.Succeeded;
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
                if (handle is not { Status: EOperationStatus.Succeeded })
                {
                    if (handle is { IsValid: true }) handle.Release();
                    sharedSource.TrySetException(new InvalidOperationException($"[AssetModule]资源{path}加载失败"));
                    return;
                }

                if (m_IsDisposed || lifecycleEpoch != m_LifecycleEpoch)
                {
                    if (handle.IsValid)
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
    }
}