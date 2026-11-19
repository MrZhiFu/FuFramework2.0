# 模块常量收敛至 Const 分部——实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 Hotfix/Framework 下 7 个模块散落的运行时参数常量收敛到各自 `XxxModule.Const.cs` partial 分部（对齐 NetworkModule.Const.cs 先例），每模块经用户确认后单独提交。

**Architecture:** 循环式篇章流程——每模块独立完成「盘点甄别 → 建 Const 分部 → 迁移+引用切换 → A4 参数链检查 → 编译归零 → README 检查 → 汇报 → 用户确认 → 提交」。模块间零依赖，可独立提交。

**Tech Stack:** C#（Unity 热更程序集）、partial class、unity-cli 编译验证

**Spec:** `docs/superpowers/specs/2026-11-19-module-const-consolidation-design.md`

## Global Constraints

- **铁律**：禁止 LINQ/协程/原生 Task/运行时反射；异步链必须有 CancellationTokenIssuer 生命周期所有者
- **缩进一律 Tab**（全仓库已 Tab 化）；新文件头 `// ReSharper disable once CheckNamespace` + 命名空间与所在模块一致
- **partial 类内 private 成员跨分部文件互见合法**——迁移常量**不改变可见性**（private 留 private）
- **豁免不搬清单**：就地 `const string errorMessage`、Editor-only 调试自包含常量（WebModule.Debug.cs、NetworkDebug.cs）、嵌套类/辅助类自包含参数（SoundGroup.STORAGE_KEY_PREFIX、StorageHelper 容差、RedDotKey.INVALID_ENUM_VALUE）、`static readonly` 数据字段（非参数）、算法固定参数（Core 已整体跳过）
- **编译验证**：`unity-cli tool call refresh_assets` → 轮询 `unity-cli tool call get_compilation_state` 至 `isCompiling: false && errorCount: 0`
- **每模块提交前必须用户确认**；提交信息 `[AI]refactor: <模块名>模块常量收敛至 Const 分部`
- **批量文本替换**：多文件用 python 临时脚本，禁用 PowerShell `-replace`（大小写不敏感陷阱）
- **文件命名**：`<模块目录>/<模块名>Module.Const.cs`（先例：`NetworkModule.Const.cs`；模块目录名不带 Module 后缀）

## Review Focus

- **常量可见性回退**：迁移时若把 private 常量误搬为独立类（非 partial 同类）会编译失败——每任务编译步骤兜底
- **引用切换遗漏**：常量本体删除后若引用点未切换，编译报错兜底；但 `<see cref>` 注释引用不参与编译——每任务收尾 grep 旧常量名检查注释零残留
- **A4 误删定制调用方**：删默认值参数前必须全项目 grep 调用方（含 Editor 面板/反射调用），有定制即保留参数
- **README 失同步**：README 中的常量值/参数描述与本轮改动不一致时更新（各任务内嵌步骤）
- **Tab/空格混入**：新 Const 文件与替换行一律 Tab；Edit 工具做空白转换时逐字符核对

---

### Task 1: Web 模块常量收敛

**Files:**
- Create: `Unity/Assets/Scripts/Hotfix/Framework/Web/WebModule.Const.cs`（+ .meta 由 Unity 生成）
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Web/WebModule.cs`（删除 PB_CONTENT_TYPE 声明及前置注释块）

**Interfaces:**
- Consumes: 无（首个任务）
- Produces: `WebModule.PB_CONTENT_TYPE`（private const string，partial 分部成员，WebModule.Pb.cs:82 原引用点自动生效）

盘点结论（已侦察）：Web 全模块仅 1 个常量符合收敛标准——`PB_CONTENT_TYPE`（WebModule.cs:55，引用点 WebModule.Pb.cs:82）。`DEBUG_LOG_CAPACITY`/`DEBUG_LOG_BODY_MAX_LENGTH` 为 Editor-only 调试自包含常量，豁免保留。魔法数字扫描零命中。WebModule.cs 已是 partial。

- [ ] **Step 1: 创建 WebModule.Const.cs**

仿照 `Network/NetworkModule.Const.cs` 结构：文件头注释声明「Web 管理模块——常量分部」、`public sealed partial class WebModule`、`#region 请求` 分区，常量 `PB_CONTENT_TYPE`（private const string，值 `"application/x-protobuf"`），`/// <summary>` 写明用途（Pb 请求体 MIME 类型）。命名空间 `Hotfix.Framework.Web`，Tab 缩进。

