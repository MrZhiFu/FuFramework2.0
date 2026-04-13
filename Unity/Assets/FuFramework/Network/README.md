# 1. FuFramework Network Module

## 1. 简介

FuFramework Network 模块是游戏框架的网络通信核心组件，提供了一套完整的网络通信解决方案。该模块支持 TCP 和 WebSocket 双协议，具备消息序列化、心跳检测、RPC 调用、连接管理等功能，采用模块化设计，易于扩展和定制。

## 2. 核心特性

- **多协议支持**：支持 TCP 和 WebSocket 协议，自动适配不同平台（WebGL 自动使用 WebSocket）
- **多频道管理**：支持多个网络频道同时运行，每个频道独立管理
- **消息处理机制**：完整的消息发送、接收、序列化、反序列化流程
- **RPC 支持**：支持请求-响应模式的远程过程调用，支持超时处理
- **心跳检测**：自动心跳检测，支持连接状态监控和自动断线检测
- **消息压缩**：支持消息压缩和解压缩，减少网络流量
- **事件驱动**：基于事件系统的连接状态通知
- **反射注册**：自动反射注册消息处理器，简化开发

## 3. 核心概念

### 3.1 网络架构

**运行时架构：**

```
┌─────────────────────────────────────────────────────────────┐
│                      NetworkModule                          │
│                    (继承 FuModule)                          │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │
│  │  Channel A  │  │  Channel B  │  │  Channel C  │  ...     │
│  │  (TCP/WS)   │  │  (TCP/WS)   │  │  (TCP/WS)   │         │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘         │
└─────────┼────────────────┼────────────────┼─────────────────┘
          │                │                │
          ▼                ▼                ▼
    ┌──────────┐     ┌──────────┐     ┌──────────┐
    │  Server  │     │  Server  │     │  Server  │
    └──────────┘     └──────────┘     └──────────┘
```

**类继承与实现体系：**

```
【类继承体系】

NetworkModule (网络管理模块)
    └── NetworkChannelBase (网络频道基类)
        ├── SystemTcpNetworkChannel (TCP实现)
        └── WebSocketNetworkChannel (WebSocket实现)

【接口实现体系】

NetworkChannelBase 实现:
    ├── INetworkChannel (网络频道接口)
    │   ├── 属性: Name, Connected, SendPacketCount, SentPacketCount, ReceivedPacketCount, HeartBeatInterval, MissHeartBeatCount, etc.
    │   └── 方法: Connect(), Close(), Shutdown(), Send<T>(), Call<TResult>(), RegisterHandler(), RegisterHeartBeatHandler(), RegisterMessageCompressHandler(), RegisterMessageDecompressHandler(), SetRPCStartHandler(), SetRPCEndHandler(), SetRPCErrorHandler(), SetRPCErrorCodeHandler()
    └── IDisposable (可释放接口)

INetworkChannelHelper (频道辅助器接口)
    └── DefaultNetworkChannelHelper (默认实现)
        ├── 方法: Initialize(), Shutdown(), PrepareForConnecting(), SendHeartBeat(), SerializePacketHeader<T>(), SerializePacketBody(), DeserializePacketHeader(), DeserializePacketBody()
        └── 特性: 反射自动注册各类处理器

INetworkSocket (网络Socket接口)
    ├── SystemNetSocket (TCP Socket实现)
    └── WebSocketNetSocket (WebSocket实现)

【包处理器接口体系】

IPacketHandler (包处理器基接口)
    ├── IPacketSendHeaderHandler (发送包头处理器)
    │   └── 方法: Handler<T>()
    ├── IPacketSendBodyHandler (发送包体处理器)
    │   └── 方法: Handler()
    ├── IPacketReceiveHeaderHandler (接收包头处理器)
    │   └── 方法: Handler()
    ├── IPacketReceiveBodyHandler (接收包体处理器)
    │   └── 方法: Handler()
    └── IPacketHeartBeatHandler (心跳包处理器)
        ├── 方法: Handler()
        └── 属性: HeartBeatInterval, MissHeartBeatCountByClose

IMessageCompressHandler (消息压缩处理器)
    └── 方法: Compress()

IMessageDecompressHandler (消息解压处理器)
    └── 方法: Decompress()

【消息处理器体系】

IMessageHandler (消息处理器接口)
    └── 通过 MessageHandlerAttribute 特性标记处理方法
        └── 方法签名: void OnMessage(T message)

【消息对象体系】

MessageObject (消息基类)
    ├── IRequestMessage (请求消息标记接口)
    ├── IResponseMessage (响应消息标记接口)
    │   └── 属性: ErrorCode
    ├── INotifyMessage (通知消息标记接口)
    └── IHeartBeatMessage (心跳消息标记接口)

【事件参数体系】

GameEventArgs (事件参数基类)
    ├── NetworkConnectedEventArgs (连接成功事件)
    ├── NetworkClosedEventArgs (连接关闭事件)
    ├── NetworkMissHeartBeatEventArgs (心跳丢失事件)
    └── NetworkErrorEventArgs (网络错误事件)
```

