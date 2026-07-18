# FuFramework Mono Module

## 1. 简介

FuFramework Mono 模块是游戏框架的 MonoBehaviour 生命周期事件管理系统，提供了一种统一、安全的方式来管理 Unity 游戏对象的生命周期事件。该模块通过事件监听器机制，让非 MonoBehaviour 对象也能够响应 Unity 的生命周期事件。

## 2. 核心特性

- **生命周期事件管理**：统一管理 Update、FixedUpdate、LateUpdate、OnDestroy 等生命周期事件
- **应用程序事件支持**：处理应用程序暂停、恢复、焦点变化等系统事件
- **线程安全**：使用双缓冲队列机制确保多线程环境下的操作安全
- **错误处理**：完善的异常捕获和日志记录机制
- **高性能**：通过队列交换避免执行时的竞态条件

## 3. 核心概念

### 3.1 双缓冲队列机制

模块采用双缓冲队列设计，确保在多线程环境下安全执行回调函数：

```
等待队列 (WaitList)     执行队列 (DoingList)
     ↓                        ↓
  收集新回调              执行当前帧回调
     ↓                        ↓
     └────── 每帧交换 ────────┘
```

**工作流程**：
1. **等待队列**：收集新添加的监听器
2. **执行队列**：执行当前帧的回调函数
3. **队列交换**：每帧开始时交换两个队列的引用

### 3.2 生命周期事件类型

| 事件类型 | 执行频率 | 典型用途 |
|---------|---------|---------|
| Update | 每帧 | 游戏逻辑、输入处理 |
| FixedUpdate | 固定时间间隔 | 物理模拟、刚体运动 |
| LateUpdate | 每帧（Update后） | 相机跟随、后期处理 |
| OnDestroy | 销毁时 | 资源释放、数据保存 |
| OnApplicationPause | 暂停/恢复时 | 游戏状态管理 |
| OnApplicationFocus | 焦点变化时 | 音频控制、网络管理 |

## 4. 核心类详细说明

### 4.1 MonoModule

MonoBehaviour 生命周期管理器，继承自 `ModuleBase`。

**核心功能**：

```csharp
public class MonoModule : ModuleBase
{
    // 生命周期监听管理
    public void AddUpdateListener(Action action)           // 添加 Update 监听
    public void RemoveUpdateListener(Action action)        // 移除 Update 监听
    public void AddFixedUpdateListener(Action action)      // 添加 FixedUpdate 监听
    public void RemoveFixedUpdateListener(Action action)   // 移除 FixedUpdate 监听
    public void AddLateUpdateListener(Action action)       // 添加 LateUpdate 监听
    public void RemoveLateUpdateListener(Action action)    // 移除 LateUpdate 监听
    public void AddDestroyListener(Action action)          // 添加 Destroy 监听
    public void RemoveDestroyListener(Action action)       // 移除 Destroy 监听
    
    // 应用程序事件监听
    public void AddOnApplicationPauseListener(Action<bool> action)   // 添加暂停监听
    public void RemoveOnApplicationPauseListener(Action<bool> action)// 移除暂停监听
    public void AddOnApplicationFocusListener(Action<bool> action)   // 添加焦点监听
    public void RemoveOnApplicationFocusListener(Action<bool> action)// 移除焦点监听
    
    // 应用程序事件触发（由 Launcher 调用）
    public void OnApplicationFocus(bool focusStatus)       // 焦点变化
    public void OnApplicationPause(bool pauseStatus)       // 暂停变化
}
```

**内部实现**：

```csharp
// 双缓冲队列
private readonly List<Action> m_WaitUpdateList = new();      // 等待执行的 Update 回调
private readonly List<Action> m_DoingUpdateList = new();     // 正在执行的 Update 回调
private readonly List<Action> m_WaitFixedUpdateList = new(); // 等待执行的 FixedUpdate 回调
private readonly List<Action> m_DoingFixedUpdateList = new();// 正在执行的 FixedUpdate 回调
private readonly List<Action> m_WaitLateUpdateList = new();  // 等待执行的 LateUpdate 回调
private readonly List<Action> m_DoingLateUpdateList = new(); // 正在执行的 LateUpdate 回调
private readonly List<Action> m_WaitDestroyList = new();     // 等待执行的 Destroy 回调
private readonly List<Action> m_DoingDestroyList = new();    // 正在执行的 Destroy 回调
private List<Action<bool>> m_WaitOnApplicationPauseList = new();  // 等待执行的暂停回调
private List<Action<bool>> m_DoOnApplicationPauseList = new();    // 正在执行的暂停回调
private List<Action<bool>> m_WaitOnApplicationFocusList = new();  // 等待执行的焦点回调
private List<Action<bool>> m_DoOnApplicationFocusList = new();    // 正在执行的焦点回调
```

