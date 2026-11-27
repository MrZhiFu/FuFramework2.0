# FuFramework2.0 AI 工程体系设计文档

> 日期：2026-11-23
> 参考：`D:\_WorkSpace\Knowledge-Repository\SLG\4.AI工程导读`（10 篇）+ `client-ai` 仓
> 状态：设计定稿，待评审

## 1. 意图与动机

**做什么**：为 FuFramework2.0 建一套 Claude Code 专属的「AI 入职手册 + 工具箱」——把项目约束、踩坑经验、开发流程、功能地图、验收工具沉淀成 git 内的文件，让 AI 每次开工自动带上，不靠口头交代、不靠某台机器的个人记忆。

**为什么现在做**：
1. 当前知识载体只有根 `CLAUDE.md`（常驻全量进上下文）+ `Docs/` 两篇规范。规则无法按文件路径按需加载，每次会话背全量，且细节写不下。
2. 协作过程中沉淀的约 30 条项目踩坑记录（UniTask 取消语义、Luban 模板工作流、proto 格式陷阱、PowerShell 编码坑等）存放在本机 `~/.claude/projects/.../memory/`，**不进 git**——换机器即丢失，团队不可共享。
3. 没有功能→文件索引（feature-map），AI 定位代码靠全仓盲搜。

**目标用户**：本仓库的日常开发者（当前一人）+ Claude Code。

## 2. 目标 / 非目标 / 反目标

### 目标

- G1：约束按路径渐进披露——碰哪类文件才加载哪类规则，常驻指令瘦身。
- G2：踩坑经验进 git、可迁移、可被 AI 主动检索。
- G3：建立功能→文件地图，AI 定位代码从「盲搜」变「查表精读」。
- G4：建立三档开发流（轻/中/重）+ 业务操作技能（FGUI/Luban/unity-cli）+ 治理技能（评审/文档同步）。
- G5：验收操作封成一条命令（编译验证）。
- G6：子代理分工定义（带工具白名单）。

### 非目标

- 不做多工具支持：只服务 Claude Code，Qoder/zcode/Codex/Cursor 的壳生成器、junction 多仓装配、`rules-content/` 正文拆层全部砍掉。
- 不做独立配置仓：`.claude/` 直接放在本仓库内，git 同仓提交。
- 不做规则注入器（`rules_inject.py` 类外挂）：Claude Code 原生支持 `paths:` 路由，无需外挂。
- `Docs/网络模块学习笔记/` 是个人学习记录，不是工程约束，不进 rules、不被任何规则引用。

### 反目标（做了即使能跑也算失败）

- 把具体业务规则塞回 `CLAUDE.md` 常驻层（上下文膨胀，回到老路）。
- traps 记录一次性现场、业务组件名、无法核实的传闻（违背准入三问，三个月后变成永久噪音）。
- 流程技能与 superpowers 插件互相抢调用（同一任务两条流程打架）。

## 3. 约束

- C1：代码铁律五条（UniTask/禁协程/禁 LINQ/禁反射/异步链生命周期）是所有规则、技能、子代理、评审清单的最高纲领，任何文档不得与之冲突。
- C2：全程中文交流（含所有新建文档正文）。
- C3：Windows 环境。钩子/脚本须兼容 PowerShell 5.1（中文 Windows 默认 ANSI 读 stdin，涉及编码必须显式 UTF-8）；`bash` 可用（Git Bash）。
- C4：`.gitignore` 已忽略 `.claude/settings.local.json` 与 `.superpowers/`，其余 `.claude/` 内容可提交。
- C5：现有 `.claude/` 内容处置：`commands/`（luban-excel、polish-module）、`skills/luban-excel`、`settings.local.json` 保留；`hooks/check-style.ps1` 及其 PostToolUse 注册**删除**（已裁决：风格强制不进钩子层，检查项归口 fu-code-review）。
- C6：unity-cli 已安装可用（TCP 操控 Unity Editor），编译验证走 unity-cli 而非命令行 Unity。
- C7：fu-\* 三档开发流**全面接管**项目流程，独立运作不调 superpowers（已裁决）；superpowers 插件不再作为流程依赖，其历史产物文档（Docs/superpowers/ 74 篇）删除。

## 4. 方案对比（三档）

