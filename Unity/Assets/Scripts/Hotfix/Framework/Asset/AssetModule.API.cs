using System;
using System.Collections.Generic;
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
    ///     2. 资源包初始化与访问。
    ///     3. 资源卸载与查询。
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
            => CreateHandleTask(() => GetDefaultPackage().LoadAssetAsync(path), (h, t) => { h.Completed += h2 => t.TrySetResult(h2); });

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <typeparam name="T">资源类型</typeparam>
        /// <returns></returns>
        public UniTask<AssetHandle> LoadAssetAsync<T>(string path) where T : Object
            => CreateHandleTask(() => GetDefaultPackage().LoadAssetAsync<T>(path), (h, t) => { h.Completed += h2 => t.TrySetResult(h2); });

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="type">资源类型</param>
        /// <returns></returns>
        public UniTask<AssetHandle> LoadAssetAsync(string path, Type type)
            => CreateHandleTask(() => GetDefaultPackage().LoadAssetAsync(path, type), (h, t) => { h.Completed += h2 => t.TrySetResult(h2); });

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
            => CreateHandleTask(() => GetDefaultPackage().LoadSceneAsync(path, sceneMode, LocalPhysicsMode.None, activateOnLoad), (h, t) => { h.Completed += h2 => t.TrySetResult(h2); });

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

        #region 资源包

        /// <summary>
        /// 初始化默认资源包。
        /// </summary>
        /// <param name="downloadURL">热更资源包下载URL</param>
        /// <param name="downloadBackupURL">备用热更资源包下载URL</param>
        /// <returns></returns>
        public UniTask<bool> InitDefaultPackageAsync(string downloadURL = null, string downloadBackupURL = null)
        {
            return InitPackageAsync(DefaultPackageName, downloadURL, downloadBackupURL);
        }

        /// <summary>
        /// 初始化资源包。
        /// </summary>
        /// <param name="packageName">包名称</param>
        /// <param name="downloadURL">热更资源包下载URL</param>
        /// <param name="downloadBackupURL">备用热更资源包下载URL</param>
        /// <returns></returns>
        public UniTask<bool> InitPackageAsync(string packageName, string downloadURL = null, string downloadBackupURL = null)
        {
            packageName.NotNull(nameof(packageName));

            // 包已完成初始化（含启动流程 LaunchAssetHelper 预初始化的默认包），直接返回
            if (m_InitedPackageSet.Contains(packageName))
            {
                return UniTask.FromResult(true);
            }

            // 并发去重：同一包初始化中共享完成源（UniTaskCompletionSource.Task 可多次 await），
            // 避免二次调用在首次初始化完成前直接返回未就绪的 true
            if (m_InitPackageTasks.TryGetValue(packageName, out var sharedTask))
            {
                return sharedTask.Task;
            }

            var taskCompletionSource = new UniTaskCompletionSource<bool>();
            m_InitPackageTasks[packageName] = taskCompletionSource;

            try
            {
                // 创建资源包（不存在时）。v3 移除了全局默认包概念，只需创建包即可，通过包名访问
                var resourcePackage = TryGetPackage(packageName);
                if (resourcePackage == null)
                    resourcePackage = CreatePackage(packageName);

                // 同步创建初始化操作：抛异常（模拟构建失败等）或返回 null（非法 PlayMode）时回滚，允许重试
                var initHandler = InitPackage(resourcePackage, downloadURL, downloadBackupURL);
                if (initHandler == null)
                {
                    m_InitPackageTasks.Remove(packageName);
                    taskCompletionSource.TrySetException(new InvalidOperationException($"初始化资源包失败：{packageName}"));
                    return taskCompletionSource.Task;
                }

                initHandler.Completed += asyncOperationBase =>
                {
                    m_InitPackageTasks.Remove(packageName); // 完成后移除，失败也允许重试
                    if (asyncOperationBase.Error == null && asyncOperationBase.Status == EOperationStatus.Succeeded && asyncOperationBase.IsDone)
                    {
                        m_InitedPackageSet.Add(packageName);
                        taskCompletionSource.TrySetResult(true);
                    }
                    else
                    {
                        taskCompletionSource.TrySetException(new Exception(asyncOperationBase.Error));
                    }
                };
            }
            catch (Exception e)
            {
                m_InitPackageTasks.Remove(packageName);
                taskCompletionSource.TrySetException(e);
            }

            return taskCompletionSource.Task;
        }

        /// <summary>
        /// 创建资源包
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        /// <returns></returns>
        public ResourcePackage CreatePackage(string packageName) => YooAssets.CreatePackage(packageName);

        /// <summary>
        /// 尝试获取资源包
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        /// <returns></returns>
        public ResourcePackage TryGetPackage(string packageName)
        {
            if (!YooAssets.IsInitialized) return null; // YooAssets 未初始化（全局销毁后），防御不抛
            return YooAssets.TryGetPackage(packageName, out var package) ? package : null;
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
        /// 注意：默认包必须已初始化，否则同步抛 YooPackageInvalidException。
        /// </summary>
        public AssetInfo GetAssetInfo(string path) => GetDefaultPackage().GetAssetInfo(path);

        /// <summary>
        /// 检查指定的资源路径是否有效。
        /// 注意：默认包未初始化/不存在时返回 false，避免同步抛异常。
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
