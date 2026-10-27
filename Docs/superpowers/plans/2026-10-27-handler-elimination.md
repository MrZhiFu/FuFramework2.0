# Handler 目录删除（信封读写并入 Helper）实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 删除 `Network/Handler/` 目录与 6 个装备类，信封读写并入 `NetworkChannelHelper` 私有；`INetworkChannel` 删 12 个装备成员、保留 2 个心跳成员；行为字节级不变。

**Architecture:** `NetworkChannelHelper` 从"转发器"升级为"信封读写器"（原 6 个 Handler 的逻辑与状态直接成为 helper 的私有实现）；接收侧跨批次状态（ZipFlag/包长）从"持有 Handler 引用"改为"`ReceiveState` 携带解析值字段"；心跳通道（`RegisterHeartBeatHandler` + `PacketHeartBeatHandler` 属性）是已兑现契约，原样保留。

**Tech Stack:** C#（Unity Hotfix 程序集）、Python（gen-proto-registry.py）、protobuf 导出链

**Spec:** `Docs/superpowers/specs/2026-10-27-handler-elimination-design.md`

## Global Constraints

- 字节级行为不变：14B 包头五字段顺序（PacketLength/OperationType/ZipFlag/UniqueId/Id）、大端、压缩阈值 `LimitCompressLength => 512`、心跳 `is IHeartBeatMessage` 判定
- 生成物 `MessageRegistry.gen.cs` 的 diff 仅为头部注释措辞（心跳注册行原样保留）
- 锁序不变：`m_HeartBeatLock` / `m_handleMsgLock` / `m_closeLock`
- 铁律 4：全程零反射；杜绝 LINQ（新增代码手写循环）
- 每任务结束 Unity 编译 0 错误（`unity-cli tool call refresh_assets` → `get_compilation_state`）
- 每笔提交前先询问用户（用户纪律）
- 重跑 `Proto2CsExport-All.bat ci` 时**必须带 `ci` 参数**（否则 pause 假死）

## Review Focus

1. **字节布局漂移**：helper 内联序列化时五字段写入顺序/长度与原 `PacketSendHeaderHandler` 逐字节一致 → Task 1 步骤 5 逐字段比对
2. **接收状态跨批次丢失**：`ZipFlag`/包长在"包头期与包体期跨收包批次"时必须存活（`ReceiveState` 字段承载）→ Task 2 半包场景代码走查
3. **心跳通道误删**：`RegisterHeartBeatHandler`/`PacketHeartBeatHandler` 在接口上必须保留（生成物在用）→ Task 3 grep 断言
4. **生成物非注释 diff**：重跑后 `.gen.cs` 除头部注释外必须零变化 → Task 3 diff 检查
5. **空消息分支状态机残留**：包体长 0 时当帧 `ProcessPackBody` 复位语义不得变 → Task 2 走查 TCP `ProcessReceivedBytes` 空消息分支

---

### Task 1: 发送侧切换（helper 内联 + 调用点 + 删 3 类）

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Network/Channel/NetworkChannelHelper.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Network/Channel/NetworkChannelBase.API.cs`（删发送侧属性/Register）
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Network/Channel/NetworkChannelBase.Send.cs`（DebugSendLog 切换）
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Network/Channel/INetworkChannel.cs`（删发送侧成员）
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Network/Channel/NetworkChannelHelper.cs` 的 `RegisterHandlers()`（删发送/压缩 4 行注册）
- Delete: `Unity/Assets/Scripts/Hotfix/Framework/Network/Handler/PacketSendHeaderHandler.cs`、`PacketSendBodyHandler.cs`、`MessageCompressHandler.cs`（含 .meta）

**Interfaces:**
- Consumes: 现状 `PacketSendHeaderHandler.SerializeAndWriteToCacheStream<T>` 的完整逻辑（含 `LimitCompressLength => 512`、`m_cachedByte`/`m_offset`）、`PacketSendBodyHandler.WriteToCacheStream` 一行
- Produces: helper 私有字段 `m_cachedByte`（byte[14]）/`m_offset`/`m_currentSentId`（int，供 DebugSendLog）与公开只读 `int LastSentMessageId => m_currentSentId`；`SerializeAndWriteToCacheStream<T>(T msgObj, MemoryStream cacheStream, out byte[] msgBodyBuffer)`（**签名去掉 MessageCompressHandler 参数**，压缩判定内联 `Utility.Zip.Compress`）；`WritePacketBodyToCacheStream(byte[] msgBodyBuffer, MemoryStream cacheStream)` 直实现。`INetworkChannel` 发送侧成员（`PacketSendHeaderHandler`/`PacketSendBodyHandler`/`MessageCompressHandler` 属性 + `RegisterPacketSendHeaderHandler`/`RegisterPacketSendBodyHandler`/`RegisterMessageCompressHandler`/`RegisterMessageDecompressHandler` 方法）删除

