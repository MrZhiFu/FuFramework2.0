# UI 窗口事件订阅"关窗挂起/开窗重放"（方案A）实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 窗口关闭时自动挂起其事件订阅（总线退订、账本保留）并终止计时器，打开时自动重放——确立"关窗=暂停、开窗=恢复"的框架立场，业务零手动配对。

**Architecture:** 机制集中在 `EventRegister`（新增 `m_suspended` 标志 + `Suspend()`/`Resume()`，账本即重放清单）与 `WinBase.Life` 两个生命周期调用点；顺带以账本去重守卫堵住审查遗留 #1（账本-总线失配）。行为与既有 `m_cancellation`（关窗取消异步）同构。

**Tech Stack:** Unity 2022 / C# 热更程序集（HybridCLR）/ FairyGUI / FuLogger / unity-cli 编译验证。

**Spec:** `Docs/superpowers/specs/2026-11-08-ui-window-event-suspend-design.md`（方案A 已拍板，5.3/5.4/5.5 为机制权威定义；执行者须同时读本计划与设计稿）。

## Global Constraints

- Tab 缩进；禁 LINQ / 原生 Task / 协程 / 运行时反射（`Docs/代码风格规范.md` + CLAUDE.md 五铁律）。
- 模块守卫一律 `FuLogger` 日志（LogError/LogWarning + 忽略），**不抛异常**——EventModule/EventRegister 既定契约（a4d2948a 定案）。
- 每个新建/修改的 .cs 文件 namespace 前保留 `// ReSharper disable once CheckNamespace`。
- 单文件 ≤500 行；注释风格=防御式守卫逐条说明"为什么"，陷阱写进注释防回退。
- 每个任务收尾必须编译验证：`unity-cli raw refresh_assets --json '{}' && sleep 8 && unity-cli raw get_compilation_state --json '{}'` → 期望 `"errorCount": 0`（2 条 warning 是 unity-cli-bridge 自身日志，忽略）。
- 提交信息 `[AI]<type>: <中文描述>`（Docs/Git提交规范.md）；只暂存本任务文件，工作区其他改动（其他会话的守卫扫荡等）严禁混入。
- 设计稿 §5.3 伪码为本任务群的行为权威；本计划与之冲突时以设计稿为准并回报。

## Review Focus

规格隐含、但单项任务测试覆盖不到、最容易咬人的五个失效模式（每条已在归属任务中钉住验证步骤）：

1. **挂起态寄存器回池复用给新窗口**：`Clear()` 漏复位 `m_suspended` → 新窗口订阅只进账本不进总线，事件静默失效。→ Task 2 步骤 4 的复位行 + Task 6 检查项 F。
2. **Resume 晚于业务 OnOpen**：业务 OnOpen 期间订阅未就绪。→ Task 3 锚点钉死（`m_cancellation.Recreate()` 之后、`OnOpen()` 之前）+ Task 6 检查项 D。
3. **半成品实例（三个注册器为 null）走 _OnClose/_OnOpen**：挂起调用必须 null 安全（`EventRegister?.`），否则半成品销毁链路 NRE。→ Task 3 插入代码固定带 `?.`。
4. **WinUI==null 的 teardown 关闭分支漏挂起**：插桩点必须在 `m_cancellation.Cancel()` 之后、`WinUI == null` 判空之前，两个分支都覆盖。→ Task 3 锚点钉死 + Task 6 检查项 E。
5. **OnOpen 旧习惯重复订阅 → 账本无界膨胀**：Task 1 的账本去重守卫是唯一防线（分发不翻倍由总线去重保证，但账本会 N 倍涨）。→ Task 1 步骤 2 + Task 6 检查项 F。

---

### Task 1: EventRegister 账本去重守卫（堵审查遗留 #1 账本失配）

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Event/EventRegister.cs`（`Subscribe` 方法，约 :58-74）

**Interfaces:**
- Consumes: 无（独立任务）。
- Produces: `EventRegister.Subscribe` 的新契约——同一 `(id, handler)` 重复登记时 LogWarning + 忽略，账本与总线条目严格 1:1（后续 Task 2 的 Suspend/Resume 依赖此对称性）。

- [ ] **Step 1: 在 `Subscribe` 的 `m_eventModule == null` 守卫之后、`m_eventHandlerDict.Add` 之前插入去重守卫**

```csharp
			if (m_eventHandlerDict.Contains(id, handler))
			{
				FuLogger.LogWarning($"[EventRegister]重复登记事件 '{id}' 的同一处理函数，本次已忽略（账本与总线条目保持 1:1，防挂起重放对账错位）:{handler}");
				return;
			}
