# FuFramework Asset Module

## 简介
FuFramework Asset 模块是基于 [YooAsset](https://www.yooasset.com/) 进行二次封装的资源管理系统。它集成了 [UniTask](https://github.com/Cysharp/UniTask) 以提供现代化的异步编程体验，并作为 FuFramework 的核心模块运行。该模块旨在简化资源加载、卸载、热更新以及场景管理流程。

## 特性
- **YooAsset 深度集成**：屏蔽底层复杂性，提供统一的 API 接口。
- **UniTask 异步支持**：所有异步操作均返回 `UniTask`，避免回调地狱。
- **多模式支持**：
  - `EditorSimulateMode`: 编辑器模拟模式，无需构建 AssetBundle 即可快速开发。
  - `OfflinePlayMode`: 单机模式，读取 StreamingAssets。
  - `HostPlayMode`: 联机模式，支持资源热更新。
  - `WebPlayMode`: WebGL 模式，支持微信/字节小游戏适配。
- **资源生命周期管理**：提供 `AssetLoadRegister` 辅助类，方便管理特定上下文（如 UI 窗口）的资源引用与释放。
- **完善的事件系统**：提供热更新流程中各个阶段的事件通知（下载进度、状态变更、错误处理等）。

## 核心类说明

### AssetManager
核心管理器，继承自 `FuModule`。负责整个系统的初始化、配置读取以及全局资源操作。

- **初始化**：根据 `AssetSetting` 配置自动选择运行模式。
- **资源加载**：`LoadAssetAsync<T>`, `LoadSceneAsync`, `LoadRawFileAsync`。
- **资源卸载**：`UnloadAsset`, `UnloadUnusedAssetsAsync`。

### AssetLoadRegister
资源加载注册器，实现了 `IReference` 接口。
- 用于在特定业务逻辑中加载资源，并记录已加载的资源路径。
- 支持 `UnloadAll()` 一键释放该注册器加载的所有资源，防止内存泄漏。

## 使用示例

### 1. 异步加载资源
```csharp
// 获取 AssetManager 模块
var assetManager = ModuleManager.GetModule<AssetManager>();

// 异步加载 GameObject
var assetHandle = await assetManager.LoadAssetAsync<GameObject>("Assets/Game/Prefabs/MyCube.prefab");
if (assetHandle.AssetObject != null)
{
    var go = assetHandle.InstantiateSync();
    // ...
}

// 使用 AssetLoadRegister (推荐用于 UI 或特定逻辑块)
var loader = AssetLoadRegister.Create();
var prefab = await loader.Load<GameObject>("Assets/Game/Prefabs/MyHero.prefab");
// 业务结束时释放
loader.Release(); // 会自动卸载通过此 loader 加载的所有资源
```

### 2. 加载场景
```csharp
await assetManager.LoadSceneAsync("Assets/Game/Scenes/GameScene.unity", LoadSceneMode.Single);
```

### 3. 监听热更新事件
模块提供了丰富的事件用于构建热更新 UI：
- `AssetDownloadProgressUpdateEventArgs`: 下载进度。
- `AssetPatchStatesChangeEventArgs`: 流程状态变更（检查更新、下载中、更新完成等）。
- `AssetWebFileDownloadFailedEventArgs`: 下载失败。

## 配置
资源系统的配置位于 `AssetSetting` (ScriptableObject) 中，包括：
- **PlayMode**: 运行模式。
- **DefaultPackageName**: 默认资源包名称。
- **DownloadingMaxNum**: 最大并发下载数。
- **AsyncSystemMaxSlicePerFrame**: 异步系统每帧最大时间切片。

## 目录结构
- `Runtime/`: 核心运行时代码。
  - `Event/`: 事件定义。
  - `AssetManager.cs`: 主逻辑。
  - `AssetLoadRegister.cs`: 资源注册器。
- `Editor/`: 编辑器扩展代码。
