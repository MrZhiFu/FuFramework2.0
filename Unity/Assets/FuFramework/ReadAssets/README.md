# FuFramework ReadAssets Module

## 概述

ReadAssets 模块是 FuFramework 的 Android 平台资源读取组件，专门用于在 Android 平台上读取 APK 包内的 assets 目录中的文件。该模块通过原生 Android 插件实现高性能的资源读取操作。

### 核心特性

- **Android 专用**：专门为 Android 平台设计的资源读取工具
- **原生性能**：通过 Android Java 插件实现高性能文件读取
- **简单易用**：提供静态方法直接读取 assets 文件
- **文件检测**：支持文件存在性检查
- **二进制读取**：直接读取文件字节数据，支持各种文件格式

## 核心类说明

### BlankReadAssets

Android 平台资源读取静态类，提供统一的 assets 文件读取接口。

```csharp
public static class BlankReadAssets
```

**主要方法：**
- `Read(string path)` - 读取指定路径的文件内容
- `IsFileExists(string path)` - 检查指定路径的文件是否存在

## 技术架构

### 依赖关系

```
BlankReadAssets → AndroidJavaClass (Unity Android API)
BlankReadAssets → com.alianblank.readassets.MainActivity (Android 原生插件)
```

### 实现原理

ReadAssets 模块通过以下方式实现 Android 平台的 assets 文件读取：

1. **Unity 层**：使用 `AndroidJavaClass` 调用 Android 原生代码
2. **Android 原生层**：通过 `com.alianblank.readassets.MainActivity` 类实现文件读取
3. **文件系统**：直接访问 APK 包内的 assets 目录

### 文件路径说明

- **相对路径**：相对于 assets 目录的路径
- **示例路径**：`config/game.json` 对应 `assets/config/game.json`
- **不支持绝对路径**：只能使用相对于 assets 目录的相对路径

## 使用指南

### 1. 基础使用

#### 读取文本文件

```csharp
using FuFramework.ReadAssets.Runtime;

// 读取文本配置文件
public class ConfigManager
{
    public string LoadConfig(string configPath)
    {
        try
        {
            // 检查文件是否存在
            if (!BlankReadAssets.IsFileExists(configPath))
            {
                Debug.LogWarning($"配置文件不存在: {configPath}");
                return null;
            }
            
            // 读取文件内容
            byte[] fileBytes = BlankReadAssets.Read(configPath);
            
            if (fileBytes == null || fileBytes.Length == 0)
            {
                Debug.LogError($"文件读取失败或为空: {configPath}");
                return null;
            }
            
            // 转换为字符串
            string configContent = System.Text.Encoding.UTF8.GetString(fileBytes);
            Debug.Log($"配置文件加载成功: {configPath}");
            
            return configContent;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"配置文件读取异常: {ex.Message}");
            return null;
        }
    }
}

// 使用示例
var configManager = new ConfigManager();
string gameConfig = configManager.LoadConfig("config/game.json");
if (!string.IsNullOrEmpty(gameConfig))
{
    // 处理配置文件内容
    ProcessGameConfig(gameConfig);
}
```

#### 读取二进制文件

