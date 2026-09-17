# AOT 前置本地化（TbLocalizationAOT）实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 新增独立本地化表 `TbLocalizationAOT`，数据产物入 `Assets/Resources/Config/`、代码产物入 `Assets/Scripts/AOT/Launch/Localization/`，支持 bin/json 双变体，并将启动流程全部硬编码文本接入。

**Architecture:** 复用 Luban 原生分组机制（`aot` 分组 + 独立 `aot` target）隔离生成；AOT 侧新增静态门面 `LaunchLocalization`（Resources 同步加载 + 生成 TableManager），经 `ENABLE_BINARY_CONFIG` 宏同构支持双数据格式；`ELanguage` 下沉 AOT 程序集共享；Hotfix 侧经 AOT 门面接线。

**Tech Stack:** Luban（源码在 `Tools/Luban/source/`）、openpyxl（Excel 修正）、UniTask、HybridCLR 双程序集（AOT/Hotfix）。

**Spec:** `Docs/superpowers/specs/2026-09-17-aot-localization-design.md`（执行本计划前必读）

## Global Constraints

- Unity 运行时铁律（CLAUDE.md）：禁 `Task`/`Coroutine`/LINQ/运行时反射，异步一律 `UniTask` 且必须有生命周期所有者。**Luban 工具源码（`Tools/Luban/`）不受此限**（构建期 .NET 工具，沿用其现有 LINQ 风格）。
- 生成代码（`TableManager.cs` / `TbLocalizationAOT.cs` / `LocalizationAOT.cs` / `LanguageKey.cs`）禁止手改，一切变更经 Excel + gen 脚本。
- 提交规范：Conventional Commits 中文描述，本计划所有提交加 `[AI]` 前缀；每次 git 操作前确认用户同意。
- bin/json 双变体由全局宏 `ENABLE_BINARY_CONFIG` 切换（`Config/ConfigImporter.cs` 管理）；bin 用 `Luban.ByteBuf`，json 用 `SimpleJSON.JSON`。
- gen 脚本必须 在 `Config/` 目录下运行；bat 末尾有 `pause`，脚本化执行用 `cmd /c "echo. | .\gen-client-bin.bat"` 喂回车。
- 类名冲突注意：`Hotfix.Game.Config.LanguageKey` 与 `AOT.Launch.Localization.LanguageKey` 同名。在同时 using 两命名空间的文件（如 `HotfixLauncher.cs`）中，AOT 侧必须用别名访问（`using AotL10N = AOT.Launch.Localization;`）。

---

### Task 1: Excel 表修正（改名 + key 列头）

**Files:**
- Rename: `Config/Excels/Local/L-Localization-AOT.xlsx` → `Config/Excels/Local/L-LocalizationAOT-aot-热更前.xlsx`
- Modify: 新文件第 1 行第 3 列 `v` → `key`

**Interfaces:**
- Produces: 表名 `TbLocalizationAOT`（导入器解析 `LocalizationAOT` 段）、分组 `aot`、key 列名 `key`（Task 3 生成链路与 `l10n.textFile.keyFieldName=key` 校验依赖）。

- [ ] **Step 1: 确认文件当前未被 git 跟踪**

Run: `git -C "D:/_WorkSpace/Unity/FuFramework2.0" status --porcelain -- "Config/Excels/Local/"`
Expected: `?? Config/Excels/Local/L-Localization-AOT.xlsx`（untracked，可直接重命名）。若为 `M`/`A` 状态（已跟踪），改用 `git mv` 并告知用户。

- [ ] **Step 2: 用 openpyxl 重命名并修正 key 列头**

```bash
python - <<'PY'
import openpyxl, os
base = r'D:/_WorkSpace/Unity/FuFramework2.0/Config/Excels/Local'
src  = os.path.join(base, 'L-Localization-AOT.xlsx')
dst  = os.path.join(base, 'L-LocalizationAOT-aot-热更前.xlsx')

wb = openpyxl.load_workbook(src)          # 可写模式
ws = wb.active
assert ws.cell(1, 3).value == 'v', f'第1行第3列不是 v: {ws.cell(1,3).value!r}'
ws.cell(1, 3).value = 'key'
wb.save(dst)
os.remove(src)
wb.close()

# 读回验证
wb2 = openpyxl.load_workbook(dst, read_only=True, data_only=True)
ws2 = wb2.active
rows = list(ws2.iter_rows(min_row=1, max_row=15, values_only=True))
wb2.close()
assert rows[0][:5] == ('##var', '#Description', 'key', 'is_code', 'ChineseSimplified'), rows[0][:5]
keys = [r[2] for r in rows[3:] if r and r[2]]
print('key 列头 OK, 数据 key 数:', len(keys))
print('\n'.join(keys))
PY
```

