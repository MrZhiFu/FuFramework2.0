# FuFramework DataSave Module

## 1. 简介

**FuFramework DataSave** 模块是框架的本地数据持久化解决方案，提供简单易用的 API 来管理游戏的本地存档、设置和用户数据。采用模块化设计，完美集成框架的生命周期管理。

---

## 2. 特性

- **多数据类型支持**：`bool`、`int`、`long`、`float`、`double`、`string` 及任意对象类型（JSON 序列化）
- **多文件管理**：支持按数据文件分离存储，避免单文件过大，便于数据分类管理
- **二进制序列化**：高效紧凑的二进制存储格式，包含文件头验证（GMD 标识）
- **数据加密支持**：基于 AES 算法的文件级加密，保护敏感数据安全
- **自动保存机制**：支持配置化的自动保存功能，可设置保存间隔时间
- **脏数据检测**：智能检测数据变更，避免不必要的磁盘写入
- **框架集成**：完美融入 FuFramework 的模块化和生命周期管理体系
- **开发工具**：提供 Inspector 扩展，便于运行时调试和数据管理

---

## 3. 核心类说明

### 3.1 DataSaveModule

数据保存管理模块，继承自 `FuModule`，是数据保存的核心管理类。

**主要功能：**
- 统一管理所有数据文件（DataSaveHelper）
- 提供便捷的数据读写 API
- 支持自动保存机制
- 模块生命周期管理（初始化加载、退出时保存）

**配置参数（从 ModuleSetting 读取）：**
- `EnableAutoSave`: 是否启用自动保存
- `AutoSaveInterval`: 自动保存间隔（秒，默认 300 秒即 5 分钟）
- `EnableEncrypt`: 是否启用加密
- `EncryptKey`: 加密密钥

**主要方法：**

```csharp
// 获取或创建指定文件的数据辅助器
DataSaveHelper GetOrCreateHelper(string fileName)

// 获取指定文件的数据辅助器
DataSaveHelper GetHelper(string fileName)

// 获取所有数据辅助器
Dictionary<string, DataSaveHelper> GetAllHelpers()

// 加载/保存指定文件的数据
bool Load(string fileName = "DefaultData")
bool Save(string fileName = "DefaultData")

// 加载/保存所有数据文件
void LoadAll()
void SaveAll()

// 移除指定文件的数据辅助器
void RemoveHelper(string fileName)

// 清空所有辅助器及数据文件
void RemoveAllHelper()

// 获取有未保存数据的 Helper 数量
int GetDirtyHelperCount()

// 数据项操作（快捷方法，操作默认文件）
bool HasData(string dataName, string fileName = "DefaultData")
bool RemoveData(string dataName, string fileName = "DefaultData")
void RemoveAllData()

// 读取数据（支持默认值）
bool GetBool(string dataName, string fileName = "DefaultData", bool defaultValue = false)
int GetInt(string dataName, string fileName = "DefaultData", int defaultValue = 0)
long GetLong(string dataName, string fileName = "DefaultData", long defaultValue = 0)
float GetFloat(string dataName, string fileName = "DefaultData", float defaultValue = 0)
double GetDouble(string dataName, string fileName = "DefaultData", double defaultValue = 0)
string GetString(string dataName, string fileName = "DefaultData", string defaultValue = null)
T GetObject<T>(string dataName, string fileName = "DefaultData") where T : class, new()
object GetObject(string dataName, Type objectType, string fileName = "DefaultData")

// 写入数据
void SetBool(string dataName, bool value, string fileName = "DefaultData")
void SetInt(string dataName, int value, string fileName = "DefaultData")
void SetLong(string dataName, long value, string fileName = "DefaultData")
void SetFloat(string dataName, float value, string fileName = "DefaultData")
void SetDouble(string dataName, double value, string fileName = "DefaultData")
void SetString(string dataName, string value, string fileName = "DefaultData")
void SetObject<T>(string dataName, T obj, string fileName = "DefaultData")
```

---

### 3.2 DataSaveHelper

数据存储辅助器，继承自 `MonoBehaviour`，每个实例对应一个特定的数据文件。

**主要功能：**
- 管理单个数据文件的生命周期
- 提供数据读写接口
- 实现自动保存逻辑
- 支持数据加密/解密

**核心属性：**

```csharp
string FileName { get; }                    // 文件名
string FilePath { get; }                    // 完整文件路径
Data Data { get; }                          // 数据容器
bool IsDirty { get; }                       // 是否有未保存的修改
bool EnableAutoSave { get; set; }            // 是否启用自动保存
float AutoSaveInterval { get; set; }         // 自动保存间隔（秒）
bool EnableEncryption { get; private set; }  // 是否启用加密
string EncryptKey { get; private set; }      // 加密密钥
int Count { get; }                          // 数据项数量
```

