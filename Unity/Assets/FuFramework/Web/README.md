# FuFramework Web 模块

## 概述

Web 模块是 FuFramework 中的网络请求管理系统，提供基于 UnityWebRequest 的 HTTP 请求功能，支持 GET/POST 请求、JSON 数据处理、ProtoBuf 协议等。该模块通过队列管理和连接数控制，实现了高效的网络请求处理机制。

### 核心特性

- **异步请求**：基于 Task 的异步编程模型
- **队列管理**：自动管理请求队列，控制并发连接数
- **多种数据格式**：支持字符串、字节数组、JSON、ProtoBuf 等数据格式
- **请求头支持**：灵活配置请求头信息
- **超时控制**：可配置的请求超时时间
- **连接数限制**：控制每个服务器的最大连接数
- **错误处理**：完善的异常处理和日志记录

## 系统架构

### 核心类说明

#### 1. WebManager (网络请求管理器)
位于 `Runtime/Web/WebManager.cs`，是 Web 模块的核心管理类，继承自 `FuModule`。

**主要功能：**
- 管理请求队列和并发连接
- 处理 GET/POST 请求
- 支持 JSON 和 ProtoBuf 数据格式
- 提供超时和连接数控制

**主要属性：**
- `float Timeout` - 请求超时时间（秒）
- `int MaxConnectionPerServer` - 每个服务器的最大连接数
- `TimeSpan RequestTimeout` - 请求超时时间

#### 2. WebStringResult (字符串结果类)
位于 `Runtime/Web/WebStringResult.cs`，封装 HTTP 请求返回的字符串数据。

**主要属性：**
- `string Result` - 请求返回的字符串结果
- `object UserData` - 用户自定义数据

#### 3. WebBufferResult (字节数组结果类)
位于 `Runtime/Web/WebBufferResult.cs`，封装 HTTP 请求返回的字节数组数据。

**主要属性：**
- `byte[] Result` - 请求返回的字节数组结果
- `object UserData` - 用户自定义数据

#### 4. HttpJsonResult (HTTP JSON 响应结构)
位于 `Runtime/Extensions/HttpJsonResult.cs`，定义标准的 HTTP JSON 响应格式。

**主要属性：**
- `int Code` - 响应码（0 表示成功）
- `string Message` - 响应消息
- `string Data` - 响应数据

#### 5. HttpJsonResultHelper (JSON 结果辅助类)
位于 `Runtime/Extensions/HttpJsonResultHelper.cs`，提供 JSON 数据处理工具方法。

**主要方法：**
- `ToHttpJsonResultData<T>()` - 将 JSON 字符串转换为强类型对象

## 快速开始

### 基本使用

```csharp
using FuFramework.Web.Runtime;

// 获取 WebManager 实例
var webManager = ModuleManager.GetModule<WebManager>();

// 发送 GET 请求获取字符串结果
var result = await webManager.GetToString("https://api.example.com/data");
Debug.Log($"响应结果: {result.Result}");

// 发送 GET 请求获取字节数组结果
var bytesResult = await webManager.GetToBytes("https://api.example.com/image");
Debug.Log($"响应数据长度: {bytesResult.Result.Length}");
```

### 带参数的请求

```csharp
// 带查询参数的 GET 请求
var queryParams = new Dictionary<string, string>
{
    ["page"] = "1",
    ["limit"] = "10"
};

var result = await webManager.GetToString("https://api.example.com/users", queryParams);

// 带请求头的请求
var headers = new Dictionary<string, string>
{
    ["Authorization"] = "Bearer token123",
    ["User-Agent"] = "MyApp/1.0"
};

var result = await webManager.GetToString("https://api.example.com/protected", null, headers);
```

## 详细使用指南

### 1. GET 请求

#### 获取字符串结果
```csharp
// 简单 GET 请求
var result = await webManager.GetToString("https://api.example.com/data");

// 带查询参数的 GET 请求
var queryParams = new Dictionary<string, string>
{
    ["category"] = "games",
    ["sort"] = "popular"
};
var result = await webManager.GetToString("https://api.example.com/products", queryParams);

// 带请求头的 GET 请求
var headers = new Dictionary<string, string>
{
    ["Accept"] = "application/json",
    ["X-API-Key"] = "your-api-key"
};
var result = await webManager.GetToString("https://api.example.com/data", null, headers);
```

#### 获取字节数组结果
```csharp
// 下载文件或二进制数据
var result = await webManager.GetToBytes("https://api.example.com/file.pdf");

// 保存文件
File.WriteAllBytes("downloaded.pdf", result.Result);
```

### 2. POST 请求

