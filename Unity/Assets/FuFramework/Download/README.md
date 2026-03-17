# FuFramework Download Module

## 简介
FuFramework Download 模块是一个功能强大的文件下载管理系统。它基于任务池（TaskPool）架构设计，支持多任务并发下载、断点续传、速度监控以及流式写入。该模块旨在为游戏提供稳定、高效的资源下载服务，特别适用于热更新、DLC 下载等场景。

## 特性
- **并发下载**：支持同时下载多个文件（默认并发数 3，可配置）。
- **断点续传**：自动检测本地已下载的文件大小，从断点处继续下载，节省流量。
- **流式写入**：下载数据实时写入磁盘，内存占用极低，支持大文件下载。
- **速度监控**：内置下载速度统计，方便 UI 实时显示。
- **健壮性**：支持超时重试、错误处理和自动恢复。

## 核心类说明

### DownloadModule
下载管理器，继承自 `FuModule`。
- **AddDownloadTask**: 添加一个下载任务。
- **RemoveDownloadTask**: 移除一个下载任务。
- **RemoveAllDownloadTasks**: 移除所有下载任务。
- **Paused**: 全局暂停/恢复下载。
- **CurrentSpeed**: 获取当前实时下载速度（字节/秒）。

### 事件系统
模块通过 `EventModule` 抛出以下事件：
- `DownloadStartEventArgs`: 任务开始。
- `DownloadUpdateEventArgs`: 进度更新（包含已下载大小、总大小、当前速度）。
- `DownloadSuccessEventArgs`: 任务成功。
- `DownloadFailureEventArgs`: 任务失败。

## 使用示例

### 1. 添加下载任务
```csharp
// 获取下载管理器
var downloadModule = ModuleManager.GetModule<DownloadModule>();

// 定义下载地址和本地保存路径
string url = "https://example.com/file.zip";
string savePath = Path.Combine(Application.persistentDataPath, "file.zip");

// 添加任务 (返回任务序列ID)
// 参数: 下载地址, 本地路径, 优先级(默认0), 用户自定义数据(可选)
int serialId = downloadModule.AddDownloadTask(url, savePath, 0, "MyUserData");
```

### 2. 监听下载事件
建议创建一个专门的控制器来处理下载事件。

```csharp
using FuFramework.Event.Runtime;
using FuFramework.Download.Runtime;

public class DownloadController : MonoBehaviour
{
    private void Start()
    {
        var eventModule = ModuleManager.GetModule<EventModule>();
        eventModule.Subscribe(DownloadStartEventArgs.EventId, OnDownloadStart);
        eventModule.Subscribe(DownloadUpdateEventArgs.EventId, OnDownloadUpdate);
        eventModule.Subscribe(DownloadSuccessEventArgs.EventId, OnDownloadSuccess);
        eventModule.Subscribe(DownloadFailureEventArgs.EventId, OnDownloadFailure);
    }

    private void OnDownloadUpdate(object sender, GameEventArgs e)
    {
        var args = (DownloadUpdateEventArgs)e;
        // 打印进度：任务ID - 进度百分比 - 当前速度
        float progress = (float)args.CurrentLength / args.TotalLength;
        Debug.Log($"Task {args.SerialId}: {progress:P} Speed: {args.DownloadSpeed / 1024f:F2} KB/s");
    }

    private void OnDownloadSuccess(object sender, GameEventArgs e)
    {
        var args = (DownloadSuccessEventArgs)e;
        Debug.Log($"Task {args.SerialId} Finished! Saved to: {args.DownloadPath}");
    }
    
    // ... 其他事件处理
}
```

### 3. 控制下载
```csharp
// 暂停所有下载
downloadModule.Paused = true;

// 恢复下载
downloadModule.Paused = false;

// 取消指定任务
downloadModule.RemoveDownloadTask(serialId);
```

## 配置说明
在 `DownloadModule` 初始化时（或运行时）可以调整以下参数：
- **Timeout**: 下载超时时间（默认 30秒）。
- **FlushSize**: 缓冲区写入磁盘的阈值（默认 1MB）。

## 编辑器扩展
选中场景中的 `[ModuleManager]` 节点，在 Inspector 面板的 `DownloadModule` 组件中可以查看：
- **实时状态**：总代理数、工作代理数、等待任务数。
- **下载速度**：当前全局下载速度。
- **任务列表**：所有正在进行的任务详情（ID、优先级、状态）。
