# FuFramework Scene Module

## 概述

Scene 模块是 FuFramework 中的场景管理系统，专门用于管理 Unity 场景的加载、卸载和状态跟踪。该模块基于 YooAsset 资源管理系统，提供异步场景加载、进度跟踪、事件通知等高级功能，是游戏场景管理的核心组件。

### 核心特性

- **异步场景加载**：基于 UniTask 的异步场景加载，支持进度跟踪
- **事件驱动架构**：完整的场景生命周期事件通知机制
- **资源管理集成**：与 YooAsset 资源管理系统深度集成
- **状态跟踪**：实时跟踪场景的加载、卸载、使用状态
- **错误处理**：完善的错误处理和异常捕获机制

## 系统架构

### 核心类说明

#### 1. GameSceneManager
场景管理器，继承自 FuModule，负责整个场景系统的生命周期管理。

**主要职责：**
- 管理场景资源的加载、卸载
- 提供加载、卸载场景的接口
- 发布场景加载进度、成功、失败等事件
- 跟踪场景状态（已加载、加载中、卸载中）

#### 2. 场景事件类
一系列事件参数类，用于场景生命周期的事件通知：

- **LoadSceneSuccessEventArgs**：加载场景成功事件
- **LoadSceneFailureEventArgs**：加载场景失败事件
- **LoadSceneUpdateEventArgs**：加载场景更新事件（进度）
- **UnloadSceneSuccessEventArgs**：卸载场景成功事件
- **UnloadSceneFailureEventArgs**：卸载场景失败事件
- **ActiveSceneChangedEventArgs**：活动场景改变事件

### 技术架构

```
GameSceneManager (管理器)
    ↓
SceneHandle (场景句柄)
    ↓
事件系统 (EventManager)
    ↓
资源系统 (AssetManager)
```

## 快速开始

### 1. 基本场景加载示例

```csharp
using FuFramework.Scene.Runtime;
using Cysharp.Threading.Tasks;

public class SceneLoader : MonoBehaviour
{
    private async void Start()
    {
        // 获取场景管理器
        var sceneManager = GameSceneManager.Instance;
        
        // 异步加载场景
        var sceneHandle = await sceneManager.LoadSceneByName("MainScene", LoadSceneMode.Single);
        
        if (sceneHandle.IsDone)
        {
            Debug.Log("场景加载完成");
        }
    }
}
```

### 2. 事件监听示例

```csharp
using FuFramework.Scene.Runtime;
using FuFramework.Event.Runtime;

public class SceneEventListener : MonoBehaviour
{
    private void Start()
    {
        // 注册场景加载成功事件
        EventManager.Instance.Subscribe<LoadSceneSuccessEventArgs>(OnSceneLoadSuccess);
        
        // 注册场景加载进度事件
        EventManager.Instance.Subscribe<LoadSceneUpdateEventArgs>(OnSceneLoadProgress);
        
        // 注册场景加载失败事件
        EventManager.Instance.Subscribe<LoadSceneFailureEventArgs>(OnSceneLoadFailed);
    }
    
    private void OnSceneLoadSuccess(object sender, LoadSceneSuccessEventArgs e)
    {
        Debug.Log($"场景 {e.SceneName} 加载成功");
    }
    
    private void OnSceneLoadProgress(object sender, LoadSceneUpdateEventArgs e)
    {
        Debug.Log($"场景 {e.SceneName} 加载进度: {e.Progress:P2}");
    }
    
    private void OnSceneLoadFailed(object sender, LoadSceneFailureEventArgs e)
    {
        Debug.LogError($"场景 {e.SceneName} 加载失败: {e.ErrorMessage}");
    }
    
    private void OnDestroy()
    {
        // 注销事件监听
        EventManager.Instance.Unsubscribe<LoadSceneSuccessEventArgs>(OnSceneLoadSuccess);
        EventManager.Instance.Unsubscribe<LoadSceneUpdateEventArgs>(OnSceneLoadProgress);
        EventManager.Instance.Unsubscribe<LoadSceneFailureEventArgs>(OnSceneLoadFailed);
    }
}
```

## 详细使用指南

### 1. 场景管理流程示例