**主要方法：**

```csharp
// 初始化
void Init(string fileName, bool enableAutoSave, float autoSaveInterval, bool enableEncryption = false, string encryptKey = null)

// 加载/保存数据
bool Load()
bool Save()

// 数据项操作
bool HasData(string dataName)
bool RemoveData(string dataName)
void RemoveAllData()
string[] GetAllDataNames()

// 读取数据
bool GetBool(string dataName, bool defaultValue = false)
int GetInt(string dataName, int defaultValue = 0)
long GetLong(string dataName, long defaultValue = 0)
float GetFloat(string dataName, float defaultValue = 0)
double GetDouble(string dataName, double defaultValue = 0)
string GetString(string dataName, string defaultValue = null)
T GetObject<T>(string dataName)
object GetObject(Type objectType, string dataName)

// 写入数据
void SetBool(string dataName, bool value)
void SetInt(string dataName, int value)
void SetLong(string dataName, long value)
void SetFloat(string dataName, float value)
void SetDouble(string dataName, double value)
void SetString(string dataName, string value)
void SetObject<T>(string dataName, T obj)
```

---

### 3.3 Data

数据容器类，内部使用 `SortedDictionary<string, string>` 存储键值对，所有类型最终都序列化为字符串存储。

**数据存储格式：**
- 使用 `SortedDictionary` 保持数据有序
- 所有值以字符串形式存储
- 序列化时使用 7 位编码整数优化文件大小

**主要方法：**

```csharp
// 序列化/反序列化
void Serialize(Stream stream)
void Deserialize(Stream stream)

// 数据项操作
bool HasData(string dataName)
bool RemoveData(string dataName)
void RemoveAllData()
int Count { get; }
string[] GetAllDataNames()

// 读取数据（自动类型转换）
bool GetBool(string dataName, bool defaultValue = false)
int GetInt(string dataName, int defaultValue = 0)
long GetLong(string dataName, long defaultValue = 0)
float GetFloat(string dataName, float defaultValue = 0)
double GetDouble(string dataName, double defaultValue = 0)
string GetString(string dataName, string defaultValue = null)

// 写入数据
void SetBool(string dataName, bool value)
void SetInt(string dataName, int value)
void SetLong(string dataName, long value)
void SetFloat(string dataName, float value)
void SetDouble(string dataName, double value)
void SetString(string dataName, string value)
```

---

### 3.4 DataSerializer

数据序列化器，继承自 `FuSerializer<Data>`，采用二进制格式存储数据。

**文件格式：**
- **文件头**：3 字节标识 `GMD`（GameData）
- **数据体**：7 位编码整数（数据项数量）+ 键值对序列

**序列化流程：**
1. 写入文件头 `GMD`
2. 写入版本号
3. 写入数据项数量（7 位编码）
4. 遍历写入所有键值对

---

## 4. 使用示例

### 4.1 基础数据操作

```csharp
using FuFramework.Core.Runtime;
using FuFramework.SaveData.Runtime;

public class GameSaveExample : MonoBehaviour
{
    private DataSaveModule m_SaveModule;
    
    void Start()
    {
        // 注册 DataSaveModule
        ModuleManager.RegisterModule<DataSaveModule>();
        m_SaveModule = ModuleManager.GetModule<DataSaveModule>();
        
        // 保存基础类型数据
        m_SaveModule.SetInt("PlayerLevel", 10);
        m_SaveModule.SetBool("IsFirstTime", false);
        m_SaveModule.SetFloat("Volume", 0.8f);
        m_SaveModule.SetString("PlayerName", "Hero");
        m_SaveModule.SetLong("PlayerExp", 999999999L);
        
        // 读取基础类型数据（带默认值）
        int level = m_SaveModule.GetInt("PlayerLevel");
        bool isFirstTime = m_SaveModule.GetBool("IsFirstTime", true);
        float volume = m_SaveModule.GetFloat("Volume", 1.0f);
        string playerName = m_SaveModule.GetString("PlayerName", "Guest");
    }
}
```

### 4.2 对象序列化

```csharp
// 定义数据类
[Serializable]
public class PlayerData
{
    public string Name;
    public int Level;
    public int Experience;
    public List<string> Items;
}

// 保存复杂对象
var playerInfo = new PlayerData
{
    Name = "Hero",
    Level = 10,
    Experience = 1250,
    Items = new List<string> { "Sword", "Shield", "Potion" }
};

m_SaveModule.SetObject("PlayerData", playerInfo);

// 读取复杂对象
PlayerData data = m_SaveModule.GetObject<PlayerData>("PlayerData");
if (data != null)
{
    Debug.Log($"Player: {data.Name}, Level: {data.Level}");
}
```