- [ ] **Step 2: 删除 WebModule.cs 原声明**

删除 WebModule.cs:53-56 的注释块+常量行（含后置空行）。引用点 WebModule.Pb.cs:82 不需改动（partial 同类互见）。

- [ ] **Step 3: A4 参数链检查**

grep Web 模块 API 的默认值参数（`userData = null` 等），逐个全项目搜定制调用方；无定制调用方则删参内部直取，有则保留。结果记入汇报。

- [ ] **Step 4: 编译验证**

Run: `unity-cli tool call refresh_assets` 后轮询 `get_compilation_state`
Expected: `isCompiling: false, errorCount: 0`

- [ ] **Step 5: 收尾检查**

grep 全项目 `PB_CONTENT_TYPE` 确认仅 Const.cs 声明一处；读 `Web/README.md` 检查是否提及常量散布/参数位置，需更新则同步。

- [ ] **Step 6: 汇报并等待用户确认**

汇报修正清单（迁移项/豁免项/A4 结论/README 变更），等待用户确认。

- [ ] **Step 7: 提交**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/Web/
git commit -m "[AI]refactor: Web模块常量收敛至 Const 分部"
```

---

### Task 2: Sound 模块常量收敛

**Files:**
- Create: `Unity/Assets/Scripts/Hotfix/Framework/Sound/SoundModule.Const.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Sound/SoundModule.cs:129-132`（删 AUDIO_MIXER_ASSET_PATH 声明）
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Sound/SoundModule.SoundAgent.cs:442`（MaxDistance 魔法数字→常量引用，需先 Read 确认上下文）
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Sound/Misc/SoundParams.cs:83,107`（MaxDistance 重复默认值→常量引用）

**Interfaces:**
- Produces: `SoundModule.AUDIO_MIXER_ASSET_PATH`（private const string）、`SoundModule.SOUND_MAX_DISTANCE`（public/internal const float = 100f，供 SoundParams/SoundAgent 引用；可见性按引用方跨类需求定，SoundParams 与 SoundAgent 非嵌套类时需 internal/public）

盘点结论（已侦察）：`AUDIO_MIXER_ASSET_PATH`（SoundModule.cs:131）收敛；`STORAGE_KEY_PREFIX`（SoundModule.SoundGroup.cs:22）为嵌套类自包含存储键前缀，豁免保留；`MaxDistance = 100` 三处重复定义（SoundParams.cs:83,107 + SoundAgent.cs:442）为「重复定义的同一常量」重点对象，收敛为 `SOUND_MAX_DISTANCE = 100f` 后三处引用切换。

- [ ] **Step 1: Read SoundParams.cs 与 SoundAgent.cs 相关段确认 MaxDistance 上下文**

确认三处是否同一语义（音源最大可听距离默认值）；若语义不同则不合并，仅收敛相同语义处。

- [ ] **Step 2: 创建 SoundModule.Const.cs**

两个 `#region`：`资源路径`（AUDIO_MIXER_ASSET_PATH，summary 写明 Bundle 内 AudioMixer 资源路径）、`声音参数`（SOUND_MAX_DISTANCE = 100f，summary 写单位为米/Unity 世界单位、取值依据）。类声明 `public sealed partial class SoundModule`。

- [ ] **Step 3: 迁移与引用切换**

删 SoundModule.cs:129-132 声明；SoundParams.cs/SoundAgent.cs 三处 `MaxDistance = 100` 改 `= SoundModule.SOUND_MAX_DISTANCE`（确认 SoundParams 命名空间可直引或加 using）。STORAGE_KEY_PREFIX 不动。

- [ ] **Step 4: A4 参数链检查**

grep Sound 模块 API 默认值参数与定制调用方，结论记入汇报。

- [ ] **Step 5: 编译验证**

同 Global Constraints 编译命令。Expected: errorCount: 0。

- [ ] **Step 6: 收尾检查**

grep 全项目 `AUDIO_MIXER_ASSET_PATH`、`MaxDistance   = 100` 零残留（Const.cs 除外）；检查 `Sound/README.md`。

- [ ] **Step 7: 汇报并等待用户确认**

