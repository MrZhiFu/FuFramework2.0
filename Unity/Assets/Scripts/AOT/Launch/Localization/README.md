# AOT Launch Localization

热更前（AOT 阶段）本地化目录。

## 生成文件（禁止手改，由 `Config/gen-client-*.bat` 生成）

| 文件 | 说明 |
|---|---|
| `L10nKey.cs` | AOT 多语言 key 常量（仅 is_code=true 的 key） |

数据产物：`Assets/Resources/LaunchLocalizationText/tblocalizationaot.bytes`（bin 变体）/ `.json`（json 变体）。
AOT 段**只生成数据与 key 常量，不生成表/管理器代码**——生成代码依赖热更侧配置框架（`BaseDataTable`/`ConfigModule`），AOT 程序集反向引用不可行。

## 手写文件

- `LaunchLocalization.cs`：AOT 本地化门面（加载/语言偏好/GetLanguage），解析自包含：
  - json 变体用 SimpleJSON（`com.code-philosophy.luban` 包 Runtime，autoReferenced）；
  - bin 变体用 `Luban.ByteBuf`（`ReadSize` 计数 + 按 Excel 列顺序 `ReadString`/`ReadBool`）。
  **注意**：bin 解析按 Excel 列顺序硬约定，调整 AOT 表列时须同步 `LocalizationAOTRow` 与 `ParseBin`。
  热更后仍可被 Hotfix 侧调用（Hotfix 程序集引用 AOT 程序集）。

数据源：`Config/Excels/Local/L-LocalizationAOT-aot-热更前.xlsx`（分组 `aot`）。
