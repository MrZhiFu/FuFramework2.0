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

### 类继承体系

```
TaskPool<T> (泛型任务池)
    ├── ITaskAgent<T> (任务代理接口)
    │       ↓
    │   自定义任务代理实现
    │
    ├── TaskBase (任务基类)
    │       ↓
    │   IReference (引用池接口)
    │       ↓
    │   自定义任务实现
    │
    ├── TaskInfo (任务信息结构体)
    │
    └── 枚举类型
            ├── TaskStatus (任务状态)
            └── StartTaskStatus (启动状态)
```

### 技术架构

```
┌─────────────────────────────────────────────────────────────┐
│                      TaskPool<T>                            │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  m_FreeAgentStack (Stack<ITaskAgent<T>>)            │   │
│  │  - 空闲任务代理栈，后进先出                          │   │
│  └─────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  m_WaitingTaskList (FuLinkedList<T>)                │   │
│  │  - 等待任务链表，按优先级排序                        │   │
│  └─────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  m_WorkingAgentList (FuLinkedList<ITaskAgent<T>>)   │   │
│  │  - 工作中代理链表                                   │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              ↓
                    Update() 轮询处理
                              ↓
        ┌─────────────────────┴─────────────────────┐
        ↓                                           ↓
┌──────────────────┐                    ┌──────────────────┐
│ _ProcessRunning  │                    │ _ProcessWaiting  │
│ Tasks()          │                    │ Tasks()          │
│ - 更新运行中任务  │                    │ - 分配空闲代理    │
│ - 完成任务回收    │                    │ - 启动等待任务    │
└──────────────────┘                    └──────────────────┘
```

## 核心类详解

### TaskPool<T>

任务池核心类，负责任务的调度、执行和管理。

**核心字段：**

| 字段 | 类型 | 说明 |
|------|------|------|
| m_FreeAgentStack | Stack<ITaskAgent<T>> | 空闲任务代理栈 |
| m_WaitingTaskList | FuLinkedList<T> | 等待任务链表 |
| m_WorkingAgentList | FuLinkedList<ITaskAgent<T>> | 工作中代理链表 |

**核心属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| Paused | bool | 任务池是否暂停 |
| TotalAgentCount | int | 总代理数量（空闲+工作中） |
| FreeAgentCount | int | 空闲代理数量 |
| WorkingAgentCount | int | 工作中代理数量 |
| WaitingTaskCount | int | 等待任务数量 |

**核心方法：**

```csharp
// 添加任务代理
public void AddAgent(ITaskAgent<T> agent)

// 添加任务到等待队列（自动按优先级排序）
public void AddTask(T task)

// 移除任务
public bool RemoveTask(int serialId)                    // 按序列号移除
public int RemoveTasks(string tag)                      // 按标签移除
public int RemoveAllTasks()                             // 移除所有任务

// 获取任务信息
public TaskInfo GetTaskInfo(int serialId)               // 获取单个任务信息
public TaskInfo[] GetTaskInfos(string tag)              // 按标签获取任务信息
public TaskInfo[] GetAllTaskInfos()                     // 获取所有任务信息

// 更新任务池（需在主循环中调用）
public void Update(float deltaTime, float unscaledDeltaTime)

// 关闭并清理任务池
public void Shutdown()
```

**调度机制：**

1. **_ProcessRunningTasks**：遍历工作中代理链表，更新每个代理的任务状态
   - 如果任务未完成，调用代理的 Update 方法
   - 如果任务完成，重置代理并放回空闲栈

2. **_ProcessWaitingTasks**：从等待任务链表中取出任务，分配给空闲代理
   - 按优先级从高到低处理等待任务
   - 根据 Start 方法返回值决定任务状态

### TaskBase

任务基类，所有自定义任务必须继承此类。实现了 IReference 接口，支持引用池管理。

**核心属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| SerialId | int | 任务序列编号（唯一标识） |
| Tag | string | 任务标签（用于分类） |
| Priority | int | 任务优先级（数值越大优先级越高） |
| Done | bool | 任务是否完成 |
| Description | string | 任务描述（虚属性，可重写） |
| UserData | object | 用户自定义数据 |

