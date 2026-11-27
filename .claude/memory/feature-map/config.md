# 配置表体系

| 功能 | 路径 |
|---|---|
| 配置模块 | `Unity/Assets/Scripts/Hotfix/Framework/Config/ConfigModule.cs`、`ConfigModule.API.cs` |
| 表基类 / 接口 | `.../Config/BaseDataTable.cs`、`IDataTable.cs` |
| 生成的表代码 | `Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate/` |
| 生成的语言 key | `Hotfix/Game/AutoGen/Tables/Extension/L10nKey.cs` |
| 表管理扩展（多语言刷新接线） | `Hotfix/Game/AutoGen/Tables/Extension/TableManager.Localization.cs` |
| 表源文件（业务表） | `Config/Excels/Tables/*.xlsx` |
| 表源文件（本地化） | `Config/Excels/Local/*.xlsx` |
| 表 / 枚举 / Bean 清单 | `Config/Excels/__tables__.xlsx`、`__enums__.xlsx`、`__beans__.xlsx` |
| 表定义（schema） | `Config/Defines/` |
| Luban 配置 | `Config/luban.conf` |
| 生成脚本 | `Config/gen-client-bin.{bat,sh}`、`Config/gen-server-bin.{bat,sh}` |
| Luban 工具源码与构建 | `Tools/Luban/`（`build-luban.bat`） |
| Luban 生成模板 | `Tools/Luban/source/` |
| L10nKey 代码生成器 | `Tools/Luban/source/src/Luban.CSharp/CodeTarget/CsharpL10NKeyCodeTarget.cs` |
| 本地化 YooAsset collector 同步 | `Config/sync_localization_yooasset_collectors.py` |
| 清理 L10n 无用 key 工具 | `Tools/CleanL10nKeys/` |
| 服务端配置产物 | `Server/` 下对应 Config 目录 |

相关规则：`rules/framework/config-tables.md`、`rules/framework/localization.md`