| | A 最小可用 | B 完整六类（**选定**） | C 折中 |
|---|---|---|---|
| rules + memory | ✓ | ✓ | ✓ |
| 三档开发流技能 | ✗ | ✓ | ✗（用 superpowers） |
| 业务/治理技能 | ✗ | ✓ | 部分补 |
| agents + hooks | ✗ | ✓ | ✗ |
| 取舍 | 成本最低，但无项目 SOP，流程全靠通用插件 | 功能最全，工作量大；与 superpowers 有部分重叠 | 省一半工作量，但流程与知识分层不完整 |
| 为什么选 B | 用户裁决选 B：要 client-ai 对齐的完整体系；与 superpowers 的重叠通过「口径裁决」（C7）消解而非砍功能 | | |

## 5. 总体结构

```
FuFramework2.0/
├── CLAUDE.md                        ← 瘦身：铁律纲领 + 行为准则 + 文档路由表（常驻）
├── .claude/
│   ├── rules/                       ← 机制① 约束层：正文直写 + paths: frontmatter（13 个域）
│   │   ├── coding/                  coding-standards.md, comment-conventions.md, git-conventions.md
│   │   ├── architecture/            project-structure.md, module-boundary.md
│   │   ├── framework/               uni-task-async.md, module-system.md, event-system.md,
│   │   │                            network-protocol.md, config-tables.md, localization.md
│   │   ├── ui/                      fgui-workflow.md
│   │   └── tooling/                 powershell-and-encoding.md, unity-cli.md
│   ├── memory/                      ← 机制② 经验层
│   │   ├── MEMORY.md                ← 团队踩坑索引 + 维护规则（进 git，注意与个人记忆区分）
│   │   ├── traps-authoring.md       ← 写作规范（准入三问 + 体例 + 5000 token 清理阈值）
│   │   ├── traps-framework.md       ← 按域拆：framework/ui/network/config/l10n/tooling
│   │   ├── traps-tooling.md
│   │   └── feature-map/             ← 功能→文件索引，按模块域拆分
│   │       ├── _index.md
│   │       ├── framework-core.md, config.md, network.md, ui.md, localization.md ...
│   ├── skills/                      ← 机制③ 流程层
│   │   ├── using-project-skills/    ← 元技能（SessionStart 注入目标）
│   │   ├── fu-quick-help/           ← 轻档：≤3 文件，7 步
│   │   ├── fu-brainstorming-quick/  ← 中档：disable-model-invocation，5 Phase
│   │   ├── fu-new-feature-dev/      ← 重档：disable-model-invocation，13 Phase
│   │   ├── fgui-edit/               ← 业务技能：FGUI 操作流
│   │   ├── unity-cli-usage/         ← 业务技能：unity-cli 工作流
│   │   ├── fu-code-review/          ← 治理技能：铁律扫描 + 项目踩坑核对
│   │   ├── fu-doc-sync/             ← 治理技能：提交前文档同步
│   │   ├── fu-rules-from-review/    ← 治理技能：评审发现沉淀成 traps
│   │   └── luban-excel/             ← 已有，保留
│   ├── agents/                      ← 机制④ 分工层（5 个 .md）
│   │   ├── agent-fu-dev.md          ← 通用实现者
│   │   ├── agent-luban-expert.md
│   │   ├── agent-fgui-ui.md
│   │   ├── agent-quality-checker.md ← 铁律深检
│   │   └── agent-feature-reviewer.md
│   ├── hooks/
│   │   └── session-start            ← 新增：注入 using-project-skills 元技能
│   │       （check-style.ps1 及其 PostToolUse 注册删除，检查项归口 fu-code-review）
│   ├── verify-compile.sh            ← 机制⑥ 工具链：编译验证一条命令
│   ├── settings.local.json          ← 已有，本地追加 SessionStart/Stop 钩子注册
│   └── commands/                    ← 已有，保留
```

## 6. 机制① rules：渐进披露规则层

**形态**：正文直写在规则文件里，不再拆壳/正文两层（非目标里已裁决）。

**frontmatter 契约**：

```markdown
---
paths:
  - "Unity/Assets/Scripts/**"
---
```

**paths 路由与域分配**（基于真实目录结构）：