#### 完整的场景切换流程

```csharp
using FuFramework.Scene.Runtime;
using Cysharp.Threading.Tasks;
using FuFramework.Event.Runtime;

public class GameSceneManager : MonoBehaviour
{
    private const string MAIN_MENU_SCENE = "MainMenu";
    private const string GAME_SCENE = "GameScene";
    private const string LOADING_SCENE = "LoadingScene";
    
    private async void Start()
    {
        // 启动游戏，加载主菜单场景
        await SwitchToMainMenu();
    }
    
    // 切换到主菜单
    public async UniTask SwitchToMainMenu()
    {
        // 卸载当前游戏场景（如果有）
        await UnloadCurrentScene();
        
        // 加载主菜单场景
        await GameSceneManager.Instance.LoadSceneByName(MAIN_MENU_SCENE, LoadSceneMode.Single);
    }
    
    // 开始游戏
    public async UniTask StartGame()
    {
        // 显示加载界面
        ShowLoadingScreen();
        
        // 卸载主菜单场景
        await UnloadScene(MAIN_MENU_SCENE);
        
        // 加载游戏场景
        await LoadGameScene();
        
        // 隐藏加载界面
        HideLoadingScreen();
    }
    
    // 加载游戏场景（带进度显示）
    private async UniTask LoadGameScene()
    {
        var sceneManager = GameSceneManager.Instance;
        
        // 注册进度事件
        EventManager.Instance.Subscribe<LoadSceneUpdateEventArgs>(OnGameSceneLoadProgress);
        
        try
        {
            // 异步加载游戏场景
            var sceneHandle = await sceneManager.LoadSceneByName(GAME_SCENE, LoadSceneMode.Single);
            
            if (!sceneHandle.IsDone)
            {
                Debug.LogError("游戏场景加载失败");
                return;
            }
            
            Debug.Log("游戏场景加载完成");
        }
        finally
        {
            // 注销进度事件
            EventManager.Instance.Unsubscribe<LoadSceneUpdateEventArgs>(OnGameSceneLoadProgress);
        }
    }
    
    private void OnGameSceneLoadProgress(object sender, LoadSceneUpdateEventArgs e)
    {
        // 更新加载界面进度
        UpdateLoadingProgress(e.Progress);
    }
    
    // 卸载指定场景
    private async UniTask UnloadScene(string sceneName)
    {
        var sceneManager = GameSceneManager.Instance;
        
        if (sceneManager.SceneIsLoaded(sceneName))
        {
            sceneManager.UnloadScene(sceneName);
            
            // 等待场景卸载完成
            await UniTask.WaitUntil(() => !sceneManager.SceneIsLoaded(sceneName));
        }
    }
    
    // 卸载当前场景
    private async UniTask UnloadCurrentScene()
    {
        var sceneManager = GameSceneManager.Instance;
        var loadedScenes = sceneManager.GetAllLoadedSceneAssetPaths();
        
        foreach (var scenePath in loadedScenes)
        {
            var sceneName = sceneManager.GetSceneName(scenePath);
            if (sceneName != MAIN_MENU_SCENE && sceneName != GAME_SCENE)
            {
                await UnloadScene(sceneName);
            }
        }
    }
    
    private void ShowLoadingScreen()
    {
        // 显示加载界面逻辑
    }
    
    private void HideLoadingScreen()
    {
        // 隐藏加载界面逻辑
    }
    
    private void UpdateLoadingProgress(float progress)
    {
        // 更新加载进度显示逻辑
    }
}
```

#### 多场景叠加管理