Expected: 输出 12 个 key（`aot_init_res_package`、`aot_get_res_version`、`aot_get_res_version_fail`、`aot_update_res_manifest`、`aot_update_res_manifest_fail`、`aot_req_remote_update_config_fail`、`aot_res_downloading`、`aot_res_download_fail`、`aot_res_load_fail`、`aot_res_loading_config`、`aot_res_loading_init_res`、`aot_update_dialog_ok_btn`）。数量不符时停下报告用户（表可能被更新过，以实际为准，勿擅自改数据）。

- [ ] **Step 3: Commit**

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
git add "Config/Excels/Local/L-LocalizationAOT-aot-热更前.xlsx"
git commit -m "[AI]feat: AOT 本地化表重命名为独立表 TbLocalizationAOT 并统一 key 列"
```

---

### Task 2: luban.conf 加分组与 target + Luban 源码微调 + 重建

**Files:**
- Modify: `Config/luban.conf`（groups 与 targets 各加一项）
- Modify: `Tools/Luban/source/src/Luban.CSharp/CodeTarget/CsharpL10NKeyCodeTarget.cs:105`（表名筛选）

**Interfaces:**
- Consumes: Task 1 的分组 `aot`（文件名第 3 段）。
- Produces: Luban target `aot`（Task 3 脚本以 `-t aot` 调用）；`cs-l10n-key` 能收集 `TbLocalizationAOT`。

- [ ] **Step 1: 修改 `Config/luban.conf`**

groups 数组追加一项、targets 数组追加一项（其余不动）：

```json
{
	"groups": 
	[
		{"names": ["c"],"default": true},
		{"names": ["s"],"default": true},
		{"names": ["aot"],"default": false}
	],
	"schemaFiles": 
	[
		{"fileName": "Defines","type": ""},
		{"fileName": "Excels/__tables__.xlsx","type": "table"},
		{"fileName": "Excels/__beans__.xlsx","type": "bean"},
		{"fileName": "Excels/__enums__.xlsx","type": "enum"}
	],
	"dataDir": "Excels",
	"targets": 
	[
		{"name": "client","manager": "TableManager","groups": ["c"],"topModule": "Hotfix.Game.Config"},
		{"name": "server","manager": "TableManager","groups": ["s"],"topModule": "FuFramework.Config"},
		{"name": "all","manager": "TableManager","groups": ["c","s"],"topModule": "cfg"},
		{"name": "aot","manager": "TableManager","groups": ["aot"],"topModule": "AOT.Launch.Localization"}
	]
}
```

- [ ] **Step 2: 微调 `CsharpL10NKeyCodeTarget.cs` 的 `CollectKeys` 表名筛选**

将（约 105 行）：

```csharp
        // 筛选出多语言表
        var l10NTables = tables.Where(t => t.Name == "TbLocalization").ToList();
```

改为：

```csharp
        // 筛选出多语言表（含 AOT 前置本地化表）
        var l10NTables = tables.Where(t => t.Name is "TbLocalization" or "TbLocalizationAOT").ToList();
```

- [ ] **Step 3: 重建 Luban**

Run: `& "D:\_WorkSpace\Unity\FuFramework2.0\Tools\Luban\build-luban.bat" ci`
Expected: `已成功生成。 0 个警告 0 个错误`。

- [ ] **Step 4: Commit**

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
git add Config/luban.conf Tools/Luban/source/src/Luban.CSharp/CodeTarget/CsharpL10NKeyCodeTarget.cs
git commit -m "[AI]feat: luban 新增 aot 分组与独立导出目标，LanguageKey 收集纳入 TbLocalizationAOT"
```

---

### Task 3: gen 脚本接入 AOT 段并生成、验证产物

**Files:**
- Modify: `Config/gen-client-bin.bat`（`pause` 前插入第二段命令）
- Modify: `Config/gen-client-json.bat`（同上）
- 生成产物（不入手写）：`Unity/Assets/Resources/Config/tblocalizationaot.{bytes,json}`、`Unity/Assets/Scripts/AOT/Launch/Localization/{TableManager,TbLocalizationAOT,LocalizationAOT,LanguageKey}.cs`

**Interfaces:**
- Consumes: Task 2 的 `-t aot` target。
- Produces: 生成 `TableManager`（命名空间 `AOT.Launch.Localization`，属性 `TbLocalizationAOT`，方法 `LoadAsync`；bin 版签名 `LoadAsync(Func<string, UniTask<ByteBuf>>)`、json 版 `LoadAsync(Func<string, UniTask<JSONNode>>)`，与 `HotfixLauncher.ConfigBufferLoader/ConfigLoader` 同构）；生成 `LocalizationAOT` bean（语言属性 `ChineseSimplified`...`Vietnamese`，同 Hotfix 版）；生成 `LanguageKey`（12 个 `aot_*` 常量）。Task 5/6 依赖这些类型与成员名。

