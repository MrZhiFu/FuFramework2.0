# FuFramework Mono Module

## 简介
FuFramework Mono 模块是游戏框架的 MonoBehaviour 生命周期事件管理系统，提供了一种统一、安全的方式来管理 Unity 游戏对象的生命周期事件。该模块通过事件监听器机制，让非 MonoBehaviour 对象也能够响应 Unity 的生命周期事件。

## 核心特性

- **生命周期事件管理**：统一管理 Update、FixedUpdate、LateUpdate、OnDestroy 等生命周期事件
- **应用程序事件支持**：处理应用程序暂停、恢复、焦点变化等系统事件
- **线程安全**：使用锁机制确保多线程环境下的操作安全
- **事件驱动架构**：与事件系统深度集成，支持事件广播
- **性能优化**：使用双缓冲队列避免竞态条件，提高执行效率
- **错误处理**：完善的异常捕获和日志记录机制

## 核心类说明

### MonoManager
MonoBehaviour 生命周期管理器，继承自 `FuModule`。
- **职责**：
  1. 管理所有生命周期事件的监听器列表
  2. 提供线程安全的监听器添加和移除接口
  3. 实现双缓冲队列机制确保执行安全
  4. 与事件系统集成，广播应用程序事件

### OnApplicationFocusChangedEventArgs
应用程序焦点变化事件参数类，继承自 `GameEventArgs`。
- **职责**：
  1. 封装应用程序焦点状态变化信息
  2. 提供事件标识符和对象池支持
  3. 支持事件系统的序列化和反序列化

### OnApplicationPauseChangedEventArgs
应用程序暂停状态变化事件参数类，继承自 `GameEventArgs`。
- **职责**：
  1. 封装应用程序暂停状态变化信息
  2. 提供事件标识符和对象池支持
  3. 支持事件系统的序列化和反序列化

## 技术架构

### 依赖关系
- **FuFramework.Core**：基础框架模块（FuModule、FuGuard、Utility）
- **FuFramework.Event**：事件管理模块（EventManager、GameEventArgs）
- **ReferencePool**：对象池管理系统

### 模块优先级
MonoManager 的优先级为 `ModulePriority.Game`，确保在游戏逻辑模块中正确初始化。

### 双缓冲队列机制
模块采用双缓冲队列设计，确保在多线程环境下安全执行回调函数：
1. **等待队列**：收集新添加的监听器
2. **执行队列**：执行当前帧的回调函数
3. **队列交换**：每帧开始时交换两个队列的引用

## 使用指南

### 1. 基础生命周期事件监听
```csharp
using FuFramework.Mono.Runtime;
using UnityEngine;

public class LifecycleExample : MonoBehaviour
{
    private void Start()
    {
        // 获取 Mono 管理器
        var monoManager = GlobalModule.MonoModule;
        
        // 添加 Update 监听器
        monoManager.AddUpdateListener(OnUpdate);
        
        // 添加 FixedUpdate 监听器
        monoManager.AddFixedUpdateListener(OnFixedUpdate);
        
        // 添加 LateUpdate 监听器
        monoManager.AddLateUpdateListener(OnLateUpdate);
        
        // 添加 Destroy 监听器
        monoManager.AddDestroyListener(OnDestroyed);
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
        var monoManager = GlobalModule.MonoModule;
        monoManager.RemoveUpdateListener(OnUpdate);
        monoManager.RemoveFixedUpdateListener(OnFixedUpdate);
        monoManager.RemoveLateUpdateListener(OnLateUpdate);
        monoManager.RemoveDestroyListener(OnDestroyed);
    }
}
```

