---
name: fu-config-edit
description: >
  本项目 Luban 配置表的编辑操作手册：表结构布局、openpyxl 读写、
  `__enums__`/`__beans__` 段格式、类型坑。凡涉及 Config/Excels 下表的
  实际内容编辑（新增表、加字段、填词翻译、定义枚举/bean）时使用。
  目录分工、命名规则、分组与生成约束见 rules/framework/config-tables.md。
---

# Luban 配置表编辑手册

**约束以 `rules/framework/config-tables.md` 为准**（目录分工、文件命名、分组、生成流程、`is_code` 约定）。本技能只讲**单元格怎么填**——那些内容是规则里没有的。

规则在碰 `Config/**` 时自动加载；本技能负责操作细节。

## Tools Required

- Bash（运行 python + openpyxl、gen 脚本）
- Read / Edit / Grep（查代码引用、读 `Config/README.md`）

## 表头布局

### 普通表：4 行表头（数据行首列留空，从第 2 列起）

以 `U-UIConfig-UI配置表.xlsx` 为准：

| 行 | A 列 | B 列起 |
|---|---|---|
| 1 | `##var` | 字段名（`##comment` 等特殊列名原样保留） |
| 2 | `##type` | Luban 类型（`string` / `int` / `long` / `float` / `bool` / 枚举名 / bean 名…） |
| 3 | `##group` | 分组 `c`（与文件名组名一致） |
| 4 | `##` | 中文注释（可含换行） |
| 5+ | 留空 | 数据 |

### 本地化表：3 行表头 + 固定列

以 `L-Localization-通用.xlsx` 为准。**必须有 `is_code` 列**（缺失时生成直接报错终止）：

| 行 | 内容 |
|---|---|
| 1 | `##var`、`#Description`、`key`、`is_code`、16 个语言列（`ChineseSimplified` … `Vietnamese`） |
| 2 | `##type`（key/语言列 = `string`，is_code = `bool`） |
| 3 | `##` 中文说明 |
| 4+ | 数据；key 前缀按模块（`common_*`、`aot_*` …） |

- `is_code=True` → 导出到 `L10nKey` 常量类；`False` → 仅进数据供表间引用
- AOT 前置表（启动期文案）：`L-LocalizationAOT-aot-热更前.xlsx`，`is_code` 恒为 `True`

### 键值紧凑表（如 `G-GlobalDefine-全局常量定义表.xlsx`）

表内自带列定义：第 1 行首两列为 `##column#var`、`##type` 标记；第 2 行中文说明；第 3 行起数据**含首列**。新增条目照抄现有行列布局，**不要改动标记行**。

## 类型坑速记（详见 `Config/README.md` 类型表）

- `bool`：`true/false/0/1` 均可，大小写不敏感；空 = 默认值
- `datetime`：自 UTC 1970-01-01 起的**秒**数；填 `yyyy-mm-dd hh:mm:ss`（可省略到日）；**不可为空**
- 容器：`(list#sep),T` / `(array#sep),T` / `(set#sep),T` / `(map#sep),T`，数据如 `1,2,3`、`1:1,2:3`
- 可空类型：`int?` / `Color?` / `Shape?`，仅基本与自定义类型；**容器及其 key/value 不支持可空**
- 复杂列的解析格式写在**字段名**上（`##var` 行，如 `pos#format=lite`），写在 `##type` 行**不生效**
  - `lite` 用 `{...}`（多态 `{TypeName,字段...}`，不支持缺省字段）
  - `json` 用 `{"$type":"Circle",...}`；`lua` 用 `{x=1,...}`（多态 `_type_`）
  - **推荐 `lite`**

## 枚举与自定义类型定义

通则：数据表行结构从表头推导 → **禁止在 `__beans__.xlsx` 定义与数据表同名的 bean**。两个定义文件维护的是独立复合类型，供数据表列的 `##type` 引用（`Property`、`(list,PropItem)`、`EUILayer`…）。

### `__enums__.xlsx`：sheet 分「业务层」「框架层」，每个枚举一段

实测布局（枚举之间空行分隔）：

