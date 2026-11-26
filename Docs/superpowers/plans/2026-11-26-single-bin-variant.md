# 配置表数据格式收敛为单 bin 变体 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 删除 json 数据变体全套（脚本/模板/宏/双 Loader/双解析），双端配置产物只保留 bin（`.bytes`），结构性消灭变体配对陷阱与双份维护税。

**Architecture:** Luban 侧删 `cs-simple-json`/`cs-dotnet-json` 模板与 4 个 json 生成脚本；手写侧三处变体分支（HotfixLauncher、LaunchLocalization.Parse、ConfigComponent）恒取 bin 分支并删宏；工具侧 CleanL10nKeys 扫描后缀 `.json`→`.bytes`；最后重生成双端产物并做首次 bin 链路运行时验证。

**Tech Stack:** Luban（配置生成）、Unity 2022.3 + HybridCLR（热更）、.NET 8（服务端）、YooAsset 3.0.5、unity-cli（编辑器自动化）。

**Spec:** `Docs/superpowers/specs/2026-11-26-single-bin-variant-design.md`

## Global Constraints

- **提交前必须询问用户**：每个 Task 的提交步骤都是「向用户列清单 → 确认后提交」，绝不自动 commit。
- **CLAUDE.md 代码铁律**：禁原生 `Task`（用 UniTask，服务端为 .NET 无此约束）、禁协程、禁 LINQ、运行时禁反射。
- **缩进 Tab**（.cs 文件），编辑空白用脚本/精确 Edit，勿整文件重写。
- gen 脚本无 BOM、纯 ASCII（中文只出现在注释时确认编辑器编码安全，约定为英文注释）。
- 产物入库形态：业务表 `.bytes`、本地化拆分 `.bytes`、AOT 表 `.bytes`（本次改造后全部二进制）。
- 历史记录不动：`.superpowers/sdd/`、`Docs/superpowers/plans/` 下既有文件保留原样。

## Review Focus

- **EditorSimulateMode 下 `.bytes` 资产的加载**：编辑器模拟模式走 AssetDatabase，`.bytes` 必须仍被识别为 `TextAsset`（当前 `tblocalization_*.bytes` 已验证可行，业务表 `.bytes` 首次走这条路）；探针需覆盖。
- **`Resources.Load` 同名歧义**：AOT 表从 `.json` 切 `.bytes` 后若旧 `.json` 未清理，`LaunchLocalization.LoadLanguageRows` 的 `Resources.Load<TextAsset>("LaunchLocalizationText/tblocalizationaot_X")` 可能命中旧文件；生成后必须核对目录内只有 `.bytes`。
- **英语兜底表懒加载**：`LoadEnglishTableAsync` 路径硬编码 `.bytes` 且 `CreateEnglishTableLoadFunc` 返回 `Func<UniTask<ByteBuf>>`——与 `TbLocalization` 构造签名（bin 变体）配对，错配即编译失败（此即本次改造要消除的那类陷阱）。
- **服务端缺失文件报错**：`ConfigComponent.Loader` 只读 `Data/{file}.bytes`，若 Data 目录里残留 `.json` 而实际生成的是 `.json`（跑错脚本）会 FileNotFound；单 bin 后跑错脚本的场景消失，但部署目录需核对。
- **CleanL10nKeys 误报**：bin 二进制中 ASCII token 通过 `errors='ignore'` 文本扫描提取，理论上会吸入二进制噪声 token；需用 `--preview` 对比改造前后报告差异是否仅为预期变化。

---

### Task 1: 删除 json 生成脚本 + 更新 bin 脚本头注释

**Files:**
- Delete: `Config/gen-client-json.bat`、`Config/gen-client-json.sh`、`Config/gen-server-json.bat`、`Config/gen-server-json.sh`
- Modify: `Config/gen-client-bin.bat:4`、`Config/gen-client-bin.sh:3`、`Config/gen-server-bin.bat`、`Config/gen-server-bin.sh`（头注释去掉"binary variant"表述）

**Interfaces:**
- Produces: 仓库中仅存 `gen-client-bin.{bat,sh}` 与 `gen-server-bin.{bat,sh}` 四个生成入口；后续任务与文档统一引用 bin 脚本。

- [ ] **Step 1: 删除 4 个 json 脚本**

