# AI 工程体系一期（打底）实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 落地 AI 工程体系一期的四个交付物——rules 渐进披露规则层（14 篇：coding 3 + architecture 2 + framework 6 + ui 1 + tooling 2）、memory 经验层（索引 + 写作规范 + 6 域 traps + feature-map 7 文件）、verify-compile.sh 编译验证一条命令、CLAUDE.md 宗旨型重写（含删除两篇旧规范）。

**Architecture:** 规则正文直写 `.claude/rules/**/*.md`、frontmatter `paths:` 按路径渐进披露（不拆壳/正文两层）；经验层按准入三问从个人记忆筛入 git；CLAUDE.md 只留行为准则与一行式铁律，项目细节全部下沉 rules。

**Tech Stack:** Markdown（Claude Code 原生 rules/memory/skills 机制）+ bash（Git Bash）+ unity-cli（TCP 操控 Unity Editor）。

**Spec:** `.claude/plans/2026-11-23-ai-project-system-design.md`（已批准的设计文档；本计划只覆盖其中「一期·打底」，二三期另行写计划）

## Global Constraints

- 全部新建文档正文使用中文；技术术语与代码标识符保留原文。
- 规则文件 frontmatter 契约固定为：`---` 包裹的 `paths:` 列表（见设计文档 §6）；无 paths 的文件视为全局常驻（本期只有 git-conventions.md 与 unity-cli.md 两篇如此）。
- **统一提交**：三期全部完成后统一一次提交，本计划所有任务均**不含 commit 步骤**；工作区结束时保持干净可 diff。
- 现有资产不动：`.claude/commands/`、`.claude/skills/luban-excel/`、`.claude/settings.local.json`。
- `Docs/网络模块学习笔记/` 不动、不引用（个人学习记录）。
- Windows 环境：`.sh` 脚本走 Git Bash；脚本内的输出字面量**只用纯 ASCII**（PS/bash 编码坑，个人记忆 powershell-ps1-no-bom-hazard）。
- traps 内容必须过「准入三问」（可迁移性 / 名称硬否决 / 正确性），体例固定句式「违反：后果；必须：做法」，禁止图形符号。
- Unity Editor 需处于运行状态才能测 verify-compile.sh（Task 1 前置检查）。

## Review Focus

- **paths: 语义假设不成立**（无 paths 不常驻、或 paths 通配不匹配本项目相对路径）→ 整个 rules 层路由设计失效。Task 0 首日实测拦截，实测结论写进任务产物。
- **verify-compile.sh 在 Unity 未连接时**：必须 exit 2 并输出明确提示，而不是挂死或静默 exit 0。Task 1 步骤 5 实测。
- **规则 frontmatter YAML 写错**（多余缩进、中文冒号）导致规则静默不加载——静默失败是渐进披露最危险的形态。Task 0 实测同时验证格式被正确解析。
- **两篇旧规范删除后的死指针**：已知引用点共三处——CLAUDE.md（重写自然消除）、check-style.ps1（三期删除，本期仅备注）、.editorconfig:18 注释（Task 10 修正）。实施时不得遗漏 .editorconfig。
- **traps 写成一次性现场或业务名流水账**：筛入条目须剥离「某某面板/某某配置行」级别的业务专名，保留框架 API 名与机制规律。Task 7 自查步骤按条目核对。

---

### Task 0: paths: 语义实测（硬性前置，最先做）

**Files:**
- Create: `.claude/rules/_probe/semantic-probe.md`（临时，测完删除）
- Create: `.claude/plans/paths-semantics-findings.md`（实测结论，保留）

**Interfaces:**
- Produces: 实测结论文件，钉死两个语义事实——(A) 带 `paths:` 的规则是否命中才加载；(B) 无 `paths:` 的规则是否每次常驻。Task 2-5 的路由设计依赖结论 (A)(B) 成立；不成立则**停止后续任务**并向用户报告，修正路由设计后再继续。

- [ ] **Step 1: 写临时规则**

