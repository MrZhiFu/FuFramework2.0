# FuFramework WebSocket 模块

## 概述

WebSocket 模块是 FuFramework 中的实时通信系统，提供基于 WebSocket 协议的双向通信功能。该模块支持跨平台开发，包括 WebGL 和非 WebGL 环境，实现了完整的 WebSocket 连接管理、消息收发和事件处理机制。

### 核心特性

- **跨平台支持**：支持 WebGL 和非 WebGL 环境
- **完整协议支持**：实现 WebSocket 标准协议
- **异步操作**：所有连接和消息操作都是异步的
- **事件驱动**：基于事件回调的消息处理机制
- **连接状态管理**：完整的连接状态跟踪和错误处理
- **子协议支持**：支持 WebSocket 子协议
- **自动重连**：内置连接断开自动重连机制

## 系统架构

### 核心类说明

#### 1. IWebSocket (WebSocket 接口)
位于 `Runtime/Core/IWebSocket.cs`，定义了 WebSocket 的核心接口。

**主要方法：**
- `ConnectAsync()` - 异步建立连接
- `CloseAsync()` - 异步关闭连接
- `SendAsync(byte[] data)` - 异步发送二进制数据
- `SendAsync(string text)` - 异步发送文本数据

**主要属性：**
- `bool IsConnected` - 是否已连接
- `WebSocketState ReadyState` - 连接状态

**事件：**
- `OnOpen` - 连接打开事件
- `OnClose` - 连接关闭事件
- `OnError` - 错误事件
- `OnMessage` - 消息接收事件

#### 2. WebSocket (WebSocket 实现类)
位于 `Runtime/Implementation/NoWebGL/WebSocket.cs` 和 `Runtime/Implementation/WebGL/WebSocket.cs`，提供不同平台的 WebSocket 实现。

**主要功能：**
- 管理 WebSocket 连接生命周期
- 处理消息发送和接收
- 管理连接状态
- 处理事件分发

#### 3. WebSocketManager (WebSocket 管理器)
位于 `Runtime/Implementation/NoWebGL/WebSocketManager.cs`，负责管理所有 WebSocket 实例。

**主要功能：**
- 统一管理所有 WebSocket 连接
- 提供全局更新循环
- 处理连接生命周期

#### 4. 事件参数类
- `OpenEventArgs` - 连接打开事件参数
- `CloseEventArgs` - 连接关闭事件参数
- `MessageEventArgs` - 消息接收事件参数
- `ErrorEventArgs` - 错误事件参数

#### 5. 枚举和设置
- `WebSocketState` - WebSocket 连接状态枚举
- `CloseStatusCode` - WebSocket 关闭状态码
- `Opcode` - WebSocket 操作码
- `Settings` - 模块设置信息

## 快速开始

### 基本使用

```csharp
using UnityWebSocket;

// 创建 WebSocket 实例
var ws = new WebSocket("ws://localhost:8080/ws");

// 注册事件处理
ws.OnOpen += (sender, e) => {
    Debug.Log("WebSocket 连接已建立");
};

ws.OnMessage += (sender, e) => {
    Debug.Log($"收到消息: {e.Data}");
};

ws.OnClose += (sender, e) => {
    Debug.Log($"WebSocket 连接已关闭，状态码: {e.Code}");
};

ws.OnError += (sender, e) => {
    Debug.LogError($"WebSocket 错误: {e.Message}");
};

// 建立连接
ws.ConnectAsync();

// 发送消息
ws.SendAsync("Hello, WebSocket!");

// 关闭连接
ws.CloseAsync();
```

### 带子协议的使用

```csharp
// 使用子协议创建 WebSocket
var ws = new WebSocket("ws://localhost:8080/ws", "my-protocol");

// 或者使用多个子协议
var protocols = new string[] { "protocol1", "protocol2" };
var ws = new WebSocket("ws://localhost:8080/ws", protocols);
```

