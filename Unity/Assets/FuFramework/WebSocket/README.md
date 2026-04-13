# FuFramework WebSocket 模块

## 1. 概述

WebSocket 模块是 FuFramework 中的实时通信系统，提供基于 WebSocket 协议的双向通信功能。该模块支持跨平台（WebGL和非WebGL）、自动重连、消息队列管理等功能。

### 1.1 核心特性

- **跨平台支持**：支持 WebGL 和非 WebGL 环境
- **完整协议支持**：实现 WebSocket 标准协议
- **异步操作**：所有连接和消息操作都是异步的
- **事件驱动**：基于事件回调的消息处理机制
- **连接状态管理**：完整的连接状态跟踪和错误处理
- **子协议支持**：支持 WebSocket 子协议
- **自动重连**：内置连接断开自动重连机制

## 系统架构

### 类继承体系

```
IWebSocket (接口)
    ├── WebSocket (NoWebGL 实现)
    │   └── WebSocketManager (NoWebGL 管理器)
    └── WebSocket (WebGL 实现)
        └── WebSocketManager (WebGL 管理器)

EventArgs (事件基类)
    ├── OpenEventArgs (连接打开事件)
    ├── CloseEventArgs (连接关闭事件)
    ├── MessageEventArgs (消息接收事件)
    └── ErrorEventArgs (错误事件)

枚举类型
    ├── WebSocketState (连接状态)
    ├── CloseStatusCode (关闭状态码)
    └── Opcode (操作码)
```

### 技术架构图

```
┌─────────────────────────────────────────────────────────────┐
│                    IWebSocket (接口)                        │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  核心方法                                            │   │
│  │  - ConnectAsync()    异步连接                        │   │
│  │  - CloseAsync()      异步关闭                        │   │
│  │  - SendAsync()       异步发送                        │   │
│  └─────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  核心属性                                            │   │
│  │  - IsConnected       是否已连接                      │   │
│  │  - ReadyState        连接状态                        │   │
│  │  - Address           连接地址                        │   │
│  │  - SubProtocols      子协议数组                      │   │
│  └─────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  事件                                                │   │
│  │  - OnOpen            连接打开                        │   │
│  │  - OnClose           连接关闭                        │   │
│  │  - OnError           错误发生                        │   │
│  │  - OnMessage         收到消息                        │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              ↓
        ┌─────────────────────┼─────────────────────┐
        ↓                     ↓                     ↓
┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│  NoWebGL     │    │    WebGL     │    │  事件系统     │
│  实现        │    │    实现      │    │              │
│              │    │              │    │              │
│ System.Net   │    │ 浏览器原生   │    │  线程安全     │
│ .WebSockets  │    │ WebSocket API│    │  事件队列     │
│              │    │              │    │  回调分发     │
└──────────────┘    └──────────────┘    └──────────────┘
```

### 2.2 核心类说明

#### 2.2.1 IWebSocket (WebSocket 接口)
位于 `Runtime/Core/IWebSocket.cs`，定义了 WebSocket 的核心接口。

**主要属性：**
| 属性 | 类型 | 说明 |
|------|------|------|
| `IsConnected` | `bool` | 是否已连接 |
| `ReadyState` | `WebSocketState` | 连接状态 |
| `Address` | `string` | 连接地址 |
| `SubProtocols` | `string[]` | 子协议数组 |

**主要方法：**
| 方法 | 参数 | 说明 |
|------|------|------|
| `ConnectAsync()` | 无 | 异步建立连接 |
| `CloseAsync()` | 无 | 异步关闭连接 |
| `SendAsync(byte[] data)` | `data` - 字节数组 | 异步发送二进制数据 |
| `SendAsync(string text)` | `text` - 文本字符串 | 异步发送文本数据 |