### 2. 应用程序事件监听
```csharp
using FuFramework.Mono.Runtime;
using UnityEngine;

public class ApplicationEventExample : MonoBehaviour
{
    private void Start()
    {
        var monoManager = GlobalModule.MonoModule;
        
        // 添加应用程序暂停/恢复监听器
        monoManager.AddOnApplicationPauseListener(OnApplicationPauseChanged);
        
        // 添加应用程序焦点变化监听器
        monoManager.AddOnApplicationFocusListener(OnApplicationFocusChanged);
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
        var monoManager = GlobalModule.MonoModule;
        monoManager.RemoveOnApplicationPauseListener(OnApplicationPauseChanged);
        monoManager.RemoveOnApplicationFocusListener(OnApplicationFocusChanged);
    }
}
```

### 3. 事件系统集成使用
```csharp
using FuFramework.Mono.Runtime;
using FuFramework.Event.Runtime;
using UnityEngine;

public class EventSystemIntegration : MonoBehaviour
{
    private EventManager eventManager;
    
    private void Start()
    {
        eventManager = GlobalModule.EventModule;
        
        // 订阅应用程序焦点变化事件
        eventManager.Subscribe<OnApplicationFocusChangedEventArgs>(
            OnApplicationFocusChangedEventArgs.EventId, 
            OnAppFocusChanged);
        
        // 订阅应用程序暂停变化事件
        eventManager.Subscribe<OnApplicationPauseChangedEventArgs>(
            OnApplicationPauseChangedEventArgs.EventId, 
            OnAppPauseChanged);
    }
    
    private void OnAppFocusChanged(object sender, GameEventArgs e)
    {
        if (e is OnApplicationFocusChangedEventArgs focusArgs)
        {
            Debug.Log($"通过事件系统：应用程序焦点变化 - 是否有焦点: {focusArgs.IsFocus}");
            
            // 根据焦点状态执行相应逻辑
            if (focusArgs.IsFocus)
            {
                OnApplicationGainedFocus();
            }
            else
            {
                OnApplicationLostFocus();
            }
        }
    }
    
    private void OnAppPauseChanged(object sender, GameEventArgs e)
    {
        if (e is OnApplicationPauseChangedEventArgs pauseArgs)
        {
            Debug.Log($"通过事件系统：应用程序暂停变化 - 是否暂停: {pauseArgs.IsPause}");
            
            // 根据暂停状态执行相应逻辑
            if (pauseArgs.IsPause)
            {
                OnApplicationPaused();
            }
            else
            {
                OnApplicationResumed();
            }
        }
    }
    
    private void OnApplicationGainedFocus()
    {
        // 应用程序获得焦点的处理逻辑
        Debug.Log("应用程序获得焦点，恢复交互...");
    }
    
    private void OnApplicationLostFocus()
    {
        // 应用程序失去焦点的处理逻辑
        Debug.Log("应用程序失去焦点，暂停非关键操作...");
    }
    
    private void OnApplicationPaused()
    {
        // 应用程序暂停的处理逻辑
        Debug.Log("应用程序暂停，保存状态...");
    }
    
    private void OnApplicationResumed()
    {
        // 应用程序恢复的处理逻辑
        Debug.Log("应用程序恢复，重新初始化...");
    }
    
    private void OnDestroy()
    {
        // 取消订阅事件
        if (eventManager != null)
        {
            eventManager.Unsubscribe<OnApplicationFocusChangedEventArgs>(
                OnApplicationFocusChangedEventArgs.EventId, 
                OnAppFocusChanged);
            
            eventManager.Unsubscribe<OnApplicationPauseChangedEventArgs>(
                OnApplicationPauseChangedEventArgs.EventId, 
                OnAppPauseChanged);
        }
    }
}
```

### 4. 非 MonoBehaviour 对象使用生命周期事件
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
        
        var monoManager = GlobalModule.MonoModule;
        
        // 注册生命周期事件监听器
        monoManager.AddUpdateListener(OnUpdate);
        monoManager.AddFixedUpdateListener(OnFixedUpdate);
        monoManager.AddDestroyListener(OnDestroy);
        
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
        var monoManager = GlobalModule.MonoModule;
        monoManager.RemoveUpdateListener(OnUpdate);
        monoManager.RemoveFixedUpdateListener(OnFixedUpdate);
        monoManager.RemoveDestroyListener(OnDestroy);
        
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

