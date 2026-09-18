# 语言枚举配置化 + 语言定义表 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 语言枚举 `ELanguage` 转为 Luban 配置驱动（两侧生成、缩减为 16 成员），新增语言定义表 `TbLanguageDef`，删除手写 `ELanguage.cs`。

**Architecture:** `__enums__.xlsx` 定义 `ELanguage`（group=`aot`：aot target 独立导出、client target 经语言表引用导出）；Luban 新增 `cs-enums` code target（仅渲染枚举）为 aot 段生成 `AOT.Framework.Localization.ELanguage`；Hotfix 侧随 `TbLanguageDef` 引用生成 `Hotfix.Game.Config.ELanguage`；两侧 `FromSystemLanguage` 映射与 switch 同步缩减为 16 成员。

**Tech Stack:** Luban（源码内嵌模板 `common/cs/enum.sbn`）、openpyxl（Excel）、UniTask。

**Spec:** `Docs/superpowers/specs/2026-09-18-language-config-design.md`

## Global Constraints

- 生成枚举值：`Unspecified=0`，15 语言按字母序 `1..15`（连续编号，见 spec 附录 A）；成员名/值以 spec 附录 A 为唯一标准。
- Unity 运行时铁律：禁 Task/Coroutine/LINQ/运行时反射；Luban 工具源码（`Tools/Luban/`）不受限。
- Luban `FileCleaner` 会删除输出目录中非本 target 文件（`.meta` 除外）：**凡输出到含手写文件的目录，必须传 `-x outputSaver.<target>.cleanUpOutputDir=false`**。
- gen 脚本在 `Config/` 下运行；bat 末尾 `pause` 用 `cmd /c "echo. | .\gen-client-json.bat"` 喂回车。
- 提交规范 `[AI]<type>: <中文描述>`；每次 git 操作前确认。
- Unity 编译验证命令：`unity-cli tool call refresh_assets` → 等 12s → `unity-cli tool call get_compilation_state`，期望 `errorCount: 0`。

---

### Task 1: Excel 数据（语言枚举 sheet + 语言定义表）

**Files:**
- Modify: `Config/Excels/__enums__.xlsx`（新增 sheet「语言」）
- Create: `Config/Excels/L-LanguageDef-语言定义.xlsx`

**Interfaces:**
- Produces: schema `ELanguage`（full_name 无模块前缀，group=`aot`，16 成员值 0..15）；表 `TbLanguageDef`（值类型 `LanguageDef`，字段 `language:ELanguage`、`name:string`、`icon:string`、`sort:int`、`separator:string`，15 行）。Task 2 生成与 Task 3/4 代码适配依赖这些名字。

- [ ] **Step 1: 探测现有枚举 sheet 的精确列布局**

枚举项（items）横向展开的起始列与子字段布局因 sheet 而异，**必须先探测再写入**：

```bash
python - <<'PY'
import openpyxl, sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
wb = openpyxl.load_workbook(r'D:/_WorkSpace/Unity/FuFramework2.0/Config/Excels/__enums__.xlsx', read_only=True, data_only=True)
ws = wb.worksheets[0]
for i, r in enumerate(ws.iter_rows(min_row=1, max_row=4, values_only=True), 1):
    # 打印完整列位（索引从 1 计）：重点定位 '*items' 列与行2 items 子字段（name/alias/value）的起始列
    print(f'行{i}:', [(j, v) for j, v in enumerate(r, 1) if v is not None][:16])
wb.close()
PY
```

记录：`*items` 所在列号 `X`；行2 中 `name`/`alias`/`value` 的列号（items 每个子字段一列，跨 N 个枚举项横向重复）。后续写入严格对齐这些列号。

- [ ] **Step 2: openpyxl 写入枚举 sheet 与语言定义表**

