# Tools 工具与资源

本目录存放框架的**工具工程**与**辅助资源**。约定：**只放工程 / 资源目录，不放散落脚本**——
工具的使用脚本放在对应业务目录（例如协议生成脚本在 `Protobuf/`）。

---

## 目录一览

| 目录 | 类型 | 说明 |
|---|---|---|
| `ProtoExport/` | .NET 工具工程 | **协议导出工具**：读取 `Protobuf/Proto/*.proto`，生成客户端 / 服务端 C#。由 `Protobuf/Proto2CsExport_*.bat/.sh` 以 `dotnet ProtoExport.dll` 调用。详见 `ProtoExport/README.md`。<br>其 `Dockerfile`、`ProtoExport.sln`、`.gitignore`、`.dockerignore` 随工程置于本目录内（构建上下文 = `ProtoExport/`）。 |
| `UnityCli/` | 集成脚本 | **unity-cli**（让 Claude Code 通过 TCP 操控 Unity Editor）的安装脚本与说明。详见 `UnityCli/README.md`。<br>安装：Windows 双击 `install-unity-cli.bat`，macOS 运行 `bash install-unity-cli.sh`。 |
| `HttpCDN/` | 辅助资源 | **本地 CDN 测试服务**：`miniserve.exe` + 各平台 CDN 根目录（`CDN/Android`、`CDN/IOS`、`CDN/Windows`）。<br>由编辑器菜单 `FuFramework/启动HttpCDN服务器(用于模拟资源更新)` 启动（见 `Assets/Editor/.../Misc/ExeRunner.cs`），服务于 `http://localhost:8080`。 |
| `FairyGUI-Editor-master/` | 三方源码 | 从 GitHub 下载的 **FairyGUI 编辑器源码**（`plugin/` 插件示例：CustomInspector、CustomInspectorTs、HelloWorld、LuaAPI、TsAPI；`ui/` 编辑器 UI 源码）。<br>**仅供编写 FairyGUI 编辑器插件时参考，非本项目的构建依赖，亦无任何脚本引用。** |

---

## 约定

1. **不放散落脚本**：工具的调用脚本放在对应业务目录。
   - 协议生成 → `Protobuf/`（见 `Protobuf/README.md`）
   - 详见本仓库 `CLAUDE.md` 中 unity-cli 的说明。
2. **新增工具**：建独立子目录，并在上表登记「类型 / 说明 / 如何调用」。
3. **构建产物**不入库：`.NET` 的 `obj/`、`bin/` 由各工程的 `.gitignore` 忽略。
