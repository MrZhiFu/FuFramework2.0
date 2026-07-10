# 红点系统配置表化实施方案

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将红点系统从 ScriptableObject 配置迁移至 Luban 配置表，RedDotModule 从 AOT 迁移至 Hotfix，用 `ERedDotKey` 枚举替代字符串 Key，废弃 `RedDotRegister` 改为 FGUI 编辑器层代码自动生成。

**Architecture:** RedDotModule 移入 Hotfix 程序集，直接引用 Luban 生成的 `ERedDotKey` 枚举和 `TbRedDot` 配置表。双字典（静态枚举 + 动态 string）管理节点。FGUI CSharpCodeGen 插件识别 CompRedDot 的 `i18n=ERedDotKey.Xxx` 自定义数据，自动生成 `compXxx.Register(uiView, ...)` 注册代码。

**Tech Stack:** C# (Unity 2022.3), Luban (配置表), Lua (FGUI 插件), FairyGUI, HybridCLR

## Global Constraints

- RedDotModule / RedDotNode 从 `FuFramework.RedDot.Runtime` (AOT) 迁移至 `Assets/Scripts/Hotfix/RedDot/Runtime/` (Hotfix)
- 直接引用 Luban 生成的 `ERedDotKey`, `ERedDotDisplayMode`, `ERedDotCleanStrategy`, `TbRedDot`
- 原 AOT 程序集中删除所有 RedDot 相关引用
- FGUI 自定义数据格式: `i18n=ERedDotKey.Xxx`，插件原样输出枚举名
- 枚举值按区间分配 (1000/2000/3000/4000)，ParentId 可空
- 项目无自动化测试，验证靠 Unity Editor 手动运行

---

### Task 1: Luban 枚举与配置表定义

**Files:**
- Create: `Config/Excels/Tables/R-RedDot-红点表.xlsx`
- Modify: `Config/Excels/__enums__.xlsx`

**Interfaces:**
- Produces: Luban 生成 `ERedDotKey.cs`, `ERedDotDisplayMode.cs`, `ERedDotCleanStrategy.cs`, `RedDot.cs`, `TbRedDot.cs` — 被 Task 2/3/5/6 消费

- [ ] **Step 1: 在 `__enums__.xlsx` 中新增三个枚举**

打开 `Config/Excels/__enums__.xlsx`，新增 sheet：

```
// ERedDotKey
枚举名: ERedDotKey
  Bag         = 1000
  Bag_Item    = 1001
  Bag_Skill   = 1002
  Shop        = 2000
  Shop_Gift   = 2001
  Shop_Res    = 2002
  Hero        = 3000
  Hero_Equip  = 3001
  Hero_Skill  = 3002
  Hero_Skin   = 3003
  Battle      = 4000
  Battle_Team = 4001
```

```
// ERedDotDisplayMode
枚举名: ERedDotDisplayMode
  DotOnly   = 0
  DotNumber = 1
  Auto      = 2
```

```
// ERedDotCleanStrategy
枚举名: ERedDotCleanStrategy
  Manual        = 0
  ViewAutoClean = 1
```

- [ ] **Step 2: 创建红点配置表 Excel**

新建 `Config/Excels/Tables/R-RedDot-红点表.xlsx`，表头：

| Id | ##备注 | ParentId | DisplayMode | CleanStrategy |
|---|---|---|---|---|
| 1000 | 背包 | (空) | 0 | 0 |
| 1001 | 背包.道具 | 1000 | 1 | 0 |
| 1002 | 背包.技能 | 1000 | 0 | 0 |
| 2000 | 商店 | (空) | 1 | 0 |
| 2001 | 商店.礼包 | 2000 | 0 | 0 |
| 2002 | 商店.资源 | 2000 | 0 | 0 |
| 3000 | 英雄 | (空) | 0 | 0 |
| 3001 | 英雄.装备 | 3000 | 0 | 0 |
| 3002 | 英雄.技能 | 3000 | 0 | 0 |
| 3003 | 英雄.皮肤 | 3000 | 0 | 0 |
| 4000 | 战斗 | (空) | 0 | 0 |
| 4001 | 战斗.队伍 | 4000 | 0 | 0 |

> Id 列类型设为 `ERedDotKey`，DisplayMode 列类型设为 `ERedDotDisplayMode`，CleanStrategy 列类型设为 `ERedDotCleanStrategy`，ParentId 为可空整数。

- [ ] **Step 3: 运行 gen-client-bin.bat 生成代码**

```bash
cd Config && ./gen-client-bin.bat
```

- [ ] **Step 4: 验证生成产物**

检查以下文件已生成：
- `Assets/Scripts/Hotfix/Config/Generate/ERedDotKey.cs`
- `Assets/Scripts/Hotfix/Config/Generate/ERedDotDisplayMode.cs`
- `Assets/Scripts/Hotfix/Config/Generate/ERedDotCleanStrategy.cs`
- `Assets/Scripts/Hotfix/Config/Generate/Tables/RedDot.cs`
- `Assets/Scripts/Hotfix/Config/Generate/Tables/TbRedDot.cs`
- `Assets/Scripts/Hotfix/Config/Generate/TableManager.cs` 中包含 `TbRedDot` 字段