```markdown
---
paths:
  - ".claude/plans/**"
---

# 语义探针
探针规则生效标记：PROBE-RULE-LOADED-2026-11-24。若你能看到此文本，说明带 paths 的规则被加载了。
```

- [ ] **Step 2: 命中路径触载测试**——Read 本计划文件（`.claude/plans/` 命中路径），观察会话上下文是否出现探针规则内容（system-reminder 或 rules 注入块）。记录结果。

- [ ] **Step 3: 无 paths 常驻测试**——另建临时规则不带 frontmatter（`_probe/global-probe.md`），同会话内 Read 任意不匹配文件（如 `CLAUDE.md`），观察是否常驻注入。记录结果。

- [ ] **Step 4: 同会话新建规则即时性**——探针规则是本会话内新建的，若 Step 2 未触发，说明规则索引在会话启动时快照，需重启会话验证。记录即时性结论。

- [ ] **Step 5: 写结论文件并清理探针**

`.claude/plans/paths-semantics-findings.md` 内容至少记录：命中是否加载、无 paths 是否常驻、新建规则是否需重启会话、frontmatter 格式是否被正确解析（探针若加载即证明格式合法）。然后删除 `.claude/rules/_probe/` 整个目录。

**结论成立（A 且 B 均符合设计假设）→ 继续 Task 1；任一不成立 → 停止，报告用户。**

---

### Task 1: verify-compile.sh 编译验证一条命令

**Files:**
- Create: `.claude/verify-compile.sh`

**Interfaces:**
- Consumes: unity-cli 子命令 `system ping`、`clear_console`、`refresh_assets`、`tool call get_compilation_state --output json`（JSON 字段 `isCompiling`/`errorCount`）、`tool call read_console --params-file`（schema 先 `unity-cli tool schema read_console` 查，个人记忆：参数为 `logTypes` 数组 + `count`）。
- Produces: 命令 `bash .claude/verify-compile.sh`——exit 0 编译通过 / exit 1 有编译错误（输出错误列表）/ exit 2 编辑器未连接。后续所有规则与技能正文引用此命令。

- [ ] **Step 1: 前置检查**——`unity-cli system ping` 返回 pong（Unity Editor 已运行）。未运行则本任务挂起，请用户打开 Unity。

- [ ] **Step 2: 写脚本**（完整脚本如下，直接照写）

```bash
#!/usr/bin/env bash
# FuFramework compile verification via unity-cli.
# Exit codes: 0 = compile passed; 1 = compile errors; 2 = editor not connected.
set -u

UNITY_CLI_TIMEOUT_MS=15000
POLL_INTERVAL=2
POLL_MAX=60   # 60 * 2s = 120s upper bound

unity-cli system ping --timeout-ms "$UNITY_CLI_TIMEOUT_MS" >/dev/null 2>&1
if [ $? -ne 0 ]; then
  echo "[verify-compile] ERROR: Unity Editor not connected (unity-cli ping failed)."
  echo "[verify-compile] Open the Unity project first, then retry."
  exit 2
fi

unity-cli tool call clear_console >/dev/null 2>&1
unity-cli tool call refresh_assets >/dev/null 2>&1

elapsed=0
while [ $elapsed -lt 120 ]; do
  state=$(unity-cli tool call get_compilation_state --output json --timeout-ms "$UNITY_CLI_TIMEOUT_MS" 2>/dev/null)
  compiling=$(echo "$state" | grep -o '"isCompiling"[^,}]*' | grep -o 'true\|false')
  if [ "$compiling" = "false" ]; then
    break
  fi
  sleep $POLL_INTERVAL
  elapsed=$((elapsed + POLL_INTERVAL))
done

errors=$(echo "$state" | grep -o '"errorCount"[^,}]*' | grep -o '[0-9]\+')
if [ "$errors" = "0" ] || [ -z "$errors" ]; then
  echo "[verify-compile] PASS: no compile errors."
  exit 0
fi

echo "[verify-compile] FAIL: $errors compile error(s):"
unity-cli tool call read_console --params-file /dev/stdin <<'EOF'
{"logTypes": ["Error", "Exception"], "count": 20}
EOF
exit 1
```

