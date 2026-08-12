using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using YooAsset;
using Hotfix.Framework.Core;
using AOT.Framework.ModuleSetting.Runtime;
using AOT.Framework.Core.Log;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Asset
{
    /// <summary>
    /// 资源管理模块。
    /// 功能：
    ///     1. 封装了YooAsset的资源管理接口，提供更高级的UniTask异步资源加载相关接口。
    ///     2. 提供默认资源包的资源加载、卸载与查询能力。
    /// </summary>
    public partial class AssetModule : ModuleBase
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
        /// 模块生命周期代际。OnDispose 递增，使旧生命周期在途的 InstantiateAsync
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
        /// 初始化
        /// </summary>
        protected internal override void OnInit()
        {
            // 热更重载场景下 OnDispose 后可能再次 OnInit（ModuleManager.ReInit），重置销毁标记
            m_IsDisposed = false;

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

            // 释放所有实例化句柄（否则实例化引用泄漏）
            foreach (var entry in m_InstantiateRefDict.Values)
                entry.Handle.Release();
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
    }
}