- [ ] **Step 0: `FuFrameworkTableImporter` 新增 strictGroup 选项并重建（实施期裁定 R1）**

`Tools/Luban/source/src/Luban.Schema.Builtin/FuFrameworkTableImporter.cs`：

1. `ImportSetting` 类新增属性：

```csharp
        /// <summary>
        /// 严格分组模式：开启时，文件名无分组段的表跳过不导出（用于仅导出显式分组表的 target，如 aot）。
        /// </summary>
        public bool StrictGroup { get; set; }
```

2. `FormatImportSetting` 的返回对象追加一行：

```csharp
            StrictGroup         = DataUtil.ParseBool(EnvManager.Current.GetOptionOrDefault("tableImporter", "strictGroup", false, "false")),
```

3. `TryProcessFile` 中，在解析出 `tableInfo` 后（`ParseTableInfo` 返回非 null 之后、`CreateRawTable` 之前）插入：

```csharp
        // 严格分组模式：文件名无分组段（第3段非已定义分组）的表跳过不导出
        if (importSetting.StrictGroup && !importSetting.Groups.Any(g => g.Names.Contains(rawFullName.Split(new[] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries).ElementAtOrDefault(2)?.Trim(), StringComparer.OrdinalIgnoreCase)))
            return false;
```

4. 重建：`& "D:\_WorkSpace\Unity\FuFramework2.0\Tools\Luban\build-luban.bat" ci`，预期 `已成功生成。 0 个警告 0 个错误`。

> 裁定背景：bat 不传 `tableImporter.target` 时 `ExportTarget=null`，带分组段的表被静默跳过；且 `CreateRawTable.Groups` 恒为空（空=属于所有分组），无组段表会全量涌入 aot target。strictGroup 与 `tableImporter.target=aot` 配合解决两者。

- [ ] **Step 1: `gen-client-bin.bat` 追加第二段（插在 `pause` 之前）**

```bat
dotnet ../Tools/Luban/bin/Luban.dll ^
    -t aot ^
    -d bin ^
    -c cs-bin ^
    -c cs-l10n-key ^
    -x outputDataDir=../Unity/Assets/Resources/Config ^
    -x cs-bin.outputCodeDir=../Unity/Assets/Scripts/AOT/Launch/Localization/Generate ^
    -x cs-l10n-key.outputCodeDir=../Unity/Assets/Scripts/AOT/Launch/Localization/LanguageKey ^
    -x tableImporter.name=fuframework ^
    -x tableImporter.target=aot ^
    -x tableImporter.strictGroup=true ^
    -x l10n.provider=fuframework ^
    -x l10n.textFile.keyFieldName=key ^
    -x l10n.textFile.path=./Excels/Local/ ^
    --conf ./Luban.conf
```

- [ ] **Step 2: `gen-client-json.bat` 追加第二段（插在 `pause` 之前）**

与 Step 1 唯一差异是第 2、3 个参数：`-d json` 与 `-c cs-simple-json`（其余行逐字相同）。

- [ ] **Step 3: 跑 bin 变体并验证**

Run: `cd "D:/_WorkSpace/Unity/FuFramework2.0/Config"; cmd /c "echo. | .\gen-client-bin.bat"`

验证输出日志（关键证据）：
- client 段 `[overwrite]` 列表**不含** `TbLocalizationAOT.cs`（client 隔离）
- aot 段 `[overwrite]` 仅含：`.../AOT/Launch/Localization/Generate/TableManager.cs`、`TbLocalizationAOT.cs`、`LocalizationAOT.cs` 与 `.../AOT/Launch/Localization/LanguageKey/LanguageKey.cs`
- aot 段数据产物：`[new] ../Unity/Assets/Resources/Config/tblocalizationaot.bytes`
- 日志含 `收集导出到代码中的多语言key: 总共收集到 12 个多语言Key`（数量与 Task 1 实际 key 数一致）
- 无 `ERROR`、无 `缺少 'is_code' 列` 报错

- [ ] **Step 4: 跑 json 变体收尾并验证**

Run: `cd "D:/_WorkSpace/Unity/FuFramework2.0/Config"; cmd /c "echo. | .\gen-client-json.bat"`

验证：aot 段产物为 `tblocalizationaot.json`（且 `[remove] tblocalizationaot.bytes`）；Hotfix 侧代码为 json 变体（与运行态一致，本项目当前使用 json）。收尾后 `Unity/Assets/Resources/Config/` 下只应有 `tblocalizationaot.json`。

- [ ] **Step 5: 抽查生成代码**

检查 `Unity/Assets/Scripts/AOT/Launch/Localization/`：
- `Generate/` 下 `TableManager.cs`、`TbLocalizationAOT.cs`、`LocalizationAOT.cs`；`LanguageKey/` 下 `LanguageKey.cs`；各文件命名空间为 `AOT.Launch.Localization`
- `LanguageKey.cs` 含 12 个 `public const string aot_*`
- `TableManager.cs` 含 `public TbLocalizationAOT TbLocalizationAOT { get; private set; }` 样式的表属性与 `LoadAsync` 方法

