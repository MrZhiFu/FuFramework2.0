# ICancelAsync 可取消异步对象 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 提供 `ICancelAsync` 接口（`Token` + `CancelAsync`），实现者的所有异步操作随对象销毁而取消且可被 await 等待排水；AssetModule/AssetLoadRegister 接入，重启 ReInit 前等待排水，根除跨生命周期写回与 bundle 泄漏。

**Architecture:** 以 `CancellationScope`（内部 CTS + 在途计数 + 「全部完成」TCS）组合实现 `ICancelAsync`；异步方法用 `Begin/End` 登记在途、经 YooAsset 官方 UniTask 集成 `ToUniTask(token, cancelImmediately)` 与自身 Token 竞速，取消路径统一「Release + UnloadAsset + 抛 OperationCanceledException」。框架 `RestartGameAsync` 在 Dispose 与 ReInit 之间插入 `ModuleManager.DrainCancelledAsync()` 等待排水。

**Tech Stack:** C# / Unity 2022.3 / YooAsset 3.0.5 / UniTask（git 包，`com.cysharp.unitask`）

**Spec:** `Docs/superpowers/specs/2026-08-27-icancelasync-design.md`

## Global Constraints

- **代码风格**（`Docs/代码风格规范.md`）：中文 XML 文档注释；`m_` 前缀私有字段；K&R 大括号；单行语句也加大括号（极简卫语句除外）；局部变量 `var`；`async` 方法加 `Async` 后缀；fire-and-forget 加 `.Forget()`。
- **Git 规范**（`Docs/Git提交规范.md`）：Conventional Commits 中文；AI 提交加 `[AI]` 前缀；任何 git 操作前须征得用户同意。
- **模块间解耦**：`ICancelAsync`/`CancellationScope` 放 `Hotfix.Framework.Core`，不依赖 Asset 模块。
- **不改异步 API 签名**：`LoadAssetAsync`/`LoadSceneAsync`/`InstantiateAsync`/`AssetLoadRegister.LoadAsync` 等签名不变，Token 内部自动接入。
- **工作区已有未提交的审查修复**（AssetModule Token 化 + 各模块中止路径补 UnloadAsset）：本计划在其之上实施，二者合并提交。
- **主线程模型**：本框架由 PlayerLoop 驱动，在途计数用普通 `++/--`，无需原子操作。

---

### Task 1: 启用 YooAsset-UniTask 集成（拷贝扩展文件，剥离守卫）

**Files:**
- Create: `Unity/Assets/Scripts/Hotfix/External/YooAsset.UniTask/HandleBaseExtensions.cs`
- Create: `Unity/Assets/Scripts/Hotfix/External/YooAsset.UniTask/AsyncOperationBaseExtensions.cs`
- Create: `Unity/Assets/Scripts/Hotfix/External/YooAsset.UniTask/HandleBaseExtensions.cs.meta`
- Create: `Unity/Assets/Scripts/Hotfix/External/YooAsset.UniTask/AsyncOperationBaseExtensions.cs.meta`

**Interfaces:**
- Consumes: 无。
- Produces: `Cysharp.Threading.Tasks.HandleBaseExtensions.ToUniTask(this HandleBase, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken cancellationToken = default, bool cancelImmediately = false)` 与 `.WithCancellation(this HandleBase, CancellationToken, bool)`；`AsyncOperationBaseExtensions` 同形。供 Task 4/5 的 `handle.ToUniTask(cancellationToken: token, cancelImmediately: true)` 使用。

> **工程决策（偏离官方宏方案）**：原文件被 `#if YOOASSET_UNITASK_SUPPORT` 守卫（宏未生效则编译为空），且项目 ProjectSettings 仅 Android 平台配了宏、逐平台手改 + 重启 Unity 操作重。本工程是刻意启用该集成（始终开启），故**剥离守卫**使文件无条件编译，省去宏与重启，让 unity-cli 编译立即可行。文件头加注释注明来源。

- [ ] **Step 1: 创建目录并拷贝文件**

拷贝自 `Unity/Library/PackageCache/com.tuyoogame.yooasset@3.0.5/Samples~/UniTask Sample/UniTask/Runtime/External/YooAsset/`：