| 域文件 | paths | 内容来源 |
|---|---|---|
| coding/coding-standards.md | `**/*.cs` | **`Docs/代码风格规范.md` 内容并入此处，原文件删除，本文件成为单一事实源**；含铁律五条的操作细节（LINQ 替代写法、UniTask 取消链模式） |
| coding/comment-conventions.md | `**/*.cs` | 风格规范第 2 节（XML 主导/最小可用风格，对应最近三次注释修正提交的口径） |
| coding/git-conventions.md | 无 paths（全局） | **`Docs/Git提交规范.md` 内容并入此处，原文件删除，本文件成为单一事实源**：类型、格式、AI 提交标记、提交前询问用户规则 |
| architecture/project-structure.md | `Unity/Assets/Scripts/**` | AOT/Hotfix 双层结构、asmdef 程序集划分、FuFramework.\* 模块分布 |
| architecture/module-boundary.md | `Unity/Assets/Scripts/**` | 模块边界：模块常量收敛 Const 分部、跨模块调用规则 |
| framework/uni-task-async.md | `**/*.cs` | 铁律 1/2/5 细化：AttachExternalCancellation 陷阱、CancellationTokenIssuer 模式、Forget 吞异常 |
| framework/module-system.md | `Unity/Assets/Scripts/**/Framework/**` | 模块生命周期（OnDispose）、TimerRegister 设计契约、对象池用法 |
| framework/event-system.md | `Unity/Assets/Scripts/**/Framework/**` | Subscribe\<T\> 类型键、无 sender 广播、事件挂起机制 |
| framework/network-protocol.md | `Protobuf/**`, `**/Network/**` | proto 分区横幅/换行大括号、消息 ID 分配规则、Req/Rsp/Notify 前缀、单频道门面 |
| framework/config-tables.md | `Config/**` | Luban 表名分段约定、-c-/-aot- 表级分组、is_code 约定、gen 脚本流程 |
| framework/localization.md | `Config/**`, `**/Localization/**` | 语种数据化、按语种拆分产物、L10N aot 段、英语兜底二段查询 |
| ui/fgui-workflow.md | `FairyGUIProject/**`, `**/UI/**` | FGUI 插件 CSharpCodeGen、InitUIEvent 手写层、发布流程、GetChild 约定 |
| tooling/powershell-and-encoding.md | `**/*.ps1`, `**/*.sh` | PS 5.1 无 BOM 陷阱、批量改名 -creplace、Edit 工具空白转换危险 |
| tooling/unity-cli.md | 无 paths（全局） | unity-cli 连接、常用命令、Play 模式插桩套路 |

**编写原则**：
- 每篇正文以「必须/禁止/不得」句式压缩表达，表格优先，目标读者是 AI。
- rules 是规范的**单一事实源**：`Docs/代码风格规范.md` 与 `Docs/Git提交规范.md` 并入对应规则文件后原文件删除（`.editorconfig` 的注释引用同步改为指向 `.claude/rules/...`）。
- 与 CLAUDE.md 的分工：CLAUDE.md 放行为准则与五条铁律一行式禁令（宗旨层）；rules 放项目细节与做法（做法层），两者不重复。

## 7. 机制② memory：经验层

**目录**：`.claude/memory/`

**MEMORY.md（团队索引）**：标题「FuFramework 踩坑记录索引」，含四条维护原则（按域拆分/约束句式/禁止与 rules 重复/禁止记录会话上下文）+ 5000 token 清理阈值 + 索引表。**注意**：与个人自动记忆（`~/.claude/projects/...`）是两个东西，本文件进 git。

**traps-\*.md 按域拆分**（初始 6 个）：

| 文件 | 初始素材（从本机记忆按准入三问筛入） |
|---|---|
| traps-framework.md | UniTask AttachExternalCancellation 只断外层、已取消 Token 下 Forget 吞异常、模块常量收敛判例 |
| traps-ui.md | FGUI 插件 LuaAPI 签名查询、验证靠编辑器实测、Gen 文件手写层约定 |
| traps-network.md | proto MessagePattern 正则坑、消息 ID 重排陷阱、重连与时间体系要点 |
| traps-config.md | Luban 模板工作流、is_code 三端约定、语种数据化坑、gen 脚本无 BOM |
| traps-l10n.md | L10nKey 导出、_Localization_Key 固化、表缺列报错、AOT 前置本地化 |
| traps-tooling.md | PowerShell 无 BOM/批量改名/Editor 面板坑（GUIStyle 序列化、Play 中编译、OnEnable 早于 Hotfix）、unity-cli 慢连接修复 |

