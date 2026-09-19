# Config 配置表

本目录是**项目配置表的源数据与生成入口**（基于 [Luban](https://github.com/focus-creative-games/luban)，MIT 许可）。

> **Luban 工具本体**（源码 / 构建脚本 / 可执行产物）已移至 **`Tools/Luban/`** —— 详见 `Tools/Luban/README.md`。

---

## 目录结构

| 项 | 说明 |
|---|---|
| `Defines/` | 表结构定义（schema，XML） |
| `Excels/` | 配置源数据（`.xlsx`） |
| `luban.conf` | Luban 工程配置：表定义、数据目录、导出目标（`client` / `server`） |
| `gen-*.bat` / `gen-*.sh` | 生成脚本（见下） |
| `配置表定义相关说明/` | 表定义相关的图文说明（内容已整理为下方「配置表定义参考」一节） |

## 生成脚本

**必须在 `Config/` 目录下运行**（脚本以相对路径引用 `./Luban.conf` 与 `../Tools/Luban/bin/Luban.dll`）。

| 脚本 | 目标 | 数据产出 | 代码产出 |
|---|---|---|---|
| `gen-client-bin.bat` / `.sh` | 客户端 | `Unity/Assets/Bundles/Config` | `Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/{Generate,LanguageKey}` |
| `gen-client-json.bat` / `.sh` | 客户端 | 同上（JSON 变体） | 同上 |
| `gen-server-bin.bat` / `.sh` | 服务端 | `Server/FuFramework.Config/Json` | `Server/FuFramework.Config/Config` |
| `gen-server-json.bat` / `.sh` | 服务端 | 同上（JSON 变体） | 同上 |

**前置**：通常无需额外操作——脚本会在 `Tools/Luban/bin/Luban.dll` 缺失时（换机 / 新 clone）**自动先构建**（约数秒）；仅当**改动过 Luban 源码**时，才需手动跑 `Tools/Luban/build-luban.bat` 重建。

## 工作流（改表）

1. 编辑 `Excels/*.xlsx`（如涉及结构变更，同时改 `Defines/`）；
2. 在 `Config/` 下运行对应的 `gen-*.bat`；
3. 回 Unity 触发重新编译；
4. 提交生成的代码与数据。

## 配置表定义参考

> 本节为 `配置表定义相关说明/` 下 4 张图的**文本整理**（原图见该目录）。
> 对应数据文件：`Excels/__enums__.xlsx`（枚举）、`Excels/__beans__.xlsx`（类结构）、`Excels/__tables__.xlsx`（表）。

### 1. 基本数据类型与容器类型

| 类型 | 说明 |
|---|---|
| `byte` | 对应 C# 的 `byte`（uint8_t）；可填空，自动赋默认值 |
| `short` | 对应 C# 的 `short`（int16_t）；可填空，自动赋默认值 |
| `int` | 对应 C# 的 `int`（int32_t）；可填空，自动赋默认值 |
| `long` | 对应 C# 的 `long`（int64_t）；可填空，自动赋默认值 |
| `float` | 对应 C# 的 `float`；可填空，自动赋默认值 |
| `double` | 对应 C# 的 `double`；可填空，自动赋默认值 |
| `bool` | `true` / `false` / `0` / `1` 都能识别，**大小写不敏感**（如 `True`、`TRUE` 亦有效）；可填空，自动赋默认值 |
| `string` | 对应 C# 的 `string`；可填空，自动赋默认值 |
| `text` | 本地化（多语言 key）；可填空，自动赋默认值 |
| `datetime` | 对应 C# 的 `long`，值为自 UTC `1970-01-01 00:00:00` 起的**秒数**；**不可填空**。数据格式：`yyyy-mm-dd hh:mm:ss`，或 `yyyy-mm-dd hh:mm`（自动补秒 0）、`yyyy-mm-dd hh`（自动补时分秒 0）、`yyyy-mm-dd`（补 0） |

**容器类型**（`#sep` 为元素分隔符）：

| 类型 | 生成的 C# 类型 | 数据格式 |
|---|---|---|
| `(array#sep),T` | `T[]` | `1,2,3,4` |
| `(list#sep),T` | `List<T>` | `1,2,3,4` |
| `(set#sep),T` | `HashSet<T>`（要求元素唯一） | `1,2,3,4` |
| `(map#sep),T` | `Dictionary<K,V>`（要求键唯一） | `1:1,2:3` |

> **可空**：基本类型与自定义类型都支持可空类型，语法 `<类型>?`（如 `int?`、`Color?`，与 C# 相同）；**容器类型不支持可空**，其 key / value 也不支持可空。

### 2. 枚举类型（`Excels/__enums__.xlsx`）

每个枚举一段，`##var` 行为字段说明、`##` 行起为数据：

| 字段 | 说明 |
|---|---|
| `full_name` | 枚举**全名**（含模块与名字），如 `item.EQuality` |
| `flags` | 是否为**位标记**枚举（每个枚举项为位标记数据，例如 `System.IO.FileMode` 填数据时可以 `READ\|WRITE` 这样表达） |
| `unique` | 枚举项是否唯一 |
| `group` / `comment` / `tags` | 分组 / 注释 / 标签 |
| `items` 下：`name` / `alias` / `value` / `comment` / `tags` | 枚举名 / 别名 / 值 / 注释 / 标签 |

示例：
- `item.EQuality`（`flags=false`、`unique=true`）：`WHITE`=1（白/最差品质）、`BLUE`=2（蓝/蓝色的）、`PURPLE`=3（紫/紫色的）、`RED`=4（红/最高品质）
- `test.AccessFlag`（`flags=true`、`unique=true`）：`WRITE`=1、`READ`=2、`TRUNCATE`=4、`NEW`=8、`READ_WRITE`=（`WRITE\|READ`，位标记使用示例）

### 3. 类结构类型（`Excels/__beans__.xlsx`）

| 字段 | 说明 |
|---|---|
| `full_name` | 结构**全名**（含模块与名字），如 `Property` |
| `parent` | 父类（支持继承） |
| `valueType` | 值类型标记 |
| `sep` | 分割符（用于该结构的紧凑格式中分隔字段） |
| `alias` / `comment` / `group` / `tags` | 别名 / 注释 / 分组 / 标签 |
| `fields` 下：`name` / `alias` / `type` / `group` / `comment` / `tags` / `variants` | 字段名 / 字段别名 / 类型 / 分组 / 注释 / 标签 / 字段变体 |

示例：
- `Property`（别名「属性」）：字段 `PhysicalAttack`（int，物理攻击）、`MagicAttack`（int，魔法攻击）、`PhysicalDefense`（int，物理防御）、`MagicDefense`（int，魔法防御）、`Life`（int，生命值）、`Crit`（int，暴击）、`burstDamage`（int，爆伤）、`precise`（int，精准）、`block`（int，格挡）
- `PropItem`（分割符 `;`）：`Id`（int，道具id）、`Count`（int，道具数量）

### 4. 复杂类型（类 / 容器）的紧凑格式

在**标题头**上以 `#format=xxx` 指定该列的解析格式（三选一：`lite` / `json` / `lua`）。

**`lite` 格式（推荐）**：luban 独有，**无字段名**，比 json / lua 更简洁且解析更高效，适合非常复杂的嵌套结构。指定方式：`position#format=lite`。
- `vec3` 数据 `(1.0,2.0,3.0)` → `{1.0, 2.0, 3.0}`
- `class User{ int id; string name; vec3 pos; }` → `{1, xxxx, {1,2,3}}`

**`json` 格式**：指定方式：`position#format=json`。
- `vec3` `(1.0,2.0,3.0)` → `{"x":1.0, "y":2.0, "z":3.0}`
- `User` → `{"id":1, "name":"xxxx", "pos":{"x":1, "y":2, "z":3}}`

**`lua` 格式**：指定方式：`position#format=lua`。
- `vec3` `(1.0,2.0,3.0)` → `{x=1.0, y=2.0, z=3.0}`
- `User` → `{id=1, name="xxxx", pos={x=1, y=2, z=3}}`

> 实践建议：**简单**复合数据用流式格式，**复杂**复合数据用 `lite`，仅在必要时才用 `json` / `lua`。

---

## 修改和增加

### 增加本地化的文件夹配置支持

在项目中。经常会出现语言表在协作的时候有冲突。这个时候就需要按照每个模块来做表格文件本身的分离，已经不是Sheet的分离的问题了。所以将本地化文件夹配置支持文件夹下的所有文件都识别为本地化文件。

#### 示例配置

- l10n.provider=`fuframework`

这里必须为 `fuframework` 否则会导致本地化文件识别失败。

- l10n.textFile.path=`./Excels/Local/`

这里值必须为 文件夹路径 否则会导致本地化文件识别失败。

导出参数参考

```
--xargs l10n.provider=fuframework --xargs l10n.textFile.keyFieldName=key  --xargs l10n.textFile.path=./Excels/Local/
```

### 本地化体系与语言定义

本地化相关配置由三类表 + 一个配置枚举构成：

| 表 / 枚举 | 位置 | 说明 |
|---|---|---|
| `TbLocalization` | `Excels/Local/L-Localization-*.xlsx` | 多语言文本表：`key` + `is_code`(bool) + 语言列。**`is_code=true` 的 key 才会导出到 L10nKey 常量类**（代码引用用），false 仅进数据供策划表引用；所有本地化表必须带 `is_code` 列，缺失时生成报错终止 |
| `TbLocalizationAOT` | `Excels/Local/L-LocalizationAOT-aot-热更前.xlsx` | AOT 前置文本表（热更进度条等启动期文案），分组 `aot`——数据产物进 `Resources/LaunchLocalizationText/`，代码仅生成 `LaunchL10nKey` 常量与枚举（`cs-l10n-key` + `cs-enums` 目标） |
| `TbLanguageDef` | `Excels/Tables/L-LanguageDef-语言定义.xlsx` | 语言定义表：`language`(ELanguage 主键) / `name` 显示名 / `icon` 旗帜图标 / `sort` 排序 / `separator` 逗号分隔符。供语言切换 UI 等使用 |
| `ELanguage` 枚举 | `Excels/__enums__.xlsx` 语言 sheet | 语言枚举配置化：16 成员（`Unspecified=0` + 15 语言，连续编号），`group=aot`。Hotfix 侧随语言表引用生成（`Hotfix.Game.Config`），AOT 侧经 `cs-enums` 目标生成（`AOT.Launch.Localization`） |

**扩语言四步**：`__enums__.xlsx` 追加枚举成员（value 顺延）→ 各本地化表加语言列 → `L-LanguageDef` 加行 → 代码侧 `LocalizationProvider` / `LaunchLocalization` 的 switch 补分支。

**FGUI 声明式绑定**：UI 组件的多语言由编辑器插件 `L10nKeyBind`（`FairyGUIProject/plugins/`）把 key 写入组件 customData 的 `L10n:` 段，运行时 FairyGUI 构造时自动解析应用（链路与坑见 `Unity/Assets/Scripts/Hotfix/Framework/Localization/README.md` 第 11 节）。

**aot 分组**：`luban.conf` 定义了 `aot` 分组与 `aot` target（详见 `gen-client-bin/json.bat` 的第二段命令）；文件名第 3 段为 `aot` 的表只进入 aot 导出，其余表不受影响。生成器新增的 `cs-enums` 代码目标（`Tools/Luban/source/src/Luban.CSharp/CodeTarget/CsharpEnumsCodeTarget.cs`）仅生成枚举，供 AOT 侧独立获取。

#### 扩展：新增多语言资源类型（如多语言图片 / 音频）

体系当前只有**文本**一种多语言类型。未来要按语言区分其他资源（图片、音频、字体等）时，参照文本表的架构新增一张资源表（以"多语言图片"为例）：

1. **建数据表**：`Excels/Tables/L-LocalizationImage-多语言图片.xlsx`（表名 `TbLocalizationImage`）。结构参照 `TbLocalization`：`key`(string) + `is_code`(bool) + 各语言列（列值为该语言的**资源路径**）。
   **注意：不能放 `Excels/Local/`**——该目录是 `l10n.textFile` 文本校验专用通道，要求表带 `key` 字段（结构不符会在生成期崩溃）；资源表与 `TbLanguageDef` 一样放 `Excels/Tables/`。
2. **生成 key 常量**：把表名 `TbLocalizationImage` 加入 `Tools/Luban/source/src/Luban.CSharp/CodeTarget/CsharpL10NKeyCodeTarget.cs` 的 `CollectKeys` 表名筛选列表，key 常量随 `L10nKey` 一起生成；重建 Luban（`build-luban.bat`）。
3. **运行时提供器**：参照 `Unity/Assets/Scripts/Hotfix/Framework/Localization/LocalizationProvider.cs` 新建取值实现（查表 → switch 当前语言选列 → 空值回退 English 列 → `string.Format` 参数），返回的资源路径交给 `AssetModule` 加载。
4. **产出**：重跑 `gen-client-bin/json`，数据与代码走常规链路（`TableManager` 自动纳入新表加载）。

**联动提醒**：每新增一种多语言类型表，未来"扩语言"时该表也须同步加语言列（与 `TbLocalization` / `L-LanguageDef` / 枚举 sheet 四处联动，见上文扩语言四步）。

### 增加自动导表的文件名称扩展识别

#### 导出参数(必须配置)

```
--xargs tableImporter.name=fuframework
```

#### 说明

格式 [任意字母]-[导出的表名称]-[导出的组名]-[表名称注释].xlsx

表格以任意字母-开头。

中间部分的表名称为英文且不能有空格可以有下划线

导出的组名称必须是定义的`s`、 `c` 之一，可选

后面表名称注释可以接任意长度。程序只取第一个`-` 和第二个`-` 之间的内容加上 `Tb` 为最终表名称。

#### 示例

##### 导出的表名称

L-Localization.xlsx => `Tb`Localization

C-Achievement-成就表.xlsx => `Tb`Achievement

C-Achievement-成就表-AAA.xlsx => `Tb`Achievement

C-Achievement-成就表-AAA-BBB.xlsx => `Tb`Achievement

C-Achievement-成就表-AAA-BBB-CCC.xlsx => `Tb`Achievement

##### 导出的组表名称

C-Achievement-s-成就表.xlsx => `Tb`Achievement, 当前导出目标为 `s` 时才会导出

C-Achievement-c-成就表-AAA.xlsx => `Tb`Achievement, 当前导出目标为 `c` 时才会导出

C-Achievement-s-成就表-AAA-BBB.xlsx => `Tb`Achievement, 当前导出目标为 `s` 时才会导出

C-Achievement-c-成就表-AAA-BBB-CCC.xlsx => `Tb`Achievement, 当前导出目标为 `c` 时才会导出

## 上游参考

- Luban 官方文档：<https://luban.doc.code-philosophy.com/>
- 上游源码（本项目检出）：`Tools/Luban/source/`（含其 `README.md`、`LICENSE`）
- 示例项目：<https://github.com/focus-creative-games/luban_examples>

## License

Luban 采用 [MIT](https://github.com/focus-creative-games/luban/blob/main/LICENSE) 许可。