**事件：**
| 事件 | 参数类型 | 说明 |
|------|----------|------|
| `OnOpen` | `OpenEventArgs` | 连接打开事件 |
| `OnClose` | `CloseEventArgs` | 连接关闭事件 |
| `OnError` | `ErrorEventArgs` | 错误事件 |
| `OnMessage` | `MessageEventArgs` | 消息接收事件 |

#### 2.2.2 WebSocket (NoWebGL 实现)
位于 `Runtime/Implementation/NoWebGL/WebSocket.cs`，使用 `System.Net.WebSockets.ClientWebSocket` 实现。

**核心机制：**
- **并发队列**：使用 `ConcurrentQueue<SendBuffer>` 管理发送队列
- **事件队列**：使用 `ConcurrentQueue<EventArgs>` 管理事件队列
- **双任务模型**：独立的接收任务和发送任务
- **取消令牌**：使用 `CancellationTokenSource` 控制任务取消

**内部类：**
```csharp
class SendBuffer
{
    public byte[] data;           // 数据内容
    public WebSocketMessageType type;  // 消息类型（Text/Binary）
}
```

**核心方法：**
| 方法 | 说明 |
|------|------|
| `ConnectTask()` | 异步连接任务，建立连接后启动接收和发送任务 |
| `StartSendTask()` | 发送任务，从队列取出数据并发送 |
| `StartReceiveTask()` | 接收任务，持续接收消息并触发事件 |
| `Update()` | 在主线程处理事件队列 |

#### 2.2.3 WebSocketManager (NoWebGL 管理器)
位于 `Runtime/Implementation/NoWebGL/WebSocketManager.cs`，管理所有 WebSocket 实例。

**特性：**
- `[DisallowMultipleComponent]` - 禁止重复组件
- `[DefaultExecutionOrder(-10000)]` - 确保最先执行
- `DontDestroyOnLoad` - 跨场景保持

**核心功能：**
- 单例模式管理
- 维护 WebSocket 实例列表
- 每帧调用所有实例的 `Update()` 方法
- 应用退出时中止所有连接

#### 2.2.4 WebSocket (WebGL 实现)
位于 `Runtime/Implementation/WebGL/WebSocket.cs`，通过 JavaScript 插件调用浏览器原生 WebSocket API。

**核心机制：**
- **实例 ID**：每个 WebSocket 分配唯一的 `instanceId`
- **JS 回调**：通过 `DllImport` 调用 JavaScript 函数
- **错误码映射**：将 JavaScript 错误码转换为错误消息

**错误码说明：**
| 错误码 | 说明 |
|--------|------|
| -1 | WebSocket 实例未找到 |
| -2 | WebSocket 已连接或正在连接 |
| -3 | WebSocket 未连接 |
| -4 | WebSocket 正在关闭 |
| -5 | WebSocket 已关闭 |
| -6 | WebSocket 未处于打开状态 |
| -7 | 无效的关闭码或原因过长 |
| -8 | 不支持缓冲区切片 |

#### 2.2.5 WebSocketManager (WebGL 管理器)
位于 `Runtime/Implementation/WebGL/WebSocketManager.cs`，管理 WebGL 平台的 WebSocket 实例。

**核心功能：**
- 维护实例 ID 到 WebSocket 的映射表
- 初始化 JavaScript 回调委托
- 处理 Unity 6000 版本的兼容性

**JavaScript 互操作：**
```csharp
[DllImport("__Internal")]
public static extern int WebSocketConnect(int instanceId);

[DllImport("__Internal")]
public static extern int WebSocketSend(int instanceId, byte[] dataPtr, int dataLength);

// ... 更多 JS 函数
```

#### 2.2.6 事件参数类

**OpenEventArgs** - 连接打开事件参数
```csharp
public class OpenEventArgs : EventArgs
{
    internal OpenEventArgs() { }
}
```

**CloseEventArgs** - 连接关闭事件参数
| 属性 | 类型 | 说明 |
|------|------|------|
| `Code` | `ushort` | 关闭状态码 |
| `Reason` | `string` | 关闭原因 |
| `WasClean` | `bool` | 是否正常关闭 |
| `StatusCode` | `CloseStatusCode` | 枚举类型的状态码 |

