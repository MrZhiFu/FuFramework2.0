# FuFramework TaskPool Module

## 概述

TaskPool 模块是 FuFramework 中的任务池管理系统，提供高效的任务调度、执行和管理功能。该模块基于任务代理模式，通过对象池技术实现任务代理的复用，减少内存分配开销，适用于需要频繁创建和销毁任务的游戏环境。

### 核心特性

- **任务池管理**：基于栈和链表的高效任务调度机制
- **任务代理模式**：分离任务定义和执行逻辑
- **优先级调度**：支持基于优先级的任务调度
- **状态监控**：完整的任务状态跟踪和信息查询
- **资源复用**：任务代理对象池，减少内存分配
- **异步支持**：适用于各种异步操作场景

## 系统架构

### 核心类说明

#### 1. TaskPool<T>
任务池核心类，负责任务的调度、执行和管理。

#### 2. TaskBase
任务基类，所有自定义任务必须继承此类。

#### 3. ITaskAgent<T>
任务代理接口，定义任务执行的具体逻辑。

#### 4. TaskInfo
任务信息结构体，包含任务的完整状态信息。

#### 5. TaskStatus
任务状态枚举：Todo（未开始）、Doing（执行中）、Done（完成）。

#### 6. StartTaskStatus
任务启动状态枚举：Done（完成）、CanResume（可恢复）、HasToWait（需等待）、UnknownError（未知错误）。

### 技术架构图

```
TaskPool<T>
├── m_FreeAgentStack (空闲代理栈)
├── m_WaitingTaskList (等待任务链表)
├── m_WorkingAgentList (工作代理链表)
└── Update() 方法
    ├── _ProcessRunningTasks() 处理运行中任务
    └── _ProcessWaitingTasks() 处理等待任务
```

## 快速开始

### 基本使用

#### 1. 定义自定义任务

```csharp
using FuFramework.TaskPool.Runtime;
using FuFramework.ReferencePool.Runtime;

// 自定义下载任务
public class DownloadTask : TaskBase
{
    public string Url { get; private set; }
    public string SavePath { get; private set; }
    public float Progress { get; set; }

    public void Initialize(int serialId, string url, string savePath, object userData = null)
    {
        base.Initialize(serialId, "Download", DefaultPriority, userData);
        Url = url;
        SavePath = savePath;
        Progress = 0f;
    }

    public override void Clear()
    {
        base.Clear();
        Url = null;
        SavePath = null;
        Progress = 0f;
    }

    public override string Description => $"下载任务: {Url} -> {SavePath}";
}
```

#### 2. 实现任务代理

```csharp
using System;
using FuFramework.TaskPool.Runtime;

// 下载任务代理
public class DownloadAgent : ITaskAgent<DownloadTask>
{
    public DownloadTask Task { get; private set; }
    
    public void Initialize()
    {
        // 初始化下载器或其他资源
    }

    public void Update(float elapseSeconds, float realElapseSeconds)
    {
        if (Task == null || Task.Done) return;

        // 模拟下载进度
        Task.Progress += elapseSeconds * 0.1f;
        if (Task.Progress >= 1f)
        {
            Task.Progress = 1f;
            Task.Done = true;
            Console.WriteLine($"下载完成: {Task.Url}");
        }
    }

    public void Shutdown()
    {
        // 清理资源
    }

    public StartTaskStatus Start(DownloadTask task)
    {
        Task = task;
        Console.WriteLine($"开始下载: {task.Url}");
        return StartTaskStatus.CanResume;
    }

    public void Reset()
    {
        Task = null;
    }
}
```

#### 3. 使用任务池

