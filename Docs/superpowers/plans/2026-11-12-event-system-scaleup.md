# 事件系统规模化改造实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 事件总线签名现代化——sender 退场 + string ID 退场改类型键订阅（T1）+ 无参事件统一 class 体系 + 广播侧观测补齐。

**Architecture:** 三段式编译安全重构（并存层 → 全量迁移 → 删除段），编译器错误清单即遗漏清单；总线机制（延迟队列/快照/幂等去重/回收）零变化，只动签名与身份派生。EventRegister 簿记层（挂起/重放/账本）走 internal string 键 API，public 面（含 WinBase/BaseModel/Gen 转发）全部类型键。

**Tech Stack:** Unity 2021.3+ / C# 9 / HybridCLR 热更（Hotfix 程序集）/ FairyGUI（Gen 产物为 CSharpCodeGen 生成，**生成器本体在仓库外**）/ unity-cli 编辑器自动化（编译与 Play 验证）。

**Spec:** `Docs/superpowers/specs/2026-11-12-event-system-scaleup-design.md`（决策基线五判断 + D1~D5 实施决策全在 spec §1.2/§1.4）

## Global Constraints

- 五条铁律（CLAUDE.md）：禁原生 Task / 禁协程 / 禁 LINQ / 运行时禁反射 / 异步链必有生命周期所有者
- 缩进一律 Tab；代码风格遵守 `Docs/代码风格规范.md`；注释密度与措辞对齐既有 Event 模块文件
- 全部改动限于 Hotfix 程序集 + `Assets/Editor/FuFramework/Event/`，不碰 `3rdPlugins/`
- 分发行为零变化（spec 反目标 3）：快照隔离、幂等去重、逐 handler 异常隔离、`HandleEvent` 无条件回收、挂起机制、主线程断言全部保持现状
- 广播统计玩家包零成本（spec 反目标 2）：统计字段与实现全部位于 `#if UNITY_EDITOR` 的 Debug 分部
- 每任务以 unity-cli 编译通过收尾；提交信息遵守 `Docs/Git提交规范.md`（Conventional Commits 中文版）
- 启动前置：在途 Fui 异步绑定批次已提交、工作树干净（spec §5.4）

## Review Focus

1. **挂起/重放键一致性**：Suspend 退订 / Resume 重放 / UnSubscribeAll 必须仍走 (id, handler) 账本对 internal string API——若被误改为泛型路径，挂起窗口重开订阅静默失效。验证：Task 6 面板双桶构造 + Task 5 Play 回归中开关窗口收事件。
2. **`GetType().FullName` vs 泛型键失配**：非 sealed 事件类被子类化后，`Subscribe<基类>` 广播子类实例将进两个不同频道——验收口径：全部事件类保持 sealed（Task 3 迁移时逐一确认，发现非 sealed 即停下报告）。
3. **Gen 产物被 FGUI 重新导出回退**：手改的 `CompXxx.Gen.cs` 下次 FGUI 导出会被旧模板覆盖——Task 7 完成时提醒维护者在 FGUI 编辑器插件（仓库外）同步模板；本计划只改仓库内产物。
4. **玩家包泄漏**：若 `RecordBroadcast` 实现或统计字段误放非 `#if UNITY_EDITOR` 文件，真机包带入死字典——Task 8 验收步骤含非 Editor 编译体检查。
5. **隐藏调用点漏扫**：`WinXxx.Gen.cs` 可能含与 Comp 同款转发层（未逐一核实）——Task 5/7 以编译错误清单兜底，禁止"编译过了就算完"，须 grep 双验收（Task 9）。

---

