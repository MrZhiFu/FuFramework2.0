# FuFramework Web 模块

## 1. 概述

Web 模块是 FuFramework 中的网络请求管理系统，提供基于 UnityWebRequest 的 HTTP 请求功能，支持 GET/POST 请求、JSON 数据处理、ProtoBuf 协议等。该模块通过队列管理和连接数控制，实现了高效的网络请求处理机制。

### 1.1 核心特性

- **异步请求**：基于 Task 的异步编程模型
- **队列管理**：自动管理请求队列，控制并发连接数
- **多种数据格式**：支持字符串、字节数组、JSON、ProtoBuf 等数据格式
- **请求头支持**：灵活配置请求头信息
- **超时控制**：可配置的请求超时时间
- **连接数限制**：控制每个服务器的最大连接数
- **错误处理**：完善的异常处理和日志记录

## 系统架构

### 类继承体系

```
FuModule (框架模块基类)
    ↑
WebModule (Web模块核心类)
    ├── WebData (请求数据基类)
    │   └── WebJsonData (JSON请求数据)
    │   └── WebProtoBufData (ProtoBuf请求数据)
    ├── WebStringResult (字符串结果)
    ├── WebBufferResult (字节数组结果)
    ├── HttpJsonResult (JSON响应结构)
    ├── HttpJsonResultData<T> (泛型结果数据)
    └── HttpJsonResultHelper (JSON处理辅助)
```

### 技术架构图

```
┌─────────────────────────────────────────────────────────────┐
│                      WebModule                              │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  请求队列管理                                        │   │
│  │  - m_WaitingNormalQueue (等待队列)                   │   │
│  │  - m_SendingNormalList (发送中列表)                  │   │
│  │  - m_WaitingProtoBufQueue (ProtoBuf等待队列)         │   │
│  │  - m_SendingProtoBufList (ProtoBuf发送中列表)        │   │
│  └─────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  配置属性                                            │   │
│  │  - Timeout (超时时间，默认5秒)                       │   │
│  │  - MaxConnectionPerServer (最大连接数，默认8)        │   │
│  │  - RequestTimeout (TimeSpan类型)                     │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              ↓
        ┌─────────────────────┼─────────────────────┐
        ↓                     ↓                     ↓
┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│   GET请求     │    │   POST请求    │    │  ProtoBuf    │
│  GetToString │    │ PostToString │    │     Post     │
│  GetToBytes  │    │ PostToBytes  │    │  <T> Post    │
└──────────────┘    └──────────────┘    └──────────────┘
```

### 2.2 核心类说明

#### 2.2.1 WebModule (网络请求管理器)
位于 `Runtime/Web/WebModule.cs`，是 Web 模块的核心管理类，继承自 `FuModule`。

**主要功能：**
- 管理请求队列和并发连接
- 处理 GET/POST 请求
- 支持 JSON 和 ProtoBuf 数据格式
- 提供超时和连接数控制
- 双平台支持：WebGL使用UnityWebRequest，其他平台使用HttpWebRequest

