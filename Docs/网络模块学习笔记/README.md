# 网络模块学习笔记（Hotfix.Framework.Network）

> 面向网络开发小白的六篇深度自学资料。每篇都从 **What（是什么）→ Why（为什么这样设计）→ How（真实代码怎么走）** 三个维度展开，
> 并把全部自测题与参考答案整理进各篇末尾，方便随学随考。
> 内容基于仓库真实代码（`Unity/Assets/Scripts/Hotfix/Framework/Network/`），可边读边对照代码。

## 六篇导航

| 篇 | 主题 | 关键词 | 配套 archify 图 |
|----|------|--------|----------------|
| [01 · 一条消息的一生](01-一条消息的一生.md) | 全景数据流 + 网络频道 | 发送/接收管道、粘包/半包、UniqueId 侧信道还原、频道=线路封装、模块分层 | [消息数据流图](../../.archify/dataflow-message-lifecycle-20261019-222217/message-lifecycle.html) · [模块架构图](../../.archify/architecture-network-overview-20261019-222217/network-overview.html) |
| [02 · Socket 与 TCP](02-Socket与TCP.md) | 传输层 | TCP 承诺、粘包/半包、BeginReceive、APM vs await、线程分工、FIFO 队列、锁 | — |
| [03 · 包头包体与压缩](03-包头包体与压缩.md) | 协议层 | 14 字节信封、int/uint/byte、大端字节序、BinaryPrimitives、压缩策略、对表 | — |
| [04 · 频道核心](04-频道核心.md) | 收发状态机·心跳·锁 | Update 一镜到底、发送/接收状态机、心跳保活与断线判定、锁的领地 | [心跳状态机](../../.archify/lifecycle-heartbeat-20261019-222217/heartbeat.html) |
| [05 · 装配系统](05-装配系统.md) | 零反射注册 | 铁律 4、生成期前移、三张表、MessageIds 语义常量、显式锚定、强类型委托、Priority 排序、编译期守卫 | [频道类体系](../../.archify/architecture-channel-classes-20261019-222217/channel-classes.html) |
| [06 · RPC 与业务接入](06-RPC与业务接入.md) | 请求-响应 | 挂起表、UniqueId 侧信道、超时/断线异常终结、TryReply/派发分流、HTTP/TCP 双段登录、推送派发阀门 | [RPC 时序图](../../.archify/sequence-rpc-call-20261019-222217/rpc-call.html) |

## 使用建议

- **学习路线**：01 → 02 → 03 → 04 → 05 → 06，再由"参照自选站"复习。
- **每篇内嵌自测题**：先做题，再对答案；答错回到对应小节重读。
- **图怎么用**：图为独立 HTML（内联 SVG），浏览器打开可交互；节点挂真实源码行号。
- 推荐对照代码阅读，核心文件：`Framework/Network/MessageBase/MessageObject.cs`（UniqueId 侧信道）、`Register/MessageIdRegistry.cs`、`Register/NotifyMessageHandlerRegistry.cs`（绑定表 + Priority + 派发阀门）、`Register/AutoGen/MessageRegistry.gen.cs`（生成物，勿手改）、`Channel/NetworkChannelBase.API.cs`（Call/Send）、`Channel/NetworkChannelHelper.cs`（包处理器装配）、`State/RpcState.cs`（挂起表）；重连三件套 `Framework/Network/NetworkModule.Reconnect.cs` + 编排 `Game/Network/ReconnectManager.cs`（含 `PlayerLoginFlow` / `LogoutFlow`）。

## 工具链速览（写完协议 / 改完推送监听后）

- 协议导出：`.proto`（文件头 `option module`、消息体 `option opcode` 显式锚定，缺失报错）→ 菜单 `FuFramework/网络/导出Proto—全部`（或 `Protobuf/Proto2CsExport-All.bat ci`）→ 生成两端 C# + `MessageIds.gen.cs`（语义常量）；
- 推送注册表：仅加 `[NotifyMessageHandler]` 时跑菜单 `FuFramework/网络/重新生成推送注册表`（或 `gen-proto-registry.bat`）——生成 `MessageRegistry.gen.cs` 的 `BindNotify` 绑定；
- 完好性兜底：编译期守卫（`Unity/Assets/Editor/FuFramework/Network/NotifyRegistryGuard.cs`）自动比对业务声明与绑定，过期即报错；抓包查号用 `Protobuf/MessageIds.md` 速查表。

## 术语约定（贯穿全篇）

- **信封 = 自定义包头（14 字节）**，**信纸 = 包体**。
- **主信道 = proto 消息体**（正式内容）；**侧信道 = 自定义包头**（顺带夹带，如 UniqueId）。
- 全部缩写与词条见各篇附录。