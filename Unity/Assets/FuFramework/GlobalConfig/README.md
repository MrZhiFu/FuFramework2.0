# FuFramework GlobalConfig Module

## 简介
FuFramework GlobalConfig 模块是一个全局配置管理系统。它提供了从服务器获取和管理游戏全局配置信息的能力，支持版本检测、资源管理、AOT代码配置等核心功能。

## 核心特性

- **统一配置管理**：集中管理游戏的所有全局配置信息
- **版本检测支持**：提供App版本和资源版本的检测接口
- **AOT代码配置**：支持AOT编译时的代码列表配置
- **服务器配置**：支持从服务器动态获取配置信息
- **编辑器集成**：提供可视化的配置界面

## 核心类说明

### GlobalConfigModule
全局配置管理器，继承自 `FuModule`。
- **职责**：
  1. 管理游戏全局配置信息的存储和访问
  2. 提供配置信息的序列化和反序列化功能
  3. 支持AOT代码列表的自动解析
  4. 集成到框架模块系统中

### RequestBase
请求基类，定义通用的请求参数结构。
- **职责**：
  1. 定义通用的请求参数（语言、版本、平台等）
  2. 为具体请求类提供基础结构
  3. 支持请求参数的标准化

### ResponseGlobalInfo
全局信息响应类，定义服务器返回的配置信息结构。
- **职责**：
  1. 定义全局配置信息的响应结构
  2. 包含版本检测、AOT配置等关键信息
  3. 支持扩展内容的存储

## 配置信息结构

### 核心配置项
```csharp
public class GlobalConfigModule : FuModule
{
    // 检测App版本地址接口
    public string CheckAppVersionUrl { get; set; }
    
    // 检测资源版本地址接口
    public string CheckResourceVersionUrl { get; set; }
    
    // 主机服务地址
    public string HostServerUrl { get; set; }
    
    // AOT代码列表（JSON格式）
    public string AOTCodeList { get; set; }
    
    // AOT补充元数据列表
    public List<string> AOTCodeLists { get; }
    
    // 附加内容
    public string Content { get; set; }
}
```

### 请求参数结构
```csharp
public abstract class RequestBase
{
    public string Language { get; set; }        // 语言
    public string AppVersion { get; set; }     // 程序版本
    public string Platform { get; set; }       // 运行平台
    public string PackageName { get; set; }    // 包名
    public string Channel { get; set; }        // 渠道
    public string SubChannel { get; set; }     // 子渠道
}
```

## 使用指南

### 1. 基本配置使用
```csharp
using FuFramework.GlobalConfig.Runtime;

public class GameInitializer : MonoBehaviour
{
    private void Start()
    {
        // 获取全局配置管理器
        var configModule = GlobalModule.GlobalConfigModule;
        
        // 设置配置信息
        configModule.HostServerUrl = "https://api.yourgame.com";
        configModule.CheckAppVersionUrl = "https://api.yourgame.com/check_version";
        configModule.CheckResourceVersionUrl = "https://api.yourgame.com/check_resource";
        
        // 设置AOT代码列表
        configModule.AOTCodeList = "[\"System.Core\",\"System.Linq\",\"System.Collections\"]";
        
        // 获取配置信息
        Debug.Log($"主机地址：{configModule.HostServerUrl}");
        Debug.Log($"AOT代码列表：{configModule.AOTCodeList}");
    }
}
```

### 2. 服务器配置获取
```csharp
public class ConfigService : MonoBehaviour
{
    private async void Start()
    {
        // 创建请求对象
        var request = new RequestGlobalInfo
        {
            Language = "zh-CN",
            AppVersion = Application.version,
            Platform = Application.platform.ToString(),
            PackageName = Application.identifier,
            Channel = GlobalModule.ChannelModule.GetChannelName(),
            SubChannel = ""
        };
        
        try
        {
            // 发送配置请求到服务器
            var response = await HttpService.PostAsync<ResponseGlobalInfo>(
                "https://api.yourgame.com/global_config", 
                request
            );
            
            // 更新本地配置
            var configModule = GlobalModule.GlobalConfigModule;
            configModule.CheckAppVersionUrl = response.CheckAppVersionUrl;
            configModule.CheckResourceVersionUrl = response.CheckResourceVersionUrl;
            configModule.AOTCodeList = response.AOTCodeList;
            configModule.Content = response.Content;
            
            Debug.Log("全局配置更新成功");
        }
        catch (Exception ex)
        {
            Debug.LogError($"配置获取失败：{ex.Message}");
        }
    }
}
```

