# FuFramework Network Module

## 1. 简介

FuFramework Network 模块是游戏框架的网络通信系统，提供完整的客户端-服务器通信能力。该模块采用分层架构设计，支持 TCP 和 WebSocket 两种传输协议，内置包序列化/反序列化管线、心跳机制、RPC 调用、消息压缩等功能。消息处理使用**生成期静态注册表**（无运行时反射），支持请求-响应、服务器推送和心跳四种消息模式。

## 2. 核心特性

- **多协议支持**：TCP、WebSocket（可扩展 KCP、UDP）
- **分层架构**：频道层 → 辅助器层 → 套接字层，职责清晰
- **特性声明 + 生成期注册**：`[MessageId]` / `[NotifyMessageHandler]` 仅作为声明，注册数据由生成期扫描源码产出的静态表提供（proto 变更后运行一键脚本 `Protobuf/Proto2CsExport-All.bat`；仅新增处理器时运行 `Protobuf/gen-proto-registry.bat`）
- **显式装配包处理器**：`DefaultNetworkChannelHelper` 直接装配框架自带的 7 个处理器，不再反射扫描程序集；自定义处理器须通过 `AddCustomHandlerRegistrar` 或重写 `RegisterCustomHandlers` 显式注册
- **四种消息模式**：请求-响应（Request/Response）、服务器推送（Notify）、心跳（HeartBeat）
- **包处理管线**：包头处理 → 包体处理 → 压缩/解压 → 心跳检测
- **异步 RPC**：基于 `UniTask` 的异步 RPC 调用，支持超时机制
- **断线重连**：可配置的重连策略
- **IPv4/IPv6**：支持双栈网络

## 3. 核心概念

### 3.1 网络分层架构

```
┌─────────────────────────────────────────────────────────────┐
│                     NetworkModule                            │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  NetworkChannelBase (网络频道)                       │   │
│  │  - 连接管理、发送/接收调度                            │   │
│  │  - 心跳状态管理                                      │   │
│  │  - RPC 状态管理                                      │   │
│  └─────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  DefaultNetworkChannelHelper (频道辅助器)             │   │
│  │  - 包头/包体序列化与反序列化                          │   │
│  │  - 消息压缩/解压                                     │   │
│  │  - 心跳包构建与识别                                   │   │
│  └─────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  INetworkSocket (网络套接字)                        │   │
│  │  - TCP (System.Net.Sockets)                         │   │
│  │  - WebSocket                                        │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### 3.2 消息模式

```
IRequestMessage   → 发送请求 → 等待 IResponseMessage（RPC）
IResponseMessage  → 响应请求（含 ErrorCode）
INotifyMessage    → 服务器主动推送（无响应）
IHeartBeatMessage → 心跳消息
```

### 3.3 包处理管线

```
发送: 消息对象 → 序列化包体 → 压缩 → 添加包头 → 发送
接收: 接收数据 → 解析包头 → 解压 → 反序列化包体 → 消息对象
```

## 4. 核心类说明

### 4.1 NetworkModule

网络管理模块，继承自 `ModuleBase`，是网络系统的统一入口。**单频道模型**：模块直接持有唯一网络频道，业务侧通过 `Call`/`Send` 门面收发消息，无需感知频道对象与频道名。

**核心属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| `Instance` | `NetworkModule` | 模块单例 |
| `Channel` | `INetworkChannel` | 当前网络频道（未创建时为 `null`） |

**核心方法：**

```csharp
// 频道生命周期（单频道模型：模块内唯一，传输由编译宏在创建时决定 TCP/WS）
INetworkChannel CreateChannel(DefaultNetworkChannelHelper networkChannelHelper, int rpcTimeout = 5000)
bool DestroyChannel()
void Connect(Uri address, object userData = null)   // 转发频道 Connect
void Close()                                        // 转发频道 Close（可重连）

// 消息收发门面（业务侧日常入口，无需感知频道对象）
UniTask<TResponse> Call<TResponse>(IRequestMessage<TResponse> msgObj)   // RPC：Req→Resp 编译期配对
void Send<T>(T msgObj)                                                  // 单向：发完即忘
```

### 4.2 INetworkChannel

网络频道接口，定义连接管理和消息收发。

```csharp
public interface INetworkChannel
{
    string Name { get; }
    bool Connected { get; }