**队列执行机制**：

```csharp
private static void QueueInvoking(List<Action> invokeList, List<Action> waitInvokeList)
{
    // 1. 交换队列引用
    Utility.Object.Swap(ref invokeList, ref waitInvokeList);
    
    // 2. 执行当前帧的回调
    foreach (var action in invokeList)
    {
        try
        {
            action.Invoke();
        }
        catch (Exception e)
        {
            FuLogger.LogError(e);  // 异常捕获，不影响其他监听器
        }
    }
}
```

**设计特点**：
- 使用 `.NotNull()` 扩展方法进行参数校验
- 异常捕获确保单个监听器失败不影响其他监听器
- 队列交换机制避免执行时的竞态条件
- 清理时自动清空所有队列

## 5. 使用示例

### 5.1 基础生命周期事件监听

```csharp
using FuFramework.Mono.Runtime;
using UnityEngine;

public class LifecycleExample : MonoBehaviour
{
    private void Start()
    {
        // 获取 Mono 管理器
        var monoModule = GlobalModule.MonoModule;
        
        // 添加 Update 监听器
        monoModule.AddUpdateListener(OnUpdate);
        
        // 添加 FixedUpdate 监听器
        monoModule.AddFixedUpdateListener(OnFixedUpdate);
        
        // 添加 LateUpdate 监听器
        monoModule.AddLateUpdateListener(OnLateUpdate);
        
        // 添加 Destroy 监听器
        monoModule.AddDestroyListener(OnDestroyed);
    }
    
    private void OnUpdate()
    {
        // 每帧执行的逻辑
        // 例如：玩家输入处理、游戏逻辑更新
        Debug.Log("Update 执行中...");
    }
    
    private void OnFixedUpdate()
    {
        // 固定时间间隔执行的物理逻辑
        // 例如：物理模拟、刚体运动
        Debug.Log("FixedUpdate 执行中...");
    }
    
    private void OnLateUpdate()
    {
        // 在所有 Update 之后执行的逻辑
        // 例如：相机跟随、后期处理
        Debug.Log("LateUpdate 执行中...");
    }
    
    private void OnDestroyed()
    {
        // 对象销毁时执行的清理逻辑
        // 例如：资源释放、数据保存
        Debug.Log("对象即将被销毁，执行清理操作...");
    }
    
    private void OnDestroy()
    {
        // 移除监听器（可选，管理器会自动清理）
        var monoModule = GlobalModule.MonoModule;
        monoModule.RemoveUpdateListener(OnUpdate);
        monoModule.RemoveFixedUpdateListener(OnFixedUpdate);
        monoModule.RemoveLateUpdateListener(OnLateUpdate);
        monoModule.RemoveDestroyListener(OnDestroyed);
    }
}
```

### 5.2 应用程序事件监听