#### 发送表单数据
```csharp
// 准备表单数据
var formData = new Dictionary<string, object>
{
    ["username"] = "john_doe",
    ["password"] = "secure_password",
    ["email"] = "john@example.com"
};

// 发送 POST 请求
var result = await webManager.PostToString("https://api.example.com/register", formData);
```

#### 发送 JSON 数据
```csharp
// 准备 JSON 数据
var jsonData = new Dictionary<string, object>
{
    ["title"] = "New Post",
    ["content"] = "This is the content of the post",
    ["tags"] = new[] { "tech", "programming" }
};

// 发送 POST 请求
var result = await webManager.PostToString("https://api.example.com/posts", jsonData);
```

### 3. 用户自定义数据

```csharp
// 发送请求时附带用户数据
var userData = new { RequestId = Guid.NewGuid(), Timestamp = DateTime.Now };
var result = await webManager.GetToString("https://api.example.com/data", userData: userData);

// 在结果中获取用户数据
Debug.Log($"请求ID: {result.UserData}");
```

### 4. JSON 数据处理

```csharp
using FuFramework.Web.Runtime;

// 发送请求获取 JSON 响应
var result = await webManager.GetToString("https://api.example.com/users/1");

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

## 实际应用场景

### 1. API 数据获取

```csharp
public class UserService
{
    private readonly WebManager _webManager;
    
    public UserService()
    {
        _webManager = ModuleManager.GetModule<WebManager>();
    }
    
