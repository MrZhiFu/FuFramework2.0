---
name: luban-excel
description: >
  本项目 Luban 配置表（xlsx）自动化处理。创建/修改/填充配置表（源表在
  Config/Excels 下的 Tables 与 Local 目录）、新增配置表文件、编辑本地化多语言表、
  批量翻译填充、改表结构后重新生成代码与数据。凡涉及 Config/ 目录下的 .xlsx
  配置表读写、gen-client/gen-server 生成、L10nKey/is_code 约定的场景使用本 skill。
---
# Luban 配置表自动化（本项目专用）

用 Python（openpyxl）操作本项目的 Luban 配置表。**动手前先读「硬规则速查」，
写错任何一条都会导致生成失败或数据错乱。**

## Tools Required

- Bash（运行 python 与 gen 脚本）
- Read / Edit / Grep（查代码引用、读规范文档）

## Setup

openpyxl **不要每次安装**。先探测，缺了才装：

```bash
python -c "import openpyxl" 2>/dev/null || pip install openpyxl
```

读表探测结构用 `openpyxl.load_workbook(path)`；`read_only=True` 的 workbook **禁止回写**。

## 硬规则速查

### 目录分工（放错目录 = 生成失败或表丢失）

| 目录                      | 放什么                                                       | 例子                         |
| ------------------------- | ------------------------------------------------------------ | ---------------------------- |
| `Config/Excels/Tables/` | 所有普通配置表                                               | `U-UIConfig-UI配置表.xlsx` |
| `Config/Excels/Local/`  | **仅**本地化文本表（必须带 `key` 字段）              | `L-Localization-通用.xlsx` |
| `Config/Excels/` 根     | 仅`__tables__/__beans__/__enums__.xlsx` 三个定义文件，勿动 | —                           |

> 资源类多语言表（图片/音频等）**不能**放 `Local/`，放 `Tables/`（`Local/` 是文本校验专用通道）。

### 文件命名 → 自动注册，无需登记 `__tables__.xlsx`

```
[单字符前缀]-[英文表名]-[组名(可选)]-[中文注释].xlsx
```

- 表名 = `Tb` + 第 1、2 个 `-` 之间的英文段：`C-Achievement-成就表.xlsx` → `TbAchievement`
- 英文段不能有空格、**也不要用下划线**：importer 按 `-` 和 `_` 双分隔符切名，`C-Achievement_Extra-成就表.xlsx` 的表名会变成 `TbAchievement`（`Extra` 被并入注释）
- 前缀段是**单个**字母或数字（正则 `[a-zA-Z0-9]-`）；后面中文注释段随便接
- 组名（第 3 段，可选）：`c` / `s` / `aot`；文件名含 `-aot-` 的表只进 aot 导出（如 `L-LocalizationAOT-aot-热更前.xlsx`）
- 项目用自定义 `tableImporter.name=fuframework` 按文件名自动注册，**不要**往 `__tables__.xlsx` 里加行
- 同名表多个文件自动合并输入（本地化表按模块分多文件 → 归并进同一张 `TbLocalization` 就是靠这个）
- 支持扩展名：`xlsx` / `xls` / `xlsm` / `csv`

### 普通表：4 行表头（数据行首列留空，从第 2 列起）

以 `U-UIConfig-UI配置表.xlsx` 为准：

| 行 | A 列        | B 列起                                                                                    |
| -- | ----------- | ----------------------------------------------------------------------------------------- |
| 1  | `##var`   | 字段名（特殊列名如`##comment` 原样保留）                                                |
| 2  | `##type`  | Luban 类型（`string` / `int` / `long` / `float` / `bool` / 枚举名 / bean 名…） |
| 3  | `##group` | 分组`c`（与文件名组名一致）                                                             |
| 4  | `##`      | 中文注释（可含换行）                                                                      |
| 5+ | 留空        | 数据                                                                                      |

### 本地化表：3 行表头 + 固定列

以 `L-Localization-通用.xlsx` 为准，**所有 Local/ 表必须有 `is_code` 列（缺失时生成直接报错终止）**：

| 行 | 内容                                                                                                    |
| -- | ------------------------------------------------------------------------------------------------------- |
| 1  | `##var`、`#Description`、`key`、`is_code`、16 个语言列（`ChineseSimplified`…`Vietnamese`） |
| 2  | `##type`（key/语言列=`string`，is_code=`bool`）                                                   |
| 3  | `##` 中文说明                                                                                         |
| 4+ | 数据；key 前缀按模块（`common_*`、`aot_*`…）                                                       |

