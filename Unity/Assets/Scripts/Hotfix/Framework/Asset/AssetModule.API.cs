using System;
using Cysharp.Threading.Tasks;
using YooAsset;
using Hotfix.Framework.Core;
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
        /// 异步加载资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        public UniTask<AssetHandle> LoadAssetAsync(string path)
            => CreateHandleTask(package => package.LoadAssetAsync(path), (handle, tcs) => { handle.Completed += completedHandle => tcs.TrySetResult(completedHandle); });

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <typeparam name="T">资源类型</typeparam>
        /// <returns></returns>
        public UniTask<AssetHandle> LoadAssetAsync<T>(string path) where T : Object
            => CreateHandleTask(package => package.LoadAssetAsync<T>(path), (handle, tcs) => { handle.Completed += completedHandle => tcs.TrySetResult(completedHandle); });

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="type">资源类型</param>
        /// <returns></returns>
        public UniTask<AssetHandle> LoadAssetAsync(string path, Type type)
            => CreateHandleTask(package => package.LoadAssetAsync(path, type), (handle, tcs) => { handle.Completed += completedHandle => tcs.TrySetResult(completedHandle); });

        #endregion

        #region 异步加载场景

        /// <summary>
        /// 异步加载场景。
        /// 注意：activateOnLoad=false（预加载后手动激活）时，Provider 在手动激活前不会完成，
        /// 此 UniTask 将一直挂起——当前包装仅支持自动激活（默认 true）的场景加载。
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="sceneMode">场景模式</param>
        /// <param name="activateOnLoad">是否加载完成自动激活</param>
        /// <returns></returns>
        public UniTask<SceneHandle> LoadSceneAsync(string path, LoadSceneMode sceneMode, bool activateOnLoad = true)
            => CreateHandleTask(package => package.LoadSceneAsync(path, sceneMode, LocalPhysicsMode.None, activateOnLoad), (handle, tcs) => { handle.Completed += completedHandle => tcs.TrySetResult(completedHandle); });

        #endregion

        #region 异步实例化游戏物体

        /// <summary>
        /// 异步实例化实体。
        /// 句柄按路径缓存并引用计数：同一 prefab 多实例共享句柄，实例销毁时调用 ReleaseInstantiate 释放。
        /// 注意：同步/异步首次实例化请勿混用同一路径（首次加载去重仅覆盖异步路径）。
        /// <param name="path">资源路径</param>
        /// </summary>
        /// <returns>实例化后的实体。</returns>
        public async UniTask<GameObject> InstantiateAsync(string path)
        {
            if (m_IsDisposed) throw new ObjectDisposedException(nameof(AssetModule));

            AssetHandle assetHandle;
            if (m_InstantiateRefDict.TryGetValue(path, out var entry))
            {
                entry.RefCount++;
                assetHandle = entry.Handle;
            }
            else
            {
                // 并发首次加载去重：同路径共享加载任务（防覆盖句柄泄漏）
                if (!m_InstantiateLoadingTasks.TryGetValue(path, out var loadingTask))
                {
                    loadingTask                     = LoadAssetAsync(path);
                    m_InstantiateLoadingTasks[path] = loadingTask;
                }

                try
                {
                    assetHandle = await loadingTask;
                }
                finally
                {
                    m_InstantiateLoadingTasks.Remove(path);
                }

                // 模块销毁后：句柄可能已被 UnloadAllAssets 释放，不得再写回引用字典
                if (m_IsDisposed)
                {
                    if (assetHandle.IsValid) assetHandle.Release();
                    throw new ObjectDisposedException(nameof(AssetModule));
                }

                // 加载完成后写入引用；若期间被并发请求写入则复用（同一任务返回同一句柄，无泄漏）
                if (m_InstantiateRefDict.TryGetValue(path, out var existing))
                {
                    existing.RefCount++;
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
                if (instantiateOperation.Result == null)
                    throw new InvalidOperationException($"[AssetModule]实例化资源{path}失败");
                return instantiateOperation.Result;
            }
            catch
            {
                ReleaseInstantiate(path); // 失败回滚引用
                throw;
            }
        }


        /// <summary>
        /// 释放实例化资源的句柄引用。实例对象销毁后调用。
        /// 引用计数归零时 Release 句柄并移除，让资源可被卸载。
        /// </summary>
        /// <param name="path">资源路径。</param>
        public void ReleaseInstantiate(string path)
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
        /// 注意：如果该资源还在被使用，该方法会无效
        /// </summary>
        /// <param name="assetPath">资源路径</param>
        public void UnloadAsset(string assetPath)
        {
            assetPath.NotNull(nameof(assetPath));
            if (!YooAssets.IsInitialized) return;                                      // YooAssets 未初始化（全局销毁后），防御不抛
            if (!YooAssets.TryGetPackage(DefaultPackageName, out var package)) return; // 包不存在/已销毁时不抛异常
            package.TryUnloadUnusedAsset(assetPath);
        }

        #endregion

        #region Get

        /// <summary>
        /// 获取资源信息。
        /// 注意：默认包未就绪时返回 null（调用方已判空），避免同步抛异常。
        /// </summary>
        public AssetInfo GetAssetInfo(string path)
        {
            // 默认包未就绪返回 null（调用方已判空），避免同步抛异常
            if (!YooAssets.IsInitialized || !YooAssets.TryGetPackage(DefaultPackageName, out var package)) return null;
            return package.GetAssetInfo(path);
        }

        /// <summary>
        /// 检查指定的资源路径是否有效。
        /// 注意：默认包未初始化/不存在时返回 false，避免同步抛异常。
        /// 与 GetAssetInfo/CreateHandleTask 仅判包是否存在不同，IsLocationValid 依赖已初始化的资源清单，
        /// 故此处用 TryGetReadyPackage 额外校验包初始化状态（EOperationStatus.Succeeded）。
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
