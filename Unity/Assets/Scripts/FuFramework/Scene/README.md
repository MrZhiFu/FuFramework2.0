# FuFramework Scene Module

## 1. 简介

FuFramework Scene 模块是游戏框架的场景管理系统，专门用于管理 Unity 场景的加载、卸载和状态跟踪。该模块基于 YooAsset 资源管理系统和 UniTask 异步编程模型，提供异步场景加载、进度跟踪、事件通知等高级功能，是游戏场景管理的核心组件。

## 2. 核心特性

- **异步场景加载**：基于 UniTask 的异步场景加载，支持进度跟踪
- **事件驱动架构**：完整的场景生命周期事件通知机制
- **资源管理集成**：与 YooAsset 资源管理系统深度集成
- **状态跟踪**：实时跟踪场景的加载、卸载、使用状态
- **错误处理**：完善的错误处理和异常捕获机制

## 3. 核心概念

### 3.1 类继承与实现体系

```
【类继承体系】

ModuleBase (框架模块基类)
    └── SceneModule (场景管理模块)
        ├── SceneHandleData (内部类)     # 封装场景加载中的数据
        │   ├── SceneHandle              # 场景加载句柄
        │   └── UserData                 # 用户自定义数据
        │
        ├── m_LoadedSceneDict            # 已加载场景字典
        ├── m_LoadingSceneDict           # 正在加载场景字典
        └── m_UnloadingSceneDict         # 正在卸载场景字典


【事件参数类体系】

GameEventArgs (事件参数基类)
    ├── LoadSceneSuccessEventArgs      # 加载场景成功事件
    ├── LoadSceneFailureEventArgs      # 加载场景失败事件
    ├── LoadSceneUpdateEventArgs       # 加载场景更新事件(进度)
    ├── UnloadSceneSuccessEventArgs    # 卸载场景成功事件
    ├── UnloadSceneFailureEventArgs    # 卸载场景失败事件
    └── ActiveSceneChangedEventArgs    # 活动场景改变事件


【YooAsset 集成】

YooAsset.SceneHandle (场景句柄)
    └── 提供场景加载、卸载、进度查询等功能


【模块依赖关系】

SceneModule 依赖:
    ├── AssetModule (资源管理模块)
    │   └── LoadSceneAsync()           # 异步加载场景
    │
    └── EventModule (事件模块)
        └── EventRegister              # 事件订阅/广播
```

### 3.2 场景管理架构

```
┌─────────────────────────────────────────────────────────────┐
│                     SceneModule                             │
│                     (ModuleBase)                              │
│  ┌─────────────────────────────────────────────────────┐   │
│  │                    状态字典                          │   │
│  │                                                       │   │
│  │  m_LoadedSceneDict     m_LoadingSceneDict            │   │
│  │  ┌──────────────┐     ┌──────────────┐              │   │
│  │  │ Path1:Handle1│     │ Path2:Data2  │              │   │
│  │  │ Path2:Handle2│     │ Path3:Data3  │              │   │
│  │  └──────────────┘     └──────────────┘              │   │
│  │       已加载              正在加载                   │   │
│  │                                                       │   │
│  │  m_UnloadingSceneDict                                │   │
│  │  ┌──────────────┐                                    │   │
│  │  │ Path1:Handle1│                                    │   │
│  │  └──────────────┘                                    │   │
│  │       正在卸载                                       │   │
│  └─────────────────────────────────────────────────────┘   │
│                              │                            │
│                              ▼                            │
│  ┌─────────────────────────────────────────────────────┐   │
│  │                    事件系统                          │   │
│  │  EventRegister.Broadcast()                          │   │
│  │  - LoadSceneSuccessEventArgs                        │   │
│  │  - LoadSceneFailureEventArgs                        │   │
│  │  - LoadSceneUpdateEventArgs                         │   │
│  │  - UnloadSceneSuccessEventArgs                      │   │
│  │  - UnloadSceneFailureEventArgs                      │   │
│  │  - ActiveSceneChangedEventArgs                      │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
                    ┌──────────────────┐
                    │   AssetModule    │
                    │  (YooAsset集成)   │
                    │ LoadSceneAsync() │
                    └──────────────────┘
```

### 3.3 场景生命周期

