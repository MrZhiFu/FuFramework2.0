# FuFramework Procedure Module

## 概述

Procedure 模块是 FuFramework 的流程管理系统，基于有限状态机（FSM）实现游戏流程的状态管理和转换。该模块提供了一种结构化的方式来管理游戏的不同阶段和流程，如启动流程、登录流程、游戏流程等。

### 核心特性

- **基于FSM**：继承自有限状态机模块，提供状态管理能力
- **流程管理**：统一管理游戏各个阶段的流程转换
- **生命周期**：完整的流程生命周期管理
- **优先级控制**：支持流程优先级设置
- **状态监控**：实时监控当前流程状态和持续时间

## 核心类说明

### ProcedureModule

流程管理器，负责管理所有流程和流程状态转换。

```csharp
[ModuleDependency(typeof(FsmModule))]
public sealed class ProcedureModule : FuModule
```

**主要功能：**
- 流程状态机的创建和管理
- 流程的初始化和启动
- 流程状态查询和监控
- 流程生命周期管理

**主要属性：**
- `ProcedureBase CurrentProcedure` - 获取当前流程
- `float CurrentProcedureTime` - 获取当前流程持续时间

### ProcedureBase

流程基类，所有自定义流程都需要继承此类。

```csharp
public abstract class ProcedureBase : FsmStateBase
```

**主要功能：**
- 定义流程的生命周期方法
- 提供流程优先级控制
- 继承FSM状态机的所有功能

**主要方法：**
- `OnInit(Fsm procedureOwner)` - 流程初始化时调用
- `OnEnter(object userData)` - 进入流程时调用
- `OnUpdate(float elapseSeconds, float realElapseSeconds)` - 流程更新时调用
- `OnLeave(bool isShutdown)` - 离开流程时调用

## 技术架构

### 依赖关系

```
ProcedureModule → FsmModule
ProcedureBase → FsmStateBase
```

### 流程生命周期

1. **初始化阶段**：`OnInit()` - 流程初始化，设置初始状态
2. **进入阶段**：`OnEnter()` - 流程开始执行，加载资源
3. **更新阶段**：`OnUpdate()` - 流程持续运行，处理逻辑
4. **离开阶段**：`OnLeave()` - 流程结束，清理资源

### 状态转换机制

```
流程A (OnLeave) → 流程B (OnEnter) → 流程B (OnUpdate)
```

## 使用指南

### 1. 基础使用

#### 创建自定义流程类