**MessageEventArgs** - 消息接收事件参数
| 属性 | 类型 | 说明 |
|------|------|------|
| `Data` | `string` | 文本消息内容（UTF-8 解码） |
| `RawData` | `byte[]` | 原始字节数据 |
| `IsBinary` | `bool` | 是否为二进制消息 |
| `IsText` | `bool` | 是否为文本消息 |

**ErrorEventArgs** - 错误事件参数
| 属性 | 类型 | 说明 |
|------|------|------|
| `Message` | `string` | 错误消息 |
| `Exception` | `Exception` | 异常对象（可能为 null） |

#### 2.2.7 枚举类型

**WebSocketState** - 连接状态枚举
| 值 | 数值 | 说明 |
|----|------|------|
| `Connecting` | 0 | 连接中 |
| `Open` | 1 | 连接已打开 |
| `Closing` | 2 | 连接关闭中 |
| `Closed` | 3 | 连接已关闭 |

**CloseStatusCode** - 关闭状态码枚举（RFC 6455 标准）
| 值 | 数值 | 说明 |
|----|------|------|
| `Normal` | 1000 | 正常关闭 |
| `Away` | 1001 | 端点离开 |
| `ProtocolError` | 1002 | 协议错误 |
| `UnsupportedData` | 1003 | 不支持的数据类型 |
| `NoStatus` | 1005 | 无状态码（保留值） |
| `Abnormal` | 1006 | 异常关闭（保留值） |
| `InvalidData` | 1007 | 数据不一致 |
| `PolicyViolation` | 1008 | 违反策略 |
| `TooBig` | 1009 | 消息过大 |
| `MandatoryExtension` | 1010 | 需要扩展 |
| `ServerError` | 1011 | 服务器错误 |
| `TlsHandshakeFailure` | 1015 | TLS 握手失败（保留值） |

**Opcode** - WebSocket 帧类型枚举
| 值 | 数值 | 说明 |
|----|------|------|
| `Text` | 0x1 | 文本帧 |
| `Binary` | 0x2 | 二进制帧 |
| `Close` | 0x8 | 连接关闭帧 |

#### 2.2.8 Settings (模块设置)
位于 `Runtime/Core/Settings.cs`，包含模块的元数据信息。

| 常量 | 值 | 说明 |
|------|----|------|
| `VERSION` | "2.8.6" | 版本号 |
| `AUHTOR` | "psygames" | 作者 |
| `GITHUB` | URL | GitHub 仓库地址 |
| `EMAIL` | "799329256@qq.com" | 联系邮箱 |
| `QQ_GROUP` | "1126457634" | QQ 群号 |

## 3. 快速开始

### 3.1 基本使用

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

### 3.2 带子协议的使用

```csharp
// 使用子协议创建 WebSocket
var ws = new WebSocket("ws://localhost:8080/ws", "my-protocol");

// 或者使用多个子协议
var protocols = new string[] { "protocol1", "protocol2" };
var ws = new WebSocket("ws://localhost:8080/ws", protocols);
```

## 4. 详细使用指南

### 4.1 连接管理

#### 4.1.1 建立连接
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

#### 4.1.2 检查连接状态
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

#### 4.1.3 关闭连接
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

### 4.2 消息收发

#### 4.2.1 发送文本消息
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

#### 4.2.2 发送二进制消息
```csharp
// 发送字节数组
byte[] data = new byte[] { 0x01, 0x02, 0x03, 0x04 };
ws.SendAsync(data);

// 发送序列化对象
var playerData = new PlayerData { Health = 100, Position = new Vector3(1, 2, 3) };
byte[] serializedData = SerializePlayerData(playerData);
ws.SendAsync(serializedData);
```

#### 4.2.3 接收消息
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

### 4.3 错误处理

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

### 4.4 连接状态监控

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

## 5. 实际应用场景

