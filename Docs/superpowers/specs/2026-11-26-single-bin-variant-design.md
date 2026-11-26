# 配置表数据格式收敛为单 bin 变体——设计文档

日期：2026-11-26
状态：已批准（用户确认：接受产物 diff 二进制化）

## 背景与 Why

配置管线长期维持 json/bin 双数据变体（客户端 json 开发/bin 发布；服务端对称）。三轮灵魂拷问后证伪其必要性：

1. **使用率 0%**：客户端业务表 `.bytes` 全 git 历史零入库；服务端 bin 链从未端到端工作（90d42701 修部署链前）也从未真实使用；当前三个运行时全部跑 json 变体。
2. **成本被实测标定**（删宏改造失败）：bin 不是"另一种文件格式"，而是牵动生成代码委托签名（ByteBuf vs JSONNode）→ 手写 Loader 返回类型 → 解析分部 → 宏的**整条代码链形态**。双变体 = 15+ 处接缝双份维护，配对错误 = 编译失败或运行期事故。
3. **json 变体的核心价值（产物可读/diff）是伪需求**：配置的真相源是 Excel（可读可 diff），数据产物只是编译产物——如同没人要求 DLL 必须可读。用户已确认接受产物 diff 二进制化。
4. **bin 的发布期收益（体积/解析/防扒）与 json 的开发期收益（可读）不再互补**：单 bin 直接兑现发布收益，开发期可读性由 Excel 承担。

目标：**双端只保留 bin 变体，结构性地消灭双份维护税与变体配对陷阱**。

## Goals / Non-goals / Anti-goals

- **Goals**：删除 json 变体全套（脚本/模板/宏/双 Loader/双 Parse）；产物全 bin；维护接缝归一。
- **Non-goals**：不动 Excel 源表工作流；不动 YooAsset tag 机制；不改 Luban 核心管线。
- **Anti-goals**：产物 diff 二进制化后排障链路缺失——必须保证"对比两次配置数据差异"仍有工具路径（经 Excel diff 或反解工具）。

## Approaches（已裁决）

| 方案 | 双份税 | 产物 diff | 一次性工程量 | 裁决 |
|---|---|---|---|---|
| A 保持现状（双双变体） | 持续全额 | json 可读 | 0 | 否——税在付收益 0% |
| B 砍服务端 bin | 客户端税照付 | json 可读 | 极小 | 否——半吊子 |
| **C 双端单 bin（选定）** | **归零** | 二进制 | ~1 天 | ✓ 结构性消灭变体问题 |

C 的唯一风险（Luban 升级改 ByteBuf 布局时排障更黑盒）已接受。

## 改动清单（recon 实测 12 项）

### 生成脚本层
1. 删 `Config/gen-client-json.{bat,sh}`、`Config/gen-server-json.{bat,sh}`（4 文件）
2. `Config/gen-client-bin.{bat,sh}` 头注释更新（bin 即唯一变体，去掉"变体"表述）
3. `Tools/CleanL10nKeys/clean-l10n-keys-apply.{bat,sh}` 提示文案里的 gen-client-json 改 gen-client-bin（**bat 与 sh 两个都要改**）

### 工作流文档层（builder check 补充的遗漏项）
3a. `.claude/commands/luban-excel.md`（1 处）与 `.claude/skills/luban-excel/SKILL.md`（3 处）的 `gen-client-json.bat` 引用改 `gen-client-bin.bat`——**不改则 AI 配置表工作流会调用已删脚本**
3b. `Docs/项目架构与技术栈.md`（2 处：gen-client-json / gen-server-json）同步
（`.superpowers/sdd/` 与 `Docs/superpowers/plans/` 下的历史记录不动）

### Luban 源码层
4. 删模板目录 `cs-simple-json/`、`cs-dotnet-json/`（条件生成函数 bean_needs_* 在 cs-bin 已有，无损失）
5. `build-luban` 无需改（构建全量模板）

### 客户端运行时层
6. `HotfixLauncher.cs`：删 6 处宏点（恒 bin→条件编译无意义），保留 BinaryJsonConfigLoader 改名 ConfigLoader、CreateEnglishTableLoadFunc 的 ByteBuf 版保留，删 SimpleJSON 版；using SimpleJSON 删
7. `LaunchLocalization.Parse.cs`：删 ParseJson + using SimpleJSON，恒 ParseBin；`LaunchLocalization.cs:103` 宏点删
8. AOT 表产物 `Resources/LaunchLocalizationText/*.json` → `*.bytes`（gen 重生成；Resources.Load 不带扩展名，无代码改动）