```bash
git rm Config/gen-client-json.bat Config/gen-client-json.sh Config/gen-server-json.bat Config/gen-server-json.sh
```

- [ ] **Step 2: 更新 bin 脚本头注释**

`Config/gen-client-bin.bat` 第 4 行 `rem  Client config generation script (binary variant: .bytes data, cs-bin target)` 改为 `rem  Client config generation script (.bytes data, cs-bin target)`；`Config/gen-client-bin.sh` 第 3 行同理。`gen-server-bin.{bat,sh}` 头注释中的 "bin variant" 表述同样简化为唯一变体描述。

- [ ] **Step 3: 验证脚本清单**

Run: `ls Config/gen-*`
Expected: 仅 4 个文件（client-bin/server-bin × bat/sh）

- [ ] **Step 4: 停——向用户列清单，确认后提交**

---

### Task 2: 删除 json 模板目录 + 重建 Luban

**Files:**
- Delete: `Tools/Luban/source/src/Luban.CSharp/Templates/cs-simple-json/`、`Tools/Luban/source/src/Luban.CSharp/Templates/cs-dotnet-json/`
- 保留不动：`cs-bin/`、`cs-dotnet-bin/`（各含 bean/table/tables.sbn 且已带条件生成 `bean_needs_*`）、未使用的上游模板 `cs-bean/`、`cs-editor-json/`、`cs-newtonsoft-json/`

**Interfaces:**
- Produces: 模板集合只剩 bin 两套；`build-luban` 产物 `bin/Templates/` 不再含 json 目录。

- [ ] **Step 1: 删除两个 json 模板目录**

```bash
git rm -r Tools/Luban/source/src/Luban.CSharp/Templates/cs-simple-json Tools/Luban/source/src/Luban.CSharp/Templates/cs-dotnet-json
```

- [ ] **Step 2: 重建 Luban**

Run: `cd Tools/Luban && cmd //c ".\build-luban.bat ci"`
Expected: `0 个错误`，耗时数秒

- [ ] **Step 3: 验证 bin 模板仍完整**

Run: `ls Tools/Luban/bin/Templates/`
Expected: 含 `cs-bin/`、`cs-dotnet-bin/`，不含 `cs-simple-json/`、`cs-dotnet-json/`

- [ ] **Step 4: 停——向用户列清单，确认后提交**

---

### Task 3: 客户端 HotfixLauncher 单 bin 化

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/HotfixLauncher.cs`（5 处宏：using 区 35 行、LoadAsync 调用 211 行、英语兜底注释 252 行、CreateEnglishTableLoadFunc 267 行、Loader 定义 295 行）

**Interfaces:**
- Consumes: Task 2 生成的 `cs-bin` 版 `TableManager.LoadAsync(Func<string, UniTask<ByteBuf>>)` 与 `TbLocalization(Func<UniTask<ByteBuf>>)`
- Produces: 单一定义 `private static async UniTask<ByteBuf> ConfigLoader(string file)`（原 `BinaryJsonConfigLoader` 改名）与 `private static Func<UniTask<ByteBuf>> CreateEnglishTableLoadFunc(TextAsset)`；`LoadConfigAsync` 内调用 `await tableManager.LoadAsync(ConfigLoader)`

- [ ] **Step 1: 删 using 区宏**

`#if ENABLE_BINARY_CONFIG using Luban; #else using SimpleJSON; #endif` → 只保留 `using Luban;`

- [ ] **Step 2: 删 LoadConfigAsync 内的 Loader 选择宏**

```csharp
// 原：
#if ENABLE_BINARY_CONFIG
			await tableManager.LoadAsync(BinaryJsonConfigLoader);
#else
			await tableManager.LoadAsync(JsonConfigLoader);
#endif
// 改：
			await tableManager.LoadAsync(ConfigLoader);
```

- [ ] **Step 3: 删 CreateEnglishTableLoadFunc 宏，只留 ByteBuf 版**

保留 `Func<UniTask<ByteBuf>> CreateEnglishTableLoadFunc(TextAsset textAsset) { var bytes = textAsset.bytes; return () => UniTask.FromResult(ByteBuf.Wrap(bytes)); }`，删除 `#if/#else` 与 SimpleJSON 版。