- [ ] **Step 8: 提交**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/Sound/
git commit -m "[AI]refactor: Sound模块常量收敛至 Const 分部"
```

---

### Task 3: ObjectPool 模块常量收敛

**Files:**
- Create: `Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/ObjectPoolModule.Const.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/ObjectPoolModule.cs:19-36`（删 4 个 DEFAULT_* 声明）

**Interfaces:**
- Produces: `ObjectPoolModule.DEFAULT_CAPACITY`（int.MaxValue）、`DEFAULT_AUTO_DISPOSE_CHECK_INTERVAL`（float.MaxValue）、`DEFAULT_EXPIRE_TIME`（float.MaxValue）、`DEFAULT_PRIORITY`（0）——全部 private const，引用点 ObjectPoolModule.API.cs:46 同 partial 互见自动生效

盘点结论（已侦察）：4 个 DEFAULT_* 全收敛。外部调用方 EntityGroup.cs:101（传自定义值）与 UIModule.cs:128（走无参重载）不受影响。

- [ ] **Step 1: 创建 ObjectPoolModule.Const.cs**

`#region 默认参数` 分区 4 常量，summary 保留原注释语义（默认不限制容量/默认不检查自动销毁/默认不过期/默认优先级 0）。

- [ ] **Step 2: 删除 ObjectPoolModule.cs:19-36 原声明**（4 组注释块+常量行）

- [ ] **Step 3: A4 参数链检查**

重点：`CreateObjectPoolInternal` 若有默认值参数（如 capacity/expireTime 带 `= DEFAULT_XXX`），grep 全项目调用方——仅 ObjectPoolModule.API.cs:46 一处直传则可删参；`CreateObjectPool<T>(poolName, capacity, expireTimeAfterIdle, priority)` 显式参数重载有 EntityGroup 定制调用方，保留。结论记入汇报。

- [ ] **Step 4: 编译验证**（同前）

- [ ] **Step 5: 收尾检查**

grep `DEFAULT_CAPACITY` 等旧声明零残留；检查 `ObjectPool/README.md`。

- [ ] **Step 6: 汇报并等待用户确认**

- [ ] **Step 7: 提交**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/
git commit -m "[AI]refactor: ObjectPool模块常量收敛至 Const 分部"
```

---

### Task 4: Download 模块常量收敛

**Files:**
- Create: `Unity/Assets/Scripts/Hotfix/Framework/Download/DownloadModule.Const.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Download/DownloadModule.cs:26-38,128-132`（删 3 声明 + Timeout 魔法数字）
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Download/Helper/UnityWebRequestDownloadAgentHelper.cs:33,41`（2 const 迁移，注意该类非 DownloadModule 嵌套类，跨类引用需 internal 可见性）

**Interfaces:**
- Produces: `DownloadModule.DEFAULT_PRIORITY`（0）、`ONE_MEGA_BYTES`（1024*1024）、`DOWNLOAD_AGENT_HELPER_COUNT`（3）、`TIMEOUT_SECONDS`（30f）、`RANGE_NOT_SATISFIABLE_ERROR_CODE`（416）、`CACHED_BYTES_LENGTH`（0x1000=4096）——前三个保持原可见性（internal/private），后三个涉及跨类/新语义需升 internal 并按 partial 互见规则整理

盘点结论（已侦察）：5 个 const + 1 个魔法数字 `Timeout = 30f`（DownloadModule.cs:130，任务池超时参数）。`RANGE_NOT_SATISFIABLE_ERROR_CODE`/`CACHED_BYTES_LENGTH` 属 UnityWebRequestDownloadAgentHelper 类——该类非嵌套类，private 常量搬入 DownloadModule.Const.cs 后跨类不可见，须升 internal。Download 全项目零外部调用方（仅 HotfixLauncher 注册）。

- [ ] **Step 1: 创建 DownloadModule.Const.cs**

`#region 任务参数`（DEFAULT_PRIORITY/DOWNLOAD_AGENT_HELPER_COUNT/TIMEOUT_SECONDS）、`#region 数据块`（ONE_MEGA_BYTES/CACHED_BYTES_LENGTH）、`#region HTTP 状态`（RANGE_NOT_SATISFIABLE_ERROR_CODE）。CACHED_BYTES_LENGTH 值写成 `1024 * 4` 并在 summary 写明 4 KiB 收发块。RANGE_NOT_SATISFIABLE_ERROR_CODE 升 internal（summary 注明 HTTP 416 语义）。

- [ ] **Step 2: 迁移**

