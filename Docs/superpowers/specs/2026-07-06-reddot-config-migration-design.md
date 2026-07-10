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
| 9 | RedDotModule 程序集 | 从 AOT 迁移至 Hotfix，可直接引用 Luban 生成类型 |

## 三、Luban 配置定义

### 3.1 枚举定义 (`__enums__.xlsx` 新增)

```xml
<!-- ERedDotKey — 红点节点标识 -->
枚举值按区间分配，每个根节点预留 1000 个值的空间，方便后续插入:
  Bag=1000, Bag_Item=1001, Bag_Skill=1002,          // 1000~1999
  Shop=2000, Shop_Gift=2001, Shop_Res=2002,          // 2000~2999
  Hero=3000, Hero_Equip=3001, Hero_Skill=3002, Hero_Skin=3003, // 3000~3999
  Battle=4000, Battle_Team=4001                       // 4000~4999
  // 后续根节点: 5000~5999, 6000~6999, ...

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
| ##备注 | `string` | 策划备注（注释列，不进入运行时数据） |
| ParentId | `int?` | 父节点 Id，可空，空表示根节点 |
| DisplayMode | `ERedDotDisplayMode` | 默认显示模式 |
| CleanStrategy | `ERedDotCleanStrategy` | 清理策略 |

数据示例：

| Id | ##备注 | ParentId | DisplayMode | CleanStrategy |
|---|---|---|---|---|
| Bag(=1000) | 背包 | (空) | DotOnly | Manual |
| Bag_Item(=1001) | 背包.道具 | 1000 | DotNumber | Manual |
| Bag_Skill(=1002) | 背包.技能 | 1000 | DotOnly | Manual |
| Shop(=2000) | 商店 | (空) | DotNumber | Manual |
| Shop_Gift(=2001) | 商店.礼包 | 2000 | DotOnly | Manual |
| Shop_Res(=2002) | 商店.资源 | 2000 | DotOnly | Manual |

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
public class RedDotModule : ModuleBase
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
            if (row.ParentId == null) continue;
            if (!StaticNodes.TryGetValue(row.Id, out var child) ||
                !StaticNodes.TryGetValue((ERedDotKey)row.ParentId.Value, out var parent)) continue;
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
    public int? ParentId { get; private set; }
    public ERedDotDisplayMode DisplayMode { get; private set; }
    public ERedDotCleanStrategy CleanStrategy { get; private set; }
}
```

## 五、UI 层改造 — 废弃 RedDotRegister

### 5.1 CompRedDot 改造

提供两套 `Register` 重载——静态节点走枚举（由 FGUI 插件生成），动态节点走字符串（业务代码手动调用）。不含 `DisplayMode` 本地字段——显示时直接从 `RedDotNode` 配置读取：

```csharp
public partial class CompRedDot
{
    public enum DisplayMode { DotOnly = 0, DotNumber = 1, Auto = 2 }

    private ERedDotKey? m_StaticKey;
    private string m_DynamicKey;

    // 静态节点（枚举，DisplayMode 由配置表决定）
    public void Register(ViewBase view, ERedDotKey redKey)
    {
        uiView = view;
        m_StaticKey = redKey;
        GlobalModule.RedDotModule.Register(redKey, OnRedDotChanged);
    }

    // 动态节点（字符串，默认 DotOnly）
    public void Register(ViewBase view, string redKey)
    {
        uiView = view;
        m_DynamicKey = redKey;
        GlobalModule.RedDotModule.Register(redKey, OnRedDotChanged);
    }

    // 从 RedDotNode 配置读取 DisplayMode
    private DisplayMode GetDisplayMode()
    {
        if (m_StaticKey.HasValue)
        {
            var node = GlobalModule.RedDotModule.GetNode(m_StaticKey.Value);
            return (DisplayMode)(int)(node?.DisplayMode ?? ERedDotDisplayMode.DotOnly);
        }
        return DisplayMode.DotOnly; // 动态节点默认
    }

    private void OnRedDotChanged(int redCount)
    {
        var mode = GetDisplayMode();
        switch (mode)
        {
            case DisplayMode.DotOnly:
                txtCount.visible = false;
                imgRedDot.visible = redCount > 0;
                break;
            case DisplayMode.DotNumber:
                txtCount.visible = redCount >= 1;
                imgRedDot.visible = redCount > 0;
                txtCount.text = FormatRedDotCount(redCount);
                break;
            case DisplayMode.Auto:
                txtCount.visible = redCount > 1;
                imgRedDot.visible = redCount > 0;
                txtCount.text = FormatRedDotCount(redCount);
                break;
        }
    }

    protected override void OnDispose()
    {
        if (m_StaticKey.HasValue)
            GlobalModule.RedDotModule.Unregister(m_StaticKey.Value, OnRedDotChanged);
        else if (m_DynamicKey != null)
            GlobalModule.RedDotModule.Unregister(m_DynamicKey, OnRedDotChanged);
    }
}
```