- [ ] **Step 4: 删 Loader 定义宏，只留 ByteBuf 版并改名 ConfigLoader**

保留原 `BinaryJsonConfigLoader` 方法体（本地化拦截读 `GetLocalizationPath(..., ".bytes")` + `ByteBuf.Wrap(textAsset.bytes)`；业务表读 `GetConfigPath(file)` + `ByteBuf.Wrap`），方法名改 `ConfigLoader`，删 `#if/#else` 与 `JsonConfigLoader` 全部分支。同步更新 252 行附近的注释（"json 变体…"整段删除，改为一句话说明产物为 bin）。

- [ ] **Step 5: 全文件搜残留**

Run: `grep -n "ENABLE_BINARY_CONFIG\|SimpleJSON\|JSONNode\|JsonConfigLoader\|BinaryJsonConfigLoader" Unity/Assets/Scripts/Hotfix/HotfixLauncher.cs`
Expected: 无输出

- [ ] **Step 6: 编译验证**

Run: `unity-cli raw execute_menu_item --json '{"menuPath":"Assets/Refresh"}'` 后读 `Library/Bee` 最新 Tundra 日志
Expected: `Tundra build success`（若 Hotfix 报 CS0407 签名错配，说明 Loader 返回类型与生成代码不配对，必须修好再继续）

- [ ] **Step 7: 停——向用户列清单，确认后提交**

---

### Task 4: AOT 侧单 bin 化

**Files:**
- Modify: `Unity/Assets/Scripts/AOT/Launch/Localization/LaunchLocalization.cs:103-107`（宏点）
- Modify: `Unity/Assets/Scripts/AOT/Launch/Localization/LaunchLocalization.Parse.cs`（删 `ParseJson` 与 `using SimpleJSON`，保留 `ParseBin` 与 `using Luban`）

**Interfaces:**
- Produces: `LaunchLocalization.LoadLanguageRows` 恒调 `ParseBin(textAsset.bytes, rows)`；分部仅保留 `ParseBin(byte[], Dictionary<string,string>)`

- [ ] **Step 1: LaunchLocalization.cs 删宏恒 ParseBin**

```csharp
// 原：
#if ENABLE_BINARY_CONFIG
			ParseBin(textAsset.bytes, rows);
#else
			ParseJson(textAsset.text, rows);
#endif
// 改：
			ParseBin(textAsset.bytes, rows);
```

- [ ] **Step 2: 分部文件删 ParseJson**

`LaunchLocalization.Parse.cs`：删 `using SimpleJSON;`、删整个 `ParseJson` 方法与 json 相关 XML 注释；类注释改为"bin 变体用 Luban.ByteBuf 按记录数 + 2 字段顺序读取"。

- [ ] **Step 3: 残留核对**

Run: `grep -rn "ENABLE_BINARY_CONFIG\|ParseJson\|SimpleJSON" Unity/Assets/Scripts/AOT/Launch/Localization/`
Expected: 无输出

- [ ] **Step 4: 编译验证**

Run: Refresh 后核对 Tundra 日志
Expected: `Tundra build success`

- [ ] **Step 5: 停——向用户列清单，确认后提交**

---

### Task 5: 服务端 ConfigComponent 单 bin

**Files:**
- Modify: `Server/FuFramework.Config/ConfigComponent.cs:31-42`

**Interfaces:**
- Consumes: Task 2 的 `cs-dotnet-bin` 生成代码 `TableManager.LoadAsync(Func<string, bool, Task<ByteBuf>>)`
- Produces: 仅一个 `private static async Task<ByteBuf> Loader(string file, bool tag)`，读 `Data/{file}.bytes`

- [ ] **Step 1: 删 JsonElement Loader**

删除 `private static async Task<JsonElement> Loader(string file)` 方法（读 `Data/{file}.json`）及不再使用的 `System.Text.Json` using（若无其他引用）。

- [ ] **Step 2: 验证绑定无歧义**

Run: `grep -n "LoadAsync(Loader)" Server/FuFramework.Config/ConfigComponent.cs`
Expected: 1 处，且文件内只有一个 `Loader` 重载

- [ ] **Step 3: 编译验证**

