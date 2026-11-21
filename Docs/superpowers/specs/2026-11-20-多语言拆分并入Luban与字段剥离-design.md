# 多语言拆分并入 Luban 与字段剥离 — 设计文档（v3）

日期：2026-11-20
状态：方案已与用户对齐（三点确认），待用户批准
前置：v1/v2 改造已落地（提交 e8ca90ba + 64e643c7 + 8fb683a2），本篇为第二期扩展

## 一、意图与背景（Why）

第一期已实现"按语种拆分导出与加载"（拆分逻辑在独立工具 `Tools/SplitLocalization`，产物 17 字段、其余语种空串）。使用中发现两个结构性问题：

1. **工具在管线外**：拆分是 Luban 导出后的独立后处理（bat 追加调用），列序约定、ByteBuf 编码、自校验全部在工具内手写实现（SimpleByteBuf 与 Luban ByteBuf 双份语义，已埋一个编码档位污染缺陷）——**同源性差**。
2. **字段冗余**：json 产物每行 15 个空语种字段约 350 字节冗余；bin 产物虽成本极低（每空字段 1 字节），但产物结构不干净。

用户裁定（三点确认）：
- 拆分逻辑**并入 Luban 源码**（PostProcess 管线钩子），独立工具删除；
- 产物剥离到 **2 字段**（key + 目标语种），`is_code` 一并剥离（运行时零消费），**连带剥离生成代码的 `IsCode` 属性**（方案 a，不留僵尸属性）；
- 英语兜底从"导出期固化"改为**运行时二次查英语表**（懒加载英语数据，英语翻译更新即得新兜底，无需重导其他语种）。

## 二、目标 / 非目标 / 反目标

**目标**
1. 拆分成为 Luban 管线内步骤：新增 `[PostProcess]` 处理器，产 15 个拆分文件、删除整表文件（整表不再落盘）；bat 删除工具调用行；`Tools/SplitLocalization/` 目录删除。
2. 产物字段形态（**json/bin 不对称，已修正原 5.2 的 bin 3 字段方案**）：
   - json 变体：每行 **2 键**（`key` + 目标语种），其余 14 语种键缺失 → 生成模板对 Localization bean 加宽容分支（缺键→null），普通表严格性不波及；
   - bin 变体：保持 **16 字段顺序列序**（key + 15 语种，目标语种有值其余空串）→ 模板**零改动**（模板无语言上下文，3 字段列序会让模板耦合目标语种身份；16 字段序与既有读取模板天然对齐）。
3. 运行时英语兜底：`LocalizationProvider` 目标语种查不到（或为空）时，二次查英语表；英语表**懒加载**（首次兜底查询时经 AssetModule 下载+加载 `tblocalization_English`）。
4. 生成代码适配：本地化表 bean 的反序列化（json/bin 双变体）按 2 字段读取；`IsCode` 属性从生成代码剥离。
5. bin/json 双变体对称；服务器侧产物不受影响（PostProcess 只挂 client target）。

**非目标**
- AOT 前置表（`LaunchLocalization` 链路）不动：保持全字段 + 行内 English 兜底（两侧兜底机制不同但语义一致——空则英语，可接受）。
- 服务器侧（server target）不挂 PostProcess，整表照旧。
- 已加载语种缓存仍不做（英语兜底表属懒加载常驻，非切换缓存——与 v2 裁决不冲突）。
- 设置界面 UI 仍不做（沿用 v2 裁决）。

**反目标（出现即失败）**
- 切换语言后业务表翻成旧语言（沿用 v2）。
- 普通配置表（tbitem 等）的反序列化变宽容（掩盖结构错误）——**宽容化只限本地化表**。
- 英语兜底查询路径阻塞主查询（兜底表未加载时必须同步短路返回空，异步触发加载，下一帧查询生效）。

## 三、现状与约束（第二期新增部分）

