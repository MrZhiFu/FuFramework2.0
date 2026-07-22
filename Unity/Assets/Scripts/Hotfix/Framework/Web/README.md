# FuFramework Web Module

## 1. 简介

FuFramework Web 模块是游戏框架的 HTTP 请求管理系统，基于 `System.Net.WebRequest`（非 WebGL 平台）和 `UnityWebRequest`（WebGL 平台）实现高效的 Web 请求处理。该模块支持 JSON 字符串、字节数组和 ProtoBuf 三种请求/响应格式，提供请求队列和并发控制能力，并通过 `Task<T>` 支持 async/await 异步模式。

## 2. 核心特性

- **多格式支持**：JSON 字符串、字节数组（Buffer）、ProtoBuf 三种请求/响应格式
- **请求队列**：内置请求队列，支持并发控制（`MaxConnectionPerServer`）
- **异步支持**：基于 `Task<T>`（`TaskCompletionSource<T>`）的 async/await 异步请求
- **JSON 结果封装**：统一的 `HttpJsonResult` 响应结构（Code/Message/Data — PascalCase）
- **泛型结果解析**：`HttpJsonResultData<T>` 封装解析结果，`IsSuccess` 为独立属性
- **ProtoBuf 序列化**：使用 `application/x-protobuf` content type，内置消息路由
- **跨平台**：自动适配 WebGL 和非 WebGL 平台

## 3. 核心概念

### 3.1 请求队列架构

```
┌─────────────────────────────────────────────────────────────┐
│                       WebModule                              │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  m_WaitingNormalQueue (Queue<WebJsonData>)          │   │
│  │  - JSON 请求等待队列                                 │   │
│  └─────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  m_SendingNormalList (List<WebJsonData>)            │   │
│  │  - JSON 正在处理的请求列表                           │   │
│  └─────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  m_WaitingProtoBufQueue / m_SendingProtoBufList     │   │
│  │  - ProtoBuf 请求队列                                │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              │
           ┌──────────────────┼──────────────────┐
           ▼                  ▼                  ▼
   ┌──────────────┐  ┌──────────────┐  ┌──────────────┐
   │ WebJsonData  │  │WebProtoBufData│ │   WebData    │
   │ (JSON 请求)   │  │(ProtoBuf请求) │ │   (基类)     │
   └──────────────┘  └──────────────┘  └──────────────┘
           │                  │
           ▼                  ▼
   ┌──────────────┐  ┌──────────────┐
   │WebStringResult│ │WebBufferResult│
   └──────────────┘  └──────────────┘
```

## 4. 核心类说明

### 4.1 WebModule

Web 管理模块，继承自 `ModuleBase`。通过 `ModuleManager.GetModule<WebModule>()` 获取实例。请求队列在 `OnUpdate` 中轮询处理。

**核心属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| `Instance` | `WebModule` | 模块静态单例 |
| `Timeout` | `float` | 请求超时时间（秒），默认 5 |
| `MaxConnectionPerServer` | `int` | 每个服务器的最大并发连接数，默认 8 |
| `RequestTimeout` | `TimeSpan`（只读） | 请求超时时间的 TimeSpan 表示 |

**核心方法 — 字符串请求：**

```csharp
// GET 请求，返回字符串
Task<WebStringResult> GetToString(string url, object userData = null)
Task<WebStringResult> GetToString(string url, Dictionary<string, string> queryString, object userData = null)
Task<WebStringResult> GetToString(string url, Dictionary<string, string> queryString, Dictionary<string, string> header, object userData = null)

// POST 请求，返回字符串
Task<WebStringResult> PostToString(string url, Dictionary<string, object> from, object userData = null)
Task<WebStringResult> PostToString(string url, Dictionary<string, object> from, Dictionary<string, string> queryString, object userData = null)
Task<WebStringResult> PostToString(string url, Dictionary<string, object> from, Dictionary<string, string> queryString, Dictionary<string, string> header, object userData = null)
```

**核心方法 — 字节数组请求：**