```csharp
using FuFramework.Procedure.Runtime;
using FuFramework.Fsm.Runtime;

// 启动流程
public class LaunchProcedure : ProcedureBase
{
    public override int Priority => 10; // 高优先级
    
    protected override void OnInit(Fsm procedureOwner)
    {
        base.OnInit(procedureOwner);
        Debug.Log("启动流程初始化");
    }
    
    protected override void OnEnter(object userData)
    {
        base.OnEnter(userData);
        Debug.Log("进入启动流程");
        
        // 执行启动逻辑
        InitializeGameSystems();
        LoadInitialResources();
    }
    
    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
        
        // 检查启动条件
        if (IsInitializationComplete())
        {
            // 切换到登录流程
            ChangeProcedure<LoginProcedure>();
        }
    }
    
    protected override void OnLeave(bool isShutdown)
    {
        base.OnLeave(isShutdown);
        Debug.Log("离开启动流程");
        
        // 清理启动资源
        CleanupLaunchResources();
    }
    
    private void InitializeGameSystems()
    {
        // 初始化游戏系统
        Debug.Log("初始化游戏系统...");
    }
    
    private void LoadInitialResources()
    {
        // 加载初始资源
        Debug.Log("加载初始资源...");
    }
    
    private bool IsInitializationComplete()
    {
        // 检查初始化是否完成
        return CurrentProcedureTime > 3f; // 模拟3秒后完成
    }
    
    private void CleanupLaunchResources()
    {
        // 清理启动资源
        Debug.Log("清理启动资源...");
    }
}

// 登录流程
public class LoginProcedure : ProcedureBase
{
    public override int Priority => 5; // 中等优先级
    
    protected override void OnEnter(object userData)
    {
        base.OnEnter(userData);
        Debug.Log("进入登录流程");
        
        // 显示登录界面
        ShowLoginUI();
    }
    
    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
        
        // 检查登录状态
        if (IsLoginSuccessful())
        {
            // 切换到游戏流程
            ChangeProcedure<GameProcedure>();
        }
    }
    
    protected override void OnLeave(bool isShutdown)
    {
        base.OnLeave(isShutdown);
        Debug.Log("离开登录流程");
        
        // 隐藏登录界面
        HideLoginUI();
    }
    
    private void ShowLoginUI()
    {
        // 显示登录界面
        Debug.Log("显示登录界面...");
    }
    
    private void HideLoginUI()
    {
        // 隐藏登录界面
        Debug.Log("隐藏登录界面...");
    }
    
    private bool IsLoginSuccessful()
    {
        // 模拟登录成功条件
        return CurrentProcedureTime > 2f; // 2秒后登录成功
    }
}

// 游戏流程
public class GameProcedure : ProcedureBase
{
    public override int Priority => 1; // 低优先级
    
    protected override void OnEnter(object userData)
    {
        base.OnEnter(userData);
        Debug.Log("进入游戏流程");
        
        // 进入游戏主逻辑
        EnterGameWorld();
    }
    
    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
        
        // 游戏主循环逻辑
        UpdateGameLogic(elapseSeconds);
        
        // 检查退出条件
        if (ShouldExitGame())
        {
            // 切换到退出流程
            ChangeProcedure<ExitProcedure>();
        }
    }
    
    protected override void OnLeave(bool isShutdown)
    {
        base.OnLeave(isShutdown);
        Debug.Log("离开游戏流程");
        
        // 退出游戏世界
        ExitGameWorld();
    }
    
    private void EnterGameWorld()
    {
        // 进入游戏世界
        Debug.Log("进入游戏世界...");
    }
    
    private void UpdateGameLogic(float deltaTime)
    {
        // 更新游戏逻辑
        // Debug.Log($"更新游戏逻辑，时间增量: {deltaTime}");
    }
    
    private void ExitGameWorld()
    {
        // 退出游戏世界
        Debug.Log("退出游戏世界...");
    }
    
    private bool ShouldExitGame()
    {
        // 模拟退出条件
        return CurrentProcedureTime > 5f; // 5秒后退出游戏
    }
}

// 退出流程
public class ExitProcedure : ProcedureBase
{
    public override int Priority => 15; // 最高优先级（退出流程）
    
    protected override void OnEnter(object userData)
    {
        base.OnEnter(userData);
        Debug.Log("进入退出流程");
        
        // 执行退出逻辑
        PerformExitOperations();
    }
    
    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
        
        // 退出流程完成后关闭应用
        if (IsExitComplete())
        {
            Application.Quit();
        }
    }
    
    protected override void OnLeave(bool isShutdown)
    {
        base.OnLeave(isShutdown);
        Debug.Log("离开退出流程");
    }
    
    private void PerformExitOperations()
    {
        // 执行退出操作
        Debug.Log("执行退出操作...");
        SaveGameData();
        CleanupResources();
    }
    
    private void SaveGameData()
    {
        // 保存游戏数据
        Debug.Log("保存游戏数据...");
    }
    
    private void CleanupResources()
    {
        // 清理资源
        Debug.Log("清理资源...");
    }
    
    private bool IsExitComplete()
    {
        // 检查退出是否完成
        return CurrentProcedureTime > 1f; // 1秒后退出完成
    }
}
```

#### 初始化流程管理器

```csharp
using FuFramework.Procedure.Runtime;

public class GameApplication : MonoBehaviour
{
    private ProcedureModule m_ProcedureModule;
    
    private void Start()
    {
        // 获取流程管理器
        m_ProcedureModule = ModuleManager.GetModule<ProcedureModule>();
        
        // 创建流程数组
        ProcedureBase[] procedures = new ProcedureBase[]
        {
            new LaunchProcedure(),
            new LoginProcedure(),
            new GameProcedure(),
            new ExitProcedure()
        };
        
        // 初始化流程
        m_ProcedureModule.InitProcedures(procedures);
        
        // 启动第一个流程
        m_ProcedureModule.StartProcedure<LaunchProcedure>();
    }
    
    private void Update()
    {
        // 监控当前流程状态
        if (m_ProcedureModule != null && m_ProcedureModule.CurrentProcedure != null)
        {
            string currentProcedure = m_ProcedureModule.CurrentProcedure.GetType().Name;
            float procedureTime = m_ProcedureModule.CurrentProcedureTime;
            
            // Debug.Log($"当前流程: {currentProcedure}, 持续时间: {procedureTime:F2}秒");
        }
    }
}
```