- Luban 扩展点（已验证源码）：
  - PostProcess：`DefaultPipeline.cs:172` 数据导出后经 `-x dataPostprocess=<名>` 注入，`[PostProcess("名", TargetFileType.Data)]` 类经程序集扫描自动注册（`SimpleLauncher.cs:83-89`，程序集须带 `[assembly: RegisterBehaviour]`——`Luban.CSharp/AssemblyInfo.cs:25` 已有）。
  - 表原始数据：`GenerationContext.Current.GetTableDataInfo(table).FinalRecords`（`CsharpL10NKeyCodeTarget.cs:142` 同款）。
  - bin 写出复用 `BinaryDataVisitor` / `ByteBuf`（WriteSize/WriteString/WriteBool 编码与运行时 Luban.ByteBuf 同源）。
  - manifest 文件操作：`OutputFileManifest.AddFile(OutputFile)`；OutputFile.Content 支持 string（json）或 byte[]（bin）。
- 本地化表识别先例：`CsharpL10NKeyCodeTarget.cs:106` `t.Name is "TbLocalization" or "TbLocalizationAOT"`。
- `is_code` 消费链（已验证）：导出期收集 L10nKey 用（`CsharpL10NKeyCodeTarget.cs:166`）；运行时 `.IsCode` 全仓零调用。
- 反序列化 visitor：json `SimpleJsonDeserializeVisitor.cs:73`（IsString 否则 throw）；bin 逐字段顺序 ReadString（`cs-bin/bean.sbn:191` → `BinaryUnderlyingDeserializeVisitor`）。
- 运行时读取端：`TbLocalization.LoadAsync`（生成代码）→ `Localization.DeserializeLocalization`；`LocalizationProvider.GetLanguage`（目标语种 switch + 行内 English 兜底）。
- v2 既有机制沿用：loader 按语言路由（`HotfixLauncher`）、`SetLanguageAsync`（下载→重填→广播）、YooAsset 15 收集器 tag、产物扩展名统一 `.bytes`。

## 四、备选方案（用户已选）

**方案 P：拆分并入 Luban PostProcess**（已选）
管线内产拆分文件、删整表，复用 Luban 数据结构与 ByteBuf。收益：同源闭环、工具目录消失、产物不落盘中间态。代价：动 Luban 源码（build-luban 工作流已有先例）。

**否决：保留独立工具仅改字段剥离**——同源性差维持现状，与用户"统一性更好"诉求相悖。

**英语兜底：运行时二次查英语表（已选）**
懒加载英语表 + GetLanguage 二段查询。收益：英语更新即生效、产物 2 字段纯净。代价：内存多一份英语表（按需、2 字段轻量）、查询路径多一层判断。
**否决：导出期固化英语到目标语种字段**——剥离字段后无处固化；且英语更新需重导全部语种（用户不要）。

## 五、设计

### 5.1 Luban 侧：LocalizationSplitPostProcess

- 位置：`Tools/Luban/source/src/Luban.CSharp/PostProcess/LocalizationSplitPostProcess.cs`（与 CodeTarget 同程序集，复用其数据访问方式）。
- 注册：`[PostProcess("split-localization", TargetFileType.Data)]`。
- 挂载：`gen-client-bin.bat` / `gen-client-json.bat` 的 client 段 Luban 命令追加 `-x dataPostprocess=split-localization`；**aot 段不挂**（AOT 表不拆）；server 导出（gen-server）不挂。
- 逻辑（伪码级）：
  1. 遍历 manifest.DataFiles，识别表名 `tblocalization`（client target 只会有这一张热更本地化表）；
  2. 从 `GenerationContext.Current` 取该表的 `FinalRecords`（原始 Record，非序列化文本）；
  3. 按当前 Excel bean 的 15 语种字段名数组逐语种产出：
     - json：手写 StringBuilder（2 键格式：key + 目标语种 + UnsafeRelaxedJsonEscaping 保留中文原文）；
     - bin：`ByteBuf`（WriteSize 行数 + 逐行 WriteString(key) + 15 语种 WriteString——**16 字段顺序列序**，目标语种有值其余空串，兜底移交运行时不再固化英语）；
  4. 15 个 `OutputFile`（文件名 `tblocalization_<语种>.bytes`）AddFile 进新 manifest，**移除原整表文件**；
  5. 自校验（沿用 v2 工具语义：抽样行数/目标字段值断言，失败抛异常中断导出）。