```bash
python - <<'PY'
import openpyxl, sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

# Step 1 探测结果填入（示例值以实际探测为准）：
ITEMS_COL   = <探测的 *items 列号>        # 如 8
SUBFIELDS   = <行2 从 ITEMS_COL 起的子字段序列>  # 如 ['##var','name','alias','value','comment','tags']
# items 每项占用的列数 = 子字段序列中去掉 '##var' 后的长度（如 5：name/alias/value/comment/tags）

# ---- 1. __enums__.xlsx 新增 sheet「语言」 ----
p = r'D:/_WorkSpace/Unity/FuFramework2.0/Config/Excels/__enums__.xlsx'
wb = openpyxl.load_workbook(p)
if '语言' in wb.sheetnames:
    del wb['语言']
ws = wb.create_sheet('语言')

# 行1 定义列头（照抄探测到的第 1 行全部非空单元格的 (列号, 值)）
ws.cell(1, 1, '##var'); ws.cell(1, 2, 'comment'); ws.cell(1, 3, 'full_name')
ws.cell(1, 4, 'flags'); ws.cell(1, 5, 'unique'); ws.cell(1, 6, 'group')
ws.cell(1, 7, 'tags');  ws.cell(1, ITEMS_COL, '*items')
# 行2 items 子字段行（在 ITEMS_COL 起照抄 SUBFIELDS）
for k, sf in enumerate(SUBFIELDS):
    ws.cell(2, ITEMS_COL + k, sf)
# 行3 说明行（## 开头）
ws.cell(3, 1, '##'); ws.cell(3, 2, '注释'); ws.cell(3, 3, '全名(包含模块和名字)')
ws.cell(3, 4, '是否为位标记枚举'); ws.cell(3, 5, '枚举项是否唯一'); ws.cell(3, 6, '分组')

members = [  # (name, alias) —— value 连续 0..15，依 spec 附录 A 字母序
    ('Unspecified', '未指定'), ('ChineseSimplified', '简体中文'), ('ChineseTraditional', '繁体中文'),
    ('English', '英语'), ('French', '法语'), ('German', '德语'), ('Indonesian', '印尼语'),
    ('Italian', '意大利语'), ('Japanese', '日语'), ('Korean', '韩语'),
    ('PortugueseBrazil', '葡萄牙语(巴西)'), ('PortuguesePortugal', '葡萄牙语(葡萄牙)'),
    ('Russian', '俄语'), ('Spanish', '西班牙语'), ('Thai', '泰语'), ('Vietnamese', '越南语'),
]
# 行4 数据行：主列 + items 横向展开（子字段名对号入座）
row_vals = {1: '语言类型', 2: 'ELanguage', 3: False, 4: True, 5: 'aot'}
sub_offset = {sf: k for k, sf in enumerate(SUBFIELDS)}
for i, (name, alias) in enumerate(members):
    base = ITEMS_COL + sub_offset.get('##var', 0) + i * (len(SUBFIELDS) - 1)
    for k, sf in enumerate(SUBFIELDS):
        col = ITEMS_COL + i * (len(SUBFIELDS) - 1) + k
        val = {'name': name, 'alias': alias, 'value': i}.get(sf, None)
        if sf != '##var':
            ws.cell(4, col, val)
for col, val in row_vals.items():
    ws.cell(4, col, val)
wb.save(p); wb.close()
print('枚举 sheet 写入完成:', len(members), '成员')
PY
```

Expected: `枚举 sheet 写入完成: 16 成员`。写入后立即用 Step 1 的探测脚本重跑（改为读「语言」sheet）核对行4 的 items 序列与 value 0..15 对位正确——**若对不上，以实际探测布局修正脚本列映射后再保存**。