- [ ] **Step 6: Commit**

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
git add Config/gen-client-bin.bat Config/gen-client-json.bat Tools/Luban/source/src/Luban.Schema.Builtin/FuFrameworkTableImporter.cs Unity/Assets/Resources/Config Unity/Assets/Scripts/AOT/Launch/Localization
git commit -m "[AI]feat: 生成脚本接入 AOT 本地化导出（strictGroup 严格分组 + Resources 数据 + AOT 代码双变体）"
```

---

### Task 4: ELanguage 下沉至 AOT 程序集

**Files:**
- Create: `Unity/Assets/Scripts/AOT/Framework/Localization/ELanguage.cs`（由 Hotfix 侧移动 + 追加 `ELanguageHelper`）
- Delete: `Unity/Assets/Scripts/Hotfix/Framework/Localization/ELanguage.cs`（及其 `.meta`）
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Localization/LocalizationModule.cs`（删 `SystemLanguage` 属性，改调 `ELanguageHelper`）
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Localization/LocalizationProvider.cs`（加 using）
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Localization/LanguageChangeEventArgs.cs`（加 using）

**Interfaces:**
- Produces: `AOT.Framework.Localization.ELanguage`（byte 枚举，成员不变）、`AOT.Framework.Localization.ELanguageHelper.FromSystemLanguage(UnityEngine.SystemLanguage) : ELanguage`。Task 5 依赖此二者。

