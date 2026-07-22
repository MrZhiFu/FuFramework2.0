# AOT 模块

## 1. 简介

AOT（Ahead-of-Time）是游戏随包体预编译的代码，负责**极简引导流程**：显示加载界面 → 初始化资源包 → 检查更新 → 下载资源 → 加载 Hotfix 程序集 → 进入热更。

基于 HybridCLR，AOT 仅保留必需的引导代码、`GameSetting` 全局配置和少量基础工具（日志、路径、JSON 等），所有框架模块和游戏逻辑均在 Hotfix 中。

## 2. 目录结构

```
AOT/
├── Launcher.cs              # AOT 入口 MonoBehaviour（挂载到首个场景 GameObject，启动引导流程）
├── GameSetting.cs           # 游戏全局配置（帧率、速度、资源模式、存储加密等），挂载到 Launcher 同一 GameObject
├── AOT.asmdef               # AOT 程序集定义
├── Launch/                  # 启动流程
│   ├── LaunchView.cs         # AOT 加载界面（实现 ILaunchView）—— 自包含 FairyGUI 包
│   ├── LaunchAssetHelper.cs  # 资源初始化助手（封装 YooAsset 初始化/版本/清单/下载）
│   ├── LaunchProcess.cs      # 引导流程编排（线性 async）—— 完成后反射进入热更
│   ├── ILaunchView.cs        # 加载界面接口（供 Hotfix 消费）
│   ├── UI/                      # 加载界面 FairyGUI 自动生成代码
│   └── UpdateConfig/            # 远端更新配置
│       └── RemoteUpdateConfig.cs
└── Framework/
    └── Core/                    # AOT 侧基础工具（日志、路径、JSON、版本等）
        ├── Log/                 # 日志系统（FuLogger）
        ├── Extension/           # C# 类型扩展方法
        └── Utility/             # 静态工具类（Application、File、Json、Path、Version）
```

## 3. GameSetting（游戏全局配置）

`GameSetting` 是挂载在 Launcher 同一 GameObject 上的 MonoBehaviour 单例，将所有运行时配置集中到一处，在 Inspector 中统一编辑。

| 配置分组 | 主要字段 |
|----------|----------|
| 游戏基本设置 | 帧率（`FrameRate`）、游戏速度（`GameSpeed`）、后台运行、禁止休眠、是否开启引导 |
| 资源系统配置 | YooAsset 运行模式（`PlayMode`）、默认包名、下载并发数、失败重试、CDN 地址 |
| 本地数据存储配置 | 自动保存开关/间隔、加密开关/密钥 |

通过 `GameSetting.Instance` 全局访问，AOT 和 Hotfix 均可使用。

## 4. Launcher（AOT 入口）

`Launcher` 是实现 `ILaunchView` 的 MonoBehaviour，在 `Start()` 中调用 `LaunchProcess.RunAsync(this)` 启动引导流程。同时负责 `DontDestroyOnLoad` 保证 `GameSetting` 跨场景存活。

## 5. 引导流程

详见 [Launch README](Launch/README.md)。

## 6. 运行模式

`GameSetting.PlayMode` 决定 YooAsset 资源运行模式：

| 模式 | 说明 |
|------|------|
| `EditorSimulateMode` | 编辑器模拟模式，无需构建 AssetBundle |
| `OfflinePlayMode` | 单机离线模式，读取 StreamingAssets |
| `HostPlayMode` | 联机热更模式，支持资源热更新 |

## 7. 跨程序集共享

- **`LaunchAssetHelper.YooAssetInitialized`**：AOT 写入、Hotfix 读取，防止 YooAsset 二次初始化
- **`ILaunchView`**：定义在 AOT，供 HotfixLauncher 通过接口调用 `SetTip`/`Close`，保证契约解耦
- **`GameSetting.Instance`**：AOT 侧 MonoBehaviour 单例，Hotfix 通过 AOT 引用直接访问