```csharp
using FuFramework.Mono.Runtime;
using UnityEngine;

public class ApplicationEventExample : MonoBehaviour
{
    private void Start()
    {
        var monoModule = GlobalModule.MonoModule;
        
        // 添加应用程序暂停/恢复监听器
        monoModule.AddOnApplicationPauseListener(OnApplicationPauseChanged);
        
        // 添加应用程序焦点变化监听器
        monoModule.AddOnApplicationFocusListener(OnApplicationFocusChanged);
    }
    
    private void OnApplicationPauseChanged(bool isPaused)
    {
        if (isPaused)
        {
            // 应用程序进入后台
            Debug.Log("应用程序暂停，进入后台");
            
            // 暂停游戏逻辑
            Time.timeScale = 0f;
            
            // 保存游戏进度
            SaveGameProgress();
        }
        else
        {
            // 应用程序恢复
            Debug.Log("应用程序恢复，回到前台");
            
            // 恢复游戏逻辑
            Time.timeScale = 1f;
            
            // 恢复游戏状态
            RestoreGameState();
        }
    }
    
    private void OnApplicationFocusChanged(bool hasFocus)
    {
        if (hasFocus)
        {
            // 应用程序获得焦点
            Debug.Log("应用程序获得焦点");
            
            // 恢复音频播放
            AudioListener.pause = false;
            
            // 恢复网络连接
            ResumeNetworkConnection();
        }
        else
        {
            // 应用程序失去焦点
            Debug.Log("应用程序失去焦点");
            
            // 暂停音频播放
            AudioListener.pause = true;
            
            // 暂停网络请求
            PauseNetworkRequests();
        }
    }
    
    private void SaveGameProgress()
    {
        // 保存游戏进度逻辑
        Debug.Log("保存游戏进度...");
    }
    
    private void RestoreGameState()
    {
        // 恢复游戏状态逻辑
        Debug.Log("恢复游戏状态...");
    }
    
    private void ResumeNetworkConnection()
    {
        // 恢复网络连接逻辑
        Debug.Log("恢复网络连接...");
    }
    
    private void PauseNetworkRequests()
    {
        // 暂停网络请求逻辑
        Debug.Log("暂停网络请求...");
    }
    
    private void OnDestroy()
    {
        // 移除监听器
        var monoModule = GlobalModule.MonoModule;
        monoModule.RemoveOnApplicationPauseListener(OnApplicationPauseChanged);
        monoModule.RemoveOnApplicationFocusListener(OnApplicationFocusChanged);
    }
}
```

### 5.3 非 MonoBehaviour 对象使用生命周期事件

```csharp
using FuFramework.Mono.Runtime;
using System;

// 非 MonoBehaviour 的业务逻辑类
public class GameLogicManager
{
    private bool isInitialized = false;
    private int frameCount = 0;
    
    public GameLogicManager()
    {
        Initialize();
    }
    
    private void Initialize()
    {
        if (isInitialized) return;
        
        var monoModule = GlobalModule.MonoModule;
        
        // 注册生命周期事件监听器
        monoModule.AddUpdateListener(OnUpdate);
        monoModule.AddFixedUpdateListener(OnFixedUpdate);
        monoModule.AddDestroyListener(OnDestroy);
        
        isInitialized = true;
        Console.WriteLine("GameLogicManager 初始化完成，已注册生命周期监听器");
    }
    
    private void OnUpdate()
    {
        frameCount++;
        
        // 执行游戏逻辑更新
        UpdateGameState();
        
        // 每100帧输出一次日志
        if (frameCount % 100 == 0)
        {
            Console.WriteLine($"GameLogicManager Update 执行第 {frameCount} 帧");
        }
    }
    
    private void OnFixedUpdate()
    {
        // 执行物理相关的逻辑
        UpdatePhysics();
    }
    
    private void OnDestroy()
    {
        // 清理资源
        Cleanup();
        Console.WriteLine("GameLogicManager 清理完成");
    }
    
    private void UpdateGameState()
    {
        // 游戏状态更新逻辑
        // 例如：AI 行为、游戏规则检查等
    }
    
    private void UpdatePhysics()
    {
        // 物理相关逻辑
        // 例如：碰撞检测、运动模拟等
    }
    
    private void Cleanup()
    {
        // 资源清理逻辑
        var monoModule = GlobalModule.MonoModule;
        monoModule.RemoveUpdateListener(OnUpdate);
        monoModule.RemoveFixedUpdateListener(OnFixedUpdate);
        monoModule.RemoveDestroyListener(OnDestroy);
        
        isInitialized = false;
    }
    
    // 手动销毁方法
    public void Dispose()
    {
        Cleanup();
    }
}

// 使用示例
public class GameController : MonoBehaviour
{
    private GameLogicManager gameLogic;
    
    private void Start()
    {
        // 创建非 MonoBehaviour 的游戏逻辑管理器
        gameLogic = new GameLogicManager();
    }
    
    private void OnDestroy()
    {
        // 手动清理非 MonoBehaviour 对象
        gameLogic?.Dispose();
    }
}
```

### 5.4 性能监控示例

