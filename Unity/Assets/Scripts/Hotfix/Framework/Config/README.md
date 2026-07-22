# FuFramework Config Module

## 1. 简介

FuFramework Config 模块是游戏框架的配置数据管理系统，提供统一的配置表缓存和查询功能。该模块作为所有游戏配置数据的中央存储库，支持基于 Luban 等配表工具生成的强类型配置数据管理，提供高效的键值索引和丰富的集合操作方法。

## 2. 核心特性

- **统一管理**：所有配置表通过 `ConfigModule` 集中管理，提供一致的访问接口
- **线程安全**：内部使用 `ConcurrentDictionary` 确保多线程环境下的数据安全
- **异步加载**：配置表通过 `IDataTable.LoadAsync()` 支持异步加载，避免阻塞主线程
- **双重索引**：`BaseDataTable<T>` 同时按 `long` Id 和 `string` Key 建立索引，支持灵活查询
- **类型安全**：通过泛型接口确保配置数据的类型安全
- **丰富查询**：提供 Find、ForEach、Max、Min、Sum 等集合操作方法

## 3. 核心概念

### 3.1 类继承与实现体系

```
ModuleBase (框架模块基类)
    └── ConfigModule (配置管理模块)

IDataTable (配置表基础接口)
    └── IDataTable<T> (泛型配置表接口)
        └── BaseDataTable<T> (配置表基类，提供双重索引和集合操作实现)
```

### 3.2 配置架构

```
┌─────────────────────────────────────────────────────────────┐
│                     ConfigModule                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  m_CfgNameTypeDict (ConcurrentDictionary<Type,string>)│ │
│  │  - 类型→配置表名称映射                                │   │
│  ├─────────────────────────────────────────────────────┤   │
│  │  m_CfgDataDict (ConcurrentDictionary<string, IDataTable>)│
│  │  - 按名称索引所有配置表                                │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              │
                ┌─────────────┼─────────────┐
                ▼             ▼             ▼
        ┌──────────┐  ┌──────────┐  ┌──────────┐
        │ 道具表   │  │ 怪物表   │  │ 关卡表   │
        │ (Id+Key  │  │ (Id+Key  │  │ (Id+Key  │
        │  索引)   │  │  索引)   │  │  索引)   │
        └──────────┘  └──────────┘  └──────────┘
```

## 4. 核心类说明

### 4.1 ConfigModule

配置管理模块，继承自 `ModuleBase`，负责所有配置表的注册、查询和移除。

**核心方法：**

```csharp
// 获取配置表
T GetConfig<T>() where T : IDataTable
IDataTable GetConfig(string cfgName)

// 检查配置表是否存在
bool HasConfig<T>() where T : IDataTable
bool HasConfig(string cfgName)

// 添加配置表
void AddConfig(string cfgName, IDataTable cfgValue)

// 移除配置表
bool RemoveConfig<T>() where T : IDataTable
bool RemoveConfig(string cfgName)
void RemoveAllConfigs()

// 属性
int Count                        // 配置表数量
IEnumerable<string> CfgNames     // 所有配置表名称
```

**静态属性：**

```csharp
ConfigModule Instance { get; }   // 模块单例
```

### 4.2 IDataTable / IDataTable<T>

配置表接口，定义配置表的基本操作和泛型数据操作。

```csharp
public interface IDataTable
{
    Task LoadAsync();            // 异步加载
    int Count { get; }           // 数据行数
}

public interface IDataTable<T> : IDataTable where T : class
{
    // 按 ID 获取数据
    T Get(int id);
    T Get(long id);
    T Get(string id);

    // 索引器
    T this[int index] { get; }

    // 属性
    T FirstOrDefault { get; }    // 第一条数据
    T LastOrDefault { get; }     // 最后一条数据
    T[] All { get; }             // 所有数据（数组）

    // 集合转换
    T[] ToArray();
    List<T> ToList();

    // 查找
    T Find(Func<T, bool> func);
    T[] FindListArray(Func<T, bool> func);
    List<T> FindList(Func<T, bool> func);

    // 遍历
    void ForEach(Action<T> func);

    // 聚合
    TK Max<TK>(Func<T, TK> func) where TK : IComparable<TK>;
    TK Min<TK>(Func<T, TK> func) where TK : IComparable<TK>;
    int Sum(Func<T, int> func);
    long Sum(Func<T, long> func);
    float Sum(Func<T, float> func);
    double Sum(Func<T, double> func);
    decimal Sum(Func<T, decimal> func);
}
```