### 5.1 实时聊天系统

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

### 5.2 实时游戏数据同步

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

### 5.3 实时数据监控

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

### 5.4 多人游戏房间管理

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

## 6. 目录结构

```
FuFramework/WebSocket/
├── Editor/
│   ├── UnityWebSocket.Editor.asmdef
│   └── SettingsWindow.cs              # 设置窗口编辑器
├── Plugins/
│   └── WebGL/
│       └── WebSocket.jslib            # WebGL JavaScript 插件
├── Runtime/
│   ├── UnityWebSocket.Runtime.asmdef
│   ├── Core/
│   │   ├── EventArgs/
│   │   │   ├── OpenEventArgs.cs       # 连接打开事件参数
│   │   │   ├── CloseEventArgs.cs      # 连接关闭事件参数
│   │   │   ├── CloseStatusCode.cs     # 关闭状态码枚举
│   │   │   ├── MessageEventArgs.cs    # 消息接收事件参数
│   │   │   └── ErrorEventArgs.cs      # 错误事件参数
│   │   ├── IWebSocket.cs              # WebSocket 接口定义
│   │   ├── Opcode.cs                  # WebSocket 操作码枚举
│   │   ├── Settings.cs                # 模块设置信息
│   │   └── WebSocketState.cs          # 连接状态枚举
│   └── Implementation/
│       ├── NoWebGL/
│       │   ├── WebSocket.cs           # NoWebGL 平台实现
│       │   └── WebSocketManager.cs    # NoWebGL 管理器
│       └── WebGL/
│           ├── WebSocket.cs           # WebGL 平台实现
│           └── WebSocketManager.cs    # WebGL 管理器
└── README.md                          # 本文档
```

## 7. 高级特性

### 7.1 双平台架构设计

WebSocket 模块采用条件编译实现跨平台支持：

| 平台 | 条件编译指令 | 实现方式 | 特点 |
|------|-------------|----------|------|
| NoWebGL | `!NET_LEGACY && (UNITY_EDITOR \|\| !UNITY_WEBGL)` | System.Net.WebSockets | 完整功能，异步任务 |
| WebGL | `!UNITY_EDITOR && UNITY_WEBGL` | JavaScript 插件 | 浏览器原生 API |

**架构优势：**
- 统一的 `IWebSocket` 接口，平台无关的编程模型
- 自动平台检测，无需手动选择实现
- 事件机制保持一致，便于跨平台开发

### 7.2 线程安全设计

**NoWebGL 平台的线程安全机制：**

```
主线程 (Unity Main Thread)
    │
    ├── Update() ──→ 处理 eventQueue ──→ 触发事件回调
    │
    └── 用户代码调用 ConnectAsync/SendAsync/CloseAsync

后台线程 (Background Threads)
    │
    ├── ConnectTask ──→ 建立连接
    │
    ├── SendTask ──→ 从 sendQueue 取数据 ──→ 发送
    │
    └── ReceiveTask ──→ 接收数据 ──→ 加入 eventQueue
```

**线程安全组件：**
- `ConcurrentQueue<SendBuffer> sendQueue` - 线程安全的发送队列
- `ConcurrentQueue<EventArgs> eventQueue` - 线程安全的事件队列
- `CancellationTokenSource cts` - 取消令牌源

### 7.3 WebGL JavaScript 插件

**WebSocket.jslib 核心功能：**

```javascript
// 实例管理
var instances = {};        // 存储 WebSocket 实例
var lastId = 0;            // 实例 ID 计数器

// 回调函数
var onOpen = null;         // 连接打开回调
var onMessage = null;      // 消息接收回调
var onMessageStr = null;   // 文本消息回调
var onError = null;        // 错误回调
var onClose = null;        // 关闭回调
```

**C# 与 JavaScript 交互流程：**