### 4.3 多文件管理

```csharp
// 创建指定文件名的数据辅助器
var settingsHelper = m_SaveModule.GetOrCreateHelper("UserSettings");

// 对特定文件进行操作
settingsHelper.SetInt("GraphicsQuality", 2);
settingsHelper.SetBool("Fullscreen", true);
settingsHelper.SetFloat("MusicVolume", 0.5f);

// 另一个文件存储游戏进度
var progressHelper = m_SaveModule.GetOrCreateHelper("GameProgress");
progressHelper.SetInt("CurrentLevel", 5);
progressHelper.SetString("LastSaveTime", DateTime.Now.ToString());

// 保存指定文件
settingsHelper.Save();
progressHelper.Save();

// 获取所有已加载的 Helper 名称
string[] helperNames = m_SaveModule.GetAllHelperNames();
foreach (var name in helperNames)
{
    Debug.Log($"Data file: {name}");
}
```

### 4.4 数据加密

```csharp
// 创建加密的数据辅助器
var secureHelper = m_SaveModule.GetOrCreateHelper("UserAccount");

// 通过配置文件启用加密（推荐）
// 在 ModuleSetting 中设置 EnableEncrypt = true, EncryptKey = "YourSecretKey123"

// 代码中直接设置加密（仅针对特定文件）
// 注意：需要在 Init 之后设置，或重新创建 Helper

// 保存敏感数据
m_SaveModule.SetString("UserEmail", "user@example.com", "UserAccount");
m_SaveModule.SetString("AccessToken", "encrypted_token_value", "UserAccount");
m_SaveModule.SetBool("RememberPassword", true, "UserAccount");

// 数据会自动加密保存到文件
m_SaveModule.Save("UserAccount");

// 读取时自动解密
string email = m_SaveModule.GetString("UserEmail", fileName: "UserAccount");
string token = m_SaveModule.GetString("AccessToken", fileName: "UserAccount");
```

### 4.5 数据管理操作

```csharp
// 检查数据是否存在
bool hasLevel = m_SaveModule.HasData("PlayerLevel");

// 删除指定数据
bool removed = m_SaveModule.RemoveData("TempData");

// 清空所有数据（默认文件）
m_SaveModule.RemoveAllData();

// 清空指定文件的所有数据
var helper = m_SaveModule.GetHelper("UserSettings");
if (helper != null)
{
    helper.RemoveAllData();
    helper.Save();
}

// 删除整个数据文件
m_SaveModule.RemoveHelper("OldSaveData");

// 清空所有数据文件（危险操作）
m_SaveModule.RemoveAllHelper();
```

### 4.6 自动保存机制

```csharp
// 自动保存由模块统一管理，在 ModuleSetting 中配置
// EnableAutoSave: 是否启用
// AutoSaveInterval: 自动保存间隔（秒）

// 手动检查是否有未保存的数据
int dirtyCount = m_SaveModule.GetDirtyHelperCount();
if (dirtyCount > 0)
{
    Debug.Log($"有 {dirtyCount} 个数据文件需要保存");
}

// 检查指定 Helper 是否有未保存数据
var helper = m_SaveModule.GetHelper("UserSettings");
if (helper != null && helper.IsDirty)
{
    Debug.Log("UserSettings 有未保存的修改");
    helper.Save(); // 手动触发保存
}

// 强制保存所有有未保存数据的 Helper
m_SaveModule.SaveAll();
```

---

## 5. 编辑器功能

### 5.1 SaveModuleInspector

`DataSaveModule` 的 Inspector 扩展，提供运行时数据查看功能。

**功能：**
- 显示当前数据文件数量
- 列出所有已加载的 Helper 及其数据项数量
- 显示每个数据文件中的数据项
- 提供 "清除所有数据" 按钮

**使用方法：**
1. 在编辑器中运行游戏
2. 在 Hierarchy 中找到 `[FrameworkModule]` 下的 `DataSaveModule`
3. 选中后在 Inspector 面板查看数据信息

### 5.2 SaveHelperInspector

`DataSaveHelper` 的 Inspector 扩展。

**功能：**
- 显示当前数据项数量
- 显示是否有未保存的数据（IsDirty）
- 列出所有数据项及其值
- 提供 "清除数据" 按钮

---

## 6. 目录结构说明

