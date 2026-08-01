using System;
using System.Collections.Generic;
using YooAsset;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Core;
using AOT.Launch;
using AOT.Framework.ModuleSetting.Runtime;
using AOT.Framework.Core.Log;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

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
        /// 资源运行模式。
        /// </summary>
        public EPlayMode PlayMode { get; private set; }

        /// <summary>
        /// 默认资源包名称
        /// </summary>
        public string DefaultPackageName { get; private set; }

        /// <summary>
        /// 资源下载最大并发数量
        /// </summary>
        public int DownloadingMaxNum { get; private set; }

        /// <summary>
        /// 资源下载失败重试次数
        /// </summary>
        public int FailedTryAgainNum { get; private set; }

        /// <summary>
        /// YooAsset异步系统参数-每帧执行消耗的最大时间切片（单位：毫秒）
        /// </summary>
        public int AsyncSystemMaxSlicePerFrame { get; private set; }

        /// <summary>
        /// 已成功完成初始化的资源包集合（含启动流程 LaunchAssetHelper 预初始化的默认包）。
        /// 用于避免重复初始化。
        /// </summary>
        private readonly HashSet<string> m_InitedPackageSet = new();

        /// <summary>
        /// 资源包初始化任务去重字典，key:包名，value:共享完成源。
        /// 同一包并发初始化共享任务（UniTaskCompletionSource.Task 可多次 await），
        /// 防止二次调用直接返回未就绪的 true，也允许初始化失败后重试。
        /// </summary>
        private readonly Dictionary<string, UniTaskCompletionSource<bool>> m_InitPackageTasks = new();

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

            // 获取资源管理模块配置数据
            PlayMode                    = GameSetting.Instance.PlayMode;
            DefaultPackageName          = GameSetting.Instance.DefaultPackageName;
            DownloadingMaxNum           = GameSetting.Instance.DownloadingMaxNum;
            FailedTryAgainNum           = GameSetting.Instance.FailedTryAgainNum;
            AsyncSystemMaxSlicePerFrame = GameSetting.Instance.AsyncSystemMaxSlicePerFrame;

            FuLogger.LogInfo($"[AssetModule]资源系统运行模式：{PlayMode}");

            // 初始化YooAsset（启动阶段 LaunchAssetHelper 可能已初始化，避免二次初始化）
            if (LaunchAssetHelper.YooAssetInitialized)
            {
                // 启动流程已初始化 YooAsset 并创建/初始化默认包，标记默认包已初始化，避免热更侧重复初始化默认包
                m_InitedPackageSet.Add(DefaultPackageName);
            }
            else
            {
                YooAssets.Initialize();

                // 设置异步系统参数，每帧执行消耗的最大时间切片（单位：毫秒）
                YooAssets.SetAsyncOperationMaxTimeSlice(AsyncSystemMaxSlicePerFrame);

                LaunchAssetHelper.YooAssetInitialized = true;
            }

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

            // 清理包初始化状态（热更重载 OnInit 会按 LaunchAssetHelper.YooAssetInitialized 重新标记）
            m_InitedPackageSet.Clear();
            m_InitPackageTasks.Clear();

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

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <returns></returns>
        public UniTask<AssetHandle> LoadAssetAsync(AssetInfo assetInfo)
            => CreateHandleTask(() => GetDefaultPackage().LoadAssetAsync(assetInfo), (h, t) => { h.Completed += h2 => t.TrySetResult(h2); });

        /// <summary>
        /// 异步加载全部资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        public UniTask<AllAssetsHandle> LoadAllAssetsAsync<T>(string path) where T : Object
            => CreateHandleTask(() => GetDefaultPackage().LoadAllAssetsAsync<T>(path), (h, t) => { h.Completed += h2 => t.TrySetResult(h2); });

        /// <summary>
        /// 异步加载全部资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="type">资源类型</param>
        /// <returns></returns>
        public UniTask<AllAssetsHandle> LoadAllAssetsAsync(string path, Type type)
            => CreateHandleTask(() => GetDefaultPackage().LoadAllAssetsAsync(path, type), (h, t) => { h.Completed += h2 => t.TrySetResult(h2); });

        /// <summary>
        /// 异步加载资源包内所有资源对象
        /// </summary>
        /// <param name="path">资源的定位地址</param>
        public UniTask<AllAssetsHandle> LoadAllAssetsAsync(string path)
            => CreateHandleTask(() => GetDefaultPackage().LoadAllAssetsAsync(path), (h, t) => { h.Completed += h2 => t.TrySetResult(h2); });

        /// <summary>
        /// 异步加载资源包内所有资源对象
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        public UniTask<AllAssetsHandle> LoadAllAssetsAsync(AssetInfo assetInfo)
            => CreateHandleTask(() => GetDefaultPackage().LoadAllAssetsAsync(assetInfo), (h, t) => { h.Completed += h2 => t.TrySetResult(h2); });

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

        /// <summary>
        /// 异步加载场景
        /// </summary>
        /// <param name="assetInfo">资源路径</param>
        /// <param name="sceneMode">场景模式</param>
        /// <param name="activateOnLoad">是否加载完成自动激活</param>
        /// <returns></returns>
        public UniTask<SceneHandle> LoadSceneAsync(AssetInfo assetInfo, LoadSceneMode sceneMode, bool activateOnLoad = true)
            => CreateHandleTask(() => GetDefaultPackage().LoadSceneAsync(assetInfo, sceneMode, LocalPhysicsMode.None, activateOnLoad), (h, t) => { h.Completed += h2 => t.TrySetResult(h2); });

        #endregion

        #region 异步加载子资源对象

        /// <summary>
        /// 异步加载子资源对象
        /// </summary>
        /// <param name="path">资源的定位地址</param>
        public UniTask<SubAssetsHandle> LoadSubAssetsAsync(string path)
            => CreateHandleTask(() => GetDefaultPackage().LoadSubAssetsAsync(path), (h, t) => { h.Completed += h2 => t.TrySetResult(h2); });

        /// <summary>
        /// 异步加载子资源对象
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        public UniTask<SubAssetsHandle> LoadSubAssetsAsync<T>(string path) where T : Object
            => CreateHandleTask(() => GetDefaultPackage().LoadSubAssetsAsync<T>(path), (h, t) => { h.Completed += h2 => t.TrySetResult(h2); });

        /// <summary>
        /// 异步加载子资源对象
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="type"></param>
        /// <returns></returns>
        public UniTask<SubAssetsHandle> LoadSubAssetsAsync(string path, Type type)
            => CreateHandleTask(() => GetDefaultPackage().LoadSubAssetsAsync(path, type), (h, t) => { h.Completed += h2 => t.TrySetResult(h2); });

        /// <summary>
        /// 异步加载子资源对象
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <returns></returns>
        public UniTask<SubAssetsHandle> LoadSubAssetsAsync(AssetInfo assetInfo)
            => CreateHandleTask(() => GetDefaultPackage().LoadSubAssetsAsync(assetInfo), (h, t) => { h.Completed += h2 => t.TrySetResult(h2); });

        #endregion

        #region 异步加载原生文件

        /// <summary>
        /// 异步加载原生文件
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <returns></returns>
        public UniTask<BundleFileHandle> LoadRawFileAsync(AssetInfo assetInfo)
            => CreateHandleTask(() => GetDefaultPackage().LoadBundleFileAsync(assetInfo), (h, t) => { h.Completed += h2 => t.TrySetResult(h2); });

        /// <summary>
        /// 异步加载原生文件
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        public UniTask<BundleFileHandle> LoadRawFileAsync(string path)
            => CreateHandleTask(() => GetDefaultPackage().LoadBundleFileAsync(path), (h, t) => { h.Completed += h2 => t.TrySetResult(h2); });

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

        /// <summary>
        /// 检查资源包是否存在
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        /// <returns></returns>
        public bool HasPackage(string packageName)
        {
            if (!YooAssets.IsInitialized) return false; // YooAssets 未初始化（全局销毁后），防御不抛
            return YooAssets.TryGetPackage(packageName, out _);
        }

        /// <summary>
        /// 获取默认资源包
        /// </summary>
        /// <returns></returns>
        public ResourcePackage GetDefaultPackage() => YooAssets.GetPackage(DefaultPackageName);

        /// <summary>
        /// 获取资源包
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        /// <returns></returns>
        public ResourcePackage GetPackage(string packageName) => YooAssets.GetPackage(packageName);

        /// <summary>
        /// 创建资源下载器。
        /// 注意：不传 tags 时下载全部资源（YooAsset ResourceDownloaderOptions 的 Tags 为 null 表示全部），
        /// 建议传入资源标签只下载所需增量，避免全量下载。
        /// </summary>
        /// <param name="tags">资源标签，为空时下载全部。</param>
        /// <returns>资源下载器。</returns>
        public ResourceDownloaderOperation CreateResourceDownloader(params string[] tags)
        {
            if (tags == null || tags.Length == 0)
                return GetDefaultPackage().CreateResourceDownloader(new ResourceDownloaderOptions(DownloadingMaxNum, FailedTryAgainNum));   // 全部
            return GetDefaultPackage().CreateResourceDownloader(new ResourceDownloaderOptions(tags, DownloadingMaxNum, FailedTryAgainNum)); // 按标签
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

        /// <summary>
        /// 卸载指定资源包下的指定资源。
        /// 注意：如果该资源还在被使用，该方法会无效
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        /// <param name="assetPath">资源路径</param>
        public void UnloadAsset(string packageName, string assetPath)
        {
            packageName.NotNull(nameof(packageName));
            assetPath.NotNull(nameof(assetPath));
            if (!YooAssets.IsInitialized) return;                               // YooAssets 未初始化（全局销毁后），防御不抛
            if (!YooAssets.TryGetPackage(packageName, out var package)) return; // 包不存在/已销毁时不抛异常
            package.TryUnloadUnusedAsset(assetPath);
        }

        /// <summary>
        /// 卸载所有无用资源。
        /// 注意：该方法会卸载所有引用计数为零的资源包，可以在切换场景之后调用资源释放方法或者写定时器间隔时间去释放。
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        public async UniTaskVoid UnloadUnusedAssetsAsync(string packageName)
        {
            packageName.NotNull(nameof(packageName));
            if (!YooAssets.IsInitialized) return;                               // YooAssets 未初始化（全局销毁后），防御不抛
            if (!YooAssets.TryGetPackage(packageName, out var package)) return; // 包不存在/已销毁时不抛异常
            await package.UnloadUnusedAssetsAsync();
        }

        /// <summary>
        /// 强制卸载所有资源。
        /// 注意：该方法请在合适的时机调用。Package在销毁的时候也会自动调用该方法。
        /// 警告：此操作会释放所有已加载句柄；进行中的 LoadAssetAsync 句柄被 Release 后
        /// Completed 回调不再触发，其 UniTask 将永久挂起，请确保调用时无进行中的加载。
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        public async UniTaskVoid UnloadAllAssetsAsync(string packageName)
        {
            packageName.NotNull(nameof(packageName));
            if (!YooAssets.IsInitialized) return;
            if (!YooAssets.TryGetPackage(packageName, out var package)) return;
            await package.UnloadAllAssetsAsync();
        }

        /// <summary>
        /// 清理YooAssets文件系统所有的缓存资源文件。
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        public async UniTaskVoid ClearAllBundleFilesAsync(string packageName)
        {
            packageName.NotNull(nameof(packageName));
            if (!YooAssets.IsInitialized) return;                               // YooAssets 未初始化（全局销毁后），防御不抛
            if (!YooAssets.TryGetPackage(packageName, out var package)) return; // 包不存在/已销毁时不抛异常
            await package.ClearCacheAsync(new ClearCacheOptions("ClearAllBundleFiles"));
        }

        /// <summary>
        /// 清理YooAssets文件系统未使用的缓存资源文件。
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        public async UniTaskVoid ClearUnusedBundleFilesAsync(string packageName)
        {
            packageName.NotNull(nameof(packageName));
            if (!YooAssets.IsInitialized) return;                               // YooAssets 未初始化（全局销毁后），防御不抛
            if (!YooAssets.TryGetPackage(packageName, out var package)) return; // 包不存在/已销毁时不抛异常
            await package.ClearCacheAsync(new ClearCacheOptions("ClearUnusedBundleFiles"));
        }

        #endregion

        #region Get

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
        /// 是否需要下载。
        /// 注意：默认包未初始化/不存在时返回 false，避免同步抛异常。
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <returns></returns>
        public bool IsNeedDownload(AssetInfo assetInfo)
        {
            if (!TryGetReadyPackage(out var package)) return false;
            return package.GetDownloadSize(assetInfo) > 0;
        }

        /// <summary>
        /// 是否需要下载。
        /// 注意：默认包未初始化/不存在时返回 false，避免同步抛异常。
        /// </summary>
        /// <param name="path">资源地址</param>
        /// <returns></returns>
        public bool IsNeedDownload(string path)
        {
            if (!TryGetReadyPackage(out var package)) return false;
            return package.GetDownloadSize(path) > 0;
        }

        /// <summary>
        /// 获取资源信息。
        /// 注意：默认包必须已初始化，否则同步抛 YooPackageInvalidException。
        /// </summary>
        /// <param name="assetTags">资源标签列表</param>
        /// <returns></returns>
        public AssetInfo[] GetAssetInfos(string[] assetTags) => GetDefaultPackage().GetAssetInfos(assetTags);

        /// <summary>
        /// 获取资源信息。
        /// 注意：默认包必须已初始化，否则同步抛 YooPackageInvalidException。
        /// </summary>
        /// <param name="assetTag">资源标签</param>
        /// <returns></returns>
        public AssetInfo[] GetAssetInfos(string assetTag) => GetDefaultPackage().GetAssetInfos(assetTag);

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