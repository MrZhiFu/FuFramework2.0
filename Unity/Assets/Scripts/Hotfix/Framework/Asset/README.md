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
UniTask<RawFileHandle> LoadRawFileAsync(string path)
UniTask<RawFileHandle> LoadRawFileAsync(AssetInfo assetInfo)
```

##### 同步加载资源(不推荐使用)

```csharp
AssetHandle LoadAssetSync(string path)
AssetHandle LoadAssetSync<T>(string path) where T : Object
AssetHandle LoadAssetSync(string path, Type type)
AssetHandle LoadAssetSync(AssetInfo assetInfo)

AllAssetsHandle LoadAllAssetsSync(string path)
AllAssetsHandle LoadAllAssetsSync<T>(string path) where T : Object
AllAssetsHandle LoadAllAssetsSync(string path, Type type)
AllAssetsHandle LoadAllAssetsSync(AssetInfo assetInfo)

SubAssetsHandle LoadSubAssetSync(string path)

RawFileHandle LoadRawFileSync(string path)
RawFileHandle LoadRawFileSync(AssetInfo assetInfo)
```

##### 异步加载场景

```csharp
UniTask<SceneHandle> LoadSceneAsync(string path, LoadSceneMode sceneMode, bool activateOnLoad = true)
UniTask<SceneHandle> LoadSceneAsync(AssetInfo assetInfo, LoadSceneMode sceneMode, bool activateOnLoad = true)
```

##### 实例化

```csharp
UniTask<GameObject> InstantiateAsync(string path)  // 异步实例化(推荐使用)
GameObject InstantiateSync(string path)   // 同步实例化(不推荐使用)
```

#### 资源包管理

```csharp
// 初始化资源包
UniTask<bool> InitPackageAsync(string packageName, string downloadURL = null, string fallbackDownloadURL = null, bool isDefaultPackage = true)

// 资源包操作
ResourcePackage CreatePackage(string packageName)
ResourcePackage TryGetPackage(string packageName)
bool HasPackage(string packageName)
void SetDefaultPackage(ResourcePackage package)
```

#### 资源卸载

```csharp
void UnloadAsset(string path)
UniTask UnloadAllAssetsAsync(string packageName)
void UnloadUnusedAssets()
UniTask UnloadUnusedAssetsAsync()
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
    "https://your-cdn.com/assets/",
    "https://your-backup-cdn.com/assets/"
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
```

### 子资源加载（如图集中的精灵）

```csharp
var subAssetsHandle = await assetModule.LoadSubAssetsAsync<Sprite>("Assets/Game/Atlases/UIAtlas.spriteatlas");
Sprite[] sprites = subAssetsHandle.GetSubAssetObjects<Sprite>();
```

## 8. 依赖

- [YooAsset](https://www.yooasset.com/) - 资源管理核心
- [UniTask](https://github.com/Cysharp/UniTask) - 异步编程支持
- FuFramework.Core - 框架核心模块
- FuFramework.Event - 事件系统
- FuFramework.ReferencePool - 引用池系统

## 9. 注意事项

### 通用注意事项

1. 使用 `AssetLoadRegister` 时，重复加载同一资源会返回已缓存的句柄
2. 资源句柄使用完毕后需要调用 `Release()` 释放，否则会导致内存泄漏
3. 热更新流程需要通过事件系统监听各个阶段的状态

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
