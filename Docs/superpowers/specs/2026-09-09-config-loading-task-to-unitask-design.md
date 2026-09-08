# Config 加载链全链 Task→UniTask + 生成代码完备注释 设计

> **日期**：2026-09-09
> **分支**：refactor/framework-modules-to-hotfix
> **类型**：架构级改造（触及 Luban 生成模板 + 生成代码 + 框架手写接口）
> **兼容约定**：全程禁止原生 `Task`（代码铁律 #1），一律 `UniTask` 代替；禁止 `Coroutine`/`Task`（铁律 #1/#2）、禁止运行时反射（铁律 #4）。
>
> **双目标**：
> 1. Config 加载链全链 `Task`→`UniTask`（消除铁律 #1 违规）。
> 2. 生成配置表代码（`TableManager` + 所有 `TbXxx` + 所有 bean `Xxx`）的相关方法与私有成员补全完备注释。
>
> 两项都落在**同一批工具版模板**（`cs-simple-json` / `cs-bin`）上，一次模板改动同时覆盖，生成后一起进 git。

## 1. 背景与动机

`HotfixLauncher.cs` 的两个配置加载方法 `ConfigLoader` / `ConfigBufferLoader` 目前返回原生 `Task<JSONNode>` / `Task<ByteBuf>`，违反代码铁律 #1（杜绝原生 Task）。它们以 `Func<string, Task<...>>` 委托传给 Luban 生成的 `TableManager.LoadAsync(...)`，而后者及整条生成链（`BaseDataTable.LoadAsync`、`IDataTable.LoadAsync`、所有 `Tb*.cs`）都在用原生 `Task` 与 `Task.WhenAll`。

本次将客户端 Hotfix 配置加载链**全链**转成 `UniTask`，彻底消除该链路上的原生 `Task`。

## 2. 方案简述（已确认决策）

- **全链转 UniTask**（不是只包一层）。
- **只改客户端 Hotfix**，不碰 server 端（`FuFramework.Config`）。
- **只改工具版模板**（`Config/Tools/Luban/Templates/`），**不改源码版**（`Config/luban/src/Luban.CSharp/Templates/`）。理由：工具版才是真正生成客户端 `Generate/TableManager.cs` 的模板（命名空间 `Hotfix.Framework.Config` 对得上）；源码版命名空间是 `FuFramework.Config.Runtime`，与客户端产物对不上，改它无益且可能破坏。因此**无需重新编译 Luban**。
- **HybridCLR AOT 泛型引用**（`AOTGenericReferences.cs`）由使用方在重生成后手动跑 `GenAOTGenericReferences`。

### 生成代码完备注释（目标 2）

- **覆盖范围**：含私有成员全量——`TableManager`、全部 `TbXxx`、全部 bean `Xxx` 的公开方法与私有成员（`_loadFunc` 字段、`PostInit` 等 partial 声明）都加注释。
- **注释语言**：全中文，符合 `Docs/代码风格规范.md` §2（XML `///`、`<summary>` 换行、含 `<param>`）。
- **来源处理**：保留 Luban Excel 表配置导出的 `comment`（如"道具表"），对缺失的方法/构造/参数/字段注释，由模板写死生成。
- **载体**：所有注释注入在工具版模板（`cs-simple-json` / `cs-bin` 的 `tables.sbn` / `table.sbn` / `bean.sbn`）中，重新生成后随生成代码落盘。

## 3. 触及面

| 层 | 文件 | 改动类型 |
|---|---|---|
| 框架手写 | `Unity/Assets/Scripts/Hotfix/Framework/Config/IDataTable.cs` | 改（Task→UniTask）|
| 框架手写 | `Unity/Assets/Scripts/Hotfix/Framework/Config/BaseDataTable.cs` | 改（Task→UniTask）|
| 框架手写 | `Unity/Assets/Scripts/Hotfix/HotfixLauncher.cs` | 改（Task→UniTask）|
| 模板（工具版·JSON） | `Config/Tools/Luban/Templates/cs-simple-json/table.sbn` | 改（Task→UniTask + 注释）|
| 模板（工具版·JSON） | `Config/Tools/Luban/Templates/cs-simple-json/tables.sbn` | 改（Task→UniTask + 注释）|
| 模板（工具版·JSON） | `Config/Tools/Luban/Templates/cs-simple-json/bean.sbn` | 改（仅注释）|
| 模板（工具版·二进制） | `Config/Tools/Luban/Templates/cs-bin/table.sbn` | 改（Task→UniTask + 注释）|
| 模板（工具版·二进制） | `Config/Tools/Luban/Templates/cs-bin/tables.sbn` | 改（Task→UniTask + 注释）|
| 模板（工具版·二进制） | `Config/Tools/Luban/Templates/cs-bin/bean.sbn` | 改（仅注释）|
| 生成产物 | `Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate/**` | 由改后模板**重生成**（使用方执行）|
| AOT 引用 | `Unity/Assets/Scripts/Hotfix/Game/AutoGen/.../AOTGenericReferences.cs`（部分） | 使用方执行 HybridCLR `GenAOTGenericReferences` |

