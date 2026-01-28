# FuFramework GetChannel Module

## 简介
FuFramework GetChannel 模块是一个跨平台的渠道号获取工具，专为Unity游戏开发设计。它提供了统一的API来获取iOS和Android平台的分发渠道信息，支持多渠道打包和渠道统计功能。

## 核心特性

- **跨平台支持**：支持iOS、Android、PC等主流平台
- **统一API**：提供简单易用的静态方法获取渠道信息
- **自动配置**：iOS平台在构建时自动配置默认渠道信息
- **灵活配置**：支持自定义渠道键名和默认值
- **原生集成**：通过原生插件实现高性能渠道信息获取

## 核心类说明

### ChannelHelper
渠道帮助类，提供静态方法获取渠道信息。
- **职责**：
  1. 提供统一的渠道获取接口
  2. 处理不同平台的渠道信息获取逻辑
  3. 支持自定义渠道键名和默认值

### PostProcessBuildHandler
iOS构建后处理类，自动配置渠道信息。
- **职责**：
  1. 在iOS构建完成后自动处理Info.plist文件
  2. 配置默认渠道信息（如果未设置）
  3. 确保渠道信息的正确性

## 平台实现机制

### Android平台
通过Android原生插件获取渠道信息：
- **实现方式**：使用Java插件读取AndroidManifest.xml中的meta-data
- **配置文件**：需要在AndroidManifest.xml中添加渠道配置
- **原生插件**：位于 `Plugins/Android/com.alianhome.getchannel.jar`

### iOS平台
通过Objective-C++原生插件获取渠道信息：
- **实现方式**：读取Info.plist文件中的渠道配置
- **自动配置**：构建时自动在Info.plist中添加默认渠道信息
- **原生插件**：位于 `Plugins/iOS/GetChannelInIOS.mm`

### PC/编辑器平台
通过文件方式获取渠道信息：
- **实现方式**：读取StreamingAssets目录下的channel.txt文件
- **配置方式**：手动创建channel.txt文件并写入渠道信息

## 使用指南

### 1. 基本使用
```csharp
using FuFramework.GetChannel.Runtime;

public class GameManager : MonoBehaviour
{
    private void Start()
    {
        // 获取默认渠道信息（使用"channel"作为键名）
        string channel = ChannelHelper.GetChannelName();
        Debug.Log($"当前渠道：{channel}");
        
        // 根据渠道信息执行不同的逻辑
        switch (channel)
        {
            case "ios_cn_appstore":
                // App Store渠道逻辑
                break;
            case "android_cn_taptap":
                // TapTap渠道逻辑
                break;
            case "default":
                // 默认渠道逻辑
                break;
        }
    }
}
```

### 2. 自定义渠道键名
```csharp
// 使用自定义键名获取渠道信息
string customChannel = ChannelHelper.GetChannelName("custom_channel_key");
Debug.Log($"自定义渠道：{customChannel}");
```

### 3. 渠道信息统计
```csharp
public class AnalyticsManager : MonoBehaviour
{
    private void Start()
    {
        // 获取渠道信息用于数据统计
        string channel = ChannelHelper.GetChannelName();
        
        // 上报渠道信息到统计平台
        Analytics.SetUserProperty("channel", channel);
        
        // 根据渠道配置不同的参数
        ConfigureByChannel(channel);
    }
    
    private void ConfigureByChannel(string channel)
    {
        // 根据渠道配置不同的游戏参数
        if (channel.Contains("taptap"))
        {
            // TapTap渠道特有配置
            GameConfig.EnableTapTapLogin = true;
        }
        else if (channel.Contains("appstore"))
        {
            // App Store渠道特有配置
            GameConfig.EnableGameCenter = true;
        }
    }
}
```

## 平台配置说明

### Android平台配置
在AndroidManifest.xml中添加渠道配置：
```xml
<application>
    <!-- 其他配置 -->
    
    <!-- 渠道配置 -->
    <meta-data
        android:name="channel"
        android:value="android_cn_taptap" />
        
    <!-- 自定义渠道键名配置 -->
    <meta-data
        android:name="custom_channel_key"
        android:value="custom_channel_value" />
</application>
```