## 详细使用指南

### 1. 连接管理

#### 建立连接
```csharp
var ws = new WebSocket("wss://echo.websocket.org");

// 注册连接成功事件
ws.OnOpen += (sender, e) => {
    Debug.Log("连接成功建立");
    
    // 连接成功后发送消息
    ws.SendAsync("连接测试消息");
};

// 开始连接
ws.ConnectAsync();
```

#### 检查连接状态
```csharp
// 检查是否已连接
if (ws.IsConnected)
{
    Debug.Log("WebSocket 已连接");
}

// 获取详细连接状态
var state = ws.ReadyState;
switch (state)
{
    case WebSocketState.Connecting:
        Debug.Log("连接中...");
        break;
    case WebSocketState.Open:
        Debug.Log("连接已打开");
        break;
    case WebSocketState.Closing:
        Debug.Log("连接关闭中...");
        break;
    case WebSocketState.Closed:
        Debug.Log("连接已关闭");
        break;
}
```

#### 关闭连接
```csharp
// 正常关闭连接
ws.CloseAsync();

// 在关闭事件中处理清理工作
ws.OnClose += (sender, e) => {
    Debug.Log($"连接已关闭，状态码: {e.Code}, 原因: {e.Reason}");
    
    // 执行清理操作
    CleanupResources();
};
```

### 2. 消息收发

#### 发送文本消息
```csharp
// 发送简单文本消息
ws.SendAsync("Hello, World!");

// 发送 JSON 格式消息
var message = new { type = "chat", content = "Hello", user = "Player1" };
var json = JsonUtility.ToJson(message);
ws.SendAsync(json);

// 发送带时间戳的消息
var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
ws.SendAsync($"[{timestamp}] 这是一条聊天消息");
```

#### 发送二进制消息
```csharp
// 发送字节数组
byte[] data = new byte[] { 0x01, 0x02, 0x03, 0x04 };
ws.SendAsync(data);

// 发送序列化对象
var playerData = new PlayerData { Health = 100, Position = new Vector3(1, 2, 3) };
byte[] serializedData = SerializePlayerData(playerData);
ws.SendAsync(serializedData);
```

#### 接收消息
```csharp
ws.OnMessage += (sender, e) => {
    // 处理文本消息
    if (!string.IsNullOrEmpty(e.Data))
    {
        Debug.Log($"收到文本消息: {e.Data}");
        
        // 解析 JSON 消息
        try
        {
            var message = JsonUtility.FromJson<ChatMessage>(e.Data);
            HandleChatMessage(message);
        }
        catch (Exception ex)
        {
            Debug.LogError($"消息解析失败: {ex.Message}");
        }
    }
    
    // 处理二进制消息
    if (e.RawData != null && e.RawData.Length > 0)
    {
        Debug.Log($"收到二进制消息，长度: {e.RawData.Length}");
        
        // 处理二进制数据
        ProcessBinaryData(e.RawData);
    }
};
```

### 3. 错误处理

```csharp
ws.OnError += (sender, e) => {
    Debug.LogError($"WebSocket 错误: {e.Message}");
    
    // 根据错误类型进行不同处理
    if (e.Message.Contains("timeout"))
    {
        Debug.LogError("连接超时，尝试重新连接...");
        Reconnect();
    }
    else if (e.Message.Contains("refused"))
    {
        Debug.LogError("连接被拒绝，请检查服务器状态");
    }
    else
    {
        Debug.LogError("未知错误发生");
    }
};
```

### 4. 连接状态监控

