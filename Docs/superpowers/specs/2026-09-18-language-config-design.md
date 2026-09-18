# 语言枚举配置化 + 语言定义表设计文档

- 日期：2026-09-18
- 状态：待用户审阅
- 分支：refactor/framework-modules-to-hotfix
- 前置：`2026-09-17-aot-localization-design.md`（AOT 前置本地化，已实施）

## 1. 背景与目标

当前语言类型是手写 C# 枚举（`AOT/Framework/Localization/ELanguage.cs`，AOT 程序集共享，Hotfix 经 asmdef 引用）。本设计将语言枚举转为 **Luban 配置驱动**：

1. `__enums__.xlsx` 定义 `ELanguage` 枚举，两侧（Hotfix/AOT）各自生成枚举类型；
2. 新增语言定义表 `TbLanguageDef`（枚举、显示名、旗帜 icon、排序、逗号分隔符），供语言切换 UI 与后续多语言元数据管理使用；
3. 删除手写 `ELanguage.cs`，所有代码改用生成枚举。

## 2. 决策记录

| 决策点 | 结论 | 理由 |
|---|---|---|
| 枚举成员值 | **连续编号**：`Unspecified=0`，15 个语言成员按字母序 `1..15`（显式赋值） | Hotfix 存储为枚举名字符串（与数值无关）；AOT PlayerPrefs 仅有开发期测试数据，实施时清除一次即可——可读性优先 |
| 枚举成员名 | 与 `TbLocalization` bean 语言字段名逐一对齐（ChineseSimplified…） | `LocalizationProvider` 的 switch 与语言表/bean 字段可直接对上 |
| 两侧枚举类型 | 各生成一份：`Hotfix.Game.Config.ELanguage` / `AOT.Framework.Localization.ELanguage` | 不同程序集不可能共享同一类型；同名同空间使手写文件删除后引用零改动 |
| AOT 枚举来源 | Luban 新增 `cs-enums` code target，aot 段 bat 追加 | aot 段只有 `cs-l10n-key`（不生成 bean/枚举）；语言表在 client 分组不会进入 aot 的 imported types，必须有独立枚举生成通道 |
| 取文本逻辑 | `LocalizationProvider` switch 保留，仅换枚举类型 | 语言表驱动字段映射需要委托表/反射（铁律禁反射），复杂度高收益低；YAGNI |
| FromSystemLanguage | 纯代码逻辑，**不可配置化**；两侧各留一份（各操作本侧枚举类型） | UnityEngine.SystemLanguage → 枚举的映射是代码行为；两程序集类型隔离下无法共用 |
| 语言表分组 | client（热更侧数据，无分组段=全组） | 旗帜 icon/语言切换 UI 是登录后热更侧功能；AOT 阶段只需枚举类型不需语言元数据 |

## 3. 语言枚举配置（`Config/Excels/__enums__.xlsx`）

新增枚举段（建议放在新 sheet 或现有 sheet 末尾）：

- `full_name`: `ELanguage`（无模块前缀 → 生成到各 target 的 topModule 命名空间）
- `flags`: false、`unique`: true、`comment`: 语言类型
- **16 个成员**（缩减不常用语言，后续按需补齐）：`Unspecified=0` + 15 个语言成员按字母序**连续编号 `1..15`**（完整清单见附录 A）；未来补齐语言直接追加 `16, 17...`
- 成员 `name` = PascalCase（对齐 bean 字段名），`alias` = 中文名（简体中文/英语…）

生成物：

- client target（语言表 bean 引用 → imported types）→ `Hotfix/Game/AutoGen/Tables/Generate/` 下 `ELanguage.cs`（命名空间 `Hotfix.Game.Config`）
- aot target（`cs-enums`）→ `Assets/Scripts/AOT/Launch/Localization/ELanguage.cs`（命名空间 `AOT.Launch.Localization`，即 aot target 的 topModule——实施事实：枚举与 LaunchL10nKey/LaunchLocalization 同空间更内聚）

## 4. 语言定义表 `TbLanguageDef`

- 文件：`Config/Excels/Tables/L-LanguageDef-语言定义.xlsx`（无分组段 = 所有 target 导出；client 生成数据+代码）
- 表名：`TbLanguageDef`（`L-LanguageDef` → 第 2 段 `LanguageDef`）、值类型 `LanguageDef`
- Mode：MAP，主键 `language`（enum 主键，Luban 支持枚举主键；若验证不支持则改 `int` 主键 `id` + 枚举字段，实施时确认）

| 字段 | 类型 | 说明 |
|---|---|---|
| `language` | `ELanguage` | 主键，语言枚举 |
| `name` | `string` | 显示名（简体中文 / English） |
| `icon` | `string` | 国家旗帜图标路径 |
| `sort` | `int` | 语言切换 UI 排序 |
| `separator` | `string` | 该语言的逗号分隔符 |

数据：15 行（与枚举语言成员一一对应，不含 `Unspecified`）。

生成物：`Generate/LanguageDef.cs` + `Generate/TbLanguageDef.cs`（命名空间 `Hotfix.Game.Config`）+ 数据 `tblanguagedef.json`；`TableManager` 由生成器自动纳入加载。

