# 多语言

| 功能 | 路径 |
|---|---|
| 本地化模块 | `Hotfix/Framework/Localization/LocalizationModule.cs`、`LocalizationModule.API.cs` |
| 语言变更事件 | `Localization/LanguageChangeEventArgs.cs` |
| 表多语言刷新接线 | `Hotfix/Game/AutoGen/Tables/Extension/TableManager.Localization.cs` |
| 生成的语言 key（热更） | `Hotfix/Game/AutoGen/Tables/Extension/L10nKey.cs` |
| 生成的语言 key（AOT） | `AOT/Launch/Localization/AutoGen/LaunchL10nKey.cs` |
| AOT 本地化运行时 | `AOT/Launch/Localization/LaunchLocalization.cs` |
| AOT 本地化数据 | `Unity/Assets/Resources/LaunchLocalizationText/` |
| AOT 本地化表源 | `Config/Excels/Local/L-LocalizationAOT-aot-热更前.xlsx` |
| 语言定义表 | `Config/Excels/Tables/L-LanguageDef-语言定义.xlsx` |
| 语言枚举（生成） | `Hotfix/Game/AutoGen/Tables/Generate/ELanguage.cs` 与 `AOT/Launch/Localization/AutoGen/` 各一份 |
| 系统语言映射（AOT） | `AOT/Launch/Localization/AutoGen/LaunchLanguageRegistry.cs`（`FromSystemLanguage`） |
| 系统语言映射（热更） | `LocalizationModule`（`FromSystemLanguage`） |
| FGUI 声明式绑定插件 | `FairyGUIProject/plugins/L10nKeyBind/` |
| FGUI 绑定运行时钩子 | `3rdPlugins/FairyGUI/Scripts/UI/CustomExt/*.L10n.cs` |
| 语种数据产物 | `Unity/Assets/Bundles/Config/Data/`、`Unity/Assets/Bundles/Config/Localization/` |
| 生成产物与 YooAsset collector 同步 | `Config/sync_localization_yooasset_collectors.py` |

相关规则：`rules/framework/localization.md`