- [ ] **Step 1: 移动文件（含 .meta，保 GUID）**

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
mkdir -p "Unity/Assets/Scripts/AOT/Framework/Localization"
git mv "Unity/Assets/Scripts/Hotfix/Framework/Localization/ELanguage.cs" "Unity/Assets/Scripts/AOT/Framework/Localization/ELanguage.cs"
git mv "Unity/Assets/Scripts/Hotfix/Framework/Localization/ELanguage.cs.meta" "Unity/Assets/Scripts/AOT/Framework/Localization/ELanguage.cs.meta"
```

- [ ] **Step 2: 改写 `ELanguage.cs`（命名空间 + 追加 helper）**

文件整体替换为（enum 成员与原文件逐字一致，仅换命名空间；映射从 `LocalizationModule.SystemLanguage` 原样搬迁）：

```csharp
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace AOT.Framework.Localization
{
    /// <summary>
    /// 本地化语言类型。
    /// </summary>
    public enum ELanguage : byte
    {
        /// <summary>
        /// 未指定。
        /// </summary>
        Unspecified = 0,

        Afrikaans,
        Albanian,
        Arabic,
        Basque,
        Belarusian,
        Bulgarian,
        Catalan,
        ChineseSimplified,
        ChineseTraditional,
        Croatian,
        Czech,
        Danish,
        Dutch,
        English,
        Estonian,
        Faroese,
        Finnish,
        French,
        Georgian,
        German,
        Greek,
        Hebrew,
        Hungarian,
        Icelandic,
        Indonesian,
        Italian,
        Japanese,
        Korean,
        Latvian,
        Lithuanian,
        Macedonian,
        Malayalam,
        Norwegian,
        Persian,
        Polish,
        PortugueseBrazil,
        PortuguesePortugal,
        Romanian,
        Russian,
        SerboCroatian,
        SerbianCyrillic,
        SerbianLatin,
        Slovak,
        Slovenian,
        Spanish,
        Swedish,
        Thai,
        Turkish,
        Ukrainian,
        Vietnamese
    }

    /// <summary>
    /// 语言类型辅助函数集。
    /// </summary>
    public static class ELanguageHelper
    {
        /// <summary>
        /// 将 Unity 系统语言映射为本地化语言类型。
        /// </summary>
        /// <param name="systemLanguage">Unity 系统语言</param>
        /// <returns>对应的本地化语言类型（无法识别时返回 Unspecified）</returns>
        public static ELanguage FromSystemLanguage(SystemLanguage systemLanguage)
        {
            return systemLanguage switch
            {
                // @formatter:off
                SystemLanguage.Afrikaans          => ELanguage.Afrikaans,
                SystemLanguage.Albanian           => ELanguage.Unspecified,
                SystemLanguage.Arabic             => ELanguage.Arabic,
                SystemLanguage.Basque             => ELanguage.Basque,
                SystemLanguage.Belarusian         => ELanguage.Belarusian,
                SystemLanguage.Bulgarian          => ELanguage.Bulgarian,
                SystemLanguage.Catalan            => ELanguage.Catalan,
                SystemLanguage.Chinese            => ELanguage.ChineseSimplified,
                SystemLanguage.ChineseSimplified  => ELanguage.ChineseSimplified,
                SystemLanguage.ChineseTraditional => ELanguage.ChineseTraditional,
                SystemLanguage.Czech              => ELanguage.Czech,
                SystemLanguage.Danish             => ELanguage.Danish,
                SystemLanguage.Dutch              => ELanguage.Dutch,
                SystemLanguage.English            => ELanguage.English,
                SystemLanguage.Estonian           => ELanguage.Estonian,
                SystemLanguage.Faroese            => ELanguage.Faroese,
                SystemLanguage.Finnish            => ELanguage.Finnish,
                SystemLanguage.French             => ELanguage.French,
                SystemLanguage.German             => ELanguage.German,
                SystemLanguage.Greek              => ELanguage.Greek,
                SystemLanguage.Hebrew             => ELanguage.Hebrew,
                SystemLanguage.Hungarian          => ELanguage.Hungarian,
                SystemLanguage.Icelandic          => ELanguage.Icelandic,
                SystemLanguage.Indonesian         => ELanguage.Indonesian,
                SystemLanguage.Italian            => ELanguage.Italian,
                SystemLanguage.Japanese           => ELanguage.Japanese,
                SystemLanguage.Korean             => ELanguage.Korean,
                SystemLanguage.Latvian            => ELanguage.Latvian,
                SystemLanguage.Lithuanian         => ELanguage.Lithuanian,
                SystemLanguage.Norwegian          => ELanguage.Norwegian,
                SystemLanguage.Polish             => ELanguage.Polish,
                SystemLanguage.Portuguese         => ELanguage.PortuguesePortugal,
                SystemLanguage.Romanian           => ELanguage.Romanian,
                SystemLanguage.Russian            => ELanguage.Russian,
                SystemLanguage.SerboCroatian      => ELanguage.SerboCroatian,
                SystemLanguage.Slovak             => ELanguage.Slovak,
                SystemLanguage.Slovenian          => ELanguage.Slovenian,
                SystemLanguage.Spanish            => ELanguage.Spanish,
                SystemLanguage.Swedish            => ELanguage.Swedish,
                SystemLanguage.Thai               => ELanguage.Thai,
                SystemLanguage.Turkish            => ELanguage.Turkish,
                SystemLanguage.Ukrainian          => ELanguage.Ukrainian,
                SystemLanguage.Unknown            => ELanguage.Unspecified,
                SystemLanguage.Vietnamese         => ELanguage.Vietnamese,
                _                                 => ELanguage.Unspecified
                // @formatter:on
            };
        }
    }
}
```

- [ ] **Step 3: 改 `LocalizationModule.cs`**

1. using 区追加 `using AOT.Framework.Localization;`
2. 删除整个 `SystemLanguage` 静态属性（约 71-128 行，含 XML 注释）——已确认无外部调用方
3. `OnInit` 内两处 `m_Language = SystemLanguage;`（约 144、152 行）改为 `m_Language = ELanguageHelper.FromSystemLanguage(Application.systemLanguage);`

- [ ] **Step 4: `LocalizationProvider.cs` 与 `LanguageChangeEventArgs.cs` 加 using**

两文件 using 区各追加一行：`using AOT.Framework.Localization;`

- [ ] **Step 5: Unity 编译验证**

通过 unity-cli 触发 Unity 刷新并确认 Console 无编译错误；若 unity-cli 未连接，请用户在 Unity 中聚焦后等待编译完成并确认 Console 干净。Expected: 0 error（`Hotfix` 与 `AOT` 两程序集均通过）。

- [ ] **Step 6: Commit**

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
git add Unity/Assets/Scripts/AOT/Framework/Localization Unity/Assets/Scripts/Hotfix/Framework/Localization
git commit -m "[AI]refactor: ELanguage 下沉至 AOT 程序集共享并收敛系统语言映射"
```

---

### Task 5: AOT 本地化门面 LaunchLocalization

**Files:**
- Create: `Unity/Assets/Scripts/AOT/Launch/Localization/LaunchLocalization.cs`
- Create: `Unity/Assets/Scripts/AOT/Launch/Localization/README.md`

**Interfaces:**
- Consumes: Task 3 生成的 `TableManager`（属性 `TbLocalizationAOT`、方法 `LoadAsync`）、bean `LocalizationAOT`（语言属性）、`LanguageKey`；Task 4 的 `ELanguage` / `ELanguageHelper`。
- Produces: `AOT.Launch.Localization.LaunchLocalization`：
  - `UniTask InitializeAsync()` — 加载表并确定语言
  - `ELanguage Language { get; }`
  - `string GetLanguage(string key, params object[] args)`
  Task 6 依赖这三个成员（签名逐字）。

- [ ] **Step 1: 写 `LaunchLocalization.cs`**