**核心方法：**

```csharp
// 初始化任务
public void Initialize(int serialId, string tag, int priority, object userData)

// 清理任务（实现 IReference 接口）
public virtual void Clear()
```

**使用示例：**

```csharp
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

    public override string Description => $"下载任务: {Url}";
}
```

### ITaskAgent<T>

任务代理接口，定义任务执行的具体逻辑。

**接口定义：**

```csharp
public interface ITaskAgent<T> where T : TaskBase
{
    // 获取当前处理的任务
    T Task { get; }

    // 初始化代理（添加时调用）
    void Initialize();

    // 轮询更新（每帧调用）
    void Update(float deltaTime, float unscaledDeltaTime);

    // 关闭并清理代理（Shutdown时调用）
    void Shutdown();

    // 开始处理任务
    StartTaskStatus Start(T task);

    // 重置代理（任务完成或失败时调用）
    void Reset();
}
```

**使用示例：**

```csharp
public class DownloadAgent : ITaskAgent<DownloadTask>
{
    public DownloadTask Task { get; private set; }
    
    public void Initialize()
    {
        // 初始化下载器资源
    }

    public void Update(float deltaTime, float unscaledDeltaTime)
    {
        if (Task == null || Task.Done) return;

        // 更新下载进度
        Task.Progress += deltaTime * 0.1f;
        if (Task.Progress >= 1f)
        {
            Task.Progress = 1f;
            Task.Done = true;
        }
    }

    public void Shutdown()
    {
        // 清理资源
    }

    public StartTaskStatus Start(DownloadTask task)
    {
        Task = task;
        return StartTaskStatus.CanResume;
    }

    public void Reset()
    {
        Task = null;
    }
}
```

### TaskInfo

任务信息结构体（readonly struct），用于外部查询任务状态。

**核心属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| IsValid | bool | 信息是否有效 |
| SerialId | int | 任务序列编号 |
| Tag | string | 任务标签 |
| Priority | int | 任务优先级 |
| Status | TaskStatus | 任务状态 |
| Description | string | 任务描述 |
| UserData | object | 用户自定义数据 |

### 枚举类型

#### TaskStatus

```csharp
public enum TaskStatus : byte
{
    Todo = 0,   // 未开始
    Doing,      // 执行中
    Done        // 完成
}
```

#### StartTaskStatus

```csharp
public enum StartTaskStatus : byte
{
    Done = 0,       // 完成此任务（立即完成，无需Update）
    CanResume,      // 恢复处理此任务（需要后续Update）
    HasToWait,      // 不能处理此任务，需等待（放回等待队列）
    UnknownError    // 不能处理此任务，出现未知错误（移除任务）
}
```

## 使用示例

### 基本使用流程

```csharp
using FuFramework.TaskPool.Runtime;
using FuFramework.ReferencePool.Runtime;

public class TaskPoolExample : MonoBehaviour
{
    private TaskPool<DownloadTask> m_DownloadPool;
    private int m_SerialId = 0;

    private void Start()
    {
        // 创建任务池
        m_DownloadPool = new TaskPool<DownloadTask>();
        
        // 添加任务代理
        for (int i = 0; i < 3; i++)
        {
            m_DownloadPool.AddAgent(new DownloadAgent());
        }

        // 添加下载任务
        AddDownloadTask("https://example.com/file1.zip", "D:/Downloads/file1.zip");
        AddDownloadTask("https://example.com/file2.zip", "D:/Downloads/file2.zip");
    }

    private void Update()
    {
        // 更新任务池
        m_DownloadPool.Update(Time.deltaTime, Time.unscaledDeltaTime);
    }

    private void AddDownloadTask(string url, string savePath)
    {
        var task = ReferencePool.Acquire<DownloadTask>();
        task.Initialize(++m_SerialId, url, savePath);
        m_DownloadPool.AddTask(task);
    }

    private void OnDestroy()
    {
        m_DownloadPool?.Shutdown();
    }
}
```