### 3.2 消息类型体系

```
MessageObject (基类)
    ├── IRequestMessage    # 请求消息（客户端→服务器）
    ├── IResponseMessage   # 响应消息（服务器→客户端）
    ├── INotifyMessage     # 通知消息（服务器主动推送）
    └── IHeartBeatMessage  # 心跳消息
```

### 3.3 消息处理流程

**发送流程：**
```
MessageObject → SerializePacketHeader → SerializePacketBody → Compress → Send
```

**接收流程：**
```
Receive → Decompress → DeserializePacketHeader → DeserializePacketBody → MessageObject → Handler
```

## 4. 核心类详细说明

### 4.1 NetworkModule

网络管理模块，继承自 `FuModule`，负责管理所有网络频道。

**核心功能：**

```csharp
public sealed partial class NetworkModule : FuModule
{
    // 网络频道管理
    public int NetworkChannelCount { get; }                          // 网络频道数量
    public bool HasNetworkChannel(string channelName)               // 检查频道是否存在
    public INetworkChannel GetNetworkChannel(string channelName)    // 获取指定频道
    public INetworkChannel[] GetAllNetworkChannels()               // 获取所有频道
    
    // 频道生命周期
    public INetworkChannel CreateNetworkChannel(string channelName, INetworkChannelHelper networkChannelHelper, int rpcTimeout = 5000)
    public bool DestroyNetworkChannel(string channelName)
}
```

**平台适配：**
- 通过条件编译自动选择 Socket 实现
- WebGL 平台自动使用 WebSocket
- 其他平台使用 System TCP Socket

### 4.2 NetworkChannelBase

网络频道基类，实现 `INetworkChannel` 和 `IDisposable` 接口。

**核心属性：**

```csharp
public abstract class NetworkChannelBase : INetworkChannel, IDisposable
{
    public string Name { get; }                    // 频道名称
    public bool Connected { get; }                 // 是否已连接
    public EAddressFamily EAddressFamily { get; }  // 地址族类型
    public int SendPacketCount { get; }            // 待发送消息数
    public int SentPacketCount { get; }            // 累计发送数
    public int ReceivedPacketCount { get; }        // 累计接收数
    public float HeartBeatInterval { get; set; }   // 心跳间隔（秒）
    public int MissHeartBeatCount { get; }         // 丢失心跳次数
}
```

**核心方法：**