    // 连接管理
    void Connect(Uri address, object userData = null);
    void Close();

    // 消息发送
    void Send<T>(T msgObj) where T : MessageObject;                  // 单向消息（发完即忘）
    UniTask<TResponse> Call<TResponse>(IRequestMessage<TResponse> msgObj)  // RPC 调用（Req→Resp 编译期配对）
        where TResponse : MessageObject, IResponseMessage;
}
```

### 4.3 消息注册（生成期静态表，无运行时反射）

协议消息与消息处理方法的注册数据全部由生成脚本产出，不再于运行时扫描程序集 / 读取特性。

**方式一：协议消息 —— 类级别 `[MessageId(消息ID)]`**

```csharp
[ProtoContract]
[MessageId(6553610)]
public sealed class ReqBagInfo : MessageObject, IRequestMessage { }
```

生成脚本扫描 `Game/AutoGen/Proto/*.cs`，产出 `Register/AutoGen/MessageRegistry.g.cs` 中的
`MessageIdRegistry.Register<ReqBagInfo>(6553610, EMessageKind.Request)`。

**方式二：用户处理方法 —— 方法级别 `[NotifyMessageHandler(typeof(消息类型), nameof(方法名))]`**

```csharp
public sealed class BagManager : Singleton<BagManager>   // 声明即生效：无需实现接口、无需手动注册
{
    [NotifyMessageHandler(typeof(NotifyBagInfoChanged), nameof(OnNotifyBagInfoChanged))]
    internal void OnNotifyBagInfoChanged(NotifyBagInfoChanged msg) { /* ... */ }
}
```

> 命名惯例：处理方法用 `On` 前缀（如 `OnNotifyBagInfoChanged`）——方法名表达**处理动作**，与消息类型名区分。
> **宿主契约**：所在类型必须继承 `Singleton<T>` 或提供 `static Instance`（生成器校验，不满足则中止生成）。

生成脚本扫描 `Assets/Scripts/Hotfix/**/*.cs`，为每个方法产出**静态绑定委托**（直接捕获宿主实例）：

```csharp
NotifyMessageHandlerRegistry.BindNotify(typeof(NotifyBagInfoChanged),
    static msgObj => BagManager.Instance.OnNotifyBagInfoChanged((NotifyBagInfoChanged)msgObj));
```

绑定在 `NetworkModule.OnInit` 的 `RegisterAll` 中一次性完成，**不查找方法、不使用任何反射**
（无 `GetMethods` / `IsDefined` / `GetCustomAttribute` / `CreateDelegate` / `MethodInfo.Invoke`）；
宿主实例在首次推送时才被触达（惰性）。

**推送派发阀门（基线未就绪窗口期）**

推送多为增量更新，依赖业务先建立数据基线（如登录后先 `Call` 全量背包，之后的增量推送才有意义）。
若服务器在基线建立前就可能推送，用阀门保护：

```csharp
NetworkModule.Instance.SuspendNotifyDispatch();                  // 连接后、拉取基线前：推送进缓冲队列（保持到达顺序）
var bag = await NetworkModule.Instance.Call(new ReqBagInfo());   // 建立基线
NetworkModule.Instance.ResumeNotifyDispatch();                   // 基线就绪：按序回放缓冲
NetworkModule.Instance.ClearNotifyBuffer();                      // 登出/切换账号：丢弃旧会话推送（防串号）
```

缓冲上限 1024 条（超出丢弃最旧并警告）；回放中可再次挂起（剩余留在队列）。
心跳回包不参与业务派发（由注册表心跳类型集合在派发前截流）。

> **可见性契约（重要）**：由于生成物直接调用该方法，`[NotifyMessageHandler]` 标注的方法
> **必须是 `internal` 或 `public`**（默认的 `private` 会导致生成脚本**报错并中止**，
> 报错信息会指出文件、行号与方法名）。方法参数类型必须与 `typeof(...)` 声明的消息类型一致。
> 生成物位于同程序集 `Hotfix.Framework.Network`，故 `internal` 即可。

> **重要**：新增 / 删除协议消息、修改消息ID 后，运行**一键脚本**
> `Protobuf/Proto2CsExport-All.bat`（Windows；先导出 C# 再生成注册表）或 `Protobuf/Proto2CsExport_Client.sh + Proto2CsExport_Server.sh + gen-proto-registry.sh`（macOS/Linux）。
> 若只新增 `[NotifyMessageHandler]` 方法（未动 proto），跑 `Protobuf/gen-proto-registry.bat` 即可。
> 否则注册表与实际代码不一致（未绑定的推送消息会在运行时打警告日志并被丢弃）。

**包处理器装配（`DefaultNetworkChannelHelper`）**

框架自带的 7 个包处理器（接收包头/包体、发送包头/包体、心跳、压缩、解压）由
`RegisterDefaultHandlers()` 显式装配，不再反射扫描 `IPacketHandler` 实现。
需要替换或补充处理器时：

```csharp
// 1) 静态注册入口（对所有 DefaultNetworkChannelHelper 生效，建议启动期调用一次）
DefaultNetworkChannelHelper.AddCustomHandlerRegistrar(channel =>
{
    channel.RegisterHeartBeatHandler(new Hotfix.Game.Network.PacketHeartBeatHandler());
});

