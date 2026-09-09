# 迁移引用池 / 对象池 / 任务池到 Core 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 把 ReferencePool(剥白为静态基座)、ObjectPool(保留模块)、TaskPool(普通类) 三个池迁入 Core 命名空间与目录，行为零变化。

**Architecture:** 同一 `HotFix.asmdef` 内的纯目录+namespace 归置，外加 ReferencePool 从 `ModuleBase` 剥离为纯静态类。依赖方向归一为「Module 使用底层基座」。ObjectPool 需保留模块以维系其每帧自动销毁驱动；ReferencePool 关闭清理改挂 `HotfixLauncher.DisposeModules`。

**Tech Stack:** Unity / HybridCLR（热更代码位于 `Assets/Scripts/Hotfix`）；命名空间扁平化约定 `Hotfix.Framework.Core`；编译验证走 unity-cli（`unity-development-loop` / 编译并测试）。

**Spec:** `Docs/superpowers/specs/2026-09-09-migrate-pools-to-core-design.md`

## Global Constraints

- 全程中文交流。
- **行为零变化**：不改任何池的引用计数/过期回收/驱逐算法。
- 铁律：不用原生 `Task`（用 `UniTask`）、不用协程、不用 LINQ、运行时不用反射。
- 同一程序集（`HotFix.asmdef`），所有 namespace 断链由编译器兜底。
- 移动文件时 `.meta` 必须伴随移动，保持 GUID 不变；改名文件时 `.meta` 同步改名。
- 无全限定引用，全部走 `using` 指令（已核实）；全名为：`Hotfix.Framework.ReferencePool.` / `.ObjectPool.` / `.TaskPool.` 出现 0 次。
- git 提交需用户同意，格式 `[AI]<type>: <中文描述>`。

---

## 文件结构（迁移后）

```
Assets/Scripts/Hotfix/Framework/Core/
  GlobalModule.cs           改：删 ReferencePool 字段/属性/using
  ModuleBase.cs / ModuleManager.cs / GameDriven.cs      (不动)
  ReferencePool/             新：参考池（静态基座）
    IReference.cs / ReferenceCollection.cs / ReferencePoolInfo.cs   namespace→Core
    ReferencePool.cs         由 ReferencePoolModule.cs 改名+静态化
    ReferencePool.API.cs     由 ReferencePoolModule.API.cs 改名+静态化
  ObjectPool/                新：对象池（保留为模块）
    Base/ObjectBase.cs, ObjectPoolBase.cs
    Misc/DisposeObjectFilterCallback.cs, ObjectInfo.cs
    Pool/ObjectPool.cs, ObjectPool.Dispose.cs, ObjectPool.Manage.cs
    ObjectPoolModule.cs, ObjectPoolModule.API.cs       namespace→Core, 仍 : ModuleBase
  TaskPool/                  新：任务池（普通类）
    ITaskAgent.cs, StartTaskStatus.cs, TaskBase.cs, TaskInfo.cs, TaskPool.cs, TaskStatus.cs  namespace→Core
Hotfix/HotfixLauncher.cs      改：删 RegisterModule<ReferencePoolModule>()；DisposeModules 组合 ReferencePool.ClearAll()
```

**依赖方向（迁移后）**：`ObjectPool(Module)` → `ReferencePool(静态基座)`；`ReferencePool`/`TaskPool` 仅依赖 Core 基础类型；`GlobalModule` 不再暴露基座。

---

## Task 1: TaskPool 迁入 Core（独立、先行热身）

**Files:**
- Move: `Assets/Scripts/Hotfix/Framework/TaskPool/*` → `Assets/Scripts/Hotfix/Framework/Core/TaskPool/`（含全部 `.meta`）
- Modify: Room 内每个 `.cs` 的 namespace
- Modify: 全仓库引用 TaskPool 的 `using`（约 9 文件）

**Interfaces:**
- Consumes: 无（独立于其它池）。
- Produces: `Hotfix.Framework.Core.TaskPool`、`TaskBase`、`TaskInfo`、`ITaskAgent`、`TaskStatus`、`StartTaskStatus`（供后续任务/运行时使用，签名不变）。

- [ ] **Step 1: 搬目录（含 .meta，保留 GUID）**

```bash
cd D:/_WorkSpace/Unity/FuFramework2.0/Unity/Assets/Scripts/Hotfix/Framework
git mv TaskPool Core/TaskPool
```

- [ ] **Step 2: 改 namespace 为扁平 Core**

