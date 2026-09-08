# Config 加载链全链 Task→UniTask + 生成代码完备注释 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 把客户端配置加载链从原生 `Task` 全链转换为 `UniTask`，并在重新生成配置表代码时给 `TableManager`/`TbXxx`/bean `Xxx` 补全中文 XML 注释。

**Architecture:** 改动集中在 **工具版 Luban 模板**（`Config/Tools/Luban/Templates/cs-simple-json` + `cs-bin`）与 **三处框架手写文件**。模板改 `table.sbn`/`tables.sbn`/`bean.sbn`（Task→UniTask + 注释），手写改 `IDataTable.cs`/`BaseDataTable.cs`/`HotfixLauncher.cs`。两者同批交付，使用方跑生成 + 编译，避免单侧类型不一致。

**Tech Stack:** Unity C#（.NET Standard 2.1）、UniTask（`Cysharp.Threading.Tasks`）、Luban Scriban 模板 `.sbn`、HybridCLR。

**Spec:** `Docs/superpowers/specs/2026-09-09-config-loading-task-to-unitask-design.md`

**项目验证约定：** 本仓库无单测框架。每任务末尾的"验证"步骤用 `grep`/`git diff` 断言，不是可执行测试。执行者需配合用户在 Unity 侧重新生成 + 编译。

## Global Constraints

- **禁止原生 `Task`**（代码铁律 #1），一律 `UniTask`；禁止 `Coroutine`（铁律 #2）；禁止运行时反射（铁律 #4）；每异步链有生命周期所有者（铁律 #5）。
- **只改工具版模板** `Config/Tools/Luban/Templates/`，**不改源码版** `Config/luban/src/.../Templates/`（源码版命名空间 `FuFramework.Config.Runtime` 与客户端产物对不上，改它无益且可能破坏）。**无需重新编译 Luban**。
- **只改客户端 Hotfix**，不碰 server（`FuFramework.Config`）、不碰 `cs-l10n-key`、不碰 `cs-dotnet-json`/`cs-dotnet-bin`。
- **注释**：全中文（`Docs/代码风格规范.md` §2.3），XML `///`、`<summary>` 换行、含 `<param>`；含私有成员；保留 Excel 导出的 `comment`（如"道具表"）；无 comment 的 bean 类头也强制加类级 `<summary>`。
- **注释不改变生成语义**：生成代码前后 diff 只能含 `using Cysharp.Threading.Tasks;` + Task→UniTask + 注释行，不得有逻辑改动。
- **手写代码与模板同批交付**（spec §11）：避免单侧 `Func<string, Task<...>>` vs `Func<string, UniTask<...>>` 不一致。
- **提交规范**：`[AI]` 前缀 + Conventional Commits（`Docs/Git提交规范.md`）。

---

### Task 1: cs-simple-json 模板 Task→UniTask（JSON 版 TableManager + 表类）

**Files:**
- Modify: `Config/Tools/Luban/Templates/cs-simple-json/tables.sbn`（TableManager）
- Modify: `Config/Tools/Luban/Templates/cs-simple-json/table.sbn`（表类 TbXxx）

**Interfaces:**
- Consumes: 无（本任务为首个模板改动）
- Produces: 生成代码中 `TableManager.LoadAsync(Func<string, UniTask<JSONNode>>)`、`TbXxx.LoadAsync()` 返回 `UniTask`、`_loadFunc` 字段类型 `Func<UniTask<JSONNode>>`。供 Task 3 的手写代码与 Task 2 的 cs-bin 参照。

> **为何先做模板**：模板是生成代码的源头，先改模板保证后续手写代码（Task 3）与生成契约一致。本仓库无单测，模板改动的"验证"是 grep 断言改写后的模板不再含原生 Task。改动只落在 `tables.sbn`（TableManager 的 LoadAsync）与 `table.sbn`（表类的 _loadFunc/LoadAsync）。

- [ ] **Step 1: 改 `cs-simple-json/tables.sbn` —— LoadAsync 签名与批量等待转 UniTask**

文件路径 `Config/Tools/Luban/Templates/cs-simple-json/tables.sbn`。做 3 处替换：

1. 头部（第 1 行附近）追加：`using Cysharp.Threading.Tasks;`（首行现为 `using System;`，在其下加一行）。
2. 第 34 行：`public async System.Threading.Tasks.Task LoadAsync(System.Func<string, System.Threading.Tasks.Task<JSONNode>> loader)` → `public async UniTask LoadAsync(System.Func<string, UniTask<JSONNode>> loader)`。
3. 第 40 行：`var loadTasks = new System.Collections.Generic.List<System.Threading.Tasks.Task>();` → `var loadTasks = new System.Collections.Generic.List<UniTask>();`。
4. 第 51 行：`await System.Threading.Tasks.Task.WhenAll(loadTasks);` → `await UniTask.WhenAll(loadTasks);`。