```

同时在 `Subscribe` 的 XML 注释中补一段幂等语义说明（写明"与 EventModule.Subscribe 的总线级去重对齐，账本去重保证 Suspend/Resume 按账本增删总线时严格对称"）。

- [ ] **Step 2: 编译验证**

Run: `unity-cli raw refresh_assets --json '{}' && sleep 8 && unity-cli raw get_compilation_state --json '{}'`
Expected: `"errorCount": 0`

- [ ] **Step 3: Commit**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/Event/EventRegister.cs
git commit -m "[AI]fix: EventRegister 账本补去重守卫（重复登记警告+忽略，账本与总线 1:1 对称）——堵审查遗留 #1：重复订阅致账本 N 份总线 1 条的失配会让挂起重放对账错位"
```

---

### Task 2: EventRegister 挂起机制（m_suspended + Suspend/Resume + Clear 复位）

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Event/EventRegister.cs`

**Interfaces:**
- Consumes: Task 1 的账本 1:1 对称性。
- Produces（Task 3 依赖的精确签名）:
  - `public void Suspend()` —— 幂等；已挂起直接返回
  - `public void Resume()` —— 幂等；未挂起直接返回
  - `private bool m_suspended;`（默认 false）

- [ ] **Step 1: 新增字段与方法（类注释的"错误契约"段后补一句挂起契约）**

在 `m_eventHandlerDict` 字段声明之后新增：

```csharp
		/// <summary>
		/// 是否已挂起（窗口关闭态）：挂起时账本仍登记新订阅、总线侧保持退订，
		/// Resume 按账本统一重放。寄存器经 ReferencePool 回池复用且 Acquire 不重置字段，
		/// Clear 必须复位本标志，否则挂起态寄存器被新窗口领走后订阅全部只进账本不进总线（事件静默失效）。
		/// </summary>
		private bool m_suspended;
```

按设计稿 §5.3 伪码实现两个公开方法（签名与守卫语义如下，正文由实现者补全）：

- `Suspend()`：幂等守卫（`m_suspended` 已为 true 直接 return）→ 置位 → 遍历 `m_eventHandlerDict` 逐条 `m_eventModule.Unsubscribe(id, handler)`（模块侧对缺失条目安全空操作），**账本保留不清空**。
- `Resume()`：幂等守卫（未挂起直接 return）→ 复位 → 遍历账本逐条 `m_eventModule.Subscribe(id, handler)` 重放。

- [ ] **Step 2: `Subscribe` 加挂起态分支、`Clear` 加复位**

`Subscribe` 中，去重守卫之后、账本登记处改为：账本登记照旧，但**仅当 `!m_suspended` 才向总线 `Subscribe`**（挂起态只记账，重开时由 Resume 统一重放）。`Clear()` 在 `m_eventModule = null;` 之前加 `m_suspended = false;`（Review Focus #1）。

- [ ] **Step 3: 编译验证**

Run: `unity-cli raw refresh_assets --json '{}' && sleep 8 && unity-cli raw get_compilation_state --json '{}'`
Expected: `"errorCount": 0`

- [ ] **Step 4: Commit**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/Event/EventRegister.cs
git commit -m "[AI]feat: EventRegister 新增关窗挂起/开窗重放机制（m_suspended + Suspend/Resume 幂等对，挂起态订阅只记账本，Clear 复位防回池复用静默失效）"
```

---

### Task 3: WinBase 生命周期接线（_OnClose 挂起+停计时器，_OnOpen 重放）

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/UI/View/WinBase.Life.cs`（`_OnOpen` :39-69、`_OnClose` :128-159）

**Interfaces:**
- Consumes: Task 2 的 `EventRegister.Suspend()/Resume()`；`WinBase.StopAllTimers()`（已存在，`WinBase.TimerRegister.cs:100`，自带 `TimerRegister?.` 空安全）。
- Produces: 窗口生命周期的挂起/重放触发点（Task 4/6 的行为前提）。

- [ ] **Step 1: `_OnClose` 在 `m_cancellation.Cancel();`（:131）之后插入两行**

```csharp
			EventRegister?.Suspend(); // 关窗挂起订阅：总线退订、账本保留（半成品实例注册器为 null，?. 跳过）
			StopAllTimers();          // 关窗终止计时器（破坏性不重放，与异步取消同立场）
```

插入点必须在 `WinUI == null` 判空**之前**（Review Focus #4：teardown 分支同样要挂起）。

- [ ] **Step 2: `_OnOpen` 在 `m_cancellation.Recreate();`（:42）之后插入一行**

```csharp
			EventRegister?.Resume(); // 开窗重放挂起的订阅：必须在业务 OnOpen 之前，保证其执行期间订阅已就绪