**主要属性：**
| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Timeout` | `float` | 5f | 请求超时时间（秒） |
| `MaxConnectionPerServer` | `int` | 8 | 每个服务器的最大连接数 |
| `RequestTimeout` | `TimeSpan` | - | 请求超时时间（TimeSpan） |

**核心字段：**
| 字段 | 类型 | 说明 |
|------|------|------|
| `m_UrlStr` | `StringBuilder` | URL构建器（容量256） |
| `m_WaitingNormalQueue` | `Queue<WebJsonData>` | 等待处理的普通请求队列（容量256） |
| `m_SendingNormalList` | `List<WebJsonData>` | 正在处理的普通请求列表（容量16） |
| `m_WaitingProtoBufQueue` | `Queue<WebProtoBufData>` | 等待处理的ProtoBuf请求队列（容量256） |
| `m_SendingProtoBufList` | `List<WebProtoBufData>` | 正在处理的ProtoBuf请求列表（容量16） |
| `m_MemoryStream` | `MemoryStream` | 用于存储请求和响应数据的内存流 |

#### 2.2.2 WebStringResult (字符串结果类)
位于 `Runtime/Web/WebStringResult.cs`，封装 HTTP 请求返回的字符串数据。

**主要属性：**
- `string Result` - 请求返回的字符串结果
- `object UserData` - 用户自定义数据

#### 2.2.3 WebBufferResult (字节数组结果类)
位于 `Runtime/Web/WebBufferResult.cs`，封装 HTTP 请求返回的字节数组数据。

**主要属性：**
- `byte[] Result` - 请求返回的字节数组结果
- `object UserData` - 用户自定义数据

#### 2.2.4 WebData (请求数据基类)
位于 `Runtime/Web/WebModule.WebData.cs`，是所有 Web 请求数据的抽象基类。

**主要属性：**
| 属性 | 类型 | 说明 |
|------|------|------|
| `IsGet` | `bool` | 是否为 GET 请求 |
| `URL` | `string` | 请求 URL |
| `UserData` | `object` | 用户自定义数据 |

**核心方法：**
```csharp
public virtual void Dispose() { }
```

#### 2.2.5 WebJsonData (JSON 请求数据类)
位于 `Runtime/Web/WebModule.WebJsonData.cs`，继承自 WebData，用于处理 JSON 格式的 Web 请求。

**主要属性：**
| 属性 | 类型 | 说明 |
|------|------|------|
| `Header` | `Dictionary<string, string>` | 请求头信息 |
| `Form` | `Dictionary<string, object>` | 表单数据 |
| `UniTaskCompletionStringSource` | `TaskCompletionSource<WebStringResult>` | 字符串结果的任务完成源 |
| `UniTaskCompletionBytesSource` | `TaskCompletionSource<WebBufferResult>` | 字节数组结果的任务完成源 |

**构造函数重载：**
- 用于字节数组结果的 GET/POST 请求
- 用于字符串结果的 GET/POST 请求
- 用于带表单的字符串结果 POST 请求
- 用于带表单的字节数组结果 POST 请求

#### 2.2.6 WebProtoBufData (ProtoBuf 请求数据类)
位于 `Runtime/Web/WebModule.WebProtoBufData.cs`，继承自 WebData，用于处理 Protocol Buffer 格式的 Web 请求。

**主要属性：**
| 属性 | 类型 | 说明 |
|------|------|------|
| `Task` | `TaskCompletionSource<WebBufferResult>` | 请求任务的完成源 |
| `SendData` | `byte[]` | 要发送的 ProtoBuf 序列化数据 |

#### 2.2.7 HttpJsonResult (HTTP JSON 响应结构)
位于 `Runtime/Extensions/HttpJsonResult.cs`，定义标准的 HTTP JSON 响应格式。

**主要属性：**
| 属性 | 类型 | 说明 |
|------|------|------|
| `Code` | `int` | 响应码（0 表示成功） |
| `Message` | `string` | 响应消息 |
| `Data` | `string` | 响应数据（JSON 字符串） |

**特性说明：**
- 使用 `[JsonProperty]` 特性映射 JSON 字段名
- 重载 `ToString()` 方法返回序列化后的 JSON 字符串

#### 2.2.8 HttpJsonResultData<T> (泛型结果数据类)
位于 `Runtime/Extensions/HttpJsonResultData.cs`，用于封装 HTTP 请求的返回结果。

**主要属性：**
| 属性 | 类型 | 说明 |
|------|------|------|
| `IsSuccess` | `bool` | 是否成功（默认 false） |
| `Code` | `int` | 响应码（0 表示成功） |
| `Data` | `T` | 数据对象（泛型） |

#### 2.2.9 HttpJsonResultHelper (JSON 结果辅助类)
位于 `Runtime/Extensions/HttpJsonResultHelper.cs`，提供 JSON 数据处理工具方法。

**主要方法：**
```csharp
/// <summary>
/// 将JSON字符串转换为HttpJsonResultData<T>对象
/// </summary>
/// <typeparam name="T">要反序列化为的对象类型，必须是类并具有无参数构造函数</typeparam>
/// <param name="jsonResult">包含HTTP响应的JSON字符串</param>
/// <returns>HttpJsonResultData<T>对象</returns>
public static HttpJsonResultData<T> ToHttpJsonResultData<T>(this string jsonResult) where T : class, new()
```

**处理流程：**
1. 反序列化 JSON 字符串为 HttpJsonResult 对象
2. 检查响应码是否为 0（成功）
3. 如果成功，反序列化 Data 字段为指定类型 T
4. 如果 Data 为空，返回类型 T 的默认实例
5. 捕获并记录异常信息

## 3. 快速开始

### 3.1 基本使用

```csharp
using FuFramework.Web.Runtime;