Run: `cd Server && dotnet build FuFramework.Config/FuFramework.Config.csproj -c Debug`
Expected: `0 个错误`（若出现 CS0407/CS1503，说明生成代码仍是 json 变体——先跑 gen-server-bin 再编译）

- [ ] **Step 4: 停——向用户列清单，确认后提交**

---

### Task 6: ConfigImporter 单菜单

**Files:**
- Modify: `Unity/Assets/Editor/FuFramework/Config/ConfigImporter.cs`（enum 20 行、符号常量 36 行、双菜单 41/50 行、ExportConfig 60-90 行）
- Modify: `Unity/Assets/Editor/FuFramework/Common/FuMenuPriority.cs:301-306`（两常量收拢为一个）

**Interfaces:**
- Produces: 单一菜单 `FuFramework/配置表/导出配置表`（priority `FuMenuPriority.CONFIG_EXPORT`）；无参 `ExportConfig()` 恒调 `gen-client-bin.{bat,sh}`；不再触碰 ScriptingDefineSymbols

- [ ] **Step 1: FuMenuPriority 收拢常量**

删除 `CONFIG_EXPORT_JSON`、`CONFIG_EXPORT_BIN`，新增 `public const int CONFIG_EXPORT = CONFIG_BASE + 0;`（保留注释「导出配置表」）。

- [ ] **Step 2: ConfigImporter 单菜单化**

删 `EDataTarget` 枚举与 `ENABLE_BINARY_CONFIG_SYMBOL` 常量；两个 `[MenuItem]` 方法合并为一个 `ExportConfigToBin()` 改名 `ExportConfig()` 挂在 `CONFIG_EXPORT`；`ExportConfig(EDataTarget target)` 改无参、脚本名硬编码 `gen-client-bin.{bat,sh}`、删 ScriptingDefineSymbols 增删块；成功对话框文案不变。

- [ ] **Step 3: 残留核对**

Run: `grep -n "EDataTarget\|ENABLE_BINARY_CONFIG\|CONFIG_EXPORT_JSON\|CONFIG_EXPORT_BIN\|导出配置表—" Unity/Assets/Editor/FuFramework/Config/ConfigImporter.cs Unity/Assets/Editor/FuFramework/Common/FuMenuPriority.cs`
Expected: 无输出

- [ ] **Step 4: 编译验证**

Run: Refresh 后核对 Tundra 日志
Expected: `Tundra build success`

- [ ] **Step 5: 停——向用户列清单，确认后提交**

---

### Task 7: CleanL10nKeys 支持 .bytes

**Files:**
- Modify: `Tools/CleanL10nKeys/src/CleanL10nKeys.py`（43 行 CONFIG_DIR 注释、56 行 SELF_REF 正则、114 行 glob、202 行提示文案）
- Modify: `Tools/CleanL10nKeys/clean-l10n-keys-apply.{bat,sh}`（头注释中的 gen 脚本名）

**Interfaces:**
- Produces: `collect_reference_tokens()` 扫描 `CONFIG_DIR.glob('*.bytes')`；`SELF_REF_RE = re.compile(r'^tblocalization.*\.bytes$')`

- [ ] **Step 1: 改 CONFIG_DIR 注释与 SELF_REF 正则**

43 行注释 `# exported business tables (json variant)` → `# exported business tables (bin variant, Luban ByteBuf — strings are plain UTF-8)`；56 行 `SELF_REF_JSON_RE` 改名 `SELF_REF_RE` 并适配 `.bytes`（同时更新其引用处 115 行）。

- [ ] **Step 2: 改扫描 glob**

114 行 `for path in CONFIG_DIR.glob('*.json'):` → `for path in CONFIG_DIR.glob('*.bytes'):`。`feed()` 保持 `read_text(encoding='utf-8', errors='ignore')`（实测 bin 内字符串为 UTF-8 明文，策略不变）。

- [ ] **Step 3: 改提示文案**

202 行 `请执行 gen-client-json.bat / gen-client-bin.bat` → `请执行 gen-client-bin.bat`；两个 apply 脚本头注释同步。

- [ ] **Step 4: 残留核对**

Run: `grep -rn "\.json\|gen-client-json\|SELF_REF_JSON" Tools/CleanL10nKeys/`
Expected: 无输出

