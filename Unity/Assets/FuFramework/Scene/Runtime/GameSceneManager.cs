using System;
using YooAsset;
using System.Linq;
using Cysharp.Threading.Tasks;
using FuFramework.Core.Runtime;
using FuFramework.Asset.Runtime;
using System.Collections.Generic;
using FuFramework.Event.Runtime;
using UnityEngine.SceneManagement;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable once CheckNamespace
// ReSharper disable NotAccessedField.Local
namespace FuFramework.Scene.Runtime
{
    /// <summary>
    /// 场景管理器。
    /// 功能：
    /// 1. 管理场景资源的加载、卸载。
    /// 2. 提供加载、卸载场景的接口。
    /// 3. 提供场景加载进度，加载成功、加载失败，卸载成功、卸载失败的事件。
    /// </summary>
    public sealed class GameSceneManager : MonoSingleton<GameSceneManager>
    {
        private const int DefaultPriority = 0; // 模块默认优先级

        /// <summary>
        /// 封装场景加载中的数据
        /// </summary>
        private sealed class SceneHandleData
        {
            /// 场景加载句柄
            public readonly SceneHandle SceneHandle;

            /// 用户自定义数据
            public readonly object UserData;

            public SceneHandleData(SceneHandle sceneHandle, object userData)
            {
                SceneHandle = sceneHandle;
                UserData = userData;
            }
        }

        private readonly Dictionary<string, SceneHandle> m_LoadedSceneDict = new(); // 已加载的场景字典，Key为场景资源路径，Value为场景加载句柄
        private readonly Dictionary<string, SceneHandleData> m_LoadingSceneDict = new(); // 正在加载的场景字典，Key为场景资源路径，Value为场景加载句柄数据
        private readonly Dictionary<string, SceneHandle> m_UnloadingSceneDict = new(); // 正在卸载的场景字典，Key为场景资源路径，Value为场景加载句柄

        private EventRegister EventRegister { get; set; } // 事件订阅器

        protected override void Init()
        {
            base.Init();
            EventRegister = EventRegister.Create();
        }

        /// <summary>
        /// 场景管理器轮询。
        /// </summary>
        private void Update()
        {
            foreach (var (_, sceneHandleData) in m_LoadingSceneDict)
            {
                OnLoadSceneUpdate(sceneHandleData.SceneHandle);
            }
        }

        protected override void Dispose()
        {
            base.Dispose();
            foreach (var (loadedSceneAssetName, _) in m_LoadedSceneDict)
            {
                if (SceneIsUnloading(loadedSceneAssetName)) continue;
                UnloadScene(loadedSceneAssetName);
            }

            m_LoadedSceneDict.Clear();
            m_LoadingSceneDict.Clear();
            m_UnloadingSceneDict.Clear();

            EventRegister.Clear();
            EventRegister = null;
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
                Log.Error("场景资源路径无效!");
                return false;
            }

            if (!sceneAssetPath.StartsWith("Assets/", StringComparison.Ordinal) || !sceneAssetPath.EndsWith(".unity", StringComparison.Ordinal))
            {
                Log.Error("场景资源路径 '{0}' 格式错误!", sceneAssetPath);
                return false;
            }
            
            return AssetManager.Instance.LoadSceneAsync(sceneAssetPath, LoadSceneMode.Single).Status != UniTaskStatus.Faulted;
        }

        /// <summary>
        /// 获取场景是否已加载。
        /// </summary>
        /// <param name="sceneAssetPath">场景资源路径。</param>
        /// <returns>场景是否已加载。</returns>
        public bool SceneIsLoaded(string sceneAssetPath)
        {
            if (string.IsNullOrEmpty(sceneAssetPath)) throw new FuException("场景资源路径无效!");
            return m_LoadedSceneDict.ContainsKey(sceneAssetPath);
        }

        /// <summary>
        /// 获取场景是否正在加载。
        /// </summary>
        /// <param name="sceneAssetPath">场景资源路径。</param>
        /// <returns>场景是否正在加载。</returns>
        public bool SceneIsLoading(string sceneAssetPath)
        {
            if (string.IsNullOrEmpty(sceneAssetPath)) throw new FuException("场景资源路径无效!");
            return m_LoadingSceneDict.ContainsKey(sceneAssetPath);
        }