- [ ] **Step 2: 验证 `cs-simple-json/tables.sbn` 无原生 Task 残留**

Run:
```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
grep -n "System.Threading.Tasks\|Task.WhenAll\|List<System.Threading.Tasks.Task>" Config/Tools/Luban/Templates/cs-simple-json/tables.sbn
```
Expected: 无任何输出（四个替换点都已改。注意 `using System;` 仍保留，属 `System` 非 `System.Threading.Tasks`，不算残留）。

- [ ] **Step 3: 改 `cs-simple-json/table.sbn` —— 三处 _loadFunc 与 LoadAsync 转 UniTask**

文件路径 `Config/Tools/Luban/Templates/cs-simple-json/table.sbn`。该模板有 map / list / one 三种分支，各含一处分字段与一个 `LoadAsync`：

1. 头部追加 `using Cysharp.Threading.Tasks;`（首行现为 `using Luban;`，在其下加一行）。
2. **全文件**把 `System.Func<System.Threading.Tasks.Task<JSONNode>> _loadFunc` 出现 3 次 → `System.Func<UniTask<JSONNode>> _loadFunc`。
3. **全文件**把 `System.Func<System.Threading.Tasks.Task<JSONNode>> loadFunc`（构造函数参数）出现 3 次 → `System.Func<UniTask<JSONNode>> loadFunc`。
4. **全文件**把 `public override async System.Threading.Tasks.Task LoadAsync()` 出现 3 次 → `public override async UniTask LoadAsync()`。

用全局替换（replace_all）更稳妥。

- [ ] **Step 4: 验证 `cs-simple-json/table.sbn` 无原生 Task 残留**

Run:
```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
grep -n "System.Threading.Tasks" Config/Tools/Luban/Templates/cs-simple-json/table.sbn
```
Expected: 无输出（`using Luban;` 与 `using SimpleJSON;` 保留，不含 `System.Threading.Tasks`）。

- [ ] **Step 5: 验证两个文件已含 UniTask using**

Run:
```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
grep -c "using Cysharp.Threading.Tasks;" Config/Tools/Luban/Templates/cs-simple-json/tables.sbn Config/Tools/Luban/Templates/cs-simple-json/table.sbn
```
Expected: 各自含 1 处 `using Cysharp.Threading.Tasks;`。