### Task 1: 总线并存层——新签名与 internal string 簿记 API

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Event/EventModule.API.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Event/Event/BaseEventArgs.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Event/EventRegister.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/UI/View/WinBase.EventRegister.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Model/BaseModel.cs`

**Interfaces:**
- Produces（后续所有任务依赖的精确签名）:
  - `EventModule.Subscribe<T>(Action<GameEventArgs> handler) where T : GameEventArgs` —— 内部转 internal string 版
  - `EventModule.Unsubscribe<T>(Action<GameEventArgs> handler) where T : GameEventArgs`
  - `EventModule.Broadcast(GameEventArgs e)` / `EventModule.BroadcastNow(GameEventArgs e)`
  - `EventModule.Subscribe(string id, Action<GameEventArgs> handler)` —— **internal**，簿记层专用（挂起/重放/账本），XML 注释标明「框架簿记专用，业务订阅走泛型形态」
  - `EventModule.Unsubscribe(string id, Action<GameEventArgs> handler)` —— internal
  - `BaseEventArgs.Id` 由 `abstract` 改 `public virtual string Id => GetType().FullName;`
  - `EventRegister.Subscribe<T>(Action<GameEventArgs>)` / `UnSubscribe<T>` / `Broadcast(GameEventArgs)` / `BroadcastNow(GameEventArgs)`（账本仍存 (id=typeof(T).FullName, handler)，挂起/重放/UnSubscribeAll 内部改调 `m_eventModule` 的 internal string 版）
  - `WinBase.Subscribe<T>/UnSubscribe<T>/Broadcast/BroadcastNow`、`BaseModel.Broadcast/BroadcastNow` 同款泛型转发
- 旧签名（`Subscribe(string, EventHandler<...>)`、`Broadcast(object, ...)` 等）本任务**全部保留不动**——并存。

- [ ] **Step 1: `EventModule.API.cs` 加泛型与 internal 重载**

泛型形态（新增，与旧签名并存）：

```csharp
public void Subscribe<T>(Action<GameEventArgs> handler) where T : GameEventArgs
	=> Subscribe(typeof(T).FullName, handler);   // 转 internal string 版

public void Unsubscribe<T>(Action<GameEventArgs> handler) where T : GameEventArgs
	=> Unsubscribe(typeof(T).FullName, handler);

public void Broadcast(GameEventArgs e) { /* null 守卫后 Event.Create + 入队，逻辑自旧 Broadcast(object, GameEventArgs) 平移，仅去 sender 形参 */ }
public void BroadcastNow(GameEventArgs e) { /* null 守卫后 HandleEvent(e)，同上 */ }

