# ICancelAsync 可取消异步对象设计

> 日期：2026-08-27
> 分支：`refactor/framework-modules-to-hotfix`
> 范围：`Unity/Assets/Scripts/Hotfix/Framework/Core/`（新增 `ICancelAsync`/`CancellationScope`）+ `Unity/Assets/Scripts/Hotfix/Framework/Asset/`（AssetModule/AssetLoadRegister 接入）+ `GameDriven`/`ModuleManager`（ReInit 接线）

## 1. 背景与问题

热更重载流程 `GameDriven.RestartGame()` 执行「`DisposeModules`（各模块 `OnDispose`）→ `ReInitModules`（各模块 `OnInit`）→ 重跑 AOT 启动」时，**复用了 `ModuleManager.ModuleList` 里跨重启存活的长生命周期模块实例**，而 `ModuleBase.OnDispose()` 是同步 void 方法，**框架无法 await 在途异步任务**。

由此产生一条必然的连锁：

1. **在途 `UniTask` 无法被取消**。热更重载时，正在 `await LoadAssetAsync(...)` 的任务没法被框架杀掉，它带着旧生命周期的捕获状态继续跑，结束后仍会写回模块字段。
2. **每个模块只能手写 `m_LifecycleEpoch` / `m_IsDisposed` 事后自查**「我是不是旧生命周期的僵尸」。这是对「取消」的手动近似，必须在**每一个写回点**布防——Asset/Entity/Scene/UIModule.Blur 各自实现，且评审多次发现**防线漏放**：
   - `ReleaseInstantiate` 无 epoch 校验，旧实例释放新生命周期引用；
   - 内部失败回滚无代际守卫，误卸载新生命周期资源；
   - 跨生命周期中止路径 `Release()` 后漏 `UnloadAsset`，bundle 泄漏；
   - 旧在途任务 finally 无条件移除去重项，误删新生命周期条目。

根因是**单一设计决策**：模块实例跨热更复用、无结构化机制终止在途异步任务、无结构化机制保证状态被完整重置。epoch 计数器是对该缺口的逐个补丁，天然易漏。

## 2. 目标

1. **提供 `ICancelAsync` 接口**：实现者的所有异步操作随对象销毁而取消，且可被 `await` 等待清理完成。
2. **提供 `CancellationScope` 簿记助手**：内部 CTS + 在途计数 + 「全部完成」TCS，供模块/装载器组合复用。
3. **AssetModule / AssetLoadRegister 接入**：异步操作观察自身 Token，取消路径统一「Release + UnloadAsset + 抛 OperationCanceledException」，从机制上消灭 bundle 泄漏与跨生命周期写回。
4. **框架接线**：`RestartGame` 的 Dispose 与 ReInit 之间插入「等待所有 ICancelAsync 模块排水完毕」，使 ReInit 前旧生命周期零在途残留——epoch 逐点校验可退役。

### 2.1 已确认的关键决策

| 决策点 | 结论 | 理由 |
|---|---|---|
| 接口形态 | `ICancelAsync { CancellationToken Token { get; } UniTask CancelAsync(); }` | Token 供在途操作观察取消；CancelAsync 主动取消并等待排水（可重入、幂等） |
| 复用方式 | `CancellationScope` **组合**（非基类） | `AssetModule : ModuleBase` 已占继承位，组合让纯类（AssetLoadRegister）也能复用 |
| 在途计数 | 引用计数 + 「全部完成」TCS（`Begin/End` 对称增删，`finally` 归零） | 无轮询、可重入、确定性完成；`finally` 强制取消路径也必须走到句柄清理 |
| Token 接入异步方法 | **内部自动接入，不改异步 API 签名** | 「随对象销毁而取消」零消费方改动；异步方法内部与自身 Token 竞速 |
| 取消竞速机制 | **YooAsset 官方 UniTask 集成** `handle.ToUniTask(cancellationToken, cancelImmediately: true)` | 拷贝 Samples~ 两个扩展文件入 HotFix 程序集 + 定义宏 `YOOASSET_UNITASK_SUPPORT`；TaskPool 复用零分配、取消立即完成、覆盖全部句柄类型（已核实当前 UniTask 版本 API 全 public，无需改 InternalsVisibleTo） |
| 取消语义 | `OperationCanceledException` + 取消路径 `Release()` + `UnloadAsset()` | 与 YooAsset「业务失败不抛」契约区分；从机制上消灭 `AutoUnloadBundleWhenUnused=false` 下的 bundle 泄漏 |
| `AssetLoadRegister.UnloadAll` | **不取消 Token**，保留 `m_Unloaded` 标记 | UnloadAll 是临时卸载（装载器可复用），取消 Token 会破坏复用；仅 `Dispose` 永久废弃触发取消 |
| 与 `m_IsDisposed`/epoch 关系 | **机制上替换，实施分两步**：第一版 Token 与现有守卫并存（epoch 作安全带），稳定后再逐步剥离 | 避免一次性大改风险 |
| `RestartGame` | 改为异步流（`RestartGameAsync`），调用方 `.Forget()` | 与代码库 fire-and-forget 惯例一致；是「先排水再 ReInit」的必要条件 |
| `CancelAsyncExtensions`（Break 糖） | **不做** | YAGNI：接口上已有 `CancelAsync()`，无真实调用方；fire-and-forget `Break()` 会诱导跳过 await，削弱「等待排水」保证 |
| 生命周期 Token 重建 | 每次 `OnInit` 重建 `CancellationScope`（新生命周期 = 新 Token） | 等价于 epoch：旧 Token 已取消 = 旧生命周期 |

