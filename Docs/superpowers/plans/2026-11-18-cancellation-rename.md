# 取消类重命名实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 `LifecycleCancellationSource` 重命名为 `CancellationTokenIssuer`、`CancellationScope` 重命名为 `CancellationBarrier`，全仓调用点、注释、模板、文档同步，编译零错误、旧名残留清零（历史存档除外）。

**Architecture:** 纯重命名重构，无行为变更。按依赖域分 6 个任务：类文件本体 → Core 域 → Framework 其余模块域 → Game 域（含 Gen 文件）→ README 文档 → 终验。批量替换用 PowerShell `-creplace`（大小写敏感）脚本，替换后立即编译验证。

**Tech Stack:** C# / Unity / UniTask / unity-cli（TCP 编译验证）/ PowerShell 批量替换

**Spec:** `Docs/superpowers/specs/2026-11-18-cancellation-rename-design.md`

## Global Constraints

- 仅改类名与注释提及处；**成员名不动**：`ICancelAsync` 接口名、`Token`/`Cancel()`/`Recreate()`/`CancelAsync()`/`Begin()`/`BeginScope` 全部保留原样（spec 裁决项）。
- 精确替换对（大小写敏感）：
  - `LifecycleCancellationSource` → `CancellationTokenIssuer`
  - `CancellationScope` → `CancellationBarrier`
- 替换用 PowerShell `-creplace`（**禁用 `-replace`**，大小写不敏感有吞名前科——memory: powershell-batch-rename-hazard）。
- 文件重命名必须 `git mv` 连带 `.meta`，保 GUID 与历史。
- 编译验证命令：`unity-cli refresh_assets` → 轮询 `unity-cli get_compilation_state` 直至编译完成且无错误。
- 提交纪律：**计划内不做任何 git add/commit**；全部任务完成、用户验证后，征得同意统一整理提交（Docs/Git提交规范.md）。
- Tab 缩进，遵守 Docs/代码风格规范.md；Edit 工具勿用于空白转换。

## Review Focus

重命名最容易咬人的五类失败模式，及钉住它们的验证（分布于各任务的步骤中）：

1. **PowerShell 替换吞名**：`-replace` 大小写不敏感会命中不该命中的变体 → 每个批量替换脚本必须用 `-creplace`，替换后 grep 验证旧名清零（Task 1-4 各自的验证步骤）。
2. **Gen 文件与插件模板脱节**：Gen 手改了但 `CompGenTemplate.txt` 没改（或反之），未来 FGUI 重新生成即回退旧名 → Task 4 步骤中模板与 Gen 必须同任务内一起改并 grep 互验。
3. **两类互相引用的注释漏改**：`CancellationTokenIssuer` 注释提及 `CancellationScope`、`CancellationBarrier` 注释提及 `LifecycleCancellationSource`，改类名时注释里的对方名字极易漏 → Task 1 验证步骤专门 grep 两个文件内的旧名。
4. **README 中文件路径引用漏改**：README 可能引用 `Core/Async/LifecycleCancellationSource.cs` 这类文件路径 → Task 5 全文 grep 路径形态（含 `.cs` 后缀的引用）。
5. **`.meta` 未连带重命名** → Unity 重新生成 meta、GUID 变化、场景/预制体引用丢失 → Task 1 用 `git mv` 两文件各自 `.meta` 一并移动，`git status` 验证四个文件（2 .cs + 2 .meta）均为 Rename 状态。

---

### Task 1: 重命名两个类文件本体

**Files:**
- Rename: `Unity/Assets/Scripts/Hotfix/Framework/Core/Async/LifecycleCancellationSource.cs` (+`.meta`) → `CancellationTokenIssuer.cs`
- Rename: `Unity/Assets/Scripts/Hotfix/Framework/Core/Async/CancellationScope.cs` (+`.meta`) → `CancellationBarrier.cs`

**Interfaces:**
- Produces: 类 `CancellationTokenIssuer`（成员不变：`Token`/`Cancel()`/`Recreate()`/`Dispose()`），类 `CancellationBarrier : ICancelAsync`（成员不变：`Token`/`Cancel()`/`CancelAsync()`/`Begin()`/嵌套 struct `BeginScope`），接口 `ICancelAsync`（不动）。后续所有任务按这三个名字引用。

- [ ] **Step 1: git mv 四个文件（2 .cs + 2 .meta）**

