# Luban 配置表生成工具

本目录存放 **Luban**（配置表解决方案）的**源码、构建脚本与构建产物**。

- **来源**：`source/` 是上游 Luban 仓库检出（**MIT**，见 `source/LICENSE`、`source/README.md`）；本项目在其基础上按需改造（见 `source/src`）。
- **被谁使用**：`Config/gen-*.{bat,sh}` 调用 `bin/Luban.dll` 生成配置表代码与数据。**配置源与生成入口在 `Config/`**（详见 `Config/README.md`）。
- **不被 Unity 工程引用**：纯构建期工具。

---

## 目录

| 目录 / 文件 | 说明 |
|---|---|
| `source/` | Luban 上游源码：`src/`（各 .NET 工程）、`docs/`、`scripts/`、`Tools/`、`LICENSE`、`README*.md` |
| `bin/` | 由 `source/src` 构建出的可执行产物（`Luban.dll` 等）。**不入库**（已 gitignore）——**换机/新 clone 后需构建一次**（见下） |
| `build-luban.bat` / `.sh` | 从 `source/src` 构建到 `bin/`（会**先清空 `bin/` 再重建**） |

## 构建

**通常无需手动构建**：`Config/gen-*.bat/.sh` 会在检测到 `bin/Luban.dll` **缺失时自动先构建**（换机 / 新 clone 后直接跑生成脚本即可）。

仅当**改动了 Luban 源码**时，需要手动重建：

```bat
:: Windows（双击或命令行；末尾加 ci 可免 pause，供脚本调用）
Tools\Luban\build-luban.bat
```
```bash
bash Tools/Luban/build-luban.sh
```
> `bin/` 不入库，故重建**不会**产生 git 变更。

## 使用

不改源码时**无需构建**，直接在 `Config/` 下跑生成脚本：

```bat
cd Config
gen-client-bin.bat     :: 客户端：二进制数据 + C# 代码
gen-client-json.bat    :: 客户端：JSON 变体
gen-server-bin.bat     :: 服务端
gen-server-json.bat    :: 服务端（JSON）
```

## 本项目定制（`source/src` 内）

| 模块 | 说明 |
|---|---|
| `Luban.Schema.Builtin/FuFrameworkTableImporter.cs` | 自定义表导入器：文件名 `编号-表名-[分组]-描述`、按目标分组过滤（**脚本须传 `tableImporter.target`**，否则带分组段的表全部跳过）、前缀编号排序 |
| `Luban.L10N/FuFrameworkTextProvider.cs` | 多语言文本提供器：按目录扫描多份本地化 Excel（`l10n.textFile.path`），按 key 合并 |
| `Luban.CSharp/CodeTarget/CsharpL10NKeyCodeTarget.cs` | 生成 `L10nKey` / `LaunchL10nKey` 常量类（仅 `is_code=true` 的 key） |
| `Luban.CSharp/CodeTarget/CsharpEnumsCodeTarget.cs` | 仅生成枚举，供 AOT 侧独立获取 |
| `Luban.CSharp/PostProcess/LocalizationSplitPostProcess.cs` | 客户端 `TbLocalization` 按语种拆分导出（json/bin 两变体统一 `.bytes`，整表不落盘） |
| `Luban.CSharp/Templates/cs-dotnet-json`、`cs-dotnet-bin` | 服务端专用模板：非 string 键统一 `(long)` 强转；集合转字符串用服务端自带的 `FuFramework.Core.Config.StringUtil`（模板函数 `to_pretty_string_server`） |

> 模板修改以 `source/src/.../Templates/` 为准；`build-luban` 会**先清空 `bin/` 再整体构建**并从 source 拷贝模板，不要手工改 `bin/Templates`。

## 备注

- 上游仓库：<https://github.com/focus-creative-games/luban>（MIT License）。
- `source/docs/`、`source/scripts/`、`source/Tools/` 为上游自带内容，本项目未直接使用，保留以便对照上游用法与历史。