```csharp
using UnityEngine;
using Cysharp.Threading.Tasks;
using AOT.Framework.Core.Log;
using AOT.Framework.Localization;

#if ENABLE_BINARY_CONFIG
using Luban;
#else
using SimpleJSON;
#endif

// ReSharper disable once CheckNamespace
namespace AOT.Launch.Localization
{
    /// <summary>
    /// AOT 阶段本地化多语言门面。
    ///     从 Resources 加载 AOT 本地化表（YooAsset 未就绪阶段的既有加载通道），
    ///     脱离 UIModule/EventModule 自包含运行（与 LaunchView 同一设计原则）；
    ///     热更后仍可被 Hotfix 侧调用（Hotfix 程序集引用 AOT 程序集）。
    /// </summary>
    public static class LaunchLocalization
    {
        /// <summary>
        /// 语言偏好在 PlayerPrefs 中的存储键（值为 (int)ELanguage）。
        /// </summary>
        private const string LanguagePrefKey = "AOT_Localization_Language";

        /// <summary>
        /// AOT 配置表管理器（生成的 TableManager，仅含 TbLocalizationAOT）。
        /// </summary>
        private static TableManager s_TableManager;

        /// <summary>
        /// 当前使用的语言。
        /// </summary>
        public static ELanguage Language { get; private set; } = ELanguage.Unspecified;

        /// <summary>
        /// 初始化：加载 AOT 本地化表并确定当前语言。
        /// 须在启动流程首次展示文本前 await 完成；失败仅记录错误，不阻断启动。
        /// </summary>
        /// <returns>初始化异步流程</returns>
        public static async UniTask InitializeAsync()
        {
            Language = LoadLanguagePreference();

            var textAsset = Resources.Load<TextAsset>("Config/tblocalizationaot");
            if (textAsset == null)
            {
                FuLogger.LogError("[LaunchLocalization] Resources 下未找到 Config/tblocalizationaot，AOT 本地化不可用!");
                return;
            }

            s_TableManager = new TableManager();

#if ENABLE_BINARY_CONFIG
            // 二进制变体：TextAsset.bytes 直接包为 ByteBuf
            var bytes = textAsset.bytes;
            await s_TableManager.LoadAsync(_ => UniTask.FromResult(ByteBuf.Wrap(bytes)));
#else
            // JSON 变体：解析文本
            var json = textAsset.text;
            await s_TableManager.LoadAsync(_ => UniTask.FromResult(JSON.Parse(json)));
#endif
        }

        /// <summary>
        /// 获取 AOT 本地化多语言文本。
        /// </summary>
        /// <param name="key">多语言 key（使用 AOT 版 LanguageKey 静态类字段）</param>
        /// <param name="args">格式化参数</param>
        /// <returns>本地化文本；key 未找到时记录错误并返回空串</returns>
        public static string GetLanguage(string key, params object[] args)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;

            var localization = s_TableManager?.TbLocalizationAOT?.Get(key);
            if (localization == null)
            {
                FuLogger.LogError($"[LaunchLocalization] 多语言key '{key}' 没找到，请检查AOT多语言配置表!");
                return string.Empty;
            }

            var text = Language switch
            {
                // @formatter:off
                ELanguage.ChineseSimplified  => localization.ChineseSimplified,
                ELanguage.ChineseTraditional => localization.ChineseTraditional,
                ELanguage.English            => localization.English,
                ELanguage.Japanese           => localization.Japanese,
                ELanguage.Korean             => localization.Korean,
                ELanguage.Thai               => localization.Thai,
                ELanguage.Indonesian         => localization.Indonesian,
                ELanguage.French             => localization.French,
                ELanguage.German             => localization.German,
                ELanguage.Italian            => localization.Italian,
                ELanguage.PortuguesePortugal => localization.PortuguesePortugal,
                ELanguage.Spanish            => localization.Spanish,
                ELanguage.Vietnamese         => localization.Vietnamese,
                ELanguage.PortugueseBrazil   => localization.PortuguesePortugal,
                ELanguage.Russian            => localization.Russian,
                ELanguage.Belarusian         => localization.Russian,
                ELanguage.Ukrainian          => localization.Russian,
                _                            => localization.English, // 语言类型未支持时，统一使用英语
                // @formatter:on
            };

            // 目标语言字段为空时回退英语（与 Hotfix 版同构）
            if (string.IsNullOrEmpty(text))
            {
                text = localization.English;
            }

            return args is { Length: > 0 } ? string.Format(text, args) : text;
        }

        /// <summary>
        /// 读取语言偏好：用户持久化选择优先，无记录时回退系统语言。
        /// </summary>
        /// <returns>当前语言</returns>
        private static ELanguage LoadLanguagePreference()
        {
            var value = PlayerPrefs.GetInt(LanguagePrefKey, (int)ELanguage.Unspecified);
            if (value > (int)ELanguage.Unspecified && value <= (int)ELanguage.Vietnamese)
            {
                return (ELanguage)value;
            }

            return ELanguageHelper.FromSystemLanguage(Application.systemLanguage);
        }
    }
}
```