```powershell
git mv "Unity/Assets/Scripts/Hotfix/Framework/Core/Async/LifecycleCancellationSource.cs" "Unity/Assets/Scripts/Hotfix/Framework/Core/Async/CancellationTokenIssuer.cs"
git mv "Unity/Assets/Scripts/Hotfix/Framework/Core/Async/LifecycleCancellationSource.cs.meta" "Unity/Assets/Scripts/Hotfix/Framework/Core/Async/CancellationTokenIssuer.cs.meta"
git mv "Unity/Assets/Scripts/Hotfix/Framework/Core/Async/CancellationScope.cs" "Unity/Assets/Scripts/Hotfix/Framework/Core/Async/CancellationBarrier.cs"
git mv "Unity/Assets/Scripts/Hotfix/Framework/Core/Async/CancellationScope.cs.meta" "Unity/Assets/Scripts/Hotfix/Framework/Core/Async/CancellationBarrier.cs.meta"
```

- [ ] **Step 2: 验证 git 状态为 Rename**

Run: `git status --short -- "Unity/Assets/Scripts/Hotfix/Framework/Core/Async/"`
Expected: 四行均为 `R` 状态（旧名 → 新名），无 D+?? 组合。

- [ ] **Step 3: 替换两个文件内的类名与注释**

两个文件内所有 `LifecycleCancellationSource` → `CancellationTokenIssuer`、`CancellationScope` → `CancellationBarrier`（含类声明、嵌套 struct `BeginScope` 的注释中 owner 类型、XML 注释中互相提及的对方类名）。注意：注释中提到"与 CancellationScope 的区别""与 LifecycleCancellationSource 怎么选"这类对方名字必须一起换。

- [ ] **Step 4: grep 验证两文件旧名清零**

Run: 检索两个新文件内容中 `LifecycleCancellationSource|CancellationScope`
Expected: 0 处命中。若命中即注释漏改。

- [ ] **Step 5: 编译验证**

Run: `unity-cli refresh_assets`，然后轮询 `unity-cli get_compilation_state` 直至完成
Expected: 编译错误数 0。（此时全仓其余调用点仍引用旧类名，**预期编译失败**——若 Unity 因外域报错未能通过，属正常，跳过本步编译判定，以 Task 6 终验为准；但本步必须执行 refresh 让 Unity 识别文件改名与新类名。）

---

### Task 2: Core 域调用点 + CLAUDE.md + FGUI 插件模板

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Core/ModuleManager.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Core/GameDriven.cs`
- Modify: `CLAUDE.md`（铁律第 5 条）
- Modify: `FairyGUIProject/plugins/CSharpCodeGen/Template/CompGenTemplate.txt`

**Interfaces:**
- Consumes: Task 1 的 `CancellationTokenIssuer` / `CancellationBarrier` 新类名。

- [ ] **Step 1: 批量替换四个文件**

```powershell
$files = @(
  "Unity/Assets/Scripts/Hotfix/Framework/Core/ModuleManager.cs",
  "Unity/Assets/Scripts/Hotfix/Framework/Core/GameDriven.cs"
)
foreach ($f in $files) {
  $c = Get-Content $f -Raw -Encoding UTF8
  $c = $c -creplace "LifecycleCancellationSource", "CancellationTokenIssuer"
  $c = $c -creplace "CancellationScope", "CancellationBarrier"
  Set-Content $f $c -Encoding UTF8 -NoNewline
}
```

CLAUDE.md 与 `CompGenTemplate.txt` 同法替换（模板中 `LifecycleCancellationSource m_cancellation = new()` 与其行注释）。

- [ ] **Step 2: grep 验证四文件旧名清零**

Run: 检索四文件 `LifecycleCancellationSource|CancellationScope`
Expected: 0 命中。

- [ ] **Step 3: 编译观察（不判定通过）**

Run: `unity-cli refresh_assets`
Expected: Framework 内仍有多处旧名未改（Task 3 范围），编译此时预期仍有错误；只确认替换后文件被 Unity 正常识别，无新增语法错误（read_console 查该两文件路径相关报错）。

---

### Task 3: Framework 其余模块调用点

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/UI/View/WinBase.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/UI/Fui/CustomLoader.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/UI/UIModule.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/UI/UIModule.Blur.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Scene/SceneModule.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Sound/SoundModule.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Entity/EntityModule.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Entity/EntityModule.API.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Asset/AssetModule.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Asset/AssetLoadRegister.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Web/WebModule.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Web/WebModule.API.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Download/DownloadModule.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Guide/GuideModule.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Guide/GuideModule.API.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Guide/Misc/GuideActionImpl.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Core/Utility/Utility.Time.AntiCheating.cs`

**Interfaces:**
- Consumes: Task 1 的 `CancellationTokenIssuer` / `CancellationBarrier` 新类名。

- [ ] **Step 1: 批量替换 17 个文件**

同 Task 2 Step 1 的 PowerShell 模式，文件清单换成本任务的 17 个（含 Game 层之外的全部 Framework 文件）。同样 `-creplace`。