```csharp
using FuFramework.Mono.Runtime;
using System.Diagnostics;

public class PerformanceMonitor
{
    private Stopwatch updateStopwatch = new Stopwatch();
    private long totalUpdateTime = 0;
    private int updateCount = 0;
    
    public PerformanceMonitor()
    {
        var monoModule = GlobalModule.MonoModule;
        monoModule.AddUpdateListener(OnUpdateWithTiming);
    }
    
    private void OnUpdateWithTiming()
    {
        updateStopwatch.Restart();
        
        // 执行实际的 Update 逻辑
        PerformUpdateLogic();
        
        updateStopwatch.Stop();
        totalUpdateTime += updateStopwatch.ElapsedMilliseconds;
        updateCount++;
        
        // 每60帧输出一次性能统计
        if (updateCount % 60 == 0)
        {
            double avgUpdateTime = (double)totalUpdateTime / updateCount;
            UnityEngine.Debug.Log($"Update 平均耗时: {avgUpdateTime:F2}ms");
            
            // 重置统计
            totalUpdateTime = 0;
            updateCount = 0;
        }
    }
    
    private void PerformUpdateLogic()
    {
        // 实际的 Update 逻辑
    }
}
```

## 6. 目录结构

```
FuFramework/Mono/
├── Runtime/
│   ├── FuFramework.Mono.Runtime.asmdef    # 运行时程序集定义
│   └── MonoModule.cs                       # Mono 生命周期管理模块
└── README.md                               # 模块文档
```

## 7. 依赖

- **FuFramework.Core**：框架核心模块（ModuleBase、FuGuardEx、Utility、FuLogger）

## 8. 最佳实践

### 8.1 监听器管理原则

```csharp
public class BestPracticeExample : MonoBehaviour
{
    private Action m_UpdateAction;
    private Action m_FixedUpdateAction;
    
    private void Awake()
    {
        // 预先创建委托实例，避免重复分配
        m_UpdateAction = OnUpdate;
        m_FixedUpdateAction = OnFixedUpdate;
    }
    
    private void OnEnable()
    {
        // 在启用时添加监听器
        var monoModule = GlobalModule.MonoModule;
        monoModule.AddUpdateListener(m_UpdateAction);
        monoModule.AddFixedUpdateListener(m_FixedUpdateAction);
    }
    
    private void OnDisable()
    {
        // 在禁用时移除监听器
        var monoModule = GlobalModule.MonoModule;
        monoModule.RemoveUpdateListener(m_UpdateAction);
        monoModule.RemoveFixedUpdateListener(m_FixedUpdateAction);
    }
    
    private void OnUpdate()
    {
        // Update 逻辑
    }
    
    private void OnFixedUpdate()
    {
        // FixedUpdate 逻辑
    }
}
```

### 8.2 条件执行优化

```csharp
public class ConditionalExecution : MonoBehaviour
{
    private bool m_IsActive = true;
    
    private void Start()
    {
        GlobalModule.MonoModule.AddUpdateListener(OnUpdate);
    }
    
    private void OnUpdate()
    {
        // 快速返回，减少不必要的计算
        if (!m_IsActive) return;
        
        // 执行逻辑
    }
    
    public void SetActive(bool active)
    {
        m_IsActive = active;
    }
}
```

### 8.3 批量操作优化

```csharp
public class BatchOperationExample : MonoBehaviour
{
    private List<System.Action> m_PendingActions = new List<System.Action>();
    
    private void Start()
    {
        GlobalModule.MonoModule.AddUpdateListener(ProcessPendingActions);
    }
    
    private void ProcessPendingActions()
    {
        // 批量处理待执行的操作
        foreach (var action in m_PendingActions)
        {
            action?.Invoke();
        }
        m_PendingActions.Clear();
    }
    
    public void QueueAction(System.Action action)
    {
        m_PendingActions.Add(action);
    }
}
```

## 9. 注意事项

1. **空值检查**
   - 模块内部使用 `.NotNull()` 扩展方法进行参数校验
   - 传入 null 会抛出异常

2. **异常处理**
   - 监听器中的异常会被捕获并记录
   - 单个监听器失败不会影响其他监听器
   - 建议监听器内部自行处理异常

3. **线程安全**
   - 监听器的添加和移除操作是线程安全的
   - 但监听器内部的逻辑需要自行保证线程安全
   - 使用锁机制保护共享资源的访问

4. **执行顺序**
   - 监听器的执行顺序与添加顺序相关
   - 重要逻辑应该放在靠前的位置执行
   - 避免监听器之间的循环依赖

5. **内存管理**
   - 及时移除不再需要的监听器
   - 避免监听器持有对已销毁对象的引用
   - 使用弱引用或事件解耦机制

6. **生命周期管理**
   - 确保在对象销毁时移除相关监听器
   - 使用 `OnDisable` 或 `OnDestroy` 进行清理
   - 非 MonoBehaviour 对象需要手动管理生命周期