- 语种字段名数组：与 Excel 列序一致（v2 工具 `LanguageFields` 迁移），从 bean ExportFields 动态取（排除 key/is_code 后的字符串列）——比硬编码 15 个名字更抗表结构演进。

### 5.2 生成模板：本地化表 2 字段反序列化 + IsCode 剥离

关键约束：**普通表保持严格反序列化，本地化表单独走宽容分支**。实现方式：

- 模板层不做表名判断（sbn 模板无表名上下文）——改为**数据驱动**：json visitor 对"缺字段"行为由 bean 元数据控制太深；**更简单**：本地化表 2 字段产物里 json 的键与 bean 属性一一对应（key → Key 属性），模板生成的反序列化只含**存活字段**（key + 15 语种），产物只写 2 个键 → **缺的 14 个语种键走宽容分支**。
- json 宽容分支实现：`SimpleJsonDeserializeVisitor`（全局 visitor）不动；**在模板字段循环处判 bean**（`cs-simple-json/bean.sbn` 的 `__bean` 上下文存在）：
  - `{{~if __bean.name == 'Localization' && field.type == 'string' && field.name != 'key'~}}` 走宽容函数（缺键→null），else 走原 `deserialize_field`（严格）；
  - `__bean.name == 'Localization'` 精确匹配热更本地化 bean（TbLocalizationAOT 的 bean 名是 LocalizationAOT，不受影响；key 保持严格 throw）。
- bin 变体**模板零改动**：16 字段顺序列序与既有读取模板天然对齐（见目标 2 的不对称形态修正）。
- `IsCode` 剥离：走 Excel `##group` 机制，3 张热更本地化表的 `is_code` 列标记组名 **`none`**——
  **语义：导出代码用（L10nKey 收集的过滤依据），不进入导出产物**。`none` 不属于任何 target 的 groups
  （client=`["c"]` / server=`["s"]` / aot=`["aot"]`）→ 数据产物与 bean 代码**双端**均无 is_code；
  数据加载层按 `HierarchyFields` 全量构造 DBean，导出期 L10nKey 收集仍可取值（`CsharpL10NKeyCodeTarget`
  改查 `HierarchyFields`）——is_code 仅存在于 Excel 源表与导出期决策，不落在任何产物/生成代码中。
- 生成代码重生成后：`Localization.cs` 的 `IsCode` 属性、构造参数、json/bin 反序列化中的 is_code 全部消失；`LocalizationAOT.cs` 不变。

### 5.3 运行时：英语兜底二段查询

- `LocalizationProvider` 改造：
  - 新增 `m_tbEnglishFallback`（英语兜底表引用，懒加载持有）；
  - `GetLanguage`：查当前语言表 → 命中非空返回；未命中/空 → **英语表查询**：已加载则查；未加载则**同步返回空串（记一次日志）+ 异步触发加载**（`EnsureDownloadByTagAsync("English")` + 反序列化，走 AssetModule，token 用 AssetModule.Token——模块常驻）；
  - 目标语言本身是 English 时兜底查询退化为同表（幂等）。
- 加载英语表复用 loader 路由：直接构造 `TbLocalization` 实例（loader 传 "tblocalization"，此时 LocalizationModule.Language 可能不是 English——**路由按 Language 取文件会错**！修正：英语兜底表不走 LanguageModule 路由，loader 拦截逻辑需支持**显式语言参数**的第三入口（如 `LoadLocalizationAsync(ELanguage explicitLanguage)`），或英语表加载用独立表名路由。实施时选最小改动：HotfixLauncher 暴露 `LocalizationExplicitLoader(ELanguage)`，内部直接 `GetLocalizationPath("tblocalization_English")`，不经过 Language 路由）。
- 英语表实例**不注册进 ConfigModule**（私有兜底，不污染配置体系）；TbLocalization 类复用（LoadAsync 逻辑一致）。
- `LocalizationModule` 不新增状态（兜底表归 Provider 私有；SetLanguageAsync 切换后 Provider 当前表原地重填逻辑不变，兜底表引用独立存活）。