- [ ] **Step 6: 提交**

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
git add Config/Tools/Luban/Templates/cs-simple-json/tables.sbn Config/Tools/Luban/Templates/cs-simple-json/table.sbn
git commit -m "[AI]refactor: cs-simple-json 模板 LoadAsync/_loadFunc 由原生 Task 转为 UniTask"
```

---

### Task 2: cs-bin 模板 Task→UniTask（二进制版 TableManager + 表类）

**Files:**
- Modify: `Config/Tools/Luban/Templates/cs-bin/tables.sbn`
- Modify: `Config/Tools/Luban/Templates/cs-bin/table.sbn`

**Interfaces:**
- Consumes: Task 1 的改法（JSON 版已改，参照同名替换）。
- Produces: 生成代码中 `TableManager.LoadAsync(Func<string, UniTask<ByteBuf>>)`、`TbXxx.LoadAsync()` 返回 `UniTask`、`_loadFunc` 类型 `Func<UniTask<ByteBuf>>`。供 Task 3 手写与最终生成。

> cs-bin 的 `bean.sbn` 无 `Task`，本任务不碰；仅 `tables.sbn` + `table.sbn` 有 `Task`。cs-bin 的 `tables.sbn` 含 `PostResolveRef()` 钩子与 `partial void PostResolveRef();`，**保持在原处不动**。

- [ ] **Step 1: 改 `cs-bin/tables.sbn`**

文件路径 `Config/Tools/Luban/Templates/cs-bin/tables.sbn`：
1. 头部追加 `using Cysharp.Threading.Tasks;`（现首行 `using Luban;`）。
2. 第 32 行：`public async System.Threading.Tasks.Task LoadAsync(System.Func<string, System.Threading.Tasks.Task<ByteBuf>> loader)` → `public async UniTask LoadAsync(System.Func<string, UniTask<ByteBuf>> loader)`。
3. 第 40 行：`var loadTasks = new System.Collections.Generic.List<System.Threading.Tasks.Task>();` → `List<UniTask>();`。
4. 第 49 行：`await System.Threading.Tasks.Task.WhenAll(loadTasks);` → `await UniTask.WhenAll(loadTasks);`。
5. **不要动**第 71-77 行的 `ResolveRef`、第 76 行 `PostResolveRef()` 调用、第 86 行 `partial void PostResolveRef();`。

- [ ] **Step 2: 改 `cs-bin/table.sbn`**

文件路径 `Config/Tools/Luban/Templates/cs-bin/table.sbn`：
1. 头部追加 `using Cysharp.Threading.Tasks;`（现首行 `using Luban;`）。
2. **全文件**替换 `System.Func<System.Threading.Tasks.Task<ByteBuf>> _loadFunc`（3 次）→ `System.Func<UniTask<ByteBuf>> _loadFunc`。
3. **全文件**替换 `System.Func<System.Threading.Tasks.Task<ByteBuf>> loadFunc`（构造函数参数，3 次）→ `System.Func<UniTask<ByteBuf>> loadFunc`。
4. **全文件**替换 `public override async System.Threading.Tasks.Task LoadAsync()`（3 次）→ `public override async UniTask LoadAsync()`。
5. **不要动** `PostInit()`/`PostResolveRef()` 调用（63/77/139/157/185/200 行附近）与 `partial void PostInit();`/`partial void PostResolveRef();` 声明。

- [ ] **Step 3: 验证 cs-bin 两个文件**

Run:
```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
grep -n "System.Threading.Tasks" Config/Tools/Luban/Templates/cs-bin/tables.sbn Config/Tools/Luban/Templates/cs-bin/table.sbn
grep -c "using Cysharp.Threading.Tasks;" Config/Tools/Luban/Templates/cs-bin/tables.sbn Config/Tools/Luban/Templates/cs-bin/table.sbn
grep -n "PostResolveRef" Config/Tools/Luban/Templates/cs-bin/tables.sbn Config/Tools/Luban/Templates/cs-bin/table.sbn
```
Expected: 第一条无输出；第二条各含 1；第三条 `PostResolveRef` 仍在（在 tables.sbn 的 76/86 行、table.sbn 的 77/157/200/209 行），未被误删。

- [ ] **Step 4: 提交**

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
git add Config/Tools/Luban/Templates/cs-bin/tables.sbn Config/Tools/Luban/Templates/cs-bin/table.sbn
git commit -m "[AI]refactor: cs-bin 模板 LoadAsync/_loadFunc 由原生 Task 转为 UniTask（PostResolveRef 钩子保留）"
```

---

### Task 3: cs-simple-json 表类模板补全注释

**Files:**
- Modify: `Config/Tools/Luban/Templates/cs-simple-json/table.sbn`（TbXxx 类）

**Interfaces:**
- Consumes: Task 1 已改的 `table.sbn`（UniTask LoadAsync）。
- Produces: 生成的每个 `TbXxx.cs` 类成员带中文 `///` 注释。

> 本任务只加注释、零逻辑改动。映射到生成产物：`TbItem`/`TbAchievement`/`TbSound` 等 11 个表的 `_loadFunc` 字段、构造函数、`LoadAsync`、`ResolveRef`、`TranslateText`、`PostInit`。

- [ ] **Step 1: 给 `table.sbn` 的 `_loadFunc` 字段加注释**

在 `private readonly System.Func<UniTask<JSONNode>> _loadFunc;` 前加：
```sbn
    /// <summary>
    /// 取配置表 JSON 的加载委托（由 TableManager 注入，参数为表文件路径）
    /// </summary>
```
> 用 `replace_all: true` 替换所有 3 处。

- [ ] **Step 2: 给构造函数加注释（含 loadFunc 参数）**

在 `public {{__name}}(System.Func<UniTask<JSONNode>> loadFunc)` 前加：
```sbn
    /// <summary>
    /// 创建表实例
    /// </summary>
    /// <param name="loadFunc">取配置表 JSON 的加载委托</param>
```
> 3 处（map / list / one 分支）。list 分支构造函数体还含 `_dataMapXxx` 初始化，注释位置保持在该方法前。

- [ ] **Step 3: 给 `LoadAsync` 加注释（含 returns）**