internal void Subscribe(string id, Action<GameEventArgs> handler) { /* 幂等去重等守卫逻辑自旧 public 版平移 */ }
internal void Unsubscribe(string id, Action<GameEventArgs> handler) { /* 同上平移 */ }
```

注意：新 internal string 版与旧 public `Subscribe(string, EventHandler<GameEventArgs>)` 是**重载共存**（委托类型不同不冲突）。`Broadcast(GameEventArgs e)` 与旧 `Broadcast(object, GameEventArgs)` 亦是。空参守卫措辞沿用旧版文案（去 sender 相关字样）。

- [ ] **Step 2: `BaseEventArgs.cs` Id 改基类统一派生**

```csharp
public virtual string Id => GetType().FullName;   // 原 abstract；EmptyEventArgs 的 override Id => m_eventId 保留（并存期仍被 string 重载使用）
```

子类既有 `override string Id => EventId;` 不动（Task 5 才删）。确认全部 `*EventArgs.cs` 为 `sealed`（发现非 sealed 立即停下报告维护者，见 Review Focus 2）。

- [ ] **Step 3: `EventRegister.cs` 加泛型转发 + 内部改走 internal string 版**

```csharp
public void Subscribe<T>(Action<GameEventArgs> handler) where T : GameEventArgs
{
	// 守卫逻辑平移自旧 Subscribe(string, handler)；账本 Add(typeof(T).FullName, handler)；
	// !m_suspended 时调 m_eventModule.Subscribe(typeof(T).FullName, handler)——internal string 版（挂起/重放簿记依赖 id+handler 对）
}
public void UnSubscribe<T>(Action<GameEventArgs> handler) where T : GameEventArgs { /* 同构 */ }
public void Broadcast(GameEventArgs e)  { /* 平移去 sender */ }
public void BroadcastNow(GameEventArgs e) { /* 平移去 sender */ }
```

既有 `Suspend()/Resume()/UnSubscribeAll()/Clear()` 遍历账本调 `m_eventModule.Subscribe(id, handler)`/`Unsubscribe(id, handler)` 的代码**不用改**——C# 重载解析自动选中新的 internal string 版（委托类型从 `EventHandler<>` 变 `Action<>`，账本泛型参数同步改为 `FuMultiDictionary<string, Action<GameEventArgs>>`）。

- [ ] **Step 4: `WinBase.EventRegister.cs` 与 `BaseModel.cs` 加泛型转发层**

```csharp
public void Subscribe<T>(Action<GameEventArgs> handler) where T : GameEventArgs => EventRegister.Subscribe<T>(handler);
public void UnSubscribe<T>(Action<GameEventArgs> handler) where T : GameEventArgs => EventRegister.UnSubscribe<T>(handler);
public void Broadcast(GameEventArgs e)  => EventRegister.Broadcast(e);
public void BroadcastNow(GameEventArgs e) => EventRegister.BroadcastNow(e);
```

（WinBase 版保留既有 `NotNull` 守卫链；`BaseModel` 版保留 `EventRegister?.` 空传播。旧签名转发全部保留。）

- [ ] **Step 5: unity-cli 编译验证**

Run: `unity-cli 编译命令（见 Tools/UnityCli/README.md）`
Expected: 编译通过、零错误零新警告

- [ ] **Step 6: Commit**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/Event/ Unity/Assets/Scripts/Hotfix/Framework/UI/View/WinBase.EventRegister.cs Unity/Assets/Scripts/Hotfix/Framework/Model/BaseModel.cs
git commit -m "refactor: 事件总线并存层——新增类型键订阅与无sender广播重载、基类Id统一派生、EventRegister簿记走internal string版"
```

### Task 2: 事件模块自身迁移（自洽）

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Event/EventModule.Dispatch.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Event/EventModule.Debug.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Event/EventModule.cs`（仅注释）

**Interfaces:**
- Consumes: Task 1 的 `Action<GameEventArgs>` 委托形态
- Produces: `HandleEvent` 单参化；`ForEachHandler(Action<string, Action<GameEventArgs>>)` 新签名（Task 6 面板依赖）

- [ ] **Step 1: `Dispatch.cs` 平移**

`handler.Invoke(sender, e)` → `handler(e)`；`HandleEvent(object sender, GameEventArgs e)` → `HandleEvent(GameEventArgs e)`；`DispatchBatch` 内 `HandleEvent(tempEvent.Sender, tempEvent.EventArgs)` → `HandleEvent(tempEvent.EventArgs)`。注释中「事件源」「sender」相关句子同步修订（分发语义描述去 sender 化）。

- [ ] **Step 2: `Debug.cs` 签名同步**

`ForEachHandler(Action<string, EventHandler<GameEventArgs>>)` → `ForEachHandler(Action<string, Action<GameEventArgs>>)`；`m_cachedHandlerPairSnapshot` 元组类型同步；注释同步。

- [ ] **Step 3: unity-cli 编译验证** —— Expected: 编译通过

- [ ] **Step 4: Commit**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/Event/
git commit -m "refactor: 事件模块内部迁移——HandleEvent单参化、ForEachHandler改Action委托形态"
```

### Task 3: 框架各模块调用点与 handler 迁移