- [ ] **Step 5: Commit**

```bash
git add Config/Excels/__enums__.xlsx Config/Excels/Tables/R-RedDot-红点表.xlsx
git add Unity/Assets/Scripts/Hotfix/Config/Generate/
git commit -m "feat: 新增红点 Luban 枚举和配置表定义"
```

---

### Task 2: RedDotNode 改造并移至 Hotfix

**Files:**
- Create: `Unity/Assets/Scripts/Hotfix/RedDot/Runtime/RedDotNode.cs`
- Modify: `Unity/Assets/Scripts/FuFramework/RedDot/Runtime/RedDotNode.cs` → 删除

**Interfaces:**
- Consumes: Task 1 的 `ERedDotKey`, `ERedDotDisplayMode`, `ERedDotCleanStrategy`
- Produces: `RedDotNode` 类 — 被 Task 3, Task 4 消费

- [ ] **Step 1: 在 Hotfix 目录创建新的 RedDotNode.cs**

```csharp
// Assets/Scripts/Hotfix/RedDot/Runtime/RedDotNode.cs
using System;
using System.Collections.Generic;
using FuFramework.Core.Runtime;
using FuFramework.ReferencePool.Runtime;
using Hotfix.Config; // Luban 生成的枚举所在命名空间

namespace Hotfix.RedDot.Runtime
{
    public class RedDotNode : IReference
    {
        /// <summary>静态节点 Key（配置表定义的节点）</summary>
        public ERedDotKey? StaticKey { get; private set; }

        /// <summary>动态节点 Key（运行时创建的节点，如道具实例红点）</summary>
        public string DynamicKey { get; private set; }

        /// <summary>自身计数</summary>
        public int RawCount { get; private set; }

        /// <summary>总计数（自身 + 所有子节点）</summary>
        public int TotalCount { get; private set; }

        /// <summary>父节点</summary>
        public RedDotNode Parent { get; private set; }

        /// <summary>默认显示模式（来自配置表）</summary>
        public ERedDotDisplayMode DisplayMode { get; private set; }

        /// <summary>清理策略（来自配置表）</summary>
        public ERedDotCleanStrategy CleanStrategy { get; private set; }

        private readonly List<RedDotNode> m_Children = new();

        /// <summary>计数变化事件</summary>
        public event Action<int> OnCountChanged;

        /// <summary>从配置表创建静态节点</summary>
        public static RedDotNode Create(ERedDotKey key, RedDotNode parent,
            ERedDotDisplayMode displayMode, ERedDotCleanStrategy cleanStrategy)
        {
            var node = ReferencePool.Runtime.ReferencePool.Acquire<RedDotNode>();
            node.StaticKey = key;
            node.Parent = parent;
            node.DisplayMode = displayMode;
            node.CleanStrategy = cleanStrategy;
            return node;
        }

        /// <summary>运行时创建动态节点</summary>
        public static RedDotNode CreateDynamic(string key, RedDotNode parent)
        {
            var node = ReferencePool.Runtime.ReferencePool.Acquire<RedDotNode>();
            node.DynamicKey = key;
            node.Parent = parent;
            node.DisplayMode = ERedDotDisplayMode.DotOnly;
            node.CleanStrategy = ERedDotCleanStrategy.Manual;
            return node;
        }

        /// <summary>两阶段构建 — 初始化后设置父节点</summary>
        public void SetParent(RedDotNode parent) => Parent = parent;

        /// <summary>添加子节点</summary>
        public void AddChild(RedDotNode child)
        {
            if (m_Children.Contains(child))
            {
                FuLogger.LogWarning($"[RedDotNode] 重复子节点: {child.StaticKey}");
                return;
            }
            m_Children.Add(child);
        }

        /// <summary>移除子节点（动态节点归零回收时使用）</summary>
        public void RemoveChild(RedDotNode child) => m_Children.Remove(child);

        /// <summary>获取所有子节点（只读）</summary>
        public IReadOnlyList<RedDotNode> GetChildren() => m_Children.AsReadOnly();

        /// <summary>设置计数，自动向上传播</summary>
        public void SetCount(int count)
        {
            if (RawCount == count) return;
            RawCount = count;
            UpdateTotalCount();
        }

        private void UpdateTotalCount()
        {
            var childrenTotal = 0;
            foreach (var child in m_Children)
                childrenTotal += child.TotalCount;

            var total = RawCount + childrenTotal;
            if (TotalCount == total) return;

            TotalCount = total;
            OnCountChanged?.Invoke(TotalCount);
            Parent?.UpdateTotalCount();
        }

        /// <summary>IReference — 回收到对象池时清理</summary>
        public void Clear()
        {
            StaticKey = null;
            DynamicKey = null;
            Parent = null;
            RawCount = 0;
            TotalCount = 0;
            m_Children.Clear();
            OnCountChanged = null;
            DisplayMode = ERedDotDisplayMode.DotOnly;
            CleanStrategy = ERedDotCleanStrategy.Manual;
        }

        /// <summary>清除所有事件监听</summary>
        public void ClearAllListeners() => OnCountChanged = null;
    }
}
```