```
C# 代码
    │
    ├── WebSocketAllocate(url) ──→ 创建 JS WebSocket 实例
    ├── WebSocketConnect(id) ──→ 调用 ws.connect()
    ├── WebSocketSend(id, data) ──→ 调用 ws.send()
    └── WebSocketClose(id, code, reason) ──→ 调用 ws.close()

JavaScript 回调
    │
    ├── onopen ──→ DelegateOnOpenEvent ──→ C# OnOpen 事件
    ├── onmessage ──→ DelegateOnMessageEvent ──→ C# OnMessage 事件
    ├── onerror ──→ DelegateOnErrorEvent ──→ C# OnError 事件
    └── onclose ──→ DelegateOnCloseEvent ──→ C# OnClose 事件
```

### 7.4 消息处理机制

**消息接收流程（NoWebGL）：**

```csharp
// 1. 接收数据片段
var result = await socket.ReceiveAsync(segment, cts.Token);
ms.Write(segment.Array, 0, result.Count);

// 2. 检查是否完整消息
if (!result.EndOfMessage) continue;

// 3. 获取完整数据
var data = ms.ToArray();
ms.SetLength(0);

// 4. 根据消息类型处理
switch (result.MessageType)
{
    case WebSocketMessageType.Binary:
        HandleMessage(Opcode.Binary, data);
        break;
    case WebSocketMessageType.Text:
        HandleMessage(Opcode.Text, data);
        break;
    case WebSocketMessageType.Close:
        // 处理关闭
        break;
}
```

**消息发送流程（NoWebGL）：**

```csharp
// 1. 将消息加入发送队列
sendQueue.Enqueue(new SendBuffer(data, messageType));

// 2. 发送任务从队列取出
while (sendQueue.TryDequeue(out var buffer))
{
    // 3. 异步发送
    await socket.SendAsync(
        new ArraySegment<byte>(buffer.data), 
        buffer.type, 
        true, 
        cts.Token
    );
}
```

### 7.5 自动重连机制

虽然模块本身不提供内置的自动重连，但可以通过事件轻松实现：

```csharp
public class AutoReconnectWebSocket
{
    private WebSocket ws;
    private string url;
    private int reconnectAttempts = 0;
    private const int MAX_RECONNECT_ATTEMPTS = 5;
    private const float RECONNECT_DELAY = 3f;

    public AutoReconnectWebSocket(string serverUrl)
    {
        url = serverUrl;
        Connect();
    }

    private void Connect()
    {
        ws = new WebSocket(url);
        ws.OnOpen += OnOpen;
        ws.OnClose += OnClose;
        ws.OnError += OnError;
        ws.OnMessage += OnMessage;
        ws.ConnectAsync();
    }

    private void OnClose(object sender, CloseEventArgs e)
    {
        if (e.Code != (ushort)CloseStatusCode.Normal && 
            reconnectAttempts < MAX_RECONNECT_ATTEMPTS)
        {
            reconnectAttempts++;
            Debug.Log($"{RECONNECT_DELAY}秒后尝试重连... (尝试 {reconnectAttempts}/{MAX_RECONNECT_ATTEMPTS})");
            // 使用协程或延迟调用
            Invoke(nameof(Reconnect), RECONNECT_DELAY);
        }
    }

    private void Reconnect()
    {
        Connect();
    }

    private void OnOpen(object sender, OpenEventArgs e)
    {
        reconnectAttempts = 0; // 重置重连计数
        Debug.Log("连接成功");
    }
}
```

## 8. 跨平台注意事项

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

## 9. 性能优化建议

### 9.1 消息频率控制

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

### 9.2 消息大小优化

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

## 10. API 参考

### 10.1 IWebSocket 接口

#### 10.1.1 属性
| 属性 | 类型 | 说明 |
|------|------|------|
| `IsConnected` | `bool` | 获取是否已连接 |
| `ReadyState` | `WebSocketState` | 获取连接状态 |

