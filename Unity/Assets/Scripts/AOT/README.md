# Game AOT 模块

## 1. 简介

Game AOT 模块是游戏的 AOT（Ahead-of-Time）编译部分，负责游戏启动流程、资源热更新、代码热修复等核心功能。该模块基于 FuFramework 的流程系统（Procedure）构建，与 YooAsset 资源管理系统和 HybridCLR 代码热更系统深度集成。

## 2. 特性

- **流程化架构**：基于 FuFramework Procedure 系统，将启动和热更流程拆分为多个独立的流程节点
- **资源热更新**：集成 YooAsset，支持版本检测、清单更新、资源下载完整流程
- **代码热修复**：集成 HybridCLR，支持 AOT 元数据补充和热更程序集加载
- **事件驱动**：通过事件系统实现流程间通信和 UI 更新
- **多模式支持**：适配编辑器模拟模式、单机离线模式、热更模式

## 3. 目录结构

```
AOT/
├── Procedure/ # 流程相关
│   ├── Enum/ # 枚举定义
│   │   └── EUpdateStates.cs # 更新状态枚举
│   ├── Event/ # 事件定义
│   │   ├── AssetDownloadFailedEventArgs.cs       # 资源下载失败事件
│   │   ├── AssetDownloadProgressEventArgs.cs     # 资源下载进度事件
│   │   ├── AssetManifestUpdateFailedEventArgs.cs # 清单更新失败事件
│   │   ├── AssetUpdateStateChangeEventArgs.cs    # 更新状态变更事件
│   │   ├── AssetVersionUpdateFailedEventArgs.cs  # 版本更新失败事件
│   │   └── FoundNeedUpdateAssetEventArgs.cs      # 发现需要更新资源事件
│   ├── UpdateConfig/       # 更新配置
│   │   └── RemoteUpdateConfig.cs                # 远端更新配置
│   ├── ProcedureLauncher.cs                     # 入口启动流程
│   ├── ProcedureReqRemoteUpdateConfig.cs        # 获取远端更新配置流程
│   ├── ProcedureInitPackage.cs                  # 初始化资源包流程
│   ├── ProcedureGetPackageVersion.cs            # 获取资源版本号流程
│   ├── ProcedureUpdatePackageManifest.cs        # 更新资源清单流程
│   ├── ProcedureCreateDownloader.cs             # 创建资源下载器流程
│   ├── ProcedureDownloadPackage.cs              # 下载资源包流程
│   ├── ProcedureUpdateDone.cs                   # 更新完毕流程
│   └── ProcedureCodeHotfix.cs                   # 代码热修复流程
└── UI/                     # UI 相关
    └── Launcher/           # 启动器界面
        ├── Gen/            # 自动生成的代码
        └── Impl/           # 实现代码
```

## 4. 核心流程说明

### 4.1 启动流程图

```
ProcedureLauncher (入口启动)
    │
    ├─ 编辑器/单机离线模式 ──→ ProcedureInitPackage (初始化资源包)
    │
    └─ 热更模式 ────────→ ProcedureReqRemoteUpdateConfig (获取远端更新配置)
                              │
                              ├─ 需要强更 ──→ 打开 App 下载链接
                              │
                              └─ 不需要强更 ─→ ProcedureInitPackage (初始化资源包)
                                                    │
                                                    ↓
                              ProcedureGetPackageVersion (获取资源版本号)
                                                    │
                                                    ↓
                              ProcedureUpdatePackageManifest (更新资源清单)
                                                    │
                                                    ↓
                              ProcedureCreateDownloader (创建下载器)
                                                    │
                              ├─ 无需要下载资源 ──→ ProcedureUpdateDone (更新完毕)
                              │
                              └─ 有需要下载资源 ──→ ProcedureDownloadPackage (下载资源包)
                                                    │
                                                    ↓
                              ProcedureUpdateDone (更新完毕)
                                                    │
                                                    ↓
                              ProcedureCodeHotfix (代码热修复)
                                                    │
                                                    ↓
                                          进入热更代码逻辑
```

### 4.2 流程详细说明

#### ProcedureLauncher - 入口启动流程

- 设置 FairyGUI 自定义 Loader
- 绑定 FUI 自定义组件
- 打开热更进度界面
- 根据运行模式决定后续流程

#### ProcedureReqRemoteUpdateConfig - 获取远端更新配置流程

- 从 CDN 服务器获取 `RemoteUpdateConfig.json`
- 判断是否强制更新（ForceUpdate）
- 强更：弹出提示框，点击后打开 App 下载链接
- 非强更：保存下载地址，进入初始化资源包流程

#### ProcedureInitPackage - 初始化资源包流程

- 编辑器/离线模式：直接初始化默认资源包
- 热更模式：使用配置的下载地址初始化资源包
- 进入获取资源版本号流程

#### ProcedureGetPackageVersion - 获取资源版本号流程

- 请求资源包的最新版本号
- 成功：保存版本号，进入更新资源清单流程
- 失败：延迟 3 秒后重试

#### ProcedureUpdatePackageManifest - 更新资源清单流程

- 使用版本号更新资源清单
- 成功：进入创建下载器流程
- 失败：广播清单更新失败事件，重新尝试

#### ProcedureCreateDownloader - 创建资源下载器流程

- 创建资源下载器
- 无需要下载资源：直接进入更新完毕流程
- 有需要下载资源：
  - 如果需要显示更新提示，弹出提示框等待用户确认
  - 保存下载器，进入下载资源包流程

#### ProcedureDownloadPackage - 下载资源包流程