- `is_code=True`：key 导出到 `L10nKey` 常量类（代码引用用）；`False`：仅进数据供策划表引用
- AOT 前置表（热更进度条等启动期文案）：`L-LocalizationAOT-aot-热更前.xlsx`，is_code 恒为 `True`

### 键值紧凑表（如 `G-GlobalDefine-全局常量定义表.xlsx`）

表内自带列定义：第 1 行首两列为 `##column#var`、`##type` 标记，第 2 行中文说明，
第 3 行起数据**含首列**。新增条目照抄现有行的列布局，不要改动标记行。

### 类型坑速记（详见 `Config/README.md` 类型表）

- `bool`：`true/false/0/1` 均可，大小写不敏感；空 = 默认值
- `datetime`：自 UTC 1970-01-01 起的**秒**数，填 `yyyy-mm-dd hh:mm:ss`（可省略到日），**不可填空**
- 容器：`(list#sep),T` / `(array#sep),T` / `(set#sep),T` / `(map#sep),T`，数据如 `1,2,3`、`1:1,2:3`
- 可空类型：`int?` / `Color?` / `Shape?`，仅基本与自定义类型；**容器及其 key/value 不支持可空**
- 复杂列的解析格式写在**字段名**上（`##var` 行，如 `pos#format=lite`），写在 `##type` 行不生效；`lite` 用 `{...}`（多态 `{TypeName,字段...}`，不支持缺省字段），`json` 用 `{"$type":"Circle",...}`，`lua` 用 `{x=1,...}`（多态 `_type_`）；推荐 `lite`

## 枚举与自定义类型定义（`__enums__.xlsx` / `__beans__.xlsx`）

**通则（本项目特有）**：

- 数据表的行结构一律从表头推导（importer 硬编码 `ReadSchemaFromFile=true`）→ **禁止**在 `__beans__.xlsx` 定义与数据表同名的 bean（如建了 `D-Item` 表又定义 `Item` bean → 重复定义报错）
- 两个定义文件里维护的是**独立复合类型**，供数据表列的 `##type` 引用：`Property`、`(list,PropItem)`、`EUILayer`…
- 表 mode 恒 map、index 恒空 → **每张表第一个真实字段即主键**（`#` 开头列名是注释列不导出，如本地化表 `#Description`，不算字段）

### `__enums__.xlsx`：sheet 分「业务层」「框架层」，每个枚举一段

实测布局（枚举之间空行分隔）：

```
##var | comment  | full_name | flags | unique | group | tags | *items
##var |          |           |       |        |       |      | name | alias | value | comment | tags   ← 子字段行
##    |          | 全名(包含模块和名字) | 是否位标记 | 项值唯一 |      |      | 枚举名 | 别名 | 值 | 注释
      | 语言类型 | ELanguage | False | True   | aot   |      | Unspecified | 未指定 | 0 | 未指定
      |          |           |       |        |       |      | ChineseSimplified | 简体中文 | 1 | 简体中文
```

- 枚举首行填元信息 + 第一项；后续行只填 items 列
- `flags=True` 位标记枚举，value 可填 `A|B` 组合；value 支持十进制/十六进制
- `group` 可标 `aot`（如 `ELanguage`）
- 数据表填英文名或枚举项 alias 中文名均可（官方能力；本项目数据表惯用英文名，如 `MainUI`）
- 新增枚举：在「业务层」（业务）或「框架层」（框架）sheet 末尾**空一行后追加段**——三行表头整个 sheet 只有一份，追加段**不加表头**；仅新建 sheet 时才写三行表头（bean 段同理）

### `__beans__.xlsx`：每个 bean 一段

实测布局（bean 之间空行分隔）：

```
##var | full_name | parent | valueType | sep | alias | comment | group | tags | *fields
##var |           |        |           |     |       |         |       |      | name | alias | type | group | comment | tags | variants   ← 子字段行
##    | 全名(包含模块和名字) |    |           | 分割符 |     | 注释    |       |      | 字段名 | 字段别名 | 类型 | 分组 | 注释 |     | 字段变体
      | PropItem  |        |           | ;   |       |         |       |      | Id |    | int |   | 道具id
      |                                                     | Count |  | int |   | 道具数量
```

- bean 首行填 `full_name`（+元信息）+ 第一个字段；后续行只填 fields 列
- `sep`：该 bean 在紧凑/流式填法里的默认分隔符（`PropItem` 用 `;`）
- `parent`：继承父类 → 多态；`valueType`：值类型语义（向量类用）
- **多态填法三选一**：
  - 分列：字段占多列，两行 `##var`（第二行子行首列写 `$type`，后接子类字段名），数据行 `$type` 列填子类名或 alias，无关列留空
  - 单格 sep：字段名写 `shape#sep=,`，数据 `Circle,1.5`
  - 单格 lite：字段名写 `shape#format=lite`，数据 `{Circle,1.5}`