- [ ] **Step 2: 确保 Hotfix 程序集定义包含新目录**

检查 `Assets/Scripts/Hotfix/` 目录下的 `.asmdef` 文件，确认 `Hotfix/RedDot/Runtime/` 在程序集覆盖范围内。如果需要新建，复制现有 Hotfix asmdef 引用配置。

- [ ] **Step 3: 删除 AOT 中的旧 RedDotNode.cs**

```bash
rm "Unity/Assets/Scripts/FuFramework/RedDot/Runtime/RedDotNode.cs"
```

- [ ] **Step 4: Commit**

```bash
git add Unity/Assets/Scripts/Hotfix/RedDot/Runtime/RedDotNode.cs
git rm Unity/Assets/Scripts/FuFramework/RedDot/Runtime/RedDotNode.cs
git commit -m "refactor: RedDotNode 迁移至 Hotfix，新增配置字段"
```

---

### Task 3: RedDotModule 改造并移至 Hotfix

**Files:**
- Create: `Unity/Assets/Scripts/Hotfix/RedDot/Runtime/RedDotModule.cs`
- Modify: `Unity/Assets/Scripts/FuFramework/RedDot/Runtime/RedDotModule.cs` → 删除
- Modify: `Unity/Assets/Scripts/FuFramework/Launcher/Runtime/Launcher.Modules.cs`
- Modify: `Unity/Assets/Scripts/FuFramework/Launcher/Runtime/GlobalModule.cs`

**Interfaces:**
- Consumes: Task 2 的 `RedDotNode`，Task 1 的 `ERedDotKey`, `TbRedDot`
- Produces: `RedDotModule` 类 — 被 Task 4, Task 6 消费

- [ ] **Step 1: 在 Hotfix 目录创建新的 RedDotModule.cs**

```csharp
// Assets/Scripts/Hotfix/RedDot/Runtime/RedDotModule.cs
using System;
using System.Collections.Generic;
using FuFramework.Core.Runtime;
using FuFramework.ReferencePool.Runtime;
using Hotfix.Config;
using Hotfix.Config.Tables;

namespace Hotfix.RedDot.Runtime
{
    public class RedDotModule : ModuleBase
    {
        /// <summary>快速访问实例（替代 GlobalModule.RedDotModule）</summary>
        public static RedDotModule Instance => ModuleManager.GetModule<RedDotModule>();

        // ========== 双字典存储（无装箱） ==========
        private static readonly Dictionary<ERedDotKey, RedDotNode> StaticNodes = new();
        private static readonly Dictionary<string, RedDotNode> DynamicNodes = new();

        // ========== 生命周期 ==========

        protected override void OnInit()
        {
            var tbRedDot = GlobalModule.ConfigModule.GetConfig<TbRedDot>();
            if (tbRedDot == null || tbRedDot.DataList.Count == 0)
            {
                FuLogger.LogError("[RedDotModule] 红点配置表不存在或为空.");
                return;
            }

            StaticNodes.Clear();
            DynamicNodes.Clear();

            // 阶段一：创建所有节点（row.Id 为 ERedDotKey 类型，Luban 直接生成枚举）
            foreach (var row in tbRedDot.DataList)
            {
                var node = RedDotNode.Create(
                    row.Id, null, row.DisplayMode, row.CleanStrategy);

                if (!StaticNodes.TryAdd(row.Id, node))
                {
                    FuLogger.LogError($"[RedDotModule] 重复的节点key: {row.Id}");
                    ReferencePool.Runtime.ReferencePool.Release(node);
                }
            }

            // 阶段二：建立父子关系
            foreach (var row in tbRedDot.DataList)
            {
                if (row.ParentId == null) continue;
                var parentKey = (ERedDotKey)row.ParentId.Value; // ParentId 是 int?，需强转

                if (!StaticNodes.TryGetValue(row.Id, out var child) ||
                    !StaticNodes.TryGetValue(parentKey, out var parent))
                    continue;

                child.SetParent(parent);
                parent.AddChild(child);
            }

            FuLogger.LogInfo($"[RedDotModule] 初始化完成, 静态节点: {StaticNodes.Count}");
        }

        protected override void OnDispose()
        {
            foreach (var node in StaticNodes.Values)
                ReferencePool.Runtime.ReferencePool.Release(node);
            foreach (var node in DynamicNodes.Values)
                ReferencePool.Runtime.ReferencePool.Release(node);
            StaticNodes.Clear();
            DynamicNodes.Clear();
        }

        // ========== 动态节点 ==========

        public RedDotNode AddDynamicChild(ERedDotKey parentKey, string childName)
        {
            if (!StaticNodes.TryGetValue(parentKey, out var parentNode))
            {
                FuLogger.LogError($"[RedDotModule] 父节点不存在: {parentKey}");
                return null;
            }
            if (DynamicNodes.ContainsKey(childName))
                return DynamicNodes[childName];

            var node = RedDotNode.CreateDynamic(childName, parentNode);
            parentNode.AddChild(node);
            DynamicNodes.Add(childName, node);
            return node;
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

        public void Register(ERedDotKey key, Action<int> onChange, bool immediateNotify = true)
        {
            if (!StaticNodes.TryGetValue(key, out var node))
            {
                FuLogger.LogWarning($"[RedDotModule] Register 未找到节点: {key}");
                return;
            }
            node.OnCountChanged += onChange;
            if (immediateNotify) onChange?.Invoke(node.TotalCount);
        }

        public void Unregister(ERedDotKey key, Action<int> onChange)
        {
            if (!StaticNodes.TryGetValue(key, out var node)) return;
            node.OnCountChanged -= onChange;
        }

        public RedDotNode GetNode(ERedDotKey key)
            => StaticNodes.GetValueOrDefault(key);

        public int GetCount(ERedDotKey key)
            => StaticNodes.TryGetValue(key, out var node) ? node.TotalCount : 0;

        public bool HasNode(ERedDotKey key)
            => StaticNodes.ContainsKey(key);

        public void SetCount(ERedDotKey key, int count)
        {
            if (!StaticNodes.TryGetValue(key, out var node))
            {
                FuLogger.LogWarning($"[RedDotModule] SetCount 未找到节点: {key}");
                return;
            }
            node.SetCount(count);
        }

        public void ResetCount(ERedDotKey key) => SetCount(key, 0);

        // ========== 动态节点 API（string 重载） ==========

        public void Register(string key, Action<int> onChange, bool immediateNotify = true)
        {
            if (!DynamicNodes.TryGetValue(key, out var node))
            {
                FuLogger.LogWarning($"[RedDotModule] Register 未找到动态节点: {key}");
                return;
            }
            node.OnCountChanged += onChange;
            if (immediateNotify) onChange?.Invoke(node.TotalCount);
        }

        public void Unregister(string key, Action<int> onChange)
        {
            if (!DynamicNodes.TryGetValue(key, out var node)) return;
            node.OnCountChanged -= onChange;
        }

        public RedDotNode GetNode(string key)
            => DynamicNodes.GetValueOrDefault(key);

        public int GetCount(string key)
            => DynamicNodes.TryGetValue(key, out var node) ? node.TotalCount : 0;

        public bool HasNode(string key)
            => DynamicNodes.ContainsKey(key);

        public void SetCount(string key, int count)
        {
            if (!DynamicNodes.TryGetValue(key, out var node))
            {
                FuLogger.LogWarning($"[RedDotModule] SetCount 未找到动态节点: {key}");
                return;
            }
            node.SetCount(count);

            // 归零自动回收
            if (node.RawCount == 0)
            {
                node.Parent?.RemoveChild(node);
                DynamicNodes.Remove(key);
                ReferencePool.Runtime.ReferencePool.Release(node);
            }
        }

        public void ResetCount(string key) => SetCount(key, 0);
    }
}
```

