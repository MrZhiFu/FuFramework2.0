# 消息 ID 显式锚定与语义常量化 — 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 消息 opcode 在 proto 源显式锚定（缺失报错强制），导出器生成 `MessageIds` 语义常量，消息特性/注册表/日志全部换语义引用，现有 ID 数值零变化。

**Architecture:** 改动集中在 `Tools/ProtoExport`（解析→校验→常量生成）与 `gen-proto-registry.py`（标识符解析+查表），proto 源一次性回填现值，生成物与日志随之更新。协议格式（14 字节信封、单 int ID）不动。

**Tech Stack:** C# (.NET 8 控制台工具 + Unity Hotfix) / Python 3（注册表生成器）/ proto3 文本

**Spec:** `Docs/superpowers/specs/2026-10-25-message-id-explicit-anchoring-design.md`

## Global Constraints

- 现有 ID 数值一个都不能变（`消息ID = (module << 16) + opcode`）；迁移后双端生成物 `[MessageId(...)]` 数值 diff 必须为空
- 生成物（`*.g.cs`、`MessageIds.md`）勿手改；所有行为改动落在 `Tools/ProtoExport/*.cs` 与 `Protobuf/gen-proto-registry.py`
- 铁律 4：运行时无反射（常量是编译期 const，天然满足）；杜绝 LINQ（工具代码用手写循环）
- 重跑导出必须 `Proto2CsExport-All.bat ci`（**无 `ci` 参数会在结尾 pause 假死**）
- Windows 环境：批量文本改动用 perl/-creplace，勿用大小写不敏感替换
- 每任务结束编译验证：服务器 `dotnet build Server/Server.sln`；Unity 侧 `unity-cli tool call refresh_assets` + `get_compilation_state`（errorCount 须为 0）
- opcode 取值范围 [10, 65535]；Req/Resp/Notify 各自独立占号；DTO 禁止标注

## Review Focus

1. **DTO 误标 `option opcode`**（如给 `PlayerInfo` 加标注）→ 导出器必须报错而非当作字段/静默忽略 → Task 3 负例 fixture `dto_annotated.proto`
2. **同模块将来拆多文件撞 `(module, opcode)`** → `Post()` 阶段跨文件全局唯一校验报错 → Task 6 负例 fixture
3. **`MessageIds.g.cs` 与消息类失配**（删消息后忘重跑 registry）→ `gen-proto-registry.py` 查表失败必须报错中止，不允许静默产出空注册 → Task 5 负例验证
4. **模块枚举成员名撞名/非法**（`Inner_Basic_2`→`Inner_Basic` 与 `Basic` 是不同成员，但若两个文件去号后同名）→ `Post()` 报错 → Task 6 校验点 2
5. **迁移回填手滑改错数值** → Task 2 的双端 `[MessageId]` 数值 diff 为空是硬闸门，diff 非空即回滚重填

---

### Task 1: 导出器解析 `option opcode`（兼容模式）

**Files:**
- Modify: `Tools/ProtoExport/MessageHelper.cs:13-15`（Pattern 区）、`:210-306`（ParseMessage）
- Create: `Tools/ProtoExport/TestFixtures/valid_annotated/valid_annotated.proto`（**每场景独立子目录**——工具按目录扫描，负例混入会污染正例验证）

**Interfaces:**
- Consumes: 现有 `MessageInfo.Opcode` 字段（`int`，默认 0）、`ParseMessage` 的逐行解析循环
- Produces: `ParseMessage` 能从 message 块提取 `option opcode = N;` 写入 `MessageInfo.Opcode`；option 行不再产生幽灵字段。`MessageIdHandler` 行为暂不变（`Opcode > 0` 跳过隐式分配的现状逻辑保留）

- [ ] **Step 1: 创建正例 fixture**

`Tools/ProtoExport/TestFixtures/valid_annotated/valid_annotated.proto`：

```proto
package TestAnno;
option module = 700;

message ReqPing
{
  option opcode = 10;
  int32 Id = 1; // 标识
}

message RespPing
{
  option opcode = 11;
  int32 ErrorCode = 1; // 错误码
}

message PlayerInfo
{
  int64 Id = 1; // 玩家ID
}
```

- [ ] **Step 2: 跑工具确认现状失败**