```csharp
using FuFramework.Scene.Runtime;
using Cysharp.Threading.Tasks;

public class MultiSceneManager : MonoBehaviour
{
    private const string BASE_SCENE = "BaseScene";
    private const string UI_SCENE = "UIScene";
    private const string LEVEL_1_SCENE = "Level1";
    private const string LEVEL_2_SCENE = "Level2";
    
    private async void Start()
    {
        // 加载基础场景（单例模式）
        await GameSceneManager.Instance.LoadSceneByName(BASE_SCENE, LoadSceneMode.Single);
        
        // 叠加加载 UI 场景
        await GameSceneManager.Instance.LoadSceneByName(UI_SCENE, LoadSceneMode.Additive);
        
        // 加载第一关场景
        await LoadLevel(LEVEL_1_SCENE);
    }
    
    // 切换关卡
    public async UniTask SwitchLevel(string newLevel)
    {
        var sceneManager = GameSceneManager.Instance;
        
        // 卸载当前关卡场景（如果有）
        if (sceneManager.SceneIsLoaded(LEVEL_1_SCENE))
        {
            sceneManager.UnloadScene(LEVEL_1_SCENE);
        }
        if (sceneManager.SceneIsLoaded(LEVEL_2_SCENE))
        {
            sceneManager.UnloadScene(LEVEL_2_SCENE);
        }
        
        // 加载新关卡
        await sceneManager.LoadSceneByName(newLevel, LoadSceneMode.Additive);
    }
    
    // 加载关卡（带错误处理）
    private async UniTask LoadLevel(string levelName)
    {
        var sceneManager = GameSceneManager.Instance;
        
        try
        {
            // 检查场景是否存在
            if (!sceneManager.HasScene(levelName))
            {
                Debug.LogError($"场景 {levelName} 不存在");
                return;
            }
            
            // 检查场景是否正在加载或卸载
            if (sceneManager.SceneIsLoading(levelName) || sceneManager.SceneIsUnloading(levelName))
            {
                Debug.LogWarning($"场景 {levelName} 正在操作中，请稍后重试");
                return;
            }
            
            // 加载场景
            var sceneHandle = await sceneManager.LoadSceneByName(levelName, LoadSceneMode.Additive);
            
            if (sceneHandle.IsDone)
            {
                Debug.Log($"关卡 {levelName} 加载完成");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"加载关卡 {levelName} 失败: {ex.Message}");
        }
    }
    
    // 获取当前加载的所有场景信息
    private void LogSceneInfo()
    {
        var sceneManager = GameSceneManager.Instance;
        
        var loadedScenes = sceneManager.GetAllLoadedSceneAssetPaths();
        var loadingScenes = sceneManager.GetAllLoadingSceneAssetPaths();
        var unloadingScenes = sceneManager.GetAllUnloadingSceneAssetPaths();
        
        Debug.Log($"已加载场景: {string.Join(", ", loadedScenes)}");
        Debug.Log($"正在加载场景: {string.Join(", ", loadingScenes)}");
        Debug.Log($"正在卸载场景: {string.Join(", ", unloadingScenes)}");
    }
}
```

### 2. 高级场景管理功能

#### 场景预加载系统

```csharp
using FuFramework.Scene.Runtime;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

public class ScenePreloader : MonoBehaviour
{
    private readonly Dictionary<string, SceneHandle> m_PreloadedScenes = new();
    
    // 预加载常用场景
    public async UniTask PreloadCommonScenes()
    {
        var scenesToPreload = new[]
        {
            "BattleScene",
            "ShopScene", 
            "InventoryScene",
            "SettingsScene"
        };
        
        var preloadTasks = new List<UniTask>();
        
        foreach (var sceneName in scenesToPreload)
        {
            preloadTasks.Add(PreloadScene(sceneName));
        }
        
        // 并行预加载所有场景
        await UniTask.WhenAll(preloadTasks);
        
        Debug.Log("常用场景预加载完成");
    }
    
    // 预加载单个场景
    private async UniTask PreloadScene(string sceneName)
    {
        var sceneManager = GameSceneManager.Instance;
        
        if (m_PreloadedScenes.ContainsKey(sceneName))
        {
            Debug.LogWarning($"场景 {sceneName} 已经预加载");
            return;
        }
        
        try
        {
            // 异步加载场景但不激活
            var sceneHandle = await sceneManager.LoadSceneByName(sceneName, LoadSceneMode.Additive);
            
            if (sceneHandle.IsDone)
            {
                m_PreloadedScenes[sceneName] = sceneHandle;
                Debug.Log($"场景 {sceneName} 预加载完成");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"预加载场景 {sceneName} 失败: {ex.Message}");
        }
    }
    
    // 激活预加载的场景
    public void ActivatePreloadedScene(string sceneName)
    {
        if (m_PreloadedScenes.TryGetValue(sceneName, out var sceneHandle))
        {
            // 场景已经加载，直接激活
            Debug.Log($"激活预加载场景: {sceneName}");
            // 这里可以执行场景激活逻辑
        }
        else
        {
            Debug.LogWarning($"场景 {sceneName} 未预加载");
        }
    }
    
    // 清理预加载的场景
    public void CleanupPreloadedScenes()
    {
        var sceneManager = GameSceneManager.Instance;
        
        foreach (var kvp in m_PreloadedScenes)
        {
            var sceneName = kvp.Key;
            if (sceneManager.SceneIsLoaded(sceneName))
            {
                sceneManager.UnloadScene(sceneName);
            }
        }
        
        m_PreloadedScenes.Clear();
        Debug.Log("预加载场景清理完成");
    }
}
```

