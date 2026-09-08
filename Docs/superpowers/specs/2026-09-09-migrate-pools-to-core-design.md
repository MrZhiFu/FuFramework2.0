# 迁移引用池 / 对象池 / 任务池到 Core 设计

日期：2026-09-09
分支：`refactor/framework-modules-to-hotfix`
范围：`Assets/Scripts/Hotfix/Framework/{ReferencePool, ObjectPool, TaskPool}` → 归入 `Core`

---

## 1. 背景与现状

架构上，`ReferencePool`（引用池）、`ObjectPool`（对象池）、`TaskPool`（任务池）是框架的**底层基础设施**，被众多游戏级模块（Entity/Download/Asset/Event 等）所使用。当前它们被当作一等「模块」挂在 `ModuleManager` 的生命周期体系里（`ModuleBase` 接入、`GlobalModule` 暴露、帧驱动）。

**程序集结构（关键事实）**：整个 `Assets/Scripts/Hotfix` 在同一个 `HotFix.asmdef` 内。因此本次「迁入 Core」**不涉及程序集边界**，不存在新增 IL 依赖环；它主要是**目录 + 命名空间**归置，外加对 ReferencePool 的**模块抽象剥离**。

**现状的依赖不对称**：

| 池 | 是否模块 | 生命周期依赖 | 现状命名空间 |
|---|---|---|---|
| `ReferencePool` | 是（`ReferencePoolModule : ModuleBase`） | 仅 `OnDispose` 清空 | `Hotfix.Framework.ReferencePool` |
| `ObjectPool` | 是（`ObjectPoolModule : ModuleBase`） | `OnInit` 订阅低内存 + `OnUpdate` 每帧驱动自动销毁 + `OnDispose` | `Hotfix.Framework.ObjectPool` |
| `TaskPool` | **否**（普通类） | 无 | `Hotfix.Framework.TaskPool` |

**已核实的引用与依赖**：
- `Core/GlobalModule.cs` 已反向 `using` `Hotfix.Framework.ObjectPool` / `Hotfix.Framework.ReferencePool`（存在命名空间层面循环）。
- `ObjectPool` **依赖** `ReferencePool`：`ObjectBase : IReference`（`ObjectPool/Base/ObjectBase.cs:13`）；`ObjectPool.cs:230` 与 `ObjectPool.Dispose.cs:206` 调用 `GlobalModule.ReferencePoolModule.Recycle(obj)`。方向正确：Module 使用底层基座。
- 全仓库命名空间引用面：`ReferencePool` 29 文件 / `ObjectPool` 16 文件 / `TaskPool` 9 文件；**全限定引用（`Hotfix.Framework.XxxPool.`）为 0**，全部走 `using` 指令。
- 模块注册：`Hotfix/HotfixLauncher.cs:116`（`RegisterModule<ReferencePoolModule>()`）、`:120`（`RegisterModule<ObjectPoolModule>()`）；`TaskPool` 不注册。
- `ReferencePoolModule` 方法面已完整盘点（见 §5）。

---

## 2. 目标

把三个池统一归入 `Core` 命名空间，并**彻底剥离 ReferencePool 的模块抽象**，使其成为纯静态基础设施供各模块直接调用；ObjectPool 因需每帧驱动自动销毁**保留为模块**；TaskPool 为普通类仅随迁。

一句话：**「被使用的基座」从模块体系里拿出来，真正被生命周期驱动系统的才留在模块体系里。**

---

## 3. 范围

**纳入**：
1. ReferencePool 剥离模块 → 纯静态类 `ReferencePool`，迁入 Core。
2. ObjectPool 保留模块，仅目录 + namespace 迁入 Core。
3. TaskPool 普通类，目录 + namespace 迁入 Core。
4. 全部关联的调用点、`using` 指令、注册入口、`GlobalModule` 访问器的同步调整。

**不纳入（YAGNI）**：
- 不把 TaskPool 包装成模块。
- 不引入 asmdef 拆分。
- 不改动任何池的**行为/算法**（引用计数、过期回收、驱逐策略等全部保持原样）。
- 不顺手清理其他模块的无用 `using`。

---

## 4. 目标结构与命名空间

Core 现状约定：内部子目录仅做目录组织，**全部文件使用扁平命名空间 `Hotfix.Framework.Core`**。三个池沿用此约定。