```csharp
using FuFramework.ReadAssets.Runtime;
using UnityEngine;

// 读取二进制资源文件
public class BinaryAssetLoader
{
    public Texture2D LoadTexture(string texturePath)
    {
        try
        {
            // 检查文件是否存在
            if (!BlankReadAssets.IsFileExists(texturePath))
            {
                Debug.LogWarning($"纹理文件不存在: {texturePath}");
                return null;
            }
            
            // 读取二进制数据
            byte[] textureData = BlankReadAssets.Read(texturePath);
            
            if (textureData == null || textureData.Length == 0)
            {
                Debug.LogError($"纹理文件读取失败: {texturePath}");
                return null;
            }
            
            // 创建纹理
            Texture2D texture = new Texture2D(2, 2);
            bool loadSuccess = texture.LoadImage(textureData);
            
            if (loadSuccess)
            {
                Debug.Log($"纹理加载成功: {texturePath}, 尺寸: {texture.width}x{texture.height}");
                return texture;
            }
            else
            {
                Debug.LogError($"纹理数据解析失败: {texturePath}");
                return null;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"纹理加载异常: {ex.Message}");
            return null;
        }
    }
    
    public AudioClip LoadAudioClip(string audioPath)
    {
        try
        {
            // 检查文件是否存在
            if (!BlankReadAssets.IsFileExists(audioPath))
            {
                Debug.LogWarning($"音频文件不存在: {audioPath}");
                return null;
            }
            
            // 读取音频数据
            byte[] audioData = BlankReadAssets.Read(audioPath);
            
            if (audioData == null || audioData.Length == 0)
            {
                Debug.LogError($"音频文件读取失败: {audioPath}");
                return null;
            }
            
            // 根据文件扩展名确定音频格式
            string extension = System.IO.Path.GetExtension(audioPath).ToLower();
            AudioType audioType = GetAudioType(extension);
            
            if (audioType == AudioType.UNKNOWN)
            {
                Debug.LogError($"不支持的音频格式: {extension}");
                return null;
            }
            
            // 创建临时文件并加载音频
            string tempPath = System.IO.Path.GetTempFileName();
            System.IO.File.WriteAllBytes(tempPath, audioData);
            
            // 使用 WWW 或 UnityWebRequest 加载音频
            // 注意：这里需要根据 Unity 版本选择合适的加载方式
            
            // 清理临时文件
            System.IO.File.Delete(tempPath);
            
            Debug.Log($"音频文件加载成功: {audioPath}");
            return null; // 实际实现需要返回加载的 AudioClip
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"音频加载异常: {ex.Message}");
            return null;
        }
    }
    
    private AudioType GetAudioType(string extension)
    {
        switch (extension)
        {
            case ".wav": return AudioType.WAV;
            case ".mp3": return AudioType.MPEG;
            case ".ogg": return AudioType.OGGVORBIS;
            default: return AudioType.UNKNOWN;
        }
    }
}

// 使用示例
var assetLoader = new BinaryAssetLoader();
Texture2D texture = assetLoader.LoadTexture("textures/icon.png");
if (texture != null)
{
    // 使用纹理
    ApplyTextureToUI(texture);
}
```

### 2. 高级用法

#### 批量文件读取

```csharp
using FuFramework.ReadAssets.Runtime;
using System.Collections.Generic;

// 批量资源管理器
public class BatchAssetManager
{
    private Dictionary<string, byte[]> m_AssetCache = new Dictionary<string, byte[]>();
    
    // 预加载多个资源
    public bool PreloadAssets(List<string> assetPaths)
    {
        bool allSuccess = true;
        
        foreach (string assetPath in assetPaths)
        {
            if (!PreloadAsset(assetPath))
            {
                Debug.LogWarning($"资源预加载失败: {assetPath}");
                allSuccess = false;
            }
        }
        
        return allSuccess;
    }
    
    // 预加载单个资源
    public bool PreloadAsset(string assetPath)
    {
        try
        {
            // 检查是否已缓存
            if (m_AssetCache.ContainsKey(assetPath))
            {
                Debug.Log($"资源已缓存: {assetPath}");
                return true;
            }
            
            // 检查文件是否存在
            if (!BlankReadAssets.IsFileExists(assetPath))
            {
                Debug.LogWarning($"资源文件不存在: {assetPath}");
                return false;
            }
            
            // 读取文件数据
            byte[] assetData = BlankReadAssets.Read(assetPath);
            
            if (assetData == null || assetData.Length == 0)
            {
                Debug.LogError($"资源读取失败: {assetPath}");
                return false;
            }
            
            // 缓存数据
            m_AssetCache[assetPath] = assetData;
            Debug.Log($"资源预加载成功: {assetPath}, 大小: {assetData.Length} 字节");
            
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"资源预加载异常: {ex.Message}");
            return false;
        }
    }
    
    // 获取已缓存的资源
    public byte[] GetCachedAsset(string assetPath)
    {
        if (m_AssetCache.TryGetValue(assetPath, out byte[] assetData))
        {
            return assetData;
        }
        
        Debug.LogWarning($"资源未缓存: {assetPath}");
        return null;
    }
    
    // 清理缓存
    public void ClearCache()
    {
        m_AssetCache.Clear();
        Debug.Log("资源缓存已清理");
    }
    
    // 清理特定资源缓存
    public void RemoveFromCache(string assetPath)
    {
        if (m_AssetCache.Remove(assetPath))
        {
            Debug.Log($"资源已从缓存移除: {assetPath}");
        }
    }
}

// 使用示例
var batchManager = new BatchAssetManager();

// 预加载多个资源
List<string> assetsToPreload = new List<string>
{
    "config/game.json",
    "textures/icon.png", 
    "audio/bgm.mp3",
    "data/level1.dat"
};

bool preloadSuccess = batchManager.PreloadAssets(assetsToPreload);
if (preloadSuccess)
{
    Debug.Log("所有资源预加载成功");
}

// 获取缓存的资源
byte[] configData = batchManager.GetCachedAsset("config/game.json");
if (configData != null)
{
    string configText = System.Text.Encoding.UTF8.GetString(configData);
    // 使用配置数据
}
```

