using System;
using YooAsset;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Utility;
using AOT.Framework.Core.Log;
using UtilityAOT = AOT.Framework.Core.Utility.UtilityAOT;
using Hotfix.Framework.Asset;
using Hotfix.Framework.Event;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Utility = Hotfix.Framework.Core.Utility;

// ReSharper disable once CheckNamespace
// ReSharper disable NotAccessedField.Local
// ReSharper disable UnusedMember.Global
namespace Hotfix.Framework.Scene
{
    /// <summary>
    /// 场景管理模块。
    /// 功能：
    ///     1. 配合资源管理模块，管理场景资源的加载、卸载。
    ///     2. 提供场景加载进度，加载成功、加载失败，卸载成功、卸载失败的事件。
    /// </summary>
    public sealed class SceneModule : ModuleBase
    {
        /// <summary>
        /// 模块单例
        /// </summary>
        public static SceneModule Instance { get; private set; }

        /// <summary>
        /// 封装场景加载中的数据
        /// </summary>
        private sealed class SceneHandleData
        {
            /// <summary>
            /// 场景加载句柄
            /// </summary>
            public readonly SceneHandle SceneHandle;

            /// <summary>
            /// 用户自定义数据
            /// </summary>
            public readonly object UserData;

            public SceneHandleData(SceneHandle sceneHandle, object userData)
            {
                SceneHandle = sceneHandle;
                UserData    = userData;
            }
        }

        /// <summary>
        /// 已加载的场景字典，Key为场景资源路径，Value为场景加载句柄
        /// </summary>
        /// <summary>
        /// 正在加载中的场景路径集合（await 前置位）。
        /// m_LoadingSceneDict 需在 await 完成后拿到 SceneHandle 才登记，await 期间无法拦截并发同路径加载；
        /// 此集合在 await 前占位，杜绝并发重复加载导致 Dictionary.Add 抛重复 key。
        /// </summary>
        private readonly HashSet<string> m_LoadingSceneSet = new();

        private readonly Dictionary<string, SceneHandle> m_LoadedSceneDict = new();

        /// <summary>
        /// 正在加载的场景字典，Key为场景资源路径，Value为场景加载句柄数据
        /// </summary>
        private readonly Dictionary<string, SceneHandleData> m_LoadingSceneDict = new();

        /// <summary>
        /// 正在卸载的场景字典，Key为场景资源路径，Value为场景加载句柄
        /// </summary>
        private readonly Dictionary<string, SceneHandle> m_UnloadingSceneDict = new();

        /// <summary>
        /// 资源管理模块
        /// </summary>
        private AssetModule m_AssetModule;

        /// <summary>
        /// 是否已销毁（重启/模块释放）。防止在途场景加载完成后把句柄写回已销毁模块。
        /// </summary>
        private bool m_IsDisposed;

        /// <summary>
        /// 模块生命周期代数。OnDispose 递增，使旧生命周期在途的场景加载 await 后
        /// 仍能识别并拒绝把旧句柄写回新生命周期（m_IsDisposed 会被 OnInit 重置，无法覆盖跨 ReInit 窗口）。
        /// </summary>
        private int m_LifecycleEpoch;

        /// 事件订阅器
        private EventRegister EventRegister { get; set; }

        /// <summary>
        /// 初始化
        /// </summary>
        protected internal override void OnInit()
        {
            Instance = this;
            m_IsDisposed = false;
            EventRegister = EventRegister.Create();
            m_AssetModule = ModuleManager.GetModule<AssetModule>();
        }

        /// <summary>
        /// 释放
        /// </summary>
        protected internal override void OnDispose()
        {
            m_IsDisposed = true;
            m_LifecycleEpoch++; // 递增生命周期代数：在途场景加载 await 后据此识别旧生命周期，拒绝写回新生命周期

            // 反向遍历已加载的场景，卸载所有已加载的场景
            var loadedScenePaths = new string[m_LoadedSceneDict.Count];
            m_LoadedSceneDict.Keys.CopyTo(loadedScenePaths, 0);
            for (var i = loadedScenePaths.Length - 1; i >= 0; i--)
            {
                var loadedScenePath = loadedScenePaths[i];
                if (IsUnloading(loadedScenePath)) continue;
                UnloadScene(loadedScenePath);
            }

            m_LoadedSceneDict.Clear();
            m_LoadingSceneDict.Clear();
            m_UnloadingSceneDict.Clear();
            m_LoadingSceneSet.Clear(); // 若不清理，重启后 IsLoading 对旧路径恒 true，LoadScene 永久拒绝

            EventRegister.Release();
            EventRegister = null;
            Instance = null;
        }