```csharp
public class WebSocketMonitor
{
    private WebSocket ws;
    private float lastPingTime;
    
    public WebSocketMonitor(string url)
    {
        ws = new WebSocket(url);
        SetupEventHandlers();
    }
    
    private void SetupEventHandlers()
    {
        ws.OnOpen += OnConnected;
        ws.OnClose += OnDisconnected;
        ws.OnMessage += OnMessageReceived;
        ws.OnError += OnErrorOccurred;
    }
    
    private void OnConnected(object sender, OpenEventArgs e)
    {
        Debug.Log("WebSocket 连接成功");
        lastPingTime = Time.time;
        StartHeartbeat();
    }
    
    private void OnDisconnected(object sender, CloseEventArgs e)
    {
        Debug.Log($"WebSocket 断开连接，状态码: {e.Code}");
        StopHeartbeat();
        
        // 自动重连逻辑
        if (e.Code != (ushort)CloseStatusCode.Normal)
        {
            StartReconnectTimer();
        }
    }
    
    private void StartHeartbeat()
    {
        // 启动心跳检测
        InvokeRepeating(nameof(SendHeartbeat), 30f, 30f);
    }
    
    private void SendHeartbeat()
    {
        if (ws.IsConnected)
        {
            ws.SendAsync("ping");
        }
    }
}
```

## 实际应用场景

### 1. 实时聊天系统

```csharp
public class ChatClient
{
    private WebSocket ws;
    
    public ChatClient(string serverUrl)
    {
        ws = new WebSocket(serverUrl);
        SetupChatHandlers();
    }
    
    private void SetupChatHandlers()
    {
        ws.OnOpen += (sender, e) => {
            Debug.Log("聊天服务器连接成功");
            
            // 发送登录消息
            var loginMsg = new {
                type = "login",
                username = "Player1",
                token = "auth_token"
            };
            ws.SendAsync(JsonUtility.ToJson(loginMsg));
        };
        
        ws.OnMessage += (sender, e) => {
            var message = JsonUtility.FromJson<ChatMessage>(e.Data);
            
            switch (message.type)
            {
                case "chat":
                    DisplayChatMessage(message);
                    break;
                case "user_join":
                    OnUserJoin(message);
                    break;
                case "user_leave":
                    OnUserLeave(message);
                    break;
                case "system":
                    DisplaySystemMessage(message);
                    break;
            }
        };
    }
    
    public void SendChatMessage(string content)
    {
        var message = new {
            type = "chat",
            content = content,
            timestamp = DateTime.Now
        };
        ws.SendAsync(JsonUtility.ToJson(message));
    }
}
```

### 2. 实时游戏数据同步

```csharp
public class GameSyncClient
{
    private WebSocket ws;
    
    public GameSyncClient(string gameServerUrl)
    {
        ws = new WebSocket(gameServerUrl);
        SetupGameHandlers();
    }
    
    private void SetupGameHandlers()
    {
        ws.OnMessage += (sender, e) => {
            var gameData = JsonUtility.FromJson<GameSyncData>(e.Data);
            
            // 同步游戏状态
            SyncPlayerPositions(gameData.players);
            UpdateGameObjects(gameData.objects);
            HandleGameEvents(gameData.events);
        };
    }
    
    public void SendPlayerUpdate(PlayerUpdate update)
    {
        var message = new {
            type = "player_update",
            data = update
        };
        ws.SendAsync(JsonUtility.ToJson(message));
    }
    
    public void SendGameAction(GameAction action)
    {
        var message = new {
            type = "game_action",
            action = action
        };
        ws.SendAsync(JsonUtility.ToJson(message));
    }
}
```

### 3. 实时数据监控

```csharp
public class DataMonitor
{
    private WebSocket ws;
    
    public DataMonitor(string dataSourceUrl)
    {
        ws = new WebSocket(dataSourceUrl);
        SetupMonitorHandlers();
    }
    
    private void SetupMonitorHandlers()
    {
        ws.OnMessage += (sender, e) => {
            var data = JsonUtility.FromJson<MonitorData>(e.Data);
            
            // 更新监控界面
            UpdateCpuUsage(data.cpuUsage);
            UpdateMemoryUsage(data.memoryUsage);
            UpdateNetworkStats(data.networkStats);
            
            // 触发警报
            if (data.cpuUsage > 90)
            {
                TriggerAlert("CPU 使用率过高");
            }
        };
    }
}
```