- [ ] **Step 2: 写目录 `README.md`**

```markdown
# AOT Launch Localization

热更前（AOT 阶段）本地化目录。

## 生成文件（禁止手改，由 `Config/gen-client-*.bat` 生成）

| 文件 | 说明 |
|---|---|
| `Generate/TableManager.cs` | AOT 表管理器（仅含 TbLocalizationAOT） |
| `Generate/TbLocalizationAOT.cs` | 表类 |
| `Generate/LocalizationAOT.cs` | 行数据 bean |
| `LanguageKey/LanguageKey.cs` | AOT 多语言 key 常量（仅 is_code=true 的 key） |

数据产物：`Assets/Resources/Config/tblocalizationaot.bytes`（bin 变体）/ `.json`（json 变体）。

## 手写文件

- `LaunchLocalization.cs`：AOT 本地化门面（加载 / 语言偏好 / GetLanguage）。
  热更后仍可被 Hotfix 侧调用（Hotfix 程序集引用 AOT 程序集）。

数据源：`Config/Excels/Local/L-LocalizationAOT-aot-热更前.xlsx`（分组 `aot`）。
```

- [ ] **Step 3: Unity 编译验证**

同 Task 4 Step 5 口径。Expected: 0 error。

- [ ] **Step 4: Commit**

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
git add Unity/Assets/Scripts/AOT/Launch/Localization/LaunchLocalization.cs Unity/Assets/Scripts/AOT/Launch/Localization/README.md Unity/Assets/Scripts/AOT/Launch/Localization/LaunchLocalization.cs.meta Unity/Assets/Scripts/AOT/Launch/Localization/README.md.meta
git commit -m "[AI]feat: 新增 AOT 本地化门面 LaunchLocalization"
```

（`.meta` 若 Unity 尚未生成则跳过该参数，待 Unity 生成后随下次提交补上。）

---

### Task 6: 启动流程接线（LaunchProcess / LaunchView / HotfixLauncher）

**Files:**
- Modify: `Unity/Assets/Scripts/AOT/Launch/LaunchProcess.cs`（10 处文本 + 初始化调用 + using）
- Modify: `Unity/Assets/Scripts/AOT/Launch/LaunchView.cs:118`（确认框按钮文本 + using）
- Modify: `Unity/Assets/Scripts/Hotfix/HotfixLauncher.cs:150,154`（2 处文本 + using 别名）

**Interfaces:**
- Consumes: Task 5 的 `LaunchLocalization.InitializeAsync/GetLanguage`（签名逐字）与生成 `LanguageKey` 常量。

- [ ] **Step 1: `LaunchProcess.cs` 接线**

1. using 区追加：`using AOT.Launch.Localization;`
2. `RunAsync` 中 `m_LaunchView = await LaunchView.CreateAsync();`（45 行）之后插入：

```csharp
            // 初始化 AOT 本地化（加载 AOT 多语言表并确定语言，供启动流程文本使用）
            await LaunchLocalization.InitializeAsync();