```csharp
using FuFramework.TaskPool.Runtime;
using UnityEngine;

public class TaskPoolExample : MonoBehaviour
{
    private TaskPool<DownloadTask> m_DownloadPool;
    private int m_SerialId = 0;

    private void Start()
    {
        // 创建下载任务池
        m_DownloadPool = new TaskPool<DownloadTask>();
        
        // 添加任务代理
        for (int i = 0; i < 3; i++)
        {
            m_DownloadPool.AddAgent(new DownloadAgent());
        }

        // 添加下载任务
        AddDownloadTask("https://example.com/file1.zip", "D:/Downloads/file1.zip");
        AddDownloadTask("https://example.com/file2.zip", "D:/Downloads/file2.zip");
        AddDownloadTask("https://example.com/file3.zip", "D:/Downloads/file3.zip");
    }

    private void Update()
    {
        // 更新任务池
        m_DownloadPool.Update(Time.deltaTime, Time.unscaledDeltaTime);
        
        // 监控任务状态
        MonitorTasks();
    }

    private void AddDownloadTask(string url, string savePath)
    {
        var task = ReferencePool.Runtime.ReferencePool.Acquire<DownloadTask>();
        task.Initialize(++m_SerialId, url, savePath);
        m_DownloadPool.AddTask(task);
    }

    private void MonitorTasks()
    {
        var taskInfos = m_DownloadPool.GetAllTaskInfos();
        foreach (var info in taskInfos)
        {
            Debug.Log($"任务 {info.SerialId}: {info.Description} - 状态: {info.Status}");
        }
    }

    private void OnDestroy()
    {
        m_DownloadPool?.Shutdown();
    }
}
```

## 详细使用指南

### 任务池管理

#### 1. 创建和配置任务池

```csharp
// 创建任务池
var taskPool = new TaskPool<CustomTask>();

// 设置暂停状态
taskPool.Paused = true; // 暂停任务处理
taskPool.Paused = false; // 恢复任务处理

// 添加任务代理
for (int i = 0; i < 5; i++)
{
    taskPool.AddAgent(new CustomAgent());
}
```

#### 2. 任务状态监控

```csharp
// 获取任务池统计信息
int totalAgents = taskPool.TotalAgentCount;
int freeAgents = taskPool.FreeAgentCount;
int workingAgents = taskPool.WorkingAgentCount;
int waitingTasks = taskPool.WaitingTaskCount;

Debug.Log($"总代理: {totalAgents}, 空闲: {freeAgents}, 工作中: {workingAgents}, 等待任务: {waitingTasks}");

// 获取特定任务信息
var taskInfo = taskPool.GetTaskInfo(1);
if (taskInfo.IsValid)
{
    Debug.Log($"任务 {taskInfo.SerialId}: {taskInfo.Description} - 优先级: {taskInfo.Priority}");
}

// 按标签获取任务信息
var downloadTasks = taskPool.GetTaskInfos("Download");
foreach (var info in downloadTasks)
{
    Debug.Log($"下载任务: {info.Description} - 状态: {info.Status}");
}
```

### 任务管理

#### 1. 添加任务

```csharp
// 创建任务并设置优先级
var task = ReferencePool.Runtime.ReferencePool.Acquire<CustomTask>();
task.Initialize(serialId: 1, tag: "HighPriority", priority: 10, userData: null);
taskPool.AddTask(task);

// 低优先级任务
var lowPriorityTask = ReferencePool.Runtime.ReferencePool.Acquire<CustomTask>();
lowPriorityTask.Initialize(serialId: 2, tag: "LowPriority", priority: 1, userData: null);
taskPool.AddTask(lowPriorityTask);
```

#### 2. 移除任务

```csharp
// 按序列号移除任务
bool removed = taskPool.RemoveTask(1);

// 按标签移除任务
int removedCount = taskPool.RemoveTasks("Download");

// 移除所有任务
int totalRemoved = taskPool.RemoveAllTasks();
```

### 高级任务控制

#### 1. 自定义任务代理

```csharp
public class AdvancedAgent : ITaskAgent<CustomTask>
{
    private CustomTask m_Task;
    private float m_StartTime;
    
    public CustomTask Task => m_Task;

    public void Initialize()
    {
        // 初始化代理资源
        m_StartTime = 0f;
    }

    public void Update(float elapseSeconds, float realElapseSeconds)
    {
        if (m_Task == null || m_Task.Done) return;
        
        m_StartTime += elapseSeconds;
        
        // 模拟复杂任务处理
        if (m_StartTime >= 5f) // 5秒后完成任务
        {
            m_Task.Done = true;
            Debug.Log($"任务 {m_Task.SerialId} 完成");
        }
    }

    public void Shutdown()
    {
        // 清理资源
        m_Task = null;
        m_StartTime = 0f;
    }

    public StartTaskStatus Start(CustomTask task)
    {
        m_Task = task;
        m_StartTime = 0f;
        
        // 检查任务是否可立即执行
        if (CanStartImmediately(task))
        {
            Debug.Log($"开始执行任务: {task.Description}");
            return StartTaskStatus.CanResume;
        }
        
        return StartTaskStatus.HasToWait;
    }

    public void Reset()
    {
        m_Task = null;
        m_StartTime = 0f;
    }
    
    private bool CanStartImmediately(CustomTask task)
    {
        // 自定义启动条件检查
        return true;
    }
}
```