- [ ] **Step 2: 在 HotfixLauncher.Main() 中注册 RedDotModule**

文件: `Unity/Assets/Scripts/Hotfix/HotfixLauncher.cs`

在 `Main()` 方法中，`LoadConfig()` 已执行完毕（配置表可用），`BindCustomComps()` 之后、打开 UI 之前，插入模块注册：

```csharp
// 注册 RedDotModule（从 AOT 迁移至 Hotfix，需放在 LoadConfig 之后）
ModuleManager.RegisterModule<RedDotModule>();
```

完整插入位置（Main 方法内）：

```csharp
// 绑定自动生成的Fui自定义组件(HotFix下)
BindCustomComps();

// 注册 RedDotModule（从 AOT 迁移至 Hotfix）
ModuleManager.RegisterModule<RedDotModule>();  // ← 新增

// 指定获取多语言的接口
GlobalModule.LocalizationModule.LocalizationProvider = new LocalizationProvider();
```

同时添加 using:
```csharp
using Hotfix.RedDot.Runtime;  // RedDotModule 所在命名空间
```

- [ ] **Step 3: 从 AOT Launcher.Modules.cs 中删除 RedDotModule 注册**

```csharp
// Unity/Assets/Scripts/FuFramework/Launcher/Runtime/Launcher.Modules.cs
// 删除: using FuFramework.RedDot.Runtime;
// 删除: ModuleManager.RegisterModule<RedDotModule>();
```

- [ ] **Step 4: 从 AOT GlobalModule.cs 中删除 RedDotModule 引用**

```csharp
// Unity/Assets/Scripts/FuFramework/Launcher/Runtime/GlobalModule.cs
// 删除: using FuFramework.RedDot.Runtime;
// 删除: private static RedDotModule m_RedDotModule;
// 删除: public static RedDotModule RedDotModule => ...
```

- [ ] **Step 5: 删除 AOT 中的旧 RedDotModule.cs**

```bash
rm "Unity/Assets/Scripts/FuFramework/RedDot/Runtime/RedDotModule.cs"
```

- [ ] **Step 6: Commit**

