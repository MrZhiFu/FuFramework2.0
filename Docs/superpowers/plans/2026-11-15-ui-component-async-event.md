# UI 组件层异步事件机制 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 让 Gen 组件（Comp*）的 UI 交互事件处理器支持 `async UniTask` 签名，组件自持生命周期取消源、窗口开关时自动取消/重建，与窗口侧机制（提交 9615b670）对齐。

**Architecture:** 组件 Gen 模板自持 `LifecycleCancellationSource` 并实现广播接口；`WinBase._OnClose/_OnOpen` 遍历 WinUI 组件树对命中接口的组件广播；Gen 代理新增异步重载透传组件自身取消源。`FuiEventRegister` 异步重载（9615b670 已就绪）零改动直接复用。

**Tech Stack:** Unity + FairyGUI + UniTask + CSharpCodeGen（FGUI 编辑器插件，Lua 占位符替换）

**Spec:** 对话定案（2026-11-12，记忆 `pending-ui-async-event`）；前置实现见提交 9615b670 与 `Unity/Assets/Scripts/Hotfix/Framework/UI/README.md` §4.7

## Global Constraints

- 项目铁律（CLAUDE.md）：UniTask 替代 Task/协程、杜绝 LINQ、运行时杜绝反射、异步链必须有生命周期所有者（CancellationToken 必传）
- C# 语言版本 9.0：禁用 lambda 显式返回类型（`async UniTask () =>` 会报 CS8773），匿名 lambda 用隐式返回，具名方法用局部函数/静态方法
- Gen 文件头声明"切记不可手动更改"：产物必须经 CSharpCodeGen 产出；手动同步仅作 Publish 不可用时的备选，且内容须与模板产出一致
- 验证手段：项目无测试设施（用户已豁免 TDD）——统一用 unity-cli 编译零错误 + Play 探针实测（探针放 Hotfix 程序集、`#if UNITY_EDITOR`、ASCII 菜单名、结果写 `%TEMP%` 文件轮询、用完删净含 .meta）
- 提交规范：`[AI]<type>: <中文描述>`；只提交本批文件；模板文件在 FairyGUIProject（非 Unity 工程），改后无编译验证，靠产物比对
- 缩进：.cs 文件用 Tab；模板 .txt 现状为 4 空格与 Tab 混用，新增块跟随所在区块的既有缩进风格

## Review Focus

1. **窗口关闭瞬间在途的组件异步处理器**——期望被取消且 OCE 静默（无日志无报错）；测试钉在 Task 5 探针验证点②
2. **窗口复用第二次开窗后组件处理器再触发**——期望拿到 Recreate 后的新 Token 正常执行（不得被旧生命周期的取消误杀）；测试钉在 Task 5 探针验证点③
3. **组件 Dispose 后取消源残留**——期望 `m_cancellation?.Dispose()` 随 Gen Dispose 链执行，窗口销毁路径零泄漏；测试钉在 Task 5 探针验证点④（Dispose 后触发新处理器应立即 OCE 静默——Token 退化为已取消快照）
4. **嵌套组件（Comp 内嵌 Comp）**——期望广播递归命中多层；测试钉在 Task 5（mini 组件内嵌一层子组件，验证点⑥）
5. **从未收到广播的组件（动态中途 AddChild）**——期望取消源初始态可用、异步注册正常，不因缺失广播而炸；测试钉在 Task 5 探针验证点⑤

---

### Task 1: 新建广播接口 IWinLifecycleAware

**Files:**
- Create: `Unity/Assets/Scripts/Hotfix/Framework/UI/IWinLifecycleAware.cs`

**Interfaces:**
- Consumes: 无
- Produces: `interface IWinLifecycleAware { void OnWinClosed(); void OnWinOpened(); }`（命名空间 `Hotfix.Framework.UI`）——Task 2 模板实现它，Task 4 WinBase 广播按它判定

- [ ] **Step 1: 创建接口文件**