// 2) 或派生 DefaultNetworkChannelHelper 并重写 RegisterCustomHandlers()
```

### 4.4 消息基类

| 类 | 位置 | 说明 |
|------|------|------|
| `MessageObject` | `MessageBase/` | 消息基类（含 UniqueId） |
| `MessageHttpObject` | `MessageBase/` | HTTP 消息包装基类 |
| `IRequestMessage` | `Interface/` | 请求消息接口 |
| `IResponseMessage` | `Interface/` | 响应消息接口（含 ErrorCode） |
| `INotifyMessage` | `Interface/` | 服务器推送消息接口 |
| `IHeartBeatMessage` | `Interface/` | 心跳消息接口 |

### 4.5 核心内部类

| 内部类 | 说明 |
|------|------|
| `ConnectState` | 连接状态管理：重连次数、重连间隔 |
| `HeartBeatState` | 心跳状态管理：发送间隔、超时检测 |
| `SendState` | 发送状态管理：MemoryStream 缓冲 |
| `ReceiveState` | 接收状态管理：MemoryStream + 包解析 |
| `RpcState` | RPC 状态管理：等待回复字典 + 超时 |

### 4.6 包处理器

处理器为具体类（各自只有单一实现，接口已按「只保留真多态」原则缩减；推送宿主契约见 `NotifyMessageHandlerRegistry`（声明即生效））：

| 类 | 说明 |
|------|------|
| `DefaultPacketSendHeaderHandler` | 发送包头处理器（游戏侧可派生覆盖，如心跳） |
| `DefaultPacketReceiveHeaderHandler` | 接收包头处理器 |
| `DefaultPacketSendBodyHandler` | 发送包体处理器（序列化） |
| `DefaultPacketReceiveBodyHandler` | 接收包体处理器（反序列化） |
| `DefaultPacketHeartBeatHandler` | 心跳包处理器 |
| `DefaultMessageCompressHandler` | 消息压缩处理器 |
| `DefaultMessageDecompressHandler` | 消息解压处理器 |

### 4.7 网络事件

| 事件类 | 说明 |
|------|------|
| `NetworkConnectedEventArgs` | 连接成功事件 |
| `NetworkClosedEventArgs` | 连接关闭事件 |
| `NetworkErrorEventArgs` | 网络错误事件 |
| `NetworkMissHeartBeatEventArgs` | 心跳丢失事件 |

## 5. 使用示例

### 5.1 创建网络连接

```csharp
using Hotfix.Framework.Core;
using Hotfix.Framework.Network;

public class NetworkExample
{
    public void Init()
    {
        // 创建网络频道（模块内唯一；传输由编译宏在创建时决定 TCP/WS）
        NetworkModule.Instance.CreateChannel(new DefaultNetworkChannelHelper(), rpcTimeout: 5000);

        // 注册消息处理器（使用 NotifyMessageHandlerRegistry）
        NotifyMessageHandlerRegistry.Add(new LoginMessageHandler());
        NotifyMessageHandlerRegistry.Add(new GameMessageHandler());
    }