```csharp
// 连接管理
public virtual void Connect(Uri address, object userData = null)  // 连接到服务器
public virtual void Close()                                        // 关闭连接
public virtual void Shutdown()                                     // 关闭并清理

// 消息发送
public void Send<T>(T messageObject) where T : MessageObject       // 发送消息
public Task<TResult> Call<TResult>(MessageObject messageObject)    // RPC 调用

// 处理器注册
public void RegisterHandler(IPacketSendHeaderHandler handler)      // 注册发送包头处理器
public void RegisterHandler(IPacketSendBodyHandler handler)        // 注册发送包体处理器
public void RegisterHandler(IPacketReceiveHeaderHandler handler)   // 注册接收包头处理器
public void RegisterHandler(IPacketReceiveBodyHandler handler)     // 注册接收包体处理器
public void RegisterHeartBeatHandler(IPacketHeartBeatHandler handler) // 注册心跳处理器
public void RegisterMessageCompressHandler(IMessageCompressHandler handler)   // 注册压缩处理器
public void RegisterMessageDecompressHandler(IMessageDecompressHandler handler) // 注册解压处理器

// RPC 事件设置
public void SetRPCStartHandler(EventHandler<MessageObject> handler)   // RPC 开始
public void SetRPCEndHandler(EventHandler<MessageObject> handler)     // RPC 结束
public void SetRPCErrorHandler(EventHandler<MessageObject> handler)   // RPC 错误
public void SetRPCErrorCodeHandler(EventHandler<MessageObject> handler) // RPC 错误码
```

**事件回调：**

```csharp
public Action<NetworkChannelBase, object> NetworkChannelConnected        // 连接成功
public Action<NetworkChannelBase> NetworkChannelClosed                   // 连接关闭
public Action<NetworkChannelBase, bool> NetworkChannelActiveChanged      // 激活状态变化
public Action<NetworkChannelBase, int> NetworkChannelMissHeartBeat       // 心跳丢失
public Action<NetworkChannelBase, NetworkErrorCode, SocketError, string> NetworkChannelError  // 网络错误
```

### 4.3 INetworkChannelHelper

网络频道辅助器接口，定义消息序列化和心跳处理。

```csharp
public interface INetworkChannelHelper
{
    void Initialize(INetworkChannel networkChannel);   // 初始化
    void Shutdown();                                    // 关闭清理
    void PrepareForConnecting();                        // 准备连接
    bool SendHeartBeat();                               // 发送心跳
    
    // 序列化
    bool SerializePacketHeader<T>(T messageObject, MemoryStream destination, out byte[] messageBodyBuffer) where T : MessageObject;
    bool SerializePacketBody(byte[] messageBodyBuffer, MemoryStream destination);
    
    // 反序列化
    bool DeserializePacketHeader(byte[] source);
    bool DeserializePacketBody(byte[] source, int messageId, out MessageObject messageObject);
}
```

### 4.4 DefaultNetworkChannelHelper

默认网络频道辅助器实现，提供反射自动注册功能。

**自动注册机制：**
- 扫描所有程序集类型
- 自动注册实现 `IPacketHandler` 接口的处理器
- 支持发送/接收包头、包体、心跳、压缩、解压处理器

### 4.5 MessageObject

消息基类，所有网络消息必须继承此类。

```csharp
[ProtoContract]
public class MessageObject
{
    [JsonIgnore]
    public int UniqueId { get; private set; }   // 消息唯一编号
    
    public void UpdateUniqueId()                 // 更新唯一编码
    public void SetUpdateUniqueId(int uniqueId)  // 设置唯一编码
    public override string ToString()            // JSON 序列化输出
}
```

### 4.6 ProtoMessageHandler

协议消息处理帮助类，管理消息处理器。

```csharp
public static class ProtoMessageHandler
{
    public static void Add(IMessageHandler messageHandler)      // 添加处理器
    public static void Remove(IMessageHandler messageHandler)   // 移除处理器
    internal static List<MessageHandlerAttribute> GetHandlers(Type messageType) // 获取处理器
}
```

### 4.7 RpcState

RPC 状态管理类，处理请求-响应模式的远程调用。