### iOS平台配置
在Info.plist中添加渠道配置（构建时会自动添加默认配置）：
```xml
<key>channel</key>
<string>ios_cn_appstore</string>

<key>custom_channel_key</key>
<string>custom_channel_value</string>
```

### PC/编辑器平台配置
在StreamingAssets目录下创建channel.txt文件：
```
pc_steam
```

## 多渠道打包方案

### Android多渠道打包
使用Gradle或第三方工具进行多渠道打包：
```gradle
// 在build.gradle中配置多渠道
android {
    flavorDimensions "channel"
    productFlavors {
        taptap {
            dimension "channel"
            manifestPlaceholders = [CHANNEL_VALUE: "android_cn_taptap"]
        }
        appstore {
            dimension "channel"
            manifestPlaceholders = [CHANNEL_VALUE: "android_cn_appstore"]
        }
    }
}
```

### iOS多渠道打包
使用Xcode的Scheme或脚本进行多渠道打包：
```bash
# 使用脚本修改Info.plist中的渠道信息
plutil -replace channel -string "ios_cn_appstore" Info.plist
```

## 高级用法

### 1. 渠道验证和错误处理
```csharp
public class ChannelValidator : MonoBehaviour
{
    private void Start()
    {
        string channel = ChannelHelper.GetChannelName();
        
        // 验证渠道有效性
        if (IsValidChannel(channel))
        {
            Debug.Log($"有效渠道：{channel}");
        }
        else
        {
            Debug.LogWarning($"无效渠道：{channel}，使用默认渠道");
            channel = "default";
        }
    }
    
    private bool IsValidChannel(string channel)
    {
        // 定义有效的渠道列表
        string[] validChannels = {
            "ios_cn_appstore", 
            "android_cn_taptap", 
            "pc_steam", 
            "default"
        };
        
        return Array.Exists(validChannels, c => c == channel);
    }
}
```

### 2. 渠道信息缓存
```csharp
public static class ChannelCache
{
    private static string _cachedChannel;
    
    public static string GetChannel()
    {
        if (string.IsNullOrEmpty(_cachedChannel))
        {
            _cachedChannel = ChannelHelper.GetChannelName();
        }
        return _cachedChannel;
    }
    
    public static void ClearCache()
    {
        _cachedChannel = null;
    }
}
```

### 3. 多渠道AB测试
```csharp
public class ABTestManager : MonoBehaviour
{
    private void Start()
    {
        string channel = ChannelHelper.GetChannelName();
        
        // 根据渠道分配不同的AB测试组
        string abTestGroup = GetABTestGroup(channel);
        ApplyABTestConfiguration(abTestGroup);
    }
    
    private string GetABTestGroup(string channel)
    {
        // 根据渠道分配AB测试组
        if (channel.Contains("taptap"))
        {
            return "group_a";
        }
        else if (channel.Contains("appstore"))
        {
            return "group_b";
        }
        else
        {
            return "control";
        }
    }
}
```

## 性能优化建议

1. **渠道信息缓存**：频繁获取渠道信息时使用缓存机制
2. **延迟获取**：在需要时再获取渠道信息，避免不必要的调用
3. **批量处理**：多个渠道相关操作可以批量处理
4. **错误处理**：添加适当的错误处理和默认值机制

## 注意事项

- **平台兼容性**：确保在不同平台上正确配置渠道信息
- **渠道命名规范**：建议使用统一的渠道命名规范
- **默认值处理**：为渠道信息设置合理的默认值
- **构建配置**：多渠道打包时确保渠道配置正确
- **测试验证**：在不同渠道包中测试渠道获取功能

## 常见问题

### Q: 渠道信息获取失败怎么办？
A: 检查平台配置是否正确，确保渠道键名和配置文件存在。

### Q: 如何添加新的渠道？
A: 在对应平台的配置文件中添加新的渠道配置即可。

### Q: 渠道信息可以动态修改吗？
A: 渠道信息在构建时确定，运行时无法动态修改。

## 依赖说明

- **Unity引擎**：基础运行环境
- **平台原生支持**：依赖各平台的配置文件
- **StreamingAssets**：PC/编辑器平台依赖此目录

## 技术支持

如遇到问题，请检查：
1. 平台配置文件是否正确
2. 渠道键名是否匹配
3. 构建流程是否正确配置
4. 原生插件是否正常加载