## 3. 目标架构

```
Framework/Core/
├── ICancelAsync.cs          # [新增] 接口
├── CancellationScope.cs     # [新增] 取消范围簿记助手
└── ModuleManager.cs         # [修改] 新增 DrainCancelledAsync()

Framework/Asset/
├── AssetModule.cs           # [修改] 持有 CancellationScope；OnInit 重建；OnDispose Cancel
├── AssetModule.API.cs       # [修改] 异步方法 Begin/End + ToUniTask(token) 取消竞速 + 取消路径清理
└── AssetLoadRegister.cs     # [修改] 持有 CancellationScope；Dispose Cancel；UnloadAll 保留 m_Unloaded

Framework/Core/GameDriven.cs  # [修改] RestartGame → RestartGameAsync（Dispose → 排水 → ReInit → 重跑启动）

Hotfix/External/YooAsset.UniTask/  # [新增] 拷贝自 YooAsset Samples~ 的扩展文件（HandleBaseExtensions.cs + AsyncOperationBaseExtensions.cs），须定义宏 YOOASSET_UNITASK_SUPPORT 后编译生效
```

### 3.1 接口与簿记助手

```csharp
/// <summary>
/// 可取消异步对象：实现者的所有异步操作随对象销毁而取消，且可被 await 等待清理完成。
/// </summary>
public interface ICancelAsync
{
    /// <summary>
    /// 取消令牌。对象销毁（OnDispose/Dispose）时触发，在途操作观察它并中止。
    /// </summary>
    CancellationToken Token { get; }

    /// <summary>
    /// 触发取消并等待所有在途操作完成清理（释放句柄 + 卸载资源）后才返回。可重入、幂等。
    /// </summary>
    UniTask CancelAsync();
}

/// <summary>
/// 取消范围簿记：内部持有 CTS + 在途计数 + 「全部完成」TCS，供模块/装载器组合复用。
/// 每次生命周期重建（OnInit 新建），旧实例的 Token 已取消即标识旧生命周期。
/// </summary>
public sealed class CancellationScope
{
    private CancellationTokenSource m_Cts = new();
    private int m_InFlightCount;
    private UniTaskCompletionSource m_AllDoneTcs;

    public CancellationToken Token => m_Cts.Token;

    /// <summary>
    /// 同步触发取消（供 OnDispose/Dispose 等同步销毁钩子调用）；排水等待由 CancelAsync 负责。
    /// </summary>
    public void Cancel() => m_Cts.Cancel();

    public async UniTask CancelAsync()
    {
        m_Cts.Cancel();
        if (m_InFlightCount == 0) return; // 主线程模型，普通读即可
        m_AllDoneTcs ??= new UniTaskCompletionSource();
        await m_AllDoneTcs.Task;
    }

    /// <summary>
    /// 在途操作入口调用；返回的 BeginScope 必须 Dispose（用 using）以归零计数。
    /// 返回 struct 而非 IDisposable 接口：`using` 直接调用 Dispose，零装箱零分配（勿通过 IDisposable 接口使用，否则装箱）。
    /// </summary>
    public BeginScope Begin()
    {
        m_InFlightCount++; // 主线程模型（PlayerLoop 驱动），普通递增即可，无需原子操作
        return new BeginScope(this);
    }

    /// <summary>
    /// 在途操作作用域（struct 一次性释放器）。Dispose 时递减在途计数，归零时唤醒 CancelAsync 的等待。
    /// 共享同一 CancellationScope 引用，按值复制无堆分配。
    /// </summary>
    public readonly struct BeginScope : IDisposable
    {
        private readonly CancellationScope m_Owner;
        internal BeginScope(CancellationScope owner) { m_Owner = owner; }

        public void Dispose()
        {
            if (--m_Owner.m_InFlightCount == 0) // 主线程模型，普通递减即可
                m_Owner.m_AllDoneTcs?.TrySetResult();
        }
    }
}
```