    public async Task<UserInfo> GetUserInfo(int userId)
    {
        var result = await _webManager.GetToString($"https://api.example.com/users/{userId}");
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
        
        var result = await _webManager.GetToString("https://api.example.com/users", queryParams);
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

### 2. 文件下载

```csharp
public class FileDownloader
{
    private readonly WebManager _webManager;
    
    public FileDownloader()
    {
        _webManager = ModuleManager.GetModule<WebManager>();
    }
    
    public async Task DownloadFile(string url, string savePath)
    {
        try
        {
            var result = await _webManager.GetToBytes(url);
            
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
        var result = await _webManager.GetToBytes(imageUrl);
        
        // 创建纹理
        var texture = new Texture2D(1, 1);
        texture.LoadImage(result.Result);
        
        return texture;
    }
}
```

### 3. 表单提交

```csharp
public class FormSubmitService
{
    private readonly WebManager _webManager;
    
    public FormSubmitService()
    {
        _webManager = ModuleManager.GetModule<WebManager>();
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
        
        var result = await _webManager.PostToString("https://api.example.com/contact", formData, headers);
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

### 4. 实时数据更新

```csharp
public class RealTimeDataService
{
    private readonly WebManager _webManager;
    
    public RealTimeDataService()
    {
        _webManager = ModuleManager.GetModule<WebManager>();
    }
    
    public async Task<StockData> GetStockPrice(string symbol)
    {
        var queryParams = new Dictionary<string, string>
        {
            ["symbol"] = symbol,
            ["interval"] = "1min"
        };
        
        var result = await _webManager.GetToString("https://api.example.com/stocks", queryParams);
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

## 配置和优化

### 1. 模块配置

```csharp
// 获取 WebManager 实例
var webManager = ModuleManager.GetModule<WebManager>();

// 配置超时时间（秒）
webManager.Timeout = 10f;

// 配置最大连接数
webManager.MaxConnectionPerServer = 16;

// 获取当前配置
Debug.Log($"超时时间: {webManager.Timeout}秒");
Debug.Log($"最大连接数: {webManager.MaxConnectionPerServer}");
Debug.Log($"请求超时: {webManager.RequestTimeout}");
```

### 2. 性能优化建议

#### 合理设置超时时间
```csharp
// 根据网络状况设置合适的超时时间
webManager.Timeout = 15f; // 15秒超时
```

#### 控制并发连接数
```csharp
// 根据服务器承受能力设置连接数
webManager.MaxConnectionPerServer = 8; // 默认值，适合大多数场景
```

#### 使用异步编程模式
```csharp
// 推荐：使用 async/await
public async Task<List<User>> GetUsersAsync()
{
    var result = await webManager.GetToString("https://api.example.com/users");
    return JsonUtility.FromJson<List<User>>(result.Result);
}

// 不推荐：阻塞线程
public List<User> GetUsers()
{
    var task = webManager.GetToString("https://api.example.com/users");
    task.Wait(); // 阻塞线程
    return JsonUtility.FromJson<List<User>>(task.Result.Result);
}
```

## API 参考

### WebManager 类

#### 属性
| 属性 | 类型 | 说明 |
|------|------|------|
| `Timeout` | `float` | 获取或设置超时时间（秒） |
| `MaxConnectionPerServer` | `int` | 获取或设置每个服务器的最大连接数 |
| `RequestTimeout` | `TimeSpan` | 获取请求超时时间 |

#### GET 请求方法
| 方法 | 返回类型 | 说明 |
|------|----------|------|
| `GetToString(string url, object userData = null)` | `Task<WebStringResult>` | 发送 GET 请求获取字符串结果 |
| `GetToBytes(string url, object userData = null)` | `Task<WebBufferResult>` | 发送 GET 请求获取字节数组结果 |
| `GetToString(string url, Dictionary<string, string> queryString, object userData = null)` | `Task<WebStringResult>` | 发送带查询参数的 GET 请求获取字符串结果 |
| `GetToBytes(string url, Dictionary<string, string> queryString, object userData = null)` | `Task<WebBufferResult>` | 发送带查询参数的 GET 请求获取字节数组结果 |
| `GetToString(string url, Dictionary<string, string> queryString, Dictionary<string, string> header, object userData = null)` | `Task<WebStringResult>` | 发送带查询参数和请求头的 GET 请求获取字符串结果 |
| `GetToBytes(string url, Dictionary<string, string> queryString, Dictionary<string, string> header, object userData = null)` | `Task<WebBufferResult>` | 发送带查询参数和请求头的 GET 请求获取字节数组结果 |

#### POST 请求方法
| 方法 | 返回类型 | 说明 |
|------|----------|------|
| `PostToString(string url, object userData = null)` | `Task<WebStringResult>` | 发送 POST 请求获取字符串结果 |
| `PostToBytes(string url, object userData = null)` | `Task<WebBufferResult>` | 发送 POST 请求获取字节数组结果 |
| `PostToString(string url, Dictionary<string, object> form, object userData = null)` | `Task<WebStringResult>` | 发送带表单数据的 POST 请求获取字符串结果 |
| `PostToBytes(string url, Dictionary<string, object> form, object userData = null)` | `Task<WebBufferResult>` | 发送带表单数据的 POST 请求获取字节数组结果 |
| `PostToString(string url, Dictionary<string, object> form, Dictionary<string, string> header, object userData = null)` | `Task<WebStringResult>` | 发送带表单数据和请求头的 POST 请求获取字符串结果 |
| `PostToBytes(string url, Dictionary<string, object> form, Dictionary<string, string> header, object userData = null)` | `Task<WebBufferResult>` | 发送带表单数据和请求头的 POST 请求获取字节数组结果 |

### WebStringResult 类

| 属性 | 类型 | 说明 |
|------|------|------|
| `Result` | `string` | 获取请求返回的字符串结果 |
| `UserData` | `object` | 获取用户自定义数据 |

### WebBufferResult 类

| 属性 | 类型 | 说明 |
|------|------|------|
| `Result` | `byte[]` | 获取请求返回的字节数组结果 |
| `UserData` | `object` | 获取用户自定义数据 |

### HttpJsonResult 类

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
    var result = await webManager.GetToString("https://api.example.com/data");
    
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

## 注意事项

### 1. 线程安全
- WebManager 是线程安全的，可以在多线程环境中使用
- 但建议在 Unity 的主线程中处理响应结果

### 2. 内存管理
- 请求结果对象会自动管理内存
- 大量请求时注意监控内存使用情况

### 3. 网络状态检查
- 在发送请求前检查网络连接状态
- 处理网络异常和超时情况

### 4. 性能考虑
- 合理设置超时时间和连接数限制
- 避免频繁的小请求，考虑批量请求
- 使用缓存减少重复请求

### 5. 安全性
- 不要在请求中传输敏感信息
- 使用 HTTPS 加密传输
- 验证服务器证书

## 常见问题解答

### Q: 如何设置自定义请求头？
A: 使用带 `header` 参数的方法，传入 `Dictionary<string, string>` 对象。

### Q: 如何处理请求超时？
A: 设置 `webManager.Timeout` 属性，并捕获 `TimeoutException`。

### Q: 如何下载大文件？
A: 使用 `GetToBytes` 方法获取字节数组，然后保存到文件。

### Q: 如何发送复杂的 JSON 数据？
A: 使用 `Dictionary<string, object>` 构建数据，模块会自动序列化为 JSON。

### Q: 如何处理服务器返回的错误码？
A: 使用 `HttpJsonResultHelper.ToHttpJsonResultData<T>()` 方法处理标准 JSON 响应格式。

### Q: 如何取消正在进行的请求？
A: 目前模块不支持直接取消请求，但可以在请求完成后忽略结果。

Web 模块为 FuFramework 提供了强大而灵活的网络请求功能，通过队列管理和连接数控制，确保了网络请求的高效性和稳定性，特别适合在游戏开发中处理各种网络通信需求。