```bash
git add Unity/Assets/Scripts/Hotfix/RedDot/Runtime/RedDotModule.cs
git add Unity/Assets/Scripts/FuFramework/Launcher/Runtime/Launcher.Modules.cs
git add Unity/Assets/Scripts/FuFramework/Launcher/Runtime/GlobalModule.cs
git rm Unity/Assets/Scripts/FuFramework/RedDot/Runtime/RedDotModule.cs
git commit -m "refactor: RedDotModule 迁移至 Hotfix，接入 Luban 配置表"
```

---

### Task 4: CompRedDot UI 组件改造

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/UI/Common/Impl/Comp/CompRedDot.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/UI/Common/Gen/Comp/CompRedDot.Gen.cs`（如果 Init 调用链有 `OnInit()` 需要调整）

**Interfaces:**
- Consumes: Task 3 的 `RedDotModule` API
- Produces: 新 `CompRedDot.Register()` 签名 — 被 Task 5, Task 6 消费

- [ ] **Step 1: 改造 CompRedDot.cs — 全部替换 Impl 部分**

读取现有 `CompRedDot.cs`，用以下代码替换：

```csharp
using System;
using FairyGUI;
using UnityEngine;
using FuFramework.UI.Runtime;
using Hotfix.Config;
using Hotfix.RedDot.Runtime;

namespace Hotfix.UI
{
    public partial class CompRedDot
    {
        public enum DisplayMode { DotOnly = 0, DotNumber = 1, Auto = 2 }

        private ERedDotKey? m_StaticKey;
        private string m_DynamicKey;

        /// <summary>静态节点注册（枚举，DisplayMode 由配置表决定）</summary>
        public void Register(ViewBase view, ERedDotKey redKey)
        {
            uiView = view;
            m_StaticKey = redKey;
            RedDotModule.Instance.Register(redKey, OnRedDotChanged);
        }

        /// <summary>动态节点注册（字符串，默认 DotOnly）</summary>
        public void Register(ViewBase view, string redKey)
        {
            uiView = view;
            m_DynamicKey = redKey;
            RedDotModule.Instance.Register(redKey, OnRedDotChanged);
        }

        /// <summary>从 RedDotNode 配置读取 DisplayMode</summary>
        private DisplayMode GetDisplayMode()
        {
            if (m_StaticKey.HasValue)
            {
                var node = RedDotModule.Instance.GetNode(m_StaticKey.Value);
                return (DisplayMode)(int)(node?.DisplayMode ?? ERedDotDisplayMode.DotOnly);
            }
            return DisplayMode.DotOnly;
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

        private static string FormatRedDotCount(int count)
        {
            return count switch
            {
                <= 0 => "0",
                > 99 => "99+",
                _ => count.ToString()
            };
        }

        /// <summary>手动刷新红点（列表 Item 复用场景）</summary>
        public void SetRedDot(int redCount) => OnRedDotChanged(redCount);

        public void SetRedDotPos(Vector2 offset = default)
        {
            var posX = this.parent?.width - width + offset.x ?? offset.x;
            SetXY(posX, offset.y);
        }

        private void OnDispose()
        {
            if (m_StaticKey.HasValue)
                RedDotModule.Instance.Unregister(m_StaticKey.Value, OnRedDotChanged);
            else if (m_DynamicKey != null)
                RedDotModule.Instance.Unregister(m_DynamicKey, OnRedDotChanged);
        }
    }
}
```

- [ ] **Step 2: 检查 .Gen.cs 中的 Init 调用链**

`CompRedDot.Gen.cs` 中的 `Init()` 方法调用 `OnInit()`。确认新 `CompRedDot.cs` 中不再有 `OnInit()` 方法（原有逻辑已整合到 `Register` 中）。

- [ ] **Step 3: Commit**

```bash
git add Unity/Assets/Scripts/Hotfix/UI/Common/Impl/Comp/CompRedDot.cs
git commit -m "refactor: CompRedDot 适配新 RedDotModule，双重重载支持静态/动态节点"
```

---

### Task 5: FGUI CSharpCodeGen 插件改造

**Files:**
- Modify: `FairyGUIProject/plugins/CSharpCodeGen/Template/CompGenTemplate.txt`
- Modify: `FairyGUIProject/plugins/CSharpCodeGen/Template/WinGenTemplate.txt`
- Modify: `FairyGUIProject/plugins/CSharpCodeGen/Template/CompTemplate.txt`
- Modify: `FairyGUIProject/plugins/CSharpCodeGen/Template/WinTemplate.txt`
- Modify: `FairyGUIProject/plugins/CSharpCodeGen/Src/GenCommon.lua`
- Modify: `FairyGUIProject/plugins/CSharpCodeGen/Src/GenComp.lua`
- Modify: `FairyGUIProject/plugins/CSharpCodeGen/Src/GenWin.lua`

**Interfaces:**
- Consumes: Task 4 的 `CompRedDot.Register(ViewBase, ERedDotKey)`
- Produces: `.Gen.cs` 文件自动生成 `InitRedDot()` 方法

- [ ] **Step 1: 修改 CompGenTemplate.txt**

在 `InitUIEvent()` 方法之后、`Init()` 返回之前，新增 placeholders：

```
#CompDefine#
		/// <summary>
		/// 初始化红点注册（自动生成，不可手动修改）
		/// </summary>
		private void InitRedDot()
		{
#RedDotRegister#
		}
```

确保 `Init()` 方法中在调用 `OnInit()` 之前加上 `InitRedDot()`。找到 `.Gen.cs` 中 `Init()` 方法的位置：

```csharp
public void Init(ViewBase view)
{
    // ... 已有逻辑 ...
    uiView = view;
    InitUIEvent();
    InitRedDot();   // ← 新增这一行
    OnInit();
}
```

- [ ] **Step 2: 修改 WinGenTemplate.txt**

同上，在 `InitUIEvent()` 后、`Init()` 返回前添加 `InitRedDot()`。

Win 的 `OnInit()` 流程在 `.cs` 模板中，`.Gen.cs` 的 `Init()` 方法结构不同。检查 `WinGenTemplate.txt` 的实际结构，在合适位置插入 `InitRedDot()` 调用。

- [ ] **Step 3: 修改 CompTemplate.txt — 删除 InitRedDot()**

```csharp
// 改造前
private void OnInit()
{
    InitEvent();
    InitRedDot();  // ← 删除
}

// 改造后
private void OnInit()
{
    InitEvent();
}
```

- [ ] **Step 4: 修改 WinTemplate.txt — 删除 InitRedDot()**

同上。

- [ ] **Step 5: GenCommon.lua — 新增红点注册代码生成函数**

在 `GenCommon.lua` 末尾添加：

```lua
--- 生成红点注册代码
--- 遍历组件/界面的XML displayList，查找自定义数据包含 i18n=ERedDotKey.Xxx 的 CompRedDot 实例
---@param dataList table 生成的代码将追加到此数组
---@param compCls CS.FairyEditor.PublishHandler.ClassInfo 组件或界面类信息
function GenCommon:GenRedDotRegister(dataList, compCls)
    local handler = Tool:Handler()
    local desc = handler:GetItemDesc(compCls.res)
    local displayList = desc:GetNode("displayList")
    if not displayList then
        return
    end
    self:FindRedDotComps(displayList, dataList)
end

--- 递归遍历XML节点，查找含 i18n= 自定义数据的元素
---@param xmlNode CS.FairyGUI.Utils.XML
---@param dataList table
function GenCommon:FindRedDotComps(xmlNode, dataList)
    local elements = xmlNode.elements
    local cnt = elements.Count
    for i = 1, cnt do
        local element = elements[i - 1]
        local name = element:GetAttribute("name") or ""
        local customData = element:GetAttribute("customData") or ""