```bash
cd Core/TaskPool
grep -rl 'namespace Hotfix.Framework.TaskPool' . | xargs sed -i 's/namespace Hotfix.Framework.TaskPool/namespace Hotfix.Framework.Core/'
# 若类文件前的 // ReSharper disable once CheckNamespace 保留即可
```

- [ ] **Step 3: 删除全仓库多余 `using Hotfix.Framework.TaskPool;`**

```bash
cd D:/_WorkSpace/Unity/FuFramework2.0/Unity/Assets/Scripts
grep -rl 'using Hotfix.Framework.TaskPool;' . --include=*.cs | xargs sed -i '/^using Hotfix.Framework.TaskPool;$/d'
```

> 说明：消费方（如 `DownloadTask`）引用 TaskPool 类型，现需经由其已有的 `using Hotfix.Framework.Core;` 可见；个别缺 Core using 的文件会在 Step 4 编译时报错，补上即可。

- [ ] **Step 4: 编译验证，清空全部错误**

Run: `unity-cli` → `unity-development-loop`（编译并测试）；或 `unity-cli editor` 触发编译。
Expected: 无编译错误。若有缺失类型/命名空间，逐个补 `using Hotfix.Framework.Core;`，直到清零。

- [ ] **Step 5: 冒烟验证启停**

在 Editor 中进入 Play 一次、退出 Play，确认无引用崩溃；`git status` 确认 TaskPool 旧目录消失、Core/TaskPool 就位。

- [ ] **Step 6: 提交**

```bash
cd D:/_WorkSpace/Unity/FuFramework2.0
git add Unity/Assets/Scripts/Hotfix/Framework
git commit -m "[AI]refactor: TaskPool 迁入 Core 并扁平化命名空间"
```

---

## Task 2: ObjectPool 迁入 Core（保留为模块）

**Files:**
- Move: `Assets/Scripts/Hotfix/Framework/ObjectPool/*` → `Assets/Scripts/Hotfix/Framework/Core/ObjectPool/`（含 `.meta`）
- Modify: 每个 `.cs` 的 namespace → `Hotfix.Framework.Core`
- Modify: 全仓库引用 ObjectPool 的 `using`（约 16 文件）

**Interfaces:**
- Consumes: `Hotfix.Framework.Core.ModuleBase`（保持继承）。
- Produces: `Hotfix.Framework.Core.ObjectPoolModule`（仍 `: ModuleBase`，仍注册）、`ObjectPool<T>`、`ObjectBase`、`ObjectPoolBase`、`ObjectInfo`、`DisposeObjectFilterCallback`。**此时仍引用 `GlobalModule.ReferencePoolModule.Recycle`（ReferencePool 尚未剥白，正常）。**

- [ ] **Step 1: 搬目录（含 .meta）**

```bash
cd D:/_WorkSpace/Unity/FuFramework2.0/Unity/Assets/Scripts/Hotfix/Framework
git mv ObjectPool Core/ObjectPool
```

- [ ] **Step 2: 改 namespace 为扁平 Core**

```bash
cd Core/ObjectPool
grep -rl 'namespace Hotfix.Framework.ObjectPool' . | xargs sed -i 's/namespace Hotfix.Framework.ObjectPool/namespace Hotfix.Framework.Core/'
```

- [ ] **Step 3: 删除全仓库多余 `using Hotfix.Framework.ObjectPool;`**

```bash
cd D:/_WorkSpace/Unity/FuFramework2.0/Unity/Assets/Scripts
grep -rl 'using Hotfix.Framework.ObjectPool;' . --include=*.cs | xargs sed -i '/^using Hotfix.Framework.ObjectPool;$/d'
```

> `GlobalModule.cs` 中的 `using Hotfix.Framework.ObjectPool;` 也会被删（其自身就在 Core 命名空间，ObjectPoolModule 同命名空间可直接见）。**不要**动 `GlobalModule.ObjectPoolModule` 属性与字段。

- [ ] **Step 4: 编译验证，清空全部错误**

Run: `unity-cli` 编译并测试（`unity-development-loop`）。
Expected: 无编译错误。个别缺 Core using 的文件补 `using Hotfix.Framework.Core;`。

- [ ] **Step 5: 冒烟验证对象池仍被每帧驱动**

进入 Play，确认 `ObjectPoolModule.OnUpdate` 仍被 `ModuleManager.Update` 调用（可在一次性断点或日志确认自动销毁路径存在）。退出 Play 无异常。