### 3. 版本检测功能
```csharp
public class VersionChecker : MonoBehaviour
{
    public async Task<bool> CheckAppVersion()
    {
        var configModule = GlobalModule.GlobalConfigModule;
        
        // 创建版本检测请求
        var request = new RequestGameAppVersion
        {
            AppVersion = Application.version,
            Platform = Application.platform.ToString(),
            Channel = GlobalModule.ChannelModule.GetChannelName()
        };
        
        try
        {
            // 发送版本检测请求
            var response = await HttpService.PostAsync<ResponseGameAppVersion>(
                configModule.CheckAppVersionUrl, 
                request
            );
            
            if (response.IsUpgrade)
            {
                // 需要更新
                if (response.IsForce)
                {
                    // 强制更新
                    ShowForceUpdateDialog(response.AppDownloadUrl, response.UpdateAnnouncement);
                    return false;
                }
                else
                {
                    // 可选更新
                    ShowOptionalUpdateDialog(response.AppDownloadUrl, response.UpdateAnnouncement);
                }
            }
            
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"版本检测失败：{ex.Message}");
            return true; // 网络错误时允许继续游戏
        }
    }
    
    private void ShowForceUpdateDialog(string downloadUrl, string announcement)
    {
        // 显示强制更新对话框
        Debug.Log($"强制更新：{announcement}");
        Application.OpenURL(downloadUrl);
        Application.Quit();
    }
    
    private void ShowOptionalUpdateDialog(string downloadUrl, string announcement)
    {
        // 显示可选更新对话框
        Debug.Log($"可选更新：{announcement}");
    }
}
```

### 4. AOT代码配置使用
```csharp
public class AOTConfigManager : MonoBehaviour
{
    private void Start()
    {
        var configModule = GlobalModule.GlobalConfigModule;
        
        // 获取AOT代码列表
        var aotCodeLists = configModule.AOTCodeLists;
        
        if (aotCodeLists != null && aotCodeLists.Count > 0)
        {
            Debug.Log("AOT代码列表：");
            foreach (var assembly in aotCodeLists)
            {
                Debug.Log($"- {assembly}");
                
                // 在AOT编译时预加载这些程序集
                PreloadAOTAssembly(assembly);
            }
        }
        
        // 处理附加内容
        if (!string.IsNullOrEmpty(configModule.Content))
        {
            var extraConfig = Utility.Json.ToObject<Dictionary<string, object>>(configModule.Content);
            ProcessExtraConfig(extraConfig);
        }
    }
    
    private void PreloadAOTAssembly(string assemblyName)
    {
        // AOT预加载逻辑
        try
        {
            var assembly = System.Reflection.Assembly.Load(assemblyName);
            Debug.Log($"预加载程序集：{assembly.FullName}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"预加载程序集失败：{assemblyName} - {ex.Message}");
        }
    }
    
    private void ProcessExtraConfig(Dictionary<string, object> config)
    {
        // 处理额外的配置信息
        foreach (var kvp in config)
        {
            Debug.Log($"配置项：{kvp.Key} = {kvp.Value}");
        }
    }
}
```

## 编辑器集成

### GlobalConfigModuleInspector
提供可视化的配置界面：
- **主机服务地址**：配置游戏服务器地址
- **版本检测接口**：配置App和资源版本检测地址
- **AOT代码列表**：配置AOT编译需要的程序集列表
- **附加内容**：配置额外的JSON格式内容

### 使用方式
1. 在Unity编辑器中创建GameObject
2. 添加 `GlobalConfigModule` 组件
3. 在Inspector面板中配置各项参数
4. 运行时通过 `GlobalModule.GlobalConfigModule` 访问配置

## 高级用法

### 1. 自定义请求和响应
```csharp
// 自定义请求类
public class CustomRequest : RequestBase
{
    public string UserId { get; set; }
    public string DeviceId { get; set; }
    public Dictionary<string, object> ExtraParams { get; set; }
}

// 自定义响应类
public class CustomResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; }
    public Dictionary<string, object> Data { get; set; }
}

// 使用自定义配置
public async Task<CustomResponse> GetCustomConfig()
{
    var request = new CustomRequest
    {
        UserId = "user123",
        DeviceId = SystemInfo.deviceUniqueIdentifier,
        ExtraParams = new Dictionary<string, object>
        {
            ["resolution"] = $"{Screen.width}x{Screen.height}",
            ["os"] = SystemInfo.operatingSystem
        }
    };
    
    return await HttpService.PostAsync<CustomResponse>(
        GlobalModule.GlobalConfigModule.HostServerUrl + "/custom_config", 
        request
    );
}
```

