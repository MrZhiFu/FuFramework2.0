# FuFramework Config Module

## 1. 简介

**FuFramework Config** 模块是用于管理游戏静态配置表（DataTable）的核心模块。它提供了一套泛型基础架构，支持高效的数据加载、索引构建以及多样化的查询操作（ID 查找、条件查找、聚合运算等）。

本模块与 **[Luban](https://github.com/focus-creative-games/luban)** 配置工具深度集成，支持通过 Luban 自动生成配置表代码，实现类型安全、高效的数据管理。

---

## 2. 特性

- **统一管理**：通过 `ConfigModule` 集中管理所有配置表，支持运行时动态增删。
- **泛型基类**：`BaseDataTable<T>` 提供了开箱即用的数据存储和索引功能。
- **多重索引**：内置支持 `int`、`long` 和 `string` 类型的主键索引，实现高效查找。
- **丰富查询**：支持 `Get`（按 ID 获取）、`Find`（按条件查找）、`Max/Min/Sum`（聚合运算）等操作。
- **Luban 集成**：支持 Luban 生成的配置表代码，包括多语言本地化、表间引用解析、自定义类型等。
- **调试友好**：提供 Editor Inspector 扩展，可在运行时实时查看已加载的配置表状态。
- **格式支持**：支持 JSON 和二进制两种配置表格式，可通过菜单切换。

---

## 3. 核心类说明

### 3.1 ConfigModule

配置管理器，继承自 `FuModule`，是配置表的核心管理类。

**主要功能：**
- 维护所有 `IDataTable` 实例，提供全局访问点
- 支持通过类型或名称获取配置表
- 支持动态添加、移除、清空配置表
- 线程安全（使用 `ConcurrentDictionary`）

**主要方法：**

```csharp
// 获取指定类型的配置表
T GetConfig<T>() where T : IDataTable

// 通过名称获取配置表
IDataTable GetConfig(string cfgName)

// 检查是否存在指定配置表
bool HasConfig<T>() where T : IDataTable
bool HasConfig(string cfgName)

// 添加配置表
void AddConfig(string cfgName, IDataTable cfgValue)

// 移除配置表
bool RemoveConfig<T>() where T : IDataTable
bool RemoveConfig(string cfgName)

// 清空所有配置表
void RemoveAllConfigs()

// 获取配置表数量
int Count { get; }

// 获取所有配置表名称
IEnumerable<string> CfgNames { get; }
```

---

### 3.2 IDataTable / IDataTable<T>

**IDataTable（基础接口）：**
数据表接口，定义了数据表的异步加载和数据行数查询接口。

```csharp
public interface IDataTable
{
    // 异步加载数据表
    Task LoadAsync();
    
    // 获取数据行数量
    int Count { get; }
}
```

**IDataTable<T>（泛型接口）：**
数据表泛型接口，定义了数据表的基本操作规范，如按 ID 获取、索引器访问、首尾数据获取、条件查找、聚合运算等。

```csharp
public interface IDataTable<T> : IDataTable where T : class
{
    // 按 ID 获取数据（支持 int、long、string 类型）
    T Get(int id);
    T Get(long id);
    T Get(string id);
    
    // 索引器访问
    T this[int index] { get; }
    
    // 获取首尾数据
    T FirstOrDefault { get; }
    T LastOrDefault { get; }
    
    // 获取所有数据
    T[] All { get; }
    T[] ToArray();
    List<T> ToList();
    
    // 条件查找
    T Find(Func<T, bool> func);
    T[] FindListArray(Func<T, bool> func);
    List<T> FindList(Func<T, bool> func);
    
    // 遍历
    void ForEach(Action<T> func);
    
    // 聚合运算
    TK Max<TK>(Func<T, TK> func) where TK : IComparable<TK>;
    TK Min<TK>(Func<T, TK> func) where TK : IComparable<TK>;
    int Sum(Func<T, int> func);
    long Sum(Func<T, long> func);
    float Sum(Func<T, float> func);
    double Sum(Func<T, double> func);
    decimal Sum(Func<T, decimal> func);
}
```

---

### 3.3 BaseDataTable<T>

数据表抽象基类，实现了 `IDataTable<T>` 接口的大部分方法，Luan导出的配置表都会继承此类，以实现数据表的快速基本操作。

**数据存储结构：**

```csharp
// long 类型主键索引（SortedDictionary 实现）
protected readonly SortedDictionary<long, T> LongKeyDataDict = new();

// string 类型主键索引（SortedDictionary 实现）
protected readonly SortedDictionary<string, T> StrKeyDataDict = new();

// 数据列表（用于遍历和 LINQ 查询）
protected readonly List<T> DataList = new();
```

**抽象方法：**

```csharp
// 子类需实现具体的加载逻辑（如解析 CSV、JSON 或二进制文件）
public abstract Task LoadAsync();
```

**已实现方法：**
- `Get(int/long/string id)`: 按 ID 获取数据
- `this[int index]`: 按索引访问数据
- `FirstOrDefault/LastOrDefault`: 获取首尾数据
- `All/ToArray/ToList`: 获取所有数据
- `Find/FindListArray/FindList`: 条件查找
- `ForEach`: 遍历数据
- `Max/Min`: 获取最大最小值
- `Sum`: 求和运算（支持 int、long、float、double、decimal）

---

## 4. Luban 集成

### 4.1 Luban 简介

[Luban](https://github.com/focus-creative-games/luban) 是一个功能强大、高度可配置的配置数据生成工具，支持 Excel、JSON、XML 等多种数据源，可生成多种编程语言的代码。

### 4.2 生成的代码结构

Luban 生成的配置表代码位于 `Assets/Scripts/Hotfix/Config/Generate/` 目录下：

```
Config/Generate/
├── TableManager.cs              # 配置表管理器，统一加载所有表
├── Local/                       # 本地化相关表
│   ├── Localization.cs          # 多语言数据定义
│   └── TbLocalization.cs        # 多语言表
├── Tables/                      # 游戏配置表
│   ├── Item.cs                  # 道具数据定义
│   ├── TbItem.cs                # 道具表
│   ├── GlobalDefine.cs          # 全局常量定义
│   ├── TbGlobalDefine.cs        # 全局常量表
│   └── ...                      # 其他配置表
├── EQuality.cs                  # 枚举定义
├── ItemType.cs
├── ItemSubType.cs
├── ItemUseType.cs
├── vec2.cs                      # 自定义类型
├── vec3.cs
├── vec4.cs
└── Property.cs
```

### 4.3 TableManager（配置表管理器）

`TableManager` 是Luban侧生成的配置表管理器，负责统一管理所有配置表的加载和访问。

**主要功能：**
- 集中管理所有配置表实例
- 异步批量加载所有配置表
- 支持多语言本地化适配
- 支持表间引用解析

**使用示例：**

```csharp
using Hotfix.Config;
using FuFramework.Config.Runtime;

public class GameConfigLoader : MonoBehaviour
{
    private TableManager m_TableManager;
    private ConfigModule m_ConfigModule;
    
    async void Start()
    {
        // 1. 注册 ConfigModule
        ModuleManager.RegisterModule<ConfigModule>();
        m_ConfigModule = ModuleManager.GetModule<ConfigModule>();
        
        // 2. 初始化 TableManager
        m_TableManager = new TableManager();
        m_TableManager.Init(m_ConfigModule);
        
        // 3. 异步加载所有配置表
        await m_TableManager.LoadAsync(async (fileName) =>
        {
            // 从 Resources 或 AssetBundle 加载 JSON 数据
            var textAsset = await Resources.LoadAsync<TextAsset>($"Configs/{fileName}");
            return JSON.Parse(textAsset.text);
        });
        
        Debug.Log("配置表加载完成！");
        
        // 4. 设置多语言适配器（可选）
        m_TableManager.SetTranslateText((key, defaultValue) =>
        {
            // 返回翻译后的文本
            return LocalizationManager.GetString(key, defaultValue);
        });
    }
}
```

### 4.4 生成的数据表类

Luban 为每张配置表生成两个类：

**数据定义类（如 Item.cs）：**
- 继承自 `BeanBase`
- 包含配置表所有字段的定义
- 提供配置表反序列化方法
- 支持配置表多语言字段标记

```csharp
public sealed partial class Item : BeanBase
{
    public int Id { private set; get; }
    public string Name { private set; get; }
    public string Desc { private set; get; }
    public string Icon { private set; get; }
    public EQuality Quality { private set; get; }
    public ItemType Type { private set; get; }
    // ...
    
    // 多语言支持
    public void TranslateText(Func<string, string, string> translator)
    {
        Name = translator(Name_Localization_Key, Name);
        Desc = translator(Desc_Localization_Key, Desc);
    }
}
```

**数据表类（如 TbItem.cs）：**
- 继承自 `BaseDataTable<T>`
- 实现 `LoadAsync()` 方法
- 提供表间引用解析
- 支持批量多语言转换

```csharp
public partial class TbItem : BaseDataTable<Tables.Item>
{
    public override async Task LoadAsync()
    {
        var jsonNode = await _loadFunc();
        // 解析 JSON 并填充数据
        foreach(var ele in jsonNode.Children)
        {
            var item = Tables.Item.DeserializeItem(ele);
            DataList.Add(item);
            LongKeyDataDict.Add(item.Id, item);
            StrKeyDataDict.Add(item.Id.ToString(), item);
        }
    }
}
```

### 4.5 访问配置数据

```csharp
// 获取 TableManager 实例
var tableManager = GameEntry.Config.TableManager;

// 1. 按 ID 获取道具
var item = tableManager.TbItem.Get(1001);
Debug.Log($"道具名称: {item.Name}");

// 2. 遍历所有道具
tableManager.TbItem.ForEach(item =>
{
    Debug.Log($"ID: {item.Id}, 名称: {item.Name}, 品质: {item.Quality}");
});

// 3. 条件查找 - 获取所有武器
var weapons = tableManager.TbItem.FindList(item => item.Type == ItemType.Weapon);

// 4. 条件查找 - 获取高品质道具
var highQualityItems = tableManager.TbItem.FindList(item => item.Quality >= EQuality.Epic);

// 5. 聚合运算
var maxQuality = tableManager.TbItem.Max(item => item.Quality);
var totalItems = tableManager.TbItem.Count;
```

### 4.6 多语言本地化

Luban 生成的代码内置多语言支持：

**配置表中的多语言字段：**
```csharp
public sealed partial class Item : BeanBase
{
    public string Name { private set; get; }
    private readonly string Name_Localization_Key;  // 自动生成的 Key
    public string Desc { private set; get; }
    private readonly string Desc_Localization_Key;
    
    public void TranslateText(Func<string, string, string> translator)
    {
        Name = translator(Name_Localization_Key, Name);
        Desc = translator(Desc_Localization_Key, Desc);
    }
}
```

**设置翻译适配器：**
```csharp
// 在游戏初始化时设置
m_TableManager.SetTranslateText((key, defaultValue) =>
{
    // 从本地化系统获取翻译
    if (LocalizationManager.HasKey(key))
    {
        return LocalizationManager.GetString(key);
    }
    return defaultValue;
});
```

### 4.7 自定义类型支持

Luban 支持自定义类型，如 Vector2/3/4：

**生成的自定义类型（vec3.cs）：**
```csharp
public partial struct vec3
{
    public float X { private set; get; }
    public float Y { private set; get; }
    public float Z { private set; get; }
    
    public vec3(JSONNode buf)
    {
        X = buf["x"];
        Y = buf["y"];
        Z = buf["z"];
    }
}
```

**转换工具类（ExternalTypeUtil.cs）：**
```csharp
public static class ExternalTypeUtil
{
    public static UnityEngine.Vector2 NewVector2(vec2 v)
    {
        return new UnityEngine.Vector2(v.X, v.Y);
    }
    
    public static UnityEngine.Vector3 NewVector3(vec3 v)
    {
        return new UnityEngine.Vector3(v.X, v.Y, v.Z);
    }
}
```

**使用示例：**
```csharp
// 获取 Luban 生成的 vec3 数据
var positionData = tableManager.TbScene.Get(1).PlayerSpawnPosition;

// 转换为 Unity Vector3
Vector3 spawnPosition = ExternalTypeUtil.NewVector3(positionData);
```

---

## 5. 使用示例

### 5.1 基础使用（不使用 Luban）

```csharp
// 定义配置数据项
public class ItemConfig
{
    public int Id;
    public string Name;
    public int Price;
}

// 定义配置表类
public class ItemTable : BaseDataTable<ItemConfig>
{
    public override async Task LoadAsync()
    {
        // 从文件加载数据
        var json = await File.ReadAllTextAsync("Configs/items.json");
        var items = JsonConvert.DeserializeObject<List<ItemConfig>>(json);
        
        foreach (var item in items)
        {
            DataList.Add(item);
            LongKeyDataDict.Add(item.Id, item);
        }
    }
}

// 注册和使用
var configModule = ModuleManager.GetModule<ConfigModule>();
var itemTable = new ItemTable();
await itemTable.LoadAsync();
configModule.AddConfig("ItemTable", itemTable);

var item = configModule.GetConfig<ItemTable>().Get(1001);
```

### 5.2 使用 Luban 生成的配置表（推荐）

```csharp
using Hotfix.Config;

public class ConfigInitializer : MonoBehaviour
{
    private TableManager m_TableManager;
    
    async void Start()
    {
        // 1. 注册模块
        ModuleManager.RegisterModule<ConfigModule>();
        var configModule = ModuleManager.GetModule<ConfigModule>();
        
        // 2. 初始化 TableManager
        m_TableManager = new TableManager();
        m_TableManager.Init(configModule);
        
        // 3. 加载配置表
        await m_TableManager.LoadAsync(LoadConfigFile);
        
        // 4. 使用配置数据
        var item = m_TableManager.TbItem.Get(1001);
        var achievement = m_TableManager.TbAchievement.Get(1);
        
        // 5. 查询操作
        var allItems = m_TableManager.TbItem.ToList();
        var weapons = m_TableManager.TbItem.FindList(i => i.Type == ItemType.Weapon);
    }
    
    private async Task<JSONNode> LoadConfigFile(string fileName)
    {
        // 实现配置文件的加载逻辑
        var handle = await AssetModule.Instance.LoadAssetAsync<TextAsset>($"Configs/{fileName}");
        return JSON.Parse(handle.GetAssetObject<TextAsset>().text);
    }
}
```

---

## 6. 编辑器功能

### 6.1 配置表导入 (ConfigImporter)

提供菜单项用于导出配置表数据。

**菜单项：**
- `FuFramework/配置表/导出配置表—Json`: 导出 JSON 格式的配置表
- `FuFramework/配置表/导出配置表—Bin`: 导出二进制格式的配置表

**实现原理：**
1. 查找项目同级目录下的 `Config` 文件夹
2. 执行对应的批处理脚本（`gen-client-json.bat/sh` 或 `gen-client-bin.bat/sh`）
3. 根据导出格式自动添加/移除 `ENABLE_BINARY_CONFIG` 宏定义
4. 刷新 Unity 资源

**配置表目录结构：**
```
Project/
├── Unity/                  # Unity 项目目录
│   └── Assets/
└── Config/                 # 配置表源文件目录（Luban 工作目录）
    ├── gen-client-json.bat
    ├── gen-client-json.sh
    ├── gen-client-bin.bat
    └── gen-client-bin.sh
```

### 6.2 Inspector 扩展 (ConfigModuleInspector)

在运行时实时查看已加载的配置表状态。

**功能：**
- 显示当前加载的配置表数量
- 列出所有已加载的配置表名称

**使用方法：**
1. 在编辑器中运行游戏
2. 在 Hierarchy 中找到 `[FrameworkModule]` 下的 `ConfigModule`
3. 选中后在 Inspector 面板查看配置表信息

---

## 7. 目录结构说明

```text
Config/
├── Editor/                          # 编辑器扩展代码
│   ├── ConfigImporter.cs            # 配置表导入器
│   ├── Inspector/
│   │   └── ConfigModuleInspector.cs # ConfigModule Inspector 扩展
│   └── FuFramework.Config.Editor.asmdef
├── Runtime/                         # 运行时核心代码
│   ├── ConfigModule.cs              # 配置管理模块
│   ├── Config/
│   │   ├── BaseDataTable.cs         # 数据表抽象基类
│   │   └── IDataTable.cs            # 数据表接口定义
│   └── FuFramework.Config.Runtime.asmdef
└── README.md                        # 本文档

Scripts/Hotfix/Config/               # Luban 生成的代码（示例）
├── Generate/
│   ├── TableManager.cs              # 配置表管理器
│   ├── Local/                       # 本地化表
│   ├── Tables/                      # 游戏配置表
│   ├── *.cs                         # 枚举和自定义类型
│   └── Extension/
│       └── ExternalTypeUtil.cs      # 类型转换工具
└── LanguageKey/
    └── LanguageKey.cs               # 多语言 Key 定义
```

---

## 8. 依赖

- **Unity**: 2021.3 LTS 或更高版本
- **FuFramework.Core**: 框架核心模块
- **System.Collections.Concurrent**: 线程安全字典支持
- **Luban**: 配置数据生成工具（可选但推荐）
- **SimpleJSON**: JSON 解析库（Luban 依赖）

---

## 9. 最佳实践

### 9.1 配置表加载时机

建议在游戏初始化阶段加载所有配置表：

```csharp
public class GameLauncher : MonoBehaviour
{
    async void Start()
    {
        // 1. 注册配置模块
        ModuleManager.RegisterModule<ConfigModule>();
        var configModule = ModuleManager.GetModule<ConfigModule>();
        
        // 2. 使用 Luban 的 TableManager 批量加载
        var tableManager = new TableManager();
        tableManager.Init(configModule);
        
        await tableManager.LoadAsync(async (fileName) =>
        {
            var asset = await AssetModule.Instance.LoadAssetAsync<TextAsset>($"Configs/{fileName}");
            return JSON.Parse(asset.GetAssetObject<TextAsset>().text);
        });
        
        Debug.Log($"共加载 {configModule.Count} 张配置表");
    }
}
```

### 9.2 配置表数据缓存

对于频繁访问的配置数据，建议在业务层进行缓存：

```csharp
public class ItemService
{
    private TbItem m_ItemTable;
    private Dictionary<int, Item> m_ItemCache;
    
    public void Init(TableManager tableManager)
    {
        m_ItemTable = tableManager.TbItem;
        // 预缓存所有数据
        m_ItemCache = m_ItemTable.ToList().ToDictionary(item => item.Id);
    }
    
    public Item GetItem(int id)
    {
        return m_ItemCache.TryGetValue(id, out var item) ? item : null;
    }
}
```

### 9.3 配置表热更新

配合 AssetModule 实现配置表热更新：

```csharp
public async Task ReloadConfigTable<T>(string assetPath) where T : BaseDataTable<T>, new()
{
    var assetModule = ModuleManager.GetModule<AssetModule>();
    var configModule = ModuleManager.GetModule<ConfigModule>();
    
    // 1. 从远程加载新的配置表资源
    var rawFileHandle = await assetModule.LoadRawFileAsync(assetPath);
    var bytes = rawFileHandle.GetRawFileData();
    
    // 2. 解析并创建新的配置表
    var newTable = new T();
    await newTable.LoadFromBytesAsync(bytes);
    
    // 3. 移除旧配置表，添加新配置表
    configModule.RemoveConfig<T>();
    configModule.AddConfig(typeof(T).Name, newTable);
    
    // 4. 触发配置表更新事件
    EventManager.Trigger(new ConfigReloadEvent { ConfigType = typeof(T) });
}
```

### 9.4 Luban 工作流

1. **编辑配置**：在 Excel 或 JSON 中编辑配置数据
2. **生成代码**：运行 Luban 生成 C# 代码和 JSON 数据文件
3. **导入 Unity**：将生成的 JSON 文件放入 Resources 或打包为 AssetBundle
4. **运行时加载**：使用 TableManager 异步加载配置表

**推荐目录结构：**
```
Project/
├── Config/                        # Luban 工作目录
│   ├── Designer/                  # 配置表源文件（Excel/JSON）
│   ├── Luban.conf                 # Luban 配置文件
│   └── gen.bat                    # 生成脚本
├── Unity/
│   └── Assets/
│       ├── Scripts/Hotfix/Config/ # 生成的 C# 代码
│       └── Resources/Configs/     # 生成的 JSON 数据文件
```
