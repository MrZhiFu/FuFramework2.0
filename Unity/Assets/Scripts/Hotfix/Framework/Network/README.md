# FuFramework Network Module

## 1. 简介

FuFramework Network 模块是游戏框架的网络通信系统，提供完整的客户端-服务器通信能力。该模块采用分层架构设计，支持 TCP 和 WebSocket 两种传输协议，内置包序列化/反序列化管线、心跳机制、RPC 调用、消息压缩等功能。消息处理使用特性驱动的反射注册机制，支持请求-响应、服务器推送和心跳四种消息模式。

## 2. 核心特性

- **多协议支持**：TCP、WebSocket（可扩展 KCP、UDP）
- **分层架构**：频道层 → 辅助器层 → 套接字层，职责清晰
- **特性驱动注册**：`[MessageHandler]` + `[MessageTypeHandler]` 自动注册消息处理器
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
│  │  INetworkChannelHelper (频道辅助器)                  │   │
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

网络管理模块，继承自 `ModuleBase`，是网络系统的统一入口。

**核心属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| `Instance` | `NetworkModule` | 模块单例 |
| `NetworkChannelCount` | `int` | 网络频道数量 |

**核心方法：**

```csharp
// 创建网络频道
INetworkChannel CreateNetworkChannel(string channelName, INetworkChannelHelper networkChannelHelper, int rpcTimeout = 5000)

// 获取/销毁频道
bool HasNetworkChannel(string channelName)
INetworkChannel GetNetworkChannel(string channelName)
INetworkChannel[] GetAllNetworkChannels()
void GetAllNetworkChannels(List<INetworkChannel> results)
bool DestroyNetworkChannel(string channelName)
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
    void Send<T>(T messageObject) where T : MessageObject;          // 普通消息
    Task<TResult> Call<TResult>(MessageObject messageObject)        // RPC 调用
        where TResult : MessageObject, IResponseMessage;
}
```

### 4.3 消息处理器注册

**方式一：类级别注册 `[MessageTypeHandler]`**

```csharp
[MessageTypeHandler]
public class LoginMessageHandler
{
    [MessageHandler]
    public void HandleLoginResponse(object sender, LoginResponse response)
    {
        // 处理登录响应
    }
}
```

**方式二：方法级别注册 `[MessageHandler]`**

```csharp
public class GameMessageHandler
{
    [MessageHandler]
    private void OnPlayerMove(PlayerMoveNotify notify)
    {
        // 处理玩家移动通知
    }
}
```

### 4.4 消息基类

| 类 | 说明 |
|------|------|
| `MessageObject` | 消息基类（含 UniqueId） |
| `IRequestMessage` | 请求消息接口 |
| `IResponseMessage` | 响应消息接口（含 ErrorCode） |
| `INotifyMessage` | 服务器推送消息接口 |
| `IHeartBeatMessage` | 心跳消息接口 |

### 4.5 核心内部类

| 内部类 | 说明 |
|------|------|
| `ConnectState` | 连接状态管理：重连次数、重连间隔 |
| `HeartBeatState` | 心跳状态管理：发送间隔、超时检测 |
| `SendState` | 发送状态管理：MemoryStream 缓冲 |
| `ReceiveState` | 接收状态管理：MemoryStream + 包解析 |
| `RpcState` | RPC 状态管理：等待回复字典 + 超时 |

### 4.6 包处理接口