    public void Connect()
    {
        // 连接服务器（转发频道 Connect；WebGL 平台底层自动使用 WebSocket 实现）
        NetworkModule.Instance.Connect(new Uri("tcp://127.0.0.1:8888"));

        // 日常收发：业务侧只写这两行
        // var resp = await NetworkModule.Instance.Call(new ReqBagInfo());
        // NetworkModule.Instance.Send(moveNotify);
    }
}
```

### 5.2 发送消息

```csharp
// 发送普通消息（服务器推送类型）
var moveNotify = new PlayerMoveNotify
{
    X = 10.5f,
    Y = 0f,
    Z = 3.2f
};
m_channel.Send(moveNotify);

// RPC 调用（请求-响应模式）：响应类型由请求的 IRequestMessage<TResponse> 映射接口自动推断
public async UniTask<LoginResponse> LoginAsync(string username, string password)
{
    var request = new LoginRequest
    {
        Username = username,
        Password = password
    };

    try
    {
        var response = await m_channel.Call(request);
        return response;
    }
    catch (TimeoutException)
    {
        Debug.LogError("登录超时");
        return null;
    }
}
```

### 5.3 监听网络事件

```csharp
var eventModule = ModuleManager.GetModule<EventModule>();

// 连接成功
eventModule.Subscribe(NetworkConnectedEventArgs.EventId, (sender, e) =>
{
    Debug.Log("网络已连接");
});

// 连接断开
eventModule.Subscribe(NetworkClosedEventArgs.EventId, (sender, e) =>
{
    Debug.Log("网络已断开");
});