说明：JSON 字段提取用 grep 不用 jq（Git Bash 无 jq 保证）；read_console 的确切参数名以 `unity-cli tool schema read_console` 实测为准，不符则修正 Heredoc JSON。

- [ ] **Step 3: 跑通基线（绿）**——`bash .claude/verify-compile.sh`，预期：PASS 且 exit 0（当前项目无编译错误）。

- [ ] **Step 4: 故障注入（红）**——在任一非生成 .cs 文件（如 `Unity/Assets/Scripts/Hotfix/Framework/Timer/` 下某文件）临时加入一行 `// VERIFY_COMPILE_PROBE public class __NoSuchType__ {}` 引发的语法错误（实际写 `this is not valid csharp;;;` 于类外），再跑脚本。预期：FAIL + 错误列表 + exit 1。**测完立即还原文件**（`git checkout -- <file>` 不行——无提交基线，须手工删除注入行）。

- [ ] **Step 5: 未连接路径（exit 2）**——临时把脚本内 ping 改为不存在的 `--host 127.0.0.2` 跑一次验证 exit 2 分支，改回。

- [ ] **Step 6: 三次连续运行稳定性**——连跑 `bash .claude/verify-compile.sh` 三次，三次都 PASS。防止轮询竞态（上次编译未结束就断言）。

---

### Task 2: rules/coding/ 三篇（含两篇旧规范并入）

**Files:**
- Create: `.claude/rules/coding/coding-standards.md`（paths: `**/*.cs`）
- Create: `.claude/rules/coding/comment-conventions.md`（paths: `**/*.cs`）
- Create: `.claude/rules/coding/git-conventions.md`（无 paths，全局）

