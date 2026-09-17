# AOT 前置本地化（TbLocalizationAOT）设计文档

- 日期：2026-09-17
- 状态：待用户审阅
- 分支：refactor/framework-modules-to-hotfix

## 1. 背景与目标

热更前（AOT 阶段）的 UI 文本目前为硬编码（`LaunchProcess.cs` / `LaunchView.cs`），无法多语言化。本设计新增一张独立本地化表 **`TbLocalizationAOT`**，其数据产物放 `Assets/Resources/`（AOT 阶段 YooAsset 未就绪，Resources 是既有加载通道，如 `LaunchView` 加载 FUI 包），代码产物放 `Assets/Scripts/AOT/Launch/Localization/`，支持 bin / json 两种数据变体，并接入启动流程替换全部硬编码文本。

## 2. 决策记录

| 决策点 | 结论 | 理由 |
|---|---|---|
| 表名 | 改文件名获得独立表 `TbLocalizationAOT` | 按现有导入规则 `L-Localization-AOT` 会合并进 `TbLocalization`；改导入器源码会让规则复杂化 |
| Excel 文件名 | `L-LocalizationAOT-aot-热更前.xlsx` | 第 2 段=表名，第 3 段 `aot`=分组（导入器既有能力） |
| key 列名 | 统一为 `key`（现表头为 `v`） | `l10n.textFile.keyFieldName=key` 校验与 L10nKey 收集均认 `key`，不改会崩 |
| 隔离机制 | Luban 原生分组 + 独立 target（方案 A） | 零侵入生成管线；扩展源码按表路由输出（方案 B）侵入 manifest 语义，否决 |
| 脚本组织 | 并入现有 `gen-client-bin/json.bat` 追加第二段命令 | 一条命令出全部产物，不会忘记同步 |
| 语言来源 | 用户持久化选择优先，系统语言兜底 | 与 Hotfix 初始逻辑对齐，热更前后语言一致 |
| 语言枚举 | `ELanguage` 下沉到 AOT 共享 | Hotfix 引用 AOT asmdef 可行；复制枚举则需双向转换，否决 |
| 存储衔接 | AOT 用 PlayerPrefs 独立存语言；Hotfix 接管后以 Hotfix 存储为准，不回写 | 不一致窗口仅热更几秒，避免动 Hotfix 存储链路 |

## 3. 生成链路

### 3.1 配置与文件

- Excel 重命名：`Config/Excels/Local/L-Localization-AOT.xlsx` → `L-LocalizationAOT-aot-热更前.xlsx`；key 列头 `v` → `key`
- `luban.conf`：
  - `groups` 增加 `{"names": ["aot"], "default": false}`
  - `targets` 增加 `{"name": "aot", "manager": "TableManager", "groups": ["aot"], "topModule": "AOT.Launch.Localization"}`
  - 现有 `client` / `server` / `all` 三个 target 不动
- 隔离原理：`FuFrameworkTableImporter` 按文件名第 3 段分组过滤——`client`（分组 c）看不到 `aot` 分组的表；`aot` target 只导出 `aot` 表。现有 3 张本地化表与全部业务表无分组段，行为不变。

### 3.2 生成脚本

`gen-client-bin.bat` / `gen-client-json.bat` 现有命令之后追加（bin 示例；json 变体为 `-d json -c cs-json`）：

```bat
dotnet ../Tools/Luban/bin/Luban.dll ^
    -t aot -d bin -c cs-l10n-key ^
    -x outputDataDir=../Unity/Assets/Resources/LaunchLocalizationText ^
    -x cs-l10n-key.outputCodeDir=../Unity/Assets/Scripts/AOT/Launch/Localization ^
    -x outputSaver.cs-l10n-key.cleanUpOutputDir=false ^
    -x tableImporter.name=fuframework ^
    -x tableImporter.target=aot ^
    -x l10n.provider=fuframework ^
    -x l10n.textFile.keyFieldName=key ^
    -x l10n.textFile.path=./Excels/Local/ ^
    --conf ./Luban.conf
```

> 修订说明（实施期裁定 R1'/R2/R3）：
> 1. `-x tableImporter.target=aot` 使导入器文件级分组判定生效（缺失时 `ExportTarget=null`，带分组段的表被静默跳过）。
> 2. `FuFrameworkTableImporter` 已把文件名分组段传播至 `RawTable.Groups`——此前恒空会导致显式分组表被 Luban 表级过滤丢弃（`DefAssembly.NeedExport`：空 Groups 表只属于 default 组，`aot` 组 `default=false`）。无分组段表天然不进 aot target，无需额外开关。
> 3. **AOT 段不再生成表代码**（曾计划 `-c cs-bin`/`-c cs-simple-json`）：生成的表类/管理器硬依赖热更侧框架基类（`BaseDataTable`/`ConfigModule`），AOT 程序集反向引用不可行。AOT 段只生成 `L10nKey` 常量与数据产物，运行时解析由手写的 `LaunchLocalization` 自包含完成（见 4.2）。

### 3.3 产物布局

| 产物 | 位置 |
|---|---|
| 数据 | `Assets/Resources/LaunchLocalizationText/tblocalizationaot.bytes`（json 变体 `.json`） |
| 生成代码 | `Assets/Scripts/AOT/Launch/Localization/L10nKey.cs`（命名空间 `AOT.Launch.Localization`；AOT 段不生成表/管理器代码，理由见 3.2 修订说明 3） |
| 手写代码 | `Assets/Scripts/AOT/Launch/Localization/` 根目录：`LaunchLocalization.cs`（含 `LocalizationAOTRow` 行数据类与 json/bin 自包含解析）、`README.md` |

