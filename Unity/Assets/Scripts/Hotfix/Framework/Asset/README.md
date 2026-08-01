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
- **资源注册器**：提供 `AssetLoadRegister` 辅助类，方便在特定业务逻辑中加载资源并管理其引用与释放。
- **引用池集成**：`AssetLoadRegister` 实现 `IReference` 接口，支持引用池管理。

## 3. 核心类说明

### AssetModule

核心管理器，继承自 `ModuleBase`。负责整个系统的初始化、配置读取以及全局资源操作。

#### 主要属性

| 属性                            | 类型          | 说明               |
| ----------------------------- | ----------- | ---------------- |
| `PlayMode`                    | `EPlayMode` | 资源运行模式           |
| `DefaultPackageName`          | `string`    | 默认资源包名称          |
| `DownloadingMaxNum`           | `int`       | 资源下载最大并发数量       |
| `FailedTryAgainNum`           | `int`       | 资源下载失败重试次数       |
| `AsyncSystemMaxSlicePerFrame` | `int`       | 异步系统每帧最大时间切片（毫秒） |

#### 初始化流程

1. 从 `AssetSetting` 读取配置参数
2. 初始化 YooAsset 并设置异步系统参数

#### 资源加载方法

##### 异步加载资源(推荐使用)

```csharp
// 按路径加载
UniTask<AssetHandle> LoadAssetAsync(string path)
UniTask<AssetHandle> LoadAssetAsync<T>(string path) where T : Object
UniTask<AssetHandle> LoadAssetAsync(string path, Type type)
UniTask<AssetHandle> LoadAssetAsync(AssetInfo assetInfo)

// 加载全部资源
UniTask<AllAssetsHandle> LoadAllAssetsAsync(string path)
UniTask<AllAssetsHandle> LoadAllAssetsAsync<T>(string path) where T : Object
UniTask<AllAssetsHandle> LoadAllAssetsAsync(string path, Type type)
UniTask<AllAssetsHandle> LoadAllAssetsAsync(AssetInfo assetInfo)

// 加载子资源
UniTask<SubAssetsHandle> LoadSubAssetsAsync(string path)
UniTask<SubAssetsHandle> LoadSubAssetsAsync<T>(string path) where T : Object
UniTask<SubAssetsHandle> LoadSubAssetsAsync(string path, Type type)
UniTask<SubAssetsHandle> LoadSubAssetsAsync(AssetInfo assetInfo)

// 加载原生文件
UniTask<BundleFileHandle> LoadRawFileAsync(string path)
UniTask<BundleFileHandle> LoadRawFileAsync(AssetInfo assetInfo)
```

##### 异步加载场景

```csharp
UniTask<SceneHandle> LoadSceneAsync(string path, LoadSceneMode sceneMode, bool activateOnLoad = true)
UniTask<SceneHandle> LoadSceneAsync(AssetInfo assetInfo, LoadSceneMode sceneMode, bool activateOnLoad = true)
```

> 注意：`activateOnLoad=false`（预加载后手动激活）时 Provider 在手动激活前不会完成，此 UniTask 将一直挂起——当前包装仅支持自动激活（默认 `true`）。

##### 实例化

```csharp
UniTask<GameObject> InstantiateAsync(string path)  // 异步实例化(推荐使用)
```

> 同一 prefab 多实例共享句柄并引用计数，实例销毁时需调用 `ReleaseInstantiate(path)` 释放引用。

#### 资源包管理

```csharp
// 初始化资源包（同一包并发初始化共享任务，失败后可重试）
UniTask<bool> InitPackageAsync(string packageName, string downloadURL = null, string downloadBackupURL = null)

// 资源包操作
ResourcePackage CreatePackage(string packageName)
ResourcePackage TryGetPackage(string packageName)
bool HasPackage(string packageName)
void SetDefaultPackage(ResourcePackage package)  // [Obsolete] YooAsset v3 已移除全局默认包概念
```

#### 资源卸载

```csharp
void UnloadAsset(string assetPath)
void UnloadAsset(string packageName, string assetPath)
UniTaskVoid UnloadAllAssetsAsync(string packageName)
UniTaskVoid UnloadUnusedAssetsAsync(string packageName)
UniTaskVoid ClearAllBundleFilesAsync(string packageName)
UniTaskVoid ClearUnusedBundleFilesAsync(string packageName)
```

### AssetLoadRegister

资源加载注册器，实现了 `IReference` 接口，方便使用引用池管理。

#### 主要功能

- 加载资源（仅提供异步加载接口）
- 记录加载过的资源句柄，避免重复加载
- 自动管理资源引用计数
- 支持一键卸载所有资源

#### 使用方法