在 `public override async UniTask LoadAsync()` 前加：
```sbn
    /// <summary>
    /// 异步加载本表数据
    /// </summary>
    /// <remarks>从 loader 取 JSON、清空字典、逐行反序列化，完成后调用 PostInit</remarks>
```
> 3 处。两分支 `LoadAsync` 内部结构略异（map 分支有 LongKeyDataDict.Add、list/one 不同），注释描述用统一"异步加载本表数据"，`<remarks>` 描述流程即可，不必逐分支写死。

- [ ] **Step 4: 给 `ResolveRef` 加注释（含 tables 参数）**

在 `public void ResolveRef(TableManager tables)` 前加：
```sbn
    /// <summary>
    /// 解析本表内所有跨表引用（把引用的 id 换成实际对象）
    /// </summary>
    /// <param name="tables">表管理器，用于跨表查找</param>
```

- [ ] **Step 5: 给 `TranslateText` 加注释（含 translator 参数）**

在 `public void TranslateText(System.Func<string, string, string> translator)` 前加：
```sbn
    /// <summary>
    /// 用指定翻译器刷新本表所有多语言字段
    /// </summary>
    /// <param name="translator">翻译委托：传入 (key, original) 返回译文</param>
```

- [ ] **Step 6: 给 `PostInit` partial 声明加注释（含典型场景）**

在 `partial void PostInit();` 前加：
```sbn
    /// <summary>
    /// 分部初始化钩子，由使用方补实现。
    /// 典型场景：给某张表预计算、排序、建字典索引等"只靠本表数据就能完成"的收尾工作
    /// </summary>
```

- [ ] **Step 7: 验证注释已注入**

Run:
```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
grep -c "<summary>" Config/Tools/Luban/Templates/cs-simple-json/table.sbn
grep -c "典型场景：给某张表预计算" Config/Tools/Luban/Templates/cs-simple-json/table.sbn
```
Expected: 第一条 `<summary>` 计数 ≥5（构造函数/LoadAsync/ResolveRef/TranslateText/PostInit 各至少1）；第二条 ≥1（PostInit 典型场景）。

- [ ] **Step 8: 提交**

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
git add Config/Tools/Luban/Templates/cs-simple-json/table.sbn
git commit -m "[AI]docs: cs-simple-json 表类模板补全中文注释（构造函数/LoadAsync/ResolveRef/TranslateText/PostInit）"
```

---

### Task 4: cs-bin 表类模板补全注释

**Files:**
- Modify: `Config/Tools/Luban/Templates/cs-bin/table.sbn`

**Interfaces:**
- Consumes: Task 2 已改的 cs-bin `table.sbn`（UniTask LoadAsync），Task 3 的注释写法。
- Produces: 生成的二进制表类带中文注释。

- [ ] **Step 1: 复用 Task 3 的注释，替换为 ByteBuf 语义**

将 Task 3 的 6 处注释（`_loadFunc`/构造函数/`LoadAsync`/`ResolveRef`/`TranslateText`/`PostInit`）同样插入 `cs-bin/table.sbn`，唯一的差异是 JSON 措辞改为 binary：
- `_loadFunc`：`取配置表 ByteBuf 的加载委托（由 TableManager 注入，参数为表文件路径）`
- `LoadAsync` 的 `<remarks>`：`从 loader 取 ByteBuf、清空字典、逐行反序列化，完成后调用 PostInit`

其余标题/参数/PostInit 典型场景与 Task 3 逐字一致。

- [ ] **Step 2: 验证注释已注入**

Run:
```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
grep -c "<summary>" Config/Tools/Luban/Templates/cs-bin/table.sbn
grep -c "典型场景：给某张表预计算" Config/Tools/Luban/Templates/cs-bin/table.sbn
grep -c "PostResolveRef" Config/Tools/Luban/Templates/cs-bin/table.sbn
```
Expected: 第一条 ≥5；第二条 ≥1；第三条同 Task 2（PostResolveRef 仍在，未被注释破坏）。

- [ ] **Step 3: 提交**

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
git add Config/Tools/Luban/Templates/cs-bin/table.sbn
git commit -m "[AI]docs: cs-bin 表类模板补全中文注释（构造函数/LoadAsync/ResolveRef/TranslateText/PostInit）"
```

---

### Task 5: cs-simple-json TableManager 模板补全注释

**Files:**
- Modify: `Config/Tools/Luban/Templates/cs-simple-json/tables.sbn`

**Interfaces:**
- Consumes: Task 1 已改的 `tables.sbn`。
- Produces: 生成的 `TableManager.cs` 各方法带注释。

- [ ] **Step 1: 给 `Init` 加注释**

在 `public void Init(ConfigModule configModule)` 前：
```sbn
    /// <summary>
    /// 初始化表管理器（关联配置模块并清空已有配置）
    /// </summary>
    /// <param name="configModule">配置管理模块</param>
```

