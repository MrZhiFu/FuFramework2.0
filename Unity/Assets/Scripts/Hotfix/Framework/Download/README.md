# FuFramework Download Module

## 1. 简介

FuFramework Download 模块是游戏框架的下载管理系统，基于任务池架构实现高效的并发下载调度。该模块使用 `UnityWebRequest` 作为底层下载引擎，支持断点续传、下载速度计算、超时控制和丰富的下载事件系统。

## 2. 核心特性

- **任务池调度**：基于 `TaskPool` 的任务调度机制，支持并发下载和优先级控制
- **断点续传**：通过 `DownloadHandler` 支持 HTTP Range 请求，实现断点续传
- **速度监控**：`DownloadCounter` 通过链表节点实时计算下载速度
- **超时控制**：`DownloadTask` 支持可配置的超时时间
- **异步支持**：基于 `UniTask` 的 async/await 异步模式
- **丰富事件**：覆盖下载开始、进度更新、成功、失败全流程事件

## 3. 核心概念

### 3.1 下载架构

```
┌─────────────────────────────────────────────────────────────┐
│                     DownloadModule                           │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  m_TaskPool (TaskPool<DownloadTask>)                │   │
│  │  任务调度与并发控制，内嵌 N 个 DownloadAgent：        │   │
│  │    DownloadAgent → UnityWebRequestDownloadAgentHelper │   │
│  │                → DownloadHandler（接收数据流）        │   │
│  └─────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  m_DownloadingTaskDict (serialId → DownloadData)    │   │
│  │  下载中任务登记，并持有异步完成信号(Tcs)              │   │
│  └─────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  m_DownloadCounter (DownloadCounter + CounterNode)  │   │
│  │  实时下载速度计算                                    │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### 3.2 下载任务状态

```
Todo → Doing → Done
         ↓
       Error
```

## 4. 核心类说明

### 4.1 DownloadModule

下载管理模块，继承自 `ModuleBase`。采用分片组织：`DownloadModule.cs` 承载生命周期与内部实现，公共 API（单例 `Instance`、配置属性及添加/移除/查询接口）统一收敛在 `DownloadModule.API.cs`。模块内部实现类均已独立为 `Task/`、`Agent/` 下的顶层 `internal` 类，不再作为模块嵌套类。

**核心属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| `Paused` | `bool` | 是否暂停所有下载 |
| `TotalAgentCount` | `int` | 总下载代理数量 |
| `FreeAgentCount` | `int` | 空闲代理数量 |
| `WorkingAgentCount` | `int` | 工作中下载代理数量 |
| `WaitingTaskCount` | `int` | 等待中的下载任务数量 |
| `CurrentSpeed` | `float` | 当前下载速度（字节/秒） |
| `FlushSize` | `int` | 缓冲区写入磁盘的临界大小（默认 1MB） |
| `Timeout` | `float` | 下载超时时长（秒） |

**核心方法：**

```csharp
// 添加下载任务（多种重载）
int AddDownload(string downloadedFullPath, string downloadUri)
int AddDownload(string downloadedFullPath, string downloadUri, string taskTag)
int AddDownload(string downloadedFullPath, string downloadUri, int priority)
int AddDownload(string downloadedFullPath, string downloadUri, object userData)
int AddDownload(string downloadedFullPath, string downloadUri, string taskTag, int priority)
int AddDownload(string downloadedFullPath, string downloadUri, string taskTag, object userData)
int AddDownload(string downloadedFullPath, string downloadUri, int priority, object userData)
int AddDownload(string downloadedFullPath, string downloadUri, string taskTag, int priority, object userData)

// 异步下载（await 方式）
UniTask<bool> AddDownloadAsync(string downloadPath, string downloadUri)