// 获取 WebModule 实例
var webModule = ModuleManager.GetModule<WebModule>();

// 发送 GET 请求获取字符串结果
var result = await webModule.GetToString("https://api.example.com/data");
Debug.Log($"响应结果: {result.Result}");

// 发送 GET 请求获取字节数组结果
var bytesResult = await webModule.GetToBytes("https://api.example.com/image");
Debug.Log($"响应数据长度: {bytesResult.Result.Length}");
```

### 3.2 带参数的请求

```csharp
// 带查询参数的 GET 请求
var queryParams = new Dictionary<string, string>
{
    ["page"] = "1",
    ["limit"] = "10"
};

var result = await webModule.GetToString("https://api.example.com/users", queryParams);

// 带请求头的请求
var headers = new Dictionary<string, string>
{
    ["Authorization"] = "Bearer token123",
    ["User-Agent"] = "MyApp/1.0"
};

var result = await webModule.GetToString("https://api.example.com/protected", null, headers);
```

## 4. 详细使用指南

### 4.1 GET 请求

#### 4.1.1 获取字符串结果
```csharp
// 简单 GET 请求
var result = await webModule.GetToString("https://api.example.com/data");

// 带查询参数的 GET 请求
var queryParams = new Dictionary<string, string>
{
    ["category"] = "games",
    ["sort"] = "popular"
};
var result = await webModule.GetToString("https://api.example.com/products", queryParams);

// 带请求头的 GET 请求
var headers = new Dictionary<string, string>
{
    ["Accept"] = "application/json",
    ["X-API-Key"] = "your-api-key"
};
var result = await webModule.GetToString("https://api.example.com/data", null, headers);
```

#### 4.1.2 获取字节数组结果
```csharp
// 下载文件或二进制数据
var result = await webModule.GetToBytes("https://api.example.com/file.pdf");

// 保存文件
File.WriteAllBytes("downloaded.pdf", result.Result);
```

### 4.2 POST 请求

#### 4.2.1 发送表单数据
```csharp
// 准备表单数据
var formData = new Dictionary<string, object>
{
    ["username"] = "john_doe",
    ["password"] = "secure_password",
    ["email"] = "john@example.com"
};

// 发送 POST 请求
var result = await webModule.PostToString("https://api.example.com/register", formData);
```

#### 4.2.2 发送 JSON 数据
```csharp
// 准备 JSON 数据
var jsonData = new Dictionary<string, object>
{
    ["title"] = "New Post",
    ["content"] = "This is the content of the post",
    ["tags"] = new[] { "tech", "programming" }
};

// 发送 POST 请求
var result = await webModule.PostToString("https://api.example.com/posts", jsonData);
```

### 4.3 用户自定义数据

```csharp
// 发送请求时附带用户数据
var userData = new { RequestId = Guid.NewGuid(), Timestamp = DateTime.Now };
var result = await webModule.GetToString("https://api.example.com/data", userData: userData);

// 在结果中获取用户数据
Debug.Log($"请求ID: {result.UserData}");
```

### 4.4 JSON 数据处理

```csharp
using FuFramework.Web.Runtime;

// 发送请求获取 JSON 响应
var result = await webModule.GetToString("https://api.example.com/users/1");

// 使用辅助方法处理 JSON 响应
var userData = result.Result.ToHttpJsonResultData<UserInfo>();

if (userData.IsSuccess)
{
    Debug.Log($"用户姓名: {userData.Data.Name}");
    Debug.Log($"用户邮箱: {userData.Data.Email}");
}
else
{
    Debug.LogError($"请求失败，错误码: {userData.Code}");
}

// 定义数据模型
public class UserInfo
{
    public string Name { get; set; }
    public string Email { get; set; }
    public int Age { get; set; }
}
```

## 5. 实际应用场景

### 5.1 API 数据获取

```csharp
public class UserService
{
    private readonly WebModule _webModule;
    