**Interfaces:**
- Consumes: `Docs/代码风格规范.md`（全文并入源）、`Docs/Git提交规范.md`（全文并入源）、个人记忆 `csharp-code-style.md` 与 `commit-convention.md`、`commit-confirmation-rule.md`（`C:\Users\Administrator\.claude\projects\D---WorkSpace-Unity-FuFramework2-0\memory\` 下）。
- Produces: 三篇规则成为风格与提交的单一事实源；Task 9 的 CLAUDE.md 与 Task 10 的删除动作依赖本任务完成。

- [ ] **Step 1: 读全部来源**——两篇 Docs 规范 + 三条个人记忆原文。

- [ ] **Step 2: 写 coding-standards.md**——结构：铁律五条的操作细节（LINQ 替代 = 手写循环 + 缓存字典；UniTask 取消链模式；反射禁令例外仅 Editor 程序集）→ 命名/缩进/括号/组织/修饰符/var/日志（浓缩自风格规范，按「AI 当场要用才写」裁剪，全文目标 ≤120 行）→ 结尾一行注明「本文件为单一事实源，Docs/代码风格规范.md 已删除」。m_ 前缀 / Async 后缀 / E 前缀三项一并写入（check-style.ps1 的检查项移交此文件，注明由 fu-code-review 执行检查）。

- [ ] **Step 3: 写 comment-conventions.md**——XML 主导、最小可用风格（对应提交 3f092f54 口径）、注释语言中文、何时必须写/何时省。目标 ≤60 行。

- [ ] **Step 4: 写 git-conventions.md**——类型、格式、AI 提交标记、**提交前必须列清单问用户确认**（个人记忆 commit-confirmation-rule）。目标 ≤80 行。

- [ ] **Step 5: 验证**——三篇文件 frontmatter 均为合法 YAML（对照 Task 0 的格式结论）；grep 确认每篇含「单一事实源」或「已删除」标注。

---

### Task 3: rules/architecture/ 两篇

**Files:**
- Create: `.claude/rules/architecture/project-structure.md`（paths: `Unity/Assets/Scripts/**`）
- Create: `.claude/rules/architecture/module-boundary.md`（paths: `Unity/Assets/Scripts/**`）

**Interfaces:**
- Consumes: `Docs/项目架构与技术栈.md`、真实目录布局（AOT/Framework/{Extension,Log,Utility}；Hotfix/Framework/{Asset,Config,Core,Download,Entity,Event,FSM,Guide,Localization,Model,Mono,Network,ObjectPool,Procedure,RedDot,Scene,Sound,Storage,Timer,UI,Web}；Hotfix/Game/{AutoGen,Events,Model,Network,UI}；AOT/Launch）、个人记忆 pending-module-const-b-round.md（Const 分部判例）。
- Produces: 模块布局与边界规则的单一事实源；Task 8 feature-map 与其共享同一布局事实。

- [ ] **Step 1: 读架构文档与目录实况**——架构文档全文 + `ls` 抽查两个模块目录。

- [ ] **Step 2: 写 project-structure.md**——AOT/Hotfix 双层职责（AOT 不可热更 / Hotfix 走 HybridCLR）、程序集划分（FuFramework.\*.Runtime/Editor 一模块一双 csproj）、热更模块 21 目录清单、Game 层 AutoGen/Events/Model/Network/UI。目标 ≤100 行。

- [ ] **Step 3: 写 module-boundary.md**——模块常量收敛 Const 分部（Core/Game 不碰判例）、跨模块调用方向、模块生命周期 OnDispose 挂钩取消、禁止模块间反向依赖。目标 ≤80 行。

- [ ] **Step 4: 验证**——frontmatter 合法；模块清单与真实目录 `ls` 一致。

---

### Task 4: rules/framework/ 六篇

**Files:**
- Create: `.claude/rules/framework/uni-task-async.md`（paths: `**/*.cs`）
- Create: `.claude/rules/framework/module-system.md`（paths: `Unity/Assets/Scripts/**/Framework/**`）
- Create: `.claude/rules/framework/event-system.md`（paths: `Unity/Assets/Scripts/**/Framework/**`）
- Create: `.claude/rules/framework/network-protocol.md`（paths: `Protobuf/**`、`**/Network/**`）
- Create: `.claude/rules/framework/config-tables.md`（paths: `Config/**`）
- Create: `.claude/rules/framework/localization.md`（paths: `Config/**`、`**/Localization/**`）

**Interfaces:**
- Consumes: 个人记忆 `unitask-attach-external-cancellation-pitfall.md`、`timer-register-design-contract.md`、`pending-event-system-scaleup.md`、`pending-cancellation-rename-phase2.md`（命名口径 m_xxxIssuer/取消栅栏）、`proto-format-convention.md`、`proto-id-assignment.md`、`network-single-channel-model.md`、`luban-template-workflow.md`、`config-group-wiring.md`、`l10n-iscode-export.md`、`aot-localization.md`、`pending-l10n-split-by-language.md`、`pending-l10n-language-data-driven.md`、`tablemanager-localization-design.md`；真实命令 `Config/gen-client-bin.bat` 等。
- Produces: 框架域规则六篇（rules 总数 14 篇）。config-tables.md 的 gen 命令供 Task 8 feature-map/config.md 引用。

- [ ] **Step 1: 读全部来源记忆与配置目录实况**（`ls Config/`、`ls Protobuf/`）。

- [ ] **Step 2: 写 uni-task-async.md**——AttachExternalCancellation 只断外层、孤儿链照跑；已取消 Token 下 Forget 吞异常；取消必须进业务 await 点；CancellationTokenIssuer 模式与 m_xxxIssuer 命名；网络/资源类异步 API 的 Token 必传无默认值。目标 ≤90 行。

- [ ] **Step 3: 写 module-system.md**——模块生命周期、TimerRegister 契约（批量所有权 + StartXxx 返回 id）、对象池用法、常量 Const 分部。目标 ≤80 行。

- [ ] **Step 4: 写 event-system.md**——Subscribe\<T\> 类型键、无 sender 广播、事件类 sealed + 基类派生 Id、挂起机制边界（跨所有者失联不修为裁决）。目标 ≤70 行。

- [ ] **Step 5: 写 network-protocol.md**——proto 分区横幅/换行大括号、消息 ID 按声明顺序自动分配（重排即换 ID）、Req/Rsp/Notify 前缀决定消息类型、单频道门面模型（换线归服务器）、Inner_Basic_2 空占位保留为裁决。目标 ≤80 行。

- [ ] **Step 6: 写 config-tables.md**——表名分段约定与 -c-/-aot- 分组、is_code 三端约定、gen 脚本四件套命令（gen-client-bin/json、gen-server-bin/json）、Luban 模板工作流（源码版模板 → build-luban.bat → gen 脚本）。目标 ≤90 行。

- [ ] **Step 7: 写 localization.md**——按语种拆分产物布局（Config/Data | Config_Localization）、语种数据化（6 语种零代码增删）、is_code 三端剥离（##group=none）、AOT 前置本地化（TbLocalizationAOT 独立表/LaunchLocalization 列序解析）、TableManager 多语言接线定案、英语兜底二段查询。目标 ≤90 行。

- [ ] **Step 8: 验证**——六篇 frontmatter 合法；gen 命令名与 `ls Config/gen-*` 一致。

---

### Task 5: rules/ui/ 与 rules/tooling/ 三篇

**Files:**
- Create: `.claude/rules/ui/fgui-workflow.md`（paths: `FairyGUIProject/**`、`**/UI/**`）
- Create: `.claude/rules/tooling/powershell-and-encoding.md`（paths: `**/*.ps1`、`**/*.sh`）
- Create: `.claude/rules/tooling/unity-cli.md`（无 paths，全局）

**Interfaces:**
- Consumes: 个人记忆 `fgui-plugin-workflow.md`、`fui-pkg-empirical.md`、`fu-menu-priority.md`、`powershell-ps1-no-bom-hazard.md`、`powershell-batch-rename-hazard.md`、`edit-tool-whitespace-hazard.md`、`unity-cli-slow-connection.md`、`unity-probe-workflow.md`、`editor-windows-reflection-coupling.md`；Task 1 产出的 verify-compile.sh 用法。
- Produces: tooling/unity-cli.md 中包含 verify-compile.sh 引用（改完 .cs 必跑）与两个环境变量（UNITY_CLI_NO_AUTO_UPDATE=1、UNITY_CLI_HOST=127.0.0.1）。

- [ ] **Step 1: 读全部来源记忆。**

- [ ] **Step 2: 写 fgui-workflow.md**——LuaAPI 目录查签名、验证靠编辑器实测、InitUIEvent 手写层/事件白名单/genCode 开关、GetChild 不索引化、FuMenuPriority 分段管理。目标 ≤80 行。

- [ ] **Step 3: 写 powershell-and-encoding.md**——Write 工具产 .ps1 无 BOM 破串（脚本字面量纯 ASCII）、-replace 大小写不敏感吞类型名（用 -creplace）、Edit 工具空白转换危险（Tab/空格转换用脚本+验证）。目标 ≤60 行。

- [ ] **Step 4: 写 unity-cli.md**——连接验证 ping、两个环境变量、tool call --params-file 语法与 tool schema 查询、编译验证套路指向 `bash .claude/verify-compile.sh`、Play 模式插桩/还原流程与心跳对照、Editor 面板坑三条（GUIStyle 序列化/Play 中编译/OnEnable 早于 Hotfix）。目标 ≤90 行。

- [ ] **Step 5: 验证**——三篇 frontmatter 合法；verify-compile.sh 引用与 Task 1 产物一致。

---

### Task 6: memory/MEMORY.md + traps-authoring.md

**Files:**
- Create: `.claude/memory/MEMORY.md`
- Create: `.claude/memory/traps-authoring.md`

**Interfaces:**
- Produces: MEMORY.md 的索引表结构（文件 | 覆盖域 | 核心内容），Task 7 的六个 traps 文件名必须与此索引一致；traps-authoring.md 的准入三问是 Task 7 自查的判据。

- [ ] **Step 1: 写 traps-authoring.md**——准入三问（可迁移性/名称硬否决——API 名可留业务名必剥/正确性）+ 体例四条（先 grep 查重/单条一规律/「违反：…必须：…」固定句式/禁图形符号）+ 5000 token 清理阈值与处置顺序（合并相似 → 删被 rules 覆盖 → 通用迁 rules）。目标 ≤50 行。

- [ ] **Step 2: 写 MEMORY.md**——标题「FuFramework 踩坑记录索引」；维护原则四条（按域拆分/约束句式/不与 rules 重复/不记会话上下文）+ 清理阈值 + feature-map 维护原则（只做索引禁细节）+ 索引表（Task 7 六个文件 + 本文件，先写占位行，Task 7 完成后回填核心内容列）。

- [ ] **Step 3: 验证**——两个文件结构齐全；索引表行数与预期 traps 文件数一致。

---

### Task 7: 六个域 traps（素材筛自个人记忆）

**Files:**
- Create: `.claude/memory/traps-framework.md`
- Create: `.claude/memory/traps-ui.md`
- Create: `.claude/memory/traps-network.md`
- Create: `.claude/memory/traps-config.md`
- Create: `.claude/memory/traps-l10n.md`
- Create: `.claude/memory/traps-tooling.md`

**Interfaces:**
- Consumes: traps-authoring.md 准入三问（Task 6）；个人记忆池（同 Task 2/4/5 的来源，另含 `tablemanager-localization-design.md`、`aot-localization.md`、`pending-l10n-split-by-language.md`、`pending-l10n-language-data-driven.md`、`timer-module-backup-optimizations.md`、`perf-vs-readability-lesson.md`、`codegen-interact-split-deferred.md`）。
- Produces: 六篇 traps 与 Task 6 索引表对应。

- [ ] **Step 1: 逐条筛入**——按设计文档 §7 的分配表，从记忆池逐条过准入三问后写入对应域文件。分配：framework ← UniTask 取消三坑/常量收敛判例/Timer 契约/性能vs可读性教训；ui ← FGUI 插件四决策/FuiPkg 实证/菜单优先级；network ← proto 正则坑/ID 重排/单频道门面/重连六坑；config ← Luban 模板流/is_code 三端/分组接线/gen 脚本无 BOM；l10n ← L10nKey 导出/_Localization_Key 固化/AOT 前置/语种数据化坑/英语兜底；tooling ← PowerShell 三坑/Editor 面板三坑/unity-cli 慢连接。待办性质的记忆（pending-\* 前缀）只筛「已定案的机制规律」，**不筛**「待办状态」本身。

- [ ] **Step 2: 每条自查**——句式合规（违反/必须）、无业务专名（组件类名/配置字段/英雄装备名剔除）、无图形符号。发现不合则改写或剔除。

- [ ] **Step 3: 回填 MEMORY.md 索引**——每个文件一行核心内容。

- [ ] **Step 4: 验证**——`ls .claude/memory/` 文件数与索引表行数一致；抽查三条无业务专名。

---

### Task 8: feature-map/ 功能→文件索引

**Files:**
- Create: `.claude/memory/feature-map/_index.md`
- Create: `.claude/memory/feature-map/framework-core.md`（Core/Extension/Utility/Log/Mono/FSM/Procedure/Timer/ObjectPool/ReferencePool）
- Create: `.claude/memory/feature-map/config.md`（Config 模块 + Config/ 目录 + Luban 工具）
- Create: `.claude/memory/feature-map/network.md`（Network 模块 + Protobuf/ + Server 对应关系）
- Create: `.claude/memory/feature-map/ui.md`（UI 模块 + FairyGUIProject/ + FGUI 插件）
- Create: `.claude/memory/feature-map/localization.md`（Localization 模块 + L10N 表 + AOT/Launch/Localization）
- Create: `.claude/memory/feature-map/launch.md`（Launcher/Entry/Scene/Sound/Download/Storage/Save/Setting/Web 等剩余模块）

**Interfaces:**
- Consumes: `Docs/项目架构与技术栈.md`、真实目录布局（同 Task 3）。
- Produces: 域路由索引 `_index.md`，fu-\* 技能（二期）的定位入口。

- [ ] **Step 1: 写 _index.md**——域路由表：需求关键词 → 域文件。域清单刻意不硬编码规则，新增域时索引自然覆盖。

- [ ] **Step 2: 逐域展开**——每域按「功能名 → 文件路径」表格式，只索引不写实现细节与结论。素材以架构文档为骨架，`ls` 实况校对每个路径存在。

- [ ] **Step 3: 验证**——抽查 10 个路径真实存在（`ls` 验证）；无一条含实现结论。

---

### Task 9: CLAUDE.md 宗旨型重写

**Files:**
- Modify: `CLAUDE.md`（整文件重写）

**Interfaces:**
- Consumes: `C:\Users\Administrator\Downloads\client-ai\CLAUDE.md`（宗旨模板，结构对照不改写其英文原文）；设计文档 §12 章节骨架；Task 2 的 git-conventions/coding-standards 落位。
- Produces: 新常驻层。三期 session-start 钩子上线前，保留一行过渡指引「动手前先查 `.claude/skills/` 项目技能」。

- [ ] **Step 1: 读 client-ai CLAUDE.md 模板**与设计文档 §12。

- [ ] **Step 2: 重写 CLAUDE.md**——按设计文档 §12 骨架九节：交流语言 / Think Before You Code / Explain With Concrete Cases / Goal-Driven Execution / Review Before You Build / Explore Beyond the Scope（五节从 client-ai 对应节**中文改写**，不是翻译腔）→ 硬性禁令（五条铁律每条一行零解释）→ Coding Standards 通用原则（性能/日志/设计/可读性/数据与状态/异步/文件组织各 2-4 条）→ 验收工具一行（verify-compile.sh + unity-cli 指向 rules/tooling/unity-cli.md）→ 过渡指引一行。目标 6-8 KB。

- [ ] **Step 3: 验证**——全文无业务细节（无表名/无 API 名/无目录清单）；五条铁律逐条在且每条一行；引用的两个 rules 路径真实存在。

---

### Task 10: 删除两篇旧规范 + .editorconfig 引用修正

**Files:**
- Delete: `Docs/代码风格规范.md`
- Delete: `Docs/Git提交规范.md`
- Modify: `.editorconfig:18`（注释引用改为指向 `.claude/rules/coding/coding-standards.md`）

**Interfaces:**
- Consumes: Task 2 完成（单一事实源已就位）——**本任务必须在 Task 2 验证通过后执行**。

- [ ] **Step 1: 复核引用**——`grep -rn "Docs/代码风格规范\|Docs/Git提交规范"` 全仓（排除 Docs/ 自身），预期仅剩 CLAUDE.md（Task 9 已重写消除）、check-style.ps1（三期删除，备注不处理）、.editorconfig。与预期不符则逐一处理。

- [ ] **Step 2: 改 .editorconfig 注释**——18 行注释指向新路径。

- [ ] **Step 3: 删除两篇 Docs 文件**。

- [ ] **Step 4: 验证**——再跑一次引用 grep，除 check-style.ps1 外零命中。

---

## 一期收尾

- [ ] 全仓 `git status` 过目：新增文件全部在 `.claude/`（rules/memory/plans）+ 根 CLAUDE.md + verify-compile.sh + verify-compile 探针的还原确认；删除仅两篇 Docs 规范。**不提交**（统一提交在三期末）。
- [ ] 工作区留存状态向用户汇报：Task 0 实测结论要点 + 任何偏离计划的决定。

## 后续（二三期，另行写计划）

- **二期·流程**：using-project-skills 元技能（优先级链：用户指令 > fu-\* > 默认）、三档开发流（fu-quick-help / fu-brainstorming-quick / fu-new-feature-dev）、业务技能（fgui-edit / unity-cli-usage）。素材：client-ai 实仓技能正文裁剪。
- **三期·治理收尾**：治理技能三篇（fu-code-review 承接 check-style 三项检查 / fu-doc-sync / fu-rules-from-review）、5 个 agents、session-start 钩子 + 新建 .claude/settings.json（SessionStart 注册进 git）、删除 check-style.ps1 及其 settings.local.json 注册、删除 Docs/superpowers/ 74 篇、**统一提交**（三期全部完成后唯一一次 commit，提交清单先经用户确认）。