// 移除下载任务
bool RemoveDownload(int serialId)
int RemoveDownloads(string taskTag)
int RemoveAllDownloads()
```

### 4.2 DownloadAgent

下载代理（模块内部实现类，位于 `Agent/DownloadAgent.cs`，实现 `ITaskAgent<DownloadTask>`）。每个代理持有一个 `UnityWebRequestDownloadAgentHelper`，真正驱动一次下载。

**核心功能：**
- 通过 `UnityWebRequestDownloadAgentHelper` + `DownloadHandler` 执行 HTTP 下载
- 以文件流增量写盘，结合 HTTP Range 实现断点续传
- 管理下载超时与错误处理
- 驱动模块的进度/成功/失败事件与速度统计

### 4.3 DownloadTask

下载任务（模块内部实现类，位于 `Task/DownloadTask.cs`），继承 `TaskBase`，封装单次下载的元数据，并经引用池创建与复用。

| 属性 | 说明 |
|------|------|
| `Status` | 下载任务状态（`DownloadTaskStatus`：Todo/Doing/Done/Error） |
| `DownloadedFullPath` | 下载后存放全路径 |
| `DownloadUri` | 远程下载地址 |
| `FlushSize` | 缓冲区写入磁盘的临界大小（字节） |
| `Timeout` | 超时时间（秒） |

> `SerialId`/`Tag`/`Priority`/`UserData`/`Description` 等继承自 `TaskBase`。

### 4.4 DownloadCounter

下载计数器（模块内部实现类，位于 `Agent/DownloadCounter.cs`），使用链表节点（`DownloadCounterNode`）记录历史下载量，实时计算下载速度。

### 4.5 UnityWebRequestDownloadAgentHelper 与 DownloadHandler

`UnityWebRequestDownloadAgentHelper`（位于 `Helper/UnityWebRequestDownloadAgentHelper.cs`）是基于 `UnityWebRequest` 的下载辅助器，支持：
- HTTP Range 断点续传
- 证书校验（同文件内的 `DownloadCertificateHandler`，当前恒通过）
- 通过独立的 `DownloadHandler`（`Helper/DownloadHandler.cs`，继承 `DownloadHandlerScript`）接收数据流，并广播字节/长度更新事件

### 4.6 下载事件

| 事件类 | 说明 |
|------|------|
| `DownloadStartEventArgs` | 下载开始事件 |
| `DownloadUpdateEventArgs` | 下载进度更新事件 |
| `DownloadSuccessEventArgs` | 下载成功事件 |
| `DownloadFailureEventArgs` | 下载失败事件 |

## 5. 使用示例

### 5.1 基本下载

```csharp
using Hotfix.Framework.Download;
using Hotfix.Framework.Core;

public class DownloadExample
{
    private DownloadModule m_DownloadModule;

    public void Init()
    {
        m_DownloadModule = ModuleManager.GetModule<DownloadModule>();
    }

    public async UniTask DownloadFileAsync()
    {
        // 异步下载文件
        bool success = await m_DownloadModule.AddDownloadAsync(
            downloadPath: Application.persistentDataPath + "/config.json",
            downloadUri: "https://cdn.example.com/config.json"
        );

        if (success)
        {
            Debug.Log("下载成功！");
        }
        else
        {
            Debug.LogError("下载失败!");
        }
    }
}
```

### 5.2 批量下载

```csharp
public async UniTask DownloadMultipleFilesAsync()
{
    var downloads = new List<UniTask<bool>>();

    downloads.Add(m_DownloadModule.AddDownloadAsync(
        "path/file1.bundle", "https://cdn.example.com/file1.bundle"));
    downloads.Add(m_DownloadModule.AddDownloadAsync(
        "path/file2.bundle", "https://cdn.example.com/file2.bundle"));
    downloads.Add(m_DownloadModule.AddDownloadAsync(
        "path/file3.bundle", "https://cdn.example.com/file3.bundle"));

    // 等待所有下载完成
    var results = await UniTask.WhenAll(downloads);

    for (int i = 0; i < results.Length; i++)
    {
        Debug.Log($"下载结果: {results[i]}");
    }
}
```

### 5.3 监听下载事件

```csharp
var eventModule = ModuleManager.GetModule<EventModule>();