    public UserService()
    {
        _webModule = ModuleManager.GetModule<WebModule>();
    }
    
    public async Task<UserInfo> GetUserInfo(int userId)
    {
        var result = await _webModule.GetToString($"https://api.example.com/users/{userId}");
        var userData = result.Result.ToHttpJsonResultData<UserInfo>();
        
        if (userData.IsSuccess)
        {
            return userData.Data;
        }
        
        throw new Exception($"获取用户信息失败: {userData.Code}");
    }
    
    public async Task<List<UserInfo>> GetUsers(int page = 1, int limit = 20)
    {
        var queryParams = new Dictionary<string, string>
        {
            ["page"] = page.ToString(),
            ["limit"] = limit.ToString()
        };
        
        var result = await _webModule.GetToString("https://api.example.com/users", queryParams);
        var usersData = result.Result.ToHttpJsonResultData<UserListResponse>();
        
        if (usersData.IsSuccess)
        {
            return usersData.Data.Users;
        }
        
        throw new Exception($"获取用户列表失败: {usersData.Code}");
    }
}

public class UserListResponse
{
    public List<UserInfo> Users { get; set; }
    public int TotalCount { get; set; }
}
```

### 5.2 文件下载

```csharp
public class FileDownloader
{
    private readonly WebModule _webModule;
    
    public FileDownloader()
    {
        _webModule = ModuleManager.GetModule<WebModule>();
    }
    
    public async Task DownloadFile(string url, string savePath)
    {
        try
        {
            var result = await _webModule.GetToBytes(url);
            
            // 保存文件
            await File.WriteAllBytesAsync(savePath, result.Result);
            
            Debug.Log($"文件下载完成: {savePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"文件下载失败: {ex.Message}");
            throw;
        }
    }
    
    public async Task<Texture2D> DownloadImage(string imageUrl)
    {
        var result = await _webModule.GetToBytes(imageUrl);
        
        // 创建纹理
        var texture = new Texture2D(1, 1);
        texture.LoadImage(result.Result);
        
        return texture;
    }
}
```

### 5.3 表单提交

```csharp
public class FormSubmitService
{
    private readonly WebModule _webModule;
    
    public FormSubmitService()
    {
        _webModule = ModuleManager.GetModule<WebModule>();
    }
    
    public async Task<bool> SubmitContactForm(string name, string email, string message)
    {
        var formData = new Dictionary<string, object>
        {
            ["name"] = name,
            ["email"] = email,
            ["message"] = message,
            ["timestamp"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
        
        var headers = new Dictionary<string, string>
        {
            ["Content-Type"] = "application/json",
            ["X-Requested-With"] = "XMLHttpRequest"
        };
        
        var result = await _webModule.PostToString("https://api.example.com/contact", formData, headers);
        var response = result.Result.ToHttpJsonResultData<ContactResponse>();
        
        return response.IsSuccess;
    }
}

public class ContactResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
}
```

### 5.4 实时数据更新

```csharp
public class RealTimeDataService
{
    private readonly WebModule _webModule;
    
    public RealTimeDataService()
    {
        _webModule = ModuleManager.GetModule<WebModule>();
    }
    
    public async Task<StockData> GetStockPrice(string symbol)
    {
        var queryParams = new Dictionary<string, string>
        {
            ["symbol"] = symbol,
            ["interval"] = "1min"
        };
        
        var result = await _webModule.GetToString("https://api.example.com/stocks", queryParams);
        var stockData = result.Result.ToHttpJsonResultData<StockData>();
        
        if (stockData.IsSuccess)
        {
            return stockData.Data;
        }
        
        throw new Exception($"获取股票数据失败: {stockData.Code}");
    }
    
