using System;
using YooAsset;
using UnityEngine;
using Cysharp.Threading.Tasks;
using FuFramework.Core.Runtime;
using FuFramework.Asset.Runtime;
using FuFramework.Event.Runtime;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace FuFramework.Scene.Runtime
{
    /// <summary>
    /// 场景组件。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/Scene")]
    public sealed class SceneComponent : FuComponent
    {
        private const int DefaultPriority = 0; // 场景模块优先级

        private IGameSceneManager m_gameSceneManager; // 游戏场景管理器
        private IAssetManager m_assetManager; // 资源管理器
        private EventComponent m_EventComponent; // 事件组件

        /// 记录场景加载顺序的字典。key为场景资源名称，value为加载顺序。
        private readonly SortedDictionary<string, int> m_SceneOrder = new(StringComparer.Ordinal);

        private UnityEngine.SceneManagement.Scene m_GameFrameworkScene; // 游戏框架场景

        [Header("启用加载场景更新事件")] [SerializeField]
        private bool m_EnableLoadSceneUpdateEvent = true;

        [Header("启用加载场景依赖资源事件")] [SerializeField]
        private bool m_EnableLoadSceneDependencyAssetEvent = true;

        /// <summary>
        /// 当前场景主摄像机。
        /// </summary>
        public Camera MainCamera { get; private set; }

        /// <summary>
        /// 游戏框架组件初始化。
        /// </summary>
        protected override void Awake()
        {
            ImplComponentType = Utility.Assembly.GetType(componentType);
            InterfaceComponentType = typeof(IGameSceneManager);

            base.Awake();

            m_gameSceneManager = FuEntry.GetModule<IGameSceneManager>();
            if (m_gameSceneManager == null)
            {
                Log.Fatal("Scene manager is invalid.");
                return;
            }

            m_gameSceneManager.LoadSceneSuccess += OnLoadGameSceneSuccess;
            m_gameSceneManager.LoadSceneFailure += OnLoadGameSceneFailure;

            if (m_EnableLoadSceneUpdateEvent) m_gameSceneManager.LoadSceneUpdate += OnLoadGameSceneUpdate;
            if (m_EnableLoadSceneDependencyAssetEvent)
            {
                // _gameSceneManager.LoadSceneDependencyAsset += OnLoadGameSceneDependencyAsset;
            }

            m_gameSceneManager.UnloadSceneSuccess += OnUnloadGameSceneSuccess;
            m_gameSceneManager.UnloadSceneFailure += OnUnloadGameSceneFailure;

            m_GameFrameworkScene = SceneManager.GetSceneAt(GameEntry.GameFrameworkSceneId);
            if (!m_GameFrameworkScene.IsValid()) Log.Fatal("Game Framework scene is invalid.");
        }

        private void Start()
        {
            var baseComp = GameEntry.GetComponent<BaseComponent>();
            if (baseComp == null)
            {
                Log.Fatal("Base component is invalid.");
                return;
            }

            m_EventComponent = GameEntry.GetComponent<EventComponent>();
            if (m_EventComponent == null)
            {
                Log.Fatal("Event component is invalid.");
                return;
            }

            m_assetManager = FuEntry.GetModule<IAssetManager>();
            if (m_assetManager == null)
            {
                Log.Fatal("Asset Manager is invalid.");
                return;
            }

            m_gameSceneManager.SetResourceManager(m_assetManager);
        }

        /// <summary>
        /// 获取场景名称。
        /// </summary>
        /// <param name="sceneAssetName">场景资源名称。</param>
        /// <returns>场景名称。</returns>
        public static string GetSceneName(string sceneAssetName)
        {
            if (string.IsNullOrEmpty(sceneAssetName))
            {
                Log.Error("Scene asset name is invalid.");
                return null;
            }

            int sceneNamePosition = sceneAssetName.LastIndexOf('/');
            if (sceneNamePosition + 1 >= sceneAssetName.Length)
            {
                Log.Error("Scene asset name '{0}' is invalid.", sceneAssetName);
                return null;
            }

            string sceneName = sceneAssetName.Substring(sceneNamePosition + 1);
            sceneNamePosition = sceneName.LastIndexOf(".unity", StringComparison.Ordinal);
            if (sceneNamePosition > 0)
            {
                sceneName = sceneName.Substring(0, sceneNamePosition);
            }

            return sceneName;
        }

        /// <summary>
        /// 获取场景是否已加载。
        /// </summary>
        /// <param name="sceneAssetName">场景资源名称。</param>
        /// <returns>场景是否已加载。</returns>
        public bool SceneIsLoaded(string sceneAssetName) => m_gameSceneManager.SceneIsLoaded(sceneAssetName);

        /// <summary>
        /// 获取已加载场景的资源名称。
        /// </summary>
        /// <returns>已加载场景的资源名称。</returns>
        public string[] GetLoadedSceneAssetNames() => m_gameSceneManager.GetLoadedSceneAssetNames();

        /// <summary>
        /// 获取已加载场景的资源名称。
        /// </summary>
        /// <param name="results">已加载场景的资源名称。</param>
        public void GetLoadedSceneAssetNames(List<string> results) => m_gameSceneManager.GetLoadedSceneAssetNames(results);

        /// <summary>
        /// 获取场景是否正在加载。
        /// </summary>
        /// <param name="sceneAssetName">场景资源名称。</param>
        /// <returns>场景是否正在加载。</returns>
        public bool SceneIsLoading(string sceneAssetName) => m_gameSceneManager.SceneIsLoading(sceneAssetName);

        /// <summary>
        /// 获取正在加载场景的资源名称。
        /// </summary>
        /// <returns>正在加载场景的资源名称。</returns>
        public string[] GetLoadingSceneAssetNames() => m_gameSceneManager.GetLoadingSceneAssetNames();

        /// <summary>
        /// 获取正在加载场景的资源名称。
        /// </summary>
        /// <param name="results">正在加载场景的资源名称。</param>
        public void GetLoadingSceneAssetNames(List<string> results) => m_gameSceneManager.GetLoadingSceneAssetNames(results);

        /// <summary>
        /// 获取场景是否正在卸载。
        /// </summary>
        /// <param name="sceneAssetName">场景资源名称。</param>
        /// <returns>场景是否正在卸载。</returns>
        public bool SceneIsUnloading(string sceneAssetName) => m_gameSceneManager.SceneIsUnloading(sceneAssetName);

        /// <summary>
        /// 获取正在卸载场景的资源名称。
        /// </summary>
        /// <returns>正在卸载场景的资源名称。</returns>
        public string[] GetUnloadingSceneAssetNames() => m_gameSceneManager.GetUnloadingSceneAssetNames();

        /// <summary>
        /// 获取正在卸载场景的资源名称。
        /// </summary>
        /// <param name="results">正在卸载场景的资源名称。</param>
        public void GetUnloadingSceneAssetNames(List<string> results) => m_gameSceneManager.GetUnloadingSceneAssetNames(results);

        /// <summary>
        /// 检查场景资源是否存在。
        /// </summary>
        /// <param name="sceneAssetName">要检查场景资源的名称。</param>
        /// <returns>场景资源是否存在。</returns>
        public bool HasScene(string sceneAssetName)
        {
            if (string.IsNullOrEmpty(sceneAssetName))
            {
                Log.Error("Scene asset name is invalid.");
                return false;
            }

            if (!sceneAssetName.StartsWith("Assets/", StringComparison.Ordinal) ||
                !sceneAssetName.EndsWith(".unity", StringComparison.Ordinal))
            {
                Log.Error("Scene asset name '{0}' is invalid.", sceneAssetName);
                return false;
            }

            return m_gameSceneManager.HasScene(sceneAssetName);
        }

        /// <summary>
        /// 加载场景。
        /// </summary>
        /// <param name="sceneAssetName">场景资源名称。</param>
        public UniTask<SceneHandle> LoadScene(string sceneAssetName) => LoadScene(sceneAssetName, LoadSceneMode.Additive, null);

        /// <summary>
        /// 加载场景。
        /// </summary>
        /// <param name="sceneAssetName">场景资源名称。</param>
        /// <param name="sceneMode">加载场景资源的优先级。</param>
        /// <param name="userData">用户自定义数据。</param>
        public UniTask<SceneHandle> LoadScene(string sceneAssetName, LoadSceneMode sceneMode, object userData = null)
        {
            if (string.IsNullOrEmpty(sceneAssetName))
            {
                Log.Error("Scene asset name is invalid.");
                throw new ArgumentNullException(nameof(sceneAssetName));
            }

            if (!sceneAssetName.StartsWith("Assets/", StringComparison.Ordinal) ||
                !sceneAssetName.EndsWith(".unity", StringComparison.Ordinal))
            {
                Log.Error("Scene asset name '{0}' is invalid.", sceneAssetName);
                throw new ArgumentException(nameof(sceneAssetName));
            }

            return m_gameSceneManager.LoadScene(sceneAssetName, sceneMode, userData);
        }

        /// <summary>
        /// 卸载场景。
        /// </summary>
        /// <param name="sceneAssetName">场景资源名称。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void UnloadScene(string sceneAssetName, object userData = null)
        {
            if (string.IsNullOrEmpty(sceneAssetName))
            {
                Log.Error("Scene asset name is invalid.");
                return;
            }

            if (!sceneAssetName.StartsWith("Assets/", StringComparison.Ordinal) ||
                !sceneAssetName.EndsWith(".unity", StringComparison.Ordinal))
            {
                Log.Error("Scene asset name '{0}' is invalid.", sceneAssetName);
                return;
            }

            m_gameSceneManager.UnloadScene(sceneAssetName, userData);
            m_SceneOrder.Remove(sceneAssetName);
        }

        /// <summary>
        /// 设置场景顺序。
        /// </summary>
        /// <param name="sceneAssetName">场景资源名称。</param>
        /// <param name="sceneOrder">要设置的场景顺序。</param>
        public void SetSceneOrder(string sceneAssetName, int sceneOrder)
        {
            if (string.IsNullOrEmpty(sceneAssetName))
            {
                Log.Error("Scene asset name is invalid.");
                return;
            }

            if (!sceneAssetName.StartsWith("Assets/", StringComparison.Ordinal) ||
                !sceneAssetName.EndsWith(".unity", StringComparison.Ordinal))
            {
                Log.Error("Scene asset name '{0}' is invalid.", sceneAssetName);
                return;
            }

            if (SceneIsLoading(sceneAssetName))
            {
                m_SceneOrder[sceneAssetName] = sceneOrder;
                return;
            }

            if (SceneIsLoaded(sceneAssetName))
            {
                m_SceneOrder[sceneAssetName] = sceneOrder;
                RefreshSceneOrder();
                return;
            }

            Log.Error("Scene '{0}' is not loaded or loading.", sceneAssetName);
        }

        /// <summary>
        /// 刷新当前场景主摄像机。
        /// </summary>
        public void RefreshMainCamera() => MainCamera = Camera.main;

        /// <summary>
        /// 刷新场景顺序。
        /// </summary>
        private void RefreshSceneOrder()
        {
            if (m_SceneOrder.Count > 0)
            {
                string maxSceneName = null;
                var maxSceneOrder = 0;

                foreach (var sceneOrder in m_SceneOrder)
                {
                    if (SceneIsLoading(sceneOrder.Key)) continue;

                    if (maxSceneName == null)
                    {
                        maxSceneName = sceneOrder.Key;
                        maxSceneOrder = sceneOrder.Value;
                        continue;
                    }

                    if (sceneOrder.Value > maxSceneOrder)
                    {
                        maxSceneName = sceneOrder.Key;
                        maxSceneOrder = sceneOrder.Value;
                    }
                }

                if (maxSceneName == null)
                {
                    SetActiveScene(m_GameFrameworkScene);
                    return;
                }

                var scene = SceneManager.GetSceneByName(GetSceneName(maxSceneName));
                if (!scene.IsValid())
                {
                    Log.Error("Active scene '{0}' is invalid.", maxSceneName);
                    return;
                }

                SetActiveScene(scene);
            }
            else
            {
                SetActiveScene(m_GameFrameworkScene);
            }
        }

        /// <summary>
        /// 设置活动场景。
        /// </summary>
        /// <param name="activeScene"></param>
        private void SetActiveScene(UnityEngine.SceneManagement.Scene activeScene)
        {
            var lastActiveScene = SceneManager.GetActiveScene();
            if (lastActiveScene != activeScene)
            {
                SceneManager.SetActiveScene(activeScene);
                m_EventComponent.Fire(this, ActiveSceneChangedEventArgs.Create(lastActiveScene, activeScene));
            }

            RefreshMainCamera();
        }

        /// <summary>
        /// 加载场景成功事件。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void OnLoadGameSceneSuccess(object sender, LoadSceneSuccessEventArgs eventArgs)
        {
            m_SceneOrder.TryAdd(eventArgs.SceneAssetName, 0);
            m_EventComponent.Fire(this, eventArgs);
            RefreshSceneOrder();
        }

        /// <summary>
        /// 加载场景失败事件。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void OnLoadGameSceneFailure(object sender, LoadSceneFailureEventArgs eventArgs)
        {
            Log.Warning("Load scene failure, scene asset name '{0}', error message '{1}'.", eventArgs.SceneAssetName, eventArgs.ErrorMessage);
            m_EventComponent.Fire(this, eventArgs);
        }

        /// <summary>
        /// 加载场景更新事件。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void OnLoadGameSceneUpdate(object sender, LoadSceneUpdateEventArgs eventArgs)
        {
            m_EventComponent.Fire(this, eventArgs);
        }

        /// <summary>
        /// 卸载场景成功事件。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void OnUnloadGameSceneSuccess(object sender, UnloadSceneSuccessEventArgs eventArgs)
        {
            m_EventComponent.Fire(this, eventArgs);
            m_SceneOrder.Remove(eventArgs.SceneAssetName);
            RefreshSceneOrder();
        }

        /// <summary>
        /// 卸载场景失败事件。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void OnUnloadGameSceneFailure(object sender, UnloadSceneFailureEventArgs eventArgs)
        {
            Log.Warning("Unload scene failure, scene asset name '{0}'.", eventArgs.SceneAssetName);
            m_EventComponent.Fire(this, eventArgs);
        }
    }
}