```bash
mkdir -p "Unity/Assets/Scripts/Hotfix/External/YooAsset.UniTask"
SRC="Unity/Library/PackageCache/com.tuyoogame.yooasset@3.0.5/Samples~/UniTask Sample/UniTask/Runtime/External/YooAsset"
DST="Unity/Assets/Scripts/Hotfix/External/YooAsset.UniTask"
cp "$SRC/HandleBaseExtensions.cs" "$DST/HandleBaseExtensions.cs"
cp "$SRC/AsyncOperationBaseExtensions.cs" "$DST/AsyncOperationBaseExtensions.cs"
cp "$SRC/HandleBaseExtensions.cs.meta" "$DST/HandleBaseExtensions.cs.meta"
cp "$SRC/AsyncOperationBaseExtensions.cs.meta" "$DST/AsyncOperationBaseExtensions.cs.meta"
```

- [ ] **Step 2: 剥离 `#if YOOASSET_UNITASK_SUPPORT` 守卫并加来源注释**

对两个 `.cs` 文件：
1. 删除首行 `#if YOOASSET_UNITASK_SUPPORT` 与末行 `#endif`。
2. 在文件顶部（`using` 之前）加：
```csharp
// 来源：YooAsset 3.0.5 Samples~/UniTask Sample（已剥离 YOOASSET_UNITASK_SUPPORT 守卫，本工程刻意启用该集成）。
// 注意：包升级时若该集成有更新，需同步本文件。
```

- [ ] **Step 3: 检查 HotFix.asmdef 引用**

确认 `Unity/Assets/Scripts/Hotfix/HotFix.asmdef` 的 `references` 含 `com.tuyoogame.yooasset` 与 `com.cysharp.unitask`（Hotfix 代码已用二者，通常已含）。若 `External/` 子目录被独立 asmdef 排除，则将 `YooAsset.UniTask` 目录加入 HotFix 程序集范围（Unity 默认子目录并入最近父 asmdef，无需额外操作）。

- [ ] **Step 4: 编译验证（可合并到最终统一编译）**

Run: `unity-cli raw execute_menu_item --json '{"menuPath": "Assets/Refresh"}'`，轮询 `unity-cli raw get_compilation_state` 至 `isCompiling: false`，读 `unity-cli raw read_console` 确认无 `CS` 错误。
Expected: `errorCount: 0`；`await handle` 仍正常（YooAsset 实例 `GetAwaiter` 优先于扩展，无歧义）。

- [ ] **Step 5: Commit**

```bash
git add Unity/Assets/Scripts/Hotfix/External/YooAsset.UniTask/
git commit -m "[AI]feat: 启用 YooAsset-UniTask 集成（拷贝 ToUniTask 扩展，剥离守卫无条件编译）"
```

---

### Task 2: 新增 ICancelAsync 接口与 CancellationScope

**Files:**
- Create: `Unity/Assets/Scripts/Hotfix/Framework/Core/ICancelAsync.cs`
- Create: `Unity/Assets/Scripts/Hotfix/Framework/Core/CancellationScope.cs`

**Interfaces:**
- Consumes: 无。
- Produces:
  - `public interface ICancelAsync { CancellationToken Token { get; } UniTask CancelAsync(); }`
  - `public sealed class CancellationScope { public CancellationToken Token { get; } public void Cancel(); public UniTask CancelAsync(); public CancellationScope.BeginScope Begin(); public readonly struct BeginScope : IDisposable }`
  - 供 Task 3（ModuleManager 排水）、Task 4/5（模块接入）使用。