- 非可空多态（`Shape`）必须填具体子类；可空多态（`Shape?`）才可留空；**抽象父类名不能当 `$type` 填**

### 新增 enum / bean 的流程

1. 定义文件加段（`__enums__.xlsx` 或 `__beans__.xlsx`，照实测布局）
2. 数据表列的 `##type` 引用该类型（`Property`、`(list,PropItem)`、`EUILayer`…）
3. 到 `Config/` 下跑 `gen-client-json.bat` → 回 Unity 编译

## 操作流程

### 新增一张配置表

1. 按命名规则建 `Config/Excels/Tables/<字母>-<英文表名>-<中文注释>.xlsx`
2. openpyxl 写入 4 行表头（照上面「普通表」布局；`##group` 行填 `c`，服务端表填 `s`）
3. 填数据行（首列留空）
4. 到 `Config/` 目录下运行 `gen-client-json.bat`（bin 变体为 `gen-client-bin.bat`）
5. 回 Unity 触发重新编译；生成的代码与数据**纳入本次改动即可，不自行 git commit**（改动攒到验证完毕后按用户习惯统一提交）

### 修改现有表

1. 先用 openpyxl 读出表头与若干数据行确认列布局，再动手
2. 加字段 = 在表头 4 行各补一列（`##var`/`##type`/`##group`/`##` 各行都要补，位置对齐）+ 历史数据行补默认值
3. 加数据行 = 从第 5 行（本地化表第 4 行）起追加，首列留空
4. 同上跑 gen → Unity 编译

### 本地化表填词 / 翻译

1. 新条目追加到对应模块的 `L-Localization-*.xlsx`（按 key 前缀选文件，如 `common_*` → 通用）
2. `is_code`：代码里要 `L10nKey.xxx` 引用的填 `True`，仅策划表引用填 `False`
3. 翻译列：至少填 `ChineseSimplified` + `English`；其余语言缺失时运行时回退 English 列
4. 同上跑 gen；改了 `is_code=True` 的 key 注意同步代码引用

### 生成与验证

在**项目根下的 `Config/` 目录**运行（脚本按相对路径引用自身配置，换目录会失败；结尾自动 `pause`）：

```powershell
cd <项目根>\Config
.\gen-client-json.bat
```

- 产物：数据 → `Unity/Assets/Bundles/Config`；代码 → `Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/`
- aot 表产物：`Unity/Assets/Resources/LaunchLocalizationText/` + `Unity/Assets/Scripts/AOT/Launch/Localization/AutoGen/`
- 控制台有报错 = 表结构违反上述规则，先修表再重跑；脚本缺 `Luban.dll` 会自动构建，无需手动干预
- 生成后用 unity-cli 触发 Unity 刷新编译（本项目已配置 unity-cli），并确认控制台无编译错误

## 低频深水区 → 先读 `Config/README.md`

- 扩语言（加一种语言列）：四步联动（枚举 → 各本地化表加列 → `L-LanguageDef` 加行 → 代码 switch）
- 新增多语言资源类型表（图片/音频）：README「扩展：新增多语言资源类型」
- 改生成格式/模板：动 `Tools/Luban/` 源码 → `build-luban.bat` 重建 → 再跑 gen
- 自动注册/主键/分组行为的实现细节：`Tools/Luban/source/src/Luban.Schema.Builtin/FuFrameworkTableImporter.cs`

## Luban 官方文档参考（datable.cn）

- Excel 定义 Schema（`__tables__`/`__beans__`/`__enums__` 字段全解）：[https://www.datable.cn/docs/schema/excel-schema](https://www.datable.cn/docs/schema/excel-schema)
- 多态与抽象 bean（schema 侧）：[https://www.datable.cn/docs/schema/polymorphism](https://www.datable.cn/docs/schema/polymorphism)
- 多态数据怎么填（数据侧 `$type`）：[https://www.datable.cn/docs/excel/polymorphism](https://www.datable.cn/docs/excel/polymorphism)
- 紧凑格式（stream/lite/json/lua）：[https://www.datable.cn/docs/excel/compact](https://www.datable.cn/docs/excel/compact)

> 本 skill 的 enum/bean 布局以**本项目实测**（`__enums__.xlsx`/`__beans__.xlsx` 现有段）为准；官方文档讲通用规范，两者冲突时以本项目现状 + `FuFrameworkTableImporter.cs` 行为为准。