```
【场景状态流转】

初始状态
    │
    ▼
┌───────────┐    LoadScene()    ┌───────────┐    OnUpdate()    ┌───────────┐
│   空闲    │ ─────────────────▶ │  加载中   │ ───────────────▶ │  加载中   │
│  (Idle)   │                    │(Loading)  │   进度更新       │(Loading)  │
└───────────┘                    └───────────┘                  └─────┬─────┘
                                                                      │
                         加载完成                                     │
                              │                                       │
                              ▼                                       │
                    ┌─────────────────┐                               │
                    │ OnLoadCompleted │                               │
                    │   回调处理      │                               │
                    └────────┬────────┘                               │
                             │                                        │
              ┌──────────────┼──────────────┐                        │
              │              │              │                        │
              ▼              ▼              ▼                        │
        ┌─────────┐    ┌─────────┐    ┌─────────┐                    │
        │ 成功    │    │ 失败    │    │ 更新    │◀───────────────────┘
        │(Success)│    │(Failure)│    │(Update) │
        └────┬────┘    └────┬────┘    └─────────┘
             │              │
             ▼              ▼
    ┌─────────────┐  ┌─────────────┐
    │ 已加载字典  │  │ 失败事件    │
    │ 广播成功事件│  │ 广播失败事件│
    └──────┬──────┘  └─────────────┘
           │
           ▼
    ┌─────────────┐    UnloadScene()    ┌─────────────┐
    │   已加载    │ ───────────────────▶ │  卸载中     │
    │  (Loaded)   │                      │(Unloading)  │
    └─────────────┘                      └──────┬──────┘
                                                │
                                                ▼
                                       ┌─────────────────┐
                                       │ 卸载完成回调    │
                                       │ 广播成功/失败   │
                                       └─────────────────┘


【场景加载流程】

1. 调用 LoadScene(sceneAssetPath, sceneMode, userData)
   ├── 参数校验（路径格式、非空检查）
   ├── 状态检查（是否正在加载/卸载/已加载）
   └── 调用 AssetModule.LoadSceneAsync()

2. 添加到 m_LoadingSceneDict
   └── SceneHandleData(SceneHandle, UserData)

3. 注册 Completed 回调
   └── OnLoadSceneCompleted()

4. 每帧更新进度 (OnUpdate)
   └── OnLoadSceneUpdate() 广播进度事件

5. 加载完成回调
   ├── 成功: 添加到 m_LoadedSceneDict, 广播 LoadSceneSuccessEventArgs
   └── 失败: 广播 LoadSceneFailureEventArgs
```

### 3.4 事件系统

```
【事件参数类结构】

所有事件参数类继承自 GameEventArgs:

LoadSceneSuccessEventArgs
    ├── EventId: string              # 事件唯一标识
    ├── SceneName: string            # 场景名称
    └── UserData: object             # 用户自定义数据

LoadSceneFailureEventArgs
    ├── EventId: string
    ├── SceneName: string
    ├── ErrorMessage: string         # 错误信息
    ├── Status: EOperationStatus     # 操作状态
    └── UserData: object

LoadSceneUpdateEventArgs
    ├── EventId: string
    ├── SceneName: string
    ├── Progress: float              # 加载进度 0-1
    └── UserData: object

UnloadSceneSuccessEventArgs
    ├── EventId: string
    ├── SceneName: string
    └── UserData: object

UnloadSceneFailureEventArgs
    ├── EventId: string
    ├── SceneName: string
    └── UserData: object

ActiveSceneChangedEventArgs
    ├── EventId: string
    ├── LastActiveScene: Scene       # 上一个活动场景
    └── ActiveScene: Scene           # 当前活动场景


【事件创建与清理】

所有事件参数类使用引用池管理:

Create() 方法:
    └── ReferencePool.Acquire<T>()
        └── 从引用池获取对象
        └── 设置属性值
        └── 返回事件对象

Clear() 方法 (IReference接口):
    └── 重置所有属性为默认值
    └── 对象归还引用池
```

## 4. 核心类详细说明

### 4.1 SceneModule

场景管理模块，继承自 `ModuleBase`，负责整个场景系统的生命周期管理。

**核心功能：**

