# AOT 模块

## 1. 简介

AOT 模块是游戏随包体预编译的部分（Ahead-of-Time），负责**极简引导流程**：显示加载界面 → 初始化资源包 → 检查更新 → 下载资源 → 加载 Hotfix 程序集 → 反射移交热更入口。

基于 HybridCLR，AOT 仅保留必需的引导代码和基础工具，所有框架模块和游戏逻辑均在 Hotfix 中。

## 2. 特性

- **极简引导**：线性流程（非 Procedure 状态机），仅一个 `BootstrapProcess` 静态类
- **资源热更新**：集成 YooAsset，支持版本检测、清单更新、资源下载
- **代码热修复**：集成 HybridCLR，支持 AOT 元数据补充和 Hotfix.dll 加载
- **委托桥接**：`GameDriven`（MonoSingleton）持有委托自驱动帧循环，Hotfix 侧挂接 ModuleManager 生命周期方法
- **多模式支持**：编辑器模拟模式、单机离线模式、联机热更模式

## 3. 目录结构

```
AOT/
├── Bootstrap/                  # 引导流程
│   ├── BootstrapView.cs        # AOT 加载界面（实现 IBootstrapView）——自包含 FairyGUI 包
│   ├── BootstrapAssetHelper.cs # 资源初始化助手（封装 YooAsset 初始化/版本/清单/下载）
│   ├── BootstrapProcess.cs     # 引导流程编排（线性 async，非 Procedure 状态机）
│   └── UpdateConfig/           # 远端更新配置
│       └── RemoteUpdateConfig.cs
├── Launcher.cs                 # AOT 入口 MonoBehaviour（启动引导流程）
├── GameDriven.cs               # MonoSingleton 帧驱动 + 游戏控制中枢
└── AOT.asmdef                  # AOT 程序集定义
```

## 4. 核心流程说明

### 4.1 引导流程图

```
Launcher.Start()
    │
    └── BootstrapProcess.RunAsync()
            │
            ├── BootstrapView.CreateAsync()           // 显示加载界面
            ├── ReqRemoteUpdateConfigWithRetry()       // 联机模式：获取远端配置
            │     ├─ 强更 → 弹窗打开 App 下载链接 → return
            │     └─ 非强更 → 继续
            ├── BootstrapAssetHelper.InitPackageAsync() // 初始化 YooAsset 资源包
            ├── RequestVersionAsync()                   // 获取资源版本号（失败重试）
            ├── UpdateManifestAsync()                   // 更新资源清单（失败重试）
            ├── CreateAndDownload()                     // 联机模式：下载资源
            └── LoadHotfixAndHandoff()                  // 加载 AOT 元数据 + Hotfix.dll → 反射进入热更
                    │
                    └── 进入热更逻辑...
```

### 4.2 BootstrapProcess（引导流程编排）

所有引导逻辑集中在 `BootstrapProcess` 一个静态类中，以线性 async/await 方式编写。关键方法：

| 方法 | 说明 |
|------|------|
| `RunAsync()` | 总入口，按顺序执行全部引导步骤，完成后内置反射进入热更 |
| `ReqRemoteUpdateConfigWithRetry()` | 向 CDN 获取 `RemoteUpdateConfig.json`，失败自动重试 |
| `CreateAndDownload(updateConfig)` | 创建 YooAsset 下载器并下载，失败自动重试 |
| `LoadHotfixAndHandoff()` | 加载 AOT 元数据 + Hotfix.dll，完成后反射调用 HotfixLauncher |
| `EnterHotfixAsync()` | 反射查找 Hotfix 程序集并调用 `HotfixLauncher.MainAsync()` |

### 4.3 BootstrapAssetHelper（资源助手）

封装 YooAsset 底层 API，供 `BootstrapProcess` 调用：

| 方法 | 说明 |
|------|------|
| `InitPackageAsync()` | 初始化资源包（编辑器/离线模式） |
| `InitPackageAsync(url, backupUrl)` | 初始化资源包（联机模式，指定 CDN 地址） |
| `RequestVersionAsync()` | 请求资源包最新版本号 |
| `UpdateManifestAsync(version)` | 更新资源清单 |
| `CreateDownloader()` | 创建资源下载器 |
| `LoadRawFileBytesAsync(path)` | 加载原始文件（用于 AOT 元数据和 Hotfix.dll） |