### 2. 流程管理操作

#### 流程状态查询

```csharp
// 检查流程是否存在
bool hasLaunchProcedure = m_ProcedureModule.HasProcedure<LaunchProcedure>();
bool hasLoginProcedure = m_ProcedureModule.HasProcedure(typeof(LoginProcedure));

// 获取特定流程
var launchProcedure = m_ProcedureModule.GetProcedure<LaunchProcedure>();
var loginProcedure = m_ProcedureModule.GetProcedure(typeof(LoginProcedure));

// 获取当前流程信息
var currentProcedure = m_ProcedureModule.CurrentProcedure;
var currentProcedureTime = m_ProcedureModule.CurrentProcedureTime;

if (currentProcedure != null)
{
    Debug.Log($"当前流程: {currentProcedure.GetType().Name}");
    Debug.Log($"流程优先级: {currentProcedure.Priority}");
    Debug.Log($"流程持续时间: {currentProcedureTime:F2}秒");
}
```

#### 流程切换控制

```csharp
// 直接切换到指定流程
m_ProcedureModule.StartProcedure<GameProcedure>();

// 使用类型切换流程
m_ProcedureModule.StartProcedure(typeof(ExitProcedure));

// 在流程内部切换（推荐方式）
public class CustomProcedure : ProcedureBase
{
    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
        
        // 条件满足时切换到下一个流程
        if (ShouldChangeToNextProcedure())
        {
            ChangeProcedure<NextProcedure>();
        }
    }
    
    private bool ShouldChangeToNextProcedure()
    {
        // 自定义切换条件
        return CurrentProcedureTime > 10f;
    }
}
```

### 3. 高级用法

#### 条件流程切换

```csharp
// 基于条件的流程管理
public class ConditionalProcedureModule
{
    private ProcedureModule m_ProcedureModule;
    
    public void Initialize()
    {
        m_ProcedureModule = ModuleManager.GetModule<ProcedureModule>();
    }
    
    // 根据游戏状态切换流程
    public void ChangeProcedureBasedOnGameState(GameState gameState)
    {
        switch (gameState)
        {
            case GameState.Launch:
                m_ProcedureModule.StartProcedure<LaunchProcedure>();
                break;
            case GameState.Login:
                m_ProcedureModule.StartProcedure<LoginProcedure>();
                break;
            case GameState.MainMenu:
                m_ProcedureModule.StartProcedure<MainMenuProcedure>();
                break;
            case GameState.Gameplay:
                m_ProcedureModule.StartProcedure<GameplayProcedure>();
                break;
            case GameState.Paused:
                m_ProcedureModule.StartProcedure<PauseProcedure>();
                break;
            case GameState.Exit:
                m_ProcedureModule.StartProcedure<ExitProcedure>();
                break;
        }
    }
    
    // 根据网络状态切换流程
    public void ChangeProcedureBasedOnNetworkStatus(bool isConnected)
    {
        if (isConnected)
        {
            m_ProcedureModule.StartProcedure<OnlineProcedure>();
        }
        else
        {
            m_ProcedureModule.StartProcedure<OfflineProcedure>();
        }
    }
}

// 游戏状态枚举
public enum GameState
{
    Launch,
    Login,
    MainMenu,
    Gameplay,
    Paused,
    Exit
}
```

#### 异步流程管理