**Files（以编译错误清单为准，本计划列出已核实锚点）:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Network/NetworkModule.API.cs`、`Network/NetworkModule.cs`、`Network/Socket/WebSocketNetSocket.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Scene/SceneModule.cs`、`Scene/Event/*.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Sound/`、`Download/`（含 `Download/Agent/DownloadAgent.cs`、`Download/Helper/*.cs`）、`Entity/`、`UI/`（`UIModule.Open.cs`、`UIModule.Close.cs`、`WinBase.cs`）、`RedDot/RedDotModule.cs`、`Localization/LocalizationModule.cs`、`Localization/LanguageChangeEventArgs.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Extension/TableManager.Localization.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/*/Event/*.cs`（全部框架事件参数类——本任务**只删 handler 侧引用，不删类成员**）

**Interfaces:**
- Consumes: Task 1 泛型 API
- Produces: 框架层零旧 API 调用；`LanguageChangeEventArgs` 调用点改泛型

- [ ] **Step 1: 订阅调用点改类型键**

机械模式：`m_eventModule.Subscribe(XxxEventArgs.EventId, OnXxx)` → `m_eventModule.Subscribe<XxxEventArgs>(OnXxx)`；`Unsubscribe` 同构；`WinBase`/`BaseModel` 层同理。已核实锚点：`TableManager.Localization.cs:25/33`（`LanguageChangeEventArgs.EventId` → `Subscribe<LanguageChangeEventArgs>`）、`RedDotModule.cs`、`NetworkModule.API.cs:3 处`、`DownloadAgent.cs:4 处`。

- [ ] **Step 2: 广播调用点去 sender**

`Broadcast(this, xxxArgs)` → `Broadcast(xxxArgs)`；`BroadcastNow` 同构。以编译错误清单逐个清（grep 模式 `Broadcast\((this|null|\w+)`，Task 5 结束时复核）。

- [ ] **Step 3: handler 方法签名去 sender**

`private void OnXxx(object sender, GameEventArgs e)` → `private void OnXxx(GameEventArgs e)`；XML 注释删 `<param name="sender">` 行。**仅框架层**（Game 业务在 Task 4）。

- [ ] **Step 4: unity-cli 编译验证** —— Expected: 编译通过（旧 API 已无框架层调用方，但仍在，业务层未动）

- [ ] **Step 5: Commit**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/ Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/
git commit -m "refactor: 框架模块事件调用点迁移——类型键订阅+无sender广播+handler签名去sender"
```

### Task 4: Game 业务层迁移 + Gen 产物转发层改写

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Game/UI/**`（`WinPlayerList.cs`、`CompRedDot.cs`、`CompBagContent.cs`、`ReconnectManager.cs` 等——以编译错误清单为准）
- Modify: `Unity/Assets/Scripts/Hotfix/Game/AutoGen/UI/**/*.Gen.cs`（7 个 Comp Gen + 各 `WinXxx.Gen.cs` 中含事件转发的文件）
- Modify: `Unity/Assets/Editor/FuFramework/Event/EventModuleWindow.cs`

**Interfaces:**
- Consumes: Task 1/2 的泛型 API 与新 `ForEachHandler` 签名
- Produces: Gen 转发层新形态（Task 5 验收依赖）：

```csharp
protected void Subscribe<T>(Action<GameEventArgs> handler) where T : GameEventArgs => m_eventRegister.Subscribe<T>(handler);
protected void UnSubscribe<T>(Action<GameEventArgs> handler) where T : GameEventArgs => m_eventRegister.UnSubscribe<T>(handler);
protected void Broadcast(GameEventArgs e) => m_eventRegister.Broadcast(e);
protected void BroadcastNow(GameEventArgs e) => m_eventRegister.BroadcastNow(e);
```

- [ ] **Step 1: Game 业务调用点与 handler 迁移**（模式同 Task 3 Step 1-3）

- [ ] **Step 2: Gen 产物转发层改写**

全部 `*.Gen.cs` 的事件转发区（`CompXxx.Gen.cs:111-124` 同款区块）按上方 Interfaces 形态改写——删除旧四方法（含 sender 版与 `Broadcast(string eventId, GameEventArgs e)` 这个「把 eventId 当 sender 传」的取巧重载），保留手写横幅与文件其余部分零变化。⚠️ 生成器本体在仓库外（FGUI 编辑器插件）：**完成本 Task 后在提交信息与最终汇报中提醒维护者同步 FGUI 插件模板**（Review Focus 3）。

- [ ] **Step 3: `EventModuleWindow.cs` 适配器同步**

`HandlerEntry<T>(string id, EventHandler<T> handler)` → `(string id, Action<T> handler)`、`EventEntry<T>(object sender, T e)` → `(T e)`（队列行删 `m_SenderTypeName` 与「发送者」列）；`EnsureReflection` 中 `typeof(EventHandler<>).MakeGenericType(...)` → `typeof(Action<>).MakeGenericType(...)`、`Action<,>` 构造同步（绑定 `Action<string, Action<GameEventArgs>>` 与 `Action<GameEventArgs>`）；队列视图列头/行绘制删发送者列。⚠️ 必须与本 Task 同批编译——`ForEachHandler` 已换签名，面板滞后一提交窗口即运行时绑定失败。

- [ ] **Step 4: unity-cli 编译验证 + Editor 程序集编译** —— Expected: 双程序集编译通过

- [ ] **Step 5: Commit**

```bash
git add Unity/Assets/Scripts/Hotfix/Game/ Unity/Assets/Editor/FuFramework/Event/
git commit -m "refactor: Game业务与Gen转发层迁移——类型键订阅全面替换，调试面板适配Action委托与队列去sender列（FGUI插件模板待同步）"
```

### Task 5: 删除段——旧 API、EmptyEventArgs、per-class Id 全部退役

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Event/EventModule.API.cs`（删 4 个旧签名）
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Event/EventRegister.cs`、`WinBase.EventRegister.cs`、`BaseModel.cs`（删旧转发）
- Delete: `Unity/Assets/Scripts/Hotfix/Framework/Event/Event/EmptyEventArgs.cs`
- Modify: 全部 `*EventArgs.cs`（删 `override string Id` 与 `static readonly string EventId`）
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Event/Event/EventModule.cs` 类头注释（功能清单去「事件编号」措辞）

