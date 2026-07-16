using System;
using YooAsset;
using Cysharp.Threading.Tasks;
using FuFramework.Core.Runtime;
using FuFramework.Asset.Runtime;
using FuFramework.Event.Runtime;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable once CheckNamespace
// ReSharper disable NotAccessedField.Local
// ReSharper disable UnusedMember.Global
namespace Hotfix.Scene
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

        /// 事件订阅器
        private EventRegister EventRegister { get; set; }

        /// <summary>
        /// 初始化
        /// </summary>
        protected internal override void OnInit()
        {
            Instance = this;
            EventRegister = EventRegister.Create();
            m_AssetModule = ModuleManager.GetModule<AssetModule>();
        }

        /// <summary>
        /// 帧更新
        /// </summary>
        protected internal override void OnUpdate(float deltaTime, float unscaledDeltaTime)
        {
            foreach (var (_, sceneHandleData) in m_LoadingSceneDict)
            {
                OnLoadSceneUpdate(sceneHandleData.SceneHandle);
            }
        }

        /// <summary>
        /// 释放
        /// </summary>
        protected internal override void OnDispose()
        {
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

            return m_AssetModule.LoadSceneAsync(sceneAssetPath, LoadSceneMode.Single).Status != UniTaskStatus.Faulted;
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
            return m_LoadingSceneDict.ContainsKey(sceneAssetPath);
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
            var results = new string[m_LoadingSceneDict.Count];
            m_LoadingSceneDict.Keys.CopyTo(results, 0);
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
            results.AddRange(m_LoadingSceneDict.Keys);
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
            var sceneAssetPath = Utility.AssetPath.GetScenePath(sceneAssetName);
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

            var sceneOperationHandle = await m_AssetModule.LoadSceneAsync(sceneAssetPath, sceneMode);
            m_LoadingSceneDict.Add(sceneAssetPath, new SceneHandleData(sceneOperationHandle, userData));
            sceneOperationHandle.Completed += OnLoadSceneCompleted;
            return sceneOperationHandle;
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
            FuGuard.NotNull(sceneAssetPath, nameof(sceneAssetPath));

            if (IsUnloading(sceneAssetPath))
                throw new InvalidOperationException($"[SceneModule] 卸载场景 '{sceneAssetPath}' 失败, 场景正在卸载中!.");

            if (IsLoading(sceneAssetPath))
                throw new InvalidOperationException($"[SceneModule] 卸载场景 '{sceneAssetPath}' 失败, 场景正在加载中!.");

            if (!IsLoaded(sceneAssetPath))
                throw new InvalidOperationException($"[SceneModule] 卸载场景 '{sceneAssetPath}' 失败, 场景未加载!");

            if (!m_LoadedSceneDict.TryGetValue(sceneAssetPath, out var sceneOperationHandle)) return;

            var unloadHandle = sceneOperationHandle.UnloadAsync();
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
                    // 卸载失败
                    m_UnloadingSceneDict.TryGetValue(sceneAssetPath, out var sceneHandle);
                    if (sceneHandle == null) return;
                    FuLogger.LogError($"[SceneModule] 卸载场景 '{sceneHandle.SceneName}' 失败!, 加载状态 '{sceneHandle.Status}', 错误信息 '{sceneHandle.LastError}'.");
                    m_UnloadingSceneDict.Remove(sceneAssetPath);
                    var unloadSceneFailureEventArgs = UnloadSceneFailureEventArgs.Create(sceneHandle.SceneName, userData);
                    EventRegister.Broadcast(this, unloadSceneFailureEventArgs);
                }
            }
        }

        #endregion

        #region 加载场景回调

        /// <summary>
        /// 加载场景更新回调。
        /// </summary>
        /// <param name="sceneHandle"></param>
        private void OnLoadSceneUpdate(SceneHandle sceneHandle)
        {
            FuGuard.NotNull(sceneHandle, nameof(sceneHandle));
            var assetPath = sceneHandle.GetAssetInfo().AssetPath;
            if (!m_LoadingSceneDict.TryGetValue(assetPath, out var value)) return;

            FuLogger.LogInfo($"[SceneModule] 加载场景中 '{sceneHandle.SceneName}' 进度--{sceneHandle.Progress}.");
            var loadSceneUpdateEventArgs = LoadSceneUpdateEventArgs.Create(sceneHandle.SceneName, sceneHandle.Progress, value.UserData);
            EventRegister.Broadcast(this, loadSceneUpdateEventArgs);
        }

        /// <summary>
        /// 加载场景完成回调。
        /// </summary>
        /// <param name="sceneHandle"></param>
        private void OnLoadSceneCompleted(SceneHandle sceneHandle)
        {
            FuGuard.NotNull(sceneHandle, nameof(sceneHandle));

            var assetPath = sceneHandle.GetAssetInfo().AssetPath;
            m_LoadedSceneDict.Add(assetPath, sceneHandle);
            m_LoadingSceneDict.Remove(assetPath, out var sceneHandleData);

            if (sceneHandleData == null) return;
            if (sceneHandle.IsDone)
            {
                // 加载成功
                FuLogger.LogInfo($"[SceneModule] 加载场景 '{sceneHandle.SceneName}' 成功！");
                var loadSceneSuccessEventArgs = LoadSceneSuccessEventArgs.Create(sceneHandle.SceneName, sceneHandleData.UserData);
                EventRegister.Broadcast(this, loadSceneSuccessEventArgs);
            }
            else
            {
                // 加载失败
                var errorMessage = $"[SceneModule] 加载场景 '{sceneHandle.SceneName}' 失败!, 加载状态 '{sceneHandle.Status}', 错误信息 '{sceneHandle.LastError}'.";
                FuLogger.LogError(errorMessage);
                var loadSceneFailureEventArgs = LoadSceneFailureEventArgs.Create(sceneHandle.SceneName, sceneHandle.Status, errorMessage, sceneHandleData.UserData);
                EventRegister.Broadcast(this, loadSceneFailureEventArgs);
            }
        }

        #endregion
    }
}