```csharp
public partial class RpcState : IDisposable
{
    public RpcState(int timeout)                                    // 构造函数（最小3000ms）
    public Task<IResponseMessage> Call(MessageObject messageObject) // 调用 RPC
    public bool TryReply(MessageObject message)                     // 处理响应
    public void Update(float deltaTime, float unscaledDeltaTime)    // 更新超时处理
    
    // 事件设置
    public void SetRPCStartHandler(EventHandler<MessageObject> handler)
    public void SetRPCEndHandler(EventHandler<MessageObject> handler)
    public void SetRPCErrorHandler(EventHandler<MessageObject> handler)
    public void SetRPCErrorCodeHandler(EventHandler<MessageObject> handler)
}
```

### 4.8 消息类型接口

```csharp
// 请求消息标记接口
public interface IRequestMessage { }

// 响应消息标记接口
public interface IResponseMessage
{
    int ErrorCode { get; }   // 错误码，非0表示错误
}

// 通知消息标记接口
public interface INotifyMessage { }

// 心跳消息标记接口
public interface IHeartBeatMessage { }
```

### 4.9 网络事件

```csharp
// 网络连接成功事件
public sealed class NetworkConnectedEventArgs : GameEventArgs
{
    public static readonly string EventId;
    public INetworkChannel NetworkChannel { get; }
    public object UserData { get; }
}

// 网络连接关闭事件
public sealed class NetworkClosedEventArgs : GameEventArgs
{
    public static readonly string EventId;
    public INetworkChannel NetworkChannel { get; }
}

// 心跳丢失事件
public sealed class NetworkMissHeartBeatEventArgs : GameEventArgs
{
    public static readonly string EventId;
    public INetworkChannel NetworkChannel { get; }
    public int MissCount { get; }
}

// 网络错误事件
public sealed class NetworkErrorEventArgs : GameEventArgs
{
    public static readonly string EventId;
    public INetworkChannel NetworkChannel { get; }
    public NetworkErrorCode ErrorCode { get; }
    public SocketError SocketErrorCode { get; }
    public string ErrorMessage { get; }
}
```

### 4.10 NetworkErrorCode

网络错误码枚举：

```csharp
public enum NetworkErrorCode : byte
{
    Unknown = 0,                    // 未知错误
    AddressFamilyError,             // 地址族错误
    SocketError,                    // Socket 错误
    ConnectError,                   // 连接错误
    SendError,                      // 发送错误
    ReceiveError,                   // 接收错误
    SerializeError,                 // 序列化错误
    DeserializePacketHeaderError,   // 反序列化包头错误
    DeserializePacketError          // 反序列化包错误
}
```

## 5. 使用示例

### 5.1 基础网络连接

```csharp
using FuFramework.Network.Runtime;
using UnityEngine;

public class NetworkExample : MonoBehaviour
{
    private INetworkChannel m_Channel;
    
    private void Start()
    {
        // 获取网络模块
        var networkModule = GlobalModule.NetworkModule;
        
        // 创建网络频道辅助器
        var channelHelper = new DefaultNetworkChannelHelper();
        
        // 创建网络频道（RPC超时5秒）
        m_Channel = networkModule.CreateNetworkChannel("MainChannel", channelHelper, 5000);
        
        // 订阅网络事件
        var eventModule = GlobalModule.EventModule;
        eventModule.Subscribe(NetworkConnectedEventArgs.EventId, OnNetworkConnected);
        eventModule.Subscribe(NetworkClosedEventArgs.EventId, OnNetworkClosed);
        eventModule.Subscribe(NetworkErrorEventArgs.EventId, OnNetworkError);
        
        // 连接到服务器
        m_Channel.Connect(new Uri("tcp://127.0.0.1:8080"));
    }
    
    private void OnNetworkConnected(object sender, GameEventArgs e)
    {
        if (e is NetworkConnectedEventArgs args && args.NetworkChannel == m_Channel)
        {
            Debug.Log("网络连接成功！");
        }
    }
    
    private void OnNetworkClosed(object sender, GameEventArgs e)
    {
        if (e is NetworkClosedEventArgs args && args.NetworkChannel == m_Channel)
        {
            Debug.Log("网络连接关闭！");
        }
    }
    
    private void OnNetworkError(object sender, GameEventArgs e)
    {
        if (e is NetworkErrorEventArgs args && args.NetworkChannel == m_Channel)
        {
            Debug.LogError($"网络错误: {args.ErrorCode}, {args.ErrorMessage}");
        }
    }
    
    private void OnDestroy()
    {
        // 取消订阅事件
        var eventModule = GlobalModule.EventModule;
        eventModule.Unsubscribe(NetworkConnectedEventArgs.EventId, OnNetworkConnected);
        eventModule.Unsubscribe(NetworkClosedEventArgs.EventId, OnNetworkClosed);
        eventModule.Unsubscribe(NetworkErrorEventArgs.EventId, OnNetworkError);
        
        // 销毁网络频道
        GlobalModule.NetworkModule.DestroyNetworkChannel("MainChannel");
    }
}
```