// 监听下载进度
eventModule.Subscribe(DownloadUpdateEventArgs.EventId, (sender, e) =>
{
    var args = e as DownloadUpdateEventArgs;
    // 更新事件仅携带当前已下载字节数（CurrentLength），不含总大小；进度请由业务侧自行比对期望文件长度
    Debug.Log($"已下载: {args.CurrentLength} 字节，速度: {m_DownloadModule.CurrentSpeed / 1024}KB/s");
});

// 监听下载完成
eventModule.Subscribe(DownloadSuccessEventArgs.EventId, (sender, e) =>
{
    var args = e as DownloadSuccessEventArgs;
    Debug.Log($"下载完成: {args.DownloadPath}");
});
```

### 5.4 暂停和恢复

```csharp
// 暂停所有下载
m_DownloadModule.Paused = true;

// 恢复所有下载
m_DownloadModule.Paused = false;
```

## 6. 目录结构

代码位于 `Assets/Scripts/Hotfix/Framework/Download/`：

```text
Download/
├── DownloadModule.cs                          # 模块核心（生命周期 / 内部实现 / 私有回调）
├── DownloadModule.API.cs                      # 模块公共 API（单例 Instance / 属性 / 添加·移除·查询接口）
├── Task/                                      # 下载任务域（顶层 internal 类）
│   ├── DownloadTask.cs                        # 下载任务（继承 TaskBase）
│   ├── DownloadTaskStatus.cs                  # 下载任务状态枚举
│   └── DownloadData.cs                        # 下载中登记数据（含异步完成信号）
├── Agent/                                     # 下载执行与统计（顶层 internal 类）
│   ├── DownloadAgent.cs                       # 下载代理（实现 ITaskAgent<DownloadTask>）
│   ├── DownloadCounter.cs                     # 下载速度计数器
│   └── DownloadCounterNode.cs                 # 计数器链表节点
├── Helper/
│   ├── UnityWebRequestDownloadAgentHelper.cs  # UnityWebRequest 下载辅助器（含 DownloadCertificateHandler）
│   └── DownloadHandler.cs                     # 数据流接收处理器
├── Event/
│   ├── DownloadStartEventArgs.cs              # 下载开始事件
│   ├── DownloadUpdateEventArgs.cs             # 下载进度更新事件
│   ├── DownloadSuccessEventArgs.cs            # 下载成功事件
│   ├── DownloadFailureEventArgs.cs            # 下载失败事件
│   ├── DownloadAgentHelperCompleteEventArgs.cs
│   ├── DownloadAgentHelperErrorEventArgs.cs
│   ├── DownloadAgentHelperUpdateBytesEventArgs.cs
│   └── DownloadAgentHelperUpdateLengthEventArgs.cs
└── README.md                                  # 本文档
```

## 7. 依赖

- **Hotfix.Framework.Core**：提供 ModuleBase、GlobalModule 等基础能力
- **Hotfix.Framework.TaskPool**：任务池（TaskPool/TaskBase/ITaskAgent），负责 DownloadTask 调度与 DownloadAgent 管理
- **Hotfix.Framework.Event**：事件系统
- **Hotfix.Framework.ReferencePool**：引用池
- **UnityEngine.Networking**：UnityWebRequest / DownloadHandlerScript 底层下载
- **UniTask**：异步支持

## 8. 注意事项

1. **断点续传**：需要服务器支持 HTTP Range 请求（返回 206 Partial Content）
2. **并发控制**：通过添加的下载代理数量控制并发数
3. **超时设置**：根据文件大小合理设置超时时间
4. **线程安全**：下载操作通过 UnityWebRequest 在后台线程执行，回调在主线程
5. **资源释放**：下载完成后会自动释放 UnityWebRequest 资源