```csharp
// GET 请求，返回字节数组
Task<WebBufferResult> GetToBytes(string url, object userData = null)
Task<WebBufferResult> GetToBytes(string url, Dictionary<string, string> queryString, object userData = null)
Task<WebBufferResult> GetToBytes(string url, Dictionary<string, string> queryString, Dictionary<string, string> header, object userData = null)

// POST 请求，返回字节数组
Task<WebBufferResult> PostToBytes(string url, Dictionary<string, object> from, object userData = null)
Task<WebBufferResult> PostToBytes(string url, Dictionary<string, object> from, Dictionary<string, string> queryString, object userData = null)
Task<WebBufferResult> PostToBytes(string url, Dictionary<string, object> from, Dictionary<string, string> queryString, Dictionary<string, string> header, object userData = null)
```

**核心方法 — ProtoBuf 请求：**

```csharp
// 发送 ProtoBuf POST 请求
Task<T> Post<T>(string url, MessageObject message) where T : MessageObject, IResponseMessage
```

### 4.2 WebData（内部类 base）

Web 请求数据基类 `WebData`，实现 `IDisposable`。包含请求的基本信息：`IsGet`（是否 GET）、`URL`、`UserData`。

### 4.3 WebJsonData（内部类）

JSON 格式请求数据类，继承 `WebData`。内部使用 `TaskCompletionSource<WebStringResult>` 或 `TaskCompletionSource<WebBufferResult>` 管理异步结果。POST 请求的 `Form` 类型为 `Dictionary<string, object>`。

### 4.4 WebProtoBufData（内部类）

ProtoBuf 格式请求数据类，继承 `WebData`。使用 `application/x-protobuf` content type，内部 `SendData` 为 `byte[]`。

### 4.5 HttpJsonResult

HTTP JSON 消息响应结构，用于反序列化服务器返回的原始 JSON。

命名空间：`Hotfix.Framework.Web`

```csharp
public sealed class HttpJsonResult
{
    // JSON 属性名 "code"，PascalCase 为 Code
    public int Code { get; set; }

    // JSON 属性名 "message"，PascalCase 为 Message
    public string Message { get; set; }

    // JSON 属性名 "data"，PascalCase 为 Data（JSON 字符串）
    public string Data { get; set; }
}
```

### 4.6 HttpJsonResultData\<T\>

解析后的泛型结果类。

命名空间：`Hotfix.Framework.Web`

```csharp
public sealed class HttpJsonResultData<T>
{
    // 是否成功（独立属性，由 Helper 解析时设置）
    public bool IsSuccess { get; set; }

    // 响应码（0 表示成功）
    public int Code { get; set; }

    // 反序列化后的数据对象
    public T Data { get; set; }
}
```

注意：`HttpJsonResultData<T>` 没有 `Message` 属性。错误信息可通过 `Code` 判断。

### 4.7 HttpJsonResultHelper

JSON 结果解析辅助类，提供扩展方法将 JSON 字符串转换为 `HttpJsonResultData<T>`。

```csharp
// 扩展方法：将 JSON 字符串解析为泛型结果
HttpJsonResultData<T> ToHttpJsonResultData<T>(this string jsonResult) where T : class, new()
```

解析逻辑：
1. 反序列化为 `HttpJsonResult`
2. 若 `Code != 0`：`IsSuccess = false`，设置 `Code`
3. 若 `Code == 0`：`IsSuccess = true`，反序列化 `Data` 字段为 `T`

### 4.8 WebBufferResult

字节数组结果封装类。

命名空间：`Hotfix.Framework.Web`

```csharp
public sealed class WebBufferResult
{
    // 请求结果字节数组
    public byte[] Result { get; }

    // 用户自定义数据
    public object UserData { get; }
}
```

### 4.9 WebStringResult

字符串结果封装类。

命名空间：`Hotfix.Framework.Web`

```csharp
public sealed class WebStringResult
{
    // 请求结果字符串
    public string Result { get; }

    // 用户自定义数据
    public object UserData { get; }
}
```

