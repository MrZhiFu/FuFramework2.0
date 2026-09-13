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

核心管理器，继承自 `ModuleBase`，实现 `ICancelAsync`（可取消异步对象）。负责整个系统的初始化、配置读取以及全局资源操作。

> **可取消异步**：`AssetModule`/`AssetLoadRegister` 实现 `ICancelAsync`（`Token` + `CancelAsync`）。
> 模块/装载器销毁（`OnDispose`/`Dispose`）时触发 `Token` 取消，其所有在途异步操作随之取消并自动清理
> （`Release` + `UnloadAsset`，抛 `OperationCanceledException`）。框架重启 `RestartGame` 会在重启前
> `await` 各模块 `CancelAsync` 等待清理，保证旧生命周期无在途残留；业务弃用装载器时也可 `await loader.CancelAsync()` 等待清理。
> `UnloadAll`（临时卸载）不取消 Token，装载器可复用。

#### 取消语义边界（YooAsset 底层行为）

**取消保证的是"调用方逻辑立即中断、不拿结果、不泄漏句柄"，而非"底层网络传输立即中止"。**

取消路径分两层：
1. **UniTask 层**：`handle.ToUniTask(cancellationToken, cancelImmediately: true)` 在 Token 取消时**立即**完成 await 并抛 `OperationCanceledException`——不等 YooAsset 加载完成；
2. **AssetModule 层**：`catch (OCE)` 里执行 `handle.Release()`（引用计数归零）+ `UnloadAsset(path)`（尝试卸载）。

对**加载中的资源**，YooAsset 底层的行为（依据 YooAsset 3.0.5 源码）：

| 层面 | 取消后行为 |
|------|-----------|
| UniTask await | 立即抛 OCE，调用方不等 YooAsset |
| 句柄/引用 | `RefCount` 归零，调用方不再持有引用 |
| Provider | **不销毁、继续被调度逐帧驱动**——加载中（`IsLoading`）的 Provider `CanDestroyProvider()` 返回 false（`ProviderBase`） |
| 在途下载/解压 | **不中止**，继续到自然完成（`TryUnloadUnusedAsset` 走不到强制销毁路径，`ShouldAbortDownload` 不被置位） |
| bundle 卸载 | 仅当"已完成 + `RefCount==0`"且开 `AutoUnloadBundleWhenUnused` 才真正卸载 |

**结论**：取消后加载中的 bundle 会下载/解压完并入缓存（下次加载直接命中），只是本次调用方不再拿到句柄。这符合 YooAsset 设计——在途下载通常值得下完缓存，中途掐断反而浪费已下载部分。真正的中止（`AbortOperation`/`RequestForceDestroy`）只在**全局销毁**（包销毁/`YooAssets.Destroy`）时发生。

> 取消路径的 `UnloadAsset(path)` 对在途资源实际是**空操作**（Provider 不可销毁），仅在"已加载完成但恰在取消瞬间"的边界才真正卸载——防御性调用，与 YooAsset 语义一致，无泄漏。

#### 初始化流程

YooAsset 与默认资源包的初始化由 AOT 启动流程 `LaunchAssetHelper` 完成；`AssetModule` 仅缓存默认包名，提供资源加载、卸载与查询能力。

#### 资源加载方法

##### 异步加载资源(推荐使用)

```csharp
// 按路径加载（token 必传：调用方生命周期取消令牌，如窗口关闭时中止）
UniTask<AssetHandle> LoadAssetAsync(string path, CancellationToken token)
UniTask<AssetHandle> LoadAssetAsync<T>(string path, CancellationToken token) where T : Object
UniTask<AssetHandle> LoadAssetAsync(string path, Type type, CancellationToken token)
```

##### 异步加载场景

```csharp
UniTask<SceneHandle> LoadSceneAsync(string path, LoadSceneMode sceneMode, CancellationToken token, Action<float> onProgress = null)
```

> 固定自动激活（`LocalPhysicsMode.None`），未开放自定义。
> `onProgress`：加载进度回调（0~1），每帧上报一次直至加载完成；不需要进度时传 `null`。
> 加载失败时内部释放句柄并抛异常，调用方无需也无法释放失败句柄。
> **已知边界（有意保留，未修改）**：取消与失败路径**只 `Release()` 句柄、不调 `UnloadAsset`**（场景 bundle 的生命周期交由 SceneManager/YooAsset 管理，与资源侧不对称）。而 YooAsset 的场景 `providerKey` **每次加载都唯一**（`$"{AssetKey}-{++实例序号}"`，见 `ResourceManager.cs:174`），故取消/失败后该 SceneProvider 引用计数为 0 **却仍留在 provider 表中并持有共享的 bundle loader**，只能靠后续同路径的 `UnloadAsset` 或整包销毁回收。后果：取消或失败一次场景加载，若其 bundle 已加载成功（失败点在场景句柄级而非 bundle 级），该 bundle 会**驻留到整包销毁**。
> —— 属「驻留」而非「泄漏」（最终仍会被回收），且贸然补 `UnloadAsset` 可能影响「场景重载命中缓存」一类行为，故仅记录、暂不修改。