- [ ] **Step 5: preview 验证（**须在 Task 8 产物重生成之后执行**——当前 Data 目录还是 `.json`，本步此时跑会因无 `.bytes` 而报错；若按顺序执行，跳过本步，在 Task 8 完成后回来补跑）**

Run: `Tools/CleanL10nKeys/clean-l10n-keys-preview.bat`（或等价入口）
Expected: 正常输出报告；"被引用 key" 数量与改造前 `--preview` 报告一致（吸噪可接受，但不应出现整表 key 被判为"未引用"）

- [ ] **Step 6: 停——向用户列清单，确认后提交**

---

### Task 8: 双端产物重生成 + 结构核对

**Files:**
- Regenerate: `Unity/Assets/Bundles/Config/Data/*`（json→bytes）、`Unity/Assets/Bundles/Config/Localization/*`（恒 bytes，内容为 bin）、`Unity/Assets/Resources/LaunchLocalizationText/*`（json→bytes）、`Server/FuFramework.Config/Data/*`（json→bytes）
- Regenerate code: `Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate/*`（cs-bin 变体）、`Unity/Assets/Scripts/AOT/Launch/Localization/AutoGen/*`、`Server/FuFramework.Config/Config/*`（cs-dotnet-bin 变体）

**Interfaces:**
- Produces: 双端全部数据产物为 `.bytes`；生成代码为 bin 委托签名；YooAsset 收集器 Config_Data 组（目录级收集）与 Localization 组（逐文件 .bytes）无需改动

- [ ] **Step 1: 客户端产物重生成**

Run: `cd Config && cmd //c ".\gen-client-bin.bat" < /dev/null`
Expected: 无 `run failed`；日志含「本地化表拆分完成」「AOT 本地化表拆分完成」

- [ ] **Step 2: 服务端产物重生成**

Run: `cd Config && cmd //c ".\gen-server-bin.bat" < /dev/null`
Expected: 无 `run failed`

- [ ] **Step 3: 产物扩展名核对**

Run: `ls Unity/Assets/Bundles/Config/Data/ | grep -v meta | head -5; ls Unity/Assets/Resources/LaunchLocalizationText/ | grep -v meta | head -5; ls Server/FuFramework.Config/Data/ | grep -v meta`
Expected: 全部 `.bytes`，无 `.json` 残留（含 AOT Resources 目录——见 Review Focus #2）

- [ ] **Step 4: 收集器核对**

Run: `grep -c "tblocalization_.*\.bytes" Unity/Assets/AssetBundleCollectorSetting.asset`
Expected: 6（六语种逐文件条目）；并确认 `Config_Data` 组仍为目录级 `CollectPath: Assets/Bundles/Config/Data`

- [ ] **Step 5: 生成代码签名核对**

Run: `grep -n "LoadAsync(Func" Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate/TableManager.cs Server/FuFramework.Config/Config/TableManager.cs`
Expected: 客户端 `UniTask<ByteBuf>`；服务端 `Task<ByteBuf>` 且带 `bool tag` 第二参

- [ ] **Step 6: 停——向用户列清单，确认后提交**

---

### Task 9: 运行时验证（客户端 Play 探针 + 服务端启动）

**Files:**
- Create（临时）: `Unity/Assets/Editor/FuFramework/Config/BinVariantRuntimeProbe.cs`（验证后删除）
- 验证对象: 配置表加载链路、本地化取词链路、服务端启动

**Interfaces:**
- Consumes: Task 8 的 bin 产物；`ConfigModule.Instance.GetConfig<TbSound>()`、`LocalizationModule.Instance.GetL10nText(key)`

- [ ] **Step 1: 写临时探针**

```csharp
// MenuItem("FuFramework/ConfigTools/BinProbe") 菜单，输出：
// 1) ConfigModule.Instance.GetConfig<TbSound>() 非空 + Get(6400001).GroupName
// 2) LocalizationModule.Instance.GetL10nText("common_start_game") 非空且非 [key] 占位
// 3) TbAchievement.Get(90000001) 可取（验证 bin 反序列化）
// 菜单名必须纯 ASCII（unity-cli 传中文路径会 GBK 乱码）
```

- [ ] **Step 2: 编译并进入 Play**