#### 10.1.2 方法
| 方法 | 参数 | 说明 |
|------|------|------|
| `ConnectAsync()` | 无 | 异步建立连接 |
| `CloseAsync()` | 无 | 异步关闭连接 |
| `SendAsync(byte[] data)` | `data` - 要发送的字节数组 | 异步发送二进制数据 |
| `SendAsync(string text)` | `text` - 要发送的文本 | 异步发送文本数据 |

#### 10.1.3 事件
| 事件 | 参数类型 | 说明 |
|------|----------|------|
| `OnOpen` | `OpenEventArgs` | 连接打开时触发 |
| `OnClose` | `CloseEventArgs` | 连接关闭时触发 |
| `OnError` | `ErrorEventArgs` | 发生错误时触发 |
| `OnMessage` | `MessageEventArgs` | 收到消息时触发 |

### 10.2 WebSocketState 枚举

| 值 | 说明 |
|----|------|
| `Connecting` | 连接中（数值：0） |
| `Open` | 连接已打开（数值：1） |
| `Closing` | 连接关闭中（数值：2） |
| `Closed` | 连接已关闭（数值：3） |

### 10.3 MessageEventArgs 类

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
- **NoWebGL 平台**：WebSocket 操作是线程安全的，使用 `ConcurrentQueue` 确保线程安全
- **事件回调**：在 NoWebGL 平台，事件回调在主线程执行（通过 `Update()` 方法）
- **WebGL 平台**：所有操作和回调都在主线程执行
- **建议**：避免在事件回调中执行耗时操作，以免阻塞主线程

### 2. 内存管理
- **及时关闭连接**：不再使用时调用 `CloseAsync()`，避免资源泄漏
- **避免频繁创建**：WebSocket 实例可以复用，不要频繁创建和销毁
- **消息数据处理**：
  - 文本消息使用 `MessageEventArgs.Data`（UTF-8 解码）
  - 二进制消息使用 `MessageEventArgs.RawData`
  - 避免在消息回调中创建大量临时对象
- **WebGL 注意**：JavaScript 端的 WebSocket 实例由垃圾回收管理

### 3. 网络状态
- **连接检查**：发送消息前检查 `IsConnected` 或 `ReadyState`
```csharp
if (!ws.IsConnected)
{
    Debug.LogWarning("WebSocket 未连接");
    return;
}
```
- **异常处理**：捕获并处理可能的异常（连接失败、发送失败等）
- **心跳机制**：定期发送心跳消息检测连接健康度
```csharp
private float lastPingTime;
private const float PING_INTERVAL = 30f;

void Update()
{
    if (ws.IsConnected && Time.time - lastPingTime > PING_INTERVAL)
    {
        ws.SendAsync("ping");
        lastPingTime = Time.time;
    }
}
```

### 4. 安全性
- **使用 WSS**：生产环境务必使用 `wss://` 协议（WebSocket Secure）
- **证书验证**：确保服务器证书有效，防止中间人攻击
- **数据加密**：敏感数据在应用层额外加密
- **输入验证**：验证接收到的消息数据，防止注入攻击

### 5. 平台差异
- **WebGL 限制**：
  - 无法直接访问底层 Socket API
  - 受浏览器 CORS 策略限制
  - 某些子协议可能不被支持
- **NoWebGL 优势**：
  - 完整的 WebSocket 功能
  - 更好的性能和灵活性
  - 支持自定义 HTTP 头（部分平台）

### 6. 日志调试
- 定义 `UNITY_WEB_SOCKET_LOG` 符号可以启用详细日志
- 日志包含时间戳、线程 ID 和操作信息
- 有助于排查连接和消息问题

## 常见问题解答

### Q: 如何检测 WebSocket 连接是否正常？
A: 检查 `IsConnected` 属性或 `ReadyState` 属性，并实现心跳机制：
```csharp
// 检查连接状态
if (ws.IsConnected)
{
    Debug.Log("WebSocket 已连接");
}

// 获取详细状态
switch (ws.ReadyState)
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

### Q: WebSocket 连接断开后如何自动重连？
A: 在 `OnClose` 事件中实现重连逻辑，注意重连间隔和最大重连次数：
```csharp
private int reconnectAttempts = 0;
private const int MAX_RECONNECT = 5;
private const float RECONNECT_DELAY = 3f;