### 3.2 取消路径（异步操作内部自动接入自身 Token）

```csharp
public async UniTask<AssetHandle> LoadAssetAsync(string path)
{
    m_Scope.Token.ThrowIfCancellationRequested();              // 入口：取消后拒绝新操作，防新在途使排水计数失效
    using (m_Scope.Begin())                                    // 登记在途
    {
        var package = GetReadyDefaultPackage();
        var handle  = package.LoadAssetAsync(path);
        try
        {
            // YooAsset 官方 UniTask 集成：cancelImmediately=true 时 Token 取消立即完成 await 并抛 OperationCanceledException
            await handle.ToUniTask(cancellationToken: m_Scope.Token, cancelImmediately: true);
            return handle;
        }
        catch (OperationCanceledException)
        {
            // 取消路径必须配对：Release 句柄 + UnloadAsset 卸载 bundle（AutoUnloadBundleWhenUnused=false 下仅 Release 不卸载）
            handle.Release();
            UnloadAsset(path);
            throw;
        }
    }                                                          // finally：计数归零，可能唤醒 CancelAsync 的等待
}
```

- `handle.ToUniTask(token, cancelImmediately: true)` 由 TaskPool 复用源（零分配），覆盖 Asset/Scene/SubAssets/Bundle/AllAssets 全部句柄类型；扩展文件已核实当前 UniTask 版本（ceac8d6946）下 API 全 public，无需 InternalsVisibleTo 改动。
- 在途计数归零放 `finally`，**强制**取消路径也必须走到句柄清理。
- 取消后 await 方收到 `OperationCanceledException`；对旧生命周期调用方，其生命周期已死，直接忽略。

## 4. 接入与使用

### 4.1 AssetModule

- 主文件持有 `private CancellationScope m_Scope = new();` 并转发 `Token`/`CancelAsync`（实现 `ICancelAsync`）。
- `OnInit`：`m_Scope = new CancellationScope();`（新生命周期 = 新 Token）。
- `OnDispose`：`m_Scope.Cancel();` + 现有同步清理（清 `m_InstantiateRefDict`/`m_InstantiateLoadingTasks`）。
- `AssetModule.API.cs`：`LoadAssetAsync`×3 / `LoadSceneAsync` / `LoadAsyncForInstantiate` 用 `Begin/End` 包裹，await 与自身 Token 竞速；取消路径按 §3.2。

### 4.2 AssetLoadRegister

- 持有 `private CancellationScope m_Scope = new();` 并转发。
- `LoadAssetHandleCoreAsync`（核心加载）用 `Begin/End` 包裹 + 与 Token 竞速。
- `Dispose`：`m_Scope.Cancel()`（永久废弃 → 取消在途）。
- `UnloadAll`：**不取消 Token**，保留 `m_Unloaded` 标记（临时卸载，装载器可复用）。

### 4.3 框架接线（根治 ReInit）

```csharp
// GameDriven.RestartGame() → RestartGameAsync()：
public async UniTask RestartGameAsync()
{
    DisposeModules?.Invoke();                    // OnDispose：同步清理 + 各自 Cancel()
    await ModuleManager.DrainCancelledAsync();   // ★ 等所有 ICancelAsync 模块在途任务排水完毕
    ReInitModules?.Invoke();                     // OnInit：重建各模块 Token
    await LaunchProcess.RunAsync();
}
```

```csharp
// ModuleManager 新增：
public static async UniTask DrainCancelledAsync()
{
    foreach (var module in ModuleList)
    {
        if (module is ICancelAsync cancellable)
            await cancellable.CancelAsync();
    }
}
```