#### 配置文件管理器

```csharp
using FuFramework.ReadAssets.Runtime;
using System.Text.Json;

// 配置文件管理器
public class ConfigFileManager
{
    // 读取 JSON 配置文件
    public T LoadJsonConfig<T>(string configPath) where T : class, new()
    {
        try
        {
            // 检查文件是否存在
            if (!BlankReadAssets.IsFileExists(configPath))
            {
                Debug.LogWarning($"JSON 配置文件不存在: {configPath}");
                return new T(); // 返回默认值
            }
            
            // 读取文件内容
            byte[] configBytes = BlankReadAssets.Read(configPath);
            
            if (configBytes == null || configBytes.Length == 0)
            {
                Debug.LogError($"JSON 配置文件读取失败: {configPath}");
                return new T();
            }
            
            // 解析 JSON
            string jsonText = System.Text.Encoding.UTF8.GetString(configBytes);
            T configObject = JsonSerializer.Deserialize<T>(jsonText);
            
            Debug.Log($"JSON 配置加载成功: {configPath}");
            return configObject ?? new T();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"JSON 配置解析异常: {ex.Message}");
            return new T();
        }
    }
    
    // 读取文本配置文件（键值对格式）
    public Dictionary<string, string> LoadKeyValueConfig(string configPath)
    {
        var configDict = new Dictionary<string, string>();
        
        try
        {
            // 检查文件是否存在
            if (!BlankReadAssets.IsFileExists(configPath))
            {
                Debug.LogWarning($"键值对配置文件不存在: {configPath}");
                return configDict;
            }
            
            // 读取文件内容
            byte[] configBytes = BlankReadAssets.Read(configPath);
            
            if (configBytes == null || configBytes.Length == 0)
            {
                Debug.LogError($"键值对配置文件读取失败: {configPath}");
                return configDict;
            }
            
            // 解析键值对
            string configText = System.Text.Encoding.UTF8.GetString(configBytes);
            string[] lines = configText.Split('\n');
            
            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();
                
                // 跳过空行和注释
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                    continue;
                
                // 解析键值对
                int separatorIndex = trimmedLine.IndexOf('=');
                if (separatorIndex > 0)
                {
                    string key = trimmedLine.Substring(0, separatorIndex).Trim();
                    string value = trimmedLine.Substring(separatorIndex + 1).Trim();
                    
                    if (!string.IsNullOrEmpty(key))
                    {
                        configDict[key] = value;
                    }
                }
            }
            
            Debug.Log($"键值对配置加载成功: {configPath}, 条目数: {configDict.Count}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"键值对配置解析异常: {ex.Message}");
        }
        
        return configDict;
    }
}

// 配置数据类示例
[System.Serializable]
public class GameConfig
{
    public string GameName { get; set; }
    public int MaxPlayers { get; set; }
    public float GameSpeed { get; set; }
    public bool EnableSound { get; set; }
}

// 使用示例
var configManager = new ConfigFileManager();

// 加载 JSON 配置
GameConfig gameConfig = configManager.LoadJsonConfig<GameConfig>("config/game.json");
Debug.Log($"游戏配置: {gameConfig.GameName}, 最大玩家: {gameConfig.MaxPlayers}");

// 加载键值对配置
Dictionary<string, string> appConfig = configManager.LoadKeyValueConfig("config/app.cfg");
if (appConfig.ContainsKey("version"))
{
    Debug.Log($"应用版本: {appConfig["version"]}");
}
```

### 3. 错误处理和监控

