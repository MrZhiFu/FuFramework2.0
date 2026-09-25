# Tools 工具与资源

本目录存放框架的**工具工程**与**辅助资源**。约定：**只放工程 / 资源目录，不放散落脚本**——
工具的使用脚本放在对应业务目录（例如协议生成脚本在 `Protobuf/`）。

---

## 目录一览

| 目录 | 类型 | 说明 |
|---|---|---|
| `ProtoExport/` | .NET 工具工程 | **协议导出工具**：读取 `Protobuf/Proto/*.proto`，生成客户端 / 服务端 C#。由 `Protobuf/Proto2CsExport_*.bat/.sh` 以 `dotnet ProtoExport.dll` 调用。详见 `ProtoExport/README.md`。<br>其 `Dockerfile`、`ProtoExport.sln`、`.gitignore`、`.dockerignore` 随工程置于本目录内（构建上下文 = `ProtoExport/`）。 |
| `Luban/` | 三方工具 + 构建 | **配置表生成工具（Luban）**：`source/` 为上游源码检出（MIT）、`bin/` 为构建产物（已入库）、`build-luban.bat/.sh` 为构建脚本。详见 `Luban/README.md`。<br>由 `Config/gen-*.bat/.sh` 调用；配置源与生成入口在 `Config/`。 |
| `UnityCli/` | 集成脚本 | **unity-cli**（让 Claude Code 通过 TCP 操控 Unity Editor）的安装脚本与说明。详见 `UnityCli/README.md`。<br>安装：Windows 双击 `install-unity-cli.bat`，macOS 运行 `bash install-unity-cli.sh`。 |
| `HttpCDN/` | 辅助资源 | **本地 CDN 测试服务**：`miniserve.exe` + 各平台 CDN 根目录（`CDN/Android`、`CDN/IOS`、`CDN/Windows`）。<br>由编辑器菜单 `FuFramework/启动HttpCDN服务器(用于模拟资源更新)` 启动（见 `Assets/Editor/.../Misc/ExeRunner.cs`），服务于 `http://localhost:8080`。 |
| `FairyGUI-Editor-master/` | 三方源码 | 从 GitHub 下载的 **FairyGUI 编辑器源码**（`plugin/` 插件示例：CustomInspector、CustomInspectorTs、HelloWorld、LuaAPI、TsAPI；`ui/` 编辑器 UI 源码）。<br>**仅供编写 FairyGUI 编辑器插件时参考，非本项目的构建依赖，亦无任何脚本引用。** |
| `CleanL10nKeys/` | Python 脚本 + 调用脚本 | **多语言表健康检查工具**（五项检查）：① 未引用 key（可清理）② 缺失 key（已引用但表中没有）③ 翻译覆盖率（逐语言空列统计）④ FGUI 硬编码文本（未绑 L10n 的静态中文）⑤ key 命名规范 lint。引用源为 代码（`Assets/Scripts`，排除生成的 `L10nKey.cs`/`LaunchL10nKey.cs`）、配置数据（`Bundles/Config`，排除本地化表自身导出产物）、FGUI 源（`FairyGUIProject/assets` 的 customData）。<br>`src/CleanL10nKeys.py` 为核心脚本（`--apply` 附加删除未引用 Excel 行，不导表）；`clean-l10n-keys-preview.bat/.sh` 仅检查，`clean-l10n-keys-apply.bat/.sh` 检查+删除。完整报告写入 `多语言配置报告.txt`（gitignore），控制台只输出摘要。动态拼接的 key 无法静态检测，执行前须人工核对报告。<br>Unity 菜单：`FuFramework/多语言检查/生成现存问题报告 / 打开现存问题报告 / 清理多语言配置表`（见 `Assets/Editor/FuFramework/Localization/L10nKeysCleaner.cs`）。 |

---

## 约定

1. **不放散落脚本**：工具的调用脚本放在对应业务目录。
   - 协议生成 → `Protobuf/`（见 `Protobuf/README.md`）
   - 详见本仓库 `CLAUDE.md` 中 unity-cli 的说明。
2. **新增工具**：建独立子目录，并在上表登记「类型 / 说明 / 如何调用」。
3. **构建产物**不入库：`.NET` 的 `obj/`、`bin/` 由各工程的 `.gitignore` 忽略。