- [ ] **Step 2: 给 `LoadAsync` 加注释（含 loader 参数、returns）**

在 `public async UniTask LoadAsync(System.Func<string, UniTask<JSONNode>> loader)` 前：
```sbn
    /// <summary>
    /// 异步加载全部配置表
    /// </summary>
    /// <param name="loader">取配置表 JSON 的加载委托</param>
    /// <returns>全部表加载完成</returns>
    /// <remarks>逐表建立、并行 await，全部完成后刷新引用</remarks>
```

- [ ] **Step 3: 给 `SetTranslateText` 加注释（含 translator 参数）**

在 `public void SetTranslateText(System.Func<string, string, string> translator)` 前：
```sbn
    /// <summary>
    /// 为所有配置表设置本地化翻译器
    /// </summary>
    /// <param name="translator">翻译委托：传入 (key, original) 返回译文</param>
```

- [ ] **Step 4: 给 `ResolveRef` 加注释**

在 `private void ResolveRef()` 前：
```sbn
    /// <summary>
    /// 解析所有配置表的跨表引用（表之间已加载完毕）
    /// </summary>
```

- [ ] **Step 5: 给 `Refresh` 加注释**

在 `public void Refresh()` 前：
```sbn
    /// <summary>
    /// 刷新所有配置表（先 PostInit 再做跨表引用解析）
    /// </summary>
```

- [ ] **Step 6: 给 `PostInit` partial 声明加注释（含典型场景）**

在 `partial void PostInit();` 前：
```sbn
    /// <summary>
    /// 分部初始化钩子，由使用方补实现。
    /// 典型场景：给某张表预计算、排序、建字典索引等"只靠本表数据就能完成"的收尾工作
    /// </summary>
```

- [ ] **Step 7: 验证**

Run:
```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
grep -c "<summary>" Config/Tools/Luban/Templates/cs-simple-json/tables.sbn
grep -c "典型场景：给某张表预计算" Config/Tools/Luban/Templates/cs-simple-json/tables.sbn
```
Expected: 第一条 ≥5；第二条 ≥1。

- [ ] **Step 8: 提交**

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
git add Config/Tools/Luban/Templates/cs-simple-json/tables.sbn
git commit -m "[AI]docs: cs-simple-json TableManager 模板补全中文注释（Init/LoadAsync/SetTranslateText/ResolveRef/Refresh/PostInit）"
```

---

### Task 6: cs-bin TableManager 模板补全注释

**Files:**
- Modify: `Config/Tools/Luban/Templates/cs-bin/tables.sbn`

**Interfaces:**
- Consumes: Task 2 已改的 cs-bin `tables.sbn`，Task 5 的注释写法。
- Produces: 生成的二进制 `TableManager.cs` 带注释。

- [ ] **Step 1: 复用 Task 5 的注释，JSON 措辞改 ByteBuf**

Task 5 的 6 处注释（Init/LoadAsync/SetTranslateText/ResolveRef/Refresh/PostInit）插入 `cs-bin/tables.sbn`，仅 `LoadAsync` 的 `<param name="loader">` 措辞改为 `取配置表 ByteBuf 的加载委托`。其余与 Task 5 逐字一致。注意：cs-bin 还有 `PostResolveRef()` 调用（在 `Refresh` 内的 `ResolveRef` 里，非独立方法），保持不动；`PostInit` 的典型场景注释与 Task 5 相同。

- [ ] **Step 2: 验证**

Run:
```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
grep -c "<summary>" Config/Tools/Luban/Templates/cs-bin/tables.sbn
grep -c "典型场景：给某张表预计算" Config/Tools/Luban/Templates/cs-bin/tables.sbn
grep -c "PostResolveRef" Config/Tools/Luban/Templates/cs-bin/tables.sbn
```
Expected: 第一条 ≥5；第二条 ≥1；第三条同 Task 2（仍在）。

- [ ] **Step 3: 提交**

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
git add Config/Tools/Luban/Templates/cs-bin/tables.sbn
git commit -m "[AI]docs: cs-bin TableManager 模板补全中文注释（Init/LoadAsync/SetTranslateText/ResolveRef/Refresh/PostInit）"
```

---

### Task 7: bean.sbn（JSON + 二进制）补全注释

**Files:**
- Modify: `Config/Tools/Luban/Templates/cs-simple-json/bean.sbn`
- Modify: `Config/Tools/Luban/Templates/cs-bin/bean.sbn`