```csharp
// 从引用池获取
var loader = AssetLoadRegister.Create();

// 加载资源
var prefab = await loader.LoadAsync<GameObject>("Assets/Game/Prefabs/MyHero.prefab");

// 实例化
var go = await loader.InstantiateAsync("Assets/Game/Prefabs/MyHero.prefab");

// 卸载指定资源
loader.Unload("Assets/Game/Prefabs/MyHero.prefab");

// 卸载所有资源
loader.UnloadAll();

// 归还引用池（会自动调用 UnloadAll）
loader.Release();
```

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

资源系统的配置位于 `AssetSetting` (ScriptableObject) 中：

| 配置项                           | 类型          | 说明               |
| ----------------------------- | ----------- | ---------------- |
| `PlayMode`                    | `EPlayMode` | 运行模式             |
| `DefaultPackageName`          | `string`    | 默认资源包名称          |
| `DownloadingMaxNum`           | `int`       | 最大并发下载数          |
| `FailedTryAgainNum`           | `int`       | 下载失败重试次数         |
| `AsyncSystemMaxSlicePerFrame` | `int`       | 异步系统每帧最大时间切片（毫秒） |

## 6. 使用示例

### 初始化资源包

```csharp
var assetModule = ModuleManager.GetModule<AssetModule>();

// HostPlayMode 需要传入下载地址
bool success = await assetModule.InitPackageAsync(
    "DefaultPackage",
    downloadURL: "https://your-cdn.com/assets/",
    downloadBackupURL: "https://your-backup-cdn.com/assets/"
);
```

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
var loader = AssetLoadRegister.Create();

// 加载资源
var prefab = await loader.LoadAsync<GameObject>("Assets/Game/Prefabs/MyHero.prefab");
var instance1 = Object.Instantiate(prefab);

// 或直接加载并实例化
var instance2 = await loader.InstantiateAsync("Assets/Game/Prefabs/MyHero.prefab");

// 业务结束时释放
loader.Release(); // 会自动卸载通过此 loader 加载的所有资源
```

### 加载场景

```csharp
await assetModule.LoadSceneAsync("Assets/Game/Scenes/GameScene.unity", LoadSceneMode.Single);
```

### 加载原生文件

```csharp
var rawFileHandle = await assetModule.LoadRawFileAsync("Assets/Game/Data/config.json");
byte[] fileData = rawFileHandle.GetRawFileData();
string fileText = rawFileHandle.GetRawFileText();
rawFileHandle.Release(); // RAW 文件句柄同为引用计数，使用完毕后必须释放
```

### 子资源加载（如图集中的精灵）

```csharp
var subAssetsHandle = await assetModule.LoadSubAssetsAsync<Sprite>("Assets/Game/Atlases/UIAtlas.spriteatlas");
Sprite[] sprites = subAssetsHandle.GetSubAssetObjects<Sprite>();
```

## 8. 依赖

- [YooAsset](https://www.yooasset.com/) - 资源管理核心
- [UniTask](https://github.com/Cysharp/UniTask) - 异步编程支持
- Hotfix.Framework.Core - 框架核心模块
- Hotfix.Framework.Event - 事件系统
- Hotfix.Framework.ReferencePools - 引用池系统

## 9. 注意事项

### 通用注意事项

1. **句柄释放契约**：`LoadAssetAsync` 系列返回的句柄必须在使用完毕后 `Release()`，否则 provider 引用计数不归零、资源永不卸载。`AssetModule.InstantiateAsync` 返回的实例对象销毁时必须调用 `ReleaseInstantiate(path)`；`AssetLoadRegister.Release()` 会自动释放其加载的所有句柄。
2. **并发去重**：同一路径并发加载共享 `UniTaskCompletionSource`（可多次 await），失败会传播给所有等待者；切勿把 `m_LoadingTasks`/`m_InstantiateLoadingTasks` 中存储的 `UniTask` 改为 async 方法返回值（async UniTask 只能 await 一次）。
3. **失败句柄透传**：加载失败时（路径无效、类型不匹配等）包装方法返回失败的句柄而非抛异常（与 YooAsset `OperationAwaiter` "业务失败不视为异常" 契约一致），调用方须检查 `handle.Status == EOperationStatus.Succeeded` 后再取资源。
4. **`UnloadAllAssetsAsync` 挂起风险**：该操作会释放所有已加载句柄，进行中的 `LoadAssetAsync` 句柄被 Release 后 `Completed` 回调不再触发，其 UniTask 将永久挂起，请确保调用时无进行中的加载。
5. **`m_IsDisposed` 与热更重载**：模块销毁（`OnDispose`）后 `InstantiateAsync` 抛 `ObjectDisposedException`；`ModuleManager.ReInit` 会重置销毁标记，热更重载后可正常使用。
6. **`AutoUnloadBundleWhenUnused` 为 false**（项目默认）：句柄释放不会自动卸载 bundle，需配合 `UnloadAsset`/`UnloadUnusedAssetsAsync` 显式卸载。
7. 热更新流程需要通过事件系统监听各个阶段的状态。

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