### 5.2 定义消息和处理器

```csharp
using FuFramework.Network.Runtime;
using ProtoBuf;

// 定义请求消息
[ProtoContract]
public class LoginRequest : MessageObject, IRequestMessage
{
    [ProtoMember(1)]
    public string Username { get; set; }
    
    [ProtoMember(2)]
    public string Password { get; set; }
}

// 定义响应消息
[ProtoContract]
public class LoginResponse : MessageObject, IResponseMessage
{
    [ProtoMember(1)]
    public int ErrorCode { get; set; }
    
    [ProtoMember(2)]
    public string Token { get; set; }
    
    [ProtoMember(3)]
    public long UserId { get; set; }
}

// 定义通知消息
[ProtoContract]
public class PlayerJoinNotify : MessageObject, INotifyMessage
{
    [ProtoMember(1)]
    public long PlayerId { get; set; }
    
    [ProtoMember(2)]
    public string PlayerName { get; set; }
}

// 定义消息处理器
public class GameMessageHandler : IMessageHandler
{
    // 处理登录响应
    [MessageHandler(typeof(LoginResponse))]
    private void OnLoginResponse(LoginResponse response)
    {
        if (response.ErrorCode == 0)
        {
            Debug.Log($"登录成功，Token: {response.Token}, UserId: {response.UserId}");
        }
        else
        {
            Debug.LogError($"登录失败，错误码: {response.ErrorCode}");
        }
    }
    
    // 处理玩家加入通知
    [MessageHandler(typeof(PlayerJoinNotify))]
    private void OnPlayerJoin(PlayerJoinNotify notify)
    {
        Debug.Log($"玩家加入: {notify.PlayerName} (ID: {notify.PlayerId})");
    }
}
```

### 5.3 RPC 调用

```csharp
using FuFramework.Network.Runtime;
using System.Threading.Tasks;
using UnityEngine;

public class RpcExample : MonoBehaviour
{
    private INetworkChannel m_Channel;
    
    private async void Start()
    {
        m_Channel = GlobalModule.NetworkModule.GetNetworkChannel("MainChannel");
        
        // 设置 RPC 事件
        m_Channel.SetRPCStartHandler((sender, msg) => Debug.Log($"RPC 开始: {msg.GetType().Name}"));
        m_Channel.SetRPCEndHandler((sender, msg) => Debug.Log($"RPC 结束: {msg.GetType().Name}"));
        m_Channel.SetRPCErrorHandler((sender, msg) => Debug.LogError($"RPC 错误: {msg.GetType().Name}"));
        
        // 执行 RPC 调用
        await GetUserInfoAsync(10001);
    }
    
    private async Task GetUserInfoAsync(long userId)
    {
        try
        {
            var request = new GetUserInfoRequest { UserId = userId };
            var response = await m_Channel.Call<GetUserInfoResponse>(request);
            
            if (response.ErrorCode == 0)
            {
                Debug.Log($"用户信息: {response.UserName}, 等级: {response.Level}");
            }
            else
            {
                Debug.LogError($"获取用户信息失败: {response.ErrorCode}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"RPC 调用异常: {ex.Message}");
        }
    }
}
```