### 5.4 产物与消费端对照表（改造后终态）

| 产物 | 内容 | 消费 |
|---|---|---|
| `Bundles/Localization/tblocalization_<语种>.bytes` | json 变体：2 键 JSON 行数组 / bin 变体：WriteSize + 逐行 (key + 15 语种字符串，16 字段序) | client 热更（loader 路由 + 英语兜底懒加载） |
| `Bundles/Config/tblocalization.*` | **不再产出**（整表删除） | 无 |
| AOT 表 | 不变（全字段 + 行内 English 兜底） | AOT 阶段 |

### 5.5 涉及文件清单

| 侧 | 文件 | 改动 |
|---|---|---|
| Luban | `Tools/Luban/source/src/Luban.CSharp/PostProcess/LocalizationSplitPostProcess.cs`（新增） | 拆分 PostProcess |
| Luban | `cs-simple-json/bean.sbn` / `cs-bin/bean.sbn` | 本地化 bean 宽容分支 + is_code 剥离 |
| 导出 | `Config/gen-client-bin.bat` / `gen-client-json.bat` | client 段加 `-x dataPostprocess=split-localization`；删工具调用行 |
| 删除 | `Tools/SplitLocalization/` | 整目录删除 |
| 运行时 | `Unity/Assets/Scripts/Hotfix/Framework/Localization/LocalizationProvider.cs` | 英语兜底二段查询 + 懒加载 |
| 运行时 | `Unity/Assets/Scripts/Hotfix/HotfixLauncher.cs` | 显式语言 loader 入口（英语兜底表加载） |
| 生成 | `Localization.cs` 等（Luban 重生成） | IsCode 消失、反序列化 2 字段化 |

### 5.6 验收标准

1. 双变体导出：15 个 2 字段产物落盘；`Bundles/Config/tblocalization.*` 不存在；普通表产物不变。
2. Play 启动：配置加载成功；内存 `Localization` 行对象无 `IsCode` 属性、语种字段仅目标语种有值其余恒 null/空（json 宽容分支产物缺键）。
3. 切换语言闭环（v2 全套断言复测）。
4. 英语兜底：构造一个"目标语种值为空"的 key（Excel 里清空该语种），查询返回英语值；首次兜底触发英语表懒加载日志；英语数据更新后无需重导其他语种即生效（重导英语单独验证可选）。
5. 普通表严格性：tbitem 等 json 产物缺字段仍 throw（模板分支未波及）。
6. 服务器侧：gen-server 产物无 Localization 拆分痕迹（PostProcess 未挂 server target）。

## 六、交付约定（沿用 v2）

- 提交规范 `Docs/Git提交规范.md`；**提交前询问用户确认**（新增铁律）；代码风格 + CLAUDE.md 五项铁律。
- Luban 源码改动后须 `build-luban.bat` 重建再 gen（既有工作流）。
- 验证 = Luban 自校验 + unity-cli 编译归零 + Play 探针实测。

## 七、遗留实施验证项

- server target 的数据导出是否经同一管线（PostProcess 只挂 client 的具体写法：`-x dataPostprocess` 是 per-target 选项，server 导出走 gen-server 独立 bat 无此参数即天然隔离——实施时验证）。
- json 宽容分支对 `key` 键保持严格 throw（产物永远带 key，不宽容）。
- 英语兜底懒加载的并发防护（同一 key 连续 miss 只触发一次加载）。
- `TranslateText` 机制对本地化表 bean 的引用是否受 IsCode 剥离影响（TranslateText 只翻语种字段，预期无关，实施核对）。
