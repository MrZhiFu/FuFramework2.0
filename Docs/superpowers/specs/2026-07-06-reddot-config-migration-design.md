# 红点系统配置表化设计文档

> 日期: 2026-07-06 | 状态: 待 Review

## 一、背景与目标

当前红点系统通过 `RedDotSetting.asset` (ScriptableObject) 配置红点树结构，通过自定义 Editor 面板编辑并自动生成 `RedDotKeys.cs` 常量类。本次改造将红点配置迁移至 Luban 配置表系统，实现以下目标：

- **统一管理**：红点配置与其他游戏配置一样通过 Excel 表管理，策划可在 Excel 中编辑
- **扩展性**：为红点节点增加更多配置字段（显示模式、清理策略等）
- **流程统一**：所有配置统一走 Luban 管线，享受校验、多格式导出、版本管理等能力

## 二、核心设计决策

| # | 决策 | 结论 |
|---|---|---|
| 1 | 层级表示方式 | ParentId 引用 |
| 2 | Key 类型 | Luban 生成 `ERedDotKey` 枚举，替代 `RedDotKeys.cs` |
| 3 | 枚举 ↔ 配置表映射 | Id 列 = 枚举值，ParentId 引用也用枚举值 |
| 4 | Bean 定义方式 | 运行时代码定义，不走 `__beans__.xlsx` |
| 5 | 表注册方式 | Luban 自动收集，不在 `__tables__.xlsx` 手动注册 |
| 6 | 代码生成工具 | 不需要——枚举由 Luban 直接生成 |
| 7 | 静态 vs 动态节点 | 双字典分离：`Dictionary<ERedDotKey, ...>` + `Dictionary<string, ...>` |
| 8 | RedDotRegister 类 | 废弃，改为 FGUI 编辑器层驱动（自定义数据 + 代码生成） |

## 三、Luban 配置定义

### 3.1 枚举定义 (`__enums__.xlsx` 新增)

```xml
<!-- ERedDotKey — 红点节点标识 -->
枚举值按 Id 编号，例如:
  Bag=1, Bag_Item=2, Bag_Skill=3, Shop=4, Shop_Gift=5, Shop_Res=6,
  Hero=7, Hero_Equip=8, Hero_Skill=9, Hero_Skin=10, Battle=11, Battle_Team=12

<!-- ERedDotDisplayMode — 红点显示模式 -->
  DotOnly=0       # 只显示红点
  DotNumber=1     # 红点 + 数字
  Auto=2          # =1 显示红点，>1 显示数字

<!-- ERedDotCleanStrategy — 红点清理策略 -->
  Manual=0         # 手动清除（业务代码显式调用）
  ViewAutoClean=1  # 框架提供 TryAutoClean，业务代码在合适时机调用（如点击页签）
```

### 3.2 Excel 数据表 (`R-RedDot-红点表.xlsx`)

| 列名 | 类型 | 说明 |
|---|---|---|
| Id | `ERedDotKey` | 节点唯一标识（主键，即枚举值） |
| ParentId | `int` | 父节点 Id，0 表示根节点 |
| DisplayMode | `ERedDotDisplayMode` | 默认显示模式 |
| CleanStrategy | `ERedDotCleanStrategy` | 清理策略 |
| ##备注 | `string` | 策划备注（注释列，不进入运行时数据） |

数据示例：

| Id | ParentId | DisplayMode | CleanStrategy | ##备注 |
|---|---|---|---|---|
| Bag(=1) | 0 | DotOnly | Manual | 背包 |
| Bag_Item(=2) | 1 | DotNumber | Manual | 背包.道具 |
| Bag_Skill(=3) | 1 | DotOnly | Manual | 背包.技能 |
| Shop(=4) | 0 | DotNumber | Manual | 商店 |
| Shop_Gift(=5) | 4 | DotOnly | Manual | 商店.礼包 |
| Shop_Res(=6) | 4 | DotOnly | Manual | 商店.资源 |

> 注：`##` 标记的列 Luban 自动识别为注释列，不参与代码生成

### 3.3 Luban 自动生成的代码

运行 `gen-client-bin.bat` 后自动产出：