- [ ] **Step 1: helper 内联发送与压缩**

`NetworkChannelHelper`：新增私有字段 `m_cachedByte = new byte[14]`、`m_offset`、`m_currentSentId` 与常量 `LimitCompressLength => 512`（值随迁自 handler）；`SerializeAndWriteToCacheStream<T>` 改为直实现——逐行移植 `PacketSendHeaderHandler.SerializeAndWriteToCacheStream` 原逻辑：`m_offset = 0` → `m_currentSentId = MessageIdRegistry.GetIdByType(messageType)` → `msgBodyBuffer = SerializerHelper.Serialize(msgObj)` → `Length > 512` 时 `msgBodyBuffer = Utility.Zip.Compress(msgBodyBuffer)` 并置压缩标志 → 按**原顺序**写五字段（`WriteUInt(PacketLength)`、`WriteByte(操作类型)`、`WriteByte(压缩标记)`、`WriteInt(msgObj.UniqueId)`、`WriteInt(Id)`）→ `cacheStream.Write(m_cachedByte, 0, 14)`。`WritePacketBodyToCacheStream` 直实现为 `cacheStream.Write(msgBodyBuffer, 0, msgBodyBuffer.Length); return true;`。删除对 `m_networkChannel.PacketSendHeaderHandler/PacketSendBodyHandler/MessageCompressHandler` 的全部引用。操作类型字节保持现状取值（`is IHeartBeatMessage ? 1 : 4`——原 handler 内的逻辑随迁，grep 原文件确认）。

- [ ] **Step 2: 删除发送侧接口成员与 ChannelBase 属性/Register**

`INetworkChannel.cs` 删：`PacketSendHeaderHandler`、`PacketSendBodyHandler`、`MessageCompressHandler` 三属性与 `RegisterPacketSendHeaderHandler`、`RegisterPacketSendBodyHandler`、`RegisterMessageCompressHandler`、`RegisterMessageDecompressHandler` 四方法。`NetworkChannelBase.API.cs` 删同名属性与 Register 方法体（`RegisterMessageDecompressHandler` 也删——接收侧 Decompress 内联后无人需要它）。`ChannelBase.Send.cs` 的 `DebugSendLog` 中 `PacketSendHeaderHandler.Id` → `m_NetworkChannelHelper.LastSentMessageId`。

- [ ] **Step 3: 删 3 类 + RegisterHandlers 剪发送/压缩 4 行**

`git rm` 三个 Handler 类文件（含 .meta）；`NetworkChannelHelper.RegisterHandlers()` 删除发送包头/发送包体/压缩/解压 4 行注册（保留接收包头/接收包体 2 行——Task 2 处理）。

- [ ] **Step 4: 编译验证**

Run: `unity-cli tool call refresh_assets` → 轮询 `get_compilation_state`
Expected: `errorCount: 0`

- [ ] **Step 5: 询问用户后提交**

`git add -A && git commit -m "[AI]refactor: 发送侧信封读写并入 NetworkChannelHelper（删 PacketSendHeader/PacketSendBody/MessageCompress 三类）"`——**先向用户确认**

---

### Task 2: 接收侧切换（helper 内联解析 + ReceiveState 改造 + 调用点 + 删 3 类）

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Network/Channel/NetworkChannelHelper.cs`（接收解析内联 + 只读属性）
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Network/State/ReceiveState.cs`（PacketHeader 属性 → 解析值字段）
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Network/Channel/SystemTcpNetworkChannel.cs`、`WebSocketNetworkChannel.cs`（调用点）
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Network/Channel/NetworkChannelBase.Receive.cs`（DebugReceiveLog 切换、`ValidateAndGetPacketBodyLength` 迁移）
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Network/Channel/INetworkChannel.cs`、`NetworkChannelBase.API.cs`（删接收侧成员）
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Network/Channel/NetworkChannelHelper.cs` 的 `RegisterHandlers()`（删剩余 2 行）→ 删除该方法
- Delete: `Handler/PacketReceiveHeaderHandler.cs`、`PacketReceiveBodyHandler.cs`、`MessageDecompressHandler.cs`（含 .meta）

**Interfaces:**
- Consumes: 现状 `PacketReceiveHeaderHandler.Deserialize(object)` 的解析逻辑（五字段顺序读取）、`ReceiveState.Reset(int, PacketReceiveHeaderHandler)` 的消费点清单
- Produces: helper 私有 `m_receivedPacketLength`(uint)/`m_receivedId`(int)/`m_receivedUniqueId`(int)/`m_receivedZipFlag`(byte)（`OperationType` 解析推进但不保留）；公开只读 `int LastReceivedMessageId`、`int LastReceivedUniqueId`、`byte LastReceivedZipFlag`；公开 `bool DeserializePacketHeader(byte[] source)`（直实现）与 `int ValidateAndGetPacketBodyLength()`（原 ChannelBase.Receive 静态方法迁入，读 `m_receivedPacketLength - 14` 并校验 `MaxPacketBodyLength`——该上限常量随迁至 helper）；`ReceiveState` 新形态：**删除** `PacketHeader` 属性，**新增** `bool IsAwaitingBody { get; set; }` 与 `byte ZipFlag { get; set; }`，`Reset(int targetLength, PacketReceiveHeaderHandler)` → `Reset(int targetLength)`（`PrepareForPacketHeader` 调用点同步）

