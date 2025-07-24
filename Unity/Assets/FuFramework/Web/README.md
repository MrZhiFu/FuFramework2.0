## HOMEPAGE

GameFrameX 的Web 请求组件

**Web 请求 组件 (Web Request)** - 提供使用短连接的功能，可以用 Get 或者 Post 方法向服务器发送请求并获取响应数据，可指定允许几个 Web 请求器进行同时请求。

# 使用文档(文档编写于GPT4)

# WebComponent 说明文档

`WebComponent` 类是一个游戏框架组件，用作处理网络请求的模块。它提供了一系列方法来发送 GET 和 POST 请求，并获取返回的字符串或字节数组数据。以下是该类的详细说明和使用方法。

## 功能概述

- 初始化网络管理器
- 发送 GET 请求并获取返回的字符串或字节数组
- 发送 POST 请求并获取返回的字符串或字节数组

## 方法说明

### Awake

初始化游戏框架组件。创建 `WebManager` 实例并获取网络管理器模块。

```csharp
protected override void Awake() { /* 方法体省略 */ }
```

### GetToString（重载1）

发送 GET 请求并以字符串形式获取响应。

```csharp
public Task<string> GetToString(string url) { /* 方法体省略 */ }
```

### GetToString（重载2）

发送带参数的 GET 请求并以字符串形式获取响应。

```csharp
public Task<string> GetToString(string url, Dictionary<string, string> queryString) { /* 方法体省略 */ }
```

### GetToString（重载3）

发送带参数和请求头的 GET 请求并以字符串形式获取响应。

```csharp
public Task<string> GetToString(string url, Dictionary<string, string> queryString, Dictionary<string, string> header) { /* 方法体省略 */ }
```

### GetToBytes（重载1）

发送 GET 请求并以字节数组形式获取响应。

```csharp
public Task<byte[]> GetToBytes(string url) { /* 方法体省略 */ }
```

### GetToBytes（重载2）

发送带参数的 GET 请求并以字节数组形式获取响应。

```csharp
public Task<byte[]> GetToBytes(string url, Dictionary<string, string> queryString) { /* 方法体省略 */ }
```

### GetToBytes（重载3）

发送带参数和请求头的 GET 请求并以字节数组形式获取响应。

```csharp
public Task<byte[]> GetToBytes(string url, Dictionary<string, string> queryString, Dictionary<string, string> header) { /* 方法体省略 */ }
```

### PostToString（重载1）

发送 POST 请求并以字符串形式获取响应。

```csharp
public Task<string> PostToString(string url, Dictionary<string, string> from = null) { /* 方法体省略 */ }
```

### PostToString（重载2）

发送带表单和 URL 请求参数的 POST 请求并以字符串形式获取响应。

```csharp
public Task<string> PostToString(string url, Dictionary<string, string> from, Dictionary<string, string> queryString) { /* 方法体省略 */ }
```

### PostToString（重载3）

发送带表单、URL 请求参数和请求头的 POST 请求并以字符串形式获取响应。

```csharp
public Task<string> PostToString(string url, Dictionary<string, string> from, Dictionary<string, string> queryString, Dictionary<string, string> header) { /* 方法体省略 */ }
```

### PostToBytes（重载1）

发送 POST 请求并以字节数组形式获取响应。

```csharp
public Task<byte[]> PostToBytes(string url, Dictionary<string, string> from) { /* 方法体省略 */ }
```

### PostToBytes（重载2）

发送带表单和 URL 请求参数的 POST 请求并以字节数组形式获取响应。

```csharp
public Task<byte[]> PostToBytes(string url, Dictionary<string, string> from, Dictionary<string, string> queryString) { /* 方法体省略 */ }
```

### PostToBytes（重载3）

发送带表单、URL 请求参数和请求头的 POST 请求并以字节数组形式获取响应。

```csharp
public Task<byte[]> PostToBytes(string url, Dictionary<string, string> from, Dictionary<string, string> queryString, Dictionary<string, string> header) { /* 方法体省略 */ }
```

## 使用示例

1. 调用 `GetToString` 方法获取不带参数的 GET 请求响应字符串：

```csharp
Task<string> response = webComponent.GetToString("http://example.com/api/values");
```

2. 使用 `PostToBytes` 方法发送带表单参数的 POST 请求，并以字节数组接收响应：

```csharp
Dictionary<string, string> formData = new Dictionary<string, string>
{
    { "param1", "value1" },
    { "param2", "value2" }
};
Task<byte[]> responseBytes = webComponent.PostToBytes("http://example.com/api/upload", formData);
```

## 注意事项

确保在网络请求期间合适地处理任务，例如使用 `await` 异步等待结果。

# 使用方式(任选其一)

1. 直接在 `manifest.json` 的文件中的 `dependencies` 节点下添加以下内容
   ```json
      {"com.gameframex.unity.web": "https://github.com/AlianBlank/com.gameframex.unity.web.git"}
    ```
2. 在Unity 的`Packages Manager` 中使用`Git URL` 的方式添加库,地址为：https://github.com/AlianBlank/com.gameframex.unity.web.git

3. 直接下载仓库放置到Unity 项目的`Packages` 目录下。会自动加载识别