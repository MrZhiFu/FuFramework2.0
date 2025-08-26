using System;
using YooAsset;
using System.Linq;
using Cysharp.Threading.Tasks;
using FuFramework.Core.Runtime;
using FuFramework.Asset.Runtime;
using System.Collections.Generic;
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
    public sealed class GameSceneManager : FuModule, IGameSceneManager
    {
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

        private readonly Dictionary<string, SceneHandle> m_LoadedSceneAssetNames; // 已加载的场景字典，Key为场景资源名称，Value为场景加载句柄
        private readonly Dictionary<string, SceneHandleData> m_LoadingSceneAssetNames; // 正在加载的场景字典，Key为场景资源名称，Value为场景加载句柄数据
        private readonly Dictionary<string, SceneHandle> m_UnloadingSceneAssetNames; // 正在卸载的场景字典，Key为场景资源名称，Value为场景加载句柄

        private EventHandler<LoadSceneSuccessEventArgs> m_LoadSceneSuccessEventHandler; // 加载场景成功事件
        private EventHandler<LoadSceneFailureEventArgs> m_LoadSceneFailureEventHandler; // 加载场景失败事件
        private EventHandler<LoadSceneUpdateEventArgs> m_LoadSceneUpdateEventHandler; // 加载场景更新事件
        private EventHandler<UnloadSceneSuccessEventArgs> m_UnloadSceneSuccessEventHandler; // 卸载场景成功事件
        private EventHandler<UnloadSceneFailureEventArgs> m_UnloadSceneFailureEventHandler; // 卸载场景失败事件

        /// <summary>
        /// 初始化场景管理器的新实例。
        /// </summary>
        public GameSceneManager()
        {
            m_LoadedSceneAssetNames = new Dictionary<string, SceneHandle>();
            m_LoadingSceneAssetNames = new Dictionary<string, SceneHandleData>();
            m_UnloadingSceneAssetNames = new Dictionary<string, SceneHandle>();
            m_LoadSceneSuccessEventHandler = null;
            m_LoadSceneFailureEventHandler = null;
            m_LoadSceneUpdateEventHandler = null;
            m_UnloadSceneSuccessEventHandler = null;
            m_UnloadSceneFailureEventHandler = null;
        }

        /// <summary>
        /// 获取游戏框架模块优先级。
        /// </summary>
        /// <remarks>优先级较高的模块会优先轮询，并且关闭操作会后进行。</remarks>
        protected override int Priority => 2;

        /// <summary>
        /// 加载场景成功事件。
        /// </summary>
        public event EventHandler<LoadSceneSuccessEventArgs> LoadSceneSuccess
        {
            add => m_LoadSceneSuccessEventHandler += value;
            remove => m_LoadSceneSuccessEventHandler -= value;
        }

        /// <summary>
        /// 加载场景失败事件。
        /// </summary>
        public event EventHandler<LoadSceneFailureEventArgs> LoadSceneFailure
        {
            add => m_LoadSceneFailureEventHandler += value;
            remove => m_LoadSceneFailureEventHandler -= value;
        }

        /// <summary>
        /// 加载场景更新事件。
        /// </summary>
        public event EventHandler<LoadSceneUpdateEventArgs> LoadSceneUpdate
        {
            add => m_LoadSceneUpdateEventHandler += value;
            remove => m_LoadSceneUpdateEventHandler -= value;
        }

        /// <summary>
        /// 卸载场景成功事件。
        /// </summary>
        public event EventHandler<UnloadSceneSuccessEventArgs> UnloadSceneSuccess
        {
            add => m_UnloadSceneSuccessEventHandler += value;
            remove => m_UnloadSceneSuccessEventHandler -= value;
        }

        /// <summary>
        /// 卸载场景失败事件。
        /// </summary>
        public event EventHandler<UnloadSceneFailureEventArgs> UnloadSceneFailure
        {
            add => m_UnloadSceneFailureEventHandler += value;
            remove => m_UnloadSceneFailureEventHandler -= value;
        }

        /// <summary>
        /// 场景管理器轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        protected override void Update(float elapseSeconds, float realElapseSeconds)
        {
            foreach (var (_, sceneHandleData) in m_LoadingSceneAssetNames)
            {
                OnLoadSceneUpdate(sceneHandleData.SceneHandle);
            }
        }

        /// <summary>
        /// 关闭并清理场景管理器。
        /// </summary>
        protected override void Shutdown()
        {
            var loadedSceneAssetNames = m_LoadedSceneAssetNames.Keys.ToArray();
            foreach (var loadedSceneAssetName in loadedSceneAssetNames)
            {
                if (SceneIsUnloading(loadedSceneAssetName)) continue;
                UnloadScene(loadedSceneAssetName);
            }

            m_LoadedSceneAssetNames.Clear();
            m_LoadingSceneAssetNames.Clear();
            m_UnloadingSceneAssetNames.Clear();
        }

        /// <summary>
        /// 获取场景是否已加载。
        /// </summary>
        /// <param name="sceneAssetName">场景资源名称。</param>
        /// <returns>场景是否已加载。</returns>
        public bool SceneIsLoaded(string sceneAssetName)
        {
            if (string.IsNullOrEmpty(sceneAssetName)) throw new FuException("Scene asset name is invalid.");
            return m_LoadedSceneAssetNames.ContainsKey(sceneAssetName);
        }

        /// <summary>
        /// 获取已加载场景的资源名称。
        /// </summary>
        /// <returns>已加载场景的资源名称。</returns>
        public string[] GetLoadedSceneAssetNames() => m_LoadedSceneAssetNames.Keys.ToArray();

        /// <summary>
        /// 获取已加载场景的资源名称。
        /// </summary>
        /// <param name="results">已加载场景的资源名称。</param>
        public void GetLoadedSceneAssetNames(List<string> results)
        {
            if (results == null) throw new FuException("Results is invalid.");
            results.Clear();
            results.AddRange(m_LoadedSceneAssetNames.Keys);
        }

        /// <summary>
        /// 获取场景是否正在加载。
        /// </summary>
        /// <param name="sceneAssetName">场景资源名称。</param>
        /// <returns>场景是否正在加载。</returns>
        public bool SceneIsLoading(string sceneAssetName)
        {
            if (string.IsNullOrEmpty(sceneAssetName)) throw new FuException("Scene asset name is invalid.");
            return m_LoadingSceneAssetNames.ContainsKey(sceneAssetName);
        }

        /// <summary>
        /// 获取正在加载场景的资源名称。
        /// </summary>
        /// <returns>正在加载场景的资源名称。</returns>
        public string[] GetLoadingSceneAssetNames() => m_LoadingSceneAssetNames.Keys.ToArray();

        /// <summary>
        /// 获取正在加载场景的资源名称。
        /// </summary>
        /// <param name="results">正在加载场景的资源名称。</param>
        public void GetLoadingSceneAssetNames(List<string> results)
        {
            if (results == null) throw new FuException("Results is invalid.");
            results.Clear();
            results.AddRange(m_LoadingSceneAssetNames.Keys);
        }

        /// <summary>
        /// 获取场景是否正在卸载。
        /// </summary>
        /// <param name="sceneAssetName">场景资源名称。</param>
        /// <returns>场景是否正在卸载。</returns>
        public bool SceneIsUnloading(string sceneAssetName)
        {
            if (string.IsNullOrEmpty(sceneAssetName)) throw new FuException("Scene asset name is invalid.");
            return m_UnloadingSceneAssetNames.ContainsKey(sceneAssetName);
        }

        /// <summary>
        /// 获取正在卸载场景的资源名称。
        /// </summary>
        /// <returns>正在卸载场景的资源名称。</returns>
        public string[] GetUnloadingSceneAssetNames() => m_UnloadingSceneAssetNames.Keys.ToArray();

        /// <summary>
        /// 获取正在卸载场景的资源名称。
        /// </summary>
        /// <param name="results">正在卸载场景的资源名称。</param>
        public void GetUnloadingSceneAssetNames(List<string> results)
        {
            if (results == null) throw new FuException("Results is invalid.");
            results.Clear();
            results.AddRange(m_UnloadingSceneAssetNames.Keys);
        }

        /// <summary>
        /// 检查场景资源是否存在。
        /// </summary>
        /// <param name="sceneAssetName">要检查场景资源的名称。</param>
        /// <returns>场景资源是否存在。</returns>
        public bool HasScene(string sceneAssetName)
        {
            return AssetManager.Instance.LoadSceneAsync(sceneAssetName, LoadSceneMode.Single).Status != UniTaskStatus.Faulted;
        }

        /// <summary>
        /// 加载场景。
        /// </summary>
        /// <param name="sceneAssetName">场景资源名称。</param>
        public UniTask<SceneHandle> LoadScene(string sceneAssetName) => LoadScene(sceneAssetName, LoadSceneMode.Single);

        /// <summary>
        /// 加载场景。
        /// </summary>
        /// <param name="sceneAssetName">场景资源名称。</param>
        /// <param name="sceneMode">加载场景的方式。</param>
        public UniTask<SceneHandle> LoadScene(string sceneAssetName, LoadSceneMode sceneMode) => LoadScene(sceneAssetName, sceneMode, null);

        /// <summary>
        /// 加载场景。
        /// </summary>
        /// <param name="sceneAssetName">场景资源名称。</param>
        /// <param name="userData">用户自定义数据。</param>
        public UniTask<SceneHandle> LoadScene(string sceneAssetName, object userData) => LoadScene(sceneAssetName, LoadSceneMode.Single, userData);

        /// <summary>
        /// 加载场景。
        /// </summary>
        /// <param name="sceneAssetName">场景资源名称。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="sceneMode"></param>
        public async UniTask<SceneHandle> LoadScene(string sceneAssetName, LoadSceneMode sceneMode, object userData)
        {
            if (string.IsNullOrEmpty(sceneAssetName))
                throw new FuException("Scene asset name is invalid.");

            if (SceneIsUnloading(sceneAssetName))
                throw new FuException(Utility.Text.Format("Scene asset '{0}' is being unloaded.", sceneAssetName));

            if (SceneIsLoading(sceneAssetName))
                throw new FuException(Utility.Text.Format("Scene asset '{0}' is being loaded.", sceneAssetName));

            if (SceneIsLoaded(sceneAssetName))
                throw new FuException(Utility.Text.Format("Scene asset '{0}' is already loaded.", sceneAssetName));

            var sceneOperationHandle = await AssetManager.Instance.LoadSceneAsync(sceneAssetName, sceneMode);
            m_LoadingSceneAssetNames.Add(sceneAssetName, new SceneHandleData(sceneOperationHandle, userData));
            sceneOperationHandle.Completed += OnLoadSceneCompleted;
            return sceneOperationHandle;
        }

        /// <summary>
        /// 加载场景更新回调。
        /// </summary>
        /// <param name="sceneHandle"></param>
        private void OnLoadSceneUpdate(SceneHandle sceneHandle)
        {
            var assetPath = sceneHandle.GetAssetInfo().AssetPath;
            if (!m_LoadingSceneAssetNames.TryGetValue(assetPath, out var value)) return;
            LoadSceneUpdateCallback(sceneHandle.SceneName, sceneHandle.Progress, value.UserData);
        }

        /// <summary>
        /// 加载场景完成回调。
        /// </summary>
        /// <param name="sceneHandle"></param>
        private void OnLoadSceneCompleted(SceneHandle sceneHandle)
        {
            m_LoadedSceneAssetNames.Add(sceneHandle.GetAssetInfo().AssetPath, sceneHandle);
            m_LoadingSceneAssetNames.Remove(sceneHandle.GetAssetInfo().AssetPath, out var value);

            if (value == null) return;
            if (sceneHandle.IsDone)
                LoadSceneSuccessCallback(sceneHandle.SceneName, sceneHandle.Progress, value.UserData);
            else
                LoadSceneFailureCallback(sceneHandle.SceneName, sceneHandle.Status, sceneHandle.LastError, value.UserData);
        }

        /// <summary>
        /// 卸载场景。
        /// </summary>
        /// <param name="sceneAssetName">场景资源名称。</param>
        public void UnloadScene(string sceneAssetName) => UnloadScene(sceneAssetName, null);

        /// <summary>
        /// 卸载场景。
        /// </summary>
        /// <param name="sceneAssetName">场景资源名称。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void UnloadScene(string sceneAssetName, object userData)
        {
            if (string.IsNullOrEmpty(sceneAssetName))
                throw new FuException("Scene asset name is invalid.");

            if (SceneIsUnloading(sceneAssetName))
                throw new FuException(Utility.Text.Format("Scene asset '{0}' is being unloaded.", sceneAssetName));

            if (SceneIsLoading(sceneAssetName))
                throw new FuException(Utility.Text.Format("Scene asset '{0}' is being loaded.", sceneAssetName));

            if (!SceneIsLoaded(sceneAssetName))
                throw new FuException(Utility.Text.Format("Scene asset '{0}' is not loaded yet.", sceneAssetName));

            if (!m_LoadedSceneAssetNames.TryGetValue(sceneAssetName, out var sceneOperationHandle)) return;

            var unloadSceneOperationHandle = sceneOperationHandle.UnloadAsync();
            m_LoadedSceneAssetNames.Remove(sceneAssetName);
            m_UnloadingSceneAssetNames.Add(sceneAssetName, sceneOperationHandle);

            unloadSceneOperationHandle.Completed += OnUnloadSceneOperationHandleOnCompleted;
            return;

            // 卸载场景资源完成回调
            void OnUnloadSceneOperationHandleOnCompleted(AsyncOperationBase asyncOperationBase)
            {
                if (asyncOperationBase.Error.IsNullOrEmpty())
                    UnloadSceneSuccessCallback(sceneAssetName, userData);
                else
                    UnloadSceneFailureCallback(sceneAssetName, userData);
            }
        }

        /// <summary>
        /// 加载场景成功回调。
        /// </summary>
        /// <param name="sceneAssetName"></param>
        /// <param name="duration"></param>
        /// <param name="userData"></param>
        private void LoadSceneSuccessCallback(string sceneAssetName, float duration, object userData)
        {
            m_LoadingSceneAssetNames.Remove(sceneAssetName);
            // m_LoadedSceneAssetNames.Add(sceneAssetName);
            if (m_LoadSceneSuccessEventHandler != null)
            {
                var loadSceneSuccessEventArgs = LoadSceneSuccessEventArgs.Create(sceneAssetName, duration, userData);
                m_LoadSceneSuccessEventHandler(this, loadSceneSuccessEventArgs);
                // ReferencePool.Release(loadSceneSuccessEventArgs);
            }
        }

        /// <summary>
        /// 加载场景失败回调。
        /// </summary>
        /// <param name="sceneAssetName"></param>
        /// <param name="status"></param>
        /// <param name="errorMessage"></param>
        /// <param name="userData"></param>
        /// <exception cref="FuException"></exception>
        private void LoadSceneFailureCallback(string sceneAssetName, EOperationStatus status, string errorMessage, object userData)
        {
            m_LoadingSceneAssetNames.Remove(sceneAssetName);
            var appendErrorMessage = Utility.Text.Format("Load scene failure, scene asset name '{0}', status '{1}', error message '{2}'.",
                sceneAssetName, status, errorMessage);
            if (m_LoadSceneFailureEventHandler == null) throw new FuException(appendErrorMessage);

            var loadSceneFailureEventArgs = LoadSceneFailureEventArgs.Create(sceneAssetName, status, appendErrorMessage, userData);
            m_LoadSceneFailureEventHandler(this, loadSceneFailureEventArgs);
            // ReferencePool.Release(loadSceneFailureEventArgs);
        }

        /// <summary>
        /// 加载场景更新回调。
        /// </summary>
        /// <param name="sceneAssetName"></param>
        /// <param name="progress"></param>
        /// <param name="userData"></param>
        private void LoadSceneUpdateCallback(string sceneAssetName, float progress, object userData)
        {
            if (m_LoadSceneUpdateEventHandler == null) return;
            var loadSceneUpdateEventArgs = LoadSceneUpdateEventArgs.Create(sceneAssetName, progress, userData);
            m_LoadSceneUpdateEventHandler(this, loadSceneUpdateEventArgs);
            // ReferencePool.Release(loadSceneUpdateEventArgs);
        }

        /// <summary>
        /// 卸载场景成功回调。
        /// </summary>
        /// <param name="sceneAssetName"></param>
        /// <param name="userData"></param>
        private void UnloadSceneSuccessCallback(string sceneAssetName, object userData)
        {
            m_UnloadingSceneAssetNames.Remove(sceneAssetName);
            m_LoadedSceneAssetNames.Remove(sceneAssetName);
            if (m_UnloadSceneSuccessEventHandler == null) return;
            var unloadSceneSuccessEventArgs = UnloadSceneSuccessEventArgs.Create(sceneAssetName, userData);
            m_UnloadSceneSuccessEventHandler(this, unloadSceneSuccessEventArgs);
            // ReferencePool.Release(unloadSceneSuccessEventArgs);
        }

        private void UnloadSceneFailureCallback(string sceneAssetName, object userData)
        {
            m_UnloadingSceneAssetNames.Remove(sceneAssetName);
            if (m_UnloadSceneFailureEventHandler == null)
                throw new FuException(Utility.Text.Format("Unload scene failure, scene asset name '{0}'.", sceneAssetName));

            var unloadSceneFailureEventArgs = UnloadSceneFailureEventArgs.Create(sceneAssetName, userData);
            m_UnloadSceneFailureEventHandler(this, unloadSceneFailureEventArgs);
            // ReferencePool.Release(unloadSceneFailureEventArgs);
        }
    }
}