**Interfaces:**
- Consumes: 无（bean.sbn 无 Task，纯加注释）。
- Produces: 生成的 bean 数据类（`Item`/`Achievement`/`Localization` 等）带注释，类头无 comment 时也强制加类级注释。

> bean.sbn 生成具体数据类。两份（JSON/二进帛）结构相似但 deserialize 不同（JSONNode vs ByteBuf），注释措辞需各自贴合。类头 comment 保留；无 comment 时强制加模板写死类级 `<summary>`。

- [ ] **Step 1: 类头强制类级注释（处理无 comment 分支）**

`bean.sbn` 现有类头逻辑（`cs-simple-json` 第 9-13 行）：
```sbn
    {{~if __bean.comment != '' ~}}
    /// <summary>
    /// {{escape_comment __bean.comment}}
    /// </summary>
    {{~end~}}
```
将其改为**无论有无 comment 都生成类级注释**——无 comment 时用模板写死中文（以 bean 表名兜底）：
```sbn
    {{~if __bean.comment != '' ~}}
    /// <summary>
    /// {{escape_comment __bean.comment}}
    /// </summary>
    {{~else~}}
    /// <summary>
    /// {{__name}} 数据类
    /// </summary>
    {{~end~}}
```
`cs-bin/bean.sbn` 同样处理（其头部结构一致）。

- [ ] **Step 2: 给两个构造函数加注释**

参数形式 `public {{__name}}({{get_param_def_list(__bean)}})` 前：
```sbn
    /// <summary>
    /// 创建 {{__name}} 实例
    /// </summary>
```
JSON 版 `public {{__name}}(JSONNode _buf)` 前加：
```sbn
    /// <summary>
    /// 从 JSONNode 反序列化创建 {{__name}} 实例
    /// </summary>
    /// <param name="_buf">配置数据节点</param>
```
二进制版 `public {{__name}}(ByteBuf _buf)` 前加（把 `JSONNode` 改 `ByteBuf`、`<param>` 文本改 `配置数据流`）。

- [ ] **Step 3: 给 `Deserialize{{__name}}` 加注释**

`public static {{__name}} Deserialize{{__name}}(JSONNode _buf)` 前：
```sbn
    /// <summary>
    /// 反序列化创建 {{__name}} 实例
    /// </summary>
    /// <param name="_buf">配置数据节点</param>
    /// <returns>反序列化后的 {{__name}}</returns>
```
二进制版同（措辞改 ByteBuf/配置数据流）。

- [ ] **Step 4: 给 `ResolveRef` 加注释（含 tables 参数）**

`public {{method_modifier __bean}} void ResolveRef({{__manager_name}} tables)` 前：
```sbn
    /// <summary>
    /// 解析本 bean 的跨表引用（引用的 id 换成实际对象）
    /// </summary>
    /// <param name="tables">表管理器，用于跨表查找</param>
```

- [ ] **Step 5: 给 `TranslateText` 加注释（含 translator 参数）**

`public void TranslateText(System.Func<string, string, string> translator)` 前：
```sbn
    /// <summary>
    /// 用指定翻译器刷新本 bean 的多语言字段
    /// </summary>
    /// <param name="translator">翻译委托：传入 (key, original) 返回译文</param>
```

- [ ] **Step 6: 给 `ToString` 加注释**

`public override string ToString()` 前：
```sbn
    /// <summary>
    /// 返回本对象的字段字符串表示
    /// </summary>
```

- [ ] **Step 7: 给 `GetTypeId`/`__ID__` 加注释**

`public override int GetTypeId() => __ID__;` 前加：
```sbn
    /// <summary>
    /// 获取 bean 类型标识
    /// </summary>
```
`public const int __ID__ = {{__bean.id}};` 前加：
```sbn
    /// <summary>
    /// 本 bean 的类型标识常量
    /// </summary>
```

- [ ] **Step 8: 给 `PostInit` partial 声明加注释（含典型场景）**

`partial void PostInit();` 前：
```sbn
    /// <summary>
    /// 分部初始化钩子，由使用方补实现。
    /// 典型场景：给某张表预计算、排序、建字典索引等"只靠本表数据就能完成"的收尾工作
    /// </summary>
```

- [ ] **Step 9: 验证两份 bean.sbn**

