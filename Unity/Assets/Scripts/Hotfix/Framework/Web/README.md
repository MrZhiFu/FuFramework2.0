# FuFramework Web Module

## 1. 简介

FuFramework Web 模块是游戏框架的 HTTP 请求管理系统，基于 `UnityWebRequest` 实现高效的 Web 请求处理（全平台统一，含 WebGL）。该模块支持 JSON 字符串、字节数组和 ProtoBuf 三种请求/响应格式，提供请求队列和并发控制能力，并通过 `UniTask` 支持 async/await 异步模式。该模块实现 `ICancelAsync`（可取消异步对象），模块销毁时在途请求随之中止。

## 2. 核心特性

- **多格式支持**：JSON 字符串、字节数组（Buffer）、ProtoBuf 三种请求/响应格式
- **请求队列**：内置请求队列，支持并发控制（`MaxConnectionPerServer`）
- **异步支持**：基于 `UniTask<T>`（`UniTaskCompletionSource<T>`）的 async/await 异步请求
- **可取消异步**：实现 `ICancelAsync`（`Token` + `CancelAsync`），模块销毁时在途请求随之中止，新请求被拒绝
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
│  │  m_WaitingJsonQueue (Queue<WebJsonData>)            │   │
│  │  - JSON 请求等待队列                                 │   │
│  └─────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  m_SendingJsonList (List<WebJsonData>)              │   │
│  │  - JSON 正在处理的请求列表                           │   │
│  └─────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  m_WaitingPbQueue / m_SendingPbList                 │   │
│  │  - ProtoBuf 请求队列                                │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              │
           ┌──────────────────┼──────────────────┐
           ▼                  ▼                  ▼
   ┌────────────────┐  ┌─────────────────┐  ┌─────────────┐
   │ WebJsonDataBase │  │ WebProtoBufData │  │ WebDataBase │
   │ (JSON 基类)     │  │ (ProtoBuf请求)   │  │  (基类)      │
   │ String/Bytes    │  │                 │  │             │
   └────────────────┘  └─────────────────┘  └─────────────┘
           │                  │
           ▼                  ▼
   ┌──────────────┐  ┌──────────────┐
   │WebStringResult│ │WebBufferResult│
   └──────────────┘  └──────────────┘
```

## 4. 核心类说明

### 4.1 WebModule

Web 管理模块，继承自 `ModuleBase`，实现 `ICancelAsync`（可取消异步对象）。通过 `ModuleManager.GetModule<WebModule>()` 获取实例。请求队列在 `OnUpdate` 中轮询处理。

> **可取消异步**：`WebModule` 实现 `ICancelAsync`（`Token` + `CancelAsync`）。
> 模块销毁（`OnDispose`）时触发 `Token` 取消，在途请求随之中止；此后发起的新请求被入口检查拒绝
> （`Token.ThrowIfCancellationRequested()` 抛 `OperationCanceledException`），杜绝旧生命周期请求写回。
> 框架重启 `RestartGame` 会在重启前 `await` 各模块 `CancelAsync` 等待清理，保证重新初始化前旧生命周期零在途残留；
> `OnInit` 重建 `CancellationScope`（新 Token = 新生命周期），重启后可正常使用。

**核心属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| `Instance` | `WebModule` | 模块静态单例 |
| `Timeout` | `float` | 请求超时时间（秒），默认 5 |
| `MaxConnectionPerServer` | `int` | 每个服务器的最大并发连接数，默认 8 |
| `RequestTimeout` | `TimeSpan`（只读） | 请求超时时间的 TimeSpan 表示 |

**核心方法 — 字符串请求：**

```csharp
// GET 请求，返回字符串（token 必传：调用方生命周期取消令牌，如窗口关闭时中止）
UniTask<WebStringResult> GetToString(string url, CancellationToken token, object userData = null)
UniTask<WebStringResult> GetToString(string url, Dictionary<string, string> queryString, CancellationToken token, object userData = null)
UniTask<WebStringResult> GetToString(string url, Dictionary<string, string> queryString, Dictionary<string, string> header, CancellationToken token, object userData = null)

