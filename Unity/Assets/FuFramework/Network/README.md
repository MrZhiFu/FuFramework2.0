# FuFramework Network Module

## 概述

Network 模块是 FuFramework 的网络通信核心组件，提供了一套完整的网络通信解决方案，支持 TCP 和 WebSocket 协议，具备消息序列化、心跳检测、连接管理等功能。

### 核心特性

- **多协议支持**：支持 TCP 和 WebSocket 协议，自动适配不同平台
- **消息处理机制**：完整的消息发送、接收、序列化、反序列化流程
- **心跳检测**：自动心跳检测，支持连接状态监控
- **多频道管理**：支持多个网络频道同时运行
- **事件驱动**：基于事件系统的连接状态通知
- **RPC 支持**：支持请求-响应模式的远程过程调用
- **消息压缩**：支持消息压缩和解压缩
- **错误处理**：完善的错误处理和重连机制

## 核心类说明

### NetworkManager

网络管理器，负责管理所有网络频道和网络通信。

```csharp
public sealed partial class NetworkManager : FuModule
```

**主要功能：**
- 网络频道的创建、销毁和管理
- 网络事件的分发和处理
- 网络通信的统一调度

### INetworkChannel

网络频道接口，定义网络通信的基本操作。

```csharp
public interface INetworkChannel
```

**主要属性：**
- `string Name` - 网络频道名称
- `bool Connected` - 是否已连接
- `int SendPacketCount` - 要发送的消息包数量
- `int SentPacketCount` - 累计发送的消息包数量
- `int ReceivedPacketCount` - 累计已接收的消息包数量

### MessageObject

消息对象基类，所有网络消息都需要继承此类。

```csharp
[ProtoContract]
public class MessageObject
```

**特性：**
- 支持 ProtoBuf 序列化
- 自动生成唯一消息ID
- JSON 序列化支持

### 消息类型接口

- `IRequestMessage` - 客户端发送给服务器的消息基类接口
- `IResponseMessage` - 服务器返回给客户端的消息基类接口
- `INotifyMessage` - 通知消息接口
- `IHeartBeatMessage` - 心跳消息接口

## 技术架构

### 依赖关系

```
NetworkManager → EventManager
NetworkChannelBase → INetworkSocket
MessageHandler → EventSystem
```

### 消息处理流程

1. **消息发送流程：**
   - 创建消息对象 → 序列化 → 压缩 → 添加包头 → 发送

2. **消息接收流程：**
   - 接收数据 → 解析包头 → 解压缩 → 反序列化 → 消息分发

3. **心跳检测流程：**
   - 定时发送心跳包 → 检测响应 → 统计丢失次数 → 触发重连

### 双平台支持

- **TCP Socket**：适用于 iOS、Android、PC 平台
- **WebSocket**：适用于 WebGL 平台

## 使用指南

### 1. 基础使用

#### 创建网络频道

```csharp
// 获取网络管理器
var networkManager = ModuleManager.GetModule<NetworkManager>();

// 创建网络频道辅助器
var channelHelper = new DefaultNetworkChannelHelper();

// 创建网络频道
var channel = networkManager.CreateNetworkChannel("MainChannel", channelHelper);
```

#### 连接到服务器

```csharp
// 连接到服务器
var uri = new Uri("tcp://127.0.0.1:8080");
channel.Connect(uri);

// 监听连接事件
EventManager.Get().Subscribe<NetworkConnectedEventArgs>((sender, args) =>
{
    if (args.NetworkChannel.Name == "MainChannel")
    {
        Debug.Log("连接成功");
    }
});
```

#### 发送消息

```csharp
// 定义请求消息
[ProtoContract]
public class LoginRequest : MessageObject, IRequestMessage
{
    [ProtoMember(1)]
    public string Username { get; set; }
    
    [ProtoMember(2)]
    public string Password { get; set; }
}

// 发送消息
var loginRequest = new LoginRequest { Username = "user", Password = "pass" };
channel.Send(loginRequest);
```

#### 接收消息

```csharp
// 定义响应消息
[ProtoContract]
public class LoginResponse : MessageObject, IResponseMessage
{
    [ProtoMember(1)]
    public int ErrorCode { get; set; }
    
    [ProtoMember(2)]
    public string Token { get; set; }
}

// 注册消息处理器
[MessageHandler(typeof(LoginResponse), nameof(OnLoginResponse))]
private void OnLoginResponse(LoginResponse response)
{
    if (response.ErrorCode == 0)
    {
        Debug.Log($"登录成功，Token: {response.Token}");
    }
    else
    {
        Debug.LogError($"登录失败，错误码: {response.ErrorCode}");
    }
}
```

### 2. RPC 调用

#### 同步 RPC 调用