```csharp
public sealed class SceneModule : ModuleBase
{
    // 生命周期
    protected override void OnInit()           // 初始化，获取 AssetModule
    protected override void OnUpdate(...)      // 更新加载进度
    protected override void OnDispose()        // 卸载所有场景
    
    // 状态查询
    public bool HasScene(string sceneAssetPath)              // 检查场景资源是否存在
    public bool IsLoaded(string sceneAssetPath)              // 场景是否已加载
    public bool IsLoading(string sceneAssetPath)             // 场景是否正在加载
    public bool IsUnloading(string sceneAssetPath)           // 场景是否正在卸载
    public string GetSceneName(string sceneAssetPath)        // 获取场景名称
    public string[] GetAllLoadedSceneAssetPaths()            // 获取所有已加载场景路径
    public string[] GetAllLoadingSceneAssetPaths()           // 获取所有正在加载场景路径
    public string[] GetAllUnloadingSceneAssetPaths()         // 获取所有正在卸载场景路径
    
    // 场景加载
    public UniTask<SceneHandle> LoadSceneByName(string sceneAssetName, LoadSceneMode sceneMode = LoadSceneMode.Additive, object userData = null)
    public async UniTask<SceneHandle> LoadScene(string sceneAssetPath, LoadSceneMode sceneMode = LoadSceneMode.Additive, object userData = null)
    
    // 场景卸载
    public void UnloadScene(string sceneAssetPath, object userData = null)
}
```

**内部数据结构：**
- `SceneHandleData` - 封装场景加载中的数据（SceneHandle + UserData）
- `m_LoadedSceneDict` - 已加载场景字典（Key: 路径, Value: SceneHandle）
- `m_LoadingSceneDict` - 正在加载场景字典（Key: 路径, Value: SceneHandleData）
- `m_UnloadingSceneDict` - 正在卸载场景字典（Key: 路径, Value: SceneHandle）

### 4.2 场景事件参数类

所有事件参数类都继承自 `GameEventArgs` 并实现 `IReference` 接口。

**通用结构：**

```csharp
public sealed class XxxEventArgs : GameEventArgs
{
    public override string Id => EventId;           // 事件ID
    public static readonly string EventId;          // 静态事件ID
    
    // 事件特定属性
    public string SceneName { get; private set; }
    public object UserData { get; private set; }
    
    // 工厂方法
    public static XxxEventArgs Create(...)
    {
        var args = ReferencePool.Acquire<XxxEventArgs>();
        // 设置属性
        return args;
    }
    
    // 清理方法
    public override void Clear()
    {
        // 重置属性
    }
}
```

**事件类型说明：**

| 事件类 | 触发时机 | 主要属性 |
|--------|----------|----------|
| LoadSceneSuccessEventArgs | 场景加载成功 | SceneName, UserData |
| LoadSceneFailureEventArgs | 场景加载失败 | SceneName, ErrorMessage, Status, UserData |
| LoadSceneUpdateEventArgs | 场景加载进度更新 | SceneName, Progress, UserData |
| UnloadSceneSuccessEventArgs | 场景卸载成功 | SceneName, UserData |
| UnloadSceneFailureEventArgs | 场景卸载失败 | SceneName, UserData |
| ActiveSceneChangedEventArgs | 活动场景改变 | LastActiveScene, ActiveScene |

## 5. 使用示例

### 5.1 基本场景加载

```csharp
using FuFramework.Scene.Runtime;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class SceneLoader : MonoBehaviour
{
    private async void Start()
    {
        var sceneModule = GlobalModule.SceneModule;
        
        try
        {
            // 通过场景名称加载（自动转换路径）
            var sceneHandle = await sceneModule.LoadSceneByName("MainScene", LoadSceneMode.Single);
            
            if (sceneHandle.IsDone)
            {
                Debug.Log("场景加载完成");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"场景加载失败: {e.Message}");
        }
    }
}
```

### 5.2 事件监听示例

```csharp
using FuFramework.Scene.Runtime;
using FuFramework.Event.Runtime;
using UnityEngine;

public class SceneEventListener : MonoBehaviour
{
    private EventRegister m_EventRegister;
    
    private void Start()
    {
        m_EventRegister = EventRegister.Create();
        
        // 注册场景加载事件
        m_EventRegister.Subscribe<LoadSceneSuccessEventArgs>(OnSceneLoadSuccess);
        m_EventRegister.Subscribe<LoadSceneUpdateEventArgs>(OnSceneLoadProgress);
        m_EventRegister.Subscribe<LoadSceneFailureEventArgs>(OnSceneLoadFailed);
        m_EventRegister.Subscribe<UnloadSceneSuccessEventArgs>(OnSceneUnloadSuccess);
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
    
    private void OnSceneUnloadSuccess(object sender, UnloadSceneSuccessEventArgs e)
    {
        Debug.Log($"场景 {e.SceneName} 卸载成功");
    }
    
    private void OnDestroy()
    {
        m_EventRegister?.Release();
    }
}
```