文件头注释风格对齐同目录其他文件（`// ReSharper disable once CheckNamespace`、中文 XML 注释）。接口两个方法：`OnWinClosed()`（窗口关闭通知：实现方取消自身生命周期取消源）、`OnWinOpened()`（窗口打开通知：实现方重建取消源开启新生命周期）。XML 注释写明触发点：由 `WinBase._OnClose/_OnOpen` 遍历组件树广播，实现方应在此取消/重建自持取消源。

- [ ] **Step 2: 编译验证**

Run: `unity-cli tool call refresh_assets --output json` 后轮询 `get_compilation_state`，Expected: errorCount=0

- [ ] **Step 3: Commit**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/UI/IWinLifecycleAware.cs
git commit -m "[AI]feat: 新增IWinLifecycleAware组件生命周期广播接口（窗口开关通知组件自持取消源）"
```

---

### Task 2: 改造 Gen 组件模板（CompGenTemplate.txt）

**Files:**
- Modify: `FairyGUIProject/plugins/CSharpCodeGen/Template/CompGenTemplate.txt`（唯一改动文件；`CompTemplate.txt` 手写层模板无需改——InitUIEvent 调用点写法不变靠重载解析分流）

**Interfaces:**
- Consumes: Task 1 的 `IWinLifecycleAware`（using 已在模板头部，`Hotfix.Framework.UI`）
- Produces: 每个 Gen 组件将生成——`private readonly LifecycleCancellationSource m_cancellation = new();` 字段；类声明 `: #COMPTYPE#, IWinLifecycleAware`；`OnWinClosed/OnWinOpened` 公开实现；异步代理 `AddUIListener/SetUIListener/RemoveUIListener` 双签名 6 个（透传 `m_cancellation`）；Dispose 链追加 `m_cancellation?.Dispose()`

- [ ] **Step 1: 类声明行加接口**

第 18 行 `public partial class #COMPNAME# : #COMPTYPE#` 改为 `public partial class #COMPNAME# : #COMPTYPE#, IWinLifecycleAware`。

- [ ] **Step 2: 加自持取消源字段**

在三个 Register 字段（第 20-22 行）之后插入：

```csharp
        private readonly LifecycleCancellationSource m_cancellation = new(); // 组件生命周期取消源：窗口关闭取消、打开重建（IWinLifecycleAware 广播驱动）
```

- [ ] **Step 3: Dispose 链追加取消源释放**

`Dispose()` 中三个 Register Release 之后、`base.Dispose()` 之前插入 `m_cancellation?.Dispose();`。

- [ ] **Step 4: 加广播接口实现与异步代理**

`ConstructFromXML` 尾部（`OnInit()` 之后）追加对称的挂树/摘树兜底订阅（GList 回收/复用 item 会真实派发 RemovedFromStage/AddedToStage——进池即取消在途任务、复用即重建生命周期；与窗口广播重复触发时 `Cancel`/`Recreate` 各自幂等无害）：

```csharp
			displayObject.onAddedToStage.Add(OnWinOpened);     // 挂树/复用兜底：重新入树重建生命周期（列表项出池复用）
			displayObject.onRemovedFromStage.Add(OnWinClosed); // 摘树/进池兜底：离开组件树即取消在途异步任务
```

在"UI事件处理"region 内、同步代理之后插入（方法体一行直转，注释对齐同步代理粒度）：

```csharp
        protected void AddUIListener(EventListener listener, Func<EventContext, UniTask> callback) => m_uiEventRegister.AddUIListener(listener, callback, m_cancellation);

        protected void AddUIListener(EventListener listener, Func<UniTask> callback) => m_uiEventRegister.AddUIListener(listener, callback, m_cancellation);

        protected void SetUIListener(EventListener listener, Func<EventContext, UniTask> callback) => m_uiEventRegister.SetUIListener(listener, callback, m_cancellation);

        protected void SetUIListener(EventListener listener, Func<UniTask> callback) => m_uiEventRegister.SetUIListener(listener, callback, m_cancellation);

        protected void RemoveUIListener(EventListener listener, Func<EventContext, UniTask> callback) => m_uiEventRegister.RemoveUIListener(listener, callback);

        protected void RemoveUIListener(EventListener listener, Func<UniTask> callback) => m_uiEventRegister.RemoveUIListener(listener, callback);
```