- [ ] **Step 1: helper 接收解析内联 + 只读属性**

`DeserializePacketHeader(byte[] source)` 直实现（五字段顺序读取：packetLength/operationType/zipFlag/uniqueId/id，写入私有字段；`operationType` 解析后丢弃不保留）；新增公开只读 `LastReceivedMessageId`/`LastReceivedUniqueId`/`LastReceivedZipFlag`；`ValidateAndGetPacketBodyLength()` 迁入（校验 `packetLength < 14` 抛 `InvalidOperationException`、`bodyLength > MaxPacketBodyLength` 抛出——`MaxPacketBodyLength` 静态属性随迁，ChannelBase.Receive 侧删除）。

- [ ] **Step 2: ReceiveState 改造**

删 `PacketHeader` 属性与 `PacketReceiveHeaderHandler` 引用；新增 `bool IsAwaitingBody { get; set; }` 与 `byte ZipFlag { get; set; }`；`Reset(int targetLength, PacketReceiveHeaderHandler)` → `Reset(int targetLength)`（`IsEmptyBody` 语义不变）；`PrepareForPacketHeader()` 内 `IsAwaitingBody = false`。

- [ ] **Step 3: TCP/WS 调用点切换**

TCP：`:187 m_ReceiveState.PacketHeader != null` → `m_ReceiveState.IsAwaitingBody`；`ProcessPackHeader` 内 `:229-230` 改为 `bodyLength = m_NetworkChannelHelper.ValidateAndGetPacketBodyLength(); m_ReceiveState.Reset(bodyLength); m_ReceiveState.ZipFlag = m_NetworkChannelHelper.LastReceivedZipFlag; m_ReceiveState.IsAwaitingBody = true;`；`ProcessPackBody` 内 `:240 ValidateAndGetPacketBodyLength(m_ReceiveState.PacketHeader)` → `bodyLength = (int)m_ReceiveState.Stream.Length`（Reset 时已设为目标包体长，与原返回值等价）、`:244 ZipFlag` → `m_ReceiveState.ZipFlag`、`:248 MessageDecompressHandler.Decompress` → `Utility.Zip.Decompress`、`:251 消息ID` → `m_NetworkChannelHelper.LastReceivedMessageId`、`:254 UniqueId` → `m_NetworkChannelHelper.LastReceivedUniqueId`。WS 同构修改（WS :298/307/308 各点）。**空消息分支（TCP `IsEmptyBody` 当帧 `ProcessPackBody`）的复位语义逐行走查保持**。

- [ ] **Step 4: 删接收侧接口成员与 ChannelBase 属性/Register + 删 3 类 + RegisterHandlers 删除**

`INetworkChannel`/`NetworkChannelBase.API` 删 `PacketReceiveHeaderHandler`/`PacketReceiveBodyHandler` 属性与 `RegisterPacketReceiveHeaderHandler`/`RegisterPacketReceiveBodyHandler` 方法；`ChannelBase.Receive.cs` 的 `ValidateAndGetPacketBodyLength` 静态方法删除（已迁 helper）；`RegisterHandlers()` 删剩余 2 行后整个方法删除，`Initialize` 内调用点删除；`git rm` 三个接收类文件（含 .meta）。此时 `Handler/` 目录仅剩 `PacketHeartBeatHandlerBase.cs`。

- [ ] **Step 5: 编译验证 + 行为走查**

Run: `unity-cli tool call refresh_assets` → 轮询 → `errorCount: 0`
走查（Review Focus 2/5）：半包跨批次（ZipFlag 存活）、空消息当帧复位、粘包切分——对照 `ProcessReceivedBytes` 新旧流程逐行。

- [ ] **Step 6: 询问用户后提交**

`git commit -m "[AI]refactor: 接收侧信封读写并入 NetworkChannelHelper（ReceiveState 携带解析值，删 PacketReceiveHeader/PacketReceiveBody/MessageDecompress 三类）"`——**先向用户确认**

---

### Task 3: HeartBeatHandlerBase 搬家 + 目录删除 + 生成器同步