##### 实例化

```csharp
UniTask<InstantiateResult> InstantiateAsync(string path, CancellationToken token)  // 异步实例化(推荐使用)
void ReleaseInstantiate(InstantiateResult result)          // 实例销毁时释放引用
```

> 同一 prefab 多实例共享句柄并引用计数，实例销毁时需调用 `ReleaseInstantiate(result)` 释放引用（`result` 为 `InstantiateAsync` 返回的结果，携带资源路径与创建时捕获的生命周期 `Token`）。
> 重启（`OnDispose`/`重新初始化`）后旧生命周期存活的实例再调用 `ReleaseInstantiate` 会被 Token 校验识别并忽略，**不会误释放**新生命周期同路径引用（详见 9.注意事项）。

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

// 临时卸载所有资源（装载器保留可复用，供复用）
loader.UnloadAll();

// 永久废弃装载器（释放全部句柄并标记废弃，在途加载据此中止）
loader.Dispose();
```

> **实例化释放契约**：`AssetLoadRegister.InstantiateAsync` 返回的实例销毁时**不会自动释放**资源——
> 句柄缓存在 loader 的 `m_HandleDict` 中，释放依赖业务调用 `loader.Unload(path)` 或整体 `loader.UnloadAll()`/`loader.Dispose()`（loader 生命周期管理）。
> 与 `AssetModule.InstantiateAsync` 不同（后者实例销毁时须调用 `ReleaseInstantiate(result)`，按引用计数释放，且自动防跨代际误释放）。

> **可取消异步**：`AssetLoadRegister` 与 `AssetModule` 遵循同一套 `ICancelAsync` 契约（`Token` + `CancelAsync`）——
> 装载器 `Dispose()` 时触发 `Token` 取消，在途加载随之取消并自动清理（`Release` + `UnloadAsset`，抛 `OperationCanceledException`）；
> 业务弃用装载器时也可 `await loader.CancelAsync()` 等待清理；`UnloadAll`（临时卸载）不取消 Token、装载器可复用。
> 完整说明（含 `RestartGame` 重启约定）见上方 §3「AssetModule」的「可取消异步」。

### HandleBaseExtensions（YooAsset → UniTask 适配层）

位于 `YooAsset.UniTask/`，为 YooAsset 的 `HandleBase` 提供**带取消能力**的等待入口。
它是本模块「UniTask 异步支持」以及上方「取消语义边界」中 `handle.ToUniTask(token, cancelImmediately: true)` 的**实际实现来源**。

YooAsset 自带的 `OperationAwaiter`（即实例方法 `HandleBase.GetAwaiter()`）只支持裸 `await`，**不接受 `CancellationToken`**——
本模块「取消令牌必传 + 与调用方/模块令牌 linked 竞速」的语义无法用它实现，故需要这层扩展。

```csharp
UniTask ToUniTask(this HandleBase handle, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken cancellationToken = default, bool cancelImmediately = false)
```

- 经 `TaskPool` 池化复用，由 `PlayerLoop` 逐帧驱动：每帧轮询句柄完成状态并上报加载进度。
- 完成回调按句柄具体类型（`AssetHandle`/`SceneHandle`/`SubAssetsHandle`/`BundleFileHandle`/`AllAssetsHandle`）**强类型**订阅，规避 IL2CPP 逆变委托崩溃。
- 当前工程仅使用 `ToUniTask` 一个方法（`AssetModule.API.cs` 四处，均传 `cancellationToken` + `cancelImmediately: true`）。上游同名文件中的 `GetAwaiter` / `WithCancellation` 已确认为死代码后移除，详见 §8。

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
var token = CancellationToken.None; // 调用方生命周期取消令牌（必传）；窗口传 WinBase.Token、模块内部传自身 scope Token

// 异步加载 GameObject
var assetHandle = await assetModule.LoadAssetAsync<GameObject>("Assets/Game/Prefabs/MyCube.prefab", token);
if (assetHandle.AssetObject != null)
{
    // 实例化到场景
    var go = assetHandle.InstantiateSync();
    // 使用完毕后释放句柄
    assetHandle.Release();
}

// 直接异步加载并实例化 GameObject（实例销毁时须调用 ReleaseInstantiate(result) 释放引用）
var result = await assetModule.InstantiateAsync("Assets/Game/Prefabs/MyHero.prefab", token);
GameObject instance = result.Instance;

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
await assetModule.LoadSceneAsync("Assets/Game/Scenes/GameScene.unity", LoadSceneMode.Single, token);
```

