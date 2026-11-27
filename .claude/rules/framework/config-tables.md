---
paths:
  - "{Config/**,Tools/Luban/**}"
---

# 配置表（Luban）

## 目录

| 目录 | 放什么 | 禁则 |
|---|---|---|
| `Config/Excels/Tables/` | 普通配置表**和资源类多语言表**（图片/音频等） | — |
| `Config/Excels/Local/` | **仅**文本类本地化表（必须带 `key` 字段） | 资源类多语言表**不得**放这里 |
| `Config/Excels/` 根 | 仅 `__tables__.xlsx` / `__beans__.xlsx` / `__enums__.xlsx` | **不得**放任何数据表 |

- 生成代码：`Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/`（`Generate/` 表代码、`Extension/` 语言 key）
- 生成脚本：`Config/gen-client-bin.{bat,sh}`、`Config/gen-server-bin.{bat,sh}`

## 文件命名（自动注册）

```
[单字母前缀]-[英文表名]-[组名(可选)]-[中文注释].xlsx
```

- 表名 = `Tb` + 第 1、2 个 `-` 之间的英文段。`C-Achievement-成就表.xlsx` → `TbAchievement`
- **英文段不得含空格或下划线**——importer 按 `-` 和 `_` 双分隔符切名，`C-Achievement_Extra-成就表.xlsx` 会被切成 `TbAchievement`
- 前缀段是**单个**字母或数字（正则 `[a-zA-Z0-9]-`）
- 组名段：`c` / `s` / `aot`
- **项目用自定义 importer 按文件名自动注册——不要往 `__tables__.xlsx` 加行**
- 同名表多个文件自动合并（本地化表按模块分文件归并进同一张表）
- 支持 `xlsx` / `xls` / `xlsm` / `csv`

## 主键与 bean 定义

- 表 mode 恒 map、index 恒空 → **每张表第一个真实字段即主键**。
- `#` 开头的列名是注释列（如本地化表的 `#Description`），**不导出、不算字段**。
- 数据表行结构一律从表头推导（importer 硬编码）→ **禁止在 `__beans__.xlsx` 定义与数据表同名的 bean**（会重复定义报错）。

## 生成流程

```bash
# 改了 Luban 生成格式/模板（源码版模板为准）→ 先重建工具
cd Tools/Luban && ./build-luban.bat
# 再生成
cd Config && ./gen-client-bin.bat
```

**改模板后必须重跑 build-luban**，否则用的还是旧 dll。AOT 不是独立脚本，而是 `gen-client-bin` 的第二趟（`-t aot`）。

## 表级分组（文件名第 3 段）

文件名格式：`排序编号-表名-分组-描述.xlsx`，如 `U-UIConfig-c-UI配置表.xlsx`。

- 分组段为 `c` → 仅客户端；`aot` → AOT 段。
- **只有脚本传了 `-x tableImporter.target=<target>` 才生效**：不传时所有带分组段的表都会被**静默跳过**（无任何报错）。现有 client/server/AOT 各段已补齐。
- 排除客户端专属表：文件名加 `-c-`。服务端产物由 LocalFileSaver 自动 `[remove]`。

## 枚举/Bean 分组

`Config/Excels/__enums__.xlsx` / `__beans__.xlsx` 的 `group` 列：

- 分组为空且目标含 default 组 → **无条件导出**（与是否被表引用无关）。
- 分组非空 → 仅目标 groups 包含时才导出。
- 被导出表字段引用到的类型经可达性自动补入。

当前服务端仅保留：Achievement / Item / GlobalDefine 三表 + EQuality/EItemType/EItemSubType/EItemUseType 四枚举。

## is_code 列（本地化表）

本地化表的 `is_code`（bool）列：**为 true 的 key 才导出到 `L10nKey.cs`**，供代码引用；false 只进数据产物。

**本地化表缺 `is_code` 列 → Luban 直接报错终止**（`CollectKeysFromTable` 强制）。新增本地化表必须带该列。

## 服务端模板坑（已修，勿回退）

- `LongDataMaps` 是 `SortedDictionary<long,T>`，枚举键必须 `(long)` 强转。
- 服务端不引用 Luban 运行时，集合 ToString 必须用 `FuFramework.Core.Config.StringUtil`。

## 运行时访问

```csharp
GlobalModule.ConfigModule.GetConfig<T>();
```

自定义命名：`L-<表名>.xlsx` → `Tb<表名>`。
