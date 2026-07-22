# Bootstrap（引导流程）

## 1. 简介

Bootstrap 是 AOT 入口之后的第一阶段，负责**线性异步引导流程**：显示加载界面 → 初始化 YooAsset 资源包 → 检查远端更新 → 下载资源 → 加载 Hotfix 程序集 → 反射进入热更。

采用极简的**静态类 + async/await** 模式（非 Procedure 状态机），仅一个 `BootstrapProcess` 编排全部步骤。

## 2. 目录结构

```
Bootstrap/
├── BootstrapProcess.cs         # 引导流程编排（线性 async，完成后反射进入热更）
├── BootstrapAssetHelper.cs     # YooAsset 资源初始化助手（封装初始化/版本/清单/下载）
├── BootstrapView.cs            # 加载界面 View（实现 IBootstrapView）—— 自包含 FairyGUI 包
├── BootstrapContext.cs         # 跨 AOT/Hotfix 程序集的共享状态容器
├── IBootstrapView.cs           # 加载界面接口（定义在 AOT，供 HotfixLauncher 消费）
├── UI/                         # FairyGUI 自动生成代码
│   └── WinLauncher.Gen.cs
└── UpdateConfig/
    └── RemoteUpdateConfig.cs   # 远端更新配置（CDN JSON 映射）
```

## 3. 引导流程

```
Launcher.Start()
    │
    └── BootstrapProcess.RunAsync()
            │
            ├── BootstrapView.CreateAsync()           // 显示加载界面
            ├── ReqRemoteUpdateConfigWithRetry()       // 联机模式：获取远端 RemoteUpdateConfig.json
            │     ├─ 强更 → 弹窗打开 App 下载链接 → 中止
            │     └─ 非强更 → 继续
            ├── BootstrapAssetHelper.InitPackageAsync() // 初始化 YooAsset 资源包
            ├── RequestVersionAsync()                   // 获取资源版本号（失败重试）
            ├── UpdateManifestAsync()                   // 更新资源清单（失败重试）
            ├── CreateAndDownload()                     // 联机模式：下载资源（失败重试）
            └── LoadHotfixAndHandoff()                  // 加载 AOT 元数据 + Hotfix.dll → 反射进入热更
                    │
                    └── 进入热更逻辑...
```

## 4. 核心类说明

### 4.1 BootstrapProcess

引导流程编排类，所有方法均为 `static`，以 `async UniTask` 方式顺序执行。

| 方法 | 说明 |
|------|------|
| `RunAsync(IBootstrapView)` | 总入口，按顺序执行全部引导步骤 |
| `ReqRemoteUpdateConfigWithRetry()` | 向 CDN 获取 `RemoteUpdateConfig.json`，失败自动重试 |
| `CreateAndDownload(updateConfig)` | 创建 YooAsset 下载器并下载，失败自动重试 |
| `LoadHotfixAndHandoff()` | 加载 AOT 元数据 + Hotfix.dll，完成后反射调用 HotfixLauncher |
| `EnterHotfixAsync()` | 反射查找 Hotfix 程序集并调用 `HotfixLauncher.MainAsync()` |

强更判断逻辑在 `RunAsync` 中：若 `RemoteUpdateConfig.ForceUpdate == true`，弹出更新对话框并中止后续流程。

### 4.2 BootstrapAssetHelper

封装 YooAsset 底层 API，供 `BootstrapProcess` 调用。

| 方法 | 说明 |
|------|------|
| `InitPackageAsync()` | 初始化资源包（编辑器/离线模式，参数从 `GameSetting.Instance` 读取） |
| `InitPackageAsync(url, backupUrl)` | 初始化资源包（联机模式，指定 CDN 地址） |
| `RequestVersionAsync()` | 请求资源包最新版本号（带重试） |
| `UpdateManifestAsync(version)` | 更新资源清单（带重试） |
| `CreateDownloader()` | 创建资源下载器 |
| `LoadRawFileBytesAsync(path)` | 加载原始文件（用于 AOT 元数据和 Hotfix.dll） |

初始化完成后向 `BootstrapContext` 写入 `YooAssetInitialized = true` 和 `DefaultPackageName`，供热更侧 `AssetModule` 读取，跳过重复初始化。

### 4.3 BootstrapView

自包含的 FairyGUI 加载界面，实现 `IBootstrapView` 接口。

| 方法 | 说明 |
|------|------|
| `SetTip(text)` | 设置提示文本 |
| `SetProgress(progress, text)` | 更新进度条 |
| `ShowUpdateDialog(msg, onConfirm)` | 弹出更新确认/强更对话框 |
| `SetNeedUpgrade(need)` | 设置是否需要升级 |
| `SetDownloading(downloading)` | 设置是否正在下载 |
| `Close()` | 关闭加载界面 |

### 4.4 BootstrapContext

跨 AOT/Hotfix 程序集的共享状态容器（定义在 AOT）：

| 字段 | 说明 |
|------|------|
| `YooAssetInitialized` | AOT 引导是否已完成 YooAsset 初始化 |
| `DefaultPackageName` | 默认资源包名称 |

- **AOT 写入**：`BootstrapAssetHelper.InitPackageAsync()` 完成后设置
- **Hotfix 读取**：`AssetModule.OnInit()` 检查后跳过重复初始化

### 4.5 IBootstrapView

加载界面接口，定义在 AOT，供 HotfixLauncher 通过接口类型消费（`SetTip`/`Close`），保证 AOT → Hotfix 的契约解耦。

## 5. RemoteUpdateConfig

位于 CDN 服务器的 `RemoteUpdateConfig.json`，由 `BootstrapProcess` 拉取并解析：

| 字段 | 类型 | 说明 |
|------|------|------|
| `ForceUpdate` | `bool` | 是否强制更新 App |
| `ShowUpdateTips` | `bool` | 是否显示更新提示框 |
| `UpdateAnnouncement` | `string` | 更新公告内容 |
| `AppDownloadUrl` | `string` | App 下载地址 |
| `ResDownloadUrl` | `string` | 资源下载地址，支持 `{0}` 版本号占位符 |
| `ResDownloadBackupUrl` | `string` | 资源下载备用地址 |

## 6. Bootstrap 与 Hotfix 的桥接

```
AOT 侧                          Hotfix 侧
────────                        ─────────
BootstrapContext ────写入────→  AssetModule 读取（跳过重复初始化）
IBootstrapView    ────接口────→  HotfixLauncher.MainAsync(IBootstrapView)
GameSetting.Instance ──引用────→  Hotfix 所有模块通过实例访问
```