### 4. 多人游戏房间管理

```csharp
public class GameRoomClient
{
    private WebSocket ws;
    
    public GameRoomClient(string roomServerUrl)
    {
        ws = new WebSocket(roomServerUrl);
        SetupRoomHandlers();
    }
    
    private void SetupRoomHandlers()
    {
        ws.OnMessage += (sender, e) => {
            var roomMessage = JsonUtility.FromJson<RoomMessage>(e.Data);
            
            switch (roomMessage.action)
            {
                case "room_created":
                    OnRoomCreated(roomMessage);
                    break;
                case "player_joined":
                    OnPlayerJoined(roomMessage);
                    break;
                case "player_left":
                    OnPlayerLeft(roomMessage);
                    break;
                case "game_started":
                    OnGameStarted(roomMessage);
                    break;
                case "game_ended":
                    OnGameEnded(roomMessage);
                    break;
            }
        };
    }
    
    public void CreateRoom(string roomName, int maxPlayers)
    {
        var message = new {
            action = "create_room",
            roomName = roomName,
            maxPlayers = maxPlayers
        };
        ws.SendAsync(JsonUtility.ToJson(message));
    }
    
    public void JoinRoom(string roomId)
    {
        var message = new {
            action = "join_room",
            roomId = roomId
        };
        ws.SendAsync(JsonUtility.ToJson(message));
    }
}
```

## 跨平台注意事项

### WebGL 环境

```csharp
// WebGL 环境下的特殊处理
#if !UNITY_EDITOR && UNITY_WEBGL
    // WebGL 特定的代码
    // 注意：WebGL 环境下的 WebSocket 实现基于浏览器原生 API
#endif
```

### 非 WebGL 环境

```csharp
// 非 WebGL 环境（包括编辑器和其他平台）
#if UNITY_EDITOR || !UNITY_WEBGL
    // 使用 System.Net.WebSockets 的实现
#endif
```

## 性能优化建议

### 1. 消息频率控制

```csharp
public class OptimizedWebSocketClient
{
    private WebSocket ws;
    private float lastSendTime;
    private const float SEND_INTERVAL = 0.1f; // 100ms 间隔
    
    public void SendOptimizedMessage(string message)
    {
        // 控制消息发送频率
        if (Time.time - lastSendTime < SEND_INTERVAL)
        {
            // 如果发送太频繁，可以合并消息或丢弃
            return;
        }
        
        ws.SendAsync(message);
        lastSendTime = Time.time;
    }
}
```

### 2. 消息大小优化

```csharp
// 使用压缩或简化数据格式
public class CompressedWebSocketClient
{
    public void SendCompressedData(object data)
    {
        // 简化数据结构，减少传输数据量
        var simplified = SimplifyData(data);
        var json = JsonUtility.ToJson(simplified);
        
        // 如果数据量很大，可以考虑压缩
        if (json.Length > 1024) // 1KB
        {
            var compressed = CompressData(json);
            ws.SendAsync(compressed);
        }
        else
        {
            ws.SendAsync(json);
        }
    }
}
```

## API 参考

### IWebSocket 接口

#### 属性
| 属性 | 类型 | 说明 |
|------|------|------|
| `IsConnected` | `bool` | 获取是否已连接 |
| `ReadyState` | `WebSocketState` | 获取连接状态 |

#### 方法
| 方法 | 参数 | 说明 |
|------|------|------|
| `ConnectAsync()` | 无 | 异步建立连接 |
| `CloseAsync()` | 无 | 异步关闭连接 |
| `SendAsync(byte[] data)` | `data` - 要发送的字节数组 | 异步发送二进制数据 |
| `SendAsync(string text)` | `text` - 要发送的文本 | 异步发送文本数据 |

