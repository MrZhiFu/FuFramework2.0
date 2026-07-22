# FuFramework Storage Module

## 1. 简介

FuFramework Storage 模块是游戏框架的本地数据持久化系统，提供基于二进制序列化的键值对存储方案。该模块将数据以文件形式存储在本地磁盘，支持自动保存（脏标记机制）、加密存储和数据完整性校验。

## 2. 核心特性

- **键值对存储**：使用 `SortedDictionary<string, string>` 存储数据
- **二进制序列化**：通过 `BinaryWriter`/`BinaryReader` 进行高效二进制序列化
- **脏标记机制**：数据修改后标记为脏，自动定期保存（默认 5 分钟）
- **GMD 头标识**：序列化数据写入 "GMD" (GameData) 头部，用于格式校验
- **可选加密**：支持对存储数据进行加密
- **多文件管理**：支持创建多个存储文件，每个文件独立管理

## 3. 核心概念

### 3.1 存储架构

```
┌─────────────────────────────────────────────────────────────┐
│                     StorageModule                            │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  m_StorageHelperDict (Dictionary<string, StorageHelper>)│  │
│  │  - 按名称管理所有存储实例                             │   │
│  └─────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  DataRootDir = "GameData"                           │   │
│  │  - 数据存储根目录（Application.persistentDataPath 下）│   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    StorageHelper                            │
│  ├── FileName (文件名)                                      │
│  ├── FilePath (完整路径)                                    │
│  ├── IsDirty (脏标记)                                       │
│  └── Data (SortedDictionary<string, string>)                │
└─────────────────────────────────────────────────────────────┘
                              │
                    DataSerializer (序列化)
                              │
                    ┌─────────┴─────────┐
                    │   "GMD" + Data    │
                    │ (二进制文件)       │
                    └───────────────────┘
```

### 3.2 数据序列化格式

```
| GMD (3 bytes) | 键值对数量 (int) | Key1 | Value1 | Key2 | Value2 | ... |
```

## 4. 核心类说明

### 4.1 StorageModule

存储管理模块，继承自 `ModuleBase`。

**核心属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| `Instance` | `StorageModule` | 模块单例 |
| `Count` | `int` | 存储数据项数量 |
| `DirRoot` | `string` | 数据根目录（常量 "GameData"） |

**核心方法：**

```csharp
// 存储实例管理
StorageHelper GetOrCreateHelper(string fileName)
StorageHelper GetHelper(string fileName)
Dictionary<string, StorageHelper> GetAllHelpers()
void RemoveHelper(string fileName)
void RemoveAllHelper()

// 数据操作（快捷方式，fileName 默认 "DefaultData"）
void SetBool(string dataName, bool value, string fileName = DefaultFileName)
bool GetBool(string dataName, string fileName = DefaultFileName, bool defaultValue = false)
void SetInt(string dataName, int value, string fileName = DefaultFileName)
int GetInt(string dataName, string fileName = DefaultFileName, int defaultValue = 0)
void SetLong(string dataName, long value, string fileName = DefaultFileName)
long GetLong(string dataName, string fileName = DefaultFileName, long defaultValue = 0)
void SetFloat(string dataName, float value, string fileName = DefaultFileName)
float GetFloat(string dataName, string fileName = DefaultFileName, float defaultValue = 0)
void SetDouble(string dataName, double value, string fileName = DefaultFileName)
double GetDouble(string dataName, string fileName = DefaultFileName, double defaultValue = 0)
void SetString(string dataName, string value, string fileName = DefaultFileName)
string GetString(string dataName, string fileName = DefaultFileName, string defaultValue = null)
void SetObject<T>(string dataName, T obj, string fileName = DefaultFileName) where T : class, new()
T GetObject<T>(string dataName, string fileName = DefaultFileName) where T : class, new()
void SetObject(string dataName, object obj, string fileName = DefaultFileName)
object GetObject(string dataName, Type objectType, string fileName = DefaultFileName)

// 数据检查操作
bool HasData(string dataName, string fileName = DefaultFileName)
bool RemoveData(string dataName, string fileName = DefaultFileName)
void RemoveAllData()

// 持久化
bool Save(string fileName = DefaultFileName)
bool SaveAll()
bool Load(string fileName = DefaultFileName)
void LoadAll()
```

### 4.2 StorageHelper

存储辅助器，每个实例对应一个物理文件。

**核心方法：**