### 2. 配置缓存和更新机制
```csharp
public class ConfigCacheService
{
    private const string CACHE_KEY = "global_config_cache";
    private const double CACHE_EXPIRY_HOURS = 24;
    
    public async Task<ResponseGlobalInfo> GetConfigWithCache()
    {
        // 尝试从缓存读取
        var cachedConfig = LoadCachedConfig();
        if (cachedConfig != null && !IsCacheExpired(cachedConfig))
        {
            return cachedConfig;
        }
        
        // 从服务器获取最新配置
        var freshConfig = await FetchFreshConfig();
        if (freshConfig != null)
        {
            SaveConfigToCache(freshConfig);
            return freshConfig;
        }
        
        // 服务器获取失败，返回缓存（如果有）
        return cachedConfig ?? new ResponseGlobalInfo();
    }
    
    private ResponseGlobalInfo LoadCachedConfig()
    {
        var json = PlayerPrefs.GetString(CACHE_KEY, null);
        if (!string.IsNullOrEmpty(json))
        {
            return Utility.Json.ToObject<ResponseGlobalInfo>(json);
        }
        return null;
    }
    
    private void SaveConfigToCache(ResponseGlobalInfo config)
    {
        var json = Utility.Json.ToJson(config);
        PlayerPrefs.SetString(CACHE_KEY, json);
        PlayerPrefs.SetString(CACHE_KEY + "_timestamp", DateTime.Now.ToString());
        PlayerPrefs.Save();
    }
    
    private bool IsCacheExpired(ResponseGlobalInfo config)
    {
        var timestampStr = PlayerPrefs.GetString(CACHE_KEY + "_timestamp", null);
        if (DateTime.TryParse(timestampStr, out var timestamp))
        {
            return (DateTime.Now - timestamp).TotalHours > CACHE_EXPIRY_HOURS;
        }
        return true;
    }
}
```

### 3. 多环境配置支持
```csharp
public class MultiEnvironmentConfig
{
    public enum Environment
    {
        Development,
        Staging,
        Production
    }
    
    private static readonly Dictionary<Environment, string> HostUrls = new()
    {
        [Environment.Development] = "https://dev-api.yourgame.com",
        [Environment.Staging] = "https://staging-api.yourgame.com",
        [Environment.Production] = "https://api.yourgame.com"
    };
    
    public static Environment CurrentEnvironment
    {
        get
        {
#if DEVELOPMENT_BUILD
            return Environment.Development;
#elif STAGING_BUILD
            return Environment.Staging;
#else
            return Environment.Production;
#endif
        }
    }
    
    public static void ApplyEnvironmentConfig()
    {
        var configModule = GlobalModule.GlobalConfigModule;
        var hostUrl = HostUrls[CurrentEnvironment];
        
        configModule.HostServerUrl = hostUrl;
        configModule.CheckAppVersionUrl = $"{hostUrl}/check_version";
        configModule.CheckResourceVersionUrl = $"{hostUrl}/check_resource";
        
        Debug.Log($"应用 {CurrentEnvironment} 环境配置");
    }
}
```

## 性能优化建议

1. **配置缓存**：合理使用缓存机制减少服务器请求
2. **延迟加载**：在需要时再获取配置信息
3. **增量更新**：只更新变化的配置项
4. **错误处理**：添加适当的错误处理和默认值机制
5. **网络优化**：使用压缩和CDN加速配置获取

## 注意事项

- **配置安全性**：敏感配置信息应加密存储
- **版本兼容性**：确保配置结构与服务器端兼容
- **错误处理**：网络异常时应有合理的降级方案
- **缓存策略**：合理设置缓存过期时间
- **多线程安全**：配置访问需要考虑线程安全性

## 依赖模块

- **FuFramework.Core**：基础框架模块
- **FuFramework.GetChannel**：渠道信息获取模块
- **Unity引擎**：基础运行环境

## 技术支持

如遇到配置问题，请检查：
1. 网络连接是否正常
2. 服务器接口地址是否正确
3. 请求参数格式是否符合要求
4. 响应数据结构是否匹配
5. AOT代码列表格式是否正确