**Files:**
- Move: `Handler/PacketHeartBeatHandlerBase.cs` → `Unity/Assets/Scripts/Hotfix/Framework/Network/HeartBeatHandlerBase.cs`（git mv，含 .meta）
- Delete: `Handler/` 目录与 `Handler.meta`
- Modify: `Unity/Assets/Scripts/Hotfix/Game/Network/PacketHeartBeatHandler.cs`（`override Handler()` → `override CreateHeartBeatReq()`）
- Modify: `Protobuf/gen-proto-registry.py`（`PACKET_INTERFACE_ORDER` 剪 6 条、`NETWORK_HANDLER_DIR` 处理、:25 注释）
- Regen: `.gen.cs`（重跑 `gen-proto-registry.bat ci`…实际跑 `python gen-proto-registry.py`）

**Interfaces:**
- Consumes: Task 1/2 后 `Handler/` 仅剩 `PacketHeartBeatHandlerBase`
- Produces: `PacketHeartBeatHandlerBase` 位于 Framework/Network 根；生成器 `PACKET_INTERFACE_ORDER = [("PacketHeartBeatHandlerBase", "channel.RegisterHeartBeatHandler(new {fq}());")]`；生成物 `.gen.cs` 心跳注册行不变、头部注释更新

- [ ] **Step 1: 游戏侧方法改名 + 基类搬家**

`PacketHeartBeatHandler.cs`：`public override MessageObject Handler()` → `public override MessageObject CreateHeartBeatReq()`；`git mv` 基类文件至 `Framework/Network/HeartBeatHandlerBase.cs`；删除空目录 `Handler/` 与 `Handler.meta`。

- [ ] **Step 2: 生成器剪模板与路径**

`PACKET_INTERFACE_ORDER` 只留 `("PacketHeartBeatHandlerBase", "channel.RegisterHeartBeatHandler(new {fq}());")`；**删除** `NETWORK_HANDLER_DIR` 常量（:68）与 `is_under(decl["path"], NETWORK_HANDLER_DIR)` 排除块（:592 一带——原目的为排除框架内处理器声明，框架内已无处理器声明，排除失义；基类移至 Framework/Network 根后自身不命中继承匹配，无副作用）；:25 注释"排除 Framework/Network/Handler/"同步措辞。

- [ ] **Step 3: 重跑生成 + diff 检查**

Run: `cd Protobuf && python gen-proto-registry.py`
Expected: `.gen.cs` 中心跳注册行 `channel.RegisterHeartBeatHandler(new Hotfix.Game.Network.PacketHeartBeatHandler());` 原样；头部注释"框架自带 6 个处理器"等措辞更新；**其余零 diff**（Review Focus 4）。

- [ ] **Step 4: 编译验证**

Run: unity-cli 编译轮询
Expected: `errorCount: 0`

- [ ] **Step 5: 询问用户后提交**

`git commit -m "[AI]refactor: Handler 目录删除（HeartBeatHandlerBase 搬家，生成器剪死模板）"`——**先向用户确认**

---

### Task 4: 文档与笔记同步 + 终验

**Files:**
- Modify: `Docs/网络模块学习笔记/01/02/03/04/05/06`、`README.md`（"6 个处理器"→"helper 内置信封读写"、装配章节重写、处理器类名引用清理）
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Network/README.md`（装配章节）
- Modify: 记忆 `network-single-channel-model.md`

**Interfaces:**
- Consumes: Task 1-3 的最终形态
- Produces: 文档与代码一致

- [ ] **Step 1: 笔记同步**

要点（grep 逐个核对）：01 认知 3"默认只装收发包头/包体与压缩 6 个处理器"；02 线程分工表/锁表无 Handler 类名（确认即可）；03 表格"`Id`/`UniqueId` 各 4"上下文与 `MaxPacketBodyLength`；04 认知 3、3.3 `helper.SendHeartBeat()`、05 的 3.3 装配章节（`RegisterHandlers()` 摘录重写为"helper 内置信封读写"）与 3.4 心跳段；06 无类名引用（确认）。

- [ ] **Step 2: README 与记忆同步**

`Unity/Assets/Scripts/Hotfix/Framework/Network/README.md` 装配章节（"框架自带的 6 个包处理器…RegisterHandlers() 显式装配"重写）；记忆 `network-single-channel-model.md`（"处理器与 helper 无接口"条目、装配描述、Handler 词退役）。

- [ ] **Step 3: 终验**

Run: `unity-cli` 编译轮询 → `errorCount: 0`；`dotnet build Server/Server.sln`（服务器不受影响，0 错误）；全仓 grep `PacketSendHeaderHandler|PacketSendBodyHandler|PacketReceiveHeaderHandler|PacketReceiveBodyHandler|MessageCompressHandler|MessageDecompressHandler` → 仅生成器注释与历史 spec 命中（代码零残留）。

- [ ] **Step 4: 询问用户后提交**

`git commit -m "[AI]docs: Handler 删除重构文档同步（笔记/README/记忆）"`——**先向用户确认**
