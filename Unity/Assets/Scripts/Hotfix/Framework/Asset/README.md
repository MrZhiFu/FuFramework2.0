# FuFramework Asset Module

## 1. 简介

FuFramework Asset 模块是基于 [YooAsset](https://www.yooasset.com/) 进行二次封装的资源管理系统。它集成了 [UniTask](https://github.com/Cysharp/UniTask) 以提供现代化的异步编程体验，并作为 FuFramework 的核心模块运行。该模块旨在简化资源加载、卸载、热更新以及场景管理流程。

## 2. 特性

- **YooAsset 深度集成**：屏蔽底层复杂性，提供统一的 API 接口。
- **UniTask 异步支持**：所有异步操作均返回 `UniTask`，避免回调地狱。
- **多模式支持**：
  - `EditorSimulateMode`: 编辑器模拟模式，无需构建 AssetBundle 即可快速开发。
  - `OfflinePlayMode`: 单机模式，读取 StreamingAssets。
  - `HostPlayMode`: 联机模式，支持资源热更新。
  - `WebPlayMode`: WebGL 模式，支持微信/字节小游戏适配。
- **资源注册器**：提供 `AssetLoadRegister` 辅助类，作为"逻辑分组的资源装载器"（如 UI 包）整组加载、整组释放。
- **整组释放**：`AssetLoadRegister.UnloadAll()` 临时卸载、`Dispose()` 永久废弃，一键释放整组句柄让 AssetBundle 可卸载。

## 3. 核心类说明

### AssetModule

核心管理器，继承自 `ModuleBase`。负责整个系统的初始化、配置读取以及全局资源操作。

#### 初始化流程

YooAsset 与默认资源包的初始化由 AOT 启动流程 `LaunchAssetHelper` 完成；`AssetModule` 仅缓存默认包名，提供资源加载、卸载与查询能力。

#### 资源加载方法

##### 异步加载资源(推荐使用)

```csharp
// 按路径加载
UniTask<AssetHandle> LoadAssetAsync(string path)
UniTask<AssetHandle> LoadAssetAsync<T>(string path) where T : Object
UniTask<AssetHandle> LoadAssetAsync(string path, Type type)
```

##### 异步加载场景

```csharp
UniTask<SceneHandle> LoadSceneAsync(string path, LoadSceneMode sceneMode, bool activateOnLoad = true, Action<float> onProgress = null)
```

> 注意：`activateOnLoad=false`（预加载后手动激活）时 Provider 在手动激活前不会完成，此 UniTask 将一直挂起——当前包装仅支持自动激活（默认 `true`）。
> `onProgress`：加载进度回调（0~1），每帧上报一次直至加载完成；不需要进度时传 `null`。

##### 实例化

```csharp
UniTask<GameObject> InstantiateAsync(string path)  // 异步实例化(推荐使用)
```

> 同一 prefab 多实例共享句柄并引用计数，实例销毁时需调用 `ReleaseInstantiate(path)` 释放引用。

#### 资源查询

```csharp
AssetInfo GetAssetInfo(string path)   // 默认包未就绪返回 null
bool HasAssetPath(string path)
```

#### 资源卸载

```csharp
void UnloadAsset(string assetPath)
```

### AssetLoadRegister

资源加载注册器（纯实例类，非池化）。作为「逻辑分组的资源装载器」使用（如 UI 包）：一组资源整组加载、整组释放，每个实例归单一调用方持有。

#### 主要功能

- 加载资源（仅提供异步加载接口）
- 记录加载过的资源句柄，避免重复加载
- 支持一键卸载所有资源（临时 `UnloadAll` / 永久 `Dispose`）

#### 使用方法

```csharp
// 创建资源装载器（纯实例类，直接 new）
var loader = new AssetLoadRegister();

// 加载资源
var prefab = await loader.LoadAsync<GameObject>("Assets/Game/Prefabs/MyHero.prefab");

// 实例化
var go = await loader.InstantiateAsync("Assets/Game/Prefabs/MyHero.prefab");

// 卸载指定资源
loader.Unload("Assets/Game/Prefabs/MyHero.prefab");

// 临时卸载所有资源（装载器保留可复用，供重载）
loader.UnloadAll();

// 永久废弃装载器（释放全部句柄并标记废弃，在途加载据此中止）
loader.Dispose();
```

> **实例化释放契约**：`AssetLoadRegister.InstantiateAsync` 返回的实例销毁时**不会自动释放**资源——
> 句柄缓存在 loader 的 `m_HandleDict` 中，释放依赖业务调用 `loader.Unload(path)` 或整体 `loader.UnloadAll()`/`loader.Dispose()`（loader 生命周期管理）。
> 与 `AssetModule.InstantiateAsync` 不同（后者实例销毁时须调用 `ReleaseInstantiate(path)`，按引用计数释放）。

### EPatchStates

补丁系统更新状态枚举：

- `UpdateVersion` - 更新资源版本
- `UpdateManifest` - 更新补丁清单
- `CreateDownloader` - 创建下载器
- `Download` - 下载远端文件
- `UpdateDone` - 更新流程完毕

## 4. 运行模式详解

### EditorSimulateMode（编辑器模拟模式）

- 仅在编辑器下可用
- 无需构建 AssetBundle 即可快速开发和测试
- 自动模拟构建结果

### OfflinePlayMode（单机运行模式）

- 读取 StreamingAssets 中的资源
- 不支持热更新
- 适用于单机游戏或无需热更的应用

### HostPlayMode（联机运行模式）

- 支持资源热更新
- 需要配置下载 URL 和备用下载 URL
- 使用缓存文件系统管理下载的资源

### WebPlayMode（WebGL 运行模式）

- 适配 WebGL 平台
- 支持微信小游戏（`ENABLE_WECHAT_MINI_GAME`）
- 支持字节小游戏（`ENABLE_DOUYIN_MINI_GAME`）
- 自动处理不同平台的文件系统

## 5. 配置说明

资源系统配置统一维护在 `GameSetting`（MonoBehaviour 单例）中，包括运行模式、默认资源包名称、下载并发与重试参数、异步系统时间切片、CDN 根地址等。

- 运行模式与默认资源包名称：`AssetModule` 启动时读取，用于缓存默认包名与日志输出。
- 下载并发数、失败重试次数、异步系统每帧最大时间切片：由 AOT 启动流程 `LaunchAssetHelper` 读取并应用于 YooAsset（下载器与异步系统参数）。

> 重构后 `AssetModule` 不再直接初始化 YooAsset / 默认资源包，也不对外暴露下载与异步系统参数属性。

## 6. 使用示例

### 异步加载资源

```csharp
// 获取 AssetModule 模块
var assetModule = ModuleManager.GetModule<AssetModule>();

// 异步加载 GameObject
var assetHandle = await assetModule.LoadAssetAsync<GameObject>("Assets/Game/Prefabs/MyCube.prefab");
if (assetHandle.AssetObject != null)
{
    // 实例化到场景
    var go = assetHandle.InstantiateSync();
    // 或使用异步实例化
    var goAsync = await assetHandle.InstantiateAsync();
    
    // 使用完毕后释放句柄
    assetHandle.Release();
}

// 直接异步加载并实例化 GameObject
GameObject instance = await assetModule.InstantiateAsync("Assets/Game/Prefabs/MyHero.prefab");

// 使用 AssetLoadRegister (推荐用于 UI 或特定逻辑块)
var loader = new AssetLoadRegister();

// 加载资源
var prefab = await loader.LoadAsync<GameObject>("Assets/Game/Prefabs/MyHero.prefab");
var instance1 = Object.Instantiate(prefab);

// 或直接加载并实例化
var instance2 = await loader.InstantiateAsync("Assets/Game/Prefabs/MyHero.prefab");

// 业务结束时卸载所有资源（装载器保留可复用，或改用 Dispose() 永久废弃）
loader.UnloadAll();
```

### 加载场景

```csharp
await assetModule.LoadSceneAsync("Assets/Game/Scenes/GameScene.unity", LoadSceneMode.Single);
```

## 7. 依赖

- [YooAsset](https://www.yooasset.com/) - 资源管理核心
- [UniTask](https://github.com/Cysharp/UniTask) - 异步编程支持
- Hotfix.Framework.Core - 框架核心模块
- Hotfix.Framework.Event - 事件系统

## 8. 注意事项

### 通用注意事项

1. **句柄释放契约**：`LoadAssetAsync` 系列返回的句柄必须在使用完毕后 `Release()`，否则 provider 引用计数不归零、资源永不卸载。`AssetModule.InstantiateAsync` 返回的实例对象销毁时必须调用 `ReleaseInstantiate(path)`；`AssetLoadRegister.UnloadAll()`/`Dispose()` 会释放其加载的所有句柄。
2. **并发去重**：同一路径并发加载共享 `UniTaskCompletionSource`（可多次 await），失败会传播给所有等待者；切勿把 `m_InstantiateLoadingTasks` 中存储的 `UniTask` 改为 async 方法返回值（async UniTask 只能 await 一次）。
3. **失败句柄透传**：加载失败时（路径无效、类型不匹配等）包装方法返回失败的句柄而非抛异常（与 YooAsset `OperationAwaiter` "业务失败不视为异常" 契约一致），调用方须检查 `handle.Status == EOperationStatus.Succeeded` 后再取资源。
4. **`m_IsDisposed` 与热更重载**：模块销毁（`OnDispose`）后 `InstantiateAsync` 抛 `ObjectDisposedException`；`ModuleManager.ReInit` 会重置销毁标记，热更重载后可正常使用。
5. **`AutoUnloadBundleWhenUnused` 为 false**（项目默认）：句柄释放不会自动卸载 bundle，需配合 `UnloadAsset` 显式卸载。
6. **YooAssets 未初始化防御**：`YooAssets.Destroy()` 后调用卸载方法（`UnloadAsset`）及查询方法（`GetAssetInfo`/`HasAssetPath`）时**不抛异常**（返回默认值/直接返回）。
7. **`AssetLoadRegister` 废弃/卸载防护**：`Dispose()`/`UnloadAll()` 后在途加载任务完成时检测到 `m_Disposed`/`m_Unloaded`，会释放句柄并抛 `ObjectDisposedException`，不再写回缓存（防止句柄无人释放，或 ref→0 后资源被重新缓存）。
8. 热更新流程需要通过事件系统监听各个阶段的状态。

### WebGL / 小游戏平台注意事项（详情参考：https://www.yooasset.com/docs/MiniGame）

#### 网页游戏

- **不支持同步加载**
- **不支持原生文件构建管线**
- **不支持下载器**

#### 小游戏宿主 (MiniHost)

- **不支持同步加载**
- **不支持原生文件构建管线**
- **不支持下载器**
- 使用 UOS CDN 时需要关闭 URL 尾部自动添加的时间戳（设置 `appendTimeTicks = false`）

#### 微信小游戏

- **不支持同步加载**（v2.3.x 版本开始支持加密）
- **不支持原生文件构建管线**
- Bundle 文件名称**不要带有中文**
- StreamingAssets 目录**不需要放置任何资源**
- **禁止对资源清单版本文件进行缓存**（文件名样例：`yourPackageName.version`）
- URL 地址里**不要包含双反斜杠**，例如：`www.cdn.com/v1.0/android//xxx.bundle`
- URL 地址里**不要包含 Windows 斜杠**，例如：`\` 或 `\\`
- URL 地址里**不要带端口信息**，例如：`http://127.0.0.1:80`

#### 抖音小游戏

- **不支持同步加载**
- **不支持原生文件构建管线**
- 需要定义 `ENABLE_DOUYIN_MINI_GAME` 宏
- 支持不传下载 URL 时使用抖音默认 CDN 配置

#### 支付宝小游戏

- **不支持同步加载**
- **不支持原生文件构建管线**
- **不支持下载器**

#### TapTap 小游戏

- **不支持同步加载**
- **不支持原生文件构建管线**
- **不支持下载器**