        /// <summary>
        /// 获取场景是否正在卸载。
        /// </summary>
        /// <param name="sceneAssetPath">场景资源路径。</param>
        /// <returns>场景是否正在卸载。</returns>
        public bool SceneIsUnloading(string sceneAssetPath)
        {
            if (string.IsNullOrEmpty(sceneAssetPath)) throw new FuException("场景资源路径无效!");
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
                Log.Error("场景资源路径无效!");
                return null;
            }

            var sceneNamePosition = sceneAssetPath.LastIndexOf('/');
            if (sceneNamePosition + 1 >= sceneAssetPath.Length)
            {
                Log.Error("场景资源路径 '{0}' 格式错误!", sceneAssetPath);
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
        public string[] GetAllLoadedSceneAssetPaths() => m_LoadedSceneDict.Keys.ToArray();

        /// <summary>
        /// 获取所有已加载场景的资源路径。
        /// </summary>
        /// <param name="results">已加载场景的资源路径。</param>
        public void GetAllLoadedSceneAssetPaths(List<string> results)
        {
            if (results == null) throw new FuException("结果参数列表为空!");
            results.Clear();
            results.AddRange(m_LoadedSceneDict.Keys);
        }

        /// <summary>
        /// 获取所有正在加载场景的资源路径。
        /// </summary>
        /// <returns>正在加载场景的资源路径。</returns>
        public string[] GetAllLoadingSceneAssetPaths() => m_LoadingSceneDict.Keys.ToArray();

        /// <summary>
        /// 获取所有正在加载场景的资源路径。
        /// </summary>
        /// <param name="results">正在加载场景的资源路径。</param>
        public void GetAllLoadingSceneAssetPaths(List<string> results)
        {
            if (results == null) throw new FuException("结果参数列表为空!");
            results.Clear();
            results.AddRange(m_LoadingSceneDict.Keys);
        }

        /// <summary>
        /// 获取所有正在卸载场景的资源路径。
        /// </summary>
        /// <returns>正在卸载场景的资源路径。</returns>
        public string[] GetAllUnloadingSceneAssetPaths() => m_UnloadingSceneDict.Keys.ToArray();

        /// <summary>
        /// 获取所有正在卸载场景的资源路径。
        /// </summary>
        /// <param name="results">正在卸载场景的资源路径。</param>
        public void GetAllUnloadingSceneAssetPaths(List<string> results)
        {
            if (results == null) throw new FuException("结果参数列表为空!");
            results.Clear();
            results.AddRange(m_UnloadingSceneDict.Keys);
        }

        #endregion
        
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
            EventRegister.Fire(this, activeSceneChangedEventArgs);
        }
        
        /// <summary>
        /// 加载场景。
        /// </summary>
        /// <param name="sceneAssetPath">场景资源路径。</param>
        public UniTask<SceneHandle> LoadScene(string sceneAssetPath) => LoadScene(sceneAssetPath, LoadSceneMode.Single);

        /// <summary>
        /// 加载场景。
        /// </summary>
        /// <param name="sceneAssetPath">场景资源路径。</param>
        /// <param name="sceneMode">加载场景的方式。</param>
        public UniTask<SceneHandle> LoadScene(string sceneAssetPath, LoadSceneMode sceneMode) => LoadScene(sceneAssetPath, sceneMode, null);

        /// <summary>
        /// 加载场景。
        /// </summary>
        /// <param name="sceneAssetPath">场景资源路径。</param>
        /// <param name="userData">用户自定义数据。</param>
        public UniTask<SceneHandle> LoadScene(string sceneAssetPath, object userData) => LoadScene(sceneAssetPath, LoadSceneMode.Single, userData);