## 4. 明确排除（不改）

- `cs-l10n-key` 模板（纯同步生成 `LanguageKey.cs` 静态 key 常量，无任何 `Task`/`LoadAsync`；且为 key 列表，无需注释）。
- `bean.sbn` 在 **Task→UniTask** 维度不改（`bean` 无 `LoadAsync`，只有同步 `PostInit`/`ResolveRef`/`TranslateText`，不含 `Task`）；但 **注释** 维度要改（见 §5.5/§5.6）。
- `cs-dotnet-json` / `cs-dotnet-bin` 模板（server 端 `${FuFramework.Config}`，本次不碰）。
- 源码版模板 `Config/luban/src/.../Templates/`（命名空间对不上，见 §2；注释同样不改源码版）。
- server 端生成产物 `Server/FuFramework.Config/`。

## 5. 模板改动明细

> 模板改动原则（Task 维度）：**只把异步链从原生 `Task` 换成 `UniTask`，其余（命名空间、字段名、deserialize 逻辑、`PostResolveRef`/`PostInit` 钩子、`SetTranslateText`/`ResolveRef` 等）一律保持原样**，不引入任何行为变化。
>
> 模板改动原则（注释维度）：**只新增 `///` XML 文档注释与 `<param>` 说明，完全不影响生成代码的逻辑与行为**。注释与 Task 改动互不干扰，同批落在同一模板。

> 关于注释 `__bean.comment` / `escape_comment`：`escape_comment` 是 Luban 内置 Scriban 辅助函数，负责把注释文本安全转义为 `///` 注释（不破坏 `"""` 包裹的缩进）。Luban 表配置导出的 `comment`（如"道具表"）保留不动，模板写死的注释用于补齐缺失的方法/构造/参数/字段说明。

### 5.1 cs-simple-json/tables.sbn

- 头部追加 `using Cysharp.Threading.Tasks;`（已有 `using System;` / `using SimpleJSON;` / `using Hotfix.Framework.Config;`）。
- `LoadAsync(System.Func<string, System.Threading.Tasks.Task<JSONNode>> loader)` → `LoadAsync(System.Func<string, UniTask<JSONNode>> loader)`。
- `var loadTasks = new System.Collections.Generic.List<System.Threading.Tasks.Task>();` → `List<UniTask>();`。
- `await System.Threading.Tasks.Task.WhenAll(loadTasks);` → `await UniTask.WhenAll(loadTasks);`。
- **注释**：`Init`/`LoadAsync`/`SetTranslateText`/`ResolveRef`/`Refresh`/`PostInit` 及其参数/返回补全中文 `///` 注释。`SetTranslateText` 的 `translator` 参数说明含义（`Func<string,string,string>` 传入 key + original → 返回译文）。

### 5.2 cs-simple-json/table.sbn

- 头部追加 `using Cysharp.Threading.Tasks;`。
- 三处私有字段（map / list / one 三种分支）：`private readonly System.Func<System.Threading.Tasks.Task<JSONNode>> _loadFunc;` → `Func<UniTask<JSONNode>> _loadFunc;`。
- 三处构造函数参数：`System.Func<System.Threading.Tasks.Task<JSONNode>> loadFunc` → `Func<UniTask<JSONNode>> loadFunc`。
- 三处 `public override async System.Threading.Tasks.Task LoadAsync()` → `public override async UniTask LoadAsync()`。
- **注释**：`_loadFunc` 私有字段、构造函数（含 `loadFunc` 参数）、`LoadAsync`、`ResolveRef`、`TranslateText`、`PostInit` partial 声明补全中文注释。`LoadAsync` 注明「从 loader 取 JSON、清空字典、逐行反序列化、排序后调用 `PostInit`」。

- 头部追加 `using Cysharp.Threading.Tasks;`（已有 `using Luban;` / `using System;` / `using Hotfix.Framework.Config;`）。
- `LoadAsync(System.Func<string, System.Threading.Tasks.Task<ByteBuf>> loader)` → `Func<string, UniTask<ByteBuf>> loader`。
- `List<System.Threading.Tasks.Task>()` → `List<UniTask>()`。
- `Task.WhenAll(loadTasks)` → `UniTask.WhenAll(loadTasks)`。
- **保留** `PostResolveRef()` 调用与 `partial void PostResolveRef();` 声明（与 Task 无关，原样不动）。
- **注释**：同 §5.1——`Init`/`LoadAsync`/`SetTranslateText`/`ResolveRef`/`Refresh`/`PostInit` 及参数/返回补全中文注释；`PostResolveRef` 注明「所有跨表引用解析完成后调用——后续所有 `SetTranslateText` 的 `translator` 说明与 JSON 版一致」。

