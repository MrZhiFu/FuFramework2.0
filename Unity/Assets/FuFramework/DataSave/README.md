# FuFramework 数据保存模块

## 简介

数据保存模块是FuFramework2.0框架的本地数据持久化解决方案，提供简单易用的API来管理游戏的本地存档、设置和用户数据。采用模块化设计，完美集成框架的生命周期管理。

## 架构设计

### 核心组件

- **SaveModule** - 全局数据管理器，继承自`FuModule`，统筹管理所有数据文件
- **SaveHelper** - 单个数据文件辅助器，处理具体的数据读写操作
- **Data** - 数据容器，内部使用`SortedDictionary<string, string>`存储键值对
- **DataSerializer** - 自定义序列化器，采用二进制格式存储数据

### 主要特性

- **多数据类型支持**：`bool`, `int`, `long`, `float`, `double`, `string`及任意对象类型
- **多文件管理**：支持按数据文件分离存储，避免单文件过大
- **二进制序列化**：高效紧凑的二进制存储格式，包含文件头验证
- **数据加密支持**：基于AES算法的文件级加密，保护敏感数据安全
- **框架集成**：完美融入FuFramework的模块化和生命周期管理体系
- **开发工具**：提供Inspector扩展，便于调试和数据管理
- **定时自动保存**：支持配置化的自动保存功能，每个Helper独立管理自己的保存状态

## 使用方法

### 基础操作

```csharp
// 获取SaveModule实例（通过框架模块管理器）
var saveModule = ModuleManager.GetModule<SaveModule>();

// 保存基础类型数据
saveModule.SetInt("PlayerLevel", 10);
saveModule.SetBool("IsFirstTime", false);
saveModule.SetFloat("Volume", 0.8f);
saveModule.SetString("PlayerName", "Hero");

// 读取基础类型数据
int level = saveModule.GetInt("PlayerLevel");
bool isFirstTime = saveModule.GetBool("IsFirstTime", true); // 带默认值
float volume = saveModule.GetFloat("Volume", 1.0f);
string playerName = saveModule.GetString("PlayerName", "Guest");
```

### 对象序列化

```csharp
// 保存复杂对象（自动JSON序列化）
PlayerData playerInfo = new PlayerData 
{ 
    Name = "Hero", 
    Level = 10, 
    Experience = 1250 
};
saveModule.SetObject("PlayerData", playerInfo);

// 读取复杂对象
PlayerData data = saveModule.GetObject<PlayerData>("PlayerData");
```

### 数据管理

```csharp
// 检查数据是否存在
bool hasLevel = saveModule.HasData("PlayerLevel");

// 删除指定数据
bool removed = saveModule.RemoveData("TempData");

// 清空所有数据
saveModule.RemoveAllData();

// 获取所有数据项名称
string[] allKeys = saveModule.GetAllDataNames();
```

### 多文件支持

```csharp
// 创建指定文件名的数据管理器（自动创建对应的SaveHelper）
var customHelper = saveModule.GetOrCreateHelper("UserSettings");

// 对特定文件进行操作
customHelper.SetInt("GraphicsQuality", 2);
customHelper.SetBool("Fullscreen", true);
int quality = customHelper.GetInt("GraphicsQuality");

// 获取所有已加载的Helper名称
string[] helperNames = saveModule.GetAllHelperNames();
```

### 批量操作

```csharp
// 保存所有数据到磁盘
saveModule.SaveAll();

// 加载所有数据文件
saveModule.LoadAll();

// 获取当前数据文件数量
int fileCount = saveModule.Count;
```

## 数据加密功能

### 启用加密

数据保存模块支持基于AES算法的数据文件加密，可通过配置或代码启用：

```csharp
// 通过配置文件启用加密（推荐）
var dataSaveSetting = ModuleSetting.Runtime.ModuleSetting.Instance.DataSaveSetting;
dataSaveSetting.EnableEncrypt = true;  // 启用加密功能

// 代码中启用加密（针对特定文件）
var helper = saveModule.GetOrCreateHelper("SensitiveData");
helper.EnableEncryption = true;
helper.EncryptKey = "YourSecretKey123";  // 设置加密密钥
```

### 加密配置

```csharp
// 获取加密状态
bool isEncrypted = helper.EnableEncryption;

// 修改加密密钥
helper.EncryptKey = "NewSecretKey456";

// 禁用加密（已加密的数据将无法正确读取）
helper.EnableEncryption = false;
```

### 加密最佳实践

1. **密钥管理**：使用足够复杂的密钥，建议长度不少于16个字符
2. **重要数据加密**：仅对敏感数据（用户信息、配置设置等）启用加密
3. **性能考虑**：加密会增加少量性能开销，普通数据无需加密
4. **向后兼容**：新功能默认不启用加密，确保与现有数据兼容