// POST 请求，返回字符串
UniTask<WebStringResult> PostToString(string url, Dictionary<string, object> from, CancellationToken token, object userData = null)
UniTask<WebStringResult> PostToString(string url, Dictionary<string, object> from, Dictionary<string, string> queryString, CancellationToken token, object userData = null)
UniTask<WebStringResult> PostToString(string url, Dictionary<string, object> from, Dictionary<string, string> queryString, Dictionary<string, string> header, CancellationToken token, object userData = null)
```

**核心方法 — 字节数组请求：**

```csharp
// GET 请求，返回字节数组
UniTask<WebBufferResult> GetToBytes(string url, CancellationToken token, object userData = null)
UniTask<WebBufferResult> GetToBytes(string url, Dictionary<string, string> queryString, CancellationToken token, object userData = null)
UniTask<WebBufferResult> GetToBytes(string url, Dictionary<string, string> queryString, Dictionary<string, string> header, CancellationToken token, object userData = null)

// POST 请求，返回字节数组
UniTask<WebBufferResult> PostToBytes(string url, Dictionary<string, object> from, CancellationToken token, object userData = null)
UniTask<WebBufferResult> PostToBytes(string url, Dictionary<string, object> from, Dictionary<string, string> queryString, CancellationToken token, object userData = null)
UniTask<WebBufferResult> PostToBytes(string url, Dictionary<string, object> from, Dictionary<string, string> queryString, Dictionary<string, string> header, CancellationToken token, object userData = null)
```

**核心方法 — ProtoBuf 请求：**

```csharp
// 发送 ProtoBuf POST 请求
UniTask<T> Post<T>(string url, MessageObject message, CancellationToken token) where T : MessageObject, IResponseMessage
```

> **`CancellationToken` 必传（无默认值）**：所有请求方法要求调用方提供取消令牌——窗口等生命周期所有者传 `Token`，模块内部传模块自身 `m_Scope.Token`。
> 调用方取消（如界面关闭）或模块销毁时，在途请求随之中止（抛 `OperationCanceledException`，调用方按需捕获）。

### 4.2 WebData（内部类 base）

Web 请求数据基类 `WebData`，实现 `IDisposable`。包含请求的基本信息：`IsGet`（是否 GET）、`URL`、`UserData`。

### 4.3 WebJsonDataBase（基类）与子类

JSON 请求数据基类（抽象），继承 `WebDataBase`，承载请求头 `Header` 与表单 `Form`。按结果类型拆分子类：
- `WebJsonStringData`：字符串结果（`GetToString`/`PostToString`），持有 `UniTaskCompletionSource<WebStringResult>`；
- `WebJsonBytesData`：字节数组结果（`GetToBytes`/`PostToBytes`），持有 `UniTaskCompletionSource<WebBufferResult>`。

POST 请求的 `Form` 类型为 `Dictionary<string, object>`。`OnUpdate` 出队后按子类类型分流到对应的请求处理。

### 4.4 WebProtoBufData

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
using System.Threading;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Core;
using Hotfix.Framework.Web;

public class WebExample
{
    // 调用方生命周期取消令牌（必传）。实际应从生命周期所有者获取——如窗口用 WinBase.Token、模块用自身 scope Token
    private readonly CancellationToken token = default;

    public async UniTask<string> FetchServerData()
    {
        // 简单的 GET 请求
        var result = await WebModule.Instance.GetToString("https://api.example.com/server/info", token);

        // result.Result 为服务器返回的原始字符串
        return result.Result;
    }

    // 带查询参数
    public async UniTask<string> FetchWithQuery()
    {
        var query = new Dictionary<string, string>
        {
            { "page", "1" },
            { "size", "20" }
        };

        var result = await WebModule.Instance.GetToString(
            "https://api.example.com/server/list",
            query,
            token
        );

        return result.Result;
    }

    // 带请求头
    public async UniTask<string> FetchWithHeader()
    {
        var header = new Dictionary<string, string>
        {
            { "Authorization", "Bearer xxx-token" }
        };

        var result = await WebModule.Instance.GetToString(
            "https://api.example.com/server/info",
            null,      // 无 queryString
            header,
            token
        );

        return result.Result;
    }
}
```

