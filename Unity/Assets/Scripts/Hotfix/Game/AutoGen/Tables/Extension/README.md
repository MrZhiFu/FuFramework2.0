# Tables/Extension 目录说明

本目录是**生成代码 + 少量手写扩展**的混居目录。`gen-client-*.bat` 的 `cs-l10n-key` / `cs-l10n-accessor` 两个 code target 对本目录配置了 `outputSaver.<target>.cleanUpOutputDir=false`（**不清理输出目录**），因此目录里未被生成器写入的手写文件不会被删除——但这也意味着**任何时候都不能对生成目标开启清理**（cleanUpOutputDir 改 true 或去掉），否则会删光手写文件。

> 对照：兄弟目录 `Tables/Generate/`（`cs-simple-json` / `cs-bin` 目标）是**纯生成目录**（无手写文件，清理默认开启、安全）——新增生成文件应放那里，本目录只收"生成常量/访问器 + 手写扩展"。

## 文件清单

| 文件 | 属性 | 说明 |
|---|---|---|
| `L10nKey.cs` | **生成** | 多语言 key 常量类（`is_code=true` 的 key），由 `cs-l10n-key` 目标产出 |
| `LocalizationTextAccessor.cs` | **生成** | 取词访问器（GetText 语言分支随本地化表语言列自动增减），由 `cs-l10n-accessor` 目标产出 |
| `TableManager.Localization.cs` | **手写** | `TableManager` 的本地化分部（partial）：订阅语言切换事件、重载本地化表（`ReloadLocalizationAsync`）、翻译委托。依赖生成代码但本身不参与生成 |
| `ExternalTypeUtil.cs` | **手写** | 外部类型扩展工具 |

## 约定

1. **生成文件禁止手改**——重跑 gen 会被覆盖。
2. **cleanUpOutputDir 必须保持 false**：`cs-l10n-key` 与 `cs-l10n-accessor` 指向本目录，开启清理会连带删除手写文件（见 `Config/gen-client-*.bat` 的 `outputSaver.cs-l10n-key.cleanUpOutputDir=false` / `outputSaver.cs-l10n-accessor.cleanUpOutputDir=false`）。
3. 新增生成目标若指向本目录，同样必须配 `cleanUpOutputDir=false`；新增手写文件请在本 README 登记属性。
4. 若未来手写文件增多到失衡，考虑把 `TableManager.Localization.cs` 等迁往非 AutoGen 路径（如 `Hotfix/Game/Config/`），迁移时同步修改命名空间与引用。