#### 场景加载进度管理器

```csharp
using FuFramework.Scene.Runtime;
using FuFramework.Event.Runtime;
using UnityEngine;
using UnityEngine.UI;

public class SceneLoadingProgressUI : MonoBehaviour
{
    [SerializeField] private Slider m_ProgressSlider;
    [SerializeField] private Text m_ProgressText;
    [SerializeField] private GameObject m_LoadingPanel;
    
    private string m_CurrentLoadingScene;
    private bool m_IsLoading;
    
    private void Start()
    {
        // 注册场景加载事件
        EventManager.Instance.Subscribe<LoadSceneUpdateEventArgs>(OnSceneLoadProgress);
        EventManager.Instance.Subscribe<LoadSceneSuccessEventArgs>(OnSceneLoadSuccess);
        EventManager.Instance.Subscribe<LoadSceneFailureEventArgs>(OnSceneLoadFailed);
        
        HideLoadingUI();
    }
    
    // 显示加载界面并开始跟踪进度
    public void StartTrackingSceneLoad(string sceneName)
    {
        m_CurrentLoadingScene = sceneName;
        m_IsLoading = true;
        ShowLoadingUI();
        
        // 重置进度显示
        m_ProgressSlider.value = 0f;
        m_ProgressText.text = "0% 加载中...";
    }
    
    private void OnSceneLoadProgress(object sender, LoadSceneUpdateEventArgs e)
    {
        if (!m_IsLoading || e.SceneName != m_CurrentLoadingScene) return;
        
        // 更新进度显示
        m_ProgressSlider.value = e.Progress;
        m_ProgressText.text = $"{e.Progress:P0} 加载中...";
        
        // 模拟进度条动画（可选）
        if (e.Progress >= 0.9f)
        {
            m_ProgressText.text = "场景初始化中...";
        }
    }
    
    private void OnSceneLoadSuccess(object sender, LoadSceneSuccessEventArgs e)
    {
        if (e.SceneName == m_CurrentLoadingScene)
        {
            // 加载完成，隐藏加载界面
            HideLoadingUI();
            m_IsLoading = false;
            m_CurrentLoadingScene = null;
            
            Debug.Log($"场景 {e.SceneName} 加载成功，界面已隐藏");
        }
    }
    
    private void OnSceneLoadFailed(object sender, LoadSceneFailureEventArgs e)
    {
        if (e.SceneName == m_CurrentLoadingScene)
        {
            // 加载失败，显示错误信息
            m_ProgressText.text = $"加载失败: {e.ErrorMessage}";
            m_ProgressText.color = Color.red;
            
            // 3秒后自动隐藏
            Invoke(nameof(HideLoadingUI), 3f);
            
            m_IsLoading = false;
            m_CurrentLoadingScene = null;
        }
    }
    
    private void ShowLoadingUI()
    {
        m_LoadingPanel.SetActive(true);
    }
    
    private void HideLoadingUI()
    {
        m_LoadingPanel.SetActive(false);
        m_ProgressText.color = Color.white; // 重置颜色
    }
    
    private void OnDestroy()
    {
        // 注销事件监听
        EventManager.Instance.Unsubscribe<LoadSceneUpdateEventArgs>(OnSceneLoadProgress);
        EventManager.Instance.Unsubscribe<LoadSceneSuccessEventArgs>(OnSceneLoadSuccess);
        EventManager.Instance.Unsubscribe<LoadSceneFailureEventArgs>(OnSceneLoadFailed);
    }
}
```