### 5.4 心跳配置

```csharp
using FuFramework.Network.Runtime;
using UnityEngine;

public class HeartBeatExample : MonoBehaviour
{
    private void Start()
    {
        var channel = GlobalModule.NetworkModule.GetNetworkChannel("MainChannel");
        
        // 配置心跳
        channel.HeartBeatInterval = 10f;  // 心跳间隔10秒
        channel.ResetHeartBeatElapseSecondsWhenReceivePacket = true;  // 收到消息重置心跳计时
        
        // 订阅心跳丢失事件
        GlobalModule.EventModule.Subscribe(NetworkMissHeartBeatEventArgs.EventId, OnMissHeartBeat);
    }
    
    private void OnMissHeartBeat(object sender, GameEventArgs e)
    {
        if (e is NetworkMissHeartBeatEventArgs args)
        {
            Debug.LogWarning($"心跳丢失 {args.MissCount} 次");
            
            // 连续丢失多次可以主动断开重连
            if (args.MissCount >= 3)
            {
                args.NetworkChannel.Close();
            }
        }
    }
}
```

### 5.5 多频道管理

```csharp
using FuFramework.Network.Runtime;
using UnityEngine;

public class MultiChannelExample : MonoBehaviour
{
    private INetworkChannel m_GameChannel;
    private INetworkChannel m_ChatChannel;
    
    private void Start()
    {
        var networkModule = GlobalModule.NetworkModule;
        
        // 创建游戏频道
        m_GameChannel = networkModule.CreateNetworkChannel(
            "GameChannel", 
            new DefaultNetworkChannelHelper(),
            5000);
        
        // 创建聊天频道
        m_ChatChannel = networkModule.CreateNetworkChannel(
            "ChatChannel", 
            new DefaultNetworkChannelHelper(),
            3000);
        
        // 分别连接
        m_GameChannel.Connect(new Uri("tcp://game.server.com:8080"));
        m_ChatChannel.Connect(new Uri("tcp://chat.server.com:8081"));
        
        Debug.Log($"当前频道数量: {networkModule.NetworkChannelCount}");
    }
    
    // 发送游戏消息
    public void SendGameMessage(MessageObject message)
    {
        m_GameChannel?.Send(message);
    }
    
    // 发送聊天消息
    public void SendChatMessage(MessageObject message)
    {
        m_ChatChannel?.Send(message);
    }
    
    private void OnDestroy()
    {
        // 销毁所有频道
        var networkModule = GlobalModule.NetworkModule;
        networkModule.DestroyNetworkChannel("GameChannel");
        networkModule.DestroyNetworkChannel("ChatChannel");
    }
}
```

## 6. 目录结构