        #region Get

        /// <summary>
        /// 检查场景资源是否存在。
        /// </summary>
        /// <param name="sceneAssetPath">要检查场景资源的名称。</param>
        /// <returns>场景资源是否存在。</returns>
        public bool HasScene(string sceneAssetPath)
        {
            if (string.IsNullOrEmpty(sceneAssetPath))
            {
                FuLogger.LogError("[SceneModule] 场景资源路径无效!");
                return false;
            }

            if (!sceneAssetPath.StartsWith("Assets/", StringComparison.Ordinal) || !sceneAssetPath.EndsWith(".unity", StringComparison.Ordinal))
            {
                FuLogger.LogError($"[SceneModule] 场景资源路径 '{sceneAssetPath}' 格式错误!");
                return false;
            }

            // 仅做资源存在性检查，绝不能用 LoadSceneAsync（会真正触发一次场景加载，且句柄无法释放）
            return m_AssetModule.HasAssetPath(sceneAssetPath);
        }

        /// <summary>
        /// 获取场景是否已加载。
        /// </summary>
        /// <param name="sceneAssetPath">场景资源路径。</param>
        /// <returns>场景是否已加载。</returns>
        public bool IsLoaded(string sceneAssetPath)
        {
            if (string.IsNullOrEmpty(sceneAssetPath)) throw new InvalidOperationException("[SceneModule] 场景资源路径无效!");
            return m_LoadedSceneDict.ContainsKey(sceneAssetPath);
        }

        /// <summary>
        /// 获取场景是否正在加载。
        /// </summary>
        /// <param name="sceneAssetPath">场景资源路径。</param>
        /// <returns>场景是否正在加载。</returns>
        public bool IsLoading(string sceneAssetPath)
        {
            if (string.IsNullOrEmpty(sceneAssetPath)) throw new InvalidOperationException("[SceneModule] 场景资源路径无效!");
            return m_LoadingSceneSet.Contains(sceneAssetPath) || m_LoadingSceneDict.ContainsKey(sceneAssetPath);
        }

        /// <summary>
        /// 获取场景是否正在卸载。
        /// </summary>
        /// <param name="sceneAssetPath">场景资源路径。</param>
        /// <returns>场景是否正在卸载。</returns>
        public bool IsUnloading(string sceneAssetPath)
        {
            if (string.IsNullOrEmpty(sceneAssetPath)) throw new InvalidOperationException("[SceneModule] 场景资源路径无效!");
            return m_UnloadingSceneDict.ContainsKey(sceneAssetPath);
        }

        /// <summary>
        /// 获取场景名称。
        /// </summary>
        /// <param name="sceneAssetPath">场景资源路径。</param>
        /// <returns>场景名称。</returns>
        public string GetSceneName(string sceneAssetPath)
        {
            if (string.IsNullOrEmpty(sceneAssetPath))
            {
                FuLogger.LogError("[SceneModule] 场景资源路径无效!");
                return null;
            }

            var sceneNamePosition = sceneAssetPath.LastIndexOf('/');
            if (sceneNamePosition + 1 >= sceneAssetPath.Length)
            {
                FuLogger.LogError($"[SceneModule] 场景资源路径 '{sceneAssetPath}' 格式错误!");
                return null;
            }

            var sceneName = sceneAssetPath.Substring(sceneNamePosition + 1);
            sceneNamePosition = sceneName.LastIndexOf(".unity", StringComparison.Ordinal);
            if (sceneNamePosition > 0)
            {
                sceneName = sceneName.Substring(0, sceneNamePosition);
            }

            return sceneName;
        }