- [ ] **Step 6: 提交**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework
git commit -m "[AI]refactor: ObjectPool 迁入 Core 并扁平化命名空间(保留模块)"
```

---

## Task 3: ReferencePool 剥离模块并迁入 Core（核心，波及全部调用点）

**Files:**
- Move: `Assets/Scripts/Hotfix/Framework/ReferencePool/*` → `Assets/Scripts/Hotfix/Framework/Core/ReferencePool/`
- Rename: `ReferencePoolModule.cs`→`ReferencePool.cs`；`ReferencePoolModule.API.cs`→`ReferencePool.API.cs`（`.meta` 同步改名）
- Modify: `Core/ReferencePool/*.cs`（静态化 class、namespace→Core）
- Modify: `Assets/Scripts/Hotfix/Framework/Core/GlobalModule.cs`（删字段/属性/using）
- Modify: `Assets/Scripts/Hotfix/HotfixLauncher.cs`（删注册；DisposeModules 组合 ClearAll）
- Modify: 全仓库 `GlobalModule.ReferencePoolModule.` → `ReferencePool.`（约 105 文件，含 ObjectPool.cs:230 / ObjectPool.Dispose.cs:206）
- Modify: 全仓库删除 `using Hotfix.Framework.ReferencePool;`（约 29 文件）

**Interfaces:**
- Consumes: 无（纯静态）。
- Produces: `Hotfix.Framework.Core.ReferencePool`（静态类）：`Acquire<T>()`、`Recycle(IReference)`、`Add<T>(int)`、`RemoveUnused<T>(int)`、`RemoveAllUnused<T>()`、`ClearAll()`、`Count`、`GetAllReferencePoolInfos()`；`IReference`、`ReferenceCollection`、`ReferencePoolInfo` 同 namespace→Core。

- [ ] **Step 1: 搬目录 + 改名文件（含 .meta）**

```bash
cd D:/_WorkSpace/Unity/FuFramework2.0/Unity/Assets/Scripts/Hotfix/Framework
git mv ReferencePool Core/ReferencePool
cd Core/ReferencePool
git mv ReferencePoolModule.cs     ReferencePool.cs
git mv ReferencePoolModule.cs.meta ReferencePool.cs.meta
git mv ReferencePoolModule.API.cs     ReferencePool.API.cs
git mv ReferencePoolModule.API.cs.meta ReferencePool.API.cs.meta
```

- [ ] **Step 2: 静态化 class（改 `ReferencePool.cs` = 原 Module 文件）**

将 `public sealed partial class ReferencePoolModule : ModuleBase` 改为 `public static partial class ReferencePool`；字段去实例化 → `private static readonly Dictionary<Type, ReferenceCollection> m_ReferenceCollectionDict = new();`；`GetReferenceCollection(Type)` 改 `static`；删除 `OnDispose()` 重写（不再继承 ModuleBase），改为**只允许此文件定义 `ClearAll()`**：

```csharp
// ReferencePool.cs（关键摘录）
using System;
using System.Collections.Generic;
// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Core
{
    public static partial class ReferencePool
    {
        private static readonly Dictionary<Type, ReferenceCollection> m_ReferenceCollectionDict = new();

        public static void ClearAll()
        {
            lock (m_ReferenceCollectionDict)
            {
                foreach (var (_, refCollection) in m_ReferenceCollectionDict)
                {
                    refCollection.RemoveAll();
                }
                m_ReferenceCollectionDict.Clear();
            }
        }

        private static ReferenceCollection GetReferenceCollection(Type refType) { /* 原实现不变，static */ }
    }
}
```

> 原 `OnDispose(){ RemoveAllPools(); }` 被此 `ClearAll()` 取代。**API 分部中原 `RemoveAllPools()` 必须删除**（见 Step 3），否则与本文件 `ClearAll()` 重复定义同名方法报编译错。

- [ ] **Step 3: 静态化 API 分部（改 `ReferencePool.API.cs`）**

`namespace` → `Hotfix.Framework.Core`；`public sealed partial class ReferencePoolModule` → `public static partial class ReferencePool`；全部实例方法加 `static`。**删除本文件中的 `RemoveAllPools()`**（其逻辑已并入 Step 2 的 `ClearAll()`，分部类不得重复定义同名方法）。`Count` 属性改 `static`。方法签名保持（仅加 static）：`Acquire<T>() / Recycle(IReference) / Add<T>(int) / RemoveUnused<T>(int) / RemoveAllUnused<T>() / GetAllReferencePoolInfos()`。

> **明确归属**：`ClearAll()` 只在 `ReferencePool.cs`（Step 2）定义一份；本 API 分部**不定义** `ClearAll()`，仅删除原 `RemoveAllPools()`。

- [ ] **Step 4: 改 `GlobalModule.cs`——删 ReferencePool 存取器**

```csharp
// 删除以下三处：
private static ReferencePoolModule m_ReferencePoolModule;          // 字段
public static ReferencePoolModule ReferencePoolModule => m_ReferencePoolModule ??= ModuleManager.GetModule<ReferencePoolModule>();  // 属性
using Hotfix.Framework.ReferencePool;                              // using（第10行）
```

保留 `using Hotfix.Framework.ObjectPool;`？——不，ObjectPool 已迁 Core 命名空间，`GlobalModule` 本就在 Core 命名空间，该 using 也冗余，一并删除（Step Task2 已删，可复查）。`GlobalModule.ObjectPoolModule` 属性/字段保留。

- [ ] **Step 5: 改 `HotfixLauncher.cs`——删注册 + 组合 ClearAll**

- 删除 `ModuleManager.RegisterModule<ReferencePoolModule>();`（约 116 行）。
- 将 `GameDriven.Instance.DisposeModules = ModuleManager.Dispose;`（约 107 行）改为：

```csharp
GameDriven.Instance.DisposeModules = () =>
{
    ModuleManager.Dispose();
    ReferencePool.ClearAll();
};
```

- [ ] **Step 6: 全仓库替换 `GlobalModule.ReferencePoolModule.` → `ReferencePool.`**

```bash
cd D:/_WorkSpace/Unity/FuFramework2.0/Unity/Assets/Scripts
grep -rl 'GlobalModule.ReferencePoolModule' . --include=*.cs | xargs sed -i 's/GlobalModule.ReferencePoolModule\./ReferencePool./g'
```

> 覆盖 ObjectPool.cs:230 与 ObjectPool.Dispose.cs:206 的 `Recycle(obj)` 调用（`GlobalModule.ReferencePoolModule.Recycle(obj)` → `ReferencePool.Recycle(obj)`）。

- [ ] **Step 7: 删除全仓库多余 `using Hotfix.Framework.ReferencePool;`**

```bash
grep -rl 'using Hotfix.Framework.ReferencePool;' . --include=*.cs | xargs sed -i '/^using Hotfix.Framework.ReferencePool;$/d'
```

> 这些文件的引用池类型现在都在 Core 命名空间，经其既有的 `using Hotfix.Framework.Core;` 可见。

- [ ] **Step 8: 编译验证，清空全部错误**

Run: `unity-cli` 编译并测试（`unity-development-loop`）。
Expected: 无编译错误。逐个修复 `ReferencePoolModule`/裸引用残留、缺 Core using 的文件，直到清零。

确认无残留：

```bash
grep -rn 'Hotfix.Framework.ReferencePool\|GlobalModule.ReferencePoolModule\|ReferencePoolModule' . --include=*.cs | grep -v '//' | head
# 期望：无输出
```

- [ ] **Step 9: 冒烟验证（行为不变 + 重启不残留）**

1. 进入 Play：走一次 `ReferencePool.Acquire<T>()`+`Recycle`（任一使用方如 Entity/Event 触发），确认正常。
2. 触发 `RestartGameAsync`（`GameDriven.RestartGame`）一次，确认 `DisposeModules` 组合中被调用 `ReferencePool.ClearAll()`，无静态残留报错；对象池自动销毁仍工作。

- [ ] **Step 10: 提交**

```bash
cd D:/_WorkSpace/Unity/FuFramework2.0
git add Unity/Assets/Scripts/Hotfix
git commit -m "[AI]refactor: ReferencePool 剥离为静态基座并迁入 Core"
```

---

## 自审记录

**Scope coverage:** Spec §5.1→Task3，§5.2→Task2，§5.3→Task1；§6(调用点汇总)→Task3 Step6-7；§7(依赖方向)→迁移后自检；§8(风险)→各 Task 验证步；§9(验证)→各 Task 编译+冒烟。全覆盖。

**No placeholders:** 所有改动均给出具体命令/代码。**Order note:** Task2 在 Task3 前执行——ObjectPool 在 Task2 时仍调用 `GlobalModule.ReferencePoolModule.Recycle`（合法），其 3 处调用点在 Task3 Step6 才被替换，故 ObjectPool.cs/Dispose.cs 会被 Task2(ns) 与 Task3(调用点) 各改一次，属预期。

**Type consistency:** `ReferencePool.ClearAll()` 在 Step2/3 定义（保留一份）、Step5 调用；`ReferencePool.Recycle/Acquire/...` 静态化后所有调用点一致为 `ReferencePool.<M>()`；`ReferencePoolModule` 类型在 Task3 后不再存在（Step8 校验无残留）。