```
FuFramework/Network/
├── Runtime/
│   ├── FuFramework.Network.Runtime.asmdef    # 运行时程序集定义
│   ├── NetworkModule.cs                      # 网络管理模块主类
│   ├── NetworkModule.NetworkChannelBase.cs   # 网络频道基类
│   ├── NetworkModule.RpcState.cs             # RPC 状态管理
│   ├── NetworkModule.ConnectState.cs         # 连接状态
│   ├── NetworkModule.SendState.cs            # 发送状态
│   ├── NetworkModule.ReceiveState.cs         # 接收状态
│   ├── NetworkModule.HeartBeatState.cs       # 心跳状态
│   ├── ProtoMessageHandler.cs                # 消息处理器管理
│   ├── ProtoMessageIdHandler.cs              # 消息ID处理器
│   ├── Base/                                 # 基础定义
│   │   ├── EAddressFamily.cs                 # 地址族枚举
│   │   ├── EServiceType.cs                   # 服务类型枚举
│   │   ├── MessageObject.cs                  # 消息基类
│   │   ├── MessageHttpObject.cs              # HTTP消息对象
│   │   ├── IRequestMessage.cs                # 请求消息接口
│   │   ├── IResponseMessage.cs               # 响应消息接口
│   │   ├── INotifyMessage.cs                 # 通知消息接口
│   │   ├── IHeartBeatMessage.cs              # 心跳消息接口
│   │   ├── MessageHandlerAttribute.cs        # 消息处理器特性
│   │   ├── MessageTypeHandlerAttribute.cs    # 消息类型处理器特性
│   │   └── NetworkErrorCode.cs               # 网络错误码
│   ├── Interface/                            # 接口定义
│   │   ├── INetworkChannel.cs                # 网络频道接口
│   │   ├── INetworkChannelHelper.cs          # 频道辅助器接口
│   │   ├── INetworkSocket.cs                 # 网络Socket接口
│   │   ├── IMessageHandler.cs                # 消息处理器接口
│   │   ├── IMessageCompressHandler.cs        # 消息压缩接口
│   │   ├── IMessageDecompressHandler.cs      # 消息解压接口
│   │   ├── IPacketHandler.cs                 # 包处理器接口基类
│   │   ├── IPacketHeartBeatHandler.cs        # 心跳包处理器接口
│   │   ├── IPacketSendHeaderHandler.cs       # 发送包头处理器接口
│   │   ├── IPacketSendBodyHandler.cs         # 发送包体处理器接口
│   │   ├── IPacketReceiveHeaderHandler.cs    # 接收包头处理器接口
│   │   └── IPacketReceiveBodyHandler.cs      # 接收包体处理器接口
│   ├── Helper/                               # 默认实现
│   │   ├── DefaultNetworkChannelHelper.cs    # 默认频道辅助器
│   │   ├── DefaultPacketHeartBeatHandler.cs  # 默认心跳处理器
│   │   ├── DefaultPacketSendHeaderHandler.cs # 默认发送包头处理器
│   │   ├── DefaultPacketSendBodyHandler.cs   # 默认发送包体处理器
│   │   ├── DefaultPacketReceiveHeaderHandler.cs  # 默认接收包头处理器
│   │   ├── DefaultPacketReceiveBodyHandler.cs    # 默认接收包体处理器
│   │   ├── DefaultMessageCompressHandler.cs  # 默认消息压缩处理器
│   │   └── DefaultMessageDecompressHandler.cs # 默认消息解压处理器
│   ├── Event/                                # 网络事件
│   │   ├── NetworkConnectedEventArgs.cs      # 连接成功事件
│   │   ├── NetworkClosedEventArgs.cs         # 连接关闭事件
│   │   ├── NetworkMissHeartBeatEventArgs.cs  # 心跳丢失事件
│   │   └── NetworkErrorEventArgs.cs          # 网络错误事件
│   ├── SystemSocket/                         # TCP Socket实现
│   │   ├── NetworkManager.SystemNetSocket.cs # 系统Socket实现
│   │   └── NetworkManager.SystemTcpNetworkChannel.cs # TCP频道
│   └── WebSocket/                            # WebSocket实现
│       ├── NetworkManager.WebSocketNetSocket.cs # WebSocket Socket
│       └── NetworkManager.WebSocketNetworkChannel.cs # WebSocket频道
├── Editor/
│   ├── FuFramework.Network.Editor.asmdef     # 编辑器程序集定义
│   ├── Inspector/
│   │   └── NetworkModuleInspector.cs         # 网络模块检视器
│   └── NetworkLogScriptingDefineSymbols.cs   # 网络日志符号定义
└── README.md                                 # 模块文档
```

## 7. 依赖

- **FuFramework.Core**：框架核心模块（FuModule、FuGuard、Utility、FuLogger）
- **FuFramework.Event**：事件管理模块（EventModule、GameEventArgs）
- **FuFramework.ReferencePool**：对象池模块
- **protobuf-net**：ProtoBuf 序列化库
- **Newtonsoft.Json**：JSON 序列化库

## 8. 最佳实践

### 8.1 消息定义规范