### 5.2 POST JSON 请求

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Web;

public async UniTask<string> LoginRequest()
{
    var token = CancellationToken.None; // 调用方生命周期取消令牌（必传）；实际取自窗口 Token 等生命周期所有者
    // POST 参数使用 Dictionary<string, object>
    var formData = new Dictionary<string, object>
    {
        { "username", "player1" },
        { "password", "123456" }
    };

    var result = await WebModule.Instance.PostToString(
        "https://api.example.com/auth/login",
        formData,
        token
    );

    return result.Result;
}
```

### 5.3 解析 JSON 结果为强类型

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Web;

// 定义响应数据类
public class LoginResponse
{
    public string Token { get; set; }
    public string UserName { get; set; }
}

public async UniTask<LoginResponse> LoginAndParse()
{
    var token = CancellationToken.None; // 调用方生命周期取消令牌（必传）
    var formData = new Dictionary<string, object>
    {
        { "username", "player1" },
        { "password", "123456" }
    };

    // 1. 获取原始 JSON 字符串
    var stringResult = await WebModule.Instance.PostToString(
        "https://api.example.com/auth/login",
        formData,
        token
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
using System.Threading;
using Cysharp.Threading.Tasks;
using System.Text;
using Hotfix.Framework.Web;

public async UniTask DownloadConfigFile()
{
    var token = CancellationToken.None; // 调用方生命周期取消令牌（必传）
    // GET 字节数组请求
    var bufferResult = await WebModule.Instance.GetToBytes(
        "https://cdn.example.com/asset/config.json",
        token
    );

    // bufferResult.Result 为 byte[]
    string configText = Encoding.UTF8.GetString(bufferResult.Result);
    UnityEngine.Debug.Log($"配置文件大小: {bufferResult.Result.Length} 字节");
}
```

### 5.5 带 UserData 的请求

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Web;

public async UniTask FetchWithUserData()
{
    var token = CancellationToken.None; // 调用方生命周期取消令牌（必传）
    // userData 在请求时会原样传递到结果中，方便识别请求来源
    var result = await WebModule.Instance.GetToString(
        "https://api.example.com/data",
        token,
        userData: "RequestFromMainUI"
    );

    // result.UserData == "RequestFromMainUI"
    UnityEngine.Debug.Log($"请求来源: {result.UserData}");
}
```

### 5.6 ProtoBuf 请求

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Web;
using Hotfix.Framework.Network;

public async UniTask SendProtoBufRequest()
{
    var token = CancellationToken.None; // 调用方生命周期取消令牌（必传）
    var request = new MyRequestMessage { /* ... */ };

    // Post<T> 负责序列化、发送、接收、反序列化全过程
    MyResponseMessage response = await WebModule.Instance.Post<MyResponseMessage>(
        "https://api.example.com/proto/endpoint",
        request,
        token
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
var token = CancellationToken.None; // 调用方生命周期取消令牌（必传）
var tasks = new[]
{
    WebModule.Instance.GetToString("https://api.example.com/data1", token),
    WebModule.Instance.GetToString("https://api.example.com/data2", token),
    WebModule.Instance.GetToString("https://api.example.com/data3", token),
    WebModule.Instance.GetToString("https://api.example.com/data4", token),
};

var results = await UniTask.WhenAll(tasks);
```

## 6. 目录结构