#### 2. 复杂任务场景

```csharp
// 多类型任务池管理
public class MultiTaskManager : MonoBehaviour
{
    private TaskPool<DownloadTask> m_DownloadPool;
    private TaskPool<ProcessTask> m_ProcessPool;
    private TaskPool<UploadTask> m_UploadPool;

    private void Start()
    {
        // 初始化多个任务池
        InitializeDownloadPool();
        InitializeProcessPool();
        InitializeUploadPool();
    }

    private void Update()
    {
        // 更新所有任务池
        m_DownloadPool.Update(Time.deltaTime, Time.unscaledDeltaTime);
        m_ProcessPool.Update(Time.deltaTime, Time.unscaledDeltaTime);
        m_UploadPool.Update(Time.deltaTime, Time.unscaledDeltaTime);
    }

    private void InitializeDownloadPool()
    {
        m_DownloadPool = new TaskPool<DownloadTask>();
        for (int i = 0; i < 2; i++)
        {
            m_DownloadPool.AddAgent(new DownloadAgent());
        }
    }

    // 其他池初始化方法类似...
}
```

## 实际应用场景

### 1. 资源下载系统

```csharp
public class ResourceDownloader
{
    private TaskPool<DownloadTask> m_DownloadPool;
    
    public async void DownloadResources(List<ResourceInfo> resources)
    {
        foreach (var resource in resources)
        {
            var task = ReferencePool.Runtime.ReferencePool.Acquire<DownloadTask>();
            task.Initialize(GetNextSerialId(), resource.Url, resource.LocalPath, resource);
            m_DownloadPool.AddTask(task);
        }
        
        // 等待所有下载完成
        await WaitForAllDownloads();
    }
    
    private async Task WaitForAllDownloads()
    {
        while (m_DownloadPool.WaitingTaskCount > 0 || m_DownloadPool.WorkingAgentCount > 0)
        {
            await Task.Delay(100);
        }
    }
}
```

### 2. 批量数据处理

```csharp
public class DataProcessor
{
    private TaskPool<ProcessTask> m_ProcessPool;
    
    public void ProcessBatchData(List<DataItem> dataItems)
    {
        // 根据数据量动态调整代理数量
        int requiredAgents = Math.Min(dataItems.Count, 10);
        EnsureAgentCount(requiredAgents);
        
        foreach (var item in dataItems)
        {
            var task = ReferencePool.Runtime.ReferencePool.Acquire<ProcessTask>();
            task.Initialize(GetNextSerialId(), "DataProcess", 5, item);
            m_ProcessPool.AddTask(task);
        }
    }
    
    private void EnsureAgentCount(int count)
    {
        while (m_ProcessPool.TotalAgentCount < count)
        {
            m_ProcessPool.AddAgent(new ProcessAgent());
        }
    }
}
```

### 3. 异步操作队列

```csharp
public class AsyncOperationQueue
{
    private TaskPool<AsyncTask> m_OperationPool;
    private Queue<Action> m_OperationQueue = new Queue<Action>();
    
    public void EnqueueOperation(Action operation)
    {
        m_OperationQueue.Enqueue(operation);
        
        if (m_OperationPool.FreeAgentCount > 0)
        {
            ProcessNextOperation();
        }
    }
    
    private void ProcessNextOperation()
    {
        if (m_OperationQueue.Count == 0) return;
        
        var operation = m_OperationQueue.Dequeue();
        var task = ReferencePool.Runtime.ReferencePool.Acquire<AsyncTask>();
        task.Initialize(GetNextSerialId(), "AsyncOperation", 0, operation);
        m_OperationPool.AddTask(task);
    }
}
```

## 性能优化建议

### 1. 代理数量优化

