using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using YooAsset;
using Hotfix.Framework.Core;
using AOT.Framework.ModuleSetting.Runtime;
using AOT.Framework.Core.Log;

// ReSharper disable once CheckNamespace
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Hotfix.Framework.Asset
{
    /// <summary>
    /// 资源管理模块。
    /// 功能：
    ///     1. 封装了YooAsset的资源管理接口，提供更高级的UniTask异步资源加载相关接口。
    ///     2. 统一从资源配置(AssetSetting.scriptableObject)中读取相关参数配置，传入YooAsset，方便管理。
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
        /// 实例化首次加载去重字典，key:资源路径，value:加载任务。
        /// 同一路径并发首次实例化共享任务，防止覆盖句柄泄漏。
        /// 注意：此字典存的是 LoadAssetAsync 返回的 UniTaskCompletionSource.Task（可多次 await），
        /// 切勿改成 async 方法返回值（async UniTask 只能 await 一次）。
        /// </summary>
        private readonly Dictionary<string, UniTask<AssetHandle>> m_InstantiateLoadingTasks = new();

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

            // 释放所有实例化句柄（否则实例化引用泄漏）
            foreach (var entry in m_InstantiateRefDict.Values)
                entry.Handle.Release();
            m_InstantiateRefDict.Clear();

            // 清理在途实例化加载任务（其句柄已随 UnloadAllAssets 释放，任务完成回调不得再写回引用字典）
            m_InstantiateLoadingTasks.Clear();

            UnloadAllAssetsAsync(DefaultPackageName).Forget();
        }

        /// <summary>
        /// 将 YooAsset 异步句柄包装为 UniTask 的通用逻辑。
        /// 同步异常（YooAssets 未初始化、包不存在等）统一转为 faulted UniTask，
        /// 避免包装方法同步抛异常（对非 async 调用方如 `.Status` 检查会直接崩溃）。
        /// </summary>
        /// <typeparam name="T">句柄类型。</typeparam>
        /// <param name="load">发起加载并返回句柄。</param>
        /// <param name="bind">将句柄的 Completed 事件绑定到完成源。</param>
        private static UniTask<T> CreateHandleTask<T>(Func<T> load, Action<T, UniTaskCompletionSource<T>> bind) where T : HandleBase
        {
            var taskCompletionSource = new UniTaskCompletionSource<T>();
            T   handle               = null;
            try
            {
                handle = load();
                bind(handle, taskCompletionSource);
            }
            catch (Exception e)
            {
                // bind 失败（如句柄加载后立即失效）时释放已创建的句柄，避免残留
                handle?.Release();
                taskCompletionSource.TrySetException(e);
            }

            return taskCompletionSource.Task;
        }

        /// <summary>
        /// 获取默认资源包
        /// </summary>
        /// <returns></returns>
        private ResourcePackage GetDefaultPackage() => YooAssets.GetPackage(DefaultPackageName);

        /// <summary>
        /// 强制卸载所有资源。
        /// 注意：该方法请在合适的时机调用。Package在销毁的时候也会自动调用该方法。
        /// 警告：此操作会释放所有已加载句柄；进行中的 LoadAssetAsync 句柄被 Release 后
        /// Completed 回调不再触发，其 UniTask 将永久挂起，请确保调用时无进行中的加载。
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        private async UniTaskVoid UnloadAllAssetsAsync(string packageName)
        {
            packageName.NotNull(nameof(packageName));
            if (!YooAssets.IsInitialized) return;
            if (!YooAssets.TryGetPackage(packageName, out var package)) return;
            await package.UnloadAllAssetsAsync();
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