```bash
cd Tools/ProtoExport && dotnet build ProtoExport.csproj -v q
cd bin/Debug/net8.0 && dotnet ProtoExport.dll --mode unity --inputPath ../../../../TestFixtures/valid_annotated --outputPath /tmp/pe-fixture-out --namespaceName Test.Proto --isGenerateErrorCode false
```

Expected: 当前 `option opcode = 10;` 行被当字段解析（`option` 被拆为字段名），生成物出现畸形字段或工具报错——记录现状输出作为对照。

- [ ] **Step 3: 实现 option 行拦截**

`MessageHelper.cs`：Pattern 区新增 `private const string OpcodePattern = @"option opcode = (\d+);";`。
`ParseMessage` 的 `foreach (var line in blockContent.Split(...))` 循环内、`MessageMember field = new MessageMember();` **之前**（顺序关键——现有代码先 `Add` 后解析，拦截必须在 Add 前）插入：Trim 后以 `option` 开头的行 → `Regex.Match(line, OpcodePattern)` 提取 → `info.Opcode = int.Parse(...)` 后 `continue`（不 Add 字段）；`option` 开头但不含 opcode 的行（防御性）→ 直接 `continue`。

- [ ] **Step 4: 跑工具验证正例通过**

同 Step 2 命令。
Expected: 生成 `/tmp/pe-fixture-out/TestAnno.cs`，ReqPing 特性值为 `(700 << 16) + 10 = 45875210`、RespPing 为 `45875211`；`PlayerInfo` 无 MessageId 特性；无幽灵字段。

- [ ] **Step 5: 服务器模式同验**

同命令换 `--mode server --namespaceName TestServer.Proto`。
Expected: 同样数值出现在服务器生成物。

- [ ] **Step 6: Commit**

```bash
git add Tools/ProtoExport/MessageHelper.cs Tools/ProtoExport/TestFixtures/valid_annotated.proto
git commit -m "[AI]feat: 导出器解析 message 级 option opcode（兼容模式，隐式分配保留）"
```

---

### Task 2: proto 源迁移回填 + ID 零变化校验

**Files:**
- Modify: `Protobuf/Proto/Basic_10.proto`、`Protobuf/Proto/User_300.proto`、`Protobuf/Proto/Bag_100.proto`
- （`Common_20.proto`、`Inner_Basic_2.proto` 无消息，不动）

**Interfaces:**
- Consumes: Task 1 的解析能力
- Produces: 5 个 proto 全部消息显式锚定；`MessageInfoList` 全模块 opcode 就位，供 Task 3 校验与 Task 4 常量生成

- [ ] **Step 1: 备份现值（校验基准）**

```bash
grep -n "MessageId(" Unity/Assets/Scripts/Hotfix/Game/AutoGen/Proto/*.cs > /tmp/msgid-before-client.txt
grep -n "MessageId(" Server/FuFramework.Proto/Proto/*.cs > /tmp/msgid-before-server.txt
```

- [ ] **Step 2: 按现值回填 option opcode**

每条 IsMessage 消息体首行插入 `option opcode = N;`（对照表——数值=当前隐式分配值，**逐字照抄**）：

- `Basic_10.proto`（module=10）：`ReqHeartBeat`=10、`NotifyHeartBeat`=11、`NotifyServerFullyLoaded`=12
- `User_300.proto`（module=300）：`ReqUserLogin`=10、`RespUserLogin`=11、`ReqPlayerCreate`=12、`RespPlayerCreate`=13、`ReqPlayerList`=14、`RespPlayerList`=15、`ReqPlayerLogin`=16、`RespPlayerLogin`=17、`NotifyErrorCode`=18、`NotifyPrompt`=19（`PlayerInfo` 为 DTO **不标注**）
- `Bag_100.proto`（module=100）：`ReqBagInfo`=10、`RespBagInfo`=11、`ReqUseItem`=12、`RespUseItem`=13、`ReqAddItem`=14、`RespAddItem`=15、`ReqRemoveItem`=16、`RespRemoveItem`=17、`NotifyBagItem`=18、`NotifyBagInfoChanged`=19（`BagItem` 为 DTO **不标注**）

- [ ] **Step 3: 重跑导出 + 双端数值 diff**