| 文件 | 说明 |
|---|---|
| `ERedDotKey.cs` | 红点节点枚举 |
| `ERedDotDisplayMode.cs` | 显示模式枚举 |
| `ERedDotCleanStrategy.cs` | 清理策略枚举 |
| `RedDot.cs` | 每行数据的 Bean 类（由 Luban 自动生成） |
| `TbRedDot.cs` | 配置表数据容器 |
| `TableManager.cs` | 自动注册 `TbRedDot` |

## 四、运行时数据结构

### 4.1 RedDotNode 改造

```csharp
public class RedDotNode : IReference
{
    // 节点标识（静态节点使用枚举，动态节点使用字符串）
    public ERedDotKey? StaticKey { get; private set; }
    public string DynamicKey { get; private set; }

    // 计数
    public int RawCount { get; private set; }
    public int TotalCount { get; private set; }

    // 层级
    public RedDotNode Parent { get; private set; }

    // 配置属性
    public ERedDotDisplayMode DisplayMode { get; private set; }
    public ERedDotCleanStrategy CleanStrategy { get; private set; }

    // 子节点
    private readonly List<RedDotNode> m_Children = new();
    public IReadOnlyList<RedDotNode> GetChildren() => m_Children.AsReadOnly();

    // 事件
    public event Action<int> OnCountChanged;

    // 工厂方法
    public static RedDotNode Create(ERedDotKey key, RedDotNode parent,
        ERedDotDisplayMode displayMode, ERedDotCleanStrategy cleanStrategy)
    {
        var node = ReferencePool.Acquire<RedDotNode>();
        node.StaticKey = key;
        node.Parent = parent;
        node.DisplayMode = displayMode;
        node.CleanStrategy = cleanStrategy;
        return node;
    }

    public static RedDotNode CreateDynamic(string key, RedDotNode parent)
    {
        var node = ReferencePool.Acquire<RedDotNode>();
        node.DynamicKey = key;
        node.Parent = parent;
        node.DisplayMode = ERedDotDisplayMode.DotOnly;
        node.CleanStrategy = ERedDotCleanStrategy.Manual;
        return node;
    }

    // 两阶段构建（初始化后设置父节点）
    public void SetParent(RedDotNode parent)
    {
        Parent = parent;
    }

    // 添加子节点
    public void AddChild(RedDotNode child) { ... }

    // 设置计数（自动向上传播）
    public void SetCount(int count) { ... }

    // IReference
    public void Clear() { ... }
    public void ClearAllListeners() { ... }
}
```

### 4.2 RedDotModule 改造