```bash
python - <<'PY'
import openpyxl, sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
# ---- 2. 新建语言定义表 ----
pl = r'D:/_WorkSpace/Unity/FuFramework2.0/Config/Excels/L-LanguageDef-语言定义.xlsx'
wbl = openpyxl.Workbook(); wsl = wbl.active; wsl.title = 'Sheet1'
wsl.cell(1, 1, '##var'); wsl.cell(1, 2, '#Description'); wsl.cell(1, 3, 'language')
wsl.cell(1, 4, 'name');  wsl.cell(1, 5, 'icon');          wsl.cell(1, 6, 'sort')
wsl.cell(1, 7, 'separator')
wsl.cell(2, 1, '##type'); wsl.cell(2, 3, 'ELanguage'); wsl.cell(2, 4, 'string')
wsl.cell(2, 5, 'string'); wsl.cell(2, 6, 'int'); wsl.cell(2, 7, 'string')
wsl.cell(3, 1, '##'); wsl.cell(3, 3, '语言'); wsl.cell(3, 4, '显示名')
wsl.cell(3, 5, '旗帜图标'); wsl.cell(3, 6, '排序'); wsl.cell(3, 7, '逗号分隔符')
langs = [  # (language, name, sort, separator) —— icon 初稿留空，后续策划填
    ('ChineseSimplified',  '简体中文', 1, '，'),
    ('ChineseTraditional', '繁体中文', 2, '，'),
    ('English',            'English',  3, ','),
    ('French',             'Français', 4, ','),
    ('German',             'Deutsch',  5, ','),
    ('Indonesian',         'Indonesia',6, ','),
    ('Italian',            'Italiano', 7, ','),
    ('Japanese',           '日本語',    8, '、'),
    ('Korean',             '한국어',     9, ','),
    ('PortugueseBrazil',   'Português (BR)', 10, ','),
    ('PortuguesePortugal', 'Português', 11, ','),
    ('Russian',            'Русский',  12, ','),
    ('Spanish',            'Español',  13, ','),
    ('Thai',               'ไทย',       14, ','),
    ('Vietnamese',         'Tiếng Việt', 15, ','),
]
for r, (lang, name, sort, sep) in enumerate(langs, start=4):
    wsl.cell(r, 3, lang); wsl.cell(r, 4, name); wsl.cell(r, 5, '')
    wsl.cell(r, 6, sort); wsl.cell(r, 7, sep)
wbl.save(pl); wbl.close()
print('语言定义表写入完成:', len(langs), '行')
PY
```

Expected: `语言定义表写入完成: 15 行`。

- [ ] **Step 3: 4 张本地化表插入 PortugueseBrazil 列（与枚举完全对齐）**

在 `L-Localization-成就/设置/通用.xlsx` 与 `L-LocalizationAOT-aot-热更前.xlsx` 的 `PortuguesePortugal` 列之前插入 `PortugueseBrazil`（string）列，**该列各数据行的值复制同行的 PortuguesePortugal 值**（初稿一致，后续可独立翻译）。注意：**AOT 表插列后 `LaunchLocalization.ParseBin` 的列序硬约定必须在 Task 3 同步**（PortugueseBrazil 位于 PortuguesePortugal 之前）。

```bash
python - <<'PY'
import openpyxl, sys, io, glob
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
for f in sorted(glob.glob(r'D:/_WorkSpace/Unity/FuFramework2.0/Config/Excels/Local/*.xlsx')):
    wb = openpyxl.load_workbook(f)
    ws = wb.active
    # 定位 PortuguesePortugal 列（从表头行，即第 1 行）
    pp_col = next(c for c in range(1, ws.max_column + 1) if ws.cell(1, c).value == 'PortuguesePortugal')
    if ws.cell(1, pp_col - 1).value == 'PortugueseBrazil':
        print('跳过（已存在）:', f.split('/')[-1]); wb.close(); continue
    ws.insert_cols(pp_col)
    ws.cell(1, pp_col, 'PortugueseBrazil'); ws.cell(2, pp_col, 'string')
    ws.cell(3, pp_col, '葡萄牙语(巴西)')
    for r in range(4, ws.max_row + 1):  # 数据行复制葡葡值
        src = ws.cell(r, pp_col + 1).value
        ws.cell(r, pp_col, src if src is not None else '')
    wb.save(f); wb.close()
    print('已插列:', f.split('/')[-1], '列位', pp_col)
PY
```

Expected: 4 张表各输出一行「已插列」。随后重跑 Step 1 探测脚本核对 4 张表列头均含 `PortugueseBrazil` 且位于 `PortuguesePortugal` 之前。

- [ ] **Step 2: 读回验证**

重跑 Step 1 的探测脚本（把 `wb.worksheets[0]` 改为 `wb['语言']`），核对：

- 行1/行2 的列头与子字段序列完整（与写入一致）
- 行4 主列：`ELanguage`、`aot`
- 行4 items 序列：16 个成员名（`Unspecified` 起 `Vietnamese` 止）与 value `0..15` 一一对应、无错位

任何错位都回 Step 2 修正列映射后重写（openpyxl 重跑会删除并重建「语言」sheet，天然幂等）。