```

- [ ] **Step 3: 编译验证**

Run: `unity-cli raw refresh_assets --json '{}' && sleep 8 && unity-cli raw get_compilation_state --json '{}'`
Expected: `"errorCount": 0`

- [ ] **Step 4: Commit**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/UI/View/WinBase.Life.cs
git commit -m "[AI]feat: WinBase 生命周期接入订阅挂起——_OnClose 挂起事件订阅并终止计时器，_OnOpen 在业务回调前重放订阅（与 m_cancellation 同构：关窗即暂停、开窗即恢复）"
```

---

### Task 4: WinPlayerList 迁移（删除手动配对，订阅回 OnInit）

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Game/UI/Login/WinPlayerList.cs`（:28-79 区域）

**Interfaces:**
- Consumes: Task 3 的框架级挂起/重放（本任务删除的正是被其取代的手动配对）。

- [ ] **Step 1: 三处修改**

1. `OnInit`（:31-35）末尾追加 `InitEvent();`。
2. `OnOpen`（:60-66）删除 `InitEvent();` 调用行（其余保留）。
3. 整个 `OnClose` override（:68-79，含全部注释）删除——框架已自动挂起，历史注释描述的坑（幽灵触发/误杀）由设计稿与本任务注释存档，业务代码不再需要。

4. `InitEvent` 注释（:47-49）替换为：

```csharp
		/// <summary>
		/// 注册网络事件订阅。订阅放 OnInit（每实例一次）：框架在关窗时自动挂起、开窗时自动重放，
		/// 业务不再手动配对退订（历史幽灵触发与 UnSubscribeAll 误杀问题见设计稿 2026-11-08 §1.2）。
		/// </summary>