Run:
```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
grep -c "<summary>" Config/Tools/Luban/Templates/cs-simple-json/bean.sbn Config/Tools/Luban/Templates/cs-bin/bean.sbn
grep -c "__name}} 数据类" Config/Tools/Luban/Templates/cs-simple-json/bean.sbn Config/Tools/Luban/Templates/cs-bin/bean.sbn
grep -c "典型场景：给某张表预计算" Config/Tools/Luban/Templates/cs-simple-json/bean.sbn Config/Tools/Luban/Templates/cs-bin/bean.sbn
```
Expected: 第一条各自 ≥6；第二条 ≥1（无 comment 兜底类注释）；第三条 ≥1。

- [ ] **Step 10: 提交**

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
git add Config/Tools/Luban/Templates/cs-simple-json/bean.sbn Config/Tools/Luban/Templates/cs-bin/bean.sbn
git commit -m "[AI]docs: bean 模板补全中文注释（构造函数/Deserialize/ResolveRef/TranslateText/ToString/GetTypeId/PostInit，类头无comment也强制类注释）"
```

---

### Task 8: 改手写代码 Task→UniTask

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Config/IDataTable.cs:3,18`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Config/BaseDataTable.cs:3,35`
- Modify: `Unity/Assets/Scripts/Hotfix/HotfixLauncher.cs:3,213,236`

**Interfaces:**
- Consumes: Task 1-7 已将模板改为 UniTask 契约（重生成后 `TableManager.LoadAsync` / `TbXxx.LoadAsync` / `_loadFunc` 均为 UniTask）。
- Produces: `IDataTable.LoadAsync()` / `BaseDataTable<T>.LoadAsync()` / `ConfigLoader` / `ConfigBufferLoader` 返回 `UniTask`；移除 `using System.Threading.Tasks`。

- [ ] **Step 1: 改 `IDataTable.cs`**

行 3：`using System.Threading.Tasks;` → `using Cysharp.Threading.Tasks;`（`.cs` 顶部）。
行 18：`Task LoadAsync();` → `UniTask LoadAsync();`。

- [ ] **Step 2: 验证 `IDataTable.cs`**

Run:
```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
grep -n "System.Threading.Tasks\|Task LoadAsync" Unity/Assets/Scripts/Hotfix/Framework/Config/IDataTable.cs
grep -c "using Cysharp.Threading.Tasks;" Unity/Assets/Scripts/Hotfix/Framework/Config/IDataTable.cs
```
Expected: 第一条无输出；第二条 ≥1。

- [ ] **Step 3: 改 `BaseDataTable.cs`**

行 3：`using System.Threading.Tasks;` → `using Cysharp.Threading.Tasks;`。
行 35：`public abstract Task LoadAsync();` → `public abstract UniTask LoadAsync();`。

- [ ] **Step 4: 验证 `BaseDataTable.cs`**

Run:
```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
grep -n "System.Threading.Tasks\|abstract Task LoadAsync" Unity/Assets/Scripts/Hotfix/Framework/Config/BaseDataTable.cs
grep -c "using Cysharp.Threading.Tasks;" Unity/Assets/Scripts/Hotfix/Framework/Config/BaseDataTable.cs
```
Expected: 第一条无输出；第二条 ≥1。

- [ ] **Step 5: 改 `HotfixLauncher.cs`**

- 行 3：`using System.Threading.Tasks;` → `using Cysharp.Threading.Tasks;`（注意本文件已同时有 `using System.Threading.Tasks;` 和 `using Cysharp.Threading.Tasks;`，**删除 `System.Threading.Tasks` 行**，保留已有的 `Cysharp.Threading.Tasks`，不重复添加）。
- 行 213：`private static async Task<ByteBuf> ConfigBufferLoader(string file)` → `private static async UniTask<ByteBuf> ConfigBufferLoader(string file)`。
- 行 236：`private static async Task<JSONNode> ConfigLoader(string file)` → `private static async UniTask<JSONNode> ConfigLoader(string file)`。

> 方法体内 `await GlobalModule.AssetModule.LoadAssetAsync<TextAsset>(...)` 已是 UniTask，无需改动。调用处 `await tableManager.LoadAsync(...)` 委托类型随模板重生成后自然兼容，无需改源码。

- [ ] **Step 6: 验证 `HotfixLauncher.cs`**

Run:
```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
grep -n "System.Threading.Tasks\|Task<ByteBuf>\|Task<JSONNode>\|async Task " Unity/Assets/Scripts/Hotfix/HotfixLauncher.cs
```
Expected: 无输出（已删 using、两个 loader 已改 UniTask；其余方法均已是 UniTask）。

- [ ] **Step 7: 提交**

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
git add Unity/Assets/Scripts/Hotfix/Framework/Config/IDataTable.cs Unity/Assets/Scripts/Hotfix/Framework/Config/BaseDataTable.cs Unity/Assets/Scripts/Hotfix/HotfixLauncher.cs
git commit -m "[AI]refactor: IDataTable/BaseDataTable/HotfixLauncher LoadAsync 与 ConfigLoader 由原生 Task 转为 UniTask"
```