- 开始异步下载资源
- 监听下载进度，广播进度事件
- 监听下载错误，失败时重新创建下载器
- 下载完成：进入更新完毕流程

#### ProcedureUpdateDone - 更新完毕流程

- 设置 UI 为更新完成状态
- 广播更新完毕事件
- 进入代码热修复流程

#### ProcedureCodeHotfix - 代码热修复流程

- 加载 AOT 程序集，补充元数据（HybridCLR）
- 加载热更程序集 `Game.Hotfix`
- 运行热更程序集入口函数
- 关闭启动界面，进入热更代码逻辑

## 5. 事件系统

模块提供了丰富的事件用于构建热更新 UI：

| 事件类 | 说明 | 主要属性 |
|--------|------|----------|
| `AssetDownloadProgressEventArgs` | 下载进度更新 | `PackageName`, `TotalDownloadCount`, `CurrentDownloadCount`, `TotalDownloadBytes`, `CurrentDownloadBytes` |
| `AssetUpdateStateChangeEventArgs` | 更新状态变更 | `PackageName`, `CurrentStates` |
| `FoundNeedUpdateAssetEventArgs` | 发现需要更新资源 | `PackageName`, `TotalCount`, `TotalSizeBytes` |
| `AssetManifestUpdateFailedEventArgs` | 清单更新失败 | `PackageName`, `Error` |
| `AssetVersionUpdateFailedEventArgs` | 版本号更新失败 | `PackageName`, `Error` |
| `AssetDownloadFailedEventArgs` | 资源下载失败 | `PackageName`, `FileName`, `Error` |

### 事件使用示例

```csharp
// 监听下载进度
GlobalModule.EventModule.Subscribe(AssetDownloadProgressEventArgs.EventId, OnDownloadProgress);

void OnDownloadProgress(object sender, GameEventArgs e)
{
    var args = (AssetDownloadProgressEventArgs)e;
    float progress = (float)args.CurrentDownloadBytes / args.TotalDownloadBytes;
    Debug.Log($"[{args.PackageName}] 下载进度: {progress:P2} ({args.CurrentDownloadCount}/{args.TotalDownloadCount})");
}

// 监听状态变更
GlobalModule.EventModule.Subscribe(AssetUpdateStateChangeEventArgs.EventId, OnStateChange);

void OnStateChange(object sender, GameEventArgs e)
{
    var args = (AssetUpdateStateChangeEventArgs)e;
    Debug.Log($"[{args.PackageName}] 状态变更为: {args.CurrentStates}");
}
```

## 6. 配置说明

### RemoteUpdateConfig - 远端更新配置

位于 CDN 服务器的 `RemoteUpdateConfig.json`：

| 字段 | 类型 | 说明 |
|------|------|------|
| `ForceUpdate` | `bool` | 是否强制更新 App |
| `ShowUpdateTips` | `bool` | 是否显示更新提示框 |
| `UpdateAnnouncement` | `string` | 更新公告内容 |
| `AppDownloadUrl` | `string` | App 下载地址 |
| `ResDownloadUrl` | `string` | 资源下载地址，支持 `{0}` 版本号占位符 |
| `ResDownloadBackupUrl` | `string` | 资源下载备用地址 |

### 配置示例

```json
{
  "ForceUpdate": false,
  "ShowUpdateTips": true,
  "UpdateAnnouncement": "发现新版本，建议更新！\n1. 优化游戏性能\n2. 修复已知问题",
  "AppDownloadUrl": "https://example.com/download/app.apk",
  "ResDownloadUrl": "https://cdn.example.com/game/{0}/",
  "ResDownloadBackupUrl": "https://cdn-backup.example.com/game/{0}/"
}
```

## 7. EUpdateStates - 更新状态

| 状态 | 说明 |
|------|------|
| `GetVersion` | 获取资源版本号 |
| `UpdateManifest` | 更新资源清单 |
| `CreateDownloader` | 创建资源下载器 |
| `Download` | 下载远端文件 |
| `UpdateDone` | 更新流程完毕 |

## 8. 使用说明

### 8.1 启动流程

启动流程会自动执行，无需手动调用。流程系统会根据当前运行模式自动选择合适的流程路径。

### 8.2 监听热更事件

在 `WinLauncher` 或其他 UI 脚本中监听事件来更新界面：

```csharp
public class WinLauncher : Window
{
    protected override void OnInit()
    {
        // 订阅热更事件
        GlobalModule.EventModule.Subscribe(AssetDownloadProgressEventArgs.EventId, OnDownloadProgress);
        GlobalModule.EventModule.Subscribe(AssetUpdateStateChangeEventArgs.EventId, OnStateChange);
    }

    private void OnDownloadProgress(object sender, GameEventArgs e)
    {
        var args = (AssetDownloadProgressEventArgs)e;
        // 更新进度条
        progressBar.value = (float)args.CurrentDownloadBytes / args.TotalDownloadBytes;
        // 更新文本
        progressText.text = $"{args.CurrentDownloadCount}/{args.TotalDownloadCount}";
    }

    private void OnStateChange(object sender, GameEventArgs e)
    {
        var args = (AssetUpdateStateChangeEventArgs)e;
        // 更新状态文本
        stateText.text = args.CurrentStates.ToString();
    }
}
```

### 8.3 运行模式

| 模式 | 说明 |
|------|------|
| `EditorSimulateMode` | 编辑器模拟模式，无需构建 AssetBundle |
| `OfflinePlayMode` | 单机离线模式，读取 StreamingAssets |
| `HostPlayMode` | 热更模式，支持资源热更新 |

运行模式在 `AssetSetting` 中配置。