```
##var | comment | full_name | flags | unique | group | tags | *items
##var |         |           |       |        |       |      | name | alias | value | comment | tags   ← 子字段行
##    |         | 全名(含模块) | 是否位标记 | 项值唯一 |     |      | 枚举名 | 别名 | 值 | 注释
      | 语言类型 | ELanguage | False | True   | aot   |      | Unspecified | 未指定 | 0 | 未指定
```

- 枚举首行填元信息 + 第一项；后续行只填 items 列
- `flags=True` 为位标记枚举，value 可填 `A|B`；支持十进制/十六进制
- `group` 可标 `aot`（如 `ELanguage`）
- 数据表填英文名或 alias 中文名均可（本项目惯用英文名，如 `MainUI`）
- **新增枚举**：在对应 sheet 末尾**空一行后追加段**——三行表头整个 sheet 只有一份，追加段**不加表头**；仅新建 sheet 时才写表头

### `__beans__.xlsx`：每个 bean 一段

实测布局（bean 之间空行分隔）：

```
##var | full_name | parent | valueType | sep | alias | comment | group | tags | *fields
##var |           |        |           |     |       |         |       |      | name | alias | type | group | comment | tags | variants
##    | 全名(含模块) |     |           | 分割符 |    | 注释    |       |      | 字段名 | 字段别名 | 类型 | 分组 | 注释 |     | 字段变体
      | PropItem  |        |           | ;   |       |         |       |      | Id |    | int |   | 道具id
```

- bean 首行填 `full_name`（+元信息）+ 第一个字段；后续行只填 fields 列
- `sep`：该 bean 在紧凑/流式填法里的默认分隔符
- `parent`：继承父类 → 多态；`valueType`：值类型语义（向量类用）
- **多态填法三选一**：
  - 分列：字段占多列，两行 `##var`（子行首列写 `$type`），数据行 `$type` 填子类名或 alias
  - 单格 sep：字段名写 `shape#sep=,`，数据 `Circle,1.5`
  - 单格 lite：字段名写 `shape#format=lite`，数据 `{Circle,1.5}`
- 非可空多态（`Shape`）必填具体子类；可空多态（`Shape?`）才可留空；**抽象父类名不能当 `$type` 填**

## 操作流程

所有流程都以「跑生成」收尾——命令、目标参数、产物路径见 `rules/framework/config-tables.md` 的「生成流程」。

### 新增一张配置表

1. 按命名规则建文件（命名规则见规则；`##group` 行填 `c`，服务端表填 `s`）
2. openpyxl 写入 4 行表头（照上方「普通表」布局）
3. 填数据行（首列留空）

### 修改现有表

1. 先用 openpyxl 读出表头与几行数据确认列布局，再动手
2. 加字段 = 表头 4 行各补一列（`##var`/`##type`/`##group`/`##` 对齐）+ 历史数据行补默认值
3. 加数据行 = 从第 5 行（本地化表第 4 行）起追加，首列留空

### 本地化表填词 / 翻译

1. 新条目追加到对应模块的 `L-Localization-*.xlsx`（按 key 前缀选文件）
2. `is_code`：代码要 `L10nKey.xxx` 引用的填 `True`
3. 翻译列：至少填 `ChineseSimplified` + `English`；其余缺失时运行时回退
4. 改了 `is_code=True` 的 key 记得同步代码引用

### 新增 enum / bean

1. 定义文件加段（照上方实测布局）
2. 数据表列的 `##type` 引用该类型
3. 跑生成

## 低频深水区 → 先读 `Config/README.md`

- 扩语言（加一种语言列）：四步联动（枚举 → 各本地化表加列 → `L-LanguageDef` 加行 → 代码 switch）
- 新增多语言资源类型表：README「扩展：新增多语言资源类型」
- 改生成格式/模板：动 `Tools/Luban/` → `build-luban.bat` 重建 → 再跑 gen
- 自动注册/主键/分组实现细节：`Tools/Luban/source/src/Luban.Schema.Builtin/FuFrameworkTableImporter.cs`