| 接口 | 说明 |
|------|------|
| `IPacketSendHeaderHandler` | 发送包头处理器 |
| `IPacketReceiveHeaderHandler` | 接收包头处理器 |
| `IPacketSendBodyHandler` | 发送包体处理器（序列化） |
| `IPacketReceiveBodyHandler` | 接收包体处理器（反序列化） |
| `IPacketHeartBeatHandler` | 心跳包处理器 |
| `IMessageCompressHandler` | 消息压缩接口 |
| `IMessageDecompressHandler` | 消息解压接口 |

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
    private NetworkModule m_NetworkModule;
    private INetworkChannel m_Channel;

    public void Init()
    {
        m_NetworkModule = ModuleManager.GetModule<NetworkModule>();

        // 创建网络频道（TCP）
        var helper = new DefaultNetworkChannelHelper();
        m_Channel = m_NetworkModule.CreateNetworkChannel(
            "GameChannel",
            helper,
            rpcTimeout: 5000
        );

        // 注册消息处理器（使用 ProtoMessageHandler）
        ProtoMessageHandler.Add(new LoginMessageHandler());
        ProtoMessageHandler.Add(new GameMessageHandler());
    }

    public void Connect()
    {
        // TCP 连接
        m_Channel.Connect(new Uri("tcp://127.0.0.1:8888"));

        // WebSocket 连接
        // m_Channel.Connect(new Uri("ws://127.0.0.1:8888/ws"));
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
m_Channel.Send(moveNotify);

// RPC 调用（请求-响应模式）
public async Task<LoginResponse> LoginAsync(string username, string password)
{
    var request = new LoginRequest
    {
        Username = username,
        Password = password
    };

    try
    {
        var response = await m_Channel.Call<LoginResponse>(request);
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

```text
Network/
├── Runtime/
│   ├── NetworkModule.cs                              # 网络管理模块
│   ├── NetworkModule.ConnectState.cs                 # 连接状态
│   ├── NetworkModule.HeartBeatState.cs               # 心跳状态
│   ├── NetworkModule.NetworkChannelBase.cs           # 网络频道基类
│   ├── NetworkModule.ReceiveState.cs                 # 接收状态
│   ├── NetworkModule.SendState.cs                    # 发送状态
│   ├── NetworkModule.RpcState.cs                     # RPC 状态
│   ├── ProtoMessageHandler.cs                        # 消息处理器注册
│   ├── ProtoMessageIdHandler.cs                      # 消息 ID 映射
│   ├── Base/
│   │   ├── EAddressFamily.cs                         # 地址类型枚举
│   │   ├── EServiceType.cs                           # 服务类型枚举
│   │   ├── IRequestMessage.cs                        # 请求消息接口
│   │   ├── IResponseMessage.cs                       # 响应消息接口
│   │   ├── INotifyMessage.cs                         # 推送消息接口
│   │   ├── IHeartBeatMessage.cs                      # 心跳消息接口
│   │   ├── MessageHandlerAttribute.cs                # 消息处理器特性
│   │   ├── MessageTypeHandlerAttribute.cs            # 消息类型处理器特性
│   │   ├── MessageObject.cs                          # 消息基类
│   │   └── NetworkErrorCode.cs                       # 网络错误码
│   ├── Interface/
│   │   ├── INetworkChannel.cs                        # 网络频道接口
│   │   ├── INetworkChannelHelper.cs                  # 频道辅助器接口
│   │   ├── INetworkSocket.cs                         # 网络套接字接口
│   │   ├── IPacketSendHeaderHandler.cs               # 发送包头处理器接口
│   │   ├── IPacketReceiveHeaderHandler.cs            # 接收包头处理器接口
│   │   ├── IPacketSendBodyHandler.cs                 # 发送包体处理器接口
│   │   ├── IPacketReceiveBodyHandler.cs              # 接收包体处理器接口
│   │   ├── IPacketHeartBeatHandler.cs                # 心跳包处理器接口
│   │   ├── IMessageCompressHandler.cs                # 消息压缩接口
│   │   └── IMessageDecompressHandler.cs              # 消息解压接口
│   ├── Event/
│   │   ├── NetworkConnectedEventArgs.cs
│   │   ├── NetworkClosedEventArgs.cs
│   │   ├── NetworkErrorEventArgs.cs
│   │   └── NetworkMissHeartBeatEventArgs.cs
│   ├── Helper/                                       # 默认实现
│   ├── SystemSocket/                                 # TCP 套接字实现
│   ├── WebSocket/                                    # WebSocket 实现
└── README.md                                         # 本文档
```

## 7. 依赖

- **Hotfix.Framework.Core**：提供 ModuleBase 基类、数据结构、工具方法
- **Hotfix.Framework.Event**：事件系统
- **Hotfix.Framework.ReferencePools**：引用池
- **UniTask**：异步 RPC 支持

## 8. 最佳实践

1. **消息 ID 管理**：使用 Proto 文件统一定义消息 ID 和结构
2. **消息处理器注册**：在游戏启动流程中集中注册所有消息处理器
3. **RPC 超时处理**：所有 RPC 调用应设置合理的超时时间并处理超时异常
4. **断线重连**：监听 `NetworkClosedEventArgs` 事件，实现自动重连逻辑
5. **消息压缩**：大消息（>1KB）启用压缩，减少网络传输量

## 9. 注意事项

1. **主线程限制**：网络回调在主线程执行，消息处理器中避免阻塞操作
2. **消息序列化**：消息类必须有无参构造函数，且字段必须可序列化
3. **内存管理**：接收缓冲区大小需根据消息大小合理配置
4. **心跳间隔**：根据服务器配置设置合理的心跳间隔（通常 5-30 秒）
5. **重连策略**：避免无限重连，设置最大重连次数和指数退避
