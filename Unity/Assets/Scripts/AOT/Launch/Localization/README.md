# AOT Launch Localization

热更前（AOT 阶段）本地化目录。

## 生成文件（禁止手改，由 `Config/gen-client-*.bat` 生成）

| 文件 | 说明 |
|---|---|
| `LaunchL10nKey.cs` | AOT 多语言 key 常量（仅 is_code=true 的 key，类名经 `cs-l10n-key.className` 选项配置） |
| `ELanguage.cs` | 语言枚举（`__enums__.xlsx` 语言 sheet 配置生成，cs-enums 目标产出） |

数据产物：`Assets/Resources/LaunchLocalizationText/tblocalizationaot.bytes`（bin 变体）/ `.json`（json 变体）。
AOT 段**只生成数据、key 常量与枚举，不生成表/管理器代码**——生成代码依赖热更侧配置框架（`BaseDataTable`/`ConfigModule`），AOT 程序集反向引用不可行。

## 手写文件

- `LaunchLocalization.cs`：AOT 本地化门面主体（初始化/GetLanguage/语言偏好），热更后仍可被 Hotfix 侧调用（Hotfix 程序集引用 AOT 程序集）。
- `LaunchLocalization.Parse.cs`：数据解析分部（partial）：
  - json 变体用 SimpleJSON（`com.code-philosophy.luban` 包 Runtime，autoReferenced）；
  - bin 变体用 `Luban.ByteBuf`（`ReadSize` 计数 + 按 Excel 列顺序 `ReadString`/`ReadBool`）。
  **注意**：bin 解析按 Excel 列顺序硬约定，调整 AOT 表列时须同步 `LaunchL10nRow` 与 `ParseBin`。
- `LaunchL10nRow.cs`：手写行数据类（非生成物；AOT 段不生成表代码，见上）。
- **FGUI 声明式多语言的 AOT 阶段委托**：`InitializeAsync` 时注入 `FairyGUI.GObject.GetLanguageText = key => LaunchLocalization.GetLanguage(key)`——`WinLauncher` 等启动期界面绑定的 L10n key 由 AOT 表解析；热更后由 `HotfixLauncher` 覆盖为热更表版本（详见 `Hotfix/Framework/Localization/README.md` 第 11 节）。

数据源：`Config/Excels/Local/L-LocalizationAOT-aot-热更前.xlsx`（分组 `aot`）。
语言枚举定义：`Config/Excels/__enums__.xlsx` 语言 sheet（与 Hotfix 侧生成枚举同源，成员集一致）。