```

- [ ] **Step 2: 编译验证**

Run: `unity-cli raw refresh_assets --json '{}' && sleep 8 && unity-cli raw get_compilation_state --json '{}'`
Expected: `"errorCount": 0`

- [ ] **Step 3: Commit**

```bash
git add Unity/Assets/Scripts/Hotfix/Game/UI/Login/WinPlayerList.cs
git commit -m "[AI]refactor: WinPlayerList 迁移至订阅挂起机制——InitEvent 移入 OnInit 每实例一次，删除 OnOpen 重订与 OnClose 手动退订（框架自动挂起/重放取代手动配对，2192574f 的临时修法退役）"
```

---

### Task 5: 模板与文档同步（新约定落纸面）

**Files:**
- Modify: `FairyGUIProject/plugins/CSharpCodeGen/Template/WinTemplate.txt`（先通读定位 Example 注释区）
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/UI/README.md`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Event/README.md`（EventRegister 节）

**Interfaces:**
- Consumes: Task 2/3 的最终机制形态（Suspend/Resume 触发时机、OnInit 订阅约定）。

- [ ] **Step 1: Gen 模板注释更新**

通读 `WinTemplate.txt`，找到 `InitEvent`/`Example:Subscribe` 示例注释区，将订阅示例注释改为表达新约定：「订阅放 OnInit（每实例一次），框架在关窗时自动挂起、开窗时自动重放，勿在 OnOpen 订阅（会导致账本与总线语义漂移）」。仅改注释文字，不动模板结构与生成逻辑。注意：模板只影响未来新生成的窗口，存量窗口注释由各业务自维护（WinPlayerList 已在 Task 4 手改）。

- [ ] **Step 2: UI README 新增机制章节**

在 `Unity/Assets/Scripts/Hotfix/Framework/UI/README.md` 合适位置（事件订阅相关章节附近）新增小节，内容覆盖：挂起/重放触发时机（_OnClose/_OnOpen）、订阅放 OnInit 的约定与原因、计时器关窗终止语义、"关着也要收事件"目前无场景（出现再评估）。

- [ ] **Step 3: Event README 的 EventRegister 节补挂起语义**

`Unity/Assets/Scripts/Hotfix/Framework/Event/README.md` §4.2（EventRegister）补一段：Suspend/Resume 的幂等契约、挂起态 Subscribe 行为、Clear 复位的必要性（防回池复用静默失效）。

- [ ] **Step 4: 编译验证 + Commit**

Run: `unity-cli raw refresh_assets --json '{}' && sleep 8 && unity-cli raw get_compilation_state --json '{}'`
Expected: `"errorCount": 0`

```bash
git add FairyGUIProject/plugins/CSharpCodeGen/Template/WinTemplate.txt Unity/Assets/Scripts/Hotfix/Framework/UI/README.md Unity/Assets/Scripts/Hotfix/Framework/Event/README.md
git commit -m "[AI]docs: 订阅挂起机制文档同步——Gen 模板示例对齐『订阅放 OnInit』新约定，UI/Event README 补挂起/重放语义与计时器终止说明"
```

---

### Task 6: Play 模式插桩验证（设计稿 §8 清单）+ 还原插桩

**Files:**
- Modify（临时，验证后还原）: `Unity/Assets/Scripts/Hotfix/Framework/Event/EventRegister.cs`（Suspend/Resume 入口各加一行 LogInfo 探针）
- Modify（临时，验证后还原）: `Unity/Assets/Scripts/Hotfix/Framework/UI/View/WinBase.Life.cs`（`_OnLanguageChanged` 入口加一行 LogInfo 探针）

**Interfaces:**
- Consumes: Task 1-4 的完整机制；现成观测工具 `FuFramework/调试/事件调试面板`（EventModuleWindow，订阅明细实时反映总线侧条目）。

- [ ] **Step 1: 插入三处临时探针（各一行 LogInfo，带 `[SUSPEND-PROBE]` 前缀便于检索）**

`EventRegister.Suspend`/`Resume` 入口、`WinBase._OnLanguageChanged` 入口。探针是唯一允许的临时改动，其余验证全部用现成日志与调试面板。

- [ ] **Step 2: 编译并进入 Play 模式，按清单逐项验证（连本地服务器 29100 走真实登录流程）**

- **A. 开窗收到**：WinPlayerList 打开时断开重连（或重启服务器触发重连广播）→ 控制台出现 `OnNetworkConnected` 日志。
- **B. 关窗不收到**：登录成功窗口自动关闭（`CloseSelf`）后，再次触发重连广播 → `OnNetworkConnected` 日志**不**出现，且 `[SUSPEND-PROBE] Suspend` 先于广播出现；事件调试面板中该窗口的两个网络订阅条目消失。
- **C. 重开收到**：回到登录界面重新打开 WinPlayerList → 触发广播 → 日志重新出现，调试面板条目恢复，`[SUSPEND-PROBE] Resume` 先于广播出现。
- **D. 语言刷新（Resume 先于业务 OnOpen 的间接验证）**：窗口开着时切换语言 → `_OnLanguageChanged` 探针日志出现、界面文本刷新。
- **E. teardown 分支**：在 `UIModule.PoolExpireTimeAfterIdle` 调小（如 5 秒，它是 public 属性）后关窗，等池过期销毁 → 事件调试面板 `handler 总数` 回落到基线，无残留。
- **F. 复用后可达 + 账本不膨胀**：过期销毁后重开窗口 → 广播可达（探针 Resume 出现）；随后**不关窗**再次手动触发一次重复订阅路径（如重复走一次 OnInit 的窗口重复打开）→ 控制台出现 Task 1 的 `[EventRegister]重复登记` 警告，且调试面板 `handler 总数` **不增长**。

- [ ] **Step 3: 还原三处探针并确认零残留**

`git diff` 确认仅剩预期文件；`grep -rn "SUSPEND-PROBE" Unity/Assets/` 归零。恢复 `PoolExpireTimeAfterIdle` 无需手动（代码常量未被改，仅运行时改值）。

- [ ] **Step 4: 编译验证**

Run: `unity-cli raw refresh_assets --json '{}' && sleep 8 && unity-cli raw get_compilation_state --json '{}'`
Expected: `"errorCount": 0`

- [ ] **Step 5: 验证结论回报（无 commit——探针已还原，工作区应干净）**

任一项不符合预期：按 Review Focus 对应条目回 Task 1-3 排查，修复后重跑全清单。全部通过则方案A 完成，向维护者回报六项结果。

---

## Self-Review 记录

- **Spec 覆盖**：设计稿 §5.3（Task 2）、§5.4（Task 2 Clear 复位 + Task 6-E/F）、§5.5（Task 3）、§5.6（Task 4/5）、§6 迁移（Task 4，唯一迁移点）、§8 验证清单（Task 6 A-F 全映射）、#1 账本失配（Task 1）——无遗漏。
- **类型一致性**：`Suspend()`/`Resume()`/`m_suspended` 在 Task 2 定义、Task 3 消费，签名一致；`StopAllTimers()` 为既有 API（WinBase.TimerRegister.cs:100）。
- **Review Focus**：五条各有归属任务与钉住步骤（见节内标注）。
- **比例**：计划 6 任务 ≈ 设计稿 151 行的 1.5 倍，代码块仅签名/守卫/锚点级，未替实现者写正文。