ws.OnClose += (sender, e) => {
    if (e.Code != (ushort)CloseStatusCode.Normal && 
        reconnectAttempts < MAX_RECONNECT)
    {
        reconnectAttempts++;
        Invoke(nameof(Reconnect), RECONNECT_DELAY);
    }
};

ws.OnOpen += (sender, e) => {
    reconnectAttempts = 0; // 重置计数
};
```

### Q: 如何处理大量实时数据？
A: 采取以下优化措施：
1. **使用二进制格式**：减少数据体积
2. **控制发送频率**：避免过于频繁的发送
3. **消息合并**：将多个小消息合并发送
4. **数据压缩**：对大数据进行压缩
5. **限流处理**：在接收端实现限流机制

### Q: WebGL 和非 WebGL 环境有什么差异？
A: 主要差异如下：

| 特性 | NoWebGL | WebGL |
|------|---------|-------|
| 实现方式 | System.Net.WebSockets | 浏览器原生 API |
| 线程模型 | 多线程（后台任务） | 单线程 |
| 性能 | 更高 | 受浏览器限制 |
| 功能完整性 | 完整 | 部分受限 |
| CORS | 无限制 | 受浏览器限制 |
| 自定义头 | 支持 | 部分支持 |

### Q: 如何发送和接收二进制数据？
A: 使用以下方式：
```csharp
// 发送二进制数据
byte[] data = new byte[] { 0x01, 0x02, 0x03 };
ws.SendAsync(data);

// 接收二进制数据
ws.OnMessage += (sender, e) => {
    if (e.IsBinary)
    {
        byte[] rawData = e.RawData;
        // 处理二进制数据
    }
};
```

### Q: 如何设置 WebSocket 子协议？
A: 在构造函数中传入子协议：
```csharp
// 单个子协议
var ws = new WebSocket("ws://example.com", "my-protocol");

// 多个子协议（按优先级）
var protocols = new string[] { "protocol1", "protocol2" };
var ws = new WebSocket("ws://example.com", protocols);
```

### Q: 如何处理连接超时？
A: 模块本身没有内置超时机制，可以通过以下方式实现：
```csharp
private float connectStartTime;
private const float CONNECT_TIMEOUT = 10f;

public void ConnectWithTimeout()
{
    connectStartTime = Time.time;
    ws.ConnectAsync();
}

void Update()
{
    if (ws.ReadyState == WebSocketState.Connecting)
    {
        if (Time.time - connectStartTime > CONNECT_TIMEOUT)
        {
            Debug.LogError("连接超时");
            ws.CloseAsync();
        }
    }
}
```

### Q: 为什么 WebGL 平台无法连接？
A: 可能原因：
1. **CORS 限制**：服务器需要配置 CORS 头
2. **WSS 要求**：WebGL 必须使用 WSS（HTTPS 页面）
3. **协议不匹配**：检查子协议是否正确
4. **浏览器限制**：某些浏览器有额外的安全限制

### Q: 如何在场景切换时保持连接？
A: WebSocketManager 使用 `DontDestroyOnLoad`，会自动保持：
```csharp
// WebSocketManager 会自动处理跨场景
// 只需确保在场景切换后不调用 CloseAsync()

// 如果需要手动控制：
void OnDestroy()
{
    // 只在应用退出时关闭
    if (Application.isPlaying)
    {
        ws.CloseAsync();
    }
}
```

---

WebSocket 模块为 FuFramework 提供了强大的实时通信能力，通过事件驱动的设计和跨平台支持，使得开发者可以轻松构建各种实时应用，如聊天系统、多人游戏、实时数据监控等。

**版本信息：** v2.8.6  
**作者：** psygames  
**GitHub：** https://github.com/psygames/UnityWebSocket