### 5.3 完整场景切换流程

```csharp
using FuFramework.Scene.Runtime;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class SceneSwitcher : MonoBehaviour
{
    private const string MAIN_MENU_SCENE = "MainMenu";
    private const string GAME_SCENE = "GameScene";
    private const string LOADING_SCENE = "LoadingScene";
    
    [SerializeField] private LoadingUI m_LoadingUI;
    
    private async void Start()
    {
        await SwitchToMainMenu();
    }
    
    // 切换到主菜单
    public async UniTask SwitchToMainMenu()
    {
        await UnloadCurrentScene();
        await GlobalModule.SceneModule.LoadSceneByName(MAIN_MENU_SCENE, LoadSceneMode.Single);
    }
    
    // 开始游戏
    public async UniTask StartGame()
    {
        // 显示加载界面
        m_LoadingUI.Show();
        
        // 卸载主菜单
        var sceneModule = GlobalModule.SceneModule;
        if (sceneModule.IsLoaded(Utility.AssetPath.GetScenePath(MAIN_MENU_SCENE)))
        {
            sceneModule.UnloadScene(Utility.AssetPath.GetScenePath(MAIN_MENU_SCENE));
        }
        
        // 加载游戏场景
        await LoadGameScene();
        
        // 隐藏加载界面
        m_LoadingUI.Hide();
    }
    
    // 加载游戏场景（带进度显示）
    private async UniTask LoadGameScene()
    {
        var eventRegister = EventRegister.Create();
        
        try
        {
            // 注册进度事件
            eventRegister.Subscribe<LoadSceneUpdateEventArgs>((sender, e) =>
            {
                m_LoadingUI.SetProgress(e.Progress);
            });
            
            // 异步加载游戏场景
            var sceneHandle = await GlobalModule.SceneModule.LoadSceneByName(
                GAME_SCENE, 
                LoadSceneMode.Single
            );
            
            if (!sceneHandle.IsDone)
            {
                Debug.LogError("游戏场景加载失败");
                return;
            }
            
            Debug.Log("游戏场景加载完成");
        }
        finally
        {
            eventRegister.Release();
        }
    }
    
    // 卸载当前场景
    private async UniTask UnloadCurrentScene()
    {
        var sceneModule = GlobalModule.SceneModule;
        var loadedPaths = sceneModule.GetAllLoadedSceneAssetPaths();
        
        foreach (var path in loadedPaths)
        {
            if (!sceneModule.IsUnloading(path))
            {
                sceneModule.UnloadScene(path);
            }
        }
        
        // 等待卸载完成
        await UniTask.WaitUntil(() => sceneModule.GetAllUnloadingSceneAssetPaths().Length == 0);
    }
}
```

### 5.4 场景状态检查

```csharp
public class SceneStateChecker : MonoBehaviour
{
    private void Update()
    {
        var sceneModule = GlobalModule.SceneModule;
        
        // 检查场景是否已加载
        if (sceneModule.IsLoaded("Assets/Scenes/GameScene.unity"))
        {
            Debug.Log("游戏场景已加载");
        }
        
        // 检查场景是否正在加载
        if (sceneModule.IsLoading("Assets/Scenes/LoadingScene.unity"))
        {
            Debug.Log("加载场景正在加载中...");
        }
        
        // 检查场景是否正在卸载
        if (sceneModule.IsUnloading("Assets/Scenes/MenuScene.unity"))
        {
            Debug.Log("菜单场景正在卸载中...");
        }
        
        // 获取所有已加载场景
        var loadedScenes = sceneModule.GetAllLoadedSceneAssetPaths();
        Debug.Log($"当前已加载 {loadedScenes.Length} 个场景");
        
        // 获取场景名称
        var sceneName = sceneModule.GetSceneName("Assets/Scenes/Levels/Level1.unity");
        Debug.Log($"场景名称: {sceneName}");  // 输出: Level1
    }
}
```