```csharp
// 定义 RPC 请求和响应
[ProtoContract]
public class GetUserInfoRequest : MessageObject, IRequestMessage
{
    [ProtoMember(1)]
    public int UserId { get; set; }
}

[ProtoContract]
public class GetUserInfoResponse : MessageObject, IResponseMessage
{
    [ProtoMember(1)]
    public int ErrorCode { get; set; }
    
    [ProtoMember(2)]
    public string UserName { get; set; }
    
    [ProtoMember(3)]
    public int Level { get; set; }
}

// RPC 调用
async void GetUserInfoAsync(int userId)
{
    var request = new GetUserInfoRequest { UserId = userId };
    
    try
    {
        var response = await channel.Call<GetUserInfoResponse>(request);
        
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
```

### 3. 事件监听

#### 连接状态事件

```csharp
// 订阅网络事件
var eventManager = ModuleManager.GetModule<EventManager>();

// 连接成功事件
eventManager.Subscribe<NetworkConnectedEventArgs>((sender, args) =>
{
    Debug.Log($"网络频道 {args.NetworkChannel.Name} 连接成功");
});

// 连接关闭事件
eventManager.Subscribe<NetworkClosedEventArgs>((sender, args) =>
{
    Debug.Log($"网络频道 {args.NetworkChannel.Name} 连接关闭");
});

// 心跳丢失事件
eventManager.Subscribe<NetworkMissHeartBeatEventArgs>((sender, args) =>
{
    Debug.LogWarning($"网络频道 {args.NetworkChannel.Name} 丢失心跳: {args.MissHeartBeatCount}");
});

// 网络错误事件
eventManager.Subscribe<NetworkErrorEventArgs>((sender, args) =>
{
    Debug.LogError($"网络频道 {args.NetworkChannel.Name} 错误: {args.ErrorCode}, {args.ErrorMessage}");
});
```

### 4. 高级配置

#### 自定义消息处理器

```csharp
// 自定义消息压缩处理器
public class CustomMessageCompressHandler : IMessageCompressHandler
{
    public byte[] Compress(byte[] bytes)
    {
        // 实现自定义压缩逻辑
        return CustomCompress(bytes);
    }
}

// 注册自定义处理器
channel.RegisterMessageCompressHandler(new CustomMessageCompressHandler());
```

#### 心跳配置

```csharp
// 设置心跳间隔（秒）
channel.HeartBeatInterval = 10f;

// 设置收到消息时重置心跳计时
channel.ResetHeartBeatElapseSecondsWhenReceivePacket = true;
```

#### 忽略特定消息日志

```csharp
// 忽略特定消息ID的日志输出
var sendIgnoreIds = new List<int> { 1001, 1002 };
var receiveIgnoreIds = new List<int> { 2001, 2002 };
channel.SetIgnoreLogNetworkIds(sendIgnoreIds, receiveIgnoreIds);
```

## 高级用法

### 1. 多频道管理

```csharp
// 创建多个网络频道
var mainChannel = networkManager.CreateNetworkChannel("Main", mainHelper);
var battleChannel = networkManager.CreateNetworkChannel("Battle", battleHelper);

// 分别连接到不同服务器
mainChannel.Connect(new Uri("tcp://game.example.com:8080"));
battleChannel.Connect(new Uri("tcp://battle.example.com:8081"));

// 分别处理不同频道的消息
[MessageHandler(typeof(MainServerResponse), nameof(OnMainResponse))]
private void OnMainResponse(MainServerResponse response)
{
    // 处理主服务器消息
}

[MessageHandler(typeof(BattleServerResponse), nameof(OnBattleResponse))]
private void OnBattleResponse(BattleServerResponse response)
{
    // 处理战斗服务器消息
}
```

### 2. 断线重连机制

```csharp
private async void AutoReconnect(INetworkChannel channel, Uri serverUri)
{
    int retryCount = 0;
    const int maxRetryCount = 5;
    
    while (retryCount < maxRetryCount)
    {
        try
        {
            if (!channel.Connected)
            {
                channel.Connect(serverUri);
                await Task.Delay(2000); // 等待连接建立
                
                if (channel.Connected)
                {
                    Debug.Log("重连成功");
                    retryCount = 0;
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            retryCount++;
            Debug.LogWarning($"第 {retryCount} 次重连失败: {ex.Message}");
            await Task.Delay(3000 * retryCount); // 指数退避
        }
    }
    
    if (retryCount >= maxRetryCount)
    {
        Debug.LogError("重连失败，请检查网络连接");
    }
}
```

### 3. 消息流量控制

```csharp
// 监控网络流量
void MonitorNetworkTraffic(INetworkChannel channel)
{
    // 获取发送和接收统计
    var sentCount = channel.SentPacketCount;
    var receivedCount = channel.ReceivedPacketCount;
    
    // 计算网络延迟（基于心跳响应）
    var heartBeatElapse = channel.HeartBeatElapseSeconds;
    
    Debug.Log($"网络统计: 发送 {sentCount}, 接收 {receivedCount}, 心跳延迟 {heartBeatElapse:F2}s");
}

// 限制发送频率
private float lastSendTime = 0f;
private const float minSendInterval = 0.1f; // 最小发送间隔100ms

void ThrottledSend<T>(INetworkChannel channel, T message) where T : MessageObject
{
    var currentTime = Time.time;
    if (currentTime - lastSendTime >= minSendInterval)
    {
        channel.Send(message);
        lastSendTime = currentTime;
    }
    else
    {
        // 延迟发送或丢弃消息
        Debug.LogWarning("发送频率过高，消息被延迟");
    }
}
```