**准入三问**（写进 traps-authoring.md，作为唯一准入标准）：
1. 可迁移性——换个业务/会话还成立吗？
2. 名称硬否决——含具体业务组件名/字段名则剥离重写（API 名可保留）。
3. 正确性——与当前代码一致，过时/传闻不得写入。

**体例**：单条一规律，标题陈述规律本身，正文固定句式「违反：后果；必须：做法」，禁止图形符号。

**feature-map/**：按模块域拆（\_index.md + framework-core/config/network/ui/localization/tooling 等），每条目只做「功能 → 文件路径」索引，禁止实现细节与业务结论。初始内容从 `Docs/项目架构与技术栈.md` 与真实目录展开。

## 8. 机制③ skills：流程层

### 三档开发流（fu-\*）

| 技能 | 档 | frontmatter | 流程 |
|---|---|---|---|
| fu-quick-help | 轻 | 可自动触发 | 理解确认 → 定位 → 方案确认 → 实现 → verify-compile → 自查 → 报告（7 步） |
| fu-brainstorming-quick | 中 | `disable-model-invocation: true` | 需求澄清 → 方案确认（BLUF 格式）→ 复杂度四维评估 → 实现 → 验证 → 评审 → 验收 → 提交 → 文档同步（5 Phase） |
| fu-new-feature-dev | 重 | `disable-model-invocation: true` | 13 Phase：并行探索 → 范围检查 → 需求澄清 → 流确认 → 文档准备 → 设计批准 → 计划 → 预检 → 执行 → 编译验证 → 三阶段评审 → 用户验收 → 文档同步+单 commit |

**口径（已裁决）**：fu-\* 是项目首选流程，独立运作，正文不调 superpowers 技能；superpowers 保留但日常开发走 fu-\*。fu-\* 正文写死项目域路由（读 feature-map、读 traps、跑 verify-compile、用 AskUserQuestion）。

**中/重档核心机制**（从 client-ai 移植并按本项目裁剪）：
- 主 Agent 不写代码不探索代码（重档）：探索派 Explore 子代理，实现派 agent-fu-dev。
- 显式批准关卡：沉默/追问/观察/犹豫不算批准。
- 任务物化：brief/report/review-package 落成文件传递，不贴长 prompt。
- 零上下文计划：计划完全自包含，子代理只读任务书。
- 可证伪验证：探针点名失败模式、独立推导期望值、变异检查。

### 业务技能

- **fgui-edit**：FGUI 编辑器操作流（打开工程 → 改组件 → 发布 → 代码生成 → Unity 侧验证），含 LuaAPI 签名查询套路。
- **unity-cli-usage**：unity-cli 连接验证、常用命令表、Play 模式插桩/还原流程、心跳对照验证。

### 治理技能

- **fu-code-review**：四铁律扫描清单（Task/Coroutine/LINQ/Reflection）+ **风格检查三项**（m_ 前缀字段、异步方法 Async 后缀、枚举 E 前缀——承接被删除的 check-style.ps1）+ 项目踩坑核对（读 traps）+ 风格规范要点。
- **fu-doc-sync**：提交前检查 Docs/ 与 memory/ 是否需要同步；新坑写入 traps。
- **fu-rules-from-review**：评审发现的工程规律按准入三问沉淀进 traps。

### 元技能 using-project-skills

约 40 行，只讲一条纪律：动手前先查有没有适用技能；优先级声明（用户显式指令 > 项目技能 > 默认系统提示）；含 `<SUBAGENT-STOP>`（子代理豁免）。由 session-start 钩子全文注入。

## 9. 机制④ agents：分工层

5 个子代理定义，每个含角色、工具白名单、职责边界：

| 代理 | 工具白名单要点 | 职责 |
|---|---|---|
| agent-fu-dev | 全工具 | 通用实现者：读 brief、写代码、写 report、不 commit |
| agent-luban-expert | Read/Grep/Glob/Bash(luban 脚本) | 配置表专家：改表/生成/校验 |
| agent-fgui-ui | Read/Grep/Glob/Bash(FGUI/unity-cli) | UI 专家：拼组件/发布/验证 |
| agent-quality-checker | Read/Grep/Glob | 质量检查：四铁律扫描、性能、风格 |
| agent-feature-reviewer | Read/Grep/Glob | 功能评审：Spec 合规独立审 diff |

## 10. 机制⑤ hooks：时机层

注册两个钩子（写进 `settings.local.json`，与现有 check-style PostToolUse 共存）：

| 事件 | matcher | 命令 | async | 行为 |
|---|---|---|---|---|
| SessionStart | `startup\|clear\|compact` | `bash .claude/hooks/session-start`（经 `git rev-parse` 解析仓库根） | false | 读 `skills/using-project-skills/SKILL.md` 全文 → JSON 转义 → 以 `hookSpecificOutput.additionalContext` 输出 |
| Stop | 空 | Windows Toast 完成提醒（bash → PowerShell → WinRT 三层，非 Windows 直接 exit 0） | true | 会话回合结束弹窗提醒；需要一次性 install.ps1 注册 AppUserModelId |

注意：settings.local.json 不进 git（.gitignore 已忽略），**钩子注册的持久化问题**：正式方案是将 SessionStart 注册写入一个新建的 `.claude/settings.json`（进 git，可提交）；Stop Toast 属个人偏好，注册留在 settings.local.json（不进 git）。删除 check-style.ps1 时须同步从 settings.local.json 移除其 PostToolUse 注册。

## 11. 机制⑥ 工具链：verify-compile.sh

一条命令完成编译验证（封装 unity-cli）：

1. `unity-cli system ping` 探测编辑器连接（未连接则提示并退出 2）
2. 必要时停 Play（避免编译中断播放态）
3. 清 Console
4. 刷新资源（触发 reimport + 编译）
5. 轮询等待编译完成（超时上限约 120s）
6. 读取错误数：0 → exit 0；非 0 → 输出错误列表 → exit 1

AI 侧约定：改完 `.cs` 必须跑 `bash .claude/verify-compile.sh`，规则文件与技能正文都引用此命令。

## 12. CLAUDE.md 重写方案（宗旨型，对齐 client-ai）

已裁决：CLAUDE.md 大量参考 `client-ai/CLAUDE.md` 的写法——常驻层放**行为准则与通用原则（宗旨）**，不放项目细节和做法。项目细节全部在 rules（按路径加载）与 skills（按需调用）；「动手前先查技能」的元纪律由 session-start 钩子注入的元技能承担，不靠 CLAUDE.md 罗列目录结构。

新 CLAUDE.md 章节骨架（正文中文，与 client-ai 的 8.5 KB 同量级，约 6-8 KB）：

| 节 | 内容 | 来源 |
|---|---|---|
| 交流语言 | 全程中文（硬性，保留） | 现有 CLAUDE.md |
| Think Before You Code | 不许假设、不许糊弄混乱、摆出取舍；不确定先问；定位 Bug 先查新改动、不许未经核实甩锅环境 | client-ai §1 改写 |
| Explain With Concrete Cases | 非平凡说明必带真实案例（文件+行号 / 输入输出对 / 前后对比）；抽象只做注解 | client-ai §2 改写 |
| Goal-Driven Execution | 任务改写成可验证目标，循环到验证通过；计划只锁逻辑，不假设 API 签名 | client-ai §3 改写 |
| Review Before You Build | 写码前按单一职责 / 干净边界 / 单向依赖 / 拆分充分四条自审结构 | client-ai §4 改写 |
| Explore Beyond the Scope | 不只改工单范围，每次改动顺手让结构变好 | client-ai §5 改写 |
| 硬性禁令（五条铁律） | 每条一行、零解释零指针：禁原生 Task（用 UniTask）/ 禁协程 / 禁 LINQ / 运行时禁反射 / 异步链必须有生命周期所有者 | 现有 CLAUDE.md 铁律压缩 |
| Coding Standards（通用原则） | 性能 / 日志 / 设计 / 可读性 / 数据与状态 / 异步 / 文件组织，各 2-4 条原则句（如「热路径不得用高成本抽象」「禁止 fire-and-forget」「组合优于继承」「三次法则」） | client-ai §6 改写 |
| 验收工具（一两行） | 编译验证 `bash .claude/verify-compile.sh`；unity-cli 见规则 tooling/unity-cli.md | 压缩为一行 |

移出/删除：

- unity-cli 详细段落 → `rules/tooling/unity-cli.md`（全局规则承接）
- AI 工程体系路由表 → 删除；元纪律由三期 session-start 钩子注入 using-project-skills 承担。过渡期（一、二期）CLAUDE.md 只留一行「动手前先查 `.claude/skills/` 项目技能」，钩子上线后删除该行
- 「详见 Docs/项目架构与技术栈.md」保留单行指针（成本可忽略）

常驻成本取舍：字节数比原方案大（~7 KB），但内容全是行为准则——这是「每次都付得起」的那一层；业务规则零常驻，细节靠渐进披露。

## 13. 分期计划

每期独立提交、独立可回退：

| 期 | 内容 | 交付物 |
|---|---|---|
| 一期·打底 | rules 14 篇（含两篇 Docs 规范并入）+ memory（MEMORY.md/traps-authoring/6 个 traps/feature-map 初始域）+ verify-compile.sh + CLAUDE.md 重写（宗旨型）+ 删除两篇旧规范 | 渐进披露生效，坑进 git，常驻层换血 |
| 二期·流程 | using-project-skills 元技能 + 三档开发流（fu-quick-help / fu-brainstorming-quick / fu-new-feature-dev）+ 业务技能（fgui-edit / unity-cli-usage） | 开发流可用 |
| 三期·治理收尾 | 治理技能（fu-code-review / fu-doc-sync / fu-rules-from-review）+ 5 个 agents + session-start 钩子 + settings.json（SessionStart 注册进 git）+ 删除 Docs/superpowers/（本设计文档先迁入 .claude/plans/）+ 删除 check-style.ps1 | 完整六类机制，superpowers 产物清空 |

备注：Stop Toast 提醒为可选项，放三期最后，若需要个人 install.ps1 步骤较多可再单独裁决。

## 14. 留给实施者的决定

以下事项设计文档不做死，实施时自行判断：

- **paths: 语义首日实测（一期第一个任务，硬性前置）**：写一条临时规则验证「带 `paths:` 的规则是否命中才加载、无 `paths:` 是否每次常驻」。本文档第 6 节的路由表全部建立在这两个语义假设上，实测不符则先修正路由设计再铺开。
- **元技能与 superpowers 元技能的共存口径**：superpowers 插件自身也经 SessionStart 注入「动手前先查技能」的元指令（插件级，无法关闭）。using-project-skills 正文必须写明优先级链：用户显式指令 > 项目技能（fu-\*）> 默认行为；项目流程一律走 fu-\*，不调用 superpowers 流程技能。
- 各 rules 文件的具体行数与详略（原则：AI 当场要用才写，Docs 引用兜底）
- feature-map 初始域文件数量（按模块真实分布定，不必与本文档列举一致）
- fu-\* 三档正文的具体阶段编排可参考 client-ai 实仓技能文件裁剪，Phase 数量不必死守 7/5/13
- verify-compile.sh 的 unity-cli 具体子命令序列（以 `unity-cli tool list` 实测为准）
- Stop Toast 是否最终落地（三期可选项）

## 15. 与现有资产的兼容清单

| 现有资产 | 处置 |
|---|---|
| `.claude/commands/luban-excel.md`、`polish-module.md` | 保留不动 |
| `.claude/skills/luban-excel` | 保留不动 |
| `.claude/hooks/check-style.ps1` + settings.local.json 里的 PostToolUse 注册 | **删除**（检查项 m_ 前缀/Async 后缀/E 前缀移交 fu-code-review） |
| `.claude/settings.local.json` | 保留；追加钩子注册（注意其中内容不进 git） |
| `Docs/代码风格规范.md` | **删除**，内容并入 `rules/coding/coding-standards.md`（单一事实源） |
| `Docs/Git提交规范.md` | **删除**，内容并入 `rules/coding/git-conventions.md`（单一事实源） |
| `Docs/网络模块学习笔记/` | 保留原位不动（个人学习记录，与 AI 体系无关） |
| `Docs/项目架构与技术栈.md` | 保留原位；feature-map 初始内容的展开来源 |
| `Docs/superpowers/`（specs 38 + plans 36，共 74 篇） | **整体删除**；其中大量决策已被后续提交推翻，留着会被 AI 误搜成现行规范。本设计文档先迁入 `.claude/plans/`（今后 fu-\* 流程的设计/计划产物固定放这里，对齐 client-ai） |
| 本机个人记忆 30 条 | 筛选可迁移条目入 traps 后继续保留在个人层（后续个人会话记忆仍用原机制） |
