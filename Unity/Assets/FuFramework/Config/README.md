# FuFramework Config Module

## 简介
FuFramework Config 模块是用于管理游戏静态配置表（DataTable）的核心模块。它提供了一套泛型基础架构，支持高效的数据加载、索引构建以及多样化的查询操作（ID 查找、LINQ 查询等）。

## 特性
- **统一管理**：通过 `ConfigManager` 集中管理所有配置表，支持运行时动态增删。
- **泛型基类**：`BaseDataTable<T>` 提供了开箱即用的数据存储和索引功能。
- **多重索引**：内置支持 `int`、`long` 和 `string` 类型的主键索引，实现 O(log n) 级的高效查找。
- **丰富查询**：支持 `Get`（按 ID 获取）、`Find`（按条件查找）、`Max/Min/Sum`（聚合运算）等操作。
- **调试友好**：提供 Editor Inspector 扩展，可在运行时实时查看已加载的配置表状态。

## 核心类说明

### ConfigManager
配置管理器，继承自 `FuModule`。
- **功能**：维护所有 `IDataTable` 实例，提供全局访问点。
- **接口**：
  - `GetConfig<T>()`: 获取指定类型的配置表。
  - `AddConfig/RemoveConfig`: 动态管理配置表。
  - `Count`: 获取当前加载的配置表数量。

### BaseDataTable<T>
数据表抽象基类，所有具体的配置表都应继承此类。
- **存储**：
  - `DataList`: 原始数据列表，用于遍历和 LINQ 查询。
  - `LongDataMaps`: `SortedDictionary<long, T>`，用于 int/long ID 查找。
  - `StringDataMaps`: `SortedDictionary<string, T>`，用于 string ID 查找。
- **抽象方法**：
  - `LoadAsync()`: 子类需实现具体的加载逻辑（如解析 CSV、JSON 或二进制文件）。

## 使用示例

### 1. 定义配置表类
假设有一个道具表 `ItemTable`，包含 `ItemConfig` 数据项。

```csharp
// 定义数据项
public class ItemConfig
{
    public int Id;
    public string Name;
    public int Price;
}

// 定义配置表
public class ItemTable : BaseDataTable<ItemConfig>
{
    public override async Task LoadAsync()
    {
        // 模拟加载逻辑
        // 实际项目中通常读取文件并反序列化
        var items = new List<ItemConfig>
        {
            new ItemConfig { Id = 1001, Name = "Sword", Price = 100 },
            new ItemConfig { Id = 1002, Name = "Shield", Price = 50 }
        };

        // 填充数据
        foreach (var item in items)
        {
            DataList.Add(item);
            LongDataMaps.Add(item.Id, item); // 构建索引
        }
        
        await Task.CompletedTask;
    }
}
```

### 2. 注册与获取
```csharp
// 获取管理器
var configManager = ModuleManager.GetModule<ConfigManager>();

// 注册配置表 (通常在游戏初始化流程中完成)
var itemTable = new ItemTable();
await itemTable.LoadAsync();
configManager.AddConfig("ItemTable", itemTable);

// 获取配置表
var table = configManager.GetConfig<ItemTable>();
```

### 3. 数据查询
```csharp
// 1. 按 ID 获取 (高效)
var sword = table.Get(1001);

// 2. 按条件查找
var cheapItems = table.FindList(item => item.Price < 80);

// 3. 聚合运算
var maxPrice = table.Max(item => item.Price);
var totalPrice = table.Sum(item => item.Price);
```

## 编辑器扩展
- **Inspector**: 选中场景中的 `ConfigManager` (挂载在 FuFramework 节点下)，可在 Inspector 面板查看当前已加载的配置表列表。
- **宏定义**: 菜单栏 `FuFramework/脚本编译宏定义设置` 提供了 `Enable Binary Config` 选项，用于开启二进制配置表支持（需配合具体业务实现）。