### 4.3 BaseDataTable<T>

配置表基类，实现 `IDataTable<T>` 接口，提供 `SortedDictionary<long, T>` 和 `SortedDictionary<string, T>` 两种索引实现，以及完整的集合操作（Find、ForEach、Max、Min、Sum）。

`LoadAsync()` 方法为抽象方法，由子类实现具体的加载逻辑。

## 5. 使用示例

### 5.1 注册和获取配置表

```csharp
using Hotfix.Framework.Config;
using Hotfix.Framework.Core;

public class ConfigExample
{
    private ConfigModule m_ConfigModule;

    public void Init()
    {
        m_ConfigModule = ConfigModule.Instance;

        // 注册配置表
        var itemTable = new TbItem();
        m_ConfigModule.AddConfig("TbItem", itemTable);

        // 获取配置表（泛型方式）
        var tbItem = m_ConfigModule.GetConfig<TbItem>();

        // 获取配置表（名称方式）
        var tbItem2 = m_ConfigModule.GetConfig("TbItem");

        // 检查配置表是否存在
        if (m_ConfigModule.HasConfig<TbItem>())
        {
            // 使用配置...
        }
    }
}
```

### 5.2 查询配置数据

```csharp
// 获取配置表
var itemTable = ConfigModule.Instance.GetConfig<TbItem>();

// 按 Id 查询
var item = itemTable.Get(1001);
var itemByLongId = itemTable.Get(1001L);

// 按 Key 查询
var itemByKey = itemTable.Get("Sword_Legendary");

// 按索引查询
var firstItem = itemTable[0];

// 获取第一条和最后一条
var first = itemTable.FirstOrDefault;
var last = itemTable.LastOrDefault;

// 获取所有数据
var allItems = itemTable.All;
foreach (var item in allItems)
{
    Debug.Log($"道具: {item.Name}, Id: {item.Id}");
}

// 按条件查找
var rareItem = itemTable.Find(i => i.Quality == QualityType.Rare);
var rareItems = itemTable.FindList(i => i.Quality == QualityType.Rare);

// 遍历
itemTable.ForEach(i => Debug.Log(i.Name));

// 聚合计算
var maxPrice = itemTable.Max(i => i.Price);
var minPrice = itemTable.Min(i => i.Price);
var totalPrice = itemTable.Sum(i => i.Price);
```

## 6. 目录结构

```text
Config/
├── ConfigModule.cs           # 配置管理模块
├── BaseDataTable.cs          # 配置表基类 (双重索引)
├── IDataTable.cs             # 配置表接口定义
└── README.md                 # 本文档
```

## 7. 依赖

- **Hotfix.Framework.Core**：提供 ModuleBase 基类
- **System.Threading.Tasks**：异步加载支持
- **Luban**（外部）：配置表生成工具

## 8. 最佳实践

1. **集中注册**：在游戏启动流程中统一注册所有配置表
2. **缓存引用**：频繁查询的配置表可缓存本地引用，避免重复调用 `GetConfig`
3. **空值检查**：`Get` 方法在数据不存在时返回 `default(T)`（class 类型为 null），始终检查返回值
4. **线程安全**：配置数据读取通过 `ConcurrentDictionary` 保证线程安全
5. **合理使用聚合**：`Max`、`Min`、`Sum` 遍历整个数据表，大数据量时注意性能

## 9. 注意事项

1. 配置表名称使用类名（`typeof(T).Name`），需确保类和名称一致
2. 重复添加同名配置表会被忽略（`AddConfig` 内部检查存在性）
3. 配置数据读取是线程安全的，但不会对配置行数据本身加锁
4. 使用 Luban 生成的配置表需实现 `IDataTable<T>` 接口并继承 `BaseDataTable<T>`