## 5. 使用示例

### 5.1 GET 字符串请求

```csharp
using System.Threading.Tasks;
using Hotfix.Framework.Core;
using Hotfix.Framework.Web;

public class WebExample
{
    public async Task<string> FetchServerData()
    {
        // 简单的 GET 请求
        var result = await WebModule.Instance.GetToString("https://api.example.com/server/info");

        // result.Result 为服务器返回的原始字符串
        return result.Result;
    }

    // 带查询参数
    public async Task<string> FetchWithQuery()
    {
        var query = new Dictionary<string, string>
        {
            { "page", "1" },
            { "size", "20" }
        };

        var result = await WebModule.Instance.GetToString(
            "https://api.example.com/server/list",
            query
        );

        return result.Result;
    }

    // 带请求头
    public async Task<string> FetchWithHeader()
    {
        var header = new Dictionary<string, string>
        {
            { "Authorization", "Bearer xxx-token" }
        };

        var result = await WebModule.Instance.GetToString(
            "https://api.example.com/server/info",
            null,      // 无 queryString
            header
        );

        return result.Result;
    }
}
```

### 5.2 POST JSON 请求

```csharp
using System.Threading.Tasks;
using Hotfix.Framework.Web;

public async Task<string> LoginRequest()
{
    // POST 参数使用 Dictionary<string, object>
    var formData = new Dictionary<string, object>
    {
        { "username", "player1" },
        { "password", "123456" }
    };

    var result = await WebModule.Instance.PostToString(
        "https://api.example.com/auth/login",
        formData
    );

    return result.Result;
}
```

### 5.3 解析 JSON 结果为强类型

```csharp
using System.Threading.Tasks;
using Hotfix.Framework.Web;

// 定义响应数据类
public class LoginResponse
{
    public string Token { get; set; }
    public string UserName { get; set; }
}

public async Task<LoginResponse> LoginAndParse()
{
    var formData = new Dictionary<string, object>
    {
        { "username", "player1" },
        { "password", "123456" }
    };

    // 1. 获取原始 JSON 字符串
    var stringResult = await WebModule.Instance.PostToString(
        "https://api.example.com/auth/login",
        formData
    );

    // 2. 通过扩展方法解析为强类型结果
    var jsonData = stringResult.Result.ToHttpJsonResultData<LoginResponse>();

    if (jsonData.IsSuccess)
    {
        return jsonData.Data; // LoginResponse 对象
    }
    else
    {
        throw new System.Exception($"请求失败，错误码: {jsonData.Code}");
    }
}
```

### 5.4 字节数组请求

```csharp
using System.Threading.Tasks;
using System.Text;
using Hotfix.Framework.Web;

public async Task DownloadConfigFile()
{
    // GET 字节数组请求
    var bufferResult = await WebModule.Instance.GetToBytes(
        "https://cdn.example.com/asset/config.json"
    );

    // bufferResult.Result 为 byte[]
    string configText = Encoding.UTF8.GetString(bufferResult.Result);
    UnityEngine.Debug.Log($"配置文件大小: {bufferResult.Result.Length} 字节");
}
```

### 5.5 带 UserData 的请求

```csharp
using System.Threading.Tasks;
using Hotfix.Framework.Web;

public async Task FetchWithUserData()
{
    // userData 在请求时会原样传递到结果中，方便识别请求来源
    var result = await WebModule.Instance.GetToString(
        "https://api.example.com/data",
        userData: "RequestFromMainUI"
    );

    // result.UserData == "RequestFromMainUI"
    UnityEngine.Debug.Log($"请求来源: {result.UserData}");
}
```

### 5.6 ProtoBuf 请求

```csharp
using System.Threading.Tasks;
using Hotfix.Framework.Web;
using Hotfix.Framework.Network;

public async Task SendProtoBufRequest()
{
    var request = new MyRequestMessage { /* ... */ };

    // Post<T> 负责序列化、发送、接收、反序列化全过程
    MyResponseMessage response = await WebModule.Instance.Post<MyResponseMessage>(
        "https://api.example.com/proto/endpoint",
        request
    );

    if (response != null)
    {
        // 处理响应
    }
}
```

