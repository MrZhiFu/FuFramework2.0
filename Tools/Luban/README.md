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
gen-client-bin.bat     :: 客户端：.bytes 数据 + C# 代码
gen-server-bin.bat     :: 服务端：.bytes 数据 + C# 代码
```


## 本项目定制（`source/src` 内）

| 模块 | 说明 |
|---|---|
| `Luban.Core/Pipeline/DefaultPipeline.cs` | 管线顺序修正：先 `LoadDatas()` 再启动 code target 任务（自定义 code target 读表数据的可见性保证） |
| `Luban.Schema.Builtin/FuFrameworkTableImporter.cs` | 自定义表导入器：文件名 `编号-表名-[分组]-描述`、按目标分组过滤（**脚本须传 `tableImporter.target`**，否则带分组段的表全部跳过）、前缀编号排序 |
| `Luban.L10N/FuFrameworkTextProvider.cs` | 多语言文本提供器：按目录扫描多份本地化 Excel（`l10n.textFile.path`），按 key 合并 |
| `Luban.CSharp/CodeTarget/CsharpL10NKeyCodeTarget.cs` | 生成 `L10nKey` / `LaunchL10nKey` 常量类（仅 `is_code=true` 的 key） |
| `Luban.CSharp/CodeTarget/CsharpEnumsCodeTarget.cs` | 仅生成枚举，供 AOT 侧独立获取 |
| `Luban.CSharp/PostProcess/LocalizationSplitPostProcess.cs` | 客户端 `TbLocalization` 按语种拆分导出（json/bin 统一 `.bytes`，整表不落盘；产物目录残留自动清理含 `.meta`） |
| `Luban.CSharp/PostProcess/LocalizationAotSplitPostProcess.cs` | AOT 前置表 `tblocalizationaot` 按语种拆分导出（key+value 两字段，写回 outputDataDir 由 FileCleaner 自动清理旧语种） |
| `Luban.CSharp/CodeTarget/CsharpLanguageRegistryCodeTarget.cs` | 生成语言注册表 `LaunchLanguageRegistry`（Entry 为数据驱动字段包：主键 + Fields 字典，LanguageDef 加列零改动；系统语言映射/兜底常量；生成期校验枚举↔表行一致、English 兜底必存、必需列缺失报错） |
| `Luban.CSharp/TemplateExtensions/CsharpTemplateExtension.cs` 的 `EscapeString` | 模板函数 `escape_string`：字符串字面量转义（注册表条目含非 ASCII 显示名/分隔符） |
| `Luban.CSharp/TemplateExtensions/CsharpTemplateExtension.cs` 的条件判定函数 | 模板函数 `bean_needs_translate_text` / `bean_needs_resolve_ref` / `field_generates_resolve_ref_call` / `ref_target_type_name` / `get_get_arg`：**条件生成**的判定核心——无 `text` 字段 / 无跨表引用的 bean 与表，不生成 `TranslateText` / `ResolveRef` 方法及调用行（三层联动：bean.sbn / table.sbn / tables.sbn 必须用同一函数，否则上层调用不存在的下层方法直接编译失败）。`ref_target_type_name` 输出目标 bean 短类名（Luban 逻辑全名与本项目生成命名空间不一致）；`get_get_arg` 对枚举键生成 `(long)` 强转。判定逻辑放 C# 扩展而非模板的原因：scriban 嵌套 for 内赋值不穿透作用域，且模板函数不能有 DefField/TType 双重载（解析歧义） |
| `Luban.CSharp/Templates/cs-dotnet-bin` | 服务端唯一模板（数据变体单 bin）：非 string 键统一 `(long)` 强转；集合转字符串用服务端自带的 `FuFramework.Core.Config.StringUtil`（模板函数 `to_pretty_string_server`） |

> 模板修改以 `source/src/.../Templates/` 为准；`build-luban` 会**先清空 `bin/` 再整体构建**并从 source 拷贝模板，不要手工改 `bin/Templates`。

## 备注

- 上游仓库：<https://github.com/focus-creative-games/luban>（MIT License）。
- `source/docs/`、`source/scripts/`、`source/Tools/` 为上游自带内容，本项目未直接使用，保留以便对照上游用法与历史。