新增独立 region（放在"UI事件处理"region 之后）承载接口实现：

```csharp
        #region 窗口生命周期广播（IWinLifecycleAware）

        /// <summary>
        /// 窗口关闭通知：取消本组件在途异步处理器（WinBase._OnClose 遍历广播）。
        /// </summary>
        public void OnWinClosed() => m_cancellation.Cancel();

        /// <summary>
        /// 窗口打开通知：重建取消源开启新生命周期（WinBase._OnOpen 遍历广播）。
        /// </summary>
        public void OnWinOpened() => m_cancellation.Recreate();

        #endregion
```

- [ ] **Step 5: 比对确认**

模板无占位符变动（`GenComp.lua` 纯 gsub，静态内容免改 Lua）；人工通读改后模板，确认 using 区已含 `System.Func`（`using System;` 第 6 行已有）与 `Cysharp.Threading.Tasks`（第 7 行已有）、方法插入点缩进与所在区块一致。

- [ ] **Step 6: Commit**

```bash
git add FairyGUIProject/plugins/CSharpCodeGen/Template/CompGenTemplate.txt
git commit -m "[AI]feat: Gen组件模板自持生命周期取消源——实现IWinLifecycleAware广播、Dispose释放、UI事件代理新增异步UniTask重载"
```

---