- [ ] **Step 3: Commit**

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
git add "Config/Excels/__enums__.xlsx" "Config/Excels/L-LanguageDef-语言定义.xlsx"
git commit -m "[AI]feat: 枚举表新增 ELanguage（16 成员连续编号）并新建语言定义表"
```

---

### Task 2: cs-enums code target + 生成链路验证

**Files:**
- Create: `Tools/Luban/source/src/Luban.CSharp/CodeTarget/CsharpEnumsCodeTarget.cs`
- Modify: `Config/gen-client-bin.bat`（aot 段追加 `-c cs-enums` 两行）
- Modify: `Config/gen-client-json.bat`（同上）
- 生成产物：`Unity/Assets/Scripts/AOT/Framework/Localization/ELanguage.cs`、`Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate/LanguageDef.cs`、`TbLanguageDef.cs`、`Unity/Assets/Resources/LaunchLocalizationText/`（json 变体 `.json`）

**Interfaces:**
- Consumes: Task 1 的 schema（ELanguage group=aot；TbLanguageDef）。
- Produces: `AOT.Framework.Localization.ELanguage`（生成，16 成员）、`Hotfix.Game.Config.ELanguage`（生成，同名同成员）、`Hotfix.Game.Config.LanguageDef`（bean：`Language`/`Name`/`Icon`/`Sort`/`Separator` 属性）、`TbLanguageDef`（表类，`Get(ELanguage)` 查询）。Task 3/4 依赖这些类型。

- [ ] **Step 1: 写 `CsharpEnumsCodeTarget.cs`**

```csharp
using Luban.CodeTarget;
using Luban.Utils;

namespace Luban.CSharp.CodeTarget;

/// <summary>
/// 仅生成枚举类型的代码目标。
///     用于只产出枚举（不产出 bean/table/manager）的场景，如 AOT 侧语言枚举。
///     枚举集 = 当前 target 分组独立导出的枚举（如 group=aot 的 ELanguage）。
/// </summary>
[CodeTarget("cs-enums")]
public class CsharpEnumsCodeTarget : CsharpCodeTargetBase
{
    /// <summary>
    /// 处理代码生成：仅遍历导出枚举，逐枚举渲染 enum 模板。
    /// </summary>
    public override void Handle(GenerationContext ctx, OutputFileManifest manifest)
    {
        foreach (var @enum in ctx.ExportEnums)
        {
            var writer = new CodeWriter();
            GenerateEnum(ctx, @enum, writer);
            manifest.AddFile(CreateOutputFile($"{GetFileNameWithoutExtByTypeName(@enum.FullName)}.{FileSuffixName}", writer.ToResult(FileHeader)));
        }
    }
}
```

- [ ] **Step 2: 重建 Luban**

Run: `& "D:\_WorkSpace\Unity\FuFramework2.0\Tools\Luban\build-luban.bat" ci`
Expected: `0 个警告 0 个错误`。

- [ ] **Step 3: 两份 bat 的 aot 段追加 cs-enums**

在 `-x cs-l10n-key.className=LaunchL10nKey ^` 行之后插入：

```bat
    -c cs-enums ^
    -x cs-enums.outputCodeDir=../Unity/Assets/Scripts/AOT/Framework/Localization ^
    -x outputSaver.cs-enums.cleanUpOutputDir=false ^