## 高级用法

### 1. 自定义场景加载策略

```csharp
using FuFramework.Scene.Runtime;
using Cysharp.Threading.Tasks;
using System;

public class AdvancedSceneLoader : MonoBehaviour
{
    // 自定义场景加载配置
    [Serializable]
    public class SceneLoadConfig
    {
        public string SceneName;
        public LoadSceneMode LoadMode = LoadSceneMode.Additive;
        public bool ShowLoadingScreen = true;
        public float MinLoadTime = 2f; // 最小加载时间（用于避免加载过快）
        public Action OnLoadComplete;
        public Action<float> OnProgressUpdate;
    }
    
    // 带配置的异步场景加载
    public async UniTask<SceneHandle> LoadSceneWithConfig(SceneLoadConfig config)
    {
        var sceneManager = GameSceneManager.Instance;
        var startTime = Time.time;
        
        // 显示加载界面
        if (config.ShowLoadingScreen)
        {
            ShowCustomLoadingScreen();
        }
        
        try
        {
            // 开始加载场景
            var sceneHandle = await sceneManager.LoadSceneByName(config.SceneName, config.LoadMode);
            
            // 等待最小加载时间（避免加载过快导致的视觉跳跃）
            var elapsedTime = Time.time - startTime;
            if (elapsedTime < config.MinLoadTime)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(config.MinLoadTime - elapsedTime));
            }
            
            // 调用完成回调
            config.OnLoadComplete?.Invoke();
            
            return sceneHandle;
        }
        finally
        {
            // 隐藏加载界面
            if (config.ShowLoadingScreen)
            {
                HideCustomLoadingScreen();
            }
        }
    }
    
    // 批量场景加载
    public async UniTask LoadMultipleScenes(params string[] sceneNames)
    {
        var loadTasks = new List<UniTask>();
        
        foreach (var sceneName in sceneNames)
        {
            var config = new SceneLoadConfig
            {
                SceneName = sceneName,
                LoadMode = LoadSceneMode.Additive,
                ShowLoadingScreen = false // 批量加载时不显示单独加载界面
            };
            
            loadTasks.Add(LoadSceneWithConfig(config));
        }
        
        // 并行加载所有场景
        await UniTask.WhenAll(loadTasks);
        
        Debug.Log($"批量加载完成: {string.Join(", ", sceneNames)}");
    }
    
    private void ShowCustomLoadingScreen()
    {
        // 自定义加载界面显示逻辑
    }
    
    private void HideCustomLoadingScreen()
    {
        // 自定义加载界面隐藏逻辑
    }
}
```

### 2. 场景依赖管理