### Task 3: 重生成 7 个 Gen 组件产物

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Game/AutoGen/UI/**/Comp/*.Gen.cs`（7 个：CompPlayerListItem、CompBagItem、CompBagItemInfo、CompBagContent、CompGoodItem、CompTypeItem、CompRedDot）

**Interfaces:**
- Consumes: Task 1 接口（编译期引用）、Task 2 模板
- Produces: 7 个 Gen 组件具备 `OnWinClosed/OnWinOpened` 公开方法与异步代理（Task 4 广播、Task 5 探针依赖）

- [ ] **Step 1: FGUI 编辑器 Publish 重生成（主路径）**

用户在 FairyGUI 编辑器对各启用 genCode 的包执行发布（onPublish 钩子自动生成）。若编辑器不可用，备选：按 Task 2 模板逐文件手动同步 7 个 Gen 产物（内容与模板产出一致，下次 Publish 覆盖无漂移）。

- [ ] **Step 2: 核对产物**

任取一个 Gen 文件确认三处新内容：类声明含 `IWinLifecycleAware`、`m_cancellation` 字段与 `OnWinClosed/OnWinOpened` 实现、6 个异步代理；确认手写 partial（`Game/UI/**/Comp/*.cs`）未被触碰。

- [ ] **Step 3: 编译验证**

Run: refresh_assets → get_compilation_state，Expected: errorCount=0（Gen 引用 Task 1 接口，类型链闭合）

- [ ] **Step 4: Commit**

```bash
git add Unity/Assets/Scripts/Hotfix/Game/AutoGen/UI
git commit -m "[AI]feat: 重生成7个Gen组件——自持取消源+生命周期广播实现+UI事件异步代理"
```

---

### Task 4: WinBase 开关窗口时遍历广播

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/UI/View/WinBase.Life.cs`（`_OnClose`/`_OnOpen` 各加一行广播调用）

**Interfaces:**
- Consumes: Task 1 `IWinLifecycleAware`；`WinUI`（GComponent，GObject.children 可递归）
- Produces: 私有方法 `BroadcastWinLifecycle(bool opened)`——递归遍历 `WinUI` 组件树，对 `is IWinLifecycleAware` 的节点调 `OnWinOpened()` 或 `OnWinClosed()`

- [ ] **Step 1: 实现遍历广播**

私有递归方法（签名如上）：入口守卫 `WinUI == null` 直接返回（对齐 `_OnClose` 的 WinUI 判空口径）；从 `WinUI` 开始深度递归 `children`，命中接口即调用对应方法（父节点命中后仍继续递归子节点——嵌套组件每层独立实现独立命中）。注释写明：与 `EventRegister.Suspend/Resume` 同触发点、嵌套组件逐层独立命中、GList 池中未挂载 item 不在树内不在覆盖范围（其自持取消源创建即有效）。

- [ ] **Step 2: 接入生命周期钩子**

`_OnOpen` 在 `m_cancellation.Recreate()`（WinBase.Life.cs:42）之后加 `BroadcastWinLifecycle(true);`；`_OnClose` 在 `m_cancellation.Cancel()`（WinBase.Life.cs:132）之后加 `BroadcastWinLifecycle(false);`。顺序约束：广播必须晚于窗口自身的取消/重建，保证组件先于业务 OnOpen 拿到新生命周期。

- [ ] **Step 3: 编译验证**

Run: refresh_assets → get_compilation_state，Expected: errorCount=0

- [ ] **Step 4: Commit**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/UI/View/WinBase.Life.cs
git commit -m "[AI]feat: WinBase开关窗口遍历组件树广播IWinLifecycleAware——组件自持取消源随窗口生命周期取消/重建"
```

---

### Task 5: Play 探针实测（含清理）

**Files:**
- Create: `Unity/Assets/Scripts/Hotfix/Probe/FuiCompAsyncEventProbe.cs`（临时，任务内删净）
- Modify: 无

**Interfaces:**
- Consumes: Task 3 产物形态（在探针内定义 mini 测试组件模拟 Gen 结构：GComponent + IWinLifecycleAware + 自持取消源 + FuiEventRegister + 异步代理，不依赖 FGUI 包资源）
- Produces: 四项验证结论（Review Focus 1/2/3/5 的证据）

- [ ] **Step 1: 写探针**

沿用既有套路（`#if UNITY_EDITOR`、`[MenuItem("FuProbe/CompAsyncEventProbe")]`、结果写 `%TEMP%/fui_comp_async_probe.log`、完成标记 `PROBE-DONE`、logMessageReceived 转写 Error 日志）。mini 组件含内嵌一层子组件（钉 Review Focus 4）。验证点：①异步注册+触发正常执行；②OnWinClosed 后在途处理器（Delay 带 `cancellationToken:` 传组件取消源 Token）取消、后续代码不执行、OCE 零泄漏；③OnWinOpened 后同处理器再触发正常（新生命周期）；④Dispose 后取消源退化已取消快照、新触发立即 OCE 静默；⑤未广播组件直接异步注册触发正常（初始态可用）；⑥嵌套子组件随父广播同样取消/重建。

- [ ] **Step 2: 编译 + Play + 触发 + 读结果**

Run: stop_game（如在 Play）→ refresh_assets（errorCount=0）→ play_game → execute_menu_item → 文件轮询 PROBE-DONE。Expected: ①⑤⑥执行标记出现；②取消后无后续标记且 `leaked-oce=0`；③新周期标记出现；④Dispose 后触发无异常日志。

- [ ] **Step 3: 停 Play、删探针（连 .meta）、删结果文件、复查残留**

Run: `git status --porcelain -uall | grep -i probe`，Expected: 空输出；编译回归 errorCount=0。

- [ ] **Step 4: Commit（如有产物）**

探针属临时文件无提交；本任务预期无 git 变更，若 Step 2 暴露实现缺陷则回改 Task 2/4 并按其提交格式补提交。

---

### Task 6: README 同步

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/UI/README.md`（§3.7 组件代理列表 + §4.7 取消契约表述）

**Interfaces:**
- Consumes: Task 2-4 落地后的真实行为
- Produces: 文档与实现一致

- [ ] **Step 1: §3.7 补组件异步代理与广播机制说明**

组件代理方法列表补 6 个异步重载签名；新增一小段：组件自持取消源、`IWinLifecycleAware` 广播触发点（`_OnClose/_OnOpen`）、列表项复用建议（renderer 绑定新数据时可手动调组件 `OnWinOpened()` 取消上一条数据在途任务）。

- [ ] **Step 2: §4.7 取消契约补组件侧行**

补一行：Gen 组件内异步处理器取消时机由组件自持取消源驱动（窗口开关广播），业务 await 点携带 Token 的要求不变。

- [ ] **Step 3: Commit**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/UI/README.md
git commit -m "[AI]docs: UI README补组件层异步事件用法与生命周期广播说明"
```