```csharp
using FuFramework.ReadAssets.Runtime;
using System.Collections.Generic;

// 资源读取监控器
public class AssetReadMonitor
{
    private Dictionary<string, int> m_ReadStatistics = new Dictionary<string, int>();
    private Dictionary<string, long> m_ReadSizeStatistics = new Dictionary<string, long>();
    
    // 安全的文件读取
    public byte[] SafeReadAsset(string assetPath)
    {
        try
        {
            // 记录读取统计
            RecordReadStatistics(assetPath);
            
            // 检查文件是否存在
            if (!BlankReadAssets.IsFileExists(assetPath))
            {
                Debug.LogWarning($"资源文件不存在: {assetPath}");
                return null;
            }
            
            // 读取文件
            byte[] assetData = BlankReadAssets.Read(assetPath);
            
            // 记录读取大小
            if (assetData != null)
            {
                RecordReadSize(assetPath, assetData.Length);
            }
            
            return assetData;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"资源读取异常 - 路径: {assetPath}, 错误: {ex.Message}");
            return null;
        }
    }
    
    // 批量安全读取
    public Dictionary<string, byte[]> SafeReadMultipleAssets(List<string> assetPaths)
    {
        var results = new Dictionary<string, byte[]>();
        
        foreach (string assetPath in assetPaths)
        {
            byte[] assetData = SafeReadAsset(assetPath);
            if (assetData != null)
            {
                results[assetPath] = assetData;
            }
        }
        
        return results;
    }
    
    private void RecordReadStatistics(string assetPath)
    {
        if (m_ReadStatistics.ContainsKey(assetPath))
        {
            m_ReadStatistics[assetPath]++;
        }
        else
        {
            m_ReadStatistics[assetPath] = 1;
        }
    }
    
    private void RecordReadSize(string assetPath, long size)
    {
        if (m_ReadSizeStatistics.ContainsKey(assetPath))
        {
            m_ReadSizeStatistics[assetPath] += size;
        }
        else
        {
            m_ReadSizeStatistics[assetPath] = size;
        }
    }
    
    // 获取读取统计信息
    public void PrintReadStatistics()
    {
        Debug.Log("=== 资源读取统计 ===");
        
        foreach (var kvp in m_ReadStatistics)
        {
            string assetPath = kvp.Key;
            int readCount = kvp.Value;
            long totalSize = m_ReadSizeStatistics.GetValueOrDefault(assetPath, 0);
            
            Debug.Log($"{assetPath}: 读取次数 {readCount}, 总大小 {totalSize} 字节");
        }
    }
    
    // 清理统计信息
    public void ClearStatistics()
    {
        m_ReadStatistics.Clear();
        m_ReadSizeStatistics.Clear();
        Debug.Log("读取统计已清理");
    }
}

// 使用示例
var monitor = new AssetReadMonitor();

// 安全读取资源
byte[] configData = monitor.SafeReadAsset("config/game.json");

// 批量读取
List<string> assets = new List<string> { "config/game.json", "textures/icon.png" };
var results = monitor.SafeReadMultipleAssets(assets);

// 打印统计信息
monitor.PrintReadStatistics();
```

## 平台特定说明

### Android 平台

#### 原生实现
模块使用 `com.alianblank.readassets.MainActivity` 类进行 assets 文件读取：

```java
// Android 原生实现（示例）
public class MainActivity extends Activity {
    public static byte[] readFile(String relativePath) {
        try {
            InputStream inputStream = getAssets().open(relativePath);
            ByteArrayOutputStream buffer = new ByteArrayOutputStream();
            
            int nRead;
            byte[] data = new byte[1024];
            while ((nRead = inputStream.read(data, 0, data.length)) != -1) {
                buffer.write(data, 0, nRead);
            }
            
            buffer.flush();
            return buffer.toByteArray();
        } catch (IOException e) {
            Log.e("ReadAssets", "文件读取失败: " + relativePath, e);
            return null;
        }
    }
    
    public static boolean isFileExists(String relativePath) {
        try {
            InputStream inputStream = getAssets().open(relativePath);
            inputStream.close();
            return true;
        } catch (IOException e) {
            return false;
        }
    }
}
```

#### 权限要求
在 Android 平台上，读取 assets 目录通常不需要特殊权限，因为 assets 目录是应用包的一部分。

### 其他平台

#### iOS 平台
iOS 平台需要使用不同的资源读取方式，建议使用 Unity 的 `Resources.Load` 或 `AssetBundle` 系统。

#### PC 平台
Windows 和 macOS 平台可以使用标准的文件系统 API 读取资源文件。

## 性能优化建议

### 1. 资源缓存策略