### 4. 自定义协议处理

```csharp
// 自定义包头处理器
public class CustomPacketHeaderHandler : IPacketSendHeaderHandler, IPacketReceiveHeaderHandler
{
    public byte[] SerializeHeader(MessageObject messageObject)
    {
        // 自定义包头格式
        var header = new byte[8];
        // 实现自定义序列化逻辑
        return header;
    }
    
    public MessageObject DeserializeHeader(byte[] headerData)
    {
        // 自定义包头解析逻辑
        return ParseCustomHeader(headerData);
    }
}

// 注册自定义协议处理器
channel.RegisterHandler(new CustomPacketHeaderHandler());
```

## 性能优化建议

### 1. 消息对象池

```csharp
// 使用对象池管理消息对象
public class MessageObjectPool
{
    private readonly Dictionary<Type, Queue<MessageObject>> m_Pools = new();
    
    public T Get<T>() where T : MessageObject, new()
    {
        var type = typeof(T);
        if (!m_Pools.TryGetValue(type, out var queue) || queue.Count == 0)
        {
            return new T();
        }
        
        var message = queue.Dequeue() as T;
        message.UpdateUniqueId(); // 更新唯一ID
        return message;
    }
    
    public void Return(MessageObject message)
    {
        var type = message.GetType();
        if (!m_Pools.TryGetValue(type, out var queue))
        {
            queue = new Queue<MessageObject>();
            m_Pools[type] = queue;
        }
        
        queue.Enqueue(message);
    }
}
```

### 2. 批量消息处理

```csharp
// 批量处理消息，减少GC压力
public class BatchMessageProcessor
{
    private readonly List<MessageObject> m_PendingMessages = new();
    
    public void AddMessage(MessageObject message)
    {
        m_PendingMessages.Add(message);
    }
    
    public void ProcessBatch(INetworkChannel channel)
    {
        if (m_PendingMessages.Count == 0) return;
        
        // 批量发送消息
        foreach (var message in m_PendingMessages)
        {
            channel.Send(message);
        }
        
        m_PendingMessages.Clear();
    }
}
```

### 3. 连接池管理

```csharp
// 连接池管理多个网络连接
public class ConnectionPool
{
    private readonly Queue<INetworkChannel> m_AvailableChannels = new();
    private readonly List<INetworkChannel> m_AllChannels = new();
    
    public INetworkChannel GetChannel()
    {
        if (m_AvailableChannels.Count > 0)
        {
            return m_AvailableChannels.Dequeue();
        }
        
        // 创建新连接
        var newChannel = CreateNewChannel();
        m_AllChannels.Add(newChannel);
        return newChannel;
    }
    
    public void ReturnChannel(INetworkChannel channel)
    {
        if (channel.Connected)
        {
            m_AvailableChannels.Enqueue(channel);
        }
    }
}
```

## 注意事项

### 1. 线程安全

- 网络操作主要在 Unity 主线程执行
- 消息处理回调在主线程触发
- 多线程访问需要适当的同步机制

### 2. 内存管理

- 及时释放不再使用的网络频道
- 注意消息对象的生命周期管理
- 避免在消息处理中创建大量临时对象

### 3. 错误处理

- 始终处理网络异常
- 实现适当的重连机制
- 监控网络状态变化

### 4. 平台差异

- WebGL 平台使用 WebSocket
- 其他平台使用 TCP Socket
- 注意不同平台的网络限制

## API 参考

### NetworkManager 主要方法

| 方法 | 说明 |
|------|------|
| `CreateNetworkChannel` | 创建网络频道 |
| `DestroyNetworkChannel` | 销毁网络频道 |
| `GetNetworkChannel` | 获取指定网络频道 |
| `GetAllNetworkChannels` | 获取所有网络频道 |
| `HasNetworkChannel` | 检查网络频道是否存在 |

### INetworkChannel 主要方法

| 方法 | 说明 |
|------|------|
| `Connect` | 连接到远程主机 |
| `Close` | 关闭网络连接 |
| `Send` | 发送消息 |
| `Call` | RPC 调用 |
| `RegisterHandler` | 注册消息处理器 |
| `SetIgnoreLogNetworkIds` | 设置忽略日志的消息ID |

### 事件类型

| 事件类型 | 说明 |
|----------|------|
| `NetworkConnectedEventArgs` | 网络连接成功事件 |
| `NetworkClosedEventArgs` | 网络连接关闭事件 |
| `NetworkMissHeartBeatEventArgs` | 心跳丢失事件 |
| `NetworkErrorEventArgs` | 网络错误事件 |

## 示例项目

参考 FuFramework 示例项目中的网络通信示例，了解完整的使用场景和最佳实践。

---

**注意：** 本模块需要依赖 Event 模块进行事件分发，请确保 Event 模块已正确初始化。