删 DownloadModule.cs 三组声明；DownloadModule.cs:130 `Timeout = 30f` 改 `= TIMEOUT_SECONDS`；UnityWebRequestDownloadAgentHelper.cs:33,41 两处声明删除、引用点改 `DownloadModule.XXX`（Helper 命名空间若不同则加 using）。

- [ ] **Step 3: A4/重载裁剪评估（不直接执行）**

Download 的 priority 通过 8 个显式重载传递而非默认值参数，且全项目零调用方——**不在 A4 范围内直接删**，作为建议项记入汇报（是否删重载由用户裁决）。

- [ ] **Step 4: 编译验证**（同前）

- [ ] **Step 5: 收尾检查**

grep `ONE_MEGA_BYTES|RANGE_NOT_SATISFIABLE|CACHED_BYTES_LENGTH|DOWNLOAD_AGENT_HELPER_COUNT` 零残留；检查 `Download/README.md`。

- [ ] **Step 6: 汇报并等待用户确认**（汇报中含重载裁剪建议）

- [ ] **Step 7: 提交**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/Download/
git commit -m "[AI]refactor: Download模块常量收敛至 Const 分部"
```

---

### Task 5: Storage 模块常量收敛

**Files:**
- Create: `Unity/Assets/Scripts/Hotfix/Framework/Storage/StorageModule.Const.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Storage/StorageModule.cs:17`（加 partial）、`StorageModule.cs:26-33`（删 2 声明）

**Interfaces:**
- Produces: `StorageModule.DIR_ROOT`（public const string "GameData"，保持原 public——有外部引用可能）、`StorageModule.DEFAULT_FILE_NAME`（private const string）

盘点结论（已侦察）：`DIR_ROOT`/`DEFAULT_FILE_NAME`（StorageModule.cs:27,32）收敛；`StorageModule` 主类非 partial（唯一需要补 partial 的模块）。`FLOAT/DOUBLE_COMPARE_EPSILON`（StorageHelper.cs:26,31）为辅助类内部算法容差，豁免保留；`DataSerializer.sr_header` 为 static readonly 协议头数据，豁免。先 grep `DIR_ROOT` 全项目引用确认 public 保持。

- [ ] **Step 1: grep DIR_ROOT 全项目引用点**，确认可见性需求后创建 Const 文件（`#region 存储` 分区）。

- [ ] **Step 2: StorageModule.cs 类声明加 partial**，删除两组原声明。

- [ ] **Step 3: A4 参数链检查**（StorageHelper/StorageModule API 默认值参数），结论记入汇报。

- [ ] **Step 4: 编译验证**（同前）

- [ ] **Step 5: 收尾检查**：grep `DEFAULT_FILE_NAME` 零残留；检查 `Storage/README.md`。

- [ ] **Step 6: 汇报并等待用户确认**

- [ ] **Step 7: 提交**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/Storage/
git commit -m "[AI]refactor: Storage模块常量收敛至 Const 分部"
```

---

### Task 6: RedDot 模块常量收敛

**Files:**
- Create: `Unity/Assets/Scripts/Hotfix/Framework/RedDot/RedDotModule.Const.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/RedDot/RedDotModule.cs:84-92`（删 2 声明）

**Interfaces:**
- Produces: `RedDotModule.READ_STORAGE_KEY`（"ReadSet"）、`READ_STORAGE_FILE`（"RedDotData"）——private const，partial 互见

盘点结论（已侦察）：`READ_STORAGE_KEY`/`READ_STORAGE_FILE`（RedDotModule.cs:86,91）收敛；`INVALID_ENUM_VALUE`（RedDotKey.cs:17，-1 哨兵）为 RedDotKey 类实现细节豁免；`sr_nodeDict`/`sr_Empty` 为 static readonly 数据豁免。

- [ ] **Step 1: 创建 RedDotModule.Const.cs**（`#region 持久化` 分区，summary 写明 StorageModule 存储键/文件名）

- [ ] **Step 2: 删除 RedDotModule.cs 原声明**，引用点（模块内 GetSave/Load 逻辑）partial 互见自动生效，若引用点在 RedDotState 等跨类文件则切 `RedDotModule.XXX`

- [ ] **Step 3: A4 参数链检查**，结论记入汇报。

- [ ] **Step 4: 编译验证**（同前）

- [ ] **Step 5: 收尾检查**：grep `READ_STORAGE_KEY|READ_STORAGE_FILE` 零残留；检查 `RedDot/README.md`。

- [ ] **Step 6: 汇报并等待用户确认**