```

注意：`-c cs-enums` 必须插在两处 `-c cs-l10n-key` 各自的后面（同段内多个 `-c` 并列）；`cleanUpOutputDir=false` 保护同目录手写文件（`ELanguageHelper.cs`、目录 meta）。

- [ ] **Step 4: 跑 gen-client-json 验证**

Run: `Set-Location "D:\_WorkSpace\Unity\FuFramework2.0\Config"; cmd /c "echo. | .\gen-client-json.bat"`

验证日志与产物：
- client 段：`[overwrite] .../Generate/ELanguage.cs`（或 `[new]`，ELanguage 被语言表引用）与 `LanguageDef.cs`、`TbLanguageDef.cs`；`[new] ../Unity/Assets/Resources/LaunchLocalizationText/tblanguagedef.json`
- aot 段：`[new] ../Unity/Assets/Scripts/AOT/Framework/Localization/ELanguage.cs`
- 无 `[remove]`（cleanUpOutputDir=false 生效）、无 `ERROR`

**枚举主键验证分支**：若日志报 `TbLanguageDef` 主键类型不支持（enum key 不被支持），立即执行 fallback：用 openpyxl 把语言定义表第 2 行 E4 的 `ELanguage` 改 `int`、第 1 行 C3 表头 `language` 前（B 列后）插入 `id`（int，值为 1..15），行数据同步；`TbLanguageDef` 结构变为 `id:int` 主键 + `language:ELanguage` 字段。fallback 后重跑本步验证（`LanguageDef` bean 将含 `Id` 属性，Task 4 不依赖该差异）。

- [ ] **Step 5: 抽核生成枚举**

检查两个 `ELanguage.cs`（AOT/Framework/Localization 与 Hotfix Generate）：
- 成员数 16、`Unspecified = 0`、`ChineseSimplified = 1`、`Vietnamese = 15`
- AOT 版命名空间 `AOT.Framework.Localization`；Hotfix 版 `Hotfix.Game.Config`
- 两版成员逐项一致

- [ ] **Step 6: Commit**

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
git add Tools/Luban/source/src/Luban.CSharp/CodeTarget/CsharpEnumsCodeTarget.cs Config/gen-client-bin.bat Config/gen-client-json.bat Unity/Assets/Scripts/AOT/Framework/Localization Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate Unity/Assets/Resources/LaunchLocalizationText
git commit -m "[AI]feat: Luban 新增 cs-enums 代码目标并接入 aot 段（生成两侧语言枚举与语言定义表）"
```

---

### Task 3: AOT 侧适配（拆 ELanguageHelper、删手写枚举、switch 缩减）

**Files:**
- Delete: `Unity/Assets/Scripts/AOT/Framework/Localization/ELanguage.cs`（手写，被生成版顶替）
- Create: `Unity/Assets/Scripts/AOT/Framework/Localization/ELanguageHelper.cs`（从手写文件拆出，精简为 16 成员）
- Modify: `Unity/Assets/Scripts/AOT/Launch/Localization/LaunchLocalization.cs`（switch 缩减）

**Interfaces:**
- Consumes: Task 2 生成的 `AOT.Framework.Localization.ELanguage`（命名空间不变，`ELanguageHelper`/`LaunchLocalization` 现有 using 无需改动）。
- Produces: `ELanguageHelper.FromSystemLanguage(SystemLanguage): ELanguage`（16 成员版，签名不变）。

- [ ] **Step 1: 写 `ELanguageHelper.cs`（独立文件，精简映射）**

```csharp
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace AOT.Framework.Localization
{
    /// <summary>
    /// 语言类型辅助函数集。
    /// </summary>
    public static class ELanguageHelper
    {
        /// <summary>
        /// 将 Unity 系统语言映射为本地化语言类型（枚举为配置生成，成员集见 __enums__.xlsx 语言 sheet）。
        /// </summary>
        /// <param name="systemLanguage">Unity 系统语言</param>
        /// <returns>对应的本地化语言类型（无法识别时返回 Unspecified）</returns>
        public static ELanguage FromSystemLanguage(SystemLanguage systemLanguage)
        {
            return systemLanguage switch
            {
                // @formatter:off
                SystemLanguage.Chinese            => ELanguage.ChineseSimplified,
                SystemLanguage.ChineseSimplified  => ELanguage.ChineseSimplified,
                SystemLanguage.ChineseTraditional => ELanguage.ChineseTraditional,
                SystemLanguage.English            => ELanguage.English,
                SystemLanguage.French             => ELanguage.French,
                SystemLanguage.German             => ELanguage.German,
                SystemLanguage.Indonesian         => ELanguage.Indonesian,
                SystemLanguage.Italian            => ELanguage.Italian,
                SystemLanguage.Japanese           => ELanguage.Japanese,
                SystemLanguage.Korean             => ELanguage.Korean,
                SystemLanguage.Portuguese         => ELanguage.PortuguesePortugal,
                SystemLanguage.Russian            => ELanguage.Russian,
                SystemLanguage.Spanish            => ELanguage.Spanish,
                SystemLanguage.Thai               => ELanguage.Thai,
                SystemLanguage.Vietnamese         => ELanguage.Vietnamese,
                SystemLanguage.Unknown            => ELanguage.Unspecified,
                _                                 => ELanguage.Unspecified
                // @formatter:on
            };
        }
    }
}
```