- [ ] **Step 2: grep 验证 Framework 目录旧名清零**

Run: 在 `Unity/Assets/Scripts/Hotfix/Framework/` 全目录检索 `LifecycleCancellationSource|CancellationScope`
Expected: 0 命中（README.md 归 Task 5，此处 .cs 应清零；若 README 命中可暂留）。

- [ ] **Step 3: 编译观察**

Run: `unity-cli refresh_assets` + `unity-cli get_compilation_state`
Expected: Game 层 6 个 Gen 文件仍未改（Task 4 范围），编译错误应只剩 Game 域引用旧类名的报错；Framework 域自身零报错。

---

### Task 4: Game 域调用点 + Gen 文件与模板互验

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Game/Network/ReconnectManager.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Game/Network/GameSessionFlow.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Game/AutoGen/UI/Login/Comp/CompPlayerListItem.Gen.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Game/AutoGen/UI/Common/Comp/CompRedDot.Gen.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Game/AutoGen/UI/Bag/Comp/CompBagItem.Gen.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Game/AutoGen/UI/Bag/Comp/CompBagItemInfo.Gen.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Game/AutoGen/UI/Bag/Comp/CompTypeItem.Gen.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Game/AutoGen/UI/Bag/Comp/CompBagContent.Gen.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Game/AutoGen/UI/Bag/Comp/CompGoodItem.Gen.cs`

**Interfaces:**
- Consumes: Task 2 已同步的 `CompGenTemplate.txt`（模板侧新名）；Task 1 新类名。

- [ ] **Step 1: 批量替换 9 个文件**

同前 PowerShell 模式，`-creplace` 替换两个精确串。

- [ ] **Step 2: 模板与 Gen 互验**

Run: 检索 `CompGenTemplate.txt` 与 6 个 `.Gen.cs` 中 `CancellationTokenIssuer`
Expected: 模板 1 处以上、每个 Gen 文件 1 处（`private readonly CancellationTokenIssuer m_cancellation = new();`），全部命中；旧名 0 命中。

- [ ] **Step 3: 编译验证（本任务域内应全绿）**

Run: `unity-cli refresh_assets` → 轮询 `unity-cli get_compilation_state`
Expected: **编译错误数 0**——至此全部代码域已改完，这是本次重命名的主验证点。read_console 复核无 Warning 级残留引用。

---

### Task 5: README 文档同步

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Core/README.md`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/UI/README.md`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Scene/README.md`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Sound/README.md`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Entity/README.md`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Asset/README.md`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Web/README.md`

**Interfaces:**
- Consumes: Task 1-4 已定的新类名。

- [ ] **Step 1: 批量替换 7 个 README**

同前 PowerShell 模式 `-creplace` 替换两个精确串；README 中若出现文件路径形态（`LifecycleCancellationSource.cs`）会随类名一并变为 `CancellationTokenIssuer.cs`，属预期——但需逐一确认路径段落语义仍通顺（若原句是"见 `Core/Async/CancellationScope.cs`"这类文件指引，核对新文件名真实存在）。

- [ ] **Step 2: grep 验证 README 旧名清零**

Run: 在 7 个 README 检索 `LifecycleCancellationSource|CancellationScope`
Expected: 0 命中。

---

### Task 6: 全仓终验

**Files:**
- Verify only（无文件修改；若发现残留则就地修复后重验）

- [ ] **Step 1: 全仓 grep 残留检查**

Run: 在 `D:/_WorkSpace/Unity/FuFramework2.0` 全仓检索 `LifecycleCancellationSource|CancellationScope`
Expected: 仅 `Docs/superpowers/` 历史存档（specs/plans）命中；`Unity/`、`FairyGUIProject/`、`CLAUDE.md`、`Server/` 零命中。若 `Unity/` 下有残留（含隐藏漏网文件），就地替换后重跑编译。

- [ ] **Step 2: 最终编译 + 帧验证**

Run: `unity-cli refresh_assets` → `unity-cli get_compilation_state` → `unity-cli read_console` 查 error
Expected: 编译错误 0，console 无新增 error。

- [ ] **Step 3: 语义抽查**

Run: 打开 `CancellationTokenIssuer.cs` 与 `CancellationBarrier.cs` 通读
Expected: ① 两类 XML 注释中互相提及的对方名字已是新名且句子通顺；② `ICancelAsync`/`BeginScope`/`Recreate` 等成员名原样未动；③ 注释中"怎么选"段落（Source/Barrier 对比）描述仍准确。

- [ ] **Step 4: 汇报并等待用户验证**

向用户汇报改动清单（git status 全量文件列表 + 编译结果），等待用户验证确认。**不执行任何 git add/commit**——提交由用户确认后统一整理（Docs/Git提交规范.md）。