```csharp
using FuFramework.Scene.Runtime;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

public class SceneDependencyManager : MonoBehaviour
{
    // 场景依赖关系定义
    private readonly Dictionary<string, string[]> m_SceneDependencies = new()
    {
        { "Level3", new[] { "Level2", "Level1" } },
        { "BossBattle", new[] { "Level3", "SpecialAssets" } },
        { "Ending", new[] { "BossBattle", "CreditsScene" } }
    };
    
    // 检查并加载场景依赖
    public async UniTask<bool> LoadSceneWithDependencies(string targetScene)
    {
        var sceneManager = GameSceneManager.Instance;
        
        // 检查目标场景是否已加载
        if (sceneManager.SceneIsLoaded(targetScene))
        {
            Debug.LogWarning($"场景 {targetScene} 已经加载");
            return true;
        }
        
        // 获取依赖关系
        if (!m_SceneDependencies.TryGetValue(targetScene, out var dependencies))
        {
            // 没有依赖，直接加载目标场景
            await sceneManager.LoadSceneByName(targetScene, LoadSceneMode.Additive);
            return true;
        }
        
        // 加载所有依赖场景
        foreach (var dependency in dependencies)
        {
            if (!sceneManager.SceneIsLoaded(dependency))
            {
                await sceneManager.LoadSceneByName(dependency, LoadSceneMode.Additive);
            }
        }
        
        // 加载目标场景
        await sceneManager.LoadSceneByName(targetScene, LoadSceneMode.Additive);
        
        return true;
    }
    
    // 卸载场景及其依赖（如果没有其他场景使用）
    public void UnloadSceneWithDependencies(string targetScene)
    {
        var sceneManager = GameSceneManager.Instance;
        
        if (!sceneManager.SceneIsLoaded(targetScene))
        {
            Debug.LogWarning($"场景 {targetScene} 未加载，无需卸载");
            return;
        }
        
        // 卸载目标场景
        sceneManager.UnloadScene(targetScene);
        
        // 检查依赖场景是否可以卸载
        if (m_SceneDependencies.TryGetValue(targetScene, out var dependencies))
        {
            foreach (var dependency in dependencies)
            {
                if (CanUnloadDependency(dependency))
                {
                    sceneManager.UnloadScene(dependency);
                }
            }
        }
    }
    
    // 检查依赖场景是否可以被卸载
    private bool CanUnloadDependency(string dependencyScene)
    {
        var sceneManager = GameSceneManager.Instance;
        
        // 检查是否有其他场景依赖于此场景
        foreach (var kvp in m_SceneDependencies)
        {
            var scene = kvp.Key;
            var dependencies = kvp.Value;
            
            // 如果其他已加载的场景依赖于此场景，则不能卸载
            if (sceneManager.SceneIsLoaded(scene) && 
                System.Array.IndexOf(dependencies, dependencyScene) >= 0)
            {
                return false;
            }
        }
        
        return true;
    }
}
```

## 性能优化建议

### 1. 场景加载优化

```csharp
using FuFramework.Scene.Runtime;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

public class SceneOptimizationManager : MonoBehaviour
{
    private readonly HashSet<string> m_FrequentlyUsedScenes = new()
    {
        "MainMenu",
        "GameScene", 
        "Inventory",
        "Shop"
    };
    
    // 预加载常用场景
    public async UniTask PreloadFrequentScenes()
    {
        var sceneManager = GameSceneManager.Instance;
        var preloadTasks = new List<UniTask>();
        
        foreach (var sceneName in m_FrequentlyUsedScenes)
        {
            if (!sceneManager.SceneIsLoaded(sceneName) && 
                !sceneManager.SceneIsLoading(sceneName))
            {
                preloadTasks.Add(sceneManager.LoadSceneByName(sceneName, LoadSceneMode.Additive));
            }
        }
        
        if (preloadTasks.Count > 0)
        {
            await UniTask.WhenAll(preloadTasks);
            Debug.Log("常用场景预加载完成");
        }
    }
    
    // 定期清理不常用的场景
    public void CleanupUnusedScenes()
    {
        var sceneManager = GameSceneManager.Instance;
        var loadedScenes = sceneManager.GetAllLoadedSceneAssetPaths();
        
        foreach (var scenePath in loadedScenes)
        {
            var sceneName = sceneManager.GetSceneName(scenePath);
            
            // 如果不是常用场景，且不是当前活动场景，则卸载
            if (!m_FrequentlyUsedScenes.Contains(sceneName) && 
                !IsActiveScene(sceneName))
            {
                sceneManager.UnloadScene(scenePath);
                Debug.Log($"清理不常用场景: {sceneName}");
            }
        }
    }
    
    private bool IsActiveScene(string sceneName)
    {
        // 检查是否为当前活动场景的逻辑
        return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == sceneName;
    }
}
```

### 2. 内存使用监控

```csharp
using FuFramework.Scene.Runtime;
using UnityEngine;

public class SceneMemoryMonitor : MonoBehaviour
{
    [SerializeField] private bool m_EnableMonitoring = true;
    [SerializeField] private float m_CheckInterval = 10f;
    
    private float m_LastCheckTime;
    
    private void Update()
    {
        if (!m_EnableMonitoring) return;
        
        if (Time.time - m_LastCheckTime >= m_CheckInterval)
        {
            CheckSceneMemoryUsage();
            m_LastCheckTime = Time.time;
        }
    }
    
    private void CheckSceneMemoryUsage()
    {
        var sceneManager = GameSceneManager.Instance;
        var loadedScenes = sceneManager.GetAllLoadedSceneAssetPaths();
        
        Debug.Log($"当前加载场景数量: {loadedScenes.Length}");
        
        // 检查内存使用情况
        var totalMemory = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1024 / 1024;
        Debug.Log($"总内存使用: {totalMemory} MB");
        
        // 如果内存使用过高，建议清理场景
        if (totalMemory > 500) // 500MB 阈值
        {
            Debug.LogWarning("内存使用过高，建议清理不必要的场景");
        }
    }
}
```