### 服务端运行时层
9. `ConfigComponent.cs`：删 JsonElement Loader，恒 ByteBuf Loader（单签名无重载歧义）

### 工具层
10. `Tools/CleanL10nKeys/src/CleanL10nKeys.py`：`CONFIG_DIR.glob('*.json')` → `*.bytes`；`SELF_REF_JSON_RE` 改 `.bytes`；提示文案 gen-client-json → gen-client-bin。
    **实测依据**：bin（Luban ByteBuf）中字符串为 UTF-8 明文（已验证 `achievement_name_01` 可在 `.bytes` 中直接检出），故现有 `read_text(encoding='utf-8', errors='ignore')` + `TOKEN_RE` 扫描策略对 .bytes 同样有效，**无需重写解析逻辑，只改 glob 与正则后缀**。

### Editor 层
11. `ConfigImporter.cs`：双菜单（Json/Bin）→ 单菜单「导出配置表」（FuMenuPriority 两个常量收拢为一个）；ENABLE_BINARY_CONFIG 符号逻辑删（恒 bin 无需符号）

### 文档层
12. `Config/README.md`（json 提及 10 处）、`Tools/Luban/README.md`（4 处）、`Localization/README.md` 变体描述同步

### 产物与宏
13. 全部数据产物 json→bytes（gen-client-bin + gen-server-bin 重生成）；`ENABLE_BINARY_CONFIG` 从 ProjectSettings 删除（当前未写入，确认即可）

## 排障链路保底（anti-goal 兑现）

产物 diff 二进制化后，对比配置差异的路径 = Excel diff（真相源）或临时写 bin 反解小工具（ByteBuf 布局简单：varint 记录数 + 逐条 length-prefixed string）。README 记录此工作流。

## 验证

1. build-luban → gen-client-bin / gen-server-bin 全绿
2. 产物核对：双端 Data/ 全 .bytes、Localization .bytes、AOT .bytes；收集器 Config_Data 组是目录级收集（`CollectPath: Assets/Bundles/Config/Data`），自动含新形态，无需改
3. CleanL10nKeys preview 跑通（扫 .bytes）
4. unity-cli 编译 0 错误；**Play 探针验证配置加载 + 本地化取词**（客户端 bin 路径首次真实运行；需重建临时探针——此前的探针已删除）
5. 服务端 build + 启动（服务端 bin 链首次真实运行）
6. git diff 全量核对无夹带

## 执行风险（builder check 确认）

1. **客户端 bin 路径从未真实运行**（历史全 json 变体验证）——验证步骤 4 首次覆盖，需重建探针。
2. **服务端 bin 变体从未编译过**——ConfigComponent 单 Loader 化后必须与 `cs-dotnet-bin` 生成的 ByteBuf 委托签名配对，验证步骤 5 覆盖。
3. **AOT Resources 旧 .json 清理**：依赖 Luban FileCleaner 对 data target 的默认清理（`LocalizationAotSplitPostProcess` 头注释声明"FileCleaner 自动清旧变体与删语种残留"），gen 后须核对目录内无 .json 残留（否则 Resources.Load 同名歧义）。

## 已确认无问题（builder check 实测）

- `cs-bin/`、`cs-dotnet-bin/` 模板各含完整 bean/table/tables 三件套，条件生成（bean_needs_*）在 cs-bin 已具备，删 json 模板无损失
- 服务端 `FuFramework.Config.csproj` 的 glob 已含 `Data\*.bytes`（90d42701）
- YooAsset 收集器 Localization 组恒 `.bytes`（本次不动）
- `FuMenuPriority` 的 CONFIG_EXPORT_JSON/BIN 两常量存在，可收拢为一个
- 仓库中无其他按 `.json` 后缀扫描配置产物 的运行时/Editor 工具（CleanL10nKeys 是唯一）
- 未使用的上游模板（cs-bean/cs-editor-json/cs-newtonsoft-json）不在本次范围，保留不动

## 工作量

约 1 天（含 CleanL10nKeys 改造、双端 bin 链首次真实验证、文档同步）。