```
Core/
  ModuleBase.cs / ModuleManager.cs / GlobalModule.cs  (既有，不动)
  Async/ DataStruct/ Extension/ Property/ Serializer/ Singleton/ Utility/  (既有，不动)
  ReferencePool/           ← 整目录迁入；扁平 ns
    IReference.cs
    ReferenceCollection.cs
    ReferencePoolInfo.cs
    ReferencePool.cs       ← 由 ReferencePoolModule.cs 改名而来（静态化）
  ObjectPool/              ← 整目录迁入；扁平 ns
    Base/  Misc/  Pool/    ← 其内部子结构不变
    ObjectPoolModule.cs / ObjectPoolModule.API.cs    ← 保留为模块
  TaskPool/                ← 整目录迁入；扁平 ns
    ITaskAgent.cs / StartTaskStatus.cs / TaskBase.cs / TaskInfo.cs / TaskPool.cs / TaskStatus.cs
```

所有三个池的 `namespace` 声明：`Hotfix.Framework.ReferencePool|ObjectPool|TaskPool` → `Hotfix.Framework.Core`。

---

## 5. 逐模块处理

### 5.1 ReferencePool：彻底剥离模块

**改动**：
- `ReferencePoolModule.cs` → 改名 `ReferencePool.cs`，`sealed partial class ReferencePoolModule : ModuleBase` → `public static class ReferencePool`（静态类）。
- 保留 `partial` 结构以维持 `.API.cs` 分部文件（改名为 `ReferencePool.API.cs`），方法改静态。
- `m_ReferenceCollectionDict` 改为 `static readonly Dictionary<Type, ReferenceCollection>`，保留原 `lock` 语义。
- 方法静态化映射：

| 原实例方法 | 新静态方法 |
|---|---|
| `Count` | `ReferencePool.Count` |
| `Acquire<T>()` | `ReferencePool.Acquire<T>()` |
| `Recycle(IReference)` | `ReferencePool.Recycle(IReference)` |
| `Add<T>(int)` | `ReferencePool.Add<T>(int)` |
| `RemoveUnused<T>(int)` | `ReferencePool.RemoveUnused<T>(int)` |
| `RemoveAllUnused<T>()` | `ReferencePool.RemoveAllUnused<T>()` |
| `RemoveAllPools()` | `ReferencePool.ClearAll()`（关闭/重启时调用） |
| `GetAllReferencePoolInfos()` | `ReferencePool.GetAllReferencePoolInfos()` |
| `GetReferenceCollection(Type)` 私有 | 改为 `private static`，不变 |

**删改**：
- 删除 `Core/GlobalModule.cs` 的 `m_ReferencePoolModule` 字段、`ReferencePoolModule` 属性（第 39 行）。
- 删除 `Core/GlobalModule.cs` 的 `using Hotfix.Framework.ReferencePool;`。
- 删除 `Hotfix/HotfixLauncher.cs:116` 的 `RegisterModule<ReferencePoolModule>()`。
- `IReference` / `ReferenceCollection` / `ReferencePoolInfo` 迁入 Core 扁平 namespace，类型内容不变。

**重启清理（关键）**：`GameDriven.RestartGameAsync` 不卸载热更域，静态池字典会跨重启残留。原 `OnDispose` 的 `RemoveAllPools()` 由关闭时显式 `ReferencePool.ClearAll()` 替代，**组合进 `Hotfix/HotfixLauncher.cs:107` 的 `DisposeModules` 挂接处**：

```csharp
// HotfixLauncher.cs:107（由直接赋值改为组合，仅此一处）
GameDriven.Instance.DisposeModules = () =>
{
    ModuleManager.Dispose();
    ReferencePool.ClearAll();
};
```

`GameDriven.RestartGameAsync` 与 `QuitGame` 均调用 `DisposeModules`，故此覆盖重启与退出两条路径。`ReferencePool` 无异步任务，无需接入 `ICancelAsync`/`CancelAllAsync`。

### 5.2 ObjectPool：保留模块，仅迁 Core

- 目录搬入 `Core/ObjectPool/`，namespace 扁平化为 `Hotfix.Framework.Core`。
- `ObjectPoolModule : ModuleBase` **保持不变**，继续经 `ModuleManager` 每帧驱动（`OnUpdate` 自动销毁、`OnInit` 低内存订阅、`OnDispose` 清理）。
- `Core/GlobalModule.cs`：`using Hotfix.Framework.ObjectPool;` 删除（同 ns，冗余）；`ObjectPoolModule` 属性与字段**保留**，仅其 `using` 移除、类型直接可见。
- `Hotfix/HotfixLauncher.cs:120` `RegisterModule<ObjectPoolModule>()` 保留，仅其 `using` 改为 Core。
- `ObjectPool.cs:230` / `ObjectPool.Dispose.cs:206` 的 `GlobalModule.ReferencePoolModule.Recycle(obj)` → `ReferencePool.Recycle(obj)`（跟随 §5.1 改名）。