- [ ] **Step 7: 提交**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/RedDot/
git commit -m "[AI]refactor: RedDot模块常量收敛至 Const 分部"
```

---

### Task 7: UI 模块常量收敛

**Files:**
- Create: `Unity/Assets/Scripts/Hotfix/Framework/UI/UIModule.Const.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/UI/UIModule.cs:74-87`（删 3 声明）
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/UI/UIModule.Blur.cs:40-53`（删 3 声明）

**Interfaces:**
- Produces: `UIModule.DEFAULT_AUTO_DISPOSE_CHECK_INTERVAL`（60f）、`DEFAULT_POOL_CAPACITY`（16）、`DEFAULT_POOL_EXPIRE_TIME_AFTER_IDLE`（60f）、`BLUR_SHADER_NAME`（"UIBlurBackground"）、`BLUR_SIZE`（1.6f）、`MASK_POWER`（0.35f）——全部 private const，引用点 UIModule.cs:135-137 与 UIModule.Blur.cs 内部 partial 互见自动生效

盘点结论（已侦察）：6 个 const 全收敛（UIModule.cs 3 个窗口池默认参数 + UIModule.Blur.cs 3 个背景模糊参数）。`Shader.PropertyToID` 的 sr_* 缓存字段、CustomLoader 的 static readonly 字段豁免。

- [ ] **Step 1: 创建 UIModule.Const.cs**

三个 `#region`：`窗口对象池默认参数`（3 个）、`背景模糊`（3 个）。BLUR_SIZE/MASK_POWER 的 summary 写明是 Shader 材质参数值（对应 _BlurSize/_MaskPower 属性）。

- [ ] **Step 2: 删除两组原声明**，引用点 partial 互见自动生效。

- [ ] **Step 3: A4 参数链检查**：UI 模块 API 默认值参数全项目调用方甄别（注意 Editor 调试面板），结论记入汇报。

- [ ] **Step 4: 编译验证**（同前）

- [ ] **Step 5: 收尾检查**：grep `BLUR_SIZE|MASK_POWER|DEFAULT_POOL_CAPACITY` 零残留；检查 `UI/README.md`（注意 README 较长，只同步常量/参数相关段）。

- [ ] **Step 6: 汇报并等待用户确认**

- [ ] **Step 7: 提交**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/UI/
git commit -m "[AI]refactor: UI模块常量收敛至 Const 分部"
```

---

### Task 8: 零常量模块确认与收尾

**Files:**
- Modify: 无（确认类任务；若某模块确认中翻出漏网常量，按该模块形态补 Const 分部后并入本任务提交）

**Interfaces:**
- Consumes: Task 1-7 的 Const 文件成果
- Produces: 收尾报告（12 个零常量模块逐一确认记录 + 全 Framework 终态 grep 证据）

12 个零 const 模块：Asset/Config/Entity/Event/FSM/Guide/Localization/Model/Mono/Procedure/Scene/Timer。本章不做猜测性整理——逐模块跑一次盘点 grep（const 声明 + 魔法数字扫描），零命中即记录跳过；有漏网则按先例收敛（该模块若翻出常量，则建对应 Const 文件、走完整流程，与 Task 1-7 同规格，编译验证后汇报）。

- [ ] **Step 1: 逐模块盘点 grep**

对 12 个模块各跑：`grep -rn "const \|static readonly" <模块> --include="*.cs"` + 魔法数字扫描（同设计文档 A1 模式），零命中记录「跳过」，有命中进 Step 2。

- [ ] **Step 2:（仅当有漏网）按模块形态收敛**

建 `<模块>Module.Const.cs` → 迁移 → 编译验证 → README 检查。多个模块有漏网时逐模块汇报确认、各自提交。

- [ ] **Step 3: 全 Framework 终态 grep**

Run: `grep -rn "const " Unity/Assets/Scripts/Hotfix/Framework --include="*.cs" | grep -v "Const.cs" | grep -v errorMessage`
Expected: 仅剩豁免清单内常量（NetworkDebug、StorageHelper 容差、RedDotKey 哨兵、嵌套类自包含项、Core 算法常量）。

- [ ] **Step 4: 收尾报告**

汇总 8 篇章终态：每模块 Const 文件清单 + 常量数 + 豁免记录。更新任务列表为全部 completed。

- [ ] **Step 5: 最终提交（仅当 Step 2 产生了改动）**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/
git commit -m "[AI]refactor: 零常量模块漏网收敛收尾"
```