// 网络错误
eventModule.Subscribe(NetworkErrorEventArgs.EventId, (sender, e) =>
{
    var args = e as NetworkErrorEventArgs;
    Debug.LogError($"网络错误: {args.ErrorMessage}");
});
```

## 6. 目录结构

> 组织原则：**真多态的接口跟随实现同居**（`Channel/`、`Socket/`），
> 消息契约与处理器契约接口集中于 `Interface/`，包处理器为具体类（`Handler/`），
> 消息基类独立（`MessageBase/`），注册设施与生成物集中（`Register/`）。
> 全模块统一命名空间 `Hotfix.Framework.Network`，目录仅为物理分组。

```text
Network/
├── NetworkModule.cs                                  # 网络管理模块（字段/生命周期/事件转发）
├── NetworkModule.API.cs                              # 对外公共 API（单频道门面：创建/连接/收发/销毁）
├── Channel/                                          # 网络频道（接口与实现同居，internal）
│   ├── INetworkChannel.cs                            # 网络频道接口
│   ├── NetworkChannelBase.cs                         # 网络频道基类
│   ├── SystemTcpNetworkChannel.cs                    # TCP 频道实现
│   └── WebSocketNetworkChannel.cs                    # WebSocket 频道实现
├── Socket/                                           # 网络套接字（接口与实现同居，internal）
│   ├── INetworkSocket.cs                             # 网络套接字接口
│   ├── SystemNetSocket.cs                            # TCP 套接字实现
│   └── WebSocketNetSocket.cs                         # WebSocket 套接字实现
├── Handler/                                          # 包处理器（具体类，单实现不设接口）
│   ├── DefaultNetworkChannelHelper.cs                # 默认频道辅助器（装配 7 个处理器）
│   ├── DefaultPacketSendHeaderHandler.cs             # 发送包头处理器
│   ├── DefaultPacketReceiveHeaderHandler.cs          # 接收包头处理器
│   ├── DefaultPacketSendBodyHandler.cs               # 发送包体处理器
│   ├── DefaultPacketReceiveBodyHandler.cs            # 接收包体处理器
│   ├── DefaultPacketHeartBeatHandler.cs              # 心跳处理器（兜底基类，可被游戏侧覆盖）
│   ├── DefaultMessageCompressHandler.cs              # 压缩处理器
│   └── DefaultMessageDecompressHandler.cs            # 解压处理器
├── Interface/                                        # 消息契约接口（含 [NotifyMessageHandler] 体系）
│   ├── IRequestMessage.cs                            # 请求消息接口
│   ├── IResponseMessage.cs                           # 响应消息接口
│   ├── INotifyMessage.cs                             # 推送消息接口
│   └── IHeartBeatMessage.cs                          # 心跳消息接口
├── MessageBase/                                      # 消息基类
│   ├── MessageObject.cs                              # 消息基类（含 UniqueId）
│   └── MessageHttpObject.cs                          # HTTP 消息包装基类
├── Attribute/                                        # 声明用特性（供生成脚本扫描）
│   ├── NotifyMessageHandlerAttribute.cs              # 消息处理器特性（方法须 internal/public）
│   └── MessageIdAttribute.cs                         # 消息 ID 标记特性
├── Register/                                         # 注册设施 + 生成物
│   ├── MessageIdRegistry.cs                          # 消息 ID / 心跳 / 类型 注册表
│   ├── NotifyMessageHandlerRegistry.cs               # 推送处理注册表（[NotifyMessageHandler] 绑定/派发）
│   └── AutoGen/
│       └── MessageRegistry.g.cs                      # 自动生成：消息与方法注册表 + 委托分派（勿手改）
├── State/                                            # 频道内部状态（internal）
│   ├── ConnectState.cs                               # 连接状态
│   ├── HeartBeatState.cs                             # 心跳状态
│   ├── SendState.cs                                  # 发送状态
│   ├── ReceiveState.cs                               # 接收状态
│   ├── RpcState.cs                                   # RPC 状态
│   └── RpcMessageData.cs                             # RPC 等待数据
├── Event/                                            # 网络事件参数
│   ├── NetworkConnectedEventArgs.cs
│   ├── NetworkClosedEventArgs.cs
│   ├── NetworkErrorEventArgs.cs
│   └── NetworkMissHeartBeatEventArgs.cs
└── Misc/                                             # 杂项（错误码）
│   └── NetworkErrorCode.cs                           # 网络错误码
└── README.md                                         # 本文档
```

## 7. 依赖

- **Hotfix.Framework.Core**：提供 ModuleBase 基类、数据结构、工具方法
- **Hotfix.Framework.Event**：事件系统
- **Hotfix.Framework.ReferencePool**：引用池
- **UniTask**：异步 RPC 支持

## 8. 最佳实践

1. **消息 ID 管理**：使用 Proto 文件统一定义消息 ID 和结构
2. **消息处理器注册**：在游戏启动流程中集中注册所有消息处理器
3. **RPC 超时处理**：所有 RPC 调用应设置合理的超时时间并处理超时异常
4. **断线重连**：监听 `NetworkClosedEventArgs` 事件，实现自动重连逻辑
5. **消息压缩**：大消息（>1KB）启用压缩，减少网络传输量

## 9. 注意事项

1. **主线程限制**：网络回调（数据包消息与 Connected/Closed/Error 生命周期事件）均在主线程执行——
   Socket 回调线程只入队，主线程 channel.Update 排水后触发；消息处理器中避免阻塞操作
2. **生命周期事件的销毁语义**：频道销毁（DestroyChannel 内部会调频道 Dispose）后 Update 不再被调用，未派发的生命周期事件将随频道对象一并丢弃（不入池、无泄漏）
3. **消息序列化**：消息类必须有无参构造函数，且字段必须可序列化
4. **内存管理**：接收缓冲区大小需根据消息大小合理配置
5. **心跳间隔**：根据服务器配置设置合理的心跳间隔（通常 5-30 秒）
6. **重连策略**：避免无限重连，设置最大重连次数和指数退避

## 10. 调试：网络消息日志

排查协议问题时，可开启每条消息的内容日志：

1. **开启/关闭宏**：菜单 `FuFramework/日志设置/开启网络请求日志打印`（响应日志同理），
   分别对应编译宏 `ENABLE_NETWORK_REQ_LOG` / `ENABLE_NETWORK_RSP_LOG`（Editor 类：`NetworkLogScriptingDefineSymbols`）
2. **日志效果**：开启后每收/发一条消息打印 `消息ID、UniqueId、消息内容(Json)`
3. **过滤高频消息**（如心跳、移动同步）：调用
   `NetworkModule.Instance.Channel.SetIgnoreLogNetworkIds(sendIds, receiveIds)` 传入要忽略打印的消息 ID 列表
   （传 null 清空对应列表；两列表独立，null 安全）

> 注意：宏变更会触发全量重编译，团队协作时不要长期开启；日志在主线程输出，高频包建议配合忽略列表使用。