        /// <summary>
        /// 获取所有已加载场景的资源路径。
        /// </summary>
        /// <returns>已加载场景的资源路径。</returns>
        public string[] GetAllLoadedSceneAssetPaths()
        {
            var results = new string[m_LoadedSceneDict.Count];
            m_LoadedSceneDict.Keys.CopyTo(results, 0);
            return results;
        }

        /// <summary>
        /// 获取所有已加载场景的资源路径。
        /// </summary>
        /// <param name="results">已加载场景的资源路径。</param>
        public void GetAllLoadedSceneAssetPaths(List<string> results)
        {
            if (results == null) throw new InvalidOperationException("[SceneModule] 结果参数列表为空!");
            results.Clear();
            results.AddRange(m_LoadedSceneDict.Keys);
        }

        /// <summary>
        /// 获取所有正在加载场景的资源路径。
        /// </summary>
        /// <returns>正在加载场景的资源路径。</returns>
        public string[] GetAllLoadingSceneAssetPaths()
        {
            var results = new string[m_LoadingSceneSet.Count];
            m_LoadingSceneSet.CopyTo(results);
            return results;
        }

        /// <summary>
        /// 获取所有正在加载场景的资源路径。
        /// </summary>
        /// <param name="results">正在加载场景的资源路径。</param>
        public void GetAllLoadingSceneAssetPaths(List<string> results)
        {
            if (results == null) throw new InvalidOperationException("[SceneModule] 结果参数列表为空!");
            results.Clear();
            results.AddRange(m_LoadingSceneSet);
        }

        /// <summary>
        /// 获取所有正在卸载场景的资源路径。
        /// </summary>
        /// <returns>正在卸载场景的资源路径。</returns>
        public string[] GetAllUnloadingSceneAssetPaths()
        {
            var results = new string[m_UnloadingSceneDict.Count];
            m_UnloadingSceneDict.Keys.CopyTo(results, 0);
            return results;
        }

        /// <summary>
        /// 获取所有正在卸载场景的资源路径。
        /// </summary>
        /// <param name="results">正在卸载场景的资源路径。</param>
        public void GetAllUnloadingSceneAssetPaths(List<string> results)
        {
            if (results == null) throw new InvalidOperationException("[SceneModule] 结果参数列表为空!");
            results.Clear();
            results.AddRange(m_UnloadingSceneDict.Keys);
        }

        #endregion

        #region Set

        /// <summary>
        /// 设置活动场景。
        /// </summary>
        /// <param name="activeScene"></param>
        private void SetActiveScene(UnityEngine.SceneManagement.Scene activeScene)
        {
            var lastActiveScene = SceneManager.GetActiveScene();
            if (lastActiveScene == activeScene) return;
            SceneManager.SetActiveScene(activeScene);
            var activeSceneChangedEventArgs = ActiveSceneChangedEventArgs.Create(lastActiveScene, activeScene);
            EventRegister.Broadcast(this, activeSceneChangedEventArgs);
        }

        #endregion

        #region 加载场景

        /// <summary>
        /// 加载场景。
        /// </summary>
        /// <param name="sceneAssetName">场景资源路径。</param>
        /// <param name="sceneMode">加载模式。</param>
        /// <param name="userData">用户自定义数据。</param>
        public UniTask<SceneHandle> LoadSceneByName(string sceneAssetName, LoadSceneMode sceneMode = LoadSceneMode.Additive, object userData = null)
        {
            if (string.IsNullOrEmpty(sceneAssetName)) throw new InvalidOperationException("[SceneModule] 场景资源名称不能为空!.");
            var sceneAssetPath = UtilityAOT.AssetPath.GetScenePath(sceneAssetName);
            return LoadScene(sceneAssetPath, sceneMode, userData);
        }