```csharp
// 数据读写
void SetBool(string dataName, bool value)
bool GetBool(string dataName, bool defaultValue = false)
void SetInt(string dataName, int value)
int GetInt(string dataName, int defaultValue = 0)
void SetLong(string dataName, long value)
long GetLong(string dataName, long defaultValue = 0)
void SetFloat(string dataName, float value)
float GetFloat(string dataName, float defaultValue = 0)
void SetDouble(string dataName, double value)
double GetDouble(string dataName, double defaultValue = 0)
void SetString(string dataName, string value)
string GetString(string dataName, string defaultValue = null)
void SetObject<T>(string dataName, T obj)
T GetObject<T>(string dataName)
void SetObject(string dataName, object obj)
object GetObject(Type objectType, string dataName)

// 数据管理
bool HasData(string dataName)
bool RemoveData(string dataName)
void RemoveAllData()

// 持久化
bool Save()        // 立即保存
bool Load()        // 从文件加载
```

### 4.3 Data

数据容器，使用 `SortedDictionary<string, string>` 存储键值对，支持二进制序列化和反序列化。

### 4.4 DataSerializer

数据序列化器，负责将 `Data` 对象序列化为二进制格式或从二进制格式反序列化。在数据开头写入 "GMD" 头部标识，用于格式校验。

## 5. 使用示例

### 5.1 基本读写

```csharp
using Hotfix.Framework.Core;
using Hotfix.Framework.Storage;

public class StorageExample
{
    private StorageModule m_StorageModule;

    public void Init()
    {
        m_StorageModule = ModuleManager.GetModule<StorageModule>();
    }

    public void SaveAndLoad()
    {
        // 使用快捷方法读写（dataName在前，fileName是最后一个参数）
        m_StorageModule.SetString("Language", "zh-CN", "Settings");
        m_StorageModule.SetInt("Volume", 80, "Settings");
        m_StorageModule.SetFloat("Sensitivity", 1.5f, "Settings");
        m_StorageModule.SetBool("FullScreen", true, "Settings");

        // 数据会自动定期保存（默认5分钟）
        // 也可以立即保存
        m_StorageModule.Save("Settings");
    }

    public void LoadSettings()
    {
        string language = m_StorageModule.GetString("Language", "Settings", "en");
        int volume = m_StorageModule.GetInt("Volume", "Settings", 100);
        float sensitivity = m_StorageModule.GetFloat("Sensitivity", "Settings", 1f);
        bool fullScreen = m_StorageModule.GetBool("FullScreen", "Settings", true);

        Debug.Log($"语言: {language}, 音量: {volume}");
    }
}
```

### 5.2 使用 StorageHelper

```csharp
// 获取或创建存储实例
var playerStorage = m_StorageModule.GetOrCreateHelper("PlayerData");

// 写入数据
playerStorage.SetString("PlayerName", "Hero");
playerStorage.SetInt("Level", 50);
playerStorage.SetInt("Gold", 9999);

// 读取数据
string name = playerStorage.GetString("PlayerName");
int level = playerStorage.GetInt("Level");

// 检查 Key
if (playerStorage.HasData("Gold"))
{
    int gold = playerStorage.GetInt("Gold");
}

// 手动保存
playerStorage.Save();
```

### 5.3 多文件管理

```csharp
// 不同用途使用不同文件
m_StorageModule.GetOrCreateHelper("Settings");      // 设置数据
m_StorageModule.GetOrCreateHelper("PlayerData");    // 玩家存档
m_StorageModule.GetOrCreateHelper("GuideCache");    // 引导缓存
m_StorageModule.GetOrCreateHelper("ServerList");    // 服务器列表

// 删除不需要的存储文件
m_StorageModule.RemoveHelper("OldData");

// 保存所有文件
m_StorageModule.SaveAll();
```

## 6. 目录结构

```text
Storage/
├── Runtime/
│   ├── StorageModule.cs           # 存储管理模块
│   ├── StorageHelper.cs           # 存储辅助器
│   ├── Data.cs                    # 数据容器 (SortedDictionary)
│   ├── DataSerializer.cs          # 数据序列化器 (GMD 头)
└── README.md                      # 本文档
```

## 7. 依赖

- **Hotfix.Framework.Core**：提供 ModuleBase 基类、Utility 工具

## 8. 最佳实践

1. **文件规划**：按数据类型划分存储文件（Settings、PlayerData、GuideCache 等）
2. **使用默认值**：读取数据时始终提供合理的默认值
3. **及时保存**：关键数据（如付费、游戏进度）修改后立即调用 `Save`
4. **避免频繁写入**：利用自动保存机制，减少磁盘 IO
5. **敏感数据加密**：用户敏感数据（如密码哈希）应启用加密存储

## 9. 注意事项

1. 数据存储在 `Application.persistentDataPath/GameData/` 目录下
2. iOS 上 iCloud 可能自动备份此目录，大文件存储需注意
3. 二进制文件格式与 `DataSerializer` 版本绑定，升级时需考虑兼容性
4. 自动保存间隔默认 5 分钟，频繁修改的数据建议手动保存
5. 所有数据以 `string` 形式存储在 `SortedDictionary` 中，数值类型会自动转换