### 5.3 TaskPool：普通类，仅迁 Core

- 目录搬入 `Core/TaskPool/`，namespace 扁平化为 `Hotfix.Framework.Core`。
- 保持普通类现状，不做模块包装，不接入 `GlobalModule`，不注册。
- 全体引用其类型的文件更新 `using` 指令。

---

## 6. 全局调用点 / 指令改动汇总

| 改动类别 | 数量/位置 |
|---|---|
| `GlobalModule.ReferencePoolModule.<M>()` → `ReferencePool.<M>()` | `Acquire`×87、`Recycle`×63，其余 `Add/RemoveUnused/RemoveAllUnused/Count/GetAllReferencePoolInfos` 少量；涉及 `ReferencePoolModule` 令牌的 105 个文件 |
| 删除 `using Hotfix.Framework.ReferencePool;` | 29 文件 |
| 调整 `using` 为 Core 或删除冗余 | ObjectPool 16 文件、TaskPool 9 文件 |
| `GlobalModule.cs` 删字段/属性/两个 using | 1 文件 |
| `HotfixLauncher.cs` 删/改两处注册 using | 1 文件 |
| ObjectPool 内 3 处 `GlobalModule.ReferencePoolModule.Recycle` | 2 文件 |

**兜底**：全部在同一程序集内，编译器能捕捉所有 namespace/类型断链；无全限定引用，无需要手写重写的全限定路径。

---

## 7. 依赖方向自检（迁移后）

- `ObjectPool(Module)` → 使用 `ReferencePool(静态基座)`: **正确**，Module 依赖基座。
- `ReferencePool(静态基座)` → 仅依赖 Core 基础类型（字典/`Type`），无模块依赖：**无环**。
- `TaskPool` → 普通类，仅依赖 Core：**无环**。
- `GlobalModule` 仍聚合各模块，但不再为「基座」提供模块存取器，模块体系更纯粹。

---

## 8. 风险与缓解

| 风险 | 等级 | 缓解 |
|---|---|---|
| 105 个 `ReferencePoolModule` 调用点改名出错 | 中 | 一次性脚本式替换 `GlobalModule.ReferencePoolModule.` → `ReferencePool.`，编译器兜底，逐点清错 |
| ObjectPool 失去 `ModuleManager.Update` 驱动 → 自动销毁失效 | 高（若处理错） | **不剥离 ObjectPool**（保留 Module），其 `OnUpdate` 原样；本次不重接驱动 |
| 静态池重启残留 | 中 | `ReferencePool.ClearAll()` 显式挂入关闭路径，替代原 `OnDispose` |
| `.meta` 丢失导致引用失效 | 中 | 目录/文件及 `.meta` 一起搬移，GUID 原样保留 |
| 命名冲突（`IReference/ObjectBase/taskbase/ObjectInfo`） | 低 | 已核实 Core 中无同名类型 |
| MonoBehaviour 序列化破坏 | 低 | 三个池源码无 `MonoBehaviour` 继承（仅 README 示例），namespace 改名不触及序列化引用 |

---

## 9. 验证方式

1. `unity-cli` 编译 → 清空所有编译错误（预期为纯 using/命名空间类）。
2. 冒烟：框架启动（`HotfixLauncher`）、各模块 `OnInit`、`RestartGameAsync`（确认无静态残留、自动销毁仍生效）。
3. 抽验对象池过期自动销毁（`ObjectPoolModule.OnUpdate` 仍每帧被驱动）。
4. 全仓库搜索 `Hotfix.Framework.(ReferencePool|ObjectPool|TaskPool)` 应清空。

---

## 10. 改动文件清单（预估）

- `Core/ReferencePool/`（5 文件：改名 1 + 目录 4）
- `Core/ObjectPool/`（9 文件）
- `Core/TaskPool/`（6 文件）
- `Core/GlobalModule.cs`（删字段/属性/using）
- `Hotfix/HotfixLauncher.cs`（删/改注册 using）
- 波及文件：约 105（ReferencePool 调用点）+ 16（ObjectPool）+ 9（TaskPool）
- 无新增 asmdef、无新增包。