```text
DataSave/
├── Editor/                          # 编辑器扩展代码
│   ├── Inspector/
│   │   ├── SaveModuleInspector.cs   # DataSaveModule Inspector 扩展
│   │   └── SaveHelperInspector.cs   # DataSaveHelper Inspector 扩展
│   └── FuFramework.Save.Editor.asmdef
├── Runtime/                         # 运行时核心代码
│   ├── DataSaveModule.cs            # 数据保存管理模块
│   ├── DataSaveHelper.cs            # 数据存储辅助器
│   ├── Data.cs                      # 数据容器
│   ├── DataSerializer.cs            # 数据序列化器
│   └── FuFramework.DataSave.Runtime.asmdef
└── README.md                        # 本文档
```

**数据文件存储位置：**
```
Application.persistentDataPath/
└── GameData/                        # 数据根目录
    ├── DefaultData                  # 默认数据文件
    ├── UserSettings                 # 用户设置文件
    ├── GameProgress                 # 游戏进度文件
    └── ...                          # 其他数据文件
```

---

## 7. 依赖

- **Unity**: 2021.3 LTS 或更高版本
- **FuFramework.Core**: 框架核心模块
- **FuFramework.Core.Utility.Encryption**: 加密功能模块（AES 加密支持）

---

## 8. 最佳实践

### 8.1 数据分类存储

建议按数据类型分离存储，避免单文件过大：

```csharp
// 用户设置（小数据量，频繁读取）
var settings = m_SaveModule.GetOrCreateHelper("UserSettings");

// 游戏进度（中等数据量）
var progress = m_SaveModule.GetOrCreateHelper("GameProgress");

// 玩家存档（大数据量，包含大量对象）
var saveData = m_SaveModule.GetOrCreateHelper($"SaveSlot_{slotIndex}");
```

### 8.2 加密策略

- **必须加密**：用户账号信息、支付相关数据、敏感配置
- **建议加密**：游戏进度（防止作弊）、成就数据
- **无需加密**：用户设置、图形选项、音量设置

```csharp
// 敏感数据使用加密
var accountHelper = m_SaveModule.GetOrCreateHelper("Account");
// 加密配置在 ModuleSetting 中统一设置

// 普通数据不使用加密
var settingsHelper = m_SaveModule.GetOrCreateHelper("Settings");
```

### 8.3 数据版本管理

```csharp
// 在数据中保存版本号，便于后续迁移
const int CURRENT_VERSION = 2;

void SaveGame()
{
    m_SaveModule.SetInt("SaveVersion", CURRENT_VERSION, "GameProgress");
    // ... 保存其他数据
}

void LoadGame()
{
    int version = m_SaveModule.GetInt("SaveVersion", "GameProgress", 1);
    
    // 根据版本进行数据迁移
    if (version < CURRENT_VERSION)
    {
        MigrateData(version, CURRENT_VERSION);
    }
    
    // ... 加载其他数据
}
```

### 8.4 自动保存与手动保存结合

```csharp
public class GameManager
{
    // 关键数据变更时手动保存
    public void OnLevelComplete(int level)
    {
        m_SaveModule.SetInt("CurrentLevel", level, "GameProgress");
        m_SaveModule.Save("GameProgress"); // 立即保存
    }
    
    // 非关键数据依赖自动保存
    public void OnSettingsChanged(float volume)
    {
        m_SaveModule.SetFloat("Volume", volume, "UserSettings");
        // 等待自动保存或下次退出时保存
    }
}
```

### 8.5 数据备份与恢复

```csharp
// 备份存档
public void BackupSave(int slotIndex)
{
    var sourcePath = Path.Combine(Application.persistentDataPath, "GameData", $"SaveSlot_{slotIndex}");
    var backupPath = Path.Combine(Application.persistentDataPath, "GameData", $"SaveSlot_{slotIndex}_backup");
    
    if (File.Exists(sourcePath))
    {
        File.Copy(sourcePath, backupPath, true);
    }
}

// 恢复备份
public void RestoreBackup(int slotIndex)
{
    var sourcePath = Path.Combine(Application.persistentDataPath, "GameData", $"SaveSlot_{slotIndex}");
    var backupPath = Path.Combine(Application.persistentDataPath, "GameData", $"SaveSlot_{slotIndex}_backup");
    
    if (File.Exists(backupPath))
    {
        File.Copy(backupPath, sourcePath, true);
        // 重新加载
        m_SaveModule.Load($"SaveSlot_{slotIndex}");
    }
}
```

---

## 9. 注意事项

1. **文件路径**：数据文件存储在 `Application.persistentDataPath/GameData/` 目录下
2. **加密密钥**：修改加密密钥会导致已有加密数据无法读取
3. **数据类型**：所有数据最终都以字符串形式存储，对象类型通过 JSON 序列化
4. **并发访问**：每个文件对应一个 DataSaveHelper，避免多线程同时操作同一文件
5. **内存管理**：数据文件在首次访问时加载，长期占用内存，大数据量建议分文件存储