        /// <summary>
        /// 加载场景。
        /// </summary>
        /// <param name="sceneAssetPath">场景资源路径。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="sceneMode"></param>
        public async UniTask<SceneHandle> LoadScene(string sceneAssetPath, LoadSceneMode sceneMode = LoadSceneMode.Additive, object userData = null)
        {
            if (string.IsNullOrEmpty(sceneAssetPath))
                throw new InvalidOperationException("[SceneModule] 场景资源路径不能为空!.");

            if (!sceneAssetPath.StartsWith("Assets/", StringComparison.Ordinal) || !sceneAssetPath.EndsWith(".unity", StringComparison.Ordinal))
                throw new InvalidOperationException($"[SceneModule] 场景资源路径 '{sceneAssetPath}' 格式错误!");

            if (IsUnloading(sceneAssetPath))
                throw new InvalidOperationException($"[SceneModule] 场景资源 '{sceneAssetPath}' 正在卸载中!");

            if (IsLoading(sceneAssetPath))
                throw new InvalidOperationException($"[SceneModule] 场景资源 '{sceneAssetPath}' 正在加载中!");

            if (IsLoaded(sceneAssetPath))
                throw new InvalidOperationException($"[SceneModule] 场景资源 '{sceneAssetPath}' 已被加载过，不能重复加载!");

            // await 前置位：先登记 loading 状态拦截并发同路径加载（m_LoadingSceneDict 需 await 完成拿到 handle 才能登记）
            m_LoadingSceneSet.Add(sceneAssetPath);
            var lifecycleEpoch = m_LifecycleEpoch; // 发起时生命周期代数：重启后旧在途加载据此识别并拒绝写回新生命周期
            try
            {
                var sceneName = GetSceneName(sceneAssetPath);
                var sceneOperationHandle = await m_AssetModule.LoadSceneAsync(sceneAssetPath, sceneMode, onProgress: p => OnLoadSceneProgress(sceneName, p, userData));
                // 模块已销毁/生命周期变更（重启期间在途加载）：释放句柄、不登记，抛 ObjectDisposedException
                if (m_IsDisposed || lifecycleEpoch != m_LifecycleEpoch)
                {
                    sceneOperationHandle.Release();
                    throw new ObjectDisposedException(nameof(SceneModule));
                }
                m_LoadingSceneDict.Add(sceneAssetPath, new SceneHandleData(sceneOperationHandle, userData));
                sceneOperationHandle.Completed += OnLoadSceneCompleted;
                return sceneOperationHandle;
            }
            finally
            {
                m_LoadingSceneSet.Remove(sceneAssetPath); // 成功/异常均移除占位
            }
        }

        #endregion

        #region 卸载场景

        /// <summary>
        /// 卸载场景。
        /// </summary>
        /// <param name="sceneAssetPath">场景资源路径。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void UnloadScene(string sceneAssetPath, object userData = null)
        {
            sceneAssetPath.NotNull(nameof(sceneAssetPath));

            if (IsUnloading(sceneAssetPath))
                throw new InvalidOperationException($"[SceneModule] 卸载场景 '{sceneAssetPath}' 失败, 场景正在卸载中!.");

            if (IsLoading(sceneAssetPath))
                throw new InvalidOperationException($"[SceneModule] 卸载场景 '{sceneAssetPath}' 失败, 场景正在加载中!.");

            if (!IsLoaded(sceneAssetPath))
                throw new InvalidOperationException($"[SceneModule] 卸载场景 '{sceneAssetPath}' 失败, 场景未加载!");

            if (!m_LoadedSceneDict.TryGetValue(sceneAssetPath, out var sceneOperationHandle)) return;

            var unloadHandle = sceneOperationHandle.UnloadSceneAsync();
            m_LoadedSceneDict.Remove(sceneAssetPath);
            m_UnloadingSceneDict.Add(sceneAssetPath, sceneOperationHandle);

            unloadHandle.Completed += OnUnloadSceneOperationHandleOnCompleted;
            return;

            // 卸载场景资源完成回调
            void OnUnloadSceneOperationHandleOnCompleted(AsyncOperationBase asyncOperationBase)
            {
                if (asyncOperationBase.Error.IsNullOrEmpty())
                {
                    // 卸载成功
                    m_UnloadingSceneDict.TryGetValue(sceneAssetPath, out var sceneHandle);
                    if (sceneHandle == null) return;
                    FuLogger.LogInfo($"[SceneModule] 卸载场景 '{sceneHandle.SceneName}' 成功！");
                    var unloadSceneSuccessEventArgs = UnloadSceneSuccessEventArgs.Create(sceneHandle.SceneName, userData);
                    m_UnloadingSceneDict.Remove(sceneAssetPath);
                    m_LoadedSceneDict.Remove(sceneAssetPath);
                    EventRegister.Broadcast(this, unloadSceneSuccessEventArgs);
                }
                else
                {
                    // 卸载失败：场景仍已加载，恢复登记以便重试卸载（不释放句柄——场景仍存在，YooAsset 的 sceneUnloaded 钩子不会触发，句柄仍有效）
                    m_UnloadingSceneDict.TryGetValue(sceneAssetPath, out var sceneHandle);
                    if (sceneHandle == null) return;
                    FuLogger.LogError($"[SceneModule] 卸载场景 '{sceneHandle.SceneName}' 失败!, 加载状态 '{sceneHandle.Status}', 错误信息 '{sceneHandle.Error}'.");
                    m_UnloadingSceneDict.Remove(sceneAssetPath);

                    // 模块已销毁（OnDispose 已清空登记字典）：不再恢复登记（避免残留），显式释放句柄兜底，防卸载失败句柄泄漏
                    if (m_IsDisposed)
                    {
                        sceneHandle.Release();
                        return;
                    }

                    m_LoadedSceneDict.Add(sceneAssetPath, sceneHandle);
                    var unloadSceneFailureEventArgs = UnloadSceneFailureEventArgs.Create(sceneHandle.SceneName, userData);
                    EventRegister.Broadcast(this, unloadSceneFailureEventArgs);
                }
            }
        }