---

### Task 9: 全局残留复核

**Files:** 无新增/修改。

**Interfaces:**
- Consumes: Task 1-8 全部产出。

- [ ] **Step 1: 复核模板无原生 Task**

Run:
```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
grep -rn "System.Threading.Tasks" Config/Tools/Luban/Templates/cs-simple-json Config/Tools/Luban/Templates/cs-bin
```
Expected: 无输出。

- [ ] **Step 2: 复核手写代码无 System.Threading.Tasks using**

Run:
```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
grep -rn "System.Threading.Tasks" Unity/Assets/Scripts/Hotfix/Framework/Config Unity/Assets/Scripts/Hotfix/HotfixLauncher.cs
```
Expected: 无输出。

- [ ] **Step 3: 澄清交付边界**

本任务的**生成产物**（`AutoGen/Tables/Generate/**`）与 **AOT 引用**（`AOTGenericReferences.cs`）**不在此改动内**，由使用方执行 §9 手动步骤后产生。执行者需在交付说明中明确：重新生成时若把模板产物提交进 git，属于使用方行为。

---

### Task 10: 交付与交接说明

**Files:** 无新增/修改（仅说明）。

**Interfaces:**
- Consumes: Task 1-9。

- [ ] **Step 1: 输出使用方手动步骤清单**

在交付说明中列出（来自 spec §9）：
1. 重生成：`cd Config && dotnet ./Tools/Luban/Luban.dll -t client -d json -c cs-simple-json -c cs-l10n-key -x ... --conf ./Luban.conf`，以及 `-d bin -c cs-bin` 对应命令（详见 spec §9）。
2. HybridCLR `GenAOTGenericReferences` 重新生成 AOT 引用。
3. Unity 编译。
4. Play 冒烟：`LoadConfigAsync` 成功、`ConfigModuleWindow` 表数正确。

- [ ] **Step 2: 提示两个风险**

- **AOT 引用**：若不重生成 `AOTGenericReferences.cs`，HybridCLR 热更产物可能缺新 state machine 泛型。
- **源码版漂移**：源码版模板保持旧版（`FuFramework.Config.Runtime` 命名空间），若将来有人重编 Luban 源码覆盖工具版，会引入命名空间回退。

---

## Self-Review

### Spec coverage

| 规格要求 | 对应任务 |
|---|---|
| cs-simple-json 模板 Task→UniTask（§5.1/§5.2） | Task 1 |
| cs-bin 模板 Task→UniTask（§5.3/§5.4，含 PostResolveRef 保留） | Task 2 |
| cs-simple-json 表类注释（§5.2） | Task 3 |
| cs-bin 表类注释（§5.4） | Task 4 |
| cs-simple-json TableManager 注释（§5.1） | Task 5 |
| cs-bin TableManager 注释（§5.3） | Task 6 |
| bean 注释 + 类头无 comment 强制类注释（§5.5/§5.6） | Task 7 |
| IDataTable/BaseDataTable/HotfixLauncher Task→UniTask（§6） | Task 8 |
| 全局残留复核、排除项确认（§4） | Task 9 |
| 使用方手动步骤 + 风险提示（§9/§11） | Task 10 |

### Placeholder scan

所有模板注释步骤含具体 `.sbn` 代码块；手写代码步骤含精确行号与替换文本；无 "TBD"/"implement later"/"fill in details" 类占位。Task 3/4/5/6 的注释措辞逐字给出。✓

### Type consistency

- `UniTask` / `Func<string, UniTask<JSONNode>>` / `Func<string, UniTask<ByteBuf>>` 贯穿 Task 1-8 一致。
- `_loadFunc`（Task 3 注释）、`Create`/`Deserialize`（Task 7）、`LoadAsync` 返回 `UniTask`（Task 8 契约）一致。
- `PostInit` 典型场景文案（"给某张表预计算、排序、建字典索引等"只靠本表数据就能完成"的收尾工作"）在 Task 3/4/5/6/7 逐字统一。✓

### 已知限制

- 无单测框架，"验证"用 grep 断言模板/手写代码而非可执行测试——符合 spec §10 的验证约定。
- 生成产物与 AOT 不在本计划内改动，属使用方行动（Task 9/10 已明确边界）。