```csharp
// 示例：加密用户设置数据
var settingsHelper = saveModule.GetOrCreateHelper("UserSettings");
settingsHelper.EnableEncryption = true;
settingsHelper.EncryptKey = "UserSettingsKey_2024";

// 保存敏感数据
settingsHelper.SetString("UserEmail", "user@example.com");
settingsHelper.SetString("AccessToken", "encrypted_token_value");
settingsHelper.SetBool("RememberPassword", true);
settingsHelper.Save();

// 读取加密数据（自动解密）
settingsHelper.Load();
string email = settingsHelper.GetString("UserEmail");
string token = settingsHelper.GetString("AccessToken");
bool rememberPwd = settingsHelper.GetBool("RememberPassword");
```

### 安全注意事项

- 加密后的数据文件将无法直接查看内容
- 修改加密状态会导致已有数据无法读取
- 密钥变更会使原有加密数据失效
- 建议定期备份重要数据
```

## 文件结构

### 默认配置
- **根目录**: `GameData`
- **文件扩展名**: 无扩展名（纯文件名）
- **默认文件**: `DefaultData`
- **存储位置**: `Application.persistentDataPath/GameData/`

### 文件格式
- 二进制序列化，包含自定义文件头验证
- 7位编码整数压缩，优化文件大小
- UTF-8编码，支持字符串数据
- 可选AES加密，保护敏感数据安全

## 框架集成

### 模块依赖
```csharp
[ModuleDependency] // 无外部依赖，可独立使用
public sealed class SaveModule : FuModule
```

### 生命周期管理
```csharp
// 模块初始化时自动加载所有数据
public override void OnInit()
{
    LoadAll();
}

// 模块关闭时自动保存所有数据
public override void OnShutdown()
{
    SaveAll();
}

// 每帧更新（驱动所有Helper的自动保存逻辑）
public override void OnUpdate(float elapseSeconds, float realElapseSeconds)
{
    if (!EnableAutoSave) return;

    // 驱动所有Helper的自动保存逻辑
    foreach (var helper in m_Helpers.Values)
    {
        if (helper.EnableAutoSave && helper.IsDirty)
        {
            helper.UpdateAutoSave();
        }
    }
}
```

## 开发工具

### Inspector扩展
- 运行时显示当前数据文件数量
- 列出所有已加载的Helper及其数据项数量
- "清除所有数据"按钮，便于调试
- 可视化数据管理界面

### 日志系统
- 详细的操作日志，包含成功/失败状态
- 异常捕获和警告消息
- 文件级别的操作跟踪

## 性能优化

### 内存管理
- 延迟加载：数据文件仅在访问时加载
- 基于字典的快速数据项查找
- SortedDictionary保持数据有序性

### 性能优化

- 二进制格式最小化文件大小
- 数值类型的7位编码
- 批量操作减少I/O开销
- 加密功能按需启用，避免不必要的性能开销

### 最佳实践
1. **分组相关数据**: 为不同数据类别使用单独的文件
2. **避免频繁保存**: 批量修改修改并定期保存
3. **使用合适类型**: 选择最适合的数据类型
4. **处理默认值**: 始终提供有意义的默认值
5. **清理数据**: 删除未使用的数据以保持文件紧凑
6. **加密策略**: 仅对敏感数据启用加密，平衡安全性与性能
7. **密钥管理**: 使用强密钥并妥善保管，避免硬编码在代码中

## 错误处理

### 内置保护机制
- 类型转换验证，提供详细的错误消息
- 文件I/O异常处理
- 通过文件头进行数据完整性验证
- 优雅地回退到默认值

### 常见问题
- **文件损坏**: 自动验证和恢复尝试
- **类型不匹配**: 清晰的错误消息和转换详情
- **文件缺失**: 自动创建新的数据文件
- **权限问题**: 适当的异常处理和用户反馈
- **加密错误**: 密钥错误或不匹配时提供详细错误信息
- **加密状态变更**: 修改加密配置时自动处理兼容性问题

## 安装

数据保存模块已集成到FuFramework2.0框架中，无需单独安装。确保在框架配置中启用该模块即可使用。

## 依赖项

- **FuFramework.Core**: 框架核心模块
- **FuFramework.Core.Utility.Encryption**: 加密功能模块（AES加密支持）
- **Unity Engine**: 2019.4 LTS或更高版本
- **.NET Standard 2.0**: 兼容的脚本运行时

## 许可证

此模块作为FuFramework2.0的一部分，遵循框架的整体许可协议。

### 定时自动保存

```csharp
// 配置自动保存（默认启用，间隔5分钟）
saveModule.EnableAutoSave = true;
saveModule.AutoSaveInterval = 300f; // 5分钟

// 获取有未保存数据的Helper数量
int dirtyCount = saveModule.GetDirtyHelperCount();

// 强制保存所有有未保存数据的Helper
saveModule.ForceSaveAllDirty();

// 检查指定Helper是否有未保存数据
var helper = saveModule.GetHelper("UserSettings");
if (helper != null && helper.IsDirty)
{
    Debug.Log("UserSettings 有未保存的数据");
}
```