### 任务状态监控

```csharp
// 获取任务池统计
int totalAgents = taskPool.TotalAgentCount;
int freeAgents = taskPool.FreeAgentCount;
int workingAgents = taskPool.WorkingAgentCount;
int waitingTasks = taskPool.WaitingTaskCount;

Debug.Log($"总代理: {totalAgents}, 空闲: {freeAgents}, 工作中: {workingAgents}, 等待: {waitingTasks}");

// 获取特定任务信息
var taskInfo = taskPool.GetTaskInfo(1);
if (taskInfo.IsValid)
{
    Debug.Log($"任务 {taskInfo.SerialId}: {taskInfo.Description} - 状态: {taskInfo.Status}");
}

// 按标签获取任务信息
var downloadTasks = taskPool.GetTaskInfos("Download");
foreach (var info in downloadTasks)
{
    Debug.Log($"下载任务: {info.Description} - 状态: {info.Status}");
}
```

### 任务管理

```csharp
// 添加高优先级任务
var highPriorityTask = ReferencePool.Acquire<CustomTask>();
highPriorityTask.Initialize(serialId: 1, tag: "Urgent", priority: 100, userData: null);
taskPool.AddTask(highPriorityTask);

// 添加低优先级任务
var lowPriorityTask = ReferencePool.Acquire<CustomTask>();
lowPriorityTask.Initialize(serialId: 2, tag: "Normal", priority: 1, userData: null);
taskPool.AddTask(lowPriorityTask);

// 移除任务
taskPool.RemoveTask(1);              // 按序列号移除
taskPool.RemoveTasks("Download");    // 按标签移除
taskPool.RemoveAllTasks();           // 移除所有任务
```

### 暂停和恢复

```csharp
// 暂停任务池
taskPool.Paused = true;

// 恢复任务池
taskPool.Paused = false;
```

## 目录结构

```
FuFramework/TaskPool/
├── Runtime/
│   ├── TaskPool.cs              # 任务池核心类
│   ├── TaskBase.cs              # 任务基类
│   ├── ITaskAgent.cs            # 任务代理接口
│   ├── TaskInfo.cs              # 任务信息结构体
│   ├── TaskStatus.cs            # 任务状态枚举
│   ├── StartTaskStatus.cs       # 启动状态枚举
│   └── FuFramework.TaskPool.Runtime.asmdef
├── README.md                    # 本文档
```

## 依赖模块

- **Core**: 提供 FuLinkedList、FuException、FuGuard 等工具类
- **ReferencePool**: 提供对象池管理，用于任务对象的复用

## 设计特点

### 1. 三层数据结构

- **空闲代理栈（Stack）**：后进先出，快速获取和释放代理
- **等待任务链表（LinkedList）**：支持按优先级插入和移除
- **工作代理链表（LinkedList）**：支持遍历和动态移除

### 2. 优先级调度

等待任务链表按优先级从高到低排序，高优先级任务优先获得代理资源。

### 3. 资源复用

- 任务代理通过栈结构复用，避免频繁创建销毁
- 任务对象通过 ReferencePool 管理，减少 GC 压力

### 4. 状态驱动

任务通过 Done 属性标记完成状态，代理通过 StartTaskStatus 返回启动结果，实现灵活的任务控制。

## 应用场景

1. **资源下载**：管理多个并发下载任务
2. **资源加载**：异步加载 AssetBundle 或其他资源
3. **Web 请求**：管理 HTTP 请求队列
4. **数据处理**：批量处理大量数据
5. **异步操作**：任何需要队列管理的异步任务

## 注意事项

1. **线程安全**：TaskPool 设计为单线程使用，需在主线程调用 Update
2. **代理数量**：合理设置代理数量，过多会占用资源，过少会降低并发度
3. **任务清理**：完成的任务会自动释放回 ReferencePool，无需手动处理
4. **暂停机制**：暂停后不会处理新任务，但正在执行的任务会继续直到完成
5. **错误处理**：通过 StartTaskStatus.UnknownError 标记错误任务，会自动释放