```text
Web/
├── WebModule.cs                    # Web 管理模块（字段/生命周期/JSON+ProtoBuf 请求处理）
├── WebModule.API.cs                # Web 管理模块公共 API（GET/POST 字符串/字节/ProtoBuf）
├── Data/                           # 请求数据类
│   ├── Base/                       # 请求数据基类
│   │   ├── WebDataBase.cs          # Web 请求数据基类
│   │   └── WebJsonDataBase.cs      # JSON 请求数据基类（抽象）
│   ├── WebJsonStringData.cs        # 字符串结果 JSON 请求数据
│   ├── WebJsonBytesData.cs         # 字节数组结果 JSON 请求数据
│   └── WebProtoBufData.cs          # ProtoBuf 请求数据
├── Result/                         # 请求结果类
│   ├── HttpJsonResultHelper.cs     # HTTP JSON 结果结构（HttpJsonResult/HttpJsonResultData）与解析扩展
│   ├── WebBufferResult.cs          # 字节数组结果封装
│   └── WebStringResult.cs          # 字符串结果封装
└── README.md                       # 本文档
```

## 7. 依赖

- **Hotfix.Framework.Core**：提供 `ModuleBase` 基类、`ModuleManager`、`ICancelAsync`/`CancellationScope`
- **Hotfix.Framework.Network**：ProtoBuf 消息路由（`MessageObject`、`IResponseMessage`、`SerializerHelper`）
- **Newtonsoft.Json**（外部）：JSON 序列化/反序列化
- **AOT.Framework.Core.Log**：日志（`FuLogger`）
- **AOT.Framework.Core.Utility**：工具类（`UtilityAOT.Json.ToJson`）
- **UniTask**：异步请求支持（`UniTaskCompletionSource<T>`）

## 8. 最佳实践

1. **错误处理**：使用 try-catch 捕获 `UniTask` 中抛出的异常（超时、网络错误等）
2. **超时设置**：根据请求类型设置合理的超时时间，通过 `Timeout` 属性配置
3. **并发控制**：合理配置 `MaxConnectionPerServer`，避免服务器压力过大
4. **强类型解析**：使用 `ToHttpJsonResultData<T>()` 扩展方法将 JSON 字符串解析为强类型对象
5. **序列化选择**：大数据量传输使用 ProtoBuf 格式（`Post<T>`），减少带宽和解析开销
6. **UserData 追踪**：利用 `userData` 参数传递请求上下文，方便在回调中识别请求来源

## 9. 注意事项

1. 所有请求方法返回 `UniTask<T>`（基于 `UniTaskCompletionSource<T>`）且 **`CancellationToken` 参数必传**（无默认值）；队列处理为 fire-and-forget（发起即返回，结果经完成回调写回 TCS），调用方应 `await` 或处理异常
2. POST 方法的 body 参数类型为 `Dictionary<string, object>`（序列化为 JSON 发送），非原始字符串
3. `HttpJsonResult` 的属性使用 PascalCase（`Code`、`Message`、`Data`），JSON 序列化时映射为小写
4. `HttpJsonResultData<T>` 的 `IsSuccess` 是独立属性（setter 为 public），由 `HttpJsonResultHelper` 设置
5. `HttpJsonResultData<T>` 没有 `Message` 属性，错误时通过 `Code` 判断
6. `WebBufferResult` / `WebStringResult` 使用 `Result` 属性（非 `Data`）
7. 全平台统一使用 `UnityWebRequest`；请求结束在完成回调中调用 `Dispose` 释放原生资源（官方强制，成败皆需）
8. GET 请求的 `queryString` 参数会通过 `UrlHandler` 自动拼接到 URL 上
9. **取消与重启**：模块销毁（`OnDispose`）后 `Token` 取消，在途请求随之中止，新请求被入口检查拒绝（`OperationCanceledException`）；`OnInit` 重建 `CancellationScope`（新 Token），重启后可正常使用
10. **超时契约**：`UnityWebRequest` 超时（`ConnectionError` + error 文本含 "timeout"）统一抛 `TimeoutException`（与旧 `HttpWebRequest` 行为一致），其余请求失败抛通用 `Exception`；超时粒度为整秒
11. **调用方取消令牌（必传）**：请求方法要求传入调用方 `CancellationToken`——窗口等生命周期所有者传 `WinBase.Token`，模块内部传模块自身 `m_Scope.Token`；调用方取消（如界面关闭）时在途请求抛 `OperationCanceledException`（与模块 `OnDispose` 取消同语义）