## 高级用法

### 1. 性能监控和统计
```csharp
using FuFramework.Mono.Runtime;
using System;
using System.Diagnostics;

public class PerformanceMonitor
{
    private Stopwatch updateStopwatch = new Stopwatch();
    private Stopwatch fixedUpdateStopwatch = new Stopwatch();
    private long totalUpdateTime = 0;
    private long totalFixedUpdateTime = 0;
    private int updateCount = 0;
    private int fixedUpdateCount = 0;
    
    public PerformanceMonitor()
    {
        var monoManager = GlobalModule.MonoModule;
        
        monoManager.AddUpdateListener(OnUpdateWithTiming);
        monoManager.AddFixedUpdateListener(OnFixedUpdateWithTiming);
        monoManager.AddLateUpdateListener(OnLateUpdate);
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
            Console.WriteLine($"Update 平均耗时: {avgUpdateTime:F2}ms, 总帧数: {updateCount}");
            
            // 重置统计
            totalUpdateTime = 0;
            updateCount = 0;
        }
    }
    
    private void OnFixedUpdateWithTiming()
    {
        fixedUpdateStopwatch.Restart();
        
        // 执行实际的 FixedUpdate 逻辑
        PerformFixedUpdateLogic();
        
        fixedUpdateStopwatch.Stop();
        totalFixedUpdateTime += fixedUpdateStopwatch.ElapsedMilliseconds;
        fixedUpdateCount++;
        
        // 每50次 FixedUpdate 输出一次统计
        if (fixedUpdateCount % 50 == 0)
        {
            double avgFixedUpdateTime = (double)totalFixedUpdateTime / fixedUpdateCount;
            Console.WriteLine($"FixedUpdate 平均耗时: {avgFixedUpdateTime:F2}ms, 总次数: {fixedUpdateCount}");
            
            // 重置统计
            totalFixedUpdateTime = 0;
            fixedUpdateCount = 0;
        }
    }
    
    private void OnLateUpdate()
    {
        // LateUpdate 逻辑
        PerformLateUpdateLogic();
    }
    
    private void PerformUpdateLogic()
    {
        // 实际的 Update 逻辑
    }
    
    private void PerformFixedUpdateLogic()
    {
        // 实际的 FixedUpdate 逻辑
    }
    
    private void PerformLateUpdateLogic()
    {
        // 实际的 LateUpdate 逻辑
    }
}
```

### 2. 条件性事件监听
```csharp
using FuFramework.Mono.Runtime;
using UnityEngine;

public class ConditionalEventListener : MonoBehaviour
{
    private bool enableUpdate = true;
    private bool enableFixedUpdate = false;
    
    private void Start()
    {
        var monoManager = GlobalModule.MonoModule;
        
        // 添加条件性 Update 监听
        monoManager.AddUpdateListener(ConditionalUpdate);
        
        // 添加条件性 FixedUpdate 监听
        monoManager.AddFixedUpdateListener(ConditionalFixedUpdate);
    }
    
    private void ConditionalUpdate()
    {
        if (!enableUpdate) return;
        
        // 只有在 enableUpdate 为 true 时执行
        Debug.Log("条件性 Update 执行中...");
        
        // 可以根据条件动态启用/禁用
        if (Input.GetKeyDown(KeyCode.Space))
        {
            enableUpdate = !enableUpdate;
            Debug.Log($"Update 监听已 {(enableUpdate ? "启用" : "禁用")}");
        }
    }
    
    private void ConditionalFixedUpdate()
    {
        if (!enableFixedUpdate) return;
        
        // 只有在 enableFixedUpdate 为 true 时执行
        Debug.Log("条件性 FixedUpdate 执行中...");
        
        // 可以根据条件动态启用/禁用
        if (Input.GetKeyDown(KeyCode.F))
        {
            enableFixedUpdate = !enableFixedUpdate;
            Debug.Log($"FixedUpdate 监听已 {(enableFixedUpdate ? "启用" : "禁用")}");
        }
    }
    
    // 外部控制方法
    public void EnableUpdateListening(bool enable)
    {
        enableUpdate = enable;
    }
    
    public void EnableFixedUpdateListening(bool enable)
    {
        enableFixedUpdate = enable;
    }
}
```

