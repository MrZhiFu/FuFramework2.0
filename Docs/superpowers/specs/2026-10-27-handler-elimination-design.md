# 设计：删除 Handler/ 目录（信封读写并入 NetworkChannelHelper）

- 日期：2026-10-27
- 状态：待批准
- 范围：`INetworkChannel` 接口瘦身、`NetworkChannelBase.API/Receive/Send`、`NetworkChannelHelper` 重构、TCP/WS 通道调用点、`Handler/` 目录删除（HeartBeatHandlerBase 搬家）、`gen-proto-registry.py` 路径同步、文档与笔记

## 1. 背景与动机

`Handler/` 下 6 个"协议装备"类源自反射时代（`IPacketHandler` 插件式扫描，Handler 之名当时名副其实）。上一轮动词化重构（`Handler()` → `SerializeAndWriteToCacheStream` 等）后，它们的真实形态暴露：

| 类 | 真实形态 | 状态 |
|---|---|---|
| `PacketSendHeaderHandler` | 信封写入器：序列化包头并写缓存流，**缓存当前包 Id/IsZip/PacketLength**（跨调用状态） | 有状态 |
| `PacketReceiveHeaderHandler` | 信封读取器：解析包头，**缓存当前包 Id/UniqueId**（`SetUniqueId`/日志/反序列化消费） | 有状态 |
| `PacketSendBodyHandler` | `cacheStream.Write(msgBodyBuffer,…)` **一行转发** | 纯函数 |
| `PacketReceiveBodyHandler` | `SerializerHelper.Deserialize` **一行转发** | 纯函数 |
| `MessageCompressHandler` | `Utility.Zip.Compress` **一行转发** | 纯函数 |
| `MessageDecompressHandler` | `Utility.Zip.Decompress` **一行转发** | 纯函数 |

三个问题：
1. **未兑现的扩展缝**：`INetworkChannel` 暴露 6 个处理器属性 + 对应 Register 方法（"后注册覆盖"语义），**从未被游戏侧使用**——唯一的覆盖（心跳）走 `RegisterExternalPacketHandler` 通道。违反"未兑现的扩展缝不预留"。
2. **概念-代码错位**：它们已不是"Handler"（有生命周期的插件对象），而是一组信封读写函数；"Handler"作为类别词在上一轮动词化后彻底名存实亡。
3. **间接层无增益**：Body/Compress/Decompress 纯转发，调用链多一层无信息量的中转。

## 2. 目标 / 非目标 / 反目标

**目标**
- 删除 `Handler/` 目录与 6 个装备类，信封读写逻辑并入 `NetworkChannelHelper`（私有方法 + 私有状态字段）；
- `INetworkChannel` 删除 6 个装备类的全部成员（属性 + Register 方法）；**心跳通道 2 个成员保留**（`RegisterHeartBeatHandler(PacketHeartBeatHandlerBase)` + `PacketHeartBeatHandler` 属性——它们是**已兑现**的通道：生成物 `MessageRegistry.gen.cs` 正以 `channel.RegisterHeartBeatHandler(...)` 注册游戏侧实现，删之则须改生成器模板并重跑生成，违反非目标）；
- `PacketHeartBeatHandlerBase` 保留（依赖方向支柱），文件搬出 Handler/；
- 行为完全不变（字节级：包头五字段、压缩判定 >512B、Unique 回显、心跳 OperationType）。

**非目标**
- 不做"可替换压缩器/解析器"的新扩展机制（真需要时再以显式形式回归）；
- 不动 `RegisterExternalPacketHandler` 通道与生成物 `RegisterHeartBeatHandler` 注册；
- 不动 RpcState/阀门/Priority（近期已落地机制）。

**反目标**
- 字节布局或收发行为发生任何变化（含 debug 日志输出格式）；
- 生成物 `MessageRegistry.gen.cs` 结构变化（仅其扫描排除路径注释/路径同步）。

## 3. 已定决策

| 决策 | 结论 |
|---|---|
| 6 类去向 | 全部删除：4 个纯转发内联；2 个 Header 的状态与方法并入 helper 私有 |
| `INetworkChannel` | 删除 6 个装备类的属性与 Register 方法；**保留** `RegisterHeartBeatHandler` + `PacketHeartBeatHandler` 属性（心跳通道已兑现，生成物在用） |
| 压缩器来源 | helper 直接调 `Utility.Zip.Compress/Decompress`（压缩阈值 `LimitCompressLength` 常量随迁） |
| 调试日志的 Id 来源 | helper 公开只读属性 `LastSentMessageId` / `LastReceivedMessageId` / `LastReceivedUniqueId`（原 `PacketXxxHandler.Id/UniqueId`） |
| 接收侧包头状态 | **`ReceiveState` 改为携带解析值**（bodyLength/zipFlag 等字段）：包头期从 helper 解析结果存入，包体期跨批次消费（粘包/半包状态机语义不变）；`ReceiveState.cs` 纳入范围 |
| 无消费者的附带清理 | 发送侧缓存 `IsZip`/`PacketLength`、接收侧 `OperationType` 均**无消费者**——解析照常推进，但不保留为状态/属性（builder 核实） |
| `PacketHeartBeatHandlerBase` | 文件移至 `Framework/Network/` 根（扁平命名空间，using 不变）；游戏侧 `PacketHeartBeatHandler` 不动 |
| 生成器 | 扫描机制保留（类声明+继承链匹配 `PACKET_INTERFACE_ORDER`，心跳匹配靠它）；**剪掉 6 条装备基类的死模板项**、更新 `NETWORK_HANDLER_DIR` 排除路径与注释；**重跑 `gen-proto-registry.py`** 提交再生成物（.gen.cs diff 仅为头部注释过时措辞） |