```bash
cd Protobuf && cmd /c ".\Proto2CsExport-All.bat ci"
grep -n "MessageId(" ../Unity/Assets/Scripts/Hotfix/Game/AutoGen/Proto/*.cs > /tmp/msgid-after-client.txt
grep -n "MessageId(" ../Server/FuFramework.Proto/Proto/*.cs > /tmp/msgid-after-server.txt
diff /tmp/msgid-before-client.txt /tmp/msgid-after-client.txt && diff /tmp/msgid-before-server.txt /tmp/msgid-after-server.txt
```

Expected: 两个 diff 均无输出（数值零变化；行号可能漂移属正常时改用 `grep -o "MessageId([0-9]*)"` 提数值比较）。

- [ ] **Step 4: 双端编译验证**

`unity-cli tool call refresh_assets` → `get_compilation_state` errorCount=0；`dotnet build Server/Server.sln` 0 错误。

- [ ] **Step 5: Commit**

```bash
git add Protobuf/Proto/
git commit -m "[AI]feat: proto 消息显式锚定 option opcode（现值回填，ID 零变化）"
```

---

### Task 3: `MessageIdHandler` 严格化（校验器）

**Files:**
- Modify: `Tools/ProtoExport/MessageHelper.cs:66`（调用点）、`:102-126`（MessageIdHandler）
- Create: `Tools/ProtoExport/TestFixtures/missing_opcode/missing_opcode.proto`、`TestFixtures/duplicate_opcode/duplicate_opcode.proto`、`TestFixtures/dto_annotated/dto_annotated.proto`（各独立子目录）

**Interfaces:**
- Consumes: Task 1 拦截逻辑（`info.Opcode` 已就位）
- Produces: 校验器行为契约——**缺失**（IsMessage 且 Opcode<10 或 >65535）→ throw；**同文件重复** → throw；**DTO 带标注** → throw（在 ParseMessage 拦截处判定）。隐式分配逻辑删除

- [ ] **Step 1: 创建三个负例 fixture**

- `missing_opcode/missing_opcode.proto`：module=888，一条 `message ReqA { int32 Id = 1; }` **无** option opcode
- `duplicate_opcode/duplicate_opcode.proto`：module=889，两条消息都 `option opcode = 10;`
- `dto_annotated/dto_annotated.proto`：module=890，`message SomeDto { option opcode = 10; int64 Id = 1; }`（非 Req/Resp/Notify 前缀）

- [ ] **Step 2: 跑工具确认负例现状（缺失仍被隐式分配）**

跑 Task 1 Step 2 同款命令（换 inputPath 逐个 fixture）。
Expected: missing 当前被隐式分配为 10（不报错）——这是要消灭的行为；duplicate/dto_annotated 记录现状。

- [ ] **Step 3: 实现校验器**

`ParseMessage` 拦截处追加 DTO 判定：提取 opcode 后若 `!info.IsMessage` → `throw new Exception($"[{packageName}] 非消息类型 [{info.Name}] 禁止标注 option opcode（DTO 不占消息号）")`。
`MessageIdHandler` 重写为校验器，签名改为 `MessageIdHandler(List<MessageInfo> infos, string packageName, string fileName)`（调用点 `:66` 同步传参；删除 `int start` 与 `start++`）：

- 遍历 `IsMessage` 项：`Opcode < 10 || Opcode > 65535` → `throw new Exception($"[{packageName}/{fileName}] 消息 [{name}] 缺少 option opcode 或超出范围 [10,65535]")`
- `HashSet<int>` 收集，重复 → `throw new Exception($"[{packageName}/{fileName}] 消息 opcode [{op}] 重复（{name1} 与 {name2}）")`

- [ ] **Step 4: 三负例各自报错验证**

跑 Step 2 命令逐个 fixture。
Expected: 三者均异常退出（退出码非 0），错误消息含文件名与消息名；`valid_annotated.proto` 仍通过。

- [ ] **Step 5: 全量导出回归 + 编译 + Commit**

`Proto2CsExport-All.bat ci`（真实 proto 已全部锚定，应通过且 diff 仍为空）→ 双端编译 → `git commit -m "[AI]feat: opcode 校验器严格化（缺失/重复/DTO 标注报错强制）"`

---

### Task 4: `MessageIds.g.cs` 生成 + 特性常量引用