### 5.7 并发控制

```csharp
// 设置最大并发连接数
WebModule.Instance.MaxConnectionPerServer = 3;

// 超时设置（秒）
WebModule.Instance.Timeout = 10f;

// 并发发送多个请求（自动排队，控制在 MaxConnectionPerServer 以内）
var tasks = new[]
{
    WebModule.Instance.GetToString("https://api.example.com/data1"),
    WebModule.Instance.GetToString("https://api.example.com/data2"),
    WebModule.Instance.GetToString("https://api.example.com/data3"),
    WebModule.Instance.GetToString("https://api.example.com/data4"),
};

var results = await Task.WhenAll(tasks);
```

## 6. 目录结构

```text
Web/
├── WebModule.cs                    # Web 管理模块（GET/POST 字符串和字节数组）
├── WebModule.WebData.cs            # Web 请求数据基类（内部）
├── WebModule.WebJsonData.cs        # JSON 请求数据类（内部）
├── WebModule.WebProtoBufData.cs    # ProtoBuf 请求数据类（内部）
├── WebModule.ProtoBuf.cs           # ProtoBuf 请求处理
├── HttpJsonResult.cs               # HTTP JSON 原始响应结构
├── HttpJsonResultData.cs           # JSON 解析后的泛型结果数据
├── HttpJsonResultHelper.cs         # JSON 结果解析扩展方法
├── WebBufferResult.cs              # 字节数组结果封装
├── WebStringResult.cs              # 字符串结果封装
└── README.md                       # 本文档
```

## 7. 依赖

- **Hotfix.Framework.Core**：提供 `ModuleBase` 基类、`ModuleManager`
- **Hotfix.Framework.Network**：ProtoBuf 消息路由（`MessageObject`、`IResponseMessage`、`SerializerHelper`）
- **Newtonsoft.Json**（外部）：JSON 序列化/反序列化
- **AOT.Framework.Core.Log**：日志（`FuLogger`）
- **AOT.Framework.Core.Utility**：工具类（`UtilityAOT.Json.ToJson`）

## 8. 最佳实践

1. **错误处理**：使用 try-catch 捕获 `Task` 中抛出的异常（超时、网络错误等）
2. **超时设置**：根据请求类型设置合理的超时时间，通过 `Timeout` 属性配置
3. **并发控制**：合理配置 `MaxConnectionPerServer`，避免服务器压力过大
4. **强类型解析**：使用 `ToHttpJsonResultData<T>()` 扩展方法将 JSON 字符串解析为强类型对象
5. **序列化选择**：大数据量传输使用 ProtoBuf 格式（`Post<T>`），减少带宽和解析开销
6. **UserData 追踪**：利用 `userData` 参数传递请求上下文，方便在回调中识别请求来源

## 9. 注意事项

1. 所有请求方法返回 `Task<T>`（基于 `TaskCompletionSource<T>`），非 `UniTask`
2. POST 方法的 body 参数类型为 `Dictionary<string, object>`（序列化为 JSON 发送），非原始字符串
3. `HttpJsonResult` 的属性使用 PascalCase（`Code`、`Message`、`Data`），JSON 序列化时映射为小写
4. `HttpJsonResultData<T>` 的 `IsSuccess` 是独立属性（setter 为 public），由 `HttpJsonResultHelper` 设置
5. `HttpJsonResultData<T>` 没有 `Message` 属性，错误时通过 `Code` 判断
6. `WebBufferResult` / `WebStringResult` 使用 `Result` 属性（非 `Data`）
7. WebGL 平台使用 `UnityWebRequest`，非 WebGL 平台使用 `System.Net.WebRequest`
8. GET 请求的 `queryString` 参数会通过 `UrlHandler` 自动拼接到 URL 上