    public async Task StartRealTimeUpdates(string symbol, Action<StockData> onUpdate)
    {
        while (true)
        {
            try
            {
                var stockData = await GetStockPrice(symbol);
                onUpdate?.Invoke(stockData);
                
                // 等待 5 秒后再次请求
                await Task.Delay(5000);
            }
            catch (Exception ex)
            {
                Debug.LogError($"实时数据更新失败: {ex.Message}");
                await Task.Delay(10000); // 出错后等待更长时间
            }
        }
    }
}

public class StockData
{
    public string Symbol { get; set; }
    public decimal Price { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
    public DateTime Timestamp { get; set; }
}
```

## 6. 目录结构

```
FuFramework/Web/
├── Editor/
│   ├── FuFramework.Web.Editor.asmdef
│   └── Inspector/
│       └── WebModuleInspector.cs      # Web模块Inspector编辑器
├── Runtime/
│   ├── FuFramework.Web.Runtime.asmdef
│   ├── Web/
│   │   ├── WebModule.cs               # Web模块核心类
│   │   ├── WebModule.WebData.cs       # 请求数据基类
│   │   ├── WebModule.WebJsonData.cs   # JSON请求数据类
│   │   ├── WebModule.WebProtoBufData.cs # ProtoBuf请求数据类
│   │   ├── WebModule.ProtoBuf.cs      # ProtoBuf请求实现
│   │   ├── WebStringResult.cs         # 字符串结果类
│   │   └── WebBufferResult.cs         # 字节数组结果类
│   └── Extensions/
│       ├── HttpJsonResult.cs          # HTTP JSON响应结构
│       ├── HttpJsonResultData.cs      # 泛型结果数据类
│       └── HttpJsonResultHelper.cs    # JSON处理辅助类
└── README.md                          # 本文档
```

## 7. 依赖模块

- **Core**: 提供 FuModule 基类、FuLogger 日志、Utility 工具等
- **Network**: 提供 ProtoBuf 序列化支持 (MessageObject, IResponseMessage, SerializerHelper)
- **Newtonsoft.Json**: JSON 序列化/反序列化
- **protobuf-net**: Protocol Buffer 序列化

## 8. 配置和优化

### 8.1 模块配置

```csharp
// 获取 WebModule 实例
var webModule = ModuleManager.GetModule<WebModule>();

// 配置超时时间（秒）
webModule.Timeout = 10f;

// 配置最大连接数
webModule.MaxConnectionPerServer = 16;

// 获取当前配置
Debug.Log($"超时时间: {webModule.Timeout}秒");
Debug.Log($"最大连接数: {webModule.MaxConnectionPerServer}");
Debug.Log($"请求超时: {webModule.RequestTimeout}");
```

### 8.2 性能优化建议

#### 8.2.1 合理设置超时时间
```csharp
// 根据网络状况设置合适的超时时间
webModule.Timeout = 15f; // 15秒超时
```

#### 8.2.2 控制并发连接数
```csharp
// 根据服务器承受能力设置连接数
webModule.MaxConnectionPerServer = 8; // 默认值，适合大多数场景
```

#### 8.2.3 使用异步编程模式
```csharp
// 推荐：使用 async/await
public async Task<List<User>> GetUsersAsync()
{
    var result = await webModule.GetToString("https://api.example.com/users");
    return JsonUtility.FromJson<List<User>>(result.Result);
}

// 不推荐：阻塞线程
public List<User> GetUsers()
{
    var task = webModule.GetToString("https://api.example.com/users");
    task.Wait(); // 阻塞线程
    return JsonUtility.FromJson<List<User>>(task.Result.Result);
}
```

## 9. API 参考

### 9.1 WebModule 类

#### 9.1.1 属性
| 属性 | 类型 | 说明 |
|------|------|------|
| `Timeout` | `float` | 获取或设置超时时间（秒） |
| `MaxConnectionPerServer` | `int` | 获取或设置每个服务器的最大连接数 |
| `RequestTimeout` | `TimeSpan` | 获取请求超时时间 |

#### 9.1.2 GET 请求方法
| 方法 | 返回类型 | 说明 |
|------|----------|------|
| `GetToString(string url, object userData = null)` | `Task<WebStringResult>` | 发送 GET 请求获取字符串结果 |
| `GetToBytes(string url, object userData = null)` | `Task<WebBufferResult>` | 发送 GET 请求获取字节数组结果 |
| `GetToString(string url, Dictionary<string, string> queryString, object userData = null)` | `Task<WebStringResult>` | 发送带查询参数的 GET 请求获取字符串结果 |
| `GetToBytes(string url, Dictionary<string, string> queryString, object userData = null)` | `Task<WebBufferResult>` | 发送带查询参数的 GET 请求获取字节数组结果 |
| `GetToString(string url, Dictionary<string, string> queryString, Dictionary<string, string> header, object userData = null)` | `Task<WebStringResult>` | 发送带查询参数和请求头的 GET 请求获取字符串结果 |
| `GetToBytes(string url, Dictionary<string, string> queryString, Dictionary<string, string> header, object userData = null)` | `Task<WebBufferResult>` | 发送带查询参数和请求头的 GET 请求获取字节数组结果 |

#### 9.1.3 POST 请求方法
| 方法 | 返回类型 | 说明 |
|------|----------|------|
| `PostToString(string url, object userData = null)` | `Task<WebStringResult>` | 发送 POST 请求获取字符串结果 |
| `PostToBytes(string url, object userData = null)` | `Task<WebBufferResult>` | 发送 POST 请求获取字节数组结果 |
| `PostToString(string url, Dictionary<string, object> form, object userData = null)` | `Task<WebStringResult>` | 发送带表单数据的 POST 请求获取字符串结果 |
| `PostToBytes(string url, Dictionary<string, object> form, object userData = null)` | `Task<WebBufferResult>` | 发送带表单数据的 POST 请求获取字节数组结果 |
| `PostToString(string url, Dictionary<string, object> form, Dictionary<string, string> header, object userData = null)` | `Task<WebStringResult>` | 发送带表单数据和请求头的 POST 请求获取字符串结果 |
| `PostToBytes(string url, Dictionary<string, object> form, Dictionary<string, string> header, object userData = null)` | `Task<WebBufferResult>` | 发送带表单数据和请求头的 POST 请求获取字节数组结果 |

#### 9.1.4 ProtoBuf 请求方法
| 方法 | 返回类型 | 说明 |
|------|----------|------|
| `Post<T>(string url, MessageObject message)` | `Task<T>` | 发送 ProtoBuf 格式的 POST 请求，返回强类型结果 |

**类型约束：**
- `T : MessageObject, IResponseMessage` - 返回类型必须继承 MessageObject 并实现 IResponseMessage 接口
- `message` - 必须继承自 MessageObject

### 9.2 WebStringResult 类

| 属性 | 类型 | 说明 |
|------|------|------|
| `Result` | `string` | 获取请求返回的字符串结果 |
| `UserData` | `object` | 获取用户自定义数据 |

### 9.3 WebBufferResult 类

| 属性 | 类型 | 说明 |
|------|------|------|
| `Result` | `byte[]` | 获取请求返回的字节数组结果 |
| `UserData` | `object` | 获取用户自定义数据 |

### 9.4 HttpJsonResult 类

| 属性 | 类型 | 说明 |
|------|------|------|
| `Code` | `int` | 响应码（0 表示成功） |
| `Message` | `string` | 响应消息 |
| `Data` | `string` | 响应数据 |

### HttpJsonResultHelper 扩展方法

| 方法 | 返回类型 | 说明 |
|------|----------|------|
| `ToHttpJsonResultData<T>(this string jsonResult)` | `HttpJsonResultData<T>` | 将 JSON 字符串转换为强类型对象 |

## 错误处理和调试

### 1. 异常处理

```csharp
try
{
    var result = await webModule.GetToString("https://api.example.com/data");
    
    if (string.IsNullOrEmpty(result.Result))
    {
        Debug.LogWarning("响应结果为空");
        return;
    }
    
    // 处理响应数据
    ProcessResponse(result.Result);
}
catch (Exception ex)
{
    Debug.LogError($"网络请求失败: {ex.Message}");
    
    // 根据异常类型进行不同处理
    if (ex is TimeoutException)
    {
        Debug.LogError("请求超时，请检查网络连接");
    }
    else if (ex is UnityWebRequestException webEx)
    {
        Debug.LogError($"HTTP错误: {webEx.ResponseCode}");
    }
}
```

### 2. 调试日志

Web 模块会自动记录请求日志，包括：
- 请求 URL
- 请求头信息
- 表单数据
- 响应状态

可以在控制台查看详细的请求信息：
```
Web Request: https://api.example.com/data 
Header: {"Authorization":"Bearer token123"} 
Form: {"param1":"value1"}
```

## 高级特性

### 1. ProtoBuf 协议支持

Web 模块支持 Protocol Buffer 协议，适用于高性能、低带宽的网络通信场景。

**使用示例：**

```csharp
using FuFramework.Network.Runtime;

// 定义请求消息
public class LoginRequest : MessageObject
{
    public string Username { get; set; }
    public string Password { get; set; }
}

// 定义响应消息
public class LoginResponse : MessageObject, IResponseMessage
{
    public bool Success { get; set; }
    public string Token { get; set; }
    public string ErrorMessage { get; set; }
}

// 发送 ProtoBuf 请求
public async Task<LoginResponse> Login(string username, string password)
{
    var webModule = ModuleManager.GetModule<WebModule>();
    
    var request = new LoginRequest
    {
        Username = username,
        Password = password
    };
    
    try
    {
        var response = await webModule.Post<LoginResponse>("https://api.example.com/login", request);
        return response;
    }
    catch (Exception ex)
    {
        Debug.LogError($"登录失败: {ex.Message}");
        throw;
    }
}
```

**ProtoBuf 特点：**
- 二进制序列化，数据体积小
- 序列化/反序列化速度快
- 强类型约束，安全可靠
- 适合实时游戏通信

### 2. 双平台适配

Web 模块根据目标平台自动选择网络实现：

| 平台 | 实现方式 | 特点 |
|------|----------|------|
| WebGL | UnityWebRequest | 浏览器环境兼容 |
| 其他平台 | HttpWebRequest | 完整功能支持 |

**平台差异处理：**

```csharp
#if UNITY_WEBGL
    // WebGL 平台使用 UnityWebRequest
    var unityWebRequest = UnityWebRequest.Get(url);
    // ... WebGL 特定处理
#else
    // 其他平台使用 HttpWebRequest
    var request = WebRequest.CreateHttp(url);
    // ... 标准 HTTP 处理
#endif
```

### 3. 请求队列机制

Web 模块使用队列管理请求，确保并发控制：

```
请求流程：
1. 创建请求数据 (WebJsonData/WebProtoBufData)
2. 加入等待队列 (m_WaitingNormalQueue/m_WaitingProtoBufQueue)
3. 帧更新检查 (OnUpdate)
4. 如果连接数未满，从队列取出请求
5. 加入发送中列表 (m_SendingNormalList/m_SendingProtoBufList)
6. 执行异步请求
7. 请求完成后从列表移除
```

**队列配置：**
- 等待队列初始容量：256
- 发送中列表初始容量：16
- 最大并发连接数：可配置（默认 8）

### 4. 内存优化

**内存流复用：**
```csharp
// WebModule 使用共享 MemoryStream 处理字节数据
private readonly MemoryStream m_MemoryStream = new();

// 在 GetToBytes 请求中复用内存流
m_MemoryStream.SetLength(responseStream.Length);
m_MemoryStream.Position = 0;
await responseStream.CopyToAsync(m_MemoryStream);
var resultData = m_MemoryStream.ToArray();
```

**StringBuilder 复用：**
```csharp
// URL 构建使用共享 StringBuilder
private readonly StringBuilder m_UrlStr = new(256);

// 避免频繁的字符串拼接分配
m_UrlStr.Clear();
m_UrlStr.Append(baseUrl);
m_UrlStr.Append("?");
m_UrlStr.Append(queryString);
```

## 注意事项

### 1. 线程安全
- WebModule 使用 `lock (m_UrlStr)` 确保队列操作的线程安全
- 可以在多线程环境中安全地添加请求
- 但建议在 Unity 的主线程中处理响应结果（避免 Unity API 跨线程问题）

### 2. 内存管理
- 请求结果对象（WebStringResult/WebBufferResult）会在使用后由 GC 回收
- WebJsonData/WebProtoBufData 在模块释放时会调用 Dispose 取消未完成的任务
- 大量请求时注意监控内存使用情况，特别是字节数组结果

### 3. 网络状态检查
- 在发送请求前检查网络连接状态
- 处理网络异常和超时情况
- WebGL 平台有额外的跨域限制（CORS）

### 4. 性能考虑
- 合理设置超时时间和连接数限制
- 避免频繁的小请求，考虑批量请求
- 使用缓存减少重复请求
- 字节数组请求使用 MemoryStream 复用减少内存分配

### 5. 安全性
- 不要在请求中传输敏感信息（如明文密码）
- 使用 HTTPS 加密传输
- 验证服务器证书（生产环境）
- 注意防范中间人攻击

### 6. 超时处理
- 默认超时时间为 5 秒
- 超时后会抛出 TimeoutException
- 可以根据网络环境动态调整超时时间

```csharp
// 设置较长的超时时间（适用于慢速网络）
webModule.Timeout = 30f;

// 设置较短的超时时间（适用于快速检测）
webModule.Timeout = 3f;
```

## 常见问题解答

### Q: 如何设置自定义请求头？
A: 使用带 `header` 参数的方法，传入 `Dictionary<string, string>` 对象：
```csharp
var headers = new Dictionary<string, string>
{
    ["Authorization"] = "Bearer token123",
    ["Content-Type"] = "application/json"
};
var result = await webModule.GetToString(url, null, headers);
```

### Q: 如何处理请求超时？
A: 设置 `webModule.Timeout` 属性，并捕获 `TimeoutException`：
```csharp
webModule.Timeout = 10f; // 10秒超时
try
{
    var result = await webModule.GetToString(url);
}
catch (TimeoutException ex)
{
    Debug.LogError($"请求超时: {ex.Message}");
}
```

### Q: 如何下载大文件？
A: 使用 `GetToBytes` 方法获取字节数组，然后保存到文件：
```csharp
var result = await webModule.GetToBytes("https://example.com/file.zip");
await File.WriteAllBytesAsync("local/file.zip", result.Result);
```

### Q: 如何发送复杂的 JSON 数据？
A: 使用 `Dictionary<string, object>` 构建数据，模块会自动序列化为 JSON：
```csharp
var formData = new Dictionary<string, object>
{
    ["user"] = new { name = "John", age = 30 },
    ["items"] = new[] { "item1", "item2" }
};
var result = await webModule.PostToString(url, formData);
```

### Q: 如何处理服务器返回的错误码？
A: 使用 `HttpJsonResultHelper.ToHttpJsonResultData<T>()` 方法处理标准 JSON 响应格式：
```csharp
var result = await webModule.GetToString(url);
var data = result.Result.ToHttpJsonResultData<UserData>();
if (data.IsSuccess)
{
    // 处理成功响应
    var user = data.Data;
}
else
{
    // 处理错误，data.Code 包含错误码
    Debug.LogError($"请求失败，错误码: {data.Code}");
}
```

### Q: 如何取消正在进行的请求？
A: 目前模块不支持直接取消请求，但可以在请求完成后忽略结果。模块在释放时会自动取消所有未完成的请求（通过 `Dispose` 方法调用 `TrySetCanceled`）。

### Q: WebGL 平台有什么限制？
A: WebGL 平台有以下限制：
- 使用 UnityWebRequest 而非 HttpWebRequest
- 受浏览器跨域限制（CORS）
- 无法使用 ProtoBuf 的全部功能
- 某些请求头可能不被支持

### Q: 如何优化网络请求性能？
A: 建议采取以下优化措施：
1. 合理设置 `MaxConnectionPerServer`（默认 8）
2. 使用 ProtoBuf 替代 JSON 减少数据量
3. 复用请求结果对象，避免频繁创建
4. 批量处理请求，减少请求次数
5. 使用缓存避免重复请求

### Q: 模块支持哪些数据格式？
A: Web 模块支持以下数据格式：
- **JSON**: 通过 `GetToString/PostToString` 方法
- **字节数组**: 通过 `GetToBytes/PostToBytes` 方法
- **ProtoBuf**: 通过 `Post<T>` 方法

### Q: 如何处理 HTTPS 证书验证？
A: 对于自签名证书或测试环境，可以配置证书验证回调（非 WebGL 平台）：
```csharp
ServicePointManager.ServerCertificateValidationCallback = 
    (sender, certificate, chain, sslPolicyErrors) => true;
```
**注意**：生产环境请勿禁用证书验证！

---

Web 模块为 FuFramework 提供了强大而灵活的网络请求功能，通过队列管理和连接数控制，确保了网络请求的高效性和稳定性，特别适合在游戏开发中处理各种网络通信需求。