```csharp
// 消息ID使用特性标记
[ProtoContract]
[MessageId(1001)]  // 定义消息ID
public class LoginRequest : MessageObject, IRequestMessage
{
    [ProtoMember(1)]
    public string Username { get; set; }
    
    [ProtoMember(2)]
    public string Password { get; set; }
}

// 响应消息必须实现 IResponseMessage
[ProtoContract]
[MessageId(1002)]
public class LoginResponse : MessageObject, IResponseMessage
{
    [ProtoMember(1)]
    public int ErrorCode { get; set; }   // 必须，0表示成功
    
    [ProtoMember(2)]
    public string Token { get; set; }
}
```

### 8.2 网络管理器封装

```csharp
public static class NetworkManager
{
    private static INetworkChannel s_MainChannel;
    
    public static void Initialize()
    {
        var networkModule = GlobalModule.NetworkModule;
        var helper = new DefaultNetworkChannelHelper();
        s_MainChannel = networkModule.CreateNetworkChannel("Main", helper, 5000);
        
        // 订阅事件
        GlobalModule.EventModule.Subscribe(NetworkConnectedEventArgs.EventId, OnConnected);
        GlobalModule.EventModule.Subscribe(NetworkClosedEventArgs.EventId, OnClosed);
        GlobalModule.EventModule.Subscribe(NetworkErrorEventArgs.EventId, OnError);
    }
    
    public static void Connect(string host, int port)
    {
        s_MainChannel?.Connect(new Uri($"tcp://{host}:{port}"));
    }
    
    public static void Send<T>(T message) where T : MessageObject
    {
        s_MainChannel?.Send(message);
    }
    
    public static Task<TResponse> Call<TResponse>(MessageObject request) where TResponse : MessageObject, IResponseMessage
    {
        return s_MainChannel?.Call<TResponse>(request);
    }
    
    private static void OnConnected(object sender, GameEventArgs e) { /* ... */ }
    private static void OnClosed(object sender, GameEventArgs e) { /* ... */ }
    private static void OnError(object sender, GameEventArgs e) { /* ... */ }
}
```

### 8.3 重连机制

```csharp
public class NetworkReconnectHelper
{
    private INetworkChannel m_Channel;
    private Uri m_ServerUri;
    private int m_RetryCount;
    private const int MAX_RETRY = 5;
    
    public async void StartReconnect(INetworkChannel channel, Uri serverUri)
    {
        m_Channel = channel;
        m_ServerUri = serverUri;
        m_RetryCount = 0;
        
        while (m_RetryCount < MAX_RETRY)
        {
            if (m_Channel.Connected) break;
            
            try
            {
                m_Channel.Connect(m_ServerUri);
                await Task.Delay(2000);
                
                if (m_Channel.Connected)
                {
                    Debug.Log("重连成功");
                    m_RetryCount = 0;
                    break;
                }
            }
            catch (Exception ex)
            {
                m_RetryCount++;
                Debug.LogWarning($"第 {m_RetryCount} 次重连失败: {ex.Message}");
                await Task.Delay(3000 * m_RetryCount);
            }
        }
    }
}
```

## 9. 注意事项

1. **RPC 超时**
   - 最小超时时间为 3000 毫秒
   - 超时后会触发 RPCErrorHandler

2. **线程安全**
   - 消息发送是线程安全的
   - 消息处理在主线程执行
   - 网络事件在主线程广播

3. **消息处理器**
   - 使用 `ProtoMessageHandler.Add()` 注册处理器
   - 使用 `ProtoMessageHandler.Remove()` 移除处理器
   - 支持多个处理器处理同一消息类型

4. **WebGL 平台**
   - 自动使用 WebSocket 实现
   - 不支持 TCP Socket
   - 需要配置 WebSocket 服务器

5. **心跳机制**
   - 默认心跳间隔 30 秒
   - 默认丢失 10 次心跳后断开
   - 可通过处理器自定义心跳消息

6. **资源释放**
   - 使用 `DestroyNetworkChannel()` 销毁频道
   - 频道销毁时会自动清理资源
   - 记得取消订阅网络事件