### 5.2 FGUI 编辑器工作流（替代 RedDotRegister）

FGUI 编辑器中：

1. 手动拖拽 `CompRedDot` 组件到目标按钮/图标上
2. 选中 `CompRedDot` 实例，在"自定义数据"中填写：`i18n=ERedDotKey.Bag_Item`
   - 直接写枚举全名，插件生成代码时原样输出
   - DisplayMode 由配置表决定，无需额外指定
3. CSharpCodeGen 插件导出时，自动识别并生成注册代码

### 5.3 代码生成模板修改

**CompGenTemplate.txt / WinGenTemplate.txt** — `.Gen.cs` 中新增 `InitRedDot()` 方法（每次导出自动更新）：

```csharp
/// <summary>
/// 初始化红点注册（自动生成，不可手动修改）
/// </summary>
private void InitRedDot()
{
#RedDotRegister#
}
```

**CompTemplate.txt / WinTemplate.txt** — `OnInit()` 不再包含 `InitRedDot()`：

```csharp
// 改造前
private void OnInit()
{
    InitEvent();
    InitRedDot();  // ← 移除
}

// 改造后
private void OnInit()
{
    InitEvent();
}
```

**生成的 `.Gen.cs` 效果：**

```csharp
private void InitRedDot()
{
    compRedDot1.Register(uiView, ERedDotKey.Bag_Item);
    compRedDot2.Register(uiView, ERedDotKey.Shop_Gift);
}
```

### 5.4 CSharpCodeGen 插件 Lua 脚本修改

`GenCommon.lua` — 新增两个函数：

**`GenRedDotRegister(dataList, compCls)`** — 入口函数，获取 XML displayList，递归扫描。

**`FindRedDotComps(xmlNode, dataList)`** — 递归遍历：

1. 遍历 `xmlNode.elements` 中每个元素
2. 读取 `customData` 属性，匹配 `i18n=ERedDotKey.xxx`（正则：`i18n=(ERedDotKey%.%w+)`）
3. 匹配成功则原样输出枚举名，生成 `compXxx.Register(uiView, ERedDotKey.Xxx);`
4. 递归处理子元素

`GenWin.lua` / `GenComp.lua` — 在生成流程中增加调用：

```lua
GenCommon:GenRedDotRegister(dataTable['#RedDotRegister#'], winCls)  -- Win
GenCommon:GenRedDotRegister(dataDict['#RedDotRegister#'], compCls)  -- Comp
```

并在 dataKeys 中增加 `'#RedDotRegister#'`。

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
| `CompRedDot.cs` | Register 两套重载（枚举 + string），支持静态和动态节点 |
| `CompGenTemplate.txt` | 新增 InitRedDot() + #RedDotRegister# placeholder |
| `WinGenTemplate.txt` | 同上 |
| `CompTemplate.txt` | OnInit() 中移除 InitRedDot() |
| `WinTemplate.txt` | 同上 |
| `GenCommon.lua` | 新增 GenRedDotRegister / FindRedDotComps 函数 |
| `GenComp.lua` | 新增 #RedDotRegister# dataKey + 调用 GenRedDotRegister |
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