        #endregion

        #region 加载场景回调

        /// <summary>
        /// 加载场景进度回调（LoadSceneAsync 每帧上报，供加载界面显示进度）。
        /// </summary>
        /// <param name="sceneName">场景名称。</param>
        /// <param name="progress">加载进度（0~1）。</param>
        /// <param name="userData">用户自定义数据。</param>
        private void OnLoadSceneProgress(string sceneName, float progress, object userData)
        {
            // 模块已销毁（热更/卸载，EventRegister 已置 null）：不再广播进度，避免 NRE
            if (m_IsDisposed) return;
            var loadSceneUpdateEventArgs = LoadSceneUpdateEventArgs.Create(sceneName, progress, userData);
            EventRegister.Broadcast(this, loadSceneUpdateEventArgs);
        }

        /// <summary>
        /// 加载场景完成回调。
        /// </summary>
        /// <param name="sceneHandle"></param>
        private void OnLoadSceneCompleted(SceneHandle sceneHandle)
        {
            sceneHandle.NotNull(nameof(sceneHandle));

            var assetPath = sceneHandle.GetAssetInfo().AssetPath;
            m_LoadingSceneDict.Remove(assetPath, out var sceneHandleData);

            // 模块已销毁（OnDispose 已清空字典）：不登记，释放句柄避免泄漏
            if (m_IsDisposed)
            {
                sceneHandle.Release();
                return;
            }

            if (sceneHandleData == null) return;
            if (sceneHandle.Status == EOperationStatus.Succeeded)
            {
                // 加载成功：登记已加载字典（失败不登记，否则 IsLoaded 恒 true 导致无法重试）
                m_LoadedSceneDict.Add(assetPath, sceneHandle);
                FuLogger.LogInfo($"[SceneModule] 加载场景 '{sceneHandle.SceneName}' 成功！");
                var loadSceneSuccessEventArgs = LoadSceneSuccessEventArgs.Create(sceneHandle.SceneName, sceneHandleData.UserData);
                EventRegister.Broadcast(this, loadSceneSuccessEventArgs);
            }
            else
            {
                // 加载失败：先读取状态信息，再释放句柄（避免 provider/句柄残留，场景未加载时 YooAsset 无法自动回收）
                var sceneName    = sceneHandle.SceneName;
                var status       = sceneHandle.Status;
                var errorMessage = $"[SceneModule] 加载场景 '{sceneName}' 失败!, 加载状态 '{status}', 错误信息 '{sceneHandle.Error}'.";
                FuLogger.LogError(errorMessage);
                sceneHandle.Release();
                var loadSceneFailureEventArgs = LoadSceneFailureEventArgs.Create(sceneName, status, errorMessage, sceneHandleData.UserData);
                EventRegister.Broadcast(this, loadSceneFailureEventArgs);
            }
        }

        #endregion
    }
}