**Interfaces:**
- Consumes: Task 3/4 后旧 API 调用方为零
- Produces: 最终公开 API 面 = Task 1 Interfaces 中的泛型与无 sender 形态，无并存残留

- [ ] **Step 1: 删旧签名与旧转发**——`EventModule.API.cs` 删 `Subscribe(string, EventHandler<>)`、`Unsubscribe(string, EventHandler<>)`、`Broadcast(object, GameEventArgs)`、`BroadcastNow(object, GameEventArgs)`、`Broadcast(object, string)`（string 重载）；`EventRegister`/`WinBase`/`BaseModel` 删对应旧转发。**internal string 版保留**（挂起/重放簿记依赖，注释已标明）。同批移除 `BaseEventArgs` 的 `EventArgs` 继承（`BaseEventArgs.cs:10`）——旧 `EventHandler<T>` 的 `T : EventArgs` 约束已随本步消失，必须与 Step 1 同一编译单元，不能提前到 Task 1。

- [ ] **Step 2: 删 `EmptyEventArgs.cs`**；`EventModule.cs` 类头与 README 引用留待 Task 8 文档同步（本任务只删代码）。

- [ ] **Step 3: 删全部事件类的 `override string Id` 与 `public static readonly string EventId`**（改用基类 `virtual Id => GetType().FullName`）。以编译错误兜底残留引用（如 Step 1-2 漏扫的调用点此时爆出）。** FullName 缓存验证**（spec 备选）：Play 中对高频事件断点/插桩确认 `e.Id` 热路径无每次分配——若 `Type.FullName` 每调用分配则回退 per-class `static readonly string EventId` 形态（基类 Id 保留 virtual 供 override），回退决策记入提交信息。

- [ ] **Step 4: unity-cli 编译验证 + grep 双验收**