- [ ] **Step 1: 创建 `ICancelAsync.cs`**

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Core
{
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
}
```

- [ ] **Step 2: 创建 `CancellationScope.cs`**

```csharp
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Core
{
    /// <summary>
    /// 取消范围登记：内部持有 CTS + 在途计数 + 「全部完成」TCS，供模块/装载器组合复用。
    /// 每次生命周期重建（OnInit 新建），旧实例的 Token 已取消即标识旧生命周期。
    /// 主线程模型（PlayerLoop 驱动）下普通 ++/-- 即可，无需原子操作。
    /// </summary>
    public sealed class CancellationScope
    {
        private CancellationTokenSource m_Cts = new();
        private int m_InFlightCount;
        private UniTaskCompletionSource m_AllDoneTcs;

        /// <summary>
        /// 取消令牌。
        /// </summary>
        public CancellationToken Token => m_Cts.Token;

        /// <summary>
        /// 同步触发取消（供 OnDispose/Dispose 等同步销毁钩子调用）；排水等待由 CancelAsync 负责。
        /// </summary>
        public void Cancel() => m_Cts.Cancel();

        /// <summary>
        /// 触发取消并等待所有在途操作完成清理后才返回。可重入、幂等。
        /// </summary>
        public async UniTask CancelAsync()
        {
            m_Cts.Cancel();
            if (m_InFlightCount == 0) return; // 主线程模型，普通读即可
            m_AllDoneTcs ??= new UniTaskCompletionSource();
            await m_AllDoneTcs.Task;
        }

        /// <summary>
        /// 在途操作入口调用；返回的 BeginScope 必须 Dispose（用 using）以归零计数。
        /// 返回 struct 而非 IDisposable 接口：using 直接调用 Dispose，零装箱零分配（勿通过 IDisposable 接口使用，否则装箱）。
        /// </summary>
        public BeginScope Begin()
        {
            m_InFlightCount++; // 主线程模型，普通递增即可
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
}
```

- [ ] **Step 3: 编译验证**

Run: `unity-cli raw execute_menu_item --json '{"menuPath": "Assets/Refresh"}'` → 轮询至编译结束 → `read_console` 无 CS 错误。
Expected: `errorCount: 0`（新增文件无引用依赖，独立可编译）。

- [ ] **Step 4: Commit**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/Core/ICancelAsync.cs Unity/Assets/Scripts/Hotfix/Framework/Core/CancellationScope.cs
git commit -m "[AI]feat: 新增 ICancelAsync 接口与 CancellationScope 取消登记（struct BeginScope 零分配、普通计数主线程模型）"
```

---

### Task 3: ModuleManager.DrainCancelledAsync + GameDriven.RestartGameAsync

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Core/ModuleManager.cs`（新增方法；顶部加 `using Cysharp.Threading.Tasks;`）
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Core/GameDriven.cs`（`RestartGame` → 薄包装 + `RestartGameAsync`）

**Interfaces:**
- Consumes: `ICancelAsync`（Task 2）。
- Produces: `ModuleManager.DrainCancelledAsync()`；`GameDriven.RestartGameAsync()` 与保留的 `RestartGame()`。

- [ ] **Step 1: ModuleManager 新增 `DrainCancelledAsync()`**

在 `ModuleManager.cs` 的 `ReInit()` 方法之后追加：

```csharp
        /// <summary>
        /// 等待所有实现 ICancelAsync 的模块完成取消排水（在途任务全部清理完毕）。
        /// 供重启 RestartGame 在 Dispose 之后、ReInit 之前调用，保证旧生命周期零在途残留。
        /// </summary>
        public static async UniTask DrainCancelledAsync()
        {
            foreach (var module in ModuleList)
            {
                if (module is ICancelAsync cancellable)
                    await cancellable.CancelAsync();
            }
        }
```

并确认文件顶部 usings 含 `using Cysharp.Threading.Tasks;`（无则补）。

- [ ] **Step 2: GameDriven 的 RestartGame 改异步流程**

将 `GameDriven.cs` 中现有 `RestartGame()` 替换为：

```csharp
        /// <summary>
        /// 重启游戏（如设置界面重启）。兼容旧入口：转异步流程 fire-and-forget。
        /// 依次释放所有模块、等待 ICancelAsync 模块排水完毕、重新初始化模块、重新运行 AOT 启动流程。
        /// </summary>
        public void RestartGame() => RestartGameAsync().Forget();

        /// <summary>
        /// 重启游戏异步流程：Dispose（同步清理 + 各自 Cancel）→ 等待 ICancelAsync 排水 → ReInit → 重跑启动。
        /// 排水保证 ReInit 前旧生命周期在途任务已全部清理，杜绝旧任务写回新生命周期。
        /// </summary>
        public async UniTask RestartGameAsync()
        {
            DisposeModules?.Invoke();
            await ModuleManager.DrainCancelledAsync();
            ReInitModules?.Invoke();
            await LaunchProcess.RunAsync();
        }
```

> 保留 `RestartGame()` 同名薄包装，避免反射/外部调用方失效（代码库内无调用方，已核实）。

- [ ] **Step 3: 编译验证**

Run: `unity-cli raw execute_menu_item --json '{"menuPath": "Assets/Refresh"}'` → 轮询至编译结束 → `read_console` 无 CS 错误。
Expected: `errorCount: 0`。

- [ ] **Step 4: Commit**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/Core/ModuleManager.cs Unity/Assets/Scripts/Hotfix/Framework/Core/GameDriven.cs
git commit -m "[AI]feat: ModuleManager 新增 DrainCancelledAsync，GameDriven.RestartGame 改异步排水后 ReInit"
```

---

### Task 4: AssetModule 接入 ICancelAsync

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Asset/AssetModule.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Asset/AssetModule.API.cs`

**Interfaces:**
- Consumes: `CancellationScope`、`ICancelAsync`（Task 2）、`handle.ToUniTask(cancellationToken, cancelImmediately)`（Task 1）。
- Produces: `AssetModule : ModuleBase, ICancelAsync`（`Token`/`CancelAsync` 转发至 `m_Scope`）。

- [ ] **Step 1: AssetModule.cs 增加 scope 字段、实现接口、生命周期接入**

1. 类声明改为：`public partial class AssetModule : ModuleBase, ICancelAsync`。
2. 在 `m_IsDisposed` 字段附近新增：

```csharp
        /// <summary>
        /// 取消范围：内部 CTS + 在途计数 + 全部完成信号。每次 OnInit 重建（新生命周期 = 新 Token）。
        /// OnDispose 时 Cancel，所有在途异步操作随之取消；框架 ReInit 前经 DrainCancelledAsync 等待排水。
        /// </summary>
        private CancellationScope m_Scope = new();
```

3. 新增接口实现（放在字段区之后、`OnInit` 之前）：

```csharp
        /// <summary>
        /// 取消令牌：模块销毁（OnDispose）后触发，在途操作观察它并中止。
        /// </summary>
        public CancellationToken Token => m_Scope.Token;

        /// <summary>
        /// 触发取消并等待在途操作完成清理（释放句柄 + 卸载资源）后才返回。供框架重启排水。
        /// </summary>
        public UniTask CancelAsync() => m_Scope.CancelAsync();
```

4. `OnInit` 中 `m_IsDisposed = false;` 之后加：

```csharp
            // 新生命周期 = 新 Token：旧 Token 已被 OnDispose 取消，在途旧任务据此识别中止
            m_Scope = new CancellationScope();
```

5. `OnDispose` 中 `m_LifecycleEpoch++;` 之后加：

```csharp
            m_Scope.Cancel(); // 随模块销毁取消所有在途异步操作
```

6. 确认顶部 usings 含 `using System.Threading;`（`CancellationToken`）与 `using Cysharp.Threading.Tasks;`（`UniTask`）；`CancellationScope`/`ICancelAsync` 同命名空间 `Hotfix.Framework.Core`，无需新 using。

- [ ] **Step 2: AssetModule.API.cs 三个 LoadAssetAsync 重载接入取消**

对三个重载（`(string)`、`<T>(string)`、`(string, Type)`）执行同一变换——方法体改为：

```csharp
            m_Scope.Token.ThrowIfCancellationRequested(); // 入口：取消后拒绝新操作，防新在途使排水计数失效
            using (m_Scope.Begin()) // 登记在途：OnDispose 排水时等待本操作清理完毕
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
            }
```

注意：三个重载的 `package.LoadAssetAsync` 调用签名不同（`(path)` / `LoadAssetAsync<T>(path)` / `(path, type)`），保持各自原调用。方法 XML 注释末尾补一行：

```csharp
        /// 注意3：模块销毁（OnDispose）时本方法会抛 OperationCanceledException（句柄已自动释放并卸载），调用方可按需捕获。
```

- [ ] **Step 3: AssetModule.API.cs LoadSceneAsync 接入取消**

将 `LoadSceneAsync` 方法体改为：

```csharp
            m_Scope.Token.ThrowIfCancellationRequested();
            using (m_Scope.Begin())
            {
                var package = GetReadyDefaultPackage();
                var handle  = package.LoadSceneAsync(path, sceneMode, LocalPhysicsMode.None, true);
                try
                {
                    // 可选进度上报：每帧轮询 handle.Progress 直到加载完成；同时观察取消令牌
                    if (onProgress != null)
                    {
                        while (!handle.IsDone)
                        {
                            m_Scope.Token.ThrowIfCancellationRequested();
                            TryReportProgress(onProgress, handle.Progress);
                            await UniTask.Yield();
                        }
                        TryReportProgress(onProgress, handle.Progress);
                    }

                    await handle.ToUniTask(cancellationToken: m_Scope.Token, cancelImmediately: true);
                    if (handle.Status != EOperationStatus.Succeeded)
                    {
                        handle.Release();
                        throw new InvalidOperationException($"[AssetModule]场景加载失败：{path}");
                    }
                    return handle;
                }
                catch (OperationCanceledException)
                {
                    // 取消：释放场景句柄（场景走 SceneManager 生命周期，不调 UnloadAsset）
                    handle.Release();
                    throw;
                }
            }
```

- [ ] **Step 4: AssetModule.API.cs LoadAsyncForInstantiate 无需改动**

`LoadAsyncForInstantiate` 内部 `await LoadAssetAsync(path)` 已随 Step 2 带 Token 取消（取消抛 OCE 被其 `catch (Exception e)` 捕获 → `TrySetException` 传播给等待方）；其自身的 epoch/`m_IsDisposed` 守卫继续兜底。**不改动**，仅确认编译通过。

- [ ] **Step 5: 编译验证**

Run: `unity-cli raw execute_menu_item --json '{"menuPath": "Assets/Refresh"}'` → 轮询至编译结束 → `read_console` 无 CS 错误。
Expected: `errorCount: 0`。若出现 `ToUniTask` 找不到（Task 1 未生效）或 `GetAwaiter` 歧义，回到 Task 1 检查。

- [ ] **Step 6: Commit**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/Asset/AssetModule.cs Unity/Assets/Scripts/Hotfix/Framework/Asset/AssetModule.API.cs
git commit -m "[AI]feat: AssetModule 接入 ICancelAsync（LoadAssetAsync/LoadSceneAsync 随销毁取消，OnInit 重建 scope）"
```

---

### Task 5: AssetLoadRegister 接入 ICancelAsync

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Asset/AssetLoadRegister.cs`

**Interfaces:**
- Consumes: `CancellationScope`、`ICancelAsync`（Task 2）。
- Produces: `AssetLoadRegister : ICancelAsync`（`Token`/`CancelAsync` 转发至 `m_Scope`）。

- [ ] **Step 1: 类声明 + scope 字段 + 接口实现**

1. 类声明改为：`public class AssetLoadRegister : ICancelAsync`。
2. 在 `m_AssetModule` 字段附近新增：

```csharp
        /// <summary>
        /// 取消范围：Dispose 时 Cancel，在途加载随装载器销毁而取消；UnloadAll（临时卸载）不取消，装载器可复用。
        /// 业务可 await CancelAsync 等待在途加载清理完毕。
        /// </summary>
        private readonly CancellationScope m_Scope = new();
```

3. 新增接口实现（构造方法之前）：

```csharp
        /// <summary>
        /// 取消令牌：装载器永久废弃（Dispose）后触发，在途加载观察它并中止。
        /// </summary>
        public CancellationToken Token => m_Scope.Token;

        /// <summary>
        /// 触发取消并等待在途加载完成清理（释放句柄 + 卸载资源）后才返回。可重入、幂等。
        /// </summary>
        public UniTask CancelAsync() => m_Scope.CancelAsync();
```

4. 确认顶部 usings 含 `using System.Threading;`。

- [ ] **Step 2: Dispose 触发取消**

在 `Dispose()` 方法内 `m_Disposed = true;` 之前加：

```csharp
            m_Scope.Cancel(); // 永久废弃：取消在途加载
```

- [ ] **Step 3: LoadAssetHandleCoreAsync 用 Begin/End 登记在途**

将 `LoadAssetHandleCoreAsync` 方法体整体包进 `using (m_Scope.Begin())`（缩进一层），并在 `try` 之前：

```csharp
            using (m_Scope.Begin()) // 登记在途：Dispose 排水时等待本操作清理完毕
            {
                AssetHandle assetHandle = null;
                try
                {
                    assetHandle = assetType == null
                        ? await m_AssetModule.LoadAssetAsync(path)
                        : await m_AssetModule.LoadAssetAsync(path, assetType);
                    // ...（原 AssetObject 判空、m_Disposed||m_Unloaded 中止、缓存、日志，全部保持原样）...
                    return assetHandle;
                }
                catch
                {
                    assetHandle?.Release();
                    throw;
                }
            }
```

> 说明：装载器在途加载不把自身 Token 传入模块 API（避免改签名）；Dispose 后模块加载自然完成时由既有 `m_Disposed` 守卫清理（Release+Unload+抛），排水等待该清理完成。模块自身销毁（重启）时其 Token 取消 → 模块加载抛 OCE 传播到装载器，二者正确组合。

- [ ] **Step 4: 编译验证**

Run: `unity-cli raw execute_menu_item --json '{"menuPath": "Assets/Refresh"}'` → 轮询至编译结束 → `read_console` 无 CS 错误。
Expected: `errorCount: 0`。

- [ ] **Step 5: Commit**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/Asset/AssetLoadRegister.cs
git commit -m "[AI]feat: AssetLoadRegister 接入 ICancelAsync（Dispose 取消在途，Begin/End 登记排水）"
```

---

### Task 6: README 同步

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Asset/README.md`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Core/README.md`

**Interfaces:** 无（纯文档）。

- [ ] **Step 1: Asset README 补充 ICancelAsync**

在 Asset README 的「核心类说明」AssetModule 与 AssetLoadRegister 小节各补一段：

```markdown
> **可取消异步**：`AssetModule`/`AssetLoadRegister` 实现 `ICancelAsync`（`Token` + `CancelAsync`）。
> 模块/装载器销毁（`OnDispose`/`Dispose`）时触发 `Token` 取消，其所有在途异步操作随之取消并自动清理
> （`Release` + `UnloadAsset`，抛 `OperationCanceledException`）。框架重启 `RestartGame` 会在 ReInit 前
> `await` 各模块 `CancelAsync` 排水，保证旧生命周期无在途残留；业务弃用装载器时也可 `await loader.CancelAsync()` 等待清理。
> `UnloadAll`（临时卸载）不取消 Token，装载器可复用。
```

- [ ] **Step 2: Core README 补充 ICancelAsync**

在 `Unity/Assets/Scripts/Hotfix/Framework/Core/README.md` 的模块生命周期/核心机制章节补：

```markdown
### ICancelAsync 可取消异步对象

实现 `ICancelAsync`（`Token` + `CancelAsync`）的对象，其所有异步操作随对象销毁而取消，且可被 await 等待清理完成。
- `Token`：对象销毁（`OnDispose`/`Dispose`）时触发，在途操作观察它并中止。
- `CancelAsync()`：触发取消并等待所有在途操作完成清理后才返回（可重入、幂等）。
- 实现方式：组合 `CancellationScope`（内部 CTS + 在途计数 + 「全部完成」信号），异步操作入口 `Begin()`、`finally` 归零。
- 框架集成：重启 `RestartGameAsync` 在 Dispose 与 ReInit 之间 `await ModuleManager.DrainCancelledAsync()` 等待排水，根除旧任务写回新生命周期。
```

- [ ] **Step 3: Commit**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/Asset/README.md Unity/Assets/Scripts/Hotfix/Framework/Core/README.md
git commit -m "[AI]docs: 同步 Asset 与 Core README（ICancelAsync 用法与热更排水说明）"
```

---

### Task 7: 最终编译验证（unity-cli 直到编译无误）与提交归拢

**Files:** 无（验证 + 提交已实施改动）。

**Interfaces:** 无。

- [ ] **Step 1: 完整编译验证循环**

反复执行以下循环直到 `errorCount: 0`：

```bash
unity-cli raw execute_menu_item --json '{"menuPath": "Assets/Refresh"}'
# 轮询至 isCompiling=false：
#   unity-cli raw get_compilation_state
# 读取错误：
#   unity-cli raw read_console
```

若出现 CS 错误，定位并修复（重点排查：`ToUniTask` 找不到 / `GetAwaiter` 歧义 / `CancellationToken`/`ICancelAsync` using 缺失 / `BeginScope` 被当 `IDisposable` 接口用导致装箱警告）。修复后重复循环。
Expected: `errorCount: 0`，无编译错误。

- [ ] **Step 2: 提交归拢（若 Task 1-6 已逐条提交则跳过）**

将本计划涉及的全部改动（含此前两轮审查修复）按规格 §8 提交拆分归拢提交，提交前征求用户同意。

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/Asset/ Unity/Assets/Scripts/Hotfix/Framework/Core/
git commit -m "[AI]feat: 新增 ICancelAsync/CancellationScope，AssetModule/AssetLoadRegister 接入，RestartGame 改异步排水"
```

- [ ] **Step 3: 手动冒烟（Unity 编辑器，可选但推荐）**

1. 编辑器 Play，确认框架启动、资源加载/实例化/场景加载路径可用（既有功能无回归）。
2. 重启冒烟：在途 `InstantiateAsync`/实体加载/Shader 加载被中止时，句柄释放 + bundle 卸载（`UnloadAsset` 配对）；重载后新生命周期加载正常。
3. `await loader.CancelAsync()` 排水后，确认在途计数归零、无 bundle 残留。