## 6. 目录结构

```
Assets/FuFramework/Scene/
├── Runtime/
│   ├── FuFramework.Scene.Runtime.asmdef    # 程序集定义
│   ├── SceneModule.cs                       # 场景管理模块
│   └── Event/
│       ├── LoadSceneSuccessEventArgs.cs     # 加载场景成功事件
│       ├── LoadSceneFailureEventArgs.cs     # 加载场景失败事件
│       ├── LoadSceneUpdateEventArgs.cs      # 加载场景更新事件
│       ├── UnloadSceneSuccessEventArgs.cs   # 卸载场景成功事件
│       ├── UnloadSceneFailureEventArgs.cs   # 卸载场景失败事件
│       └── ActiveSceneChangedEventArgs.cs   # 活动场景改变事件
├── Editor/
│   ├── FuFramework.Scene.Editor.asmdef      # 编辑器程序集定义
│   └── Inspector/
│       └── SceneModuleInspector.cs          # 模块 Inspector 面板
└── README.md                                # 本文档
```

## 7. 依赖

| 模块 | 说明 |
|------|------|
| FuFramework.Core | 提供 ModuleBase 基类、FuException、FuLogger、FuGuard |
| FuFramework.Asset | 提供 AssetModule 和 YooAsset 集成 |
| FuFramework.Event | 提供 GameEventArgs 和 EventRegister |
| FuFramework.ReferencePool | 提供引用池管理 |
| YooAsset | 场景加载底层实现 |
| UniTask | 异步编程支持 |

## 8. 最佳实践

### 8.1 场景加载规范

```csharp
public class SceneManager : MonoBehaviour
{
    // 1. 使用 try-catch 处理异常
    public async UniTask SafeLoadScene(string sceneName)
    {
        try
        {
            var handle = await GlobalModule.SceneModule.LoadSceneByName(sceneName);
            // 处理加载完成
        }
        catch (Exception e)
        {
            Debug.LogError($"加载场景失败: {e.Message}");
            // 错误处理
        }
    }
    
    // 2. 检查场景状态后再操作
    public void SafeUnloadScene(string scenePath)
    {
        var sceneModule = GlobalModule.SceneModule;
        
        if (!sceneModule.IsLoaded(scenePath))
        {
            Debug.LogWarning("场景未加载，无需卸载");
            return;
        }
        
        if (sceneModule.IsUnloading(scenePath))
        {
            Debug.LogWarning("场景正在卸载中");
            return;
        }
        
        sceneModule.UnloadScene(scenePath);
    }
}
```

### 8.2 加载界面集成

```csharp
public class LoadingScreen : MonoBehaviour
{
    [SerializeField] private Slider m_ProgressSlider;
    [SerializeField] private Text m_ProgressText;
    
    private EventRegister m_EventRegister;
    
    public void Show()
    {
        gameObject.SetActive(true);
        m_EventRegister = EventRegister.Create();
        
        // 监听进度事件
        m_EventRegister.Subscribe<LoadSceneUpdateEventArgs>((sender, e) =>
        {
            UpdateProgress(e.Progress);
        });
        
        // 监听完成事件
        m_EventRegister.Subscribe<LoadSceneSuccessEventArgs>((sender, e) =>
        {
            Hide();
        });
    }
    
    private void UpdateProgress(float progress)
    {
        m_ProgressSlider.value = progress;
        m_ProgressText.text = $"{progress:P0}";
    }
    
    public void Hide()
    {
        m_EventRegister?.Release();
        gameObject.SetActive(false);
    }
}
```

### 8.3 注意事项

1. **路径格式**：场景路径必须以 `Assets/` 开头，以 `.unity` 结尾，如 `Assets/Scenes/MainScene.unity`
2. **重复加载检查**：已加载的场景不能重复加载，需要先卸载
3. **状态检查**：加载/卸载前检查场景状态，避免冲突操作
4. **事件注销**：组件销毁时释放 EventRegister，避免内存泄漏
5. **异步等待**：使用 UniTask 和 await 处理异步操作，避免阻塞主线程
6. **异常处理**：使用 try-catch 捕获加载过程中的异常
7. **资源释放**：模块销毁时会自动卸载所有场景，无需手动处理