```bash
grep -rn "EmptyEventArgs" Unity/Assets/Scripts/ --include="*.cs"        # Expected: 零匹配
grep -rn "static readonly string EventId" Unity/Assets/Scripts/ --include="*.cs"  # Expected: 零匹配
grep -rn "BroadcastNow?(\(this\|null\|\w\+\)," Unity/Assets/Scripts/ --include="*.cs"  # Expected: 零匹配
```

- [ ] **Step 5: unity-cli Play 短回归**——进 Play 模式验证：语言切换（`TableManager` 订阅路径）、红点刷新、背包开关窗收事件（挂起/重放路径）。Expected: 全部正常、Console 无事件相关报错。

- [ ] **Step 6: Commit**

```bash
git add -A Unity/Assets/Scripts/
git commit -m "refactor: 事件总线旧签名与EmptyEventArgs退役——public面收敛为类型键+无sender形态，internal string版保留为挂起簿记专用"
```

### Task 6: 广播统计机制（工作流 C-1）

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Event/EventModule.API.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Event/EventModule.Debug.cs`

**Interfaces:**
- Consumes: Task 5 后的最终 API 面
- Produces（Task 7 面板数据源，精确签名）:

```csharp
// API.cs（无条件编译声明，partial）
partial void RecordBroadcast(string eventId);
// API.cs：Subscribe（internal string 版入口——所有订阅路径的单汇点）置历史标志：
partial void RecordSubscribe(string eventId);
// Debug.cs（#if UNITY_EDITOR）对外只读快照：
public readonly struct BroadcastStatSnapshot { /* Id / Count / LastBroadcastTime / NoListenerNeverSubscribedCount / NoListenerEverSubscribedCount */ }
public List<BroadcastStatSnapshot> ForEachBroadcastStat()  // 面板取数
public void ClearBroadcastStats()                          // 面板清零按钮
```

- [ ] **Step 1: API.cs 声明与调用点**——`Broadcast(GameEventArgs e)` 入队前、`BroadcastNow` 调 `HandleEvent` 前各加一行 `RecordBroadcast(e.Id);`（null 守卫之后）；internal `Subscribe` 成功路径加 `RecordSubscribe(id);`。

- [ ] **Step 2: Debug.cs 实现**（`#if UNITY_EDITOR` 文件内）

```csharp
private readonly Dictionary<string, BroadcastStat> m_broadcastStats = new();   // BroadcastStat: class { int Count; float LastTime; int NeverSubscribedCount; int EverSubscribedIdleCount; }
private readonly HashSet<string> m_everSubscribedIds = new();                  // 单调，永不清理（spec §7.1 双桶口径）
```

`RecordBroadcast`：无监听判定用 `m_eventHandlerMultiDict.Contains(eventId)`（**只查键**，非 O(n) 双参重载）；无监听时按 `m_everSubscribedIds.Contains` 分桶累加；`RecordSubscribe`：`m_everSubscribedIds.Add(id)`。

- [ ] **Step 3: unity-cli 编译验证（含非 Editor 体检）**——确认 `m_broadcastStats`/`m_everSubscribedIds` 仅存在于 `#if UNITY_EDITOR` 文件；`partial` 无实现时调用点被剥除（反目标 2）。