## 5. Luban 新增 `cs-enums` code target

- 注册：`Luban.CSharp` 新增 `[CodeTarget("cs-enums")]`（`CsharpEnumsCodeTarget`）：渲染现有 `enum.sbn` 模板，枚举集为**分组匹配的枚举**（实现时验证 Luban 的枚举导出过滤——优先用枚举 `group` 列（`ELanguage` 设 `aot` 组并同步 `luban.conf` 语义验证 client 侧不受影响）；若 Luban 无枚举分组过滤机制，则按「被任意 target 导出 bean 引用的枚举」全量生成并接受 AOT 侧少量无关枚举）
- 未匹配到任何枚举时输出空且不报错
- aot 段 bat（bin/json 两份）追加 `-c cs-enums -x cs-enums.outputCodeDir=../Unity/Assets/Scripts/AOT/Launch/Localization`，并加 `-x outputSaver.cs-enums.cleanUpOutputDir=false`（该目录有手写文件 LaunchLocalization.cs 与 README——FileCleaner 会误删；ELanguageHelper 在 AOT/Framework/Localization/ 下经 using AOT.Launch.Localization 引用生成枚举）

## 6. 删除手写 `ELanguage.cs` 与两侧适配

- 删除：`Assets/Scripts/AOT/Framework/Localization/ELanguage.cs`（被 cs-enums 生成版**同名同空间顶替**）
- `ELanguageHelper.FromSystemLanguage`：仅删 `using` 调整（生成版同空间），switch 逻辑零改动
- Hotfix 侧 `LocalizationModule`：恢复系统语言映射为私有静态方法（原 `SystemLanguage` 属性逻辑，改操作 `Hotfix.Game.Config.ELanguage`，映射项与现 ELanguageHelper 逐项一致）；`Language` 属性/存储逻辑不变（类型换成生成枚举）
- `LocalizationProvider`：switch 保留，仅换枚举类型与 using
- `LanguageChangeEventArgs`：仅换类型与 using
- `LaunchLocalization`（AOT）：仅换 using（`AOT.Framework.Localization` 不变），逻辑零改动

## 7. 存储兼容

- Hotfix StorageModule 键 `Language` 存**枚举名字符串**（如 `"ChineseSimplified"`）——保留成员名不变，`Enum.TryParse` 与存量数据无缝衔接
- AOT PlayerPrefs 键 `AOT_Localization_Language`（int）：连续编号后与旧值不兼容，但仅有开发期测试数据——实施时清除该键一次（首次读到旧值不在新值域内时按系统语言兜底，行为安全）

## 8. 验证计划

1. `build-luban.bat` 重建 0 警告 0 错误
2. `gen-client-bin/json`：`tblanguagedef.json` + `LanguageDef.cs`/`TbLanguageDef.cs` + client `ELanguage.cs` 生成；两侧 `ELanguage` 成员与值与附录 A 逐项一致；`LocalizationProvider`/`LaunchLocalization` 的 switch 覆盖全部 16 成员
3. aot 段：`cs-enums` 生成 `AOT/Framework/Localization/ELanguage.cs`；`L10nKey`/数据产物不受影响；手写文件不被清理
4. Unity 编译 0 error（删手写 ELanguage.cs 后 Hotfix/AOT 两程序集）
5. Editor 冒烟：启动流程文本正常、语言偏好读取正常（旧 PlayerPrefs 值兼容）
6. `TableManager` 含 `TbLanguageDef` 加载

## 9. 边界与不做的事

- 不做语言切换 UI、icon 加载、separator 的业务应用（只建数据与类型）
- 不改 `LocalizationProvider` 取文本的 switch 结构（数据驱动留作后续）
- 不迁移存量存储（值对齐即天然兼容）

## 附录 A：ELanguage 枚举成员清单（缩减后，16 成员，连续编号）

| 成员 | value | 说明 |
|---|---|---|
| Unspecified | 0 | 未指定 |
| ChineseSimplified | 1 | 简体中文 |
| ChineseTraditional | 2 | 繁体中文 |
| English | 3 | 英语 |
| French | 4 | 法语 |
| German | 5 | 德语 |
| Indonesian | 6 | 印尼语 |
| Italian | 7 | 意大利语 |
| Japanese | 8 | 日语 |
| Korean | 9 | 韩语 |
| PortugueseBrazil | 10 | 葡萄牙语（巴西） |
| PortuguesePortugal | 11 | 葡萄牙语（葡萄牙） |
| Russian | 12 | 俄语 |
| Spanish | 13 | 西班牙语 |
| Thai | 14 | 泰语 |
| Vietnamese | 15 | 越南语 |

**取舍依据**：成员与 `TbLocalization` 现有语言列完全对齐（`PortugueseBrazil` 为 Provider 既有兜底映射例外），每个枚举值都有实际翻译列支撑。**未来补齐**（如 Turkish/Arabic）：枚举表追加成员（value 顺延）→ `TbLocalization` 加语言列 → 语言表加行 → switch 补分支，四步。`LocalizationProvider`/`LaunchLocalization` 的 switch 随缩减同步删除 `Belarusian`/`Ukrainian` 兜底行（`PortugueseBrazil → PortuguesePortugal` 保留）。