Run: `unity-cli raw execute_menu_item --json '{"menuPath":"Assets/Refresh"}'` → 核对 Tundra success → `unity-cli raw play_game`
Expected: 编译 0 错误；Play 正常进入（启动不抛异常即为配置加载成功）

- [ ] **Step 3: 执行探针并读输出**

Run: `unity-cli raw execute_menu_item --json '{"menuPath":"FuFramework/ConfigTools/BinProbe"}'`
然后从 `C:\Users\Administrator\AppData\Local\Unity\Editor\Editor.log` 读探针块
Expected: 三项均非空；`GetL10nText("common_start_game")` 返回「开始游戏」类译文（**这是 bin 数据链路首次真实运行**）

- [ ] **Step 4: 退出 Play，删探针**

Run: `unity-cli raw stop_game`；删除 `BinVariantRuntimeProbe.cs` 与 `.meta`

- [ ] **Step 5: 服务端启动验证**

Run: `cd Server && dotnet build FuFramework.Launcher/FuFramework.Launcher.csproj -c Debug`（先确保无运行中进程锁文件）→ 启动 `bin/app_debug` 下的 Launcher
Expected: 日志出现 `Load Config End...` 与 `== load success ==`；无 `FileNotFoundException`
（注：**服务端 bin 链首次真实运行**；若启动成功，需 notify 用户确认后再关闭）

- [ ] **Step 6: 停——向用户列清单，确认后提交**

---

### Task 10: 文档同步

**Files:**
- Modify: `Config/README.md`（生成脚本表 23-29 行、本地化节 161 行、条件生成节的变体表述等共约 10 处）
- Modify: `Tools/Luban/README.md`（4 处 json 变体描述）
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Localization/README.md`（2 处 `.json|bytes` 表述）
- Modify: `Docs/项目架构与技术栈.md`（2 处 gen-*-json 命令）
- Modify: `.claude/skills/luban-excel/SKILL.md`（3 处 gen-client-json 引用）
- Modify: `.claude/commands/luban-excel.md`（1 处）

**Interfaces:**
- Produces: 全仓活跃文档中无 `gen-client-json`/`gen-server-json`/`json 变体` 的陈旧引用

- [ ] **Step 1: Config/README 生成脚本表改单 bin**

删两行 json 变体表述；`gen-client-bin` 行标注「唯一数据变体」；「服务端双变体」说明段改写为「数据目录 Data（单 bin 变体）」；本地化节 `tblocalization_<语言>.<json|bytes>` → `.bytes`；补一句排障工作流（产物二进制，对比配置差异走 Excel diff 或 bin 反解）。

- [ ] **Step 2: Tools/Luban/README 定制表更新**

删 `cs-simple-json`/`cs-dotnet-json` 相关行；模板清单只剩 bin 两套。

- [ ] **Step 3: Localization/README 同步**

「扩展名跟随数据变体」相关表述改为恒 `.bytes`。

- [ ] **Step 4: 架构文档与 skill/command 命令更新**

`Docs/项目架构与技术栈.md` 两处 `gen-*-json` → `gen-*-bin`；`.claude/skills/luban-excel/SKILL.md` 与 `.claude/commands/luban-excel.md` 的生成命令改 `gen-client-bin.bat`。

- [ ] **Step 5: 残留核对**

Run: `grep -rn "gen-client-json\|gen-server-json" --include="*.md" . | grep -v "/.superpowers/\|/superpowers/plans/\|/Library/"`
Expected: 无输出（历史记录目录除外）

- [ ] **Step 6: 停——向用户列清单，确认后提交**

---

### Task 11: 最终全量核对

**Files:** 无新增

- [ ] **Step 1: 工作区核对**

Run: `git status --porcelain`
Expected: 仅本次改造相关文件；无夹带其他会话残留

- [ ] **Step 2: 全局残留扫描**

Run: `grep -rn "ENABLE_BINARY_CONFIG" Unity/Assets Server Unity/ProjectSettings --include="*.cs" --include="*.asset" | head`
Expected: 无输出

- [ ] **Step 3: 双端编译终验**

Run: 客户端 Refresh → Tundra success；服务端 `dotnet build Server/Server.sln`
Expected: 均 0 错误

- [ ] **Step 4: 停——向用户列最终清单，确认后提交（或合成一笔提交，由用户决定）**