## 注意事项

### 1. 内存管理
- **及时卸载**：不再使用的场景应及时卸载释放内存
- **避免泄漏**：确保场景对象正确释放，避免内存泄漏
- **合理预加载**：根据使用频率合理预加载场景

### 2. 性能考虑
- **异步加载**：使用异步加载避免阻塞主线程
- **进度跟踪**：合理使用进度事件更新 UI
- **错误处理**：完善的错误处理机制确保稳定性

### 3. 线程安全
- **事件系统**：场景事件在主线程序列化处理
- **状态同步**：确保场景状态变化的线程安全
- **资源管理**：与资源管理器的线程安全协作

### 4. 错误处理
- **场景存在性**：加载前检查场景是否存在
- **状态检查**：避免重复加载或卸载
- **异常捕获**：使用 try-catch 包装关键操作

## API 参考

### GameSceneManager 类

#### 静态属性

##### Instance
```csharp
public static GameSceneManager Instance { get; }
```
**功能**：获取场景管理器单例实例

#### 实例方法

##### LoadSceneByName(string sceneAssetName, LoadSceneMode sceneMode, object userData)
```csharp
public UniTask<SceneHandle> LoadSceneByName(string sceneAssetName, LoadSceneMode sceneMode = LoadSceneMode.Additive, object userData = null)
```
**功能**：通过场景名称加载场景

**参数**：
- `sceneAssetName` (string)：场景资源名称
- `sceneMode` (LoadSceneMode)：加载模式，默认 Additive
- `userData` (object)：用户自定义数据

**返回值**：
- `UniTask<SceneHandle>`：异步场景加载任务

**示例**：
```csharp
var sceneHandle = await GameSceneManager.Instance.LoadSceneByName("MainScene");
```

##### UnloadScene(string sceneAssetPath, object userData)
```csharp
public void UnloadScene(string sceneAssetPath, object userData = null)
```
**功能**：卸载指定场景

**参数**：
- `sceneAssetPath` (string)：场景资源路径
- `userData` (object)：用户自定义数据

**示例**：
```csharp
GameSceneManager.Instance.UnloadScene("Assets/Scenes/MainScene.unity");
```

##### SceneIsLoaded(string sceneAssetPath)
```csharp
public bool SceneIsLoaded(string sceneAssetPath)
```
**功能**：检查场景是否已加载

**参数**：
- `sceneAssetPath` (string)：场景资源路径

**返回值**：
- `bool`：场景是否已加载

**示例**：
```csharp
bool isLoaded = GameSceneManager.Instance.SceneIsLoaded("MainScene");
```

## 常见问题解答

### Q: 场景加载失败怎么办？
A: 检查场景路径是否正确，场景文件是否存在，资源包是否加载成功。

### Q: 如何实现场景切换的过渡效果？
A: 使用加载界面显示进度，结合事件系统实现平滑过渡效果。

### Q: 多场景叠加时如何管理场景关系？
A: 使用场景依赖管理系统，确保场景加载顺序和依赖关系正确。

### Q: 场景加载过程中如何避免内存峰值？
A: 合理使用异步加载，分批加载资源，监控内存使用情况。

### Q: 如何优化场景加载性能？
A: 预加载常用场景，使用对象池管理场景资源，优化资源包大小。

## 总结

Scene 模块为 FuFramework 提供了强大的场景管理系统，支持异步加载、事件通知、状态跟踪等高级功能。通过合理的场景管理策略，可以显著提升游戏的加载性能和用户体验。

该模块设计合理，功能完善，与资源管理系统深度集成，是游戏开发中场景管理的理想解决方案。