- [ ] **Step 2: 删除手写 `ELanguage.cs`**

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
git rm "Unity/Assets/Scripts/AOT/Framework/Localization/ELanguage.cs"
```

（生成版 `ELanguage.cs` 由 Task 2 产出且与手写版同名同空间，删除后编译引用无缝。）

- [ ] **Step 3: `LaunchLocalization.cs` 适配（row 类 + 解析 + switch）**

1. `LocalizationAOTRow` 类新增属性（放在 `PortuguesePortugal` 之前）：

```csharp
        /// <summary> 葡萄牙语（巴西） </summary>
        public string PortugueseBrazil { get; set; }
```

2. `ParseJson` 的对象初始化器在 `PortuguesePortugal` 之前加一行：`PortugueseBrazil = node["PortugueseBrazil"],`
3. `ParseBin` 的读取链在 `PortuguesePortugal` 之前加一行：`PortugueseBrazil = buf.ReadString(),`（**列序硬约定同步**：Task 1 Step 3 已在 AOT 表 `PortuguesePortugal` 之前插列）
4. `GetLanguage` 的 switch：删除以下 2 行（成员已不存在）：

```csharp
                ELanguage.Belarusian         => row.Russian,
                ELanguage.Ukrainian          => row.Russian,
```

并将 `ELanguage.PortugueseBrazil => row.PortuguesePortugal,` 改为 `ELanguage.PortugueseBrazil => row.PortugueseBrazil,`（直取新列）。最终 switch 仅含 16 成员，`_ => row.English` 保留。

- [ ] **Step 4: Unity 编译验证**

按 Global Constraints 的 unity-cli 三步。Expected: `errorCount: 0`。

- [ ] **Step 5: Commit**

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
git add Unity/Assets/Scripts/AOT/Framework/Localization Unity/Assets/Scripts/AOT/Launch/Localization/LaunchLocalization.cs
git commit -m "[AI]refactor: 删除手写 ELanguage 改用生成枚举并缩减 FromSystemLanguage 映射"
```

---

### Task 4: Hotfix 侧适配（恢复映射、三文件换类型、switch 缩减）

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Localization/LocalizationModule.cs`（换 using、恢复私有映射、`Language` 属性类型）
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Localization/LocalizationProvider.cs`（换 using、switch 缩减）
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Localization/LanguageChangeEventArgs.cs`（换 using）

**Interfaces:**
- Consumes: Task 2 生成的 `Hotfix.Game.Config.ELanguage`（16 成员，值同 AOT 版）。
- Produces: `LocalizationModule.Language : Hotfix.Game.Config.ELanguage`（属性签名不变，仅类型命名空间变化）。

- [ ] **Step 1: `LocalizationModule.cs`**

1. `using AOT.Framework.Localization;` → `using Hotfix.Game.Config;`（手写枚举已删，引用对象变为生成枚举；`Hotfix.Game.Config` 是同程序集生成代码命名空间）
2. `OnInit` 中两处 `m_Language = ELanguageHelper.FromSystemLanguage(Application.systemLanguage);` 改为 `m_Language = GetSystemLanguage();`
3. 类内新增私有静态方法（放在 `OnInit` 之前）：

```csharp
        /// <summary>
        /// 获取系统语言对应的本地化语言类型（映射项与 AOT 侧 ELanguageHelper 保持一致）。
        /// </summary>
        /// <returns>本地化语言类型（无法识别时返回 Unspecified）</returns>
        private static ELanguage GetSystemLanguage()
        {
            return Application.systemLanguage switch
            {
                // @formatter:off
                UnityEngine.SystemLanguage.Chinese            => ELanguage.ChineseSimplified,
                UnityEngine.SystemLanguage.ChineseSimplified  => ELanguage.ChineseSimplified,
                UnityEngine.SystemLanguage.ChineseTraditional => ELanguage.ChineseTraditional,
                UnityEngine.SystemLanguage.English            => ELanguage.English,
                UnityEngine.SystemLanguage.French             => ELanguage.French,
                UnityEngine.SystemLanguage.German             => ELanguage.German,
                UnityEngine.SystemLanguage.Indonesian         => ELanguage.Indonesian,
                UnityEngine.SystemLanguage.Italian            => ELanguage.Italian,
                UnityEngine.SystemLanguage.Japanese           => ELanguage.Japanese,
                UnityEngine.SystemLanguage.Korean             => ELanguage.Korean,
                UnityEngine.SystemLanguage.Portuguese         => ELanguage.PortuguesePortugal,
                UnityEngine.SystemLanguage.Russian            => ELanguage.Russian,
                UnityEngine.SystemLanguage.Spanish            => ELanguage.Spanish,
                UnityEngine.SystemLanguage.Thai               => ELanguage.Thai,
                UnityEngine.SystemLanguage.Vietnamese         => ELanguage.Vietnamese,
                UnityEngine.SystemLanguage.Unknown            => ELanguage.Unspecified,
                _                                             => ELanguage.Unspecified
                // @formatter:on
            };
        }