**Files:**
- Modify: `Tools/ProtoExport/IProtoGenerateHelper.cs`（Post 签名）、`ProtoBuffUnityHelper.cs:106-108`、`ProtoBuffServerHelper.cs:117-119`、`ProtoBuffMessageHandler.cs`（Post 调用点，传 namespaceName 与 inputPath）、`ProtoBuffUnityHelper.cs:60`、`ProtoBuffServerHelper.cs:66`（特性行）
- Create: 生成物 `MessageIds.g.cs`（两端各一，由工具产出勿手写）

**Interfaces:**
- Consumes: Task 3 后的 `MessageInfoList`（Module/FileName/Infos 全就位）
- Produces: **`MessageIds.g.cs` 格式契约**（Task 5 依赖）——`public enum MessageModule { <模块名> = <module>, ... }` + `public static class MessageIds { public static class <模块名> { public const int <消息名> = ((int)MessageModule.<模块名> << 16) + <opcode>; // <数值> ... } }`；特性行 `[MessageId(MessageIds.<模块名>.<消息名>)]`。**模块名规则**：`FileName` 去末尾 `_<数字>`（`User_300`→`User`、`Inner_Basic_2`→`Inner_Basic`）；**常量行必须带行尾数值注释**。**Post 新签名（定死，Task 6/7 在此扩展）**：`Post(List<MessageInfoList> infos, string launcherOptionsOutputPath, string protoInputPath, string outputPath, string namespaceName)`——保留原参，追加 protoInputPath（Task 7 速查表定位）与 outputPath（MessageIds.g.cs 落盘）与 namespaceName（常量类命名空间）；`ProtoBuffMessageHandler` 调用点透传

- [ ] **Step 1: 扩展 Post 签名并透传参数**

`IProtoGenerateHelper.Post` 与两个实现、`ProtoBuffMessageHandler` 调用点同步改为五参数（namespaceName/protoInputPath/outputPath 从 Run 现有入参透传）。

- [ ] **Step 2: 实现两端 Post 生成 MessageIds.g.cs**

手写循环（无 LINQ）：遍历 infos → 模块成员名（FileName 去 `_\d+$`）→ enum 体；每个含 IsMessage 的模块生成嵌套类，每消息一行常量 `public const int ReqPing = ((int)MessageModule.TestAnno << 16) + 10; // 45875210`。写入 `Path.Combine(outputPath, "MessageIds.g.cs")`。文件头 `<auto-generated/> 由 ProtoExport 生成，勿手改`。命名空间与该端消息类一致（客户端 namespaceName 参数值，服务器同理）。

- [ ] **Step 3: 特性行换常量引用**

`ProtoBuffUnityHelper.cs:60` 与 `ProtoBuffServerHelper.cs:66` 的 `$"[MessageId({(messageInfoList.Module << 16) + operationCodeInfo.Opcode})]"` 改为 `$"[MessageId(MessageIds.{模块名}.{operationCodeInfo.Name})]"`（模块名按 Task 4 规则从 `messageInfoList.FileName` 算）。

- [ ] **Step 4: 跑全量导出验证**

`Proto2CsExport-All.bat ci`。
Expected: 两端各出现 `MessageIds.g.cs`（客户端含 `Basic = 10`、`User = 300` 等枚举与 `MessageIds.User.ReqUserLogin` 等常量，行尾注释数值与 Task 2 备份一致）；消息类特性为 `[MessageId(MessageIds.X.Y)]` 形式；`ProtoBufMessageHandler.Start()` 整目录重建包含新文件。

- [ ] **Step 5: 双端编译验证 + Commit**

Unity errorCount=0、`dotnet build Server/Server.sln` 0 错误（常量引用与 enum 均编译期解析）。
`git commit -m "[AI]feat: 生成 MessageIds 语义常量，消息特性换常量引用"`

---

### Task 5: gen-proto-registry.py 标识符化 + 查表

**Files:**
- Modify: `Protobuf/gen-proto-registry.py:82`（RE_MSG_TYPE_ATTR）、消息 ID 收集逻辑（约 `:319` 附近使用处）

**Interfaces:**
- Consumes: Task 4 的 `MessageIds.g.cs` 格式契约（注释数值 + `MessageIds.<模块>.<常量>` 嵌套路径）
- Produces: `MessageRegistry.g.cs` 注册行格式 `MessageIdRegistry.Register<ReqUserLogin>(MessageIds.User.ReqUserLogin);`——ID 数值与 Task 2 备份逐条一致