调用方（设置界面重启）改 `RestartGameAsync().Forget()`。

### 4.4 三种角色使用方式

| 角色 | 用法 |
|---|---|
| **实现者** | 组合 `CancellationScope` 并转发 `Token`/`CancelAsync`；内部异步操作 `using (m_Scope.Begin()) { ... }`，await 与自身 Token 竞速 |
| **销毁者** | 框架 ReInit：`ModuleManager.DrainCancelledAsync()` 逐个 `await CancelAsync()`；对象自身 `OnDispose`/`Dispose` 内部 `m_Scope.Cancel()` |
| **业务调用方** | 零改动：照常 `await`，对象销毁即自动取消；可选 `catch (OperationCanceledException)`；持有 loader 的业务永久弃用时 `await loader.CancelAsync()` |

## 5. 与现有机制的关系（替换，分两步）

- **机制上替换**：`m_IsDisposed` → `Token.IsCancellationRequested`；`m_LifecycleEpoch` 整数 → 每次 `OnInit` 重建的 `CancellationScope`（旧 Token 已取消 = 旧生命周期）。`InstantiateResult` 携带的代际可改为携带 Token（或保留 epoch 作标记）。
- **实施上分两步**：
  - 第一版：Token 与现有守卫（`m_IsDisposed`/epoch）**并存**，Token 先跑通，epoch 作安全带；「取消后拒绝新操作」仍由现有 `m_Disposed` 守卫承担（抛 `ObjectDisposedException`），Token 入口检查暂不启用或作冗余——业务行为与现状一致，零回归。
  - 验证稳定后：逐步剥除 Asset/Entity/Scene/UIModule.Blur 各自的 epoch/`m_IsDisposed` 逐点校验；届时「对象销毁后发起新操作」改由 Token 入口检查拦截，异常类型 `ObjectDisposedException` → `OperationCanceledException`（语义更正确，代码库 catch 均通用捕获，无专门分支依赖）。

### 5.1 业务逻辑影响说明

Token **只在对象销毁时取消**（`OnDispose`/`Dispose`），对象存活期间入口检查直接通过，正常业务零影响。入口检查仅拦截「已销毁对象上的新操作」——该场景本就是调用方 bug，现有代码已抛 `ObjectDisposedException` 响亮失败，第一版行为不变；第二版换为 `OperationCanceledException`，同样响亮失败，`AssetLoadRegister`/`UnloadAll`（临时卸载）不取消 Token，装载器可复用，重载后 `OnInit` 重建 `CancellationScope`（新 Token），业务无需感知取消。

## 6. 范围与阶段

- **第一阶段（本次实现）**：`ICancelAsync` + `CancellationScope` + AssetModule/AssetLoadRegister 接入 + `RestartGameAsync`/`DrainCancelledAsync` 接线 + 同步 README。
- **后续阶段**：Entity/Scene/Sound/UIModule 等其他消费模块迁移；再逐步剥除各自 epoch/`m_IsDisposed`。

## 7. 验证方式

1. 拷贝扩展文件 + 定义宏 `YOOASSET_UNITASK_SUPPORT` 后**重启 Unity**（宏变更需重启生效），再触发编译，无错误。
2. 编辑器 Play 冒烟：框架启动、资源加载/实例化/场景加载路径可用。
3. 热更重载冒烟：在途 `InstantiateAsync`/实体加载/Shader 加载被中止时，句柄释放 + bundle 卸载（`UnloadAsset` 配对）；重载后新生命周期加载正常。
4. `await loader.CancelAsync()` 排水后，确认在途任务计数归零、无 bundle 残留。

## 8. 提交拆分（遵循 `Docs/Git提交规范.md`）

- **Commit 1**：`[AI]feat: 启用 YooAsset-UniTask 集成（拷扩展文件 + 宏 YOOASSET_UNITASK_SUPPORT）`（独立可编译，先落）。
- **Commit 2**：`[AI]feat: 新增 ICancelAsync/CancellationScope，AssetModule/AssetLoadRegister 接入，RestartGame 改异步排水`（框架能力 + 模块接入，依赖 Commit 1 编译）。
- **Commit 3**：`[AI]docs: 同步 Asset README 与框架 Core README（ICancelAsync 用法）`。