```csharp
// 支持异步操作的流程
public class AsyncProcedure : ProcedureBase
{
    private bool m_IsLoadingComplete = false;
    private AsyncOperation m_LoadingOperation;
    
    protected override void OnEnter(object userData)
    {
        base.OnEnter(userData);
        
        // 开始异步加载
        StartAsyncLoading();
    }
    
    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
        
        // 检查异步操作状态
        if (m_LoadingOperation != null && m_LoadingOperation.isDone)
        {
            m_IsLoadingComplete = true;
        }
        
        // 加载完成后切换到下一个流程
        if (m_IsLoadingComplete)
        {
            ChangeProcedure<NextProcedure>();
        }
    }
    
    protected override void OnLeave(bool isShutdown)
    {
        base.OnLeave(isShutdown);
        
        // 清理异步操作
        m_LoadingOperation = null;
        m_IsLoadingComplete = false;
    }
    
    private void StartAsyncLoading()
    {
        // 开始异步场景加载
        m_LoadingOperation = SceneModule.LoadSceneAsync("GameScene");
        m_LoadingOperation.allowSceneActivation = false;
        
        // 监听加载进度
        StartCoroutine(MonitorLoadingProgress());
    }
    
    private System.Collections.IEnumerator MonitorLoadingProgress()
    {
        while (!m_LoadingOperation.isDone)
        {
            float progress = Mathf.Clamp01(m_LoadingOperation.progress / 0.9f);
            Debug.Log($"加载进度: {progress:P0}");
            
            if (progress >= 0.9f)
            {
                m_LoadingOperation.allowSceneActivation = true;
            }
            
            yield return null;
        }
    }
}
```

#### 流程事件系统

```csharp
// 流程事件管理器
public class ProcedureEventManager
{
    public event System.Action<ProcedureBase> OnProcedureEnter;
    public event System.Action<ProcedureBase, float> OnProcedureUpdate;
    public event System.Action<ProcedureBase, bool> OnProcedureLeave;
    public event System.Action<ProcedureBase, ProcedureBase> OnProcedureChange;
    
    private ProcedureBase m_CurrentProcedure;
    private ProcedureBase m_PreviousProcedure;
    
    public void Initialize()
    {
        var procedureModule = ModuleManager.GetModule<ProcedureModule>();
        
        // 监听流程变化
        // 注意：这里需要扩展 ProcedureModule 来支持事件通知
        // 或者通过自定义方式监控流程变化
    }
    
    public void NotifyProcedureEnter(ProcedureBase procedure)
    {
        OnProcedureEnter?.Invoke(procedure);
        m_PreviousProcedure = m_CurrentProcedure;
        m_CurrentProcedure = procedure;
        
        if (m_PreviousProcedure != null && m_CurrentProcedure != null)
        {
            OnProcedureChange?.Invoke(m_PreviousProcedure, m_CurrentProcedure);
        }
    }
    
    public void NotifyProcedureUpdate(ProcedureBase procedure, float deltaTime)
    {
        OnProcedureUpdate?.Invoke(procedure, deltaTime);
    }
    
    public void NotifyProcedureLeave(ProcedureBase procedure, bool isShutdown)
    {
        OnProcedureLeave?.Invoke(procedure, isShutdown);
    }
}

// 使用事件系统的流程
public class EventAwareProcedure : ProcedureBase
{
    private ProcedureEventManager m_EventManager;
    
    protected override void OnEnter(object userData)
    {
        base.OnEnter(userData);
        
        // 通知进入事件
        m_EventManager?.NotifyProcedureEnter(this);
    }
    
    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
        
        // 通知更新事件
        m_EventManager?.NotifyProcedureUpdate(this, elapseSeconds);
    }
    
    protected override void OnLeave(bool isShutdown)
    {
        base.OnLeave(isShutdown);
        
        // 通知离开事件
        m_EventManager?.NotifyProcedureLeave(this, isShutdown);
    }
}
```

## 性能优化建议

### 1. 流程资源管理

```csharp
// 智能资源加载和释放
public class ResourceAwareProcedure : ProcedureBase
{
    private List<UnityEngine.Object> m_LoadedResources = new List<UnityEngine.Object>();
    
    protected override void OnEnter(object userData)
    {
        base.OnEnter(userData);
        
        // 只加载当前流程需要的资源
        LoadRequiredResources();
    }
    
    protected override void OnLeave(bool isShutdown)
    {
        base.OnLeave(isShutdown);
        
        // 释放当前流程的资源
        ReleaseResources();
    }
    
    private void LoadRequiredResources()
    {
        // 加载流程特定资源
        var resource = Resources.Load<GameObject>("ProcedureSpecificPrefab");
        if (resource != null)
        {
            m_LoadedResources.Add(resource);
        }
    }
    
    private void ReleaseResources()
    {
        foreach (var resource in m_LoadedResources)
        {
            if (resource != null)
            {
                Resources.UnloadAsset(resource);
            }
        }
        m_LoadedResources.Clear();
    }
}
```

### 2. 流程优先级优化