        -- 匹配 i18n=ERedDotKey.Xxx 格式
        local enumName = customData:match("i18n=(ERedDotKey%.%w+)")
        if enumName then
            local varName = Tool:FormatVarName(name)
            table.insert(dataList, string.format(
                "\t\t\t%s.Register(uiView, %s);\n", varName, enumName))
        end

        -- 递归处理子元素
        self:FindRedDotComps(element, dataList)
    end
end
```

- [ ] **Step 6: GenComp.lua — 集成红点代码生成**

在 `dataKeys` 数组中增加 `'#RedDotRegister#'`：

```lua
local dataKeys = {
    '#CompDefine#',
    '#CompInit#',
    '#CustomCompInit#',
    '#INITUIEVENT#',
    '#RedDotRegister#',  -- 新增
}
```

在 `dataDict` 初始化后、模板替换前，增加调用：

```lua
GenCommon:GenRedDotRegister(dataDict['#RedDotRegister#'], compCls)
```

- [ ] **Step 7: GenWin.lua — 同上**

在 `dataKeys` 数组中增加 `'#RedDotRegister#'`，并在合适位置增加：

```lua
GenCommon:GenRedDotRegister(dataTable['#RedDotRegister#'], winCls)
```

- [ ] **Step 8: Commit**

```bash
git add FairyGUIProject/plugins/CSharpCodeGen/Template/
git add FairyGUIProject/plugins/CSharpCodeGen/Src/
git commit -m "feat: FGUI 插件支持 CompRedDot 自定义数据自动生成注册代码"
```

---

### Task 6: 业务层迁移 — RedDotKeys → ERedDotKey

**Files (19个):**
- `Unity/Assets/Scripts/Hotfix/UI/Tips/Impl/WinDialogMessageBox.cs`
- `Unity/Assets/Scripts/Hotfix/UI/Main/Impl/WinMain.cs`
- `Unity/Assets/Scripts/Hotfix/UI/Login/Impl/WinPlayerList.cs`
- `Unity/Assets/Scripts/Hotfix/UI/Login/Impl/WinPlayerCreate.cs`
- `Unity/Assets/Scripts/Hotfix/UI/Login/Impl/WinLoginAnnouncement.cs`
- `Unity/Assets/Scripts/Hotfix/UI/Login/Impl/WinLogin.cs`
- `Unity/Assets/Scripts/Hotfix/UI/Loading/Impl/WinLoadingScene.cs`
- `Unity/Assets/Scripts/Hotfix/UI/Guide/Impl/WinDialogGuide.cs`
- `Unity/Assets/Scripts/Hotfix/UI/Guide/Impl/WinClickGuide.cs`
- `Unity/Assets/Scripts/Hotfix/UI/Common/Impl/WinGlobalLoading.cs`
- `Unity/Assets/Scripts/Hotfix/UI/Bag/Impl/WinBag.cs`
- `Unity/Assets/Scripts/Hotfix/UI/Bag/Impl/Comp/CompBagItemInfo.cs`
- `Unity/Assets/Scripts/Hotfix/UI/Bag/Impl/Comp/CompGoodItem.cs`
- `Unity/Assets/Scripts/Hotfix/UI/Login/Impl/Comp/CompPlayerListItem.cs`
- `Unity/Assets/Scripts/Hotfix/UI/Bag/Impl/Comp/CompTypeItem.cs`
- `Unity/Assets/Scripts/Hotfix/UI/Bag/Impl/Comp/CompBagItem.cs`
- `Unity/Assets/Scripts/Hotfix/UI/Bag/Impl/Comp/CompBagContent.cs`

**Interfaces:**
- Consumes: Task 3 的 `RedDotModule.Instance`, Task 1 的 `ERedDotKey`

- [ ] **Step 1: 确认所有 RedDotKeys 引用**

每个文件中的 `RedDotKeys.Xxx` 需要替换为 `ERedDotKey.Xxx`。根据枚举定义中的命名映射：

| RedDotKeys (旧) | ERedDotKey (新) |
|---|---|
| `RedDotKeys.Bag` | `ERedDotKey.Bag` |
| `RedDotKeys.BagItem` | `ERedDotKey.Bag_Item` |
| `RedDotKeys.BagSkill` | `ERedDotKey.Bag_Skill` |
| `RedDotKeys.Shop` | `ERedDotKey.Shop` |
| `RedDotKeys.ShopGift` | `ERedDotKey.Shop_Gift` |
| `RedDotKeys.ShopRes` | `ERedDotKey.Shop_Res` |
| `RedDotKeys.Hero` | `ERedDotKey.Hero` |
| `RedDotKeys.HeroEquip` | `ERedDotKey.Hero_Equip` |
| `RedDotKeys.HeroSkill` | `ERedDotKey.Hero_Skill` |
| `RedDotKeys.HeroSkin` | `ERedDotKey.Hero_Skin` |
| `RedDotKeys.Battle` | `ERedDotKey.Battle` |
| `RedDotKeys.BattleTeam` | `ERedDotKey.Battle_Team` |

- [ ] **Step 2: 替换 GlobalModule.RedDotModule → RedDotModule.Instance**

所有 `GlobalModule.RedDotModule.XXX()` 调用改为 `RedDotModule.Instance.XXX()`。同时检查 `RedDotRegister.RegisterRedDot(...)` 调用——如果文件中有手动创建的 CompRedDot 并调用 Register，改为 `compXxx.Register(uiView, ERedDotKey.Xxx)`。

**同时添加必要的 using:**
```csharp
using Hotfix.Config;           // ERedDotKey, ERedDotDisplayMode 等枚举
using Hotfix.RedDot.Runtime;   // RedDotModule
```

**移除不再需要的 using:**
```csharp
// using FuFramework.ModuleSetting.Runtime; — 如果只引用了 RedDotKeys（已废弃），可一并移除
```

- [ ] **Step 3: 逐文件修改并验证编译**

修改所有 19 个文件后，在 Unity Editor 中确认编译通过。

- [ ] **Step 4: Commit**

```bash
git add Unity/Assets/Scripts/Hotfix/UI/
git commit -m "refactor: 业务层 RedDotKeys → ERedDotKey，GlobalModule.RedDotModule → RedDotModule.Instance"
```

---

### Task 7: 废弃文件清理

**Files:**
- `Unity/Assets/Scripts/FuFramework/ModuleSetting/SettingAssets/RedDotSetting.asset` — 删除
- `Unity/Assets/Scripts/FuFramework/ModuleSetting/Runtime/RedDot/RedDotSetting.cs` — 删除
- `Unity/Assets/Scripts/FuFramework/ModuleSetting/Runtime/RedDot/RedDotNodeData.cs` — 删除
- `Unity/Assets/Scripts/FuFramework/ModuleSetting/Editor/RedDot/RedDotSettingEditor.cs` — 删除
- `Unity/Assets/Scripts/FuFramework/ModuleSetting/Editor/RedDot/RedDotSettingEditor.CodeGeneration.cs` — 删除
- `Unity/Assets/Scripts/FuFramework/ModuleSetting/Editor/RedDot/RedDotSettingEditor.Navigation.cs` — 删除
- `Unity/Assets/Scripts/FuFramework/ModuleSetting/Editor/RedDot/RedDotSettingEditor.NodeManagement.cs` — 删除
- `Unity/Assets/Scripts/FuFramework/ModuleSetting/Editor/RedDot/RedDotSettingEditor.UI.cs` — 删除
- `Unity/Assets/Scripts/FuFramework/ModuleSetting/Editor/RedDot/RedDotSettingEditor.Validation.cs` — 删除
- `Unity/Assets/Scripts/FuFramework/ModuleSetting/Editor/RedDot/RedDotSettingEditor.Utility.cs` — 删除
- `Unity/Assets/Scripts/FuFramework/ModuleSetting/Editor/RedDot/RedDotSettingCreator.cs` — 删除
- `Unity/Assets/Scripts/Hotfix/RedDot/RedDotKeys.cs` — 删除
- `Unity/Assets/Scripts/Hotfix/RedDot/RedDotRegister.cs` — 删除
- Modify: `Unity/Assets/Scripts/FuFramework/ModuleSetting/Runtime/ModuleSetting.cs` — 删除 `RedDotSetting` 字段和属性

- [ ] **Step 1: 修改 ModuleSetting.cs — 删除 RedDotSetting 引用**

```csharp
// 删除字段
// [SerializeField] private RedDotSetting m_RedDotSetting;

// 删除属性
// public RedDotSetting RedDotSetting => m_RedDotSetting;
```

- [ ] **Step 2: 删除废弃文件**

```bash
rm "Unity/Assets/Scripts/FuFramework/ModuleSetting/SettingAssets/RedDotSetting.asset"
rm "Unity/Assets/Scripts/FuFramework/ModuleSetting/Runtime/RedDot/RedDotSetting.cs"
rm "Unity/Assets/Scripts/FuFramework/ModuleSetting/Runtime/RedDot/RedDotNodeData.cs"
rm "Unity/Assets/Scripts/FuFramework/ModuleSetting/Editor/RedDot/"*.cs
rm "Unity/Assets/Scripts/Hotfix/RedDot/RedDotKeys.cs"
rm "Unity/Assets/Scripts/Hotfix/RedDot/RedDotRegister.cs"
```

删除对应的 `.meta` 文件。

- [ ] **Step 3: 清理 FuFramework.RedDot.Runtime 程序集**

如果 `FuFramework.RedDot.Runtime` 目录下的文件已全部删除（只剩下空目录或 `.asmdef`），删除整个目录及其 `.asmdef`。

```bash
rm -rf "Unity/Assets/Scripts/FuFramework/RedDot/"
```

同时从 `FuFramework.ModuleSetting.Runtime` 和 `FuFramework.ModuleSetting.Editor` 的 `.asmdef` 中移除对 `FuFramework.RedDot.Runtime` 的引用（如果存在）。

- [ ] **Step 4: 在 Unity Editor 中确认无编译错误**

打开 Unity，等待脚本编译完成，检查 Console 无错误。

- [ ] **Step 5: Commit**

```bash
git rm -r Unity/Assets/Scripts/FuFramework/ModuleSetting/SettingAssets/RedDotSetting.asset
git rm -r Unity/Assets/Scripts/FuFramework/ModuleSetting/Runtime/RedDot/
git rm -r Unity/Assets/Scripts/FuFramework/ModuleSetting/Editor/RedDot/
git rm Unity/Assets/Scripts/Hotfix/RedDot/RedDotKeys.cs
git rm Unity/Assets/Scripts/Hotfix/RedDot/RedDotRegister.cs
git rm -r Unity/Assets/Scripts/FuFramework/RedDot/
git add Unity/Assets/Scripts/FuFramework/ModuleSetting/Runtime/ModuleSetting.cs
git commit -m "chore: 删除废弃的红点 SO 配置、编辑器、RedDotKeys、RedDotRegister"
```

---

### Task 8: Unity Editor 手动验证

- [ ] **Step 1: 启动游戏，检查红点模块初始化**

在 Unity Editor 中运行游戏，Console 应看到：
```
[RedDotModule] 初始化完成, 静态节点: 12
```

- [ ] **Step 2: 测试静态红点功能**

通过代码调用 `RedDotModule.Instance.SetCount(ERedDotKey.Bag, 3)`，确认 Bag 节点及父节点计数正常传播。

- [ ] **Step 3: 测试动态红点功能**

```csharp
var node = RedDotModule.Instance.AddDynamicChild(ERedDotKey.Bag_Item, "sword_001");
RedDotModule.Instance.SetCount("sword_001", 1);
RedDotModule.Instance.SetCount("sword_001", 0); // 应自动回收
```

- [ ] **Step 4: 测试 TryAutoClean**

配置某个节点 CleanStrategy 为 ViewAutoClean，在业务代码中触发：
```csharp
RedDotModule.Instance.TryAutoClean(ERedDotKey.Bag_Item);
```
确认该节点及所有子节点计数归零。

- [ ] **Step 5: 测试 FGUI 插件生成的代码**

在 FGUI 编辑器中为界面的 CompRedDot 实例配置自定义数据 `i18n=ERedDotKey.Bag_Item`，导出 C# 代码。检查生成的 `.Gen.cs` 中包含：
```csharp
compRedDotXxx.Register(uiView, ERedDotKey.Bag_Item);
```

在游戏中打开对应界面，验证红点显示正常。

- [ ] **Step 6: 回归测试现有红点功能**

逐一验证原有红点场景（登录、主界面、背包、商店等）。确认所有之前使用 `RedDotKeys` 的地方改用 `ERedDotKey` 后功能正常。