## 4. 设计

### 4.1 `NetworkChannelHelper` 重构后形态

- **私有状态**（原两个 Header 类字段并入）：`m_cachedByte`（14B 包头缓冲）、`m_offset`、`m_currentSentId`、`m_currentIsZip`、`m_currentPacketLength`、`m_currentReceivedId`、`m_currentReceivedUniqueId`、压缩阈值常量。
- **公开只读**（供 ChannelBase 日志/收侧消费）：`LastSentMessageId`、`LastReceivedMessageId`、`LastReceivedUniqueId`。
- **公开方法**（原 helper 方法保留签名，内部不再转发 handler）：
  - `SerializeAndWriteToCacheStream<T>`：内联原 SendHeader 逻辑（查表 ID → 序列化 → 压缩判定（直接 `Utility.Zip.Compress`）→ 写 14B 头到 cacheStream）；
  - `WritePacketBodyToCacheStream`：内联原 SendBody 一行；
  - `DeserializePacketBody`：内联原 ReceiveBody 逻辑（`SerializerHelper.Deserialize`）；
  - **新增** `DeserializePacketHeader(object source)`：原 `PacketReceiveHeaderHandler.Deserialize`（TCP/WS 收包链调用），内部刷新 `m_currentReceivedId/UniqueId` 等接收状态；
- **装配**：`RegisterHandlers()` 删除（无默认处理器可装）；`Initialize(INetworkChannel)` 只剩 external 注册循环；helper 与 channel 之间删除的 Handler 属性/校验依赖相应清理（**心跳通道除外**——`SendHeartBeat()` 仍经 `m_networkChannel.PacketHeartBeatHandler.CreateHeartBeatReq()` 构造）。

### 4.2 接口与调用点

- `INetworkChannel`：删除 6 个装备类的属性与 Register 方法（心跳的 `RegisterHeartBeatHandler` + `PacketHeartBeatHandler` 属性**保留**）；
- `NetworkChannelBase.API`：删除对应属性与 Register 方法；`Send<T>`/`Call<TResponse>` 内部校验与 DebugSendLog 改读 helper 只读属性；
- `SystemTcpNetworkChannel` / `WebSocketNetworkChannel`：`PacketReceiveHeaderHandler.Id/UniqueId` → helper 只读属性；包头解析改调 helper（解析值存入 `ReceiveState` 新字段——跨批次存活语义不变）；`MessageDecompressHandler.Decompress` → `Utility.Zip.Decompress`；
- `PacketHeartBeatHandlerBase` 的调用链（`m_NetworkChannelHelper.SendHeartBeat()` → `CreateHeartBeatReq()`）不变。

### 4.3 生成器与文档

- `gen-proto-registry.py`：扫描机制**保留**（类声明+继承链匹配 `PACKET_INTERFACE_ORDER`）；**剪掉 6 条装备基类的死模板项**、更新 `NETWORK_HANDLER_DIR` 排除路径与"Framework/Network/Handler/ 之外"等注释；**重跑生成**提交再生成物（.gen.cs diff 仅为头部注释过时措辞，如"RegisterHandlers()（框架自带 6 个处理器）"）；
- 笔记 01/02/03/04/05/06 与 README：处理器类名引用全面改写（"6 个处理器"→"helper 内置信封读写"、装配章节重写）；
- 记忆 `network-single-channel-model` 同步（接口成员数、装配描述、Handler 词退役）。

## 5. 约束

1. 字节级行为不变：14B 包头五字段顺序、大端、压缩阈值、心跳 OperationType 判定（is IHeartBeatMessage）；
2. `m_HeartBeatLock`/`m_handleMsgLock`/`m_closeLock` 锁序不变；
3. 生成物 diff 仅为头部注释措辞（心跳注册行原样保留）；
4. 全程编译验证（Unity + unity-cli）+ 既有端到端行为（登录链路）不受影响。

## 6. 留给 builder 的部分

- 接口成员删除的精确清单（以 grep 接口现状为准）；
- helper 私有字段与公开只读属性的最终命名；
- `PacketHeartBeatHandlerBase` 新位置的最终选择（根目录 vs 独立文件名）；
- 笔记各篇改写的具体措辞。