- [ ] **Step 1: 改正则与建表逻辑**

`RE_MSG_TYPE_ATTR = re.compile(r"\[\s*MessageId\s*\(\s*([\w.]+)\s*\)\s*\]")`。
新增函数 `load_message_ids(client_autogen_dir) -> dict`：读 `MessageIds.g.cs`，跟踪 `public static class (\w+)` 上下文（`MessageIds` 下的一级类 = 模块名），每行 `public\s+const\s+int\s+(\w+)\s*=.*?//\s*(\d+)` 提取，产出 `{"MessageIds.User.ReqUserLogin": 19660810, ...}`。
扫描消息类时查表：命中 → 数值入内部表（供既有 validate/输出用）；未命中 → `raise SystemExit(f"{rel}: [MessageId({expr})] 在 MessageIds.g.cs 中无对应常量——请重跑 Proto2CsExport-All.bat ci")`。
输出注册行模板改为 `MessageIdRegistry.Register<{name}>({expr});`（expr 用扫描到的原文标识符）。

- [ ] **Step 2: 正例验证**

`cd Protobuf && python gen-proto-registry.py`。
Expected: `MessageRegistry.g.cs` 重新生成；`grep -o "Register<[A-Za-z]*>([^)]*)" ...` 显示常量引用形式；**数值校验**——用 Task 2 备份的数值表逐条比对（Python 内联脚本或临时 grep+sort+diff），零差异。

- [ ] **Step 3: 负例验证（查表失败报错）**

临时把某个消息的 `option opcode` 从 proto 删掉跑全链（或临时改名 MessageIds 常量）。
Expected: 脚本报错退出、指出文件与常量名，**不产出**残缺 `MessageRegistry.g.cs`。验证后还原现场重跑恢复。

- [ ] **Step 4: Commit**

`git commit -m "[AI]feat: 注册表生成器改用语义常量标识符并查表校验"`

---

### Task 6: `Post()` 全局唯一性校验

**Files:**
- Modify: `Tools/ProtoExport/ProtoBuffUnityHelper.cs`（Post 内，Task 4 已实现的骨架）
- Create: `Tools/ProtoExport/TestFixtures/cross_mod_conflict/cross_mod_conflict_a.proto`、`cross_mod_conflict_b.proto`、`TestFixtures/dup_module/dup_module_a.proto`、`dup_module_b.proto`（同目录成对——负例场景本身就需要多文件）

**Interfaces:**
- Consumes: Task 4 的 Post（持有全部 `MessageInfoList`）
- Produces: 两道启动期闸门——① `(module, opcode)` 跨文件全局唯一；② module 号与"去号模块名"每文件唯一。违反即 throw，错误含冲突双方文件/消息名

- [ ] **Step 1: 创建负例 fixtures**

- `cross_mod_conflict/`：`a.proto`（module=900，`ReqX` opcode=10）+ `b.proto`（module=900，`ReqY` opcode=10）——跨文件同 module 同 opcode
- `dup_module/`：`a.proto` + `b.proto` 同 `option module = 901`（各自消息不撞号）——模块号不唯一

- [ ] **Step 2: 实现校验**

Unity Helper 的 Post 末尾（生成 MessageIds.g.cs 之后）加：`Dictionary<(int,int),(string,string)>` 收集 `(module, opcode) → (fileName, msgName)`，冲突 throw；`HashSet<int>` 校验 module 唯一、`HashSet<string>` 校验去号模块名唯一。Server Helper 的 Post 同加（两端独立闸门，逻辑同源可复制）。

- [ ] **Step 3: 负例报错验证**

跑 fixture 目录（mode unity）。
Expected: 两组负例均异常退出、消息指明冲突双方；`valid_annotated.proto` 与真实 proto 全量导出不误伤。

- [ ] **Step 4: Commit**

`git commit -m "[AI]feat: 导出器 Post 阶段跨文件 (module,opcode) 全局唯一校验"`

---

### Task 7: `MessageIds.md` 速查表生成

**Files:**
- Modify: `Tools/ProtoExport/ProtoBuffUnityHelper.cs`（Post 追加速查表生成——**仅客户端**，服务器 Post 不生成）