```csharp
// 根据系统负载动态调整流程优先级
public class AdaptivePriorityProcedure : ProcedureBase
{
    public override int Priority 
    {
        get 
        {
            // 根据系统性能动态调整优先级
            if (SystemInfo.systemMemorySize < 4000) // 内存小于4GB
            {
                return 20; // 提高优先级，尽快完成
            }
            return base.Priority;
        }
    }
}
```

### 3. 流程状态缓存

```csharp
// 缓存流程状态，避免重复计算
public class StateCachedProcedure : ProcedureBase
{
    private Dictionary<string, object> m_StateCache = new Dictionary<string, object>();
    
    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
        
        // 使用缓存状态
        var cachedState = GetCachedState("some_complex_calculation");
        if (cachedState == null)
        {
            // 计算并缓存状态
            cachedState = PerformComplexCalculation();
            SetCachedState("some_complex_calculation", cachedState);
        }
        
        // 使用缓存的状态
        UseCachedState(cachedState);
    }
    
    private object GetCachedState(string key)
    {
        if (m_StateCache.TryGetValue(key, out var value))
        {
            return value;
        }
        return null;
    }
    
    private void SetCachedState(string key, object value)
    {
        m_StateCache[key] = value;
    }
    
    protected override void OnLeave(bool isShutdown)
    {
        base.OnLeave(isShutdown);
        
        // 清理缓存
        m_StateCache.Clear();
    }
}
```

## 注意事项

### 1. 流程生命周期管理

- 确保在 `OnEnter` 中正确初始化流程状态
- 在 `OnUpdate` 中处理流程逻辑和状态转换条件
- 在 `OnLeave` 中彻底清理流程资源
- 避免在流程生命周期方法中进行耗时操作

### 2. 状态转换安全

- 确保流程切换的条件判断准确
- 避免在流程切换过程中出现资源竞争
- 处理流程切换失败的情况
- 考虑流程切换的动画或过渡效果

### 3. 内存管理

- 及时释放不再使用的流程资源
- 监控流程的内存使用情况
- 避免在流程中创建大量临时对象
- 使用对象池管理频繁创建销毁的对象

### 4. 错误处理

```csharp
// 安全的流程操作
public class SafeProcedureManager
{
    public bool TryChangeProcedure<T>() where T : ProcedureBase
    {
        try
        {
            var procedureModule = ModuleManager.GetModule<ProcedureModule>();
            
            if (procedureModule == null)
            {
                Debug.LogError("流程管理器未找到");
                return false;
            }
            
            if (!procedureModule.HasProcedure<T>())
            {
                Debug.LogError($"流程 {typeof(T).Name} 不存在");
                return false;
            }
            
            procedureModule.StartProcedure<T>();
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"流程切换失败: {ex.Message}");
            return false;
        }
    }
}
```

## API 参考

### ProcedureModule 主要方法

| 方法 | 说明 | 参数 | 返回值 |
|------|------|------|--------|
| `InitProcedures` | 初始化流程状态机 | `ProcedureBase[] procedure` | `void` |
| `StartProcedure<T>` | 开始指定类型的流程 | 无 | `void` |
| `StartProcedure` | 开始指定类型的流程 | `Type procedureType` | `void` |
| `HasProcedure<T>` | 检查流程是否存在 | 无 | `bool` |
| `HasProcedure` | 检查流程是否存在 | `Type procedureType` | `bool` |
| `GetProcedure<T>` | 获取指定类型的流程 | 无 | `ProcedureBase` |
| `GetProcedure` | 获取指定类型的流程 | `Type procedureType` | `ProcedureBase` |

### ProcedureBase 主要方法

| 方法 | 说明 | 参数 | 返回值 |
|------|------|------|--------|
| `OnInit` | 流程初始化时调用 | `Fsm procedureOwner` | `void` |
| `OnEnter` | 进入流程时调用 | `object userData` | `void` |
| `OnUpdate` | 流程更新时调用 | `float elapseSeconds, float realElapseSeconds` | `void` |
| `OnLeave` | 离开流程时调用 | `bool isShutdown` | `void` |
| `ChangeProcedure<T>` | 切换到指定流程 | 无 | `void` |

## 示例项目

参考 FuFramework 示例项目中的流程管理示例，了解完整的使用场景和最佳实践。

---

**注意：** 本模块需要依赖 Fsm 模块进行状态管理，请确保 Fsm 模块已正确初始化。