```csharp
// 实现资源缓存机制
public class AssetCacheManager
{
    private static Dictionary<string, byte[]> m_AssetCache = new Dictionary<string, byte[]>();
    
    public static byte[] GetCachedAsset(string assetPath)
    {
        if (m_AssetCache.TryGetValue(assetPath, out byte[] cachedData))
        {
            return cachedData;
        }
        
        // 从文件读取并缓存
        byte[] assetData = BlankReadAssets.Read(assetPath);
        if (assetData != null)
        {
            m_AssetCache[assetPath] = assetData;
        }
        
        return assetData;
    }
    
    public static void ClearCache()
    {
        m_AssetCache.Clear();
    }
    
    public static void RemoveFromCache(string assetPath)
    {
        m_AssetCache.Remove(assetPath);
    }
}
```

### 2. 批量读取优化

```csharp
// 批量读取减少调用次数
public class BatchAssetReader
{
    public Dictionary<string, byte[]> ReadMultipleAssets(List<string> assetPaths)
    {
        var results = new Dictionary<string, byte[]>();
        
        foreach (string path in assetPaths)
        {
            byte[] data = BlankReadAssets.Read(path);
            if (data != null)
            {
                results[path] = data;
            }
        }
        
        return results;
    }
}
```

### 3. 异步加载

```csharp
// 异步资源加载
public class AsyncAssetLoader
{
    public async System.Threading.Tasks.Task<byte[]> LoadAssetAsync(string assetPath)
    {
        return await System.Threading.Tasks.Task.Run(() =>
        {
            return BlankReadAssets.Read(assetPath);
        });
    }
}
```

## 注意事项

### 1. 平台限制
- **仅支持 Android**：该模块专门为 Android 平台设计
- **路径格式**：使用相对于 assets 目录的相对路径
- **文件大小**：避免读取过大的文件，建议分块读取

### 2. 错误处理
- **文件不存在**：使用 `IsFileExists` 方法检查文件存在性
- **读取失败**：处理读取返回的 null 值
- **异常捕获**：使用 try-catch 包装读取操作

### 3. 性能考虑
- **缓存策略**：对频繁读取的文件实现缓存机制
- **批量操作**：减少单个文件读取调用次数
- **内存管理**：及时清理不再使用的缓存数据

### 4. 开发建议
- **测试环境**：在 Android 真机上进行充分测试
- **路径管理**：统一管理资源路径常量
- **版本控制**：注意 assets 文件的版本管理

## API 参考

### BlankReadAssets 类

#### 静态方法

##### Read(string path)
```csharp
public static byte[] Read(string path)
```
**功能**：读取指定路径的文件内容

**参数**：
- `path` (string)：相对于 assets 目录的文件路径

**返回值**：
- `byte[]`：文件内容的字节数组，读取失败返回 null

**示例**：
```csharp
byte[] configData = BlankReadAssets.Read("config/game.json");
```

##### IsFileExists(string path)
```csharp
public static bool IsFileExists(string path)
```
**功能**：检查指定路径的文件是否存在

**参数**：
- `path` (string)：相对于 assets 目录的文件路径

**返回值**：
- `bool`：文件存在返回 true，否则返回 false

**示例**：
```csharp
bool exists = BlankReadAssets.IsFileExists("config/game.json");
```

## 常见问题解答

### Q: 为什么在非 Android 平台上无法使用？
A: 该模块专门为 Android 平台设计，使用 Android 原生 API 读取 assets 目录。在其他平台需要使用相应的资源加载方式。

### Q: 如何处理大文件读取？
A: 建议分块读取大文件，避免一次性读取导致内存压力。可以使用流式读取或分块处理。

### Q: 文件路径应该使用什么格式？
A: 使用相对于 assets 目录的相对路径，例如 `config/game.json` 对应 `assets/config/game.json`。

### Q: 如何提高读取性能？
A: 实现资源缓存机制，批量读取相关文件，避免频繁的单个文件读取操作。

### Q: 读取失败时如何处理？
A: 检查文件是否存在，处理返回的 null 值，使用 try-catch 捕获异常，并提供适当的错误提示。

## 总结

ReadAssets 模块为 Android 平台提供了高效的原生资源读取能力，通过简单的 API 接口实现 assets 目录文件的读取和存在性检查。该模块特别适合需要直接访问 APK 包内资源的场景，如配置文件读取、资源包管理等。

在使用时需要注意平台限制和性能优化，合理设计资源管理策略，确保应用的稳定性和性能表现。