### 3.4 Luban 源码微调（两处）

1. `CsharpL10NKeyCodeTarget.CollectKeys` 表名筛选：`t.Name is "TbLocalization" or "TbLocalizationAOT"`（显式列表）。
2. `FuFrameworkTableImporter`：把文件名第 3 段解析出的分组名经 `ParseCommentAndExport`/`TableInfo` 传播至 `RawTable.Groups`——缺失时显式分组表因 Groups 恒空被表级过滤丢弃（`DefAssembly.NeedExport` 语义：空 Groups 表仅当 target 含 default 组才导出，`aot` 组 `default=false`）。

改动后需 `Tools/Luban/build-luban.bat` 重建。

## 4. AOT 运行时

### 4.1 ELanguage 下沉（前置重构）

- `ELanguage.cs` 移至 `Assets/Scripts/AOT/Framework/Localization/`，命名空间 `AOT.Framework.Localization`
- Hotfix `LocalizationModule` 内「`SystemLanguage` → `ELanguage`」映射下沉为 `ELanguage.FromSystemLanguage(SystemLanguage)` 静态方法，Hotfix 改调用并删除重复映射
- Hotfix 侧使用文件（`LocalizationModule` / `LocalizationProvider` / `LanguageChangeEventArgs`）仅改 `using`，行为零变化

### 4.2 LaunchLocalization（AOT 本地化门面）

`Assets/Scripts/AOT/Launch/Localization/LaunchLocalization.cs`，静态类，脱离 UIModule/EventModule 自包含（与 `LaunchView` 同一原则）：

- `InitializeAsync()`：`Resources.Load<TextAsset>("LaunchLocalizationText/tblocalizationaot")` 同步加载；解析**自包含**（json 变体用 SimpleJSON 逐行取字段；bin 变体用 `Luban.ByteBuf` 按记录数 + Excel 列序读取，该列序为硬约定，调整表列须同步解析代码）；解析异常仅 LogError 并清空数据，不阻断启动
- `Language` 属性：PlayerPrefs（int，`(int)ELanguage`）中用户选择优先；无记录时 `ELanguage.FromSystemLanguage(Application.systemLanguage)` 兜底
- `GetLanguage(key, args)`：查行数据字典 → 当前语言字段 → 空则回退 `English`（与 Hotfix 版同构）→ 有参数则 `string.Format`；查无 key 时 LogError 返回空串

## 5. LaunchProcess / LaunchView 接线

初始化时机：启动流程开头调用 `LaunchLocalization.InitializeAsync()`（同步解析、微秒级）。文本对照（key 以 AOT 表实际为准，全部 `is_code=true`）：

| key | 现硬编码 | 位置 |
|---|---|---|
| `aot_init_res_package` | `"InitPackage..."` | LaunchProcess L64 |
| `aot_get_res_version` | `"GetVersion..."` | L78 |
| `aot_get_res_version_fail` | `"获取版本号失败，正在重试..."` | L82 |
| `aot_update_res_manifest` | `"UpdateManifest..."` | L87 |
| `aot_update_res_manifest_fail` | `"更新清单失败，正在重试..."` | L90 |
| `aot_req_remote_update_config_fail` | `"请求远端更新配置失败，正在重试..."` | L131 |
| `aot_res_downloading` | `$"下载中：{cur}/{tot}"`（key 含 `{0}/{1}` 占位） | L164 |
| `aot_res_download_fail` | `"下载失败，正在重试..."` | L174 |
| `aot_res_load_fail` | `"资源加载失败，请检查网络后重启游戏!"` | L203 / L219（共用） |
| `aot_update_dialog_ok_btn` | `"更新"` | LaunchView `ShowUpdateDialog` 按钮文本 |
| `aot_res_loading_config` | `"正在加载配置..."` | HotfixLauncher `InitDependenciesAsync`（经 AOT 门面） |
| `aot_res_loading_init_res` | `"正在加载初始化资源..."` | 同上 |

> **归属定案（用户确认）**：`aot_res_loading_config`（正在加载配置...）/ `aot_res_loading_init_res`（正在加载初始化资源...）**保留在 AOT 表**。对应文本虽位于 `HotfixLauncher.InitDependenciesAsync`（热更程序集），但 Hotfix 程序集引用 AOT 程序集——这两处通过 AOT 门面 `LaunchLocalization.GetLanguage(...)` 接线（`LaunchLocalization` 已在启动流程开头初始化，热更后全程可用），一处数据热更前后通用。

范围外：更新公告与下载地址来自远端 `RemoteUpdateConfig`，不本地化；`SetTip(string.Empty)` 清空调用不变。

## 6. 验证计划

1. `Tools/Luban/build-luban.bat` 重建（3.4 源码微调后必须），构建 0 警告 0 错误
2. 跑 `gen-client-bin.bat` / `gen-client-json.bat`：生成无报错（含 is_code 缺列校验路径）；检查 `Resources/LaunchLocalizationText/` 数据产物与 `AOT/Launch/Localization/` 生成代码；确认 Hotfix 侧产物零 diff（client target 隔离验证）
3. Unity 编译通过（ELanguage 下沉后 Hotfix / AOT 两程序集）
4. Editor 冒烟：启动流程进度条文本按语言渲染、确认框按钮文本生效

## 7. 边界与不做的事

- 不做 Hotfix 侧语言存储向 AOT 的回写
- 不做远端公告/下载地址的本地化
- 不扩展导入器源码（表名靠文件名规则解决）
- 不改 `client` / `server` / `all` 三个现有 target 的任何配置
- 顺带清理孤儿文件 `Unity/Assets/Editor/FuFramework/Localization.meta`（无对应目录的残留 meta）