### 3. 多线程安全操作
```csharp
using FuFramework.Mono.Runtime;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class ThreadSafeOperations : MonoBehaviour
{
    private int sharedCounter = 0;
    private object counterLock = new object();
    
    private void Start()
    {
        var monoManager = GlobalModule.MonoModule;
        
        // 在主线程中注册 Update 监听
        monoManager.AddUpdateListener(ThreadSafeUpdate);
        
        // 启动后台任务
        StartBackgroundTask();
    }
    
    private void ThreadSafeUpdate()
    {
        // 在主线程中安全地访问共享资源
        lock (counterLock)
        {
            sharedCounter++;
            
            // 每100帧输出一次计数
            if (sharedCounter % 100 == 0)
            {
                Debug.Log($"主线程计数: {sharedCounter}");
            }
        }
    }
    
    private async void StartBackgroundTask()
    {
        await Task.Run(() =>
        {
            // 在后台线程中执行耗时操作
            for (int i = 0; i < 1000; i++)
            {
                // 安全地修改共享资源
                lock (counterLock)
                {
                    sharedCounter += 10; // 后台线程增加更多
                }
                
                Thread.Sleep(10); // 模拟耗时操作
                
                // 每100次输出一次日志
                if (i % 100 == 0)
                {
                    Debug.Log($"后台任务执行第 {i} 次");
                }
            }
        });
        
        Debug.Log("后台任务完成");
    }
    
    // 安全的资源访问方法
    public int GetSafeCounter()
    {
        lock (counterLock)
        {
            return sharedCounter;
        }
    }
    
    public void ResetCounter()
    {
        lock (counterLock)
        {
            sharedCounter = 0;
        }
    }
}
```

## 性能优化建议

### 1. 监听器管理优化
- 及时移除不再需要的监听器
- 避免在频繁调用的方法中添加/移除监听器
- 对监听器进行分组管理，按需启用/禁用

### 2. 执行效率优化
- 减少监听器中的复杂计算
- 使用条件判断避免不必要的执行
- 对耗时操作进行分帧处理

### 3. 内存管理优化
- 使用对象池管理事件参数
- 避免在监听器中创建临时对象
- 及时清理不再使用的引用

## 注意事项

### 1. 线程安全
- 监听器的添加和移除操作是线程安全的
- 但监听器内部的逻辑需要自行保证线程安全
- 使用锁机制保护共享资源的访问

### 2. 执行顺序
- 监听器的执行顺序与添加顺序相关
- 重要逻辑应该放在靠前的位置执行
- 避免监听器之间的依赖关系

### 3. 错误处理
- 监听器中的异常会被捕获并记录
- 但异常不会影响其他监听器的执行
- 应该在监听器内部处理可能的异常

### 4. 生命周期管理
- 确保在对象销毁时移除相关监听器
- 避免监听器持有对已销毁对象的引用
- 使用弱引用或事件解耦机制

## 依赖模块

- **FuFramework.Core**：基础框架模块
- **FuFramework.Event**：事件管理模块
- **ReferencePool**：对象池管理系统

## 技术支持

如遇到 Mono 模块相关问题，请检查：
1. 监听器是否正确添加和移除
2. 多线程环境下的线程安全问题
3. 监听器执行顺序是否符合预期
4. 异常处理是否完善
5. 内存管理是否合理