#### 事件
| 事件 | 参数类型 | 说明 |
|------|----------|------|
| `OnOpen` | `OpenEventArgs` | 连接打开时触发 |
| `OnClose` | `CloseEventArgs` | 连接关闭时触发 |
| `OnError` | `ErrorEventArgs` | 发生错误时触发 |
| `OnMessage` | `MessageEventArgs` | 收到消息时触发 |

### WebSocketState 枚举

| 值 | 说明 |
|----|------|
| `Connecting` | 连接中（数值：0） |
| `Open` | 连接已打开（数值：1） |
| `Closing` | 连接关闭中（数值：2） |
| `Closed` | 连接已关闭（数值：3） |

### MessageEventArgs 类

| 属性 | 类型 | 说明 |
|------|------|------|
| `Data` | `string` | 消息文本内容 |
| `RawData` | `byte[]` | 消息原始字节数据 |
| `Opcode` | `Opcode` | 消息操作码 |

### CloseEventArgs 类

| 属性 | 类型 | 说明 |
|------|------|------|
| `Code` | `ushort` | 关闭状态码 |
| `Reason` | `string` | 关闭原因 |

## 错误处理和调试

### 1. 连接错误处理

```csharp
try
{
    ws.ConnectAsync();
}
catch (Exception ex)
{
    Debug.LogError($"连接失败: {ex.Message}");
    
    // 根据错误类型进行不同处理
    if (ex is InvalidOperationException)
    {
        Debug.LogError("WebSocket 状态异常");
    }
    else if (ex is ArgumentException)
    {
        Debug.LogError("参数错误");
    }
}
```

### 2. 消息发送错误处理

```csharp
public void SafeSend(string message)
{
    if (!ws.IsConnected)
    {
        Debug.LogWarning("WebSocket 未连接，无法发送消息");
        return;
    }
    
    if (string.IsNullOrEmpty(message))
    {
        Debug.LogWarning("消息内容为空");
        return;
    }
    
    try
    {
        ws.SendAsync(message);
    }
    catch (Exception ex)
    {
        Debug.LogError($"消息发送失败: {ex.Message}");
    }
}
```

## 注意事项

### 1. 线程安全
- WebSocket 操作是线程安全的
- 但事件回调可能在非主线程中执行
- 在 Unity 中处理事件时注意线程同步

### 2. 内存管理
- 及时关闭不再使用的 WebSocket 连接
- 避免在消息处理中创建大量临时对象
- 注意二进制数据的内存使用

### 3. 网络状态
- 在发送消息前检查连接状态
- 处理网络中断和重连逻辑
- 实现心跳机制检测连接健康度

### 4. 安全性
- 使用 WSS（WebSocket Secure）协议
- 验证服务器证书
- 对敏感数据进行加密

## 常见问题解答

### Q: 如何检测 WebSocket 连接是否正常？
A: 检查 `IsConnected` 属性或 `ReadyState` 属性，并实现心跳机制。

### Q: WebSocket 连接断开后如何自动重连？
A: 在 `OnClose` 事件中实现重连逻辑，注意重连间隔和最大重连次数。

### Q: 如何处理大量实时数据？
A: 优化消息格式，控制发送频率，使用二进制格式减少数据量。

### Q: WebGL 和非 WebGL 环境有什么差异？
A: WebGL 使用浏览器原生 WebSocket API，非 WebGL 使用 System.Net.WebSockets。

### Q: 如何发送和接收二进制数据？
A: 使用 `SendAsync(byte[] data)` 发送，通过 `MessageEventArgs.RawData` 接收。

### Q: 如何设置 WebSocket 子协议？
A: 在构造函数中传入子协议字符串或字符串数组。

WebSocket 模块为 FuFramework 提供了强大的实时通信能力，通过事件驱动的设计和跨平台支持，使得开发者可以轻松构建各种实时应用，如聊天系统、多人游戏、实时数据监控等。