## 7. 目录结构

```text
Asset/
├── AssetModule.cs                   # 资源管理模块：生命周期、取消范围、内部工具
├── AssetModule.API.cs               # 资源管理模块公共 API（异步加载/实例化/卸载/查询）
├── AssetModule.InstantiateData.cs   # 实例化数据类（InstantiateRef / InstantiateResult）
├── AssetLoadRegister.cs             # 资源加载注册器（逻辑分组的资源装载器）
├── AssetLoadRegister.LoadKey.cs     # 加载缓存键（资源路径 + 加载类型）
├── YooAsset.UniTask/                # YooAsset → UniTask 适配层（官方 Samples 植入，见 §3 末 / §8）
│   └── HandleBaseExtensions.cs      # 句柄的 ToUniTask：带进度上报与取消令牌的等待
└── README.md                        # 本文档
```

## 8. 依赖

- [YooAsset](https://www.yooasset.com/) - 资源管理核心
- [UniTask](https://github.com/Cysharp/UniTask) - 异步编程支持
- Hotfix.Framework.Core - 框架核心模块
- `YooAsset.UniTask/HandleBaseExtensions.cs` - **手工植入**的官方集成代码，来源 `YooAsset 3.0.5 Samples~/UniTask Sample/UniTask/Runtime/External/YooAsset/`。相对上游已作五处改造：
  1. 剥离 `#if YOOASSET_UNITASK_SUPPORT` 守卫（本工程刻意启用该集成）；
  2. 移除 `GetAwaiter` / `WithCancellation` —— 前者被 YooAsset 的 `HandleBase.GetAwaiter()` 实例方法遮蔽，**永不会被编译器选中**；后者全工程零调用点，二者均为死代码；
  3. 移除 `HandleBaseConfiguredSource.cancelImmediately` 字段及其赋值 —— 上游该字段**写而不读**，属死代码。注：UniTask 原版（`UniTask.Delay.cs` 的各 promise 源）会在 `GetResult` 中读它，在「`cancelImmediately` 且令牌已取消」路径上**跳过「重置状态 + 注销取消注册 + 归还对象池」三步**（`TaskTracker.RemoveTracking` 两条路径都会做，**不是差异点**），实例交由 GC 回收；YooAsset 复制时把 `GetResult` 简化为无条件 `TryReturn()`，该读取随之丢失。本工程只删该死字段，其暴露的缺陷按第 4 条修复；
  4. **【缺陷修复】** `TryReturn` 内补两处，修复「取消之后，同一池实例被复用即失效」：
     - **a. 归还池前 `RemoveCompleted()` 退订句柄完成回调**（必须在 `handle = default` 之前，否则引用已丢、退订不掉）—— 退订只在 `HandleCompleted()` 内发生，而**令牌取消路径（回调直接 `TrySetCanceled`）与「虚假完成」路径都不经过它**，会留下活订阅。取消路径下调用方随即 `Release()` 使句柄失效（订阅随之无法触发，无害）；但「虚假完成」路径下调用方**正合法持有**该句柄，其后的真实完成会打进一个已归还池、可能已被复用的实例；
     - **b. 归还池前 `completed = true`** —— 入池后若陈旧 PlayerLoop 槽位（`MoveNext` 首句）或残留完成回调（`HandleCompleted` 首句）触发，会在各自的 `completed` 判定处直接返回，**不再触碰 `core`**。

     > **已实测确认（状态级，与帧时序无关）**：修复前，取消后复用同一池实例时 `Create` 取出的 `core` 已是 `Succeeded`（断言「新建后应为 Pending」触发）→ source「出生即完成」→ 调用方 `await` 不等待、`OperationCanceledException` 不再抛出（`AssetModule` 的取消清理被跳过）。修复后每次复用 `core` 恒为 `Pending`、取消正常抛出 OCE、断言零命中；在最紧的 1 帧窗口下旧句柄完成也不再触发任何回调（退订生效）。
     >
     > **关于「跨操作串扰」的订正（推翻早先记录）**：早先版本写的「陈旧回调落在实例正被使用时会造成串扰」是**基于错误前提的推演**。经复核 YooAsset 源码：`HandleBase.Release()` 会置 `Provider = null`（`HandleBase.cs:28-39`）、`ProviderBase.ReleaseHandle` 会把句柄从 `_handles` 摘除（`ProviderBase.cs:289-299`）、`ProviderBase.InvokeCompletion` 有 `if (handle.IsValid)` 守卫（`ProviderBase.cs:347-363`）——因此**已释放句柄永远收不到完成回调**，该路径在源码层面不可能发生。真正可达的是「虚假完成」路径（调用方仍持有有效句柄），已由第 a 条覆盖。`EditorSimulateMode` 下所有资源加载均**约 1 帧**完成（已实测：1KB 与 5.9MB 同为 1 帧，与体积无关），故帧时序类的推论在本模式下无法验证。
  5. 补中文 XML 注释。

  > **升级提醒**：本文件已偏离上游，不能整份覆盖，需逐项比对上游 `Samples~` 手工同步（标识符沿用上游命名，即为便于比对）。

## 9. 注意事项

### 通用注意事项

1. **句柄释放契约**：`LoadAssetAsync` 系列返回的句柄必须在使用完毕后 `Release()`，否则 provider 引用计数不归零、资源永不卸载。`AssetModule.InstantiateAsync` 返回的实例对象销毁时必须调用 `ReleaseInstantiate(result)`（`result` 携带生命周期 `Token`，重启后旧实例释放会被 Token 校验识别并忽略，不会误伤新生命周期同路径引用）；`AssetLoadRegister.UnloadAll()`/`Dispose()` 会释放其加载的所有句柄。
2. **并发去重与句柄归属**：`AssetLoadRegister` 同一路径+类型并发加载共享 `UniTaskCompletionSource`（其 `Task` 可被多个调用方 await），失败会传播给所有等待者；切勿把 `m_LoadingTasks` 中存储的完成源直接替换为 async 方法的返回值（async 方法返回的 `UniTask` 只能 await 一次）。
   `AssetModule.InstantiateAsync` **不做模块级共享**——每个调用方各自 `LoadAssetAsync` 并持有自己的句柄，由 YooAsset 的 provider 去重保证不重复 IO；`m_InstantiateRefDict` 的引用计数是句柄释放权的**唯一**归属者。早期实现用共享句柄 + `m_InstantiateLoadingTasks` 去重，但共享一个句柄会让「谁有权释放」失去归属者：首个恢复的等待者会先释放，后恢复的等待者此时尚未登记，于是拿到已失效句柄而假失败，故已移除。
3. **失败句柄透传**：加载失败时（路径无效、类型不匹配等）包装方法返回失败的句柄而非抛异常（与 YooAsset `OperationAwaiter` "业务失败不视为异常" 契约一致），调用方须检查 `handle.Status == EOperationStatus.Succeeded` 后再取资源。
4. **取消与重启**：异步加载方法 **`CancellationToken` 参数必传**（调用方生命周期令牌），内部与模块自身 Token **linked 竞速**——调用方取消（如界面关闭）或模块销毁（`OnDispose`）任一触发即中止（释放句柄 + 卸载资源，抛 `OperationCanceledException`）；`OnInit` 重建 `CancellationScope`（新 Token），重启后可正常使用。取消的底层语义边界（在途下载是否中止、bundle 何时卸载）详见 §3「取消语义边界」。
5. **`AutoUnloadBundleWhenUnused` 为 false**（项目默认）：句柄释放不会自动卸载 bundle，需配合 `UnloadAsset` 显式卸载。
6. **YooAssets 未初始化防御**：`YooAssets.Destroy()` 后调用卸载方法（`UnloadAsset`）及查询方法（`GetAssetInfo`/`HasAssetPath`）时**不抛异常**（返回默认值/直接返回）。
7. **`AssetLoadRegister` 废弃/卸载防护**：`Dispose()`/`UnloadAll()` 后在途加载任务完成时检测到 `m_Disposed`/`m_Unloaded`，会释放句柄并抛 `ObjectDisposedException`，不再写回缓存（防止句柄无人释放，或 ref→0 后资源被重新缓存）。
8. **不要假设裸 `await handle` 是安全/免异常的**：`await handle`（不写 `ToUniTask`）会绑定到 YooAsset 的**实例方法** `HandleBase.GetAwaiter()`，而它在句柄无效时会先经 `CheckValidWithWarning()` 判定失败并 **抛 `InvalidOperationException`**，并非"安全完成"。
   `HandleBaseExtensions` 上游版本里的 `GetAwaiter` 扩展曾试图在 `!handle.IsValid` 时返回 `CompletedTask`，但它被实例方法遮蔽（C# 中实例方法优先于扩展方法），**从来没有生效过**，故已移除。
   需要取消能力、或需要"句柄无效时安全完成"时，一律显式调用 `handle.ToUniTask(cancellationToken: token, cancelImmediately: true)`。

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