初始化完成后会向 `BootstrapContext` 写入 `YooAssetInitialized = true` 和 `DefaultPackageName`，供热更侧 `AssetModule` 读取以跳过重复初始化。

### 4.4 BootstrapView（加载界面）

自包含的 FairyGUI 加载界面，实现 `IBootstrapView` 接口：

| 方法 | 说明 |
|------|------|
| `SetTip(text)` | 设置提示文本 |
| `SetProgress(progress, text)` | 更新进度条 |
| `ShowUpdateDialog(msg, onConfirm)` | 弹出更新确认/强更对话框 |
| `SetNeedUpgrade(need)` | 设置升级状态 |
| `SetDownloading(downloading)` | 设置下载中状态 |
| `Close()` | 关闭加载界面 |

`IBootstrapView` 接口定义在 `FuFramework.Core.Runtime` 中，供 HotfixLauncher 通过接口类型消费（`SetTip`/`Close`），保证 AOT → Hotfix 的契约解耦。

## 5. GameDriven（帧驱动 + 游戏控制）

`GameDriven` 继承 `MonoSingleton<GameDriven>`，是 AOT↔Hotfix 的帧驱动桥梁 + 游戏控制中枢。

### 5.1 帧驱动委托

`GameDriven` 暴露 5 个 `public Action` 委托，自驱动 `Update`/`LateUpdate`/`FixedUpdate`：

| 委托 | Hotfix 挂接目标 |
|------|-----------------|
| `GameDriven.Instance.OnUpdate` | `ModuleManager.Update` |
| `GameDriven.Instance.OnLateUpdate` | `ModuleManager.LateUpdate` |
| `GameDriven.Instance.OnFixedUpdate` | `ModuleManager.FixedUpdate` |
| `GameDriven.Instance.DisposeModules` | `ModuleManager.Dispose` |
| `GameDriven.Instance.ReInitModules` | `ModuleManager.ReInit` |

由 `HotfixLauncher.MainAsync()` 在注册完所有模块后进行挂接。AOT 不需要引用 Hotfix 程序集。

### 5.2 游戏控制

| 方法 | 说明 |
|------|------|
| `PauseGame()` | 暂停游戏（委托 `ModuleSetting.Instance.PauseGame()`） |
| `ResumeGame()` | 恢复游戏 |
| `RestartGame()` | 重启游戏（释放并重新初始化所有模块，重新运行引导流程） |
| `QuitGame()` | 退出游戏（释放模块并退出应用） |

## 6. RemoteUpdateConfig - 远端更新配置

位于 CDN 服务器的 `RemoteUpdateConfig.json`：

| 字段 | 类型 | 说明 |
|------|------|------|
| `ForceUpdate` | `bool` | 是否强制更新 App |
| `ShowUpdateTips` | `bool` | 是否显示更新提示框 |
| `UpdateAnnouncement` | `string` | 更新公告内容 |
| `AppDownloadUrl` | `string` | App 下载地址 |
| `ResDownloadUrl` | `string` | 资源下载地址，支持 `{0}` 版本号占位符 |
| `ResDownloadBackupUrl` | `string` | 资源下载备用地址 |

配置示例：

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

## 7. 运行模式

| 模式 | 说明 |
|------|------|
| `EditorSimulateMode` | 编辑器模拟模式，无需构建 AssetBundle |
| `OfflinePlayMode` | 单机离线模式，读取 StreamingAssets |
| `HostPlayMode` | 联机热更模式，支持资源热更新 |

运行模式在 `ModuleSetting` 中的 `AssetSetting` 里配置。

## 8. BootstrapContext（跨 AOT/Hotfix 共享状态）

`BootstrapContext`（定义在 `FuFramework.Core.Runtime`）是跨 AOT/热更程序集的共享状态容器：

| 字段 | 说明 |
|------|------|
| `YooAssetInitialized` | AOT 引导是否已完成 YooAsset 初始化 |
| `DefaultPackageName` | 默认资源包名称 |

- **AOT 写入**：`BootstrapAssetHelper.InitPackageAsync()` 完成后设置
- **Hotfix 读取**：`AssetModule.Init()` 检查后跳过重复初始化