        /// <summary>
        /// 加载场景。
        /// </summary>
        /// <param name="sceneAssetPath">场景资源路径。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="sceneMode"></param>
        public async UniTask<SceneHandle> LoadScene(string sceneAssetPath, LoadSceneMode sceneMode, object userData)
        {
            if (string.IsNullOrEmpty(sceneAssetPath))
                throw new FuException("场景资源路径不能为空!.");

            if (!sceneAssetPath.StartsWith("Assets/", StringComparison.Ordinal) || !sceneAssetPath.EndsWith(".unity", StringComparison.Ordinal))
                throw new FuException(Utility.Text.Format("场景资源路径 '{0}' 格式错误!", sceneAssetPath));
            
            if (SceneIsUnloading(sceneAssetPath))
                throw new FuException(Utility.Text.Format("场景资源 '{0}' 正在卸载中!", sceneAssetPath));

            if (SceneIsLoading(sceneAssetPath))
                throw new FuException(Utility.Text.Format("场景资源 '{0}' 正在加载中!", sceneAssetPath));

            if (SceneIsLoaded(sceneAssetPath))
                throw new FuException(Utility.Text.Format("场景资源 '{0}' 已被加载过!", sceneAssetPath));

            var sceneOperationHandle = await AssetManager.Instance.LoadSceneAsync(sceneAssetPath, sceneMode);
            m_LoadingSceneDict.Add(sceneAssetPath, new SceneHandleData(sceneOperationHandle, userData));
            sceneOperationHandle.Completed += OnLoadSceneCompleted;
            return sceneOperationHandle;
        }

        /// <summary>
        /// 卸载场景。
        /// </summary>
        /// <param name="sceneAssetPath">场景资源路径。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void UnloadScene(string sceneAssetPath, object userData = null)
        {
            FuGuard.NotNull(sceneAssetPath, nameof(sceneAssetPath));

            if (SceneIsUnloading(sceneAssetPath))
                throw new FuException(Utility.Text.Format("卸载场景 '{0}' 失败, 场景正在卸载中!.", sceneAssetPath));

            if (SceneIsLoading(sceneAssetPath))
                throw new FuException(Utility.Text.Format("卸载场景 '{0}' 失败, 场景正在加载中!.", sceneAssetPath));

            if (!SceneIsLoaded(sceneAssetPath))
                throw new FuException(Utility.Text.Format("卸载场景 '{0}' 失败, 场景未加载!", sceneAssetPath));

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
                    m_UnloadingSceneDict.Remove(sceneAssetPath);
                    m_LoadedSceneDict.Remove(sceneAssetPath);
                    Log.Info("卸载场景 '{0}' 成功！", sceneAssetPath);
                    var unloadSceneSuccessEventArgs = UnloadSceneSuccessEventArgs.Create(sceneAssetPath, userData);
                    EventRegister.Fire(this, unloadSceneSuccessEventArgs);
                }
                else
                {
                    // 卸载失败
                    m_UnloadingSceneDict.Remove(sceneAssetPath);
                    Log.Warning("卸载场景 '{0} 失败！'.", sceneAssetPath);
                    var unloadSceneFailureEventArgs = UnloadSceneFailureEventArgs.Create(sceneAssetPath, userData);
                    EventRegister.Fire(this, unloadSceneFailureEventArgs);
                }
            }
        }


        /// <summary>
        /// 加载场景更新回调。
        /// </summary>
        /// <param name="sceneHandle"></param>
        private void OnLoadSceneUpdate(SceneHandle sceneHandle)
        {
            var assetPath = sceneHandle.GetAssetInfo().AssetPath;
            if (!m_LoadingSceneDict.TryGetValue(assetPath, out var value)) return;
            var loadSceneUpdateEventArgs = LoadSceneUpdateEventArgs.Create(sceneHandle.SceneName, sceneHandle.Progress, value.UserData);
            EventRegister.Fire(this, loadSceneUpdateEventArgs);
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
                var loadSceneSuccessEventArgs = LoadSceneSuccessEventArgs.Create(sceneHandle.SceneName, sceneHandleData.UserData);
                EventRegister.Fire(this, loadSceneSuccessEventArgs);
            }
            else
            {
                // 加载失败
                var errorMessage = Utility.Text.Format("加载场景 '{0}' 失败!, 加载状态 '{1}', 错误信息 '{2}'.", sceneHandle.SceneName, sceneHandle.Status, sceneHandle.LastError);
                Log.Error(errorMessage);
                var loadSceneFailureEventArgs = LoadSceneFailureEventArgs.Create(sceneHandle.SceneName, sceneHandle.Status, errorMessage, sceneHandleData.UserData);
                EventRegister.Fire(this, loadSceneFailureEventArgs);
            }
        }
    }
}