**Interfaces:**
- Consumes: Task 4 的 Post 数据（全部模块与消息映射）
- Produces: `Protobuf/MessageIds.md`——按模块分组的 ID↔语义名对照表，尾部含 32766 内置协议盲区注记

- [ ] **Step 1: 实现速查表生成**

Post 内追加：markdown 输出——头部说明公式 `(module << 16) + opcode`；每模块一节 `## User（module 300）`，表格三列 `消息名 | opcode | 完整ID`；尾部固定段：

```markdown
> 盲区：module 32766（short.MaxValue-1）段为服务器内置协议 DiscoveryCenter，手写维护，不在本表生成范围。
```

写入路径：`Path.Combine(protoInputPath 的父目录, "MessageIds.md")`（protoInputPath 由 Task 4 定死的 Post 五参签名透传）。

- [ ] **Step 2: 验证**

`Proto2CsExport-All.bat ci` 后检查 `Protobuf/MessageIds.md`：5 个模块小节齐全、Basic 节含 `ReqHeartBeat | 10 | 655370`、User 节含 `ReqUserLogin | 10 | 19660810`、盲区注记存在。

- [ ] **Step 3: Commit**

`git commit -m "[AI]feat: 生成 MessageIds.md 抓包速查表（含内置协议盲区注记）"`

---

### Task 8: 收发日志格式微调

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Network/Channel/NetworkChannelBase.Send.cs:178`、`NetworkChannelBase.Receive.cs:134`

**Interfaces:**
- Consumes: 现有 `PacketSendHeaderHandler.Id`（消息 ID）、`msgObj.GetType().Name`
- Produces: 日志格式 `消息:[ReqUserLogin(19660810), UniqueId, ...] 消息内容:...`（类型名在前、ID 并置括号内）

- [ ] **Step 1: 改两处日志模板**

发送行改为：`FuLogger.LogInfo($"发送消息:[{msgObj.GetType().Name}({PacketSendHeaderHandler.Id}),{msgObj.UniqueId}] 消息内容:{UtilityAOT.Json.ToJson(msgObj)}");`
接收行对称（`收到消息`、`PacketReceiveHeaderHandler.Id`）。不新增映射表、不动宏开关与 IgnoreIds 过滤。

- [ ] **Step 2: Unity 编译验证**

`refresh_assets` → `get_compilation_state` errorCount=0。

- [ ] **Step 3: Commit**

`git commit -m "[AI]style: 收发调试日志类型名与消息 ID 并置"`

---

### Task 9: 文档/记忆同步 + 终验

**Files:**
- Modify: `Docs/网络模块学习笔记/05-装配系统.md`（§3.1 示例与显式锚定说明）、`Tools/ProtoExport/README.md`（消息 ID 公式章节改显式锚定规则）、`Protobuf/gen-proto-registry.py` 头部注释（扫描来源说明）

**Interfaces:**
- Consumes: 全部前序任务的最终形态
- Produces: 文档与代码一致；记忆更新

- [ ] **Step 1: 文档同步**

笔记 05：`:71` 注册示例改 `MessageIdRegistry.Register<ReqUserLogin>(MessageIds.User.ReqUserLogin);`；§3.1 表①说明补一句"opcode 在 proto 内 `option opcode` 显式锚定，缺失导出报错"。`Tools/ProtoExport/README.md:51-55` 公式章节改写为显式锚定规则（含 DTO 禁标、范围、全局唯一）。`gen-proto-registry.py:12-16` 扫描来源注释补 `MessageIds.g.cs` 查表说明。

- [ ] **Step 2: 记忆更新**

更新 `C:\Users\Administrator\.claude\projects\D---WorkSpace-Unity-FuFramework2-0\memory\proto-id-assignment.md`：隐式顺序分配 → `option opcode` 显式锚定（报错强制、DTO 禁标、Post 全局唯一校验、MessageIds 常量与速查表）。

- [ ] **Step 3: 全链终验**

`Proto2CsExport-All.bat ci` → Task 2 备份数值终 diff（仍为空）→ Unity + 服务器编译零错误 → `git status` 干净（生成物已含于各任务提交）。

- [ ] **Step 4: Commit**

`git commit -m "[AI]docs: 消息 ID 显式锚定规则同步文档"`（含笔记 05 与 README 改动）
