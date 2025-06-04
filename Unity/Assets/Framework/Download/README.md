## HOMEPAGE

GameFrameX 的 Download 下载任务组件

**Download 下载任务组件 (Download Component)** - 提供管理下载队列、处理下载任务，并提供下载状态的实时更新。相关的接口。

# 使用文档(文档编写于GPT4)

`DownloadComponent` 是一个用于处理下载任务的游戏框架组件。它负责管理下载队列、处理下载任务，并提供下载状态的实时更新。

## 功能概述

- 管理多个下载任务
- 支持断点续传功能
- 提供下载任务的优先级设置
- 实时更新下载进度和下载速度
- 可以通过事件接收下载的各个阶段状态

## 基础用法

要开始使用 `DownloadComponent`，需要在游戏对象上添加此组件。以下是一些核心属性的解释：

- `Paused`: 获取或设置下载是否被暂停。
- `TotalAgentCount`: 获取下载代理总数量。
- `FreeAgentCount`: 获取可用下载代理数量。
- `WorkingAgentCount`: 获取工作中下载代理数量。
- `WaitingTaskCount`: 获取等待下载任务数量。
- `Timeout`: 获取或设置下载超时时长。
- `FlushSize`: 获取或设置写入磁盘的临界大小。
- `CurrentSpeed`: 获取当前下载速度。

## 如何增加下载任务

以下是添加下载任务的几种方法：

```csharp
// 根据指定的下载路径和URI增加下载任务
public int AddDownload(string downloadPath, string downloadUri);

// 根据指定的下载路径、URI和任务标签增加下载任务
public int AddDownload(string downloadPath, string downloadUri, string tag);

// 根据指定的下载路径、URI和优先级增加下载任务
public int AddDownload(string downloadPath, string downloadUri, int priority);

// 根据指定的下载路径、URI和用户自定义数据增加下载任务
public int AddDownload(string downloadPath, string downloadUri, object userData);
```

## 如何移除下载任务

可以通过任务的序列编号或者标签来移除下载任务：

```csharp
// 根据序列编号移除下载任务
public bool RemoveDownload(int serialId);

// 根据标签移除下载任务
public int RemoveDownloads(string tag);

// 移除所有下载任务
public int RemoveAllDownloads();
```

## 事件通知

`DownloadComponent` 提供事件，以便于当下载任务开始、更新、成功、失败时接收通知。

- `DownloadStart`: 当下载开始时触发。
- `DownloadUpdate`: 当下载更新时触发。
- `DownloadSuccess`: 当下载成功时触发。
- `DownloadFailure`: 当下载失败时触发。

## 初始化

组件初始化时，它会设置其内部管理器，并为每个下载代理辅助器注册相应的事件。

```csharp
protected override void Awake()
{
    base.Awake();
    // ...初始化代码...
}
```

## 示例

要添加一个新的下载任务并接收其进度：

```csharp
// 假设已经存在 downloadComponent 实例
int serialId = downloadComponent.AddDownload("本地存储路径", "下载链接");

// 通过事件组件订阅下载成功事件
eventComponent.Subscribe(DownloadSuccessEventArgs.EventId, OnDownloadSuccess);

// 下载成功回调
void OnDownloadSuccess(object sender, GameEventArgs e)
{
    DownloadSuccessEventArgs ne = (DownloadSuccessEventArgs)e;
    if (ne.SerialId == serialId)
    {
        // 处理下载成功
    }
}
```

注意：此组件依赖于Event 组件：https://github.com/AlianBlank/com.gameframex.unity.event

# 使用方式(任选其一)

1. 直接在 `manifest.json` 的文件中的 `dependencies` 节点下添加以下内容
   ```json
   {
      "com.gameframex.unity.event": "https://github.com/AlianBlank/com.gameframex.unity.event.git",
      "com.gameframex.unity.download": "https://github.com/AlianBlank/com.gameframex.unity.download.git"
   }
    ```
2. 在Unity 的`Packages Manager` 中使用`Git URL` 的方式添加库,地址为：https://github.com/AlianBlank/com.gameframex.unity.download.git,

3. 直接下载仓库放置到Unity 项目的`Packages` 目录下。会自动加载识别