```

3. 文本替换（`LaunchLocalization.GetLanguage(...)` 包裹，注释行不变）：

| 行 | 原文 | 改为 |
|---|---|---|
| 64 | `m_LaunchView.SetTip("InitPackage...");` | `m_LaunchView.SetTip(LaunchLocalization.GetLanguage(LanguageKey.aot_init_res_package));` |
| 78 | `m_LaunchView.SetTip("GetVersion...");` | `m_LaunchView.SetTip(LaunchLocalization.GetLanguage(LanguageKey.aot_get_res_version));` |
| 82 | `m_LaunchView.SetTip("获取版本号失败，正在重试...");` | `m_LaunchView.SetTip(LaunchLocalization.GetLanguage(LanguageKey.aot_get_res_version_fail));` |
| 87 | `m_LaunchView.SetTip("UpdateManifest...");` | `m_LaunchView.SetTip(LaunchLocalization.GetLanguage(LanguageKey.aot_update_res_manifest));` |
| 90 | `m_LaunchView.SetTip("更新清单失败，正在重试...");` | `m_LaunchView.SetTip(LaunchLocalization.GetLanguage(LanguageKey.aot_update_res_manifest_fail));` |
| 131 | `m_LaunchView.SetTip("请求远端更新配置失败，正在重试...");` | `m_LaunchView.SetTip(LaunchLocalization.GetLanguage(LanguageKey.aot_req_remote_update_config_fail));` |
| 164 | `m_LaunchView.SetProgress(progress, $"下载中：{cur}/{tot}");` | `m_LaunchView.SetProgress(progress, LaunchLocalization.GetLanguage(LanguageKey.aot_res_downloading, cur, tot));` |
| 174 | `m_LaunchView.SetTip("下载失败，正在重试...");` | `m_LaunchView.SetTip(LaunchLocalization.GetLanguage(LanguageKey.aot_res_download_fail));` |
| 203 | `m_LaunchView.SetTip("资源加载失败，请检查网络后重启游戏!");` | `m_LaunchView.SetTip(LaunchLocalization.GetLanguage(LanguageKey.aot_res_load_fail));` |
| 219 | 同 203 文案 | `m_LaunchView.SetTip(LaunchLocalization.GetLanguage(LanguageKey.aot_res_load_fail));` |

- [ ] **Step 2: `LaunchView.cs` 按钮文本**

using 区追加：`using AOT.Launch.Localization;`
`ShowUpdateDialog` 中（118 行）：`m_WinLauncher.btnOk.title = "更新";` 改为 `m_WinLauncher.btnOk.title = LaunchLocalization.GetLanguage(LanguageKey.aot_update_dialog_ok_btn);`

- [ ] **Step 3: `HotfixLauncher.cs` 经 AOT 门面接线**

1. using 区追加别名（避免与 `Hotfix.Game.Config.LanguageKey` 冲突）：`using AotL10N = AOT.Launch.Localization;`
2. `InitDependenciesAsync` 中：
   - 150 行：`launchView.SetTip("正在加载配置...");` 改为 `launchView.SetTip(AotL10N.LaunchLocalization.GetLanguage(AotL10N.LanguageKey.aot_res_loading_config));`
   - 154 行：`launchView.SetTip("正在加载初始化资源...");` 改为 `launchView.SetTip(AotL10N.LaunchLocalization.GetLanguage(AotL10N.LanguageKey.aot_res_loading_init_res));`

- [ ] **Step 4: Unity 编译验证**

同 Task 4 Step 5 口径。Expected: 0 error。

- [ ] **Step 5: Commit**

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
git add Unity/Assets/Scripts/AOT/Launch/LaunchProcess.cs Unity/Assets/Scripts/AOT/Launch/LaunchView.cs Unity/Assets/Scripts/Hotfix/HotfixLauncher.cs
git commit -m "[AI]feat: 启动流程硬编码文本接入 AOT 本地化"
```

---

### Task 7: 收尾（清理 + 冒烟 + 最终确认）

**Files:**
- Delete: `Unity/Assets/Editor/FuFramework/Localization.meta`（孤儿 meta，工作区 untracked 残留）

**Interfaces:**
- Consumes: 前 6 个任务全部完成。

- [ ] **Step 1: 删除孤儿 meta**

先确认目录 `Unity/Assets/Editor/FuFramework/Localization/` 不存在（仅剩 meta）：`Test-Path "D:/_WorkSpace/Unity/FuFramework2.0/Unity/Assets/Editor/FuFramework/Localization"` 应为 False。确认后：

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
rm "Unity/Assets/Editor/FuFramework/Localization.meta"
```

- [ ] **Step 2: 确认生成态收尾**

Run: `git -C "D:/_WorkSpace/Unity/FuFramework2.0" status --porcelain`
Expected: 无意外文件；`Unity/Assets/Resources/Config/` 仅 `tblocalizationaot.json`（json 收尾态）及 `.meta`；AOT/Launch/Localization 下 `Generate/` 3 文件 + `LanguageKey/` 1 文件 + 根目录 `LaunchLocalization.cs`、`README.md`（及各自 `.meta`）。

- [ ] **Step 3: Editor 冒烟**

通过 unity-cli 进入 Play 模式观察启动流程：进度条文本显示中文（系统语言兜底路径）、无 `[LaunchLocalization]` 错误日志；热更移交后 HotfixLauncher 阶段文本正常。若 unity-cli 不可用，请用户手动进 Play 模式冒烟并反馈结果。

- [ ] **Step 4: Commit**

meta 为 untracked 文件，文件系统删除即可，git 无需操作。若本次会话已产生的 Unity 生成 `.meta`（`Resources/Config/`、AOT/Launch/Localization 下新文件的 meta）尚未提交，随本步一并提交：

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
git add Unity/Assets/Editor Unity/Assets/Resources Unity/Assets/Scripts/AOT
git status --porcelain   # 确认暂存内容仅含预期 meta
git commit -m "[AI]chore: 清理 Editor 孤儿 meta 并补提交 Unity 生成 meta"
```

若 `git status --porcelain` 无输出则跳过提交。

---

## 验证清单（对照 spec 第 6 节）

1. Luban 重建 0 警告 0 错误（Task 2）
2. 双变体生成无报错、产物齐全、client 隔离（Task 3，日志为证）
3. Unity 编译 0 error ×3 次（Task 4 / 5 / 6）
4. Editor 冒烟：进度条文本按语言渲染、确认框按钮文本生效（Task 7）