```

（`Language` 属性 setter 的存储逻辑不变——`value.ToString()` 名字符串对生成枚举继续有效。）

- [ ] **Step 2: `LocalizationProvider.cs`**

1. `using AOT.Framework.Localization;` → 删除（`Hotfix.Game.Config` 已在 using 列表——若无需新增则不加；`ELanguage`/`TbLocalization` 均解析自 `Hotfix.Game.Config`）
2. `GetLanguage` 的 switch：删除 `ELanguage.Belarusian => localization.Russian,` 与 `ELanguage.Ukrainian => localization.Russian,` 两行，并将 `ELanguage.PortugueseBrazil => localization.PortuguesePortugal,` 改为 `ELanguage.PortugueseBrazil => localization.PortugueseBrazil,`（直取新列，bean 已含该属性）。最终仅含 16 成员映射

- [ ] **Step 3: `LanguageChangeEventArgs.cs`**

`using AOT.Framework.Localization;` → `using Hotfix.Game.Config;`（`ELanguage` 属性类型随之变为生成枚举）

- [ ] **Step 4: Unity 编译验证**

Expected: `errorCount: 0`。若报 `ELanguage` 歧义（Hotfix.Framework.Localization 命名空间内无该类型，不应发生），检查是否残留旧 using。

- [ ] **Step 5: Commit**

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
git add Unity/Assets/Scripts/Hotfix/Framework/Localization
git commit -m "[AI]refactor: Hotfix 本地化框架改用配置生成的 ELanguage 并缩减语言集"
```

---

### Task 5: 收尾（全链路验证 + 记忆）

**Files:**
- Modify: memory `aot-localization.md`（语言枚举配置化后的架构更新）

**Interfaces:**
- Consumes: Task 1-4 全部完成。

- [ ] **Step 1: 双变体全链路回归**

跑 `gen-client-bin.bat`（bin 变体：`tblanguagedef.bytes` + AOT/Framework/Localization/ELanguage.cs 由 cs-enums 生成）→ 跑 `gen-client-json.bat` 收尾（json 变体）。两段均无 ERROR、无意外 `[remove]`。

- [ ] **Step 2: Unity 编译 + Editor 冒烟**

编译 `errorCount: 0`；`clear_console` → `play_game` → 12s 后 `read_console`（filterText=`LaunchLocalization`，期望 0 条错误）→ `stop_game`。

- [ ] **Step 3: 更新 memory `aot-localization.md`**

追加一条：`ELanguage` 已配置化（`__enums__.xlsx` 语言 sheet，group=aot，16 成员连续编号），`ELanguageHelper`（AOT）与 `LocalizationModule.GetSystemLanguage`（Hotfix）各持一份系统语言映射，手写枚举已删除；语言元数据表 `TbLanguageDef`（L-LanguageDef-语言定义.xlsx）供语言切换 UI/旗帜 icon/分隔符使用。

- [ ] **Step 4: Commit（如有 memory 之外的变更）**

memory 文件在仓库外无需提交。若 Step 1/2 产生产物变更（如 bin→json 切换残留），一并提交：

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
git add -A Unity/Assets/Resources Unity/Assets/Scripts
git status --porcelain   # 确认后提交
git commit -m "[AI]chore: 语言枚举配置化收尾"
```