```csharp
// 根据系统负载动态调整代理数量
public class AdaptiveTaskPool<T> where T : TaskBase
{
    private TaskPool<T> m_TaskPool;
    private int m_MaxAgents = 10;
    private int m_MinAgents = 2;
    
    public void AdjustAgentCount()
    {
        int currentAgents = m_TaskPool.TotalAgentCount;
        int waitingTasks = m_TaskPool.WaitingTaskCount;
        
        // 动态调整策略
        if (waitingTasks > currentAgents * 2 && currentAgents < m_MaxAgents)
        {
            // 增加代理
            AddAgents(1);
        }
        else if (waitingTasks == 0 && currentAgents > m_MinAgents)
        {
            // 减少代理（实际实现需要更复杂的逻辑）
        }
    }
}
```

### 2. 内存使用优化

```csharp
// 使用对象池管理任务对象
public class OptimizedTaskCreator
{
    public T CreateTask<T>() where T : TaskBase, new()
    {
        // 优先从对象池获取
        var task = ReferencePool.Runtime.ReferencePool.Acquire<T>();
        if (task != null)
        {
            return task;
        }
        
        // 对象池为空时创建新实例
        return new T();
    }
    
    public void ReleaseTask<T>(T task) where T : TaskBase
    {
        ReferencePool.Runtime.ReferencePool.Release(task);
    }
}
```

## API 参考

### TaskPool<T> 主要方法

| 方法 | 描述 | 参数 | 返回值 |
|------|------|------|--------|
| `AddAgent(ITaskAgent<T> agent)` | 添加任务代理 | agent: 任务代理实例 | void |
| `AddTask(T task)` | 添加任务到等待队列 | task: 任务实例 | void |
| `RemoveTask(int serialId)` | 按序列号移除任务 | serialId: 任务序列号 | bool |
| `RemoveTasks(string tag)` | 按标签移除任务 | tag: 任务标签 | int |
| `RemoveAllTasks()` | 移除所有任务 | - | int |
| `GetTaskInfo(int serialId)` | 获取任务信息 | serialId: 任务序列号 | TaskInfo |
| `GetTaskInfos(string tag)` | 按标签获取任务信息 | tag: 任务标签 | TaskInfo[] |
| `GetAllTaskInfos()` | 获取所有任务信息 | - | TaskInfo[] |
| `Update(float, float)` | 更新任务池 | elapseSeconds: 逻辑时间, realElapseSeconds: 真实时间 | void |
| `Shutdown()` | 关闭任务池 | - | void |

### 属性

| 属性 | 类型 | 描述 |
|------|------|------|
| `Paused` | bool | 任务池是否暂停 |
| `TotalAgentCount` | int | 总代理数量 |
| `FreeAgentCount` | int | 空闲代理数量 |
| `WorkingAgentCount` | int | 工作中代理数量 |
| `WaitingTaskCount` | int | 等待任务数量 |

## 注意事项

### 1. 内存管理
- 使用 `ReferencePool` 管理任务对象生命周期
- 及时释放完成的任务对象
- 避免任务代理的内存泄漏

### 2. 性能考虑
- 合理设置任务代理数量
- 避免频繁创建和销毁任务代理
- 使用合适的任务优先级

### 3. 错误处理
- 实现完整的异常处理机制
- 监控任务执行状态
- 提供任务失败的重试机制

### 4. 线程安全
- TaskPool 设计为单线程使用
- 在多线程环境中需要额外的同步机制
- 避免并发访问任务池

## 常见问题解答

### Q: 如何选择合适的任务代理数量？
A: 根据任务类型和系统负载动态调整。一般建议：
- CPU密集型任务：代理数量 ≈ CPU核心数
- I/O密集型任务：可适当增加代理数量
- 实时性要求高的任务：减少代理数量，提高响应速度

### Q: 任务优先级如何影响调度？
A: 高优先级任务会优先执行，但不会中断正在执行的低优先级任务。任务池会在每次调度时重新评估等待队列中的任务优先级。

### Q: 如何处理任务执行失败？
A: 在任务代理的 `Update` 方法中检测错误条件，设置任务状态为完成，并通过事件或回调通知调用方。

### Q: 任务池是否支持取消操作？
A: 支持通过 `RemoveTask` 方法取消等待中的任务，但无法直接取消正在执行的任务。需要任务代理配合实现中断机制。