### 5.4 cs-bin/table.sbn

- 头部追加 `using Cysharp.Threading.Tasks;`。
- 三处 `_loadFunc` 字段：`Func<System.Threading.Tasks.Task<ByteBuf>> _loadFunc` → `Func<UniTask<ByteBuf>> _loadFunc`。
- 三处构造函数参数同改。
- 三处 `public override async System.Threading.Tasks.Task LoadAsync()` → `async UniTask LoadAsync()`。
- **注释**：同 §5.2——`_loadFunc` 字段、构造函数、`LoadAsync`、`ResolveRef`、`TranslateText`、`PostInit` 补全中文注释；`PostResolveRef` 钩子保留原样。

> 注：cs-bin 的 `table.sbn` 同样含 `PostInit()`/`PostResolveRef()` 钩子（在 63/77/139/157/185/200 行附近），均保留原样。

### 5.5 cs-simple-json/bean.sbn（仅注释，无 Task 改动）

> `bean.sbn` 生成具体数据类（如 `Item`、`Achievement`），不存在 `LoadAsync`、不含 `Task`，**本次 Task→UniTask 不动它**，仅补注释。已有表配置 `comment`（类头/字段）保留。

注释范围：
- 类头：已有 `{{escape_comment __bean.comment}}` 保留；**无 comment 时也强制补模板写死的类级 `<summary>`**（如"<所属表名>数据类"），保证每个 bean 类都有类级注释。
- 两个构造函数：参数形式 `(Type* fields)` 与 `(JSONNode _buf)`，各加 `<summary>` + 参数说明（后者注明「从 JSONNode 反序列化」）。
- `DeserializeXxx(JSONNode)`：`<summary>` + `<param name="_buf">` + `<returns>`。
- `ResolveRef(tables)` / `TranslateText(translator)`：`<summary>` + `<param>`。
- `ToString()`：`<summary>`。
- `__ID__` / `GetTypeId()`：`<summary>`。
- `PostInit()` partial：`<summary>`（注明「分部初始化钩子，由使用方补实现」）。
- 字段：已有 `escape_comment` 的保留；`Xxx_Localization_Key`（多语言 key）、`Xxx_Index`、`GetTypeId` 等补充 `///` 说明。索引字段、`_Ref` 引用字段补「存储 xxx 的引用对象」类说明。
- `__bean.is_value_type`（struct）/ 抽象类型分支：保持结构不改，仅在生成的成员处补注释。

### 5.6 cs-bin/bean.sbn（仅注释，无 Task 改动）

> 与 §5.5 完全一致——`cs-bin` 的 `bean.sbn` 同样无 `Task`，仅补注释。**结构、`__ID__`、deserialize 逻辑、`PostInit`/`PostResolveRef` 钩子一律保持原样**，仅插入 `///` 注释与 `<param>`。

## 6. 手写代码改动明细

### 6.1 `IDataTable.cs`

- 行 3：`using System.Threading.Tasks;` 追加/替换为 `using Cysharp.Threading.Tasks;`（两者不可共存，`UniTask` 已完全覆盖该接口，移除 `System.Threading.Tasks`）。
- 行 18：`Task LoadAsync();` → `UniTask LoadAsync();`。

### 6.2 `BaseDataTable.cs`

- 行 3：`using System.Threading.Tasks;` → 追加 `using Cysharp.Threading.Tasks;`（本文件还需 `Func<...>`/`Action` 等，`System.Threading.Tasks` 移除）。
- 行 35：`public abstract Task LoadAsync();` → `public abstract UniTask LoadAsync();`。

### 6.3 `HotfixLauncher.cs`

- `ConfigBufferLoader`（行 213）：`private static async Task<ByteBuf> ConfigBufferLoader(string file)` → `async UniTask<ByteBuf>`。
- `ConfigLoader`（行 236）：`private static async Task<JSONNode> ConfigLoader(string file)` → `async UniTask<JSONNode>`。
- 方法体内 `await GlobalModule.AssetModule.LoadAssetAsync<TextAsset>(...)` 本已是 UniTask，无其他原生 Task。
- 调用处 `await tableManager.LoadAsync(ConfigBufferLoader)` / `LoadAsync(ConfigLoader)`：委托类型随模板重生成后自然变为 `Func<string, UniTask<...>>`，与 `UniTask` 返回方法组**恰好兼容**，无需改调用源码。

## 7. 委托链数据流（改后）