- [ ] **Step 4: Commit**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/Event/
git commit -m "feat: 事件模块广播统计——Editor期每ID计数/最近时间/无监听双桶，partial方法玩家包零成本"
```

### Task 7: 调试面板广播统计段（工作流 C-2）

**Files:**
- Modify: `Unity/Assets/Editor/FuFramework/Event/EventModuleWindow.cs`

**Interfaces:**
- Consumes: Task 6 的 `ForEachBroadcastStat()`/`ClearBroadcastStats()`

- [ ] **Step 1: 反射绑定扩展**——`EnsureReflection` 增加 `ForEachBroadcastStat`/`ClearBroadcastStats` 绑定（HotfixReflection.TypeNames 若需增补同步 `Common/Debug/HotfixReflection.cs`）；`OnRefresh` 一并取统计快照。

- [ ] **Step 2: 绘制「广播统计」段**——列：事件 ID｜当前订阅数｜广播次数｜最近广播时间｜无监听（从未订阅 / 有历史订阅 两列）；默认按广播次数降序；工具栏加「清零统计」按钮（调 `ClearBroadcastStats`）；无监听>0 的行高亮。布局对齐既有订阅明细段风格（foldout + 搜索过滤共用 `m_searchFilter`）。

- [ ] **Step 3: unity-cli Editor 编译 + Play 实测**——广播若干事件：计数增长、无监听分桶正确（构造一个从未订阅 ID 验证「从未订阅」桶；关一个已订阅窗口再广播验证「有历史订阅」桶——Review Focus 1 的挂起口径）。

- [ ] **Step 4: Commit**

```bash
git add Unity/Assets/Editor/FuFramework/Event/
git commit -m "feat: 事件调试面板广播统计段——双桶无监听可见性，补齐排障第一问"
```

### Task 8: README 契约与编写规范（工作流 B）

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Event/README.md`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Event/EventModule.API.cs`、`EventModule.Dispatch.cs`（各一行注释）

**Interfaces:** 纯文档 + 注释，无代码接口。

- [ ] **Step 1: README §4.4 补事件参数契约**——属性一律 `private set`、仅 `Create` 构造期赋值、发布后外部只读、处理函数不得修改/转发/缓存收到的 e；§5.1 示例按 Task 5 后形态重写（无 `Id`/`EventId` 成员、无 sender、订阅走 `Subscribe<T>`），并标注为唯一编写模板；§10.2 示例改同形态（修 public setter 反向示范）。

- [ ] **Step 2: README §10.1 删除"枚举（配合 EmptyEventArgs）"推荐与 `ToString()` 示例**；§4.2/§4.3/§6 方法签名清单全部对齐 Task 5 后 API；删除 `EmptyEventArgs` 相关段落（§3.1/§4.4/§5.3/§10.1/注意事项 7）。

- [ ] **Step 3: `Broadcast`/`HandleEvent` 注释补「处理函数不得修改收到的 e」**（spec §8.2）。

- [ ] **Step 4: Commit**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/Event/
git commit -m "docs: 事件模块README契约固化——private set参数契约/唯一编写模板/去枚举推荐，代码注释补不得修改禁令"
```

### Task 9: 终验收

**Files:** 无新改动（验收与修复轮）

- [ ] **Step 1: grep 三重验收**（spec §12 口径）

```bash
grep -rn "object sender" Unity/Assets/Scripts/Hotfix/Framework/Event/ Unity/Assets/Scripts/Hotfix/Game/ --include="*.cs" | grep -v "AutoGen"   # Expected: 仅剩 Gen 产物外的零匹配（AutoGen 已在 Task 4 清完，应全零）
grep -rn "\.Subscribe(\"\|\.Unsubscribe(\"\|\.UnSubscribe(\"" Unity/Assets/Scripts/ --include="*.cs"                                          # Expected: 零（internal 簿记内部调用除外——核对命中行均在 EventRegister.cs 内部）
grep -rn "EventHandler<GameEventArgs>" Unity/Assets/Scripts/ --include="*.cs"                                                                 # Expected: 零
```

- [ ] **Step 2: unity-cli Play 全回归**——登录→进主城→开关背包/红点窗口→语言切换→重连触发（ReconnectManager 订阅路径），Console 零事件相关报错；调试面板订阅明细/广播统计/队列三段数据正确。

- [ ] **Step 3: 残留修复轮**——验收发现的漏扫调用点按 Task 3/4 模式修掉并补提交。

- [ ] **Step 4: 汇报**——向维护者汇报：三段式提交记录、FGUI 插件模板待同步提醒（Review Focus 3）、README §5.x 旧示例延后项（D2，单独一笔后续处理）。