```csharp
public class RedDotModule : FuModule
{
    // ========== 双字典存储（无装箱） ==========
    private static readonly Dictionary<ERedDotKey, RedDotNode> StaticNodes = new();
    private static readonly Dictionary<string, RedDotNode> DynamicNodes = new();

    // ========== 生命周期 ==========
    protected override void OnInit()
    {
        // 1. 从 Luban 配置表获取数据
        var tbRedDot = GlobalModule.ConfigModule.GetConfig<TbRedDot>();
        if (tbRedDot == null || tbRedDot.DataList.Count == 0) return;

        StaticNodes.Clear();
        DynamicNodes.Clear();

        // 2. 两阶段构建（先创建所有节点，再建立父子关系）
        foreach (var row in tbRedDot.DataList)
        {
            var node = RedDotNode.Create(row.Id, null, row.DisplayMode, row.CleanStrategy);
            StaticNodes.Add(row.Id, node);
        }
        foreach (var row in tbRedDot.DataList)
        {
            if (row.ParentId == 0) continue;
            if (!StaticNodes.TryGetValue(row.Id, out var child) ||
                !StaticNodes.TryGetValue((ERedDotKey)row.ParentId, out var parent)) continue;
            child.SetParent(parent);
            parent.AddChild(child);
        }
    }

    protected override void OnDispose()
    {
        foreach (var node in StaticNodes.Values) ReferencePool.Release(node);
        foreach (var node in DynamicNodes.Values) ReferencePool.Release(node);
        StaticNodes.Clear();
        DynamicNodes.Clear();
    }

    // ========== 动态节点 ==========
    public RedDotNode AddDynamicChild(ERedDotKey parentKey, string childName)
    {
        if (!StaticNodes.TryGetValue(parentKey, out var parentNode)) return null;
        if (DynamicNodes.ContainsKey(childName)) return DynamicNodes[childName];

        var node = RedDotNode.CreateDynamic(childName, parentNode);
        parentNode.AddChild(node);
        DynamicNodes.Add(childName, node);
        return node;
    }

    // SetCount 对动态节点：归零时自动从 DynamicNodes 移除并回收
    public void SetCount(string key, int count)
    {
        if (!DynamicNodes.TryGetValue(key, out var node)) return;
        node.SetCount(count);
        if (node.RawCount == 0)
        {
            node.Parent?.GetChildren().Remove(node);  // 从父节点移除
            DynamicNodes.Remove(key);
            ReferencePool.Release(node);
        }
    }

    // ========== 清理策略 ==========
    public void TryAutoClean(ERedDotKey key)
    {
        if (!StaticNodes.TryGetValue(key, out var node)) return;
        if (node.CleanStrategy != ERedDotCleanStrategy.ViewAutoClean) return;
        CleanNodeRecursive(node);
    }

    private void CleanNodeRecursive(RedDotNode node)
    {
        node.SetCount(0);
        foreach (var child in node.GetChildren())
            CleanNodeRecursive(child);
    }

    // ========== 静态节点 API（ERedDotKey 重载） ==========
    public void Register(ERedDotKey key, Action<int> onChange, bool immediateNotify = true) { ... }
    public void Unregister(ERedDotKey key, Action<int> onChange) { ... }
    public RedDotNode GetNode(ERedDotKey key) { ... }
    public int GetCount(ERedDotKey key) { ... }
    public bool HasNode(ERedDotKey key) { ... }
    public void SetCount(ERedDotKey key, int count) { ... }
    public void ResetCount(ERedDotKey key) => SetCount(key, 0);

    // ========== 动态节点 API（string 重载） ==========
    public void Register(string key, Action<int> onChange, bool immediateNotify = true) { ... }
    public void Unregister(string key, Action<int> onChange) { ... }
    public RedDotNode GetNode(string key) { ... }
    public int GetCount(string key) { ... }
    public bool HasNode(string key) { ... }
    public void ResetCount(string key) => SetCount(key, 0);
}
```

### 4.3 Luban 自动生成的 Bean 类

```csharp
// 自动生成到 Hotfix.Config.Tables（无需手动编写）
public sealed partial class RedDot : BeanBase
{
    public ERedDotKey Id { get; private set; }
    public int ParentId { get; private set; }
    public ERedDotDisplayMode DisplayMode { get; private set; }
    public ERedDotCleanStrategy CleanStrategy { get; private set; }
}
```

## 五、UI 层改造 — 废弃 RedDotRegister

### 5.1 CompRedDot 简化

`Register` 方法签名简化（不再需要 `target` 和 `offset`——组件已手动放置）：

```csharp
public partial class CompRedDot
{
    public enum DisplayMode { DotOnly, DotNumber, Auto }

    private ERedDotKey m_Key;
    private DisplayMode m_DisplayMode;

    // 简化后的 Register
    public void Register(ViewBase view, ERedDotKey redKey,
        DisplayMode? displayModeOverride = null)
    {
        uiView = view;
        m_Key = redKey;
        m_DisplayMode = displayModeOverride ?? GetDisplayModeFromConfig(redKey);

        GlobalModule.RedDotModule.Register(m_Key, OnRedDotChanged);
    }

    // 从配置表读取默认显示模式
    private static DisplayMode GetDisplayModeFromConfig(ERedDotKey key)
    {
        var node = GlobalModule.RedDotModule.GetNode(key);
        return (DisplayMode)(int)(node?.DisplayMode ?? ERedDotDisplayMode.DotOnly);
    }

    protected override void OnDispose()
    {
        GlobalModule.RedDotModule.Unregister(m_Key, OnRedDotChanged);
    }
}
```

### 5.2 FGUI 编辑器工作流（替代 RedDotRegister）

FGUI 编辑器中：