```
HotfixLauncher.LoadConfigAsync()
  → tableManager.LoadAsync(ConfigBufferLoader)     // Func<string, UniTask<ByteBuf>>
      每表: new TbXxx(() => loader("tables_xxx"))   // _loadFunc : Func<UniTask<ByteBuf>>
      每表: loadTasks.Add(TbXxx.LoadAsync())         // 返回 UniTask
      await UniTask.WhenAll(loadTasks)
  → ConfigBufferLoader(string) → UniTask<ByteBuf>   // await LoadAssetAsync(...) → ByteBuf.Wrap
```

JSON 路径同理（`Func<string, UniTask<JSONNode>>` → `JSON.Parse`）。

## 8. 生命周期（铁律 #5 检查）

配置在启动期 `LoadConfigAsync` 一次性加载，加载后只读、无运行时释放路径。顶层所有者是 HotfixLauncher 的启动流程，loader 内部 `GlobalModule.AssetModule.Token` 透传维持现状。本次**不新增**取消逻辑（与现有行为一致），不引入新的生命周期所有者责任。满足铁律 #5（异步链顶层持 Token、透传、网络/资源类 API 必传 Token——现已经是直传 `AssetModule.Token`，无默认值）。

## 9. 使用方手动步骤（本设计不代执行）

1. **重生成配置代码**：运行 `Config/gen-client-json.bat` 与 `Config/gen-client-bin.bat`（`dotnet ./Tools/Luban/Luban.dll ...`），使 `Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate/**` 基于新模板重新生成。若需手动，命令：
   ```
   cd D:/_WorkSpace/Unity/FuFramework2.0/Config
   dotnet ./Tools/Luban/Luban.dll -t client -d json -c cs-simple-json -c cs-l10n-key -x ... --conf ./Luban.conf
   dotnet ./Tools/Luban/Luban.dll -t client -d bin  -c cs-bin      -c cs-l10n-key -x ... --conf ./Luban.conf
   ```
2. **重新生成 AOT 泛型引用**：Unity 编辑器 `HybridCLR → Settings/Generate/GenAOTGenericReferences`，使 `AOTGenericReferences.cs` 记录新的 UniTask state machine（`<ConfigLoader>d__`、`<ConfigBufferLoader>d__` 类型）。
3. **编译**：Unity 编译通过后，检查生成代码中无 `System.Threading.Tasks` 残留。
4. **运行验证**：Play 进入、`LoadConfigAsync` 成功、`ConfigModuleWindow` 面板表数正确。

## 10. 验证方式

- 模板改完后，`grep` 确认 `Tools/Luban/Templates/{cs-simple-json,cs-bin}/table.sbn, tables.sbn` 已无 `System.Threading.Tasks.Task`、已无 `Task.WhenAll`、已含 `using Cysharp.Threading.Tasks;`。
- 手写代码 `grep` 确认 `IDataTable.cs`/`BaseDataTable.cs`/`HotfixLauncher.cs` 无原生 `Task` 残留（`System.Threading.Tasks` using 移除）。
- 生成代码重生成后，`grep -n "System.Threading.Tasks"` 于 `AutoGen/Tables/Generate` 应为空。
- **注释验证**：重新生成的 `TableManager.cs` / 每个 `TbXxx.cs` / 每个 bean `Xxx.cs` 检查——公开方法（`LoadAsync`/`ResolveRef`/`TranslateText`/`DeserializeXxx`/构造函数）与私有成员（`_loadFunc`、`PostInit`）均有中文 `///` 注释，`<param>` 齐全，注释语言全中文，表配置导出的 `comment`（如"道具表"）未丢失。
- 编译 + Play 冒烟（使用方执行）。

## 11. 风险与注意

- **生成代码依赖模板类型**：手写代码与生成代码是同一契约的互联件，单侧先行会造成编译短暂不一致（`Func<string, Task<...>>` vs `Func<string, UniTask<...>>`）。**本计划采用「一起改完再交付」**：模板与手写代码改动放在同一次交付中，交付后由使用方一次性跑生成 + 编译，避免任何单侧不一致窗口。
- **`AOTGenericReferences.cs`**：若只改代码不重生成 AOT 引用，HybridCLR 热更产物可能缺该 state machine 泛型；务必在提交前由你执行 `GenAOTGenericReferences`。
- **来源模板两套**：仅工具版生效；源码版保持旧版（与工具版长期漂移）。后续若有人重新编译 Luban 源码并覆盖工具版，会引入命名空间回退风险——需在交接说明中记录。
- **注释转义**：在 `.sbn` 模板的 Scriban 内写入的中文注释需经 `{{escape_comment ...}}` 或直接写在 `"""` 块内，避免 `"""`/换行破坏生成。测试生成的 `Tb*.cs` 注释缩进与 `///` 前缀正确。
- **注释不改变生成语义**：注释插入不改变任何字段/方法签名与逻辑；生成代码前后 diff 应只含"`using Cysharp.Threading.Tasks;` + Task→UniTask + 注释行"，不应有其他逻辑改动。
