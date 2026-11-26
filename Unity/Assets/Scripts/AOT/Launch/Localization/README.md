# AOT Launch Localization

热更前（AOT 阶段）本地化目录。

## 生成文件（禁止手改，由 `Config/gen-client-bin.bat` 生成）

| 文件 | 说明 |
|---|---|
| `LaunchL10nKey.cs` | AOT 多语言 key 常量（仅 is_code=true 的 key，类名经 `cs-l10n-key.className` 选项配置） |
| `ELanguage.cs` | 语言枚举（`__enums__.xlsx` 语言 sheet 配置生成，cs-enums 目标产出） |
| `LaunchLanguageRegistry.cs` | 语言注册表（`TbLanguageDef` 数据生成，cs-language-registry 目标产出）：语种条目（显示名/排序/分隔符/系统语言映射）、`Fallback`/`FallbackName`（兜底常量，业务不变量=English）、`TryGetByName`、`FromSystemLanguage` |

数据产物：`Assets/Resources/LaunchLocalizationText/tblocalizationaot_<语言>.bytes`（bin 唯一变体）——**按语种拆分**（`split-aot-localization` 后处理），每文件仅 `key + value` 两字段；语种清单与文件集合由生成器自动联动，删语种重导后旧文件自动清理。
AOT 段**只生成数据、key 常量、枚举与注册表，不生成表/管理器代码**——生成代码依赖热更侧配置框架（`BaseDataTable`/`ConfigModule`），AOT 程序集反向引用不可行。

## 手写文件

- `LaunchLocalization.cs`：AOT 本地化门面主体——初始化（注册表定语言 → 加载当前语种文件 + 英语兜底文件）、`GetL10nText`（当前语种 miss 二段查询英语兜底）、语言偏好（字符串名存储，旧 int 值一次性迁移，校验走注册表）。热更后仍可被 Hotfix 侧调用（Hotfix 程序集引用 AOT 程序集）。
- `LaunchLocalization.Parse.cs`：数据解析分部（partial），产物每行仅 `key + value` 两字段：
  - 数据为 bin（Luban ByteBuf）：`ReadSize` 计数 + 逐行 `ReadString`×2。
- **FGUI 声明式多语言的 AOT 阶段委托**：`InitializeAsync` 时注入 `FairyGUI.GObject.GetL10nText = key => LaunchLocalization.GetL10nText(key)`——`WinLauncher` 等启动期界面绑定的 L10n key 由 AOT 表解析；热更后由 `HotfixLauncher` 覆盖为热更表版本（详见 `Hotfix/Framework/Localization/README.md` 第 11 节）。

数据源：`Config/Excels/Local/L-LocalizationAOT-aot-热更前.xlsx`（分组 `aot`）。
语言枚举定义：`Config/Excels/__enums__.xlsx` 语言 sheet（与 Hotfix 侧生成枚举同源，成员集一致）。
语种清单事实源：`Config/Excels/Tables/L-LanguageDef-c-语言定义.xlsx`（`LaunchLanguageRegistry` 的数据源，删/增语种改数据零代码）。