1. 手动拖拽 `CompRedDot` 组件到目标按钮/图标上
2. 选中 `CompRedDot` 实例，在"自定义数据"中填写：`i18n=1`
   - `1` 即 `ERedDotKey` 的枚举值
   - 可选：`i18n=1,mode=0` 覆盖显示模式
3. CSharpCodeGen 插件导出时，自动识别并生成注册代码

### 5.3 代码生成模板修改

**CompTemplate.txt / WinTemplate.txt** — `InitRedDot()` 从示例注释变为实际生成代码：

```csharp
// 改造后
private void InitRedDot()
{
    compRedDot1.Register(uiView, ERedDotKey.Bag_Item);
    compRedDot2.Register(uiView, ERedDotKey.Shop_Gift);
}
```

### 5.4 CSharpCodeGen 插件 Lua 脚本修改

`GenComp.lua` / `GenWin.lua` — 新增逻辑：

1. 遍历当前界面/组件的所有子组件
2. 筛选出类型为 `CompRedDot` 且自定义数据包含 `i18n=` 的实例
3. 生成对应的 `compXXX.Register(uiView, ERedDotKey.XXX)` 代码
4. 解析 `mode=` 可选参数，生成 displayModeOverride 参数

## 六、文件变更清单

### 新增
| 文件 | 说明 |
|---|---|
| `Config/Excels/.../R-RedDot-红点表.xlsx` | 红点配置表 |

### 修改
| 文件 | 改动说明 |
|---|---|
| `Config/Excels/__enums__.xlsx` | 新增 ERedDotKey / ERedDotDisplayMode / ERedDotCleanStrategy |
| `RedDotModule.cs` | 初始化走 Luban；双字典；两套 API；TryAutoClean；AddDynamicChild |
| `RedDotNode.cs` | 新增字段（StaticKey/DynamicKey/DisplayMode/CleanStrategy/SetParent） |
| `CompRedDot.cs` | Register 签名简化，DisplayMode 从配置表读取 |
| `CompTemplate.txt` | InitRedDot 从示例改为实际生成代码 |
| `WinTemplate.txt` | 同上 |
| `GenComp.lua` | 新增识别 CompRedDot + 自定义数据逻辑 |
| `GenWin.lua` | 同上 |
| 业务层调用点（~25 个文件） | `RedDotKeys.Xxx` → `ERedDotKey.Xxx` |

### 废弃/删除
| 文件 | 说明 |
|---|---|
| `RedDotSetting.asset` | SO 配置不再需要 |
| `RedDotSetting.cs` | ScriptableObject 类 |
| `RedDotNodeData.cs` | 配置数据结构 |
| `RedDotSettingEditor.cs` 及 5 个 partial 文件 | 自定义编辑器面板（~1000 行） |
| `RedDotSettingCreator.cs` | 编辑器创建工具 |
| `RedDotKeys.cs` | Luban 生成的 ERedDotKey 枚举替代 |
| `RedDotRegister.cs` | FGUI 编辑器层驱动替代 |

### 无需改动
| 项目 | 说明 |
|---|---|
| `gen-client-bin.bat` | Luban 自动收集 Excel 并生成代码 |
| `__beans__.xlsx` | 不走 Luban Bean，运行时代码定义 |
| `__tables__.xlsx` | Luban 自动收集，无需手动注册 |
| `RedDotNode` 计数传播逻辑 | 核心父子计数传播算法不变 |

## 七、迁移步骤

1. 在 `__enums__.xlsx` 中定义枚举（按现有红点树提取所有 Key）
2. 创建 `R-RedDot-红点表.xlsx`，将现有 SO 配置迁移为表数据
3. 运行 `gen-client-bin.bat` 生成代码
4. 改造 `RedDotNode` + `RedDotModule` 运行时核心
5. 改造 `CompRedDot` UI 组件
6. 改造 CSharpCodeGen 插件模板和 Lua 脚本
7. 迁移所有业务层调用点（`RedDotKeys.Xxx` → `ERedDotKey.Xxx`）
8. 删除废弃文件
9. Unity Editor 手动测试验证

## 八、设计自检清单

- [x] 无 placeholder / TODO
- [x] 前后一致：架构描述与变更清单匹配
- [x] 范围可控：单一红点系统改造，不涉及其他模块
- [x] 无歧义：所有接口签名、文件路径均已明确
