# RedDot 模块重构实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 消除 RedDot 模块的静态/动态双轨制 API 重复，通过 `RedDotKey` 值类型统一标识，合并 partial 类，精简内部存储。

**Architecture:** 新增 `RedDotKey` struct 作为统一 Key（隐式转换兼容 `ERedDotKey` 和 `string`），所有公开 API 合并为单参数版本，内部 `Dictionary<RedDotKey, RedDotNode>` 单存储。文件从 5 个精简为 4 个，取消 `RedDotModule.Feature.cs` partial。

**Tech Stack:** C# 9+ (.NET Standard 2.1), Unity, Luban 配置表, FairyGUI (CompRedDot)

## 全局约束

- 仅修改 `Framework/RedDot/` 目录下的文件，`CompRedDot.cs` 仅做 API 适配
- 所有现有调用方通过隐式转换保持兼容，无 breaking change
- 红点计算模型、聚合逻辑、OnUpdate 批处理机制不变
- 对象池（IReference）机制不变
- 已读持久化仅对静态枚举键生效，动态键不持久化，`HashSet<int>` 保持

---

## 文件结构

| 文件 | 操作 | 职责 |
|------|------|------|
| `RedDotKey.cs` | 新建 | `RedDotKey` 值类型，隐式转换，相等比较 |
| `RedDotChangedEventArgs.cs` | 修改 | `ChangedStaticKeys` + `ChangedDynamicKeys` → `ChangedKeys` |
| `RedDotNode.cs` | 修改 | `StaticKey` + `DynamicKey` → `Key`，`Create` 改为接收 `RedDot` 行对象 |
| `RedDotModule.cs` | 重写 | 合并 `RedDotModule.Feature.cs`，所有 API 用 `RedDotKey`，单存储容器 |
| `RedDotModule.Feature.cs` | 删除 | 内容已合并到 `RedDotModule.cs` |
| `RedDotState.cs` | 不变 | — |
| `CompRedDot.cs` | 修改 | `m_StaticKey` + `m_DynamicKey` → `m_Key`，适配新 API |

---

### Task 1: 创建 RedDotKey 统一标识符

**Files:**
- Create: `Unity/Assets/Scripts/Hotfix/Framework/RedDot/RedDotKey.cs`

**Interfaces:**
- Produces: `RedDotKey` struct with:
  - `implicit operator RedDotKey(ERedDotKey key)` — 枚举 → Key
  - `implicit operator RedDotKey(string key)` — 字符串 → Key
  - `IEquatable<RedDotKey>` — 用于 Dictionary/HashSet
  - `==` / `!=` 运算符
  - `ToString()` → 内部字符串值

- [ ] **Step 1: 编写 RedDotKey.cs**

```csharp
using System;
using Hotfix.Game.Config;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.RedDot
{
    /// <summary>
    /// 红点节点统一标识符
    /// 支持隐式转换：ERedDotKey 枚举 → RedDotKey，string → RedDotKey
    /// 内部统一为 string 存储（枚举 ToString() 或动态字符串），可直接用作 Dictionary/HashSet 的 Key
    /// </summary>
    public readonly struct RedDotKey : IEquatable<RedDotKey>
    {
        /// <summary>
        /// 内部字符串值（枚举名或动态字符串）
        /// </summary>
        private readonly string m_Value;

        private RedDotKey(string value) => m_Value = value ?? "";

        /// <summary>
        /// 隐式转换：ERedDotKey 枚举 → RedDotKey（如 ERedDotKey.Mail → "Mail"）
        /// </summary>
        public static implicit operator RedDotKey(ERedDotKey key) => new(key.ToString());

        /// <summary>
        /// 隐式转换：string → RedDotKey
        /// </summary>
        public static implicit operator RedDotKey(string key) => new(key);

        public override string ToString() => m_Value;

        public bool Equals(RedDotKey other) => string.Equals(m_Value, other.m_Value);

        public override bool Equals(object obj) => obj is RedDotKey other && Equals(other);

        public override int GetHashCode() => m_Value.GetHashCode();

        public static bool operator ==(RedDotKey left, RedDotKey right) => left.Equals(right);

        public static bool operator !=(RedDotKey left, RedDotKey right) => !left.Equals(right);
    }
}
```

- [ ] **Step 2: 验证编译**

在 Unity Editor 中执行 `Assets > Recompile All`（或通过 unity-cli 触发），确认无编译错误。

- [ ] **Step 3: Commit**

```bash
git add "Unity/Assets/Scripts/Hotfix/Framework/RedDot/RedDotKey.cs" "Unity/Assets/Scripts/Hotfix/Framework/RedDot/RedDotKey.cs.meta"
git commit -m "feat: 新增 RedDotKey 统一标识符结构体"
```

---

### Task 2: 修改 RedDotChangedEventArgs 使用 RedDotKey

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/RedDot/RedDotChangedEventArgs.cs`

**Interfaces:**
- Consumes: `RedDotKey` struct
- Produces: `RedDotChangedEventArgs.ChangedKeys` (`List<RedDotKey>`) 替代 `ChangedStaticKeys` + `ChangedDynamicKeys`

- [ ] **Step 1: 修改 RedDotChangedEventArgs.cs**

将文件内容替换为：

```csharp
using System.Collections.Generic;
using Hotfix.Framework.Event;
using Hotfix.Framework.ReferencePools;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.RedDot
{
    /// <summary>
    /// 红点变更事件参数（每帧批量广播）
    /// UI 端订阅 EventModule 的此事件，按 ChangedKeys 过滤刷新
    /// </summary>
    public sealed class RedDotChangedEventArgs : GameEventArgs
    {
        /// <summary>
        /// 获取事件编号
        /// </summary>
        public override string Id => EventId;

        /// <summary>
        /// 事件 ID 常量（用于 Subscribe/Unsubscribe）
        /// </summary>
        public static readonly string EventId = typeof(RedDotChangedEventArgs).FullName;

        /// <summary>
        /// 本帧发生变化的红点节点 Key 列表
        /// </summary>
        public readonly List<RedDotKey> ChangedKeys = new();

        /// <summary>
        /// 清空事件参数数据，用于重用
        /// </summary>
        public override void Clear()
        {
            ChangedKeys.Clear();
        }

        /// <summary>
        /// 创建事件参数实例
        /// </summary>
        /// <returns>创建的事件参数实例</returns>
        public static RedDotChangedEventArgs Create()
        {
            var redDotChangedEventArgs = ReferencePool.Acquire<RedDotChangedEventArgs>();
            return redDotChangedEventArgs;
        }
    }
}
```

改动要点：
- 移除 `using Hotfix.Game.Config;`（不再需要 `ERedDotKey`）
- `ChangedStaticKeys` (`List<ERedDotKey>`) + `ChangedDynamicKeys` (`List<string>`) → `ChangedKeys` (`List<RedDotKey>`)

- [ ] **Step 2: 验证编译**

在 Unity Editor 中重编译。此时 `RedDotModule.cs` 和 `CompRedDot.cs` 引用了旧的 `ChangedStaticKeys`/`ChangedDynamicKeys`，会产生编译错误——这是预期的，后续任务解决。

- [ ] **Step 3: Commit**

```bash
git add "Unity/Assets/Scripts/Hotfix/Framework/RedDot/RedDotChangedEventArgs.cs"
git commit -m "refactor: RedDotChangedEventArgs 使用 RedDotKey 统一变更列表"
```

---

### Task 3: 修改 RedDotNode 使用 RedDotKey

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/RedDot/RedDotNode.cs`

**Interfaces:**
- Consumes: `RedDotKey` struct, `RedDot` 配置行类型 (`Hotfix.Game.Config.Tables.RedDot`)
- Produces:
  - `RedDotNode.Key` (`RedDotKey`) 替代 `StaticKey` + `DynamicKey`
  - `RedDotNode.Create(RedDot row)` — 从配置行构造静态节点
  - `RedDotNode.CreateDynamic(RedDotKey key, RedDotNode parent)` — 创建动态节点

- [ ] **Step 1: 修改 RedDotNode.cs**

将文件内容替换为：

```csharp
using System;
using System.Collections.Generic;
using AOT.Framework.Core.Log;
using Hotfix.Framework.ReferencePools;
using Hotfix.Game.Config;
using Hotfix.Game.Config.Tables;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.RedDot
{
    /// <summary>
    /// 红点节点
    /// 支持两种来源：
    ///     1. 静态节点：由 Luban 配置表定义
    ///     2. 动态节点：由运行时创建（如道具实例红点）
    /// 统一通过 RedDotKey 标识
    /// </summary>
    public class RedDotNode : IReference
    {
        /// <summary>
        /// 节点统一标识符
        /// </summary>
        public RedDotKey Key { get; private set; }

        /// <summary>
        /// 节点的原始计数（自身，不含子节点）
        /// </summary>
        public int RawCount { get; private set; }

        /// <summary>
        /// 节点的总计数（自身 + 所有子节点）
        /// </summary>
        public int TotalCount { get; private set; }

        /// <summary>
        /// 节点的父节点
        /// </summary>
        public RedDotNode Parent { get; private set; }

        /// <summary>
        /// 默认显示模式（来自配置表）
        /// </summary>
        public ERedDotDisplayMode DisplayMode { get; private set; }

        /// <summary>
        /// 清理策略（来自配置表）
        /// </summary>
        public ERedDotCleanStrategy CleanStrategy { get; private set; }

        /// <summary>
        /// 子节点聚合逻辑（来自配置表）
        /// </summary>
        public ERedDotLogicType LogicType { get; private set; }

        /// <summary>
        /// 是否激活（false 时 TotalCount 永远为 0）
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// 是否已读（持久化，仅抑制初始加载时的红点）
        /// </summary>
        public bool IsRead { get; internal set; }

        /// <summary>
        /// 脏标记（本帧待重算）
        /// </summary>
        public bool IsDirty { get; internal set; }

        #region calculator

        /// <summary>
        /// 叶子节点红点数计算函数（不为 null 时由 OnUpdate 自动调用）
        /// </summary>
        public Func<int> Calculator { get; internal set; }

        /// <summary>
        /// 触发重算的 EventModule 事件 ID 列表
        /// </summary>
        public string[] TriggerEvents { get; internal set; }

        /// <summary>
        /// TotalCount 变化回调（内部使用，由 RedDotModule 设置）
        /// 用于收集本帧变更节点以批量广播
        /// </summary>
        internal Action<RedDotNode> OnTotalCountChanged;

        /// <summary>
        /// 节点的子节点列表
        /// </summary>
        private readonly List<RedDotNode> m_Children = new();

        #endregion

        /// <summary>
        /// 从配置表行创建静态节点
        /// </summary>
        /// <param name="row">Luban 配置表行数据</param>
        /// <returns>创建的静态节点</returns>
        public static RedDotNode Create(RedDot row)
        {
            var node = ReferencePool.Acquire<RedDotNode>();
            node.Key            = row.Id;
            node.DisplayMode   = row.DisplayMode;
            node.CleanStrategy = row.CleanStrategy;
            node.LogicType     = row.LogicType;
            node.IsActive      = row.IsActive;
            return node;
        }

        /// <summary>
        /// 运行时创建动态节点（默认 DotOnly + Manual + Sum）
        /// </summary>
        /// <param name="key">动态节点 Key</param>
        /// <param name="parent">父节点</param>
        /// <returns>创建的动态节点</returns>
        public static RedDotNode CreateDynamic(RedDotKey key, RedDotNode parent)
        {
            var node = ReferencePool.Acquire<RedDotNode>();
            node.Key            = key;
            node.Parent         = parent;
            node.DisplayMode   = ERedDotDisplayMode.DotOnly;
            node.CleanStrategy = ERedDotCleanStrategy.Manual;
            node.LogicType     = ERedDotLogicType.Sum;
            node.IsActive      = true;
            return node;
        }

        /// <summary>
        /// 两阶段构建 — 初始化后设置父节点
        /// </summary>
        /// <param name="parent">父节点</param>
        public void SetParent(RedDotNode parent) => Parent = parent;

        /// <summary>
        /// 添加子节点
        /// </summary>
        /// <param name="child">子节点</param>
        public void AddChild(RedDotNode child)
        {
            if (m_Children.Contains(child))
            {
                FuLogger.LogWarning($"[RedDotNode] 无法添加重复的子节点, 在节点{Key}下已经存在子节点{child.Key}");
                return;
            }

            m_Children.Add(child);
        }

        /// <summary>
        /// 移除子节点（动态节点归零回收时使用）
        /// </summary>
        /// <param name="child">子节点</param>
        public void RemoveChild(RedDotNode child) => m_Children.Remove(child);

        /// <summary>
        /// 强制重算 TotalCount 并向上传播（子节点增删后调用）
        /// </summary>
        internal void ForceRecalculate() => UpdateTotalCount();

        /// <summary>
        /// 设置节点计数并向上传播（仅 RedDotModule 内部调用）
        /// </summary>
        /// <param name="count">新的 RawCount 值</param>
        internal void SetCount(int count)
        {
            if (RawCount == count) return;

            RawCount = count;
            UpdateTotalCount();
        }

        /// <summary>
        /// 更新节点总计数，自动向上传播
        /// 聚合逻辑受 LogicType 和 IsActive 控制
        /// </summary>
        private void UpdateTotalCount()
        {
            if (!IsActive)
            {
                if (TotalCount == 0) return;
                TotalCount = 0;
                OnTotalCountChanged?.Invoke(this);
                return;
            }

            var childrenTotal = LogicType == ERedDotLogicType.Any ? ComputeChildrenAny() : ComputeChildrenSum();

            var total = RawCount + childrenTotal;
            if (TotalCount == total) return;

            TotalCount = total;
            OnTotalCountChanged?.Invoke(this);
            Parent?.UpdateTotalCount();
        }

        /// <summary>
        /// Sum 模式：累加所有子节点的 TotalCount
        /// </summary>
        /// <returns>所有子节点 TotalCount 之和</returns>
        private int ComputeChildrenSum()
        {
            var total = 0;
            foreach (var child in m_Children)
            {
                total += child.TotalCount;
            }

            return total;
        }

        /// <summary>
        /// Any 模式：任一子节点 TotalCount > 0 则为 1
        /// </summary>
        /// <returns>存在 TotalCount > 0 的子节点返回 1，否则返回 0</returns>
        private int ComputeChildrenAny()
        {
            foreach (var child in m_Children)
            {
                if (child.TotalCount > 0)
                    return 1;
            }

            return 0;
        }

        /// <summary>
        /// 获取最终计数（考虑 IsActive 和 IsRead）
        /// </summary>
        /// <returns>IsActive 为 false 时返回 0，否则返回 TotalCount</returns>
        public int GetFinalCount() => !IsActive ? 0 : TotalCount;

        /// <summary>
        /// 获取所有子节点（只读）
        /// </summary>
        /// <returns>子节点的只读列表</returns>
        public IReadOnlyList<RedDotNode> GetChildren() => m_Children.AsReadOnly();

        /// <summary>
        /// 回收到对象池时清理
        /// </summary>
        public void Clear()
        {
            Key                 = default;
            Parent              = null;
            RawCount            = 0;
            TotalCount          = 0;
            DisplayMode         = ERedDotDisplayMode.DotOnly;
            CleanStrategy       = ERedDotCleanStrategy.Manual;
            LogicType           = ERedDotLogicType.Sum;
            IsActive            = true;
            IsRead              = false;
            IsDirty             = false;
            Calculator          = null;
            TriggerEvents       = null;
            OnTotalCountChanged = null;
            m_Children.Clear();
        }
    }
}
```

改动要点：
- `StaticKey` (`ERedDotKey?`) + `DynamicKey` (`string`) → `Key` (`RedDotKey`)
- 新增 `using Hotfix.Game.Config.Tables;`
- `Create()` 签名从 6 个独立参数改为接收 `RedDot` 行对象
- `CreateDynamic()` 参数从 `string key` 改为 `RedDotKey key`
- `AddChild` 日志中 `StaticKey` → `Key`
- `Clear()` 中 `StaticKey = null; DynamicKey = null;` → `Key = default;`

- [ ] **Step 2: 验证编译**

在 Unity Editor 中重编译。此时 `RedDotModule.cs` 还在引用旧的 `StaticKey`/`DynamicKey` 属性和旧版 `Create()` 签名，会产生编译错误——这是预期的，Task 4 解决。

- [ ] **Step 3: Commit**

```bash
git add "Unity/Assets/Scripts/Hotfix/Framework/RedDot/RedDotNode.cs"
git commit -m "refactor: RedDotNode 使用 RedDotKey 统一 Key，Create 改为接收配置行对象"
```

---

### Task 4: 合并并重写 RedDotModule

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/RedDot/RedDotModule.cs`
- Delete: `Unity/Assets/Scripts/Hotfix/Framework/RedDot/RedDotModule.Feature.cs`
- Delete: `Unity/Assets/Scripts/Hotfix/Framework/RedDot/RedDotModule.Feature.cs.meta`

**Interfaces:**
- Consumes: `RedDotKey`, `RedDotNode.Key`, `RedDotChangedEventArgs.ChangedKeys`
- Produces: 所有公开 API 统一为 `RedDotKey` 参数版本

- [ ] **Step 1: 重写 RedDotModule.cs**

将文件内容替换为：

```csharp
using System;
using System.Collections.Generic;
using Hotfix.Framework.Core;
using Hotfix.Framework.Event;
using Hotfix.Framework.ReferencePools;
using Hotfix.Framework.Config;
using Hotfix.Framework.Storage;
using Hotfix.Game.Config;
using Hotfix.Game.Config.Tables;
using AOT.Framework.Core.Log;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.RedDot
{
    /// <summary>
    /// 红点管理模块
    /// 功能：
    ///     1. 树形结构管理 — 支持父子节点层级关系，LogicType（Any/Sum）控制聚合
    ///     2. Calculator — 业务注册 Func&lt;int&gt; 计算函数，OnUpdate 批处理重算
    ///     3. EventModule 批量广播 — 每帧统一广播 RedDotChangedEventArgs，UI 端按 Key 过滤
    ///     4. 配置化驱动 — 通过 Luban 配置表 TbRedDot 初始化红点树结构
    ///     5. 统一 Key — RedDotKey 同时支持静态枚举和动态字符串，隐式转换无感
    ///     6. 动态红点 — SyncDynamicNode 批量管理动态子节点
    ///     7. 已读持久化 — MarkRead 通过 StorageModule 持久保存已读状态（仅静态键）
    ///
    /// 使用流程：
    ///     1. 在 Luban 配置表中定义红点树结构
    ///     2. 在 UI 界面中使用 CompRedDot 组件绑定红点，填写红点枚举 ID
    ///     3. 业务模块调用 Register 注册计算函数
    ///     4. 业务触发事件 → OnUpdate 重算 → 广播红点变更事件
    ///     5. UI 组件 CompRedDot 监听广播事件，按 Key 过滤刷新 UI
    /// </summary>
    public class RedDotModule : ModuleBase
    {
        /// <summary>
        /// 模块静态单例
        /// </summary>
        public static RedDotModule Instance { get; private set; }

        #region 节点存储

        /// <summary>
        /// 统一节点字典（Key: RedDotKey，Value: 红点节点）
        /// </summary>
        private static readonly Dictionary<RedDotKey, RedDotNode> NodeDict = new();

        #endregion

        #region 脏标记批处理

        /// <summary>
        /// 本帧待重算的脏节点集合
        /// </summary>
        private readonly HashSet<RedDotNode> m_DirtyNodeSet = new();

        /// <summary>
        /// 本帧发生变更的节点 Key 集合（用于去重后广播）
        /// </summary>
        private readonly HashSet<RedDotKey> m_ChangedKeySet = new();

        #endregion

        #region 事件订阅反向映射

        /// <summary>
        /// EventModule 事件 ID → 订阅该事件的节点集合
        /// </summary>
        private readonly Dictionary<string, HashSet<RedDotNode>> m_EventToNodes = new();

        #endregion

        #region 已读持久化

        /// <summary>
        /// 已读静态节点 Key 集合（存为 ERedDotKey 的 int 值）
        /// </summary>
        private readonly HashSet<int> m_ReadSet = new();

        /// <summary>
        /// 已读状态在 StorageModule 中的 Key
        /// </summary>
        private const string ReadStorageKey = "ReadSet";

        /// <summary>
        /// 已读状态在 StorageModule 中的文件名
        /// </summary>
        private const string ReadStorageFile = "RedDotData";

        #endregion

        #region 动态红点

        /// <summary>
        /// 父节点 Key → 已同步的 id 集合（用于 SyncDynamicNode 增量更新）
        /// </summary>
        private readonly Dictionary<RedDotKey, HashSet<long>> m_DynamicIdDict = new();

        #endregion

        #region 生命周期

        /// <summary>
        /// 模块初始化：从 Luban 配置表构建红点树
        /// </summary>
        protected internal override void OnInit()
        {
            Instance = this;

            var tbRedDot = ConfigModule.Instance?.GetConfig<TbRedDot>();
            if (tbRedDot == null || tbRedDot.Count == 0)
            {
                FuLogger.LogWarning("[RedDotModule] 红点配置表不存在或为空，跳过树构建.");
                return;
            }

            NodeDict.Clear();

            var allRows = tbRedDot.All;

            // 阶段一：创建所有节点
            foreach (var row in allRows)
            {
                var node = RedDotNode.Create(row);

                if (!NodeDict.TryAdd(row.Id, node))
                {
                    FuLogger.LogError($"[RedDotModule] 重复的节点key: {row.Id}");
                    ReferencePool.Release(node);
                }
            }

            // 阶段二：建立父子关系
            foreach (var row in allRows)
            {
                if (row.ParentId == null) continue;
                RedDotKey parentKey = row.ParentId.Value;

                if (!NodeDict.TryGetValue(row.Id,    out var child) ||
                    !NodeDict.TryGetValue(parentKey, out var parent))
                    continue;

                child.SetParent(parent);
                parent.AddChild(child);
            }

            // 注入变更追踪回调
            foreach (var node in NodeDict.Values)
            {
                node.OnTotalCountChanged = OnNodeTotalCountChanged;
            }

            // 加载已读状态
            LoadReadState();

            FuLogger.LogInfo($"[RedDotModule] 初始化红点模块成功. 节点总数量: {NodeDict.Count}");
        }

        /// <summary>
        /// 模块释放：清理事件订阅、回收节点
        /// </summary>
        protected internal override void OnDispose()
        {
            // 清理事件订阅
            foreach (var eventId in m_EventToNodes.Keys)
            {
                GlobalModule.EventModule.Unsubscribe(eventId, OnTriggerEvent);
            }

            m_EventToNodes.Clear();

            // 清理所有节点
            foreach (var node in NodeDict.Values)
            {
                ReferencePool.Release(node);
            }

            NodeDict.Clear();
            m_DirtyNodeSet.Clear();
            m_ChangedKeySet.Clear();
            m_DynamicIdDict.Clear();
            m_ReadSet.Clear();
            Instance = null;
        }

        /// <summary>
        /// 每帧批处理：重算脏节点 → 聚合 → 广播变更
        /// </summary>
        /// <param name="deltaTime">游戏帧间隔时间</param>
        /// <param name="unscaledDeltaTime">不受时间缩放影响的帧间隔时间</param>
        protected internal override void OnUpdate(float deltaTime, float unscaledDeltaTime)
        {
            // 处理脏节点
            if (m_DirtyNodeSet.Count > 0)
            {
                foreach (var node in m_DirtyNodeSet)
                {
                    node.IsDirty = false;
                    if (node.Calculator == null) continue;

                    try
                    {
                        var newCount = node.Calculator.Invoke();
                        if (node.IsRead)
                            newCount = 0;
                        node.SetCount(newCount);
                    }
                    catch (Exception ex)
                    {
                        FuLogger.LogError($"[RedDotModule] 红点计算函数执行异常: {ex.Message}");
                    }
                }

                m_DirtyNodeSet.Clear();
            }

            // 广播所有本帧累积的变更（脏节点 + MarkRead 等）
            if (m_ChangedKeySet.Count > 0)
            {
                BroadcastChangedKeys();
            }
        }

        #endregion

        #region 注册

        /// <summary>
        /// 注册红点 Calculator（统一 API）
        /// </summary>
        /// <param name="key">红点节点 Key（支持 ERedDotKey 或 string 隐式转换）</param>
        /// <param name="calculator">返回红点数量的计算函数</param>
        /// <param name="triggerEvents">触发重算的 EventModule 事件 ID 列表（可变参数）</param>
        public void Register(RedDotKey key, Func<int> calculator, params string[] triggerEvents)
        {
            if (!NodeDict.TryGetValue(key, out var node))
            {
                FuLogger.LogError($"[RedDotModule] Register 未找到节点: {key}");
                return;
            }

            RegisterInternal(node, calculator, triggerEvents);
        }

        /// <summary>
        /// 一步创建动态节点并注册 Calculator
        /// </summary>
        /// <param name="parentKey">父节点 Key（支持 ERedDotKey 或 string 隐式转换）</param>
        /// <param name="dynamicKey">动态节点 Key（支持 string 隐式转换）</param>
        /// <param name="calculator">返回红点数量的计算函数</param>
        /// <param name="triggerEvents">触发重算的 EventModule 事件 ID 列表（可变参数）</param>
        public void Register(RedDotKey parentKey, RedDotKey dynamicKey, Func<int> calculator, params string[] triggerEvents)
        {
            var node = AddDynamicChild(parentKey, dynamicKey);
            if (node == null) return;

            RegisterInternal(node, calculator, triggerEvents);
            node.SetCount(calculator());
        }

        /// <summary>
        /// 注册红点（内部实现）
        /// </summary>
        private void RegisterInternal(RedDotNode node, Func<int> calculator, string[] triggerEvents)
        {
            // 先注销旧的红点
            UnregisterInternal(node);

            node.Calculator    = calculator;
            node.TriggerEvents = triggerEvents;

            if (triggerEvents == null || triggerEvents.Length == 0) return;

            // 订阅 EventModule 事件
            foreach (var eventId in triggerEvents)
            {
                if (string.IsNullOrEmpty(eventId)) continue;

                if (!m_EventToNodes.TryGetValue(eventId, out var nodeSet))
                {
                    nodeSet                 = new HashSet<RedDotNode>();
                    m_EventToNodes[eventId] = nodeSet;
                }

                if (nodeSet.Count == 0)
                {
                    // 首次订阅此事件 ID
                    GlobalModule.EventModule.Subscribe(eventId, OnTriggerEvent);
                }

                nodeSet.Add(node);
            }
        }

        /// <summary>
        /// 注销红点 Calculator（统一 API）
        /// </summary>
        /// <param name="key">红点节点 Key（支持 ERedDotKey 或 string 隐式转换）</param>
        public void Unregister(RedDotKey key)
        {
            if (!NodeDict.TryGetValue(key, out var node)) return;
            UnregisterInternal(node);
        }

        /// <summary>
        /// 注销红点（内部实现）
        /// </summary>
        private void UnregisterInternal(RedDotNode node)
        {
            if (node.TriggerEvents != null)
            {
                foreach (var eventId in node.TriggerEvents)
                {
                    if (string.IsNullOrEmpty(eventId)) continue;

                    if (!m_EventToNodes.TryGetValue(eventId, out var nodeSet)) continue;
                    nodeSet.Remove(node);

                    if (nodeSet.Count != 0) continue;
                    GlobalModule.EventModule.Unsubscribe(eventId, OnTriggerEvent);
                    m_EventToNodes.Remove(eventId);
                }
            }

            node.Calculator    = null;
            node.TriggerEvents = null;
        }

        /// <summary>
        /// EventModule 事件触发回调 — 标记对应节点为脏
        /// </summary>
        private void OnTriggerEvent(object sender, GameEventArgs e)
        {
            if (!m_EventToNodes.TryGetValue(e.Id, out var nodeSet)) return;

            foreach (var node in nodeSet)
            {
                if (node.IsDirty) continue;
                node.IsDirty = true;
                m_DirtyNodeSet.Add(node);
            }
        }

        #endregion

        #region 状态查询

        /// <summary>
        /// 查询节点状态（统一 API）
        /// </summary>
        /// <param name="key">红点节点 Key（支持 ERedDotKey 或 string 隐式转换）</param>
        /// <returns>节点的 RedDotState，未找到时返回 Empty</returns>
        public RedDotState GetState(RedDotKey key)
        {
            if (!NodeDict.TryGetValue(key, out var node)) return RedDotState.Empty;

            return new RedDotState
            {
                Count       = node.GetFinalCount(),
                IsActive    = node.IsActive,
                DisplayMode = node.DisplayMode
            };
        }

        /// <summary>
        /// 是否存在节点（统一 API）
        /// </summary>
        /// <param name="key">红点节点 Key（支持 ERedDotKey 或 string 隐式转换）</param>
        /// <returns>存在返回 true，否则返回 false</returns>
        public bool HasNode(RedDotKey key) => NodeDict.ContainsKey(key);

        #endregion

        #region 动态节点

        /// <summary>
        /// 为指定父节点添加动态子节点
        /// </summary>
        /// <param name="parentKey">父节点 Key</param>
        /// <param name="childKey">动态子节点 Key</param>
        /// <returns>创建的动态节点，父节点不存在时返回 null</returns>
        private RedDotNode AddDynamicChild(RedDotKey parentKey, RedDotKey childKey)
        {
            if (!NodeDict.TryGetValue(parentKey, out var parentNode))
            {
                FuLogger.LogError($"[RedDotModule] 父节点不存在: {parentKey}");
                return null;
            }

            if (NodeDict.TryGetValue(childKey, out var existingNode))
                return existingNode;

            var node = RedDotNode.CreateDynamic(childKey, parentNode);
            node.OnTotalCountChanged = OnNodeTotalCountChanged;
            parentNode.AddChild(node);
            NodeDict.Add(childKey, node);
            FuLogger.LogInfo($"[RedDotModule] 创建动态节点: {childKey}，父节点: {parentKey}");
            return node;
        }

        /// <summary>
        /// 移除动态节点
        /// </summary>
        /// <param name="childKey">动态节点 Key</param>
        private void RemoveDynamicChild(RedDotKey childKey)
        {
            if (!NodeDict.TryGetValue(childKey, out var node)) return;

            var parent = node.Parent;
            UnregisterInternal(node);
            parent?.RemoveChild(node);
            NodeDict.Remove(childKey);
            ReferencePool.Release(node);

            // 移除子节点后触发父节点重算
            parent?.ForceRecalculate();
        }

        /// <summary>
        /// 同步动态红点集合：比对增删，新增时自动创建节点 + 注册计算红点的函数
        /// </summary>
        /// <param name="parentKey">父节点 Key</param>
        /// <param name="ids">当前活跃的 id 列表</param>
        /// <param name="calculateFun">根据 id 返回红点数量的计算函数</param>
        public void SyncDynamicNode(RedDotKey parentKey, IReadOnlyList<long> ids, Func<long, int> calculateFun)
        {
            if (!NodeDict.ContainsKey(parentKey))
            {
                FuLogger.LogError($"[RedDotModule] SyncDynamicNode 未找到父节点: {parentKey}");
                return;
            }

            if (!m_DynamicIdDict.TryGetValue(parentKey, out var existing))
            {
                existing = new HashSet<long>();
                m_DynamicIdDict[parentKey] = existing;
            }

            // 收集新增的 id
            var newIds = new HashSet<long>();
            foreach (var id in ids)
            {
                newIds.Add(id);
            }

            // 移除不在新列表中的实例
            var removedIds = new List<long>();
            foreach (var id in existing)
            {
                if (newIds.Contains(id)) continue;
                removedIds.Add(id);
            }

            foreach (var id in removedIds)
            {
                RemoveDynamicChild(FormatDynamicKey(parentKey, id));
                existing.Remove(id);
            }

            // 添加新增的实例 — 框架内部包装闭包
            foreach (var id in ids)
            {
                if (existing.Contains(id)) continue;

                var dynamicKey = FormatDynamicKey(parentKey, id);
                var idCapture  = id; // 避免闭包捕获循环变量

                Register(parentKey, dynamicKey, () => calculateFun(idCapture));
                existing.Add(id);
            }
        }

        /// <summary>
        /// 生成动态节点 Key（格式：__dynamic__{parentKey}_{id}）
        /// </summary>
        private static string FormatDynamicKey(RedDotKey parentKey, long id)
        {
            return $"__dynamic__{parentKey}_{id}";
        }

        #endregion

        #region 已读持久化

        /// <summary>
        /// 标记红点已读（计数归零 + 持久化，仅静态键持久化）
        /// </summary>
        /// <param name="key">红点节点 Key</param>
        public void MarkRead(RedDotKey key)
        {
            if (!NodeDict.TryGetValue(key, out var node)) return;

            node.IsRead = true;
            node.SetCount(0);

            // 仅静态枚举键进行持久化
            if (Enum.TryParse<ERedDotKey>(key.ToString(), out var staticKey))
            {
                m_ReadSet.Add((int)staticKey);
                SaveReadState();
                BroadcastChangedKeys();
            }
        }

        /// <summary>
        /// 检查是否已读
        /// </summary>
        /// <param name="key">红点节点 Key</param>
        /// <returns>已读返回 true，否则返回 false</returns>
        public bool IsRead(RedDotKey key)
        {
            // 静态键检查持久化集合
            if (Enum.TryParse<ERedDotKey>(key.ToString(), out var staticKey))
                return m_ReadSet.Contains((int)staticKey);

            // 动态键检查节点自身标记
            return NodeDict.TryGetValue(key, out var node) && node.IsRead;
        }

        /// <summary>
        /// 从 StorageModule 加载已读状态
        /// </summary>
        private void LoadReadState()
        {
            if (StorageModule.Instance == null) return;

            var list = StorageModule.Instance.GetObject<List<int>>(ReadStorageKey, ReadStorageFile);
            if (list == null) return;

            foreach (var id in list)
            {
                m_ReadSet.Add(id);
                var staticKey = (ERedDotKey)id;
                RedDotKey key = staticKey; // 隐式转换
                if (NodeDict.TryGetValue(key, out var node))
                {
                    node.IsRead = true;
                    node.SetCount(0);
                }
            }
        }

        /// <summary>
        /// 保存已读状态到 StorageModule
        /// </summary>
        private void SaveReadState()
        {
            var list = new List<int>(m_ReadSet);
            StorageModule.Instance.SetObject(ReadStorageKey, list, ReadStorageFile);
        }

        #endregion

        #region 清理策略

        /// <summary>
        /// 尝试自动清除红点（仅对 ViewAutoClean 策略的节点生效）
        /// </summary>
        /// <param name="key">红点节点 Key</param>
        public void TryAutoClean(RedDotKey key)
        {
            if (!NodeDict.TryGetValue(key, out var node)) return;
            if (node.CleanStrategy != ERedDotCleanStrategy.ViewAutoClean) return;
            CleanNodeRecursive(node);
        }

        /// <summary>
        /// 递归清除节点及所有子节点的计数
        /// </summary>
        /// <param name="node">起始节点</param>
        private static void CleanNodeRecursive(RedDotNode node)
        {
            node.SetCount(0);
            foreach (var child in node.GetChildren())
            {
                CleanNodeRecursive(child);
            }
        }

        #endregion

        #region 内部方法

        /// <summary>
        /// TotalCount 变化回调（注入到每个 RedDotNode，在 UpdateTotalCount 中触发）
        /// 收集本帧变更的节点 Key，供 OnUpdate 批量广播
        /// </summary>
        /// <param name="node">发生变化的节点</param>
        private void OnNodeTotalCountChanged(RedDotNode node)
        {
            m_ChangedKeySet.Add(node.Key);
        }

        /// <summary>
        /// 通过 EventModule 批量广播本帧变更
        /// </summary>
        private void BroadcastChangedKeys()
        {
            var args = RedDotChangedEventArgs.Create();
            foreach (var key in m_ChangedKeySet)
            {
                args.ChangedKeys.Add(key);
            }

            GlobalModule.EventModule.Broadcast(this, args);

            m_ChangedKeySet.Clear();
        }

        #endregion
    }
}
```

关键改动：
- `StaticNodeDict` + `DynamicNodeDict` → `NodeDict`（`Dictionary<RedDotKey, RedDotNode>`）
- `m_ChangedStaticSet` + `m_ChangedDynamicSet` → `m_ChangedKeySet`（`HashSet<RedDotKey>`）
- `m_DynamicIdDict` 的 Key 从 `ERedDotKey` 改为 `RedDotKey`（隐式转换兼容）
- `Register(ERedDotKey, ...)` + `Register(string, ...)` → `Register(RedDotKey, ...)` 单一 API
- `Unregister(ERedDotKey)` + `Unregister(string)` → `Unregister(RedDotKey)` 单一 API
- `GetState(ERedDotKey)` + `GetState(string)` → `GetState(RedDotKey)` 单一 API
- `HasNode(ERedDotKey)` + `HasNode(string)` → `HasNode(RedDotKey)` 单一 API
- `MarkRead(ERedDotKey)` + `MarkRead(string)` → `MarkRead(RedDotKey)`，内部通过 `Enum.TryParse` 判断是否持久化
- `IsRead(ERedDotKey)` + `IsRead(string)` → `IsRead(RedDotKey)`
- `TryAutoClean(ERedDotKey)` → `TryAutoClean(RedDotKey)`
- `OnNodeTotalCountChanged` 从 `node.StaticKey.HasValue` 双分支 → 直接用 `node.Key`
- `BroadcastChangedIds` → `BroadcastChangedKeys`，从 `m_ChangedKeySet` 单集合填充
- `OnInit` 中 `RedDotNode.Create(row.Id, null, row.DisplayMode, ...)` → `RedDotNode.Create(row)`

- [ ] **Step 2: 删除 RedDotModule.Feature.cs 及其 .meta**

```bash
rm "Unity/Assets/Scripts/Hotfix/Framework/RedDot/RedDotModule.Feature.cs"
rm "Unity/Assets/Scripts/Hotfix/Framework/RedDot/RedDotModule.Feature.cs.meta"
```

- [ ] **Step 3: 清理 .csproj / .sln 中的引用（如有）**

检查项目文件中是否显式引用了 `RedDotModule.Feature.cs`，如有则移除（通常 Unity 自动管理 .csproj，删除 .meta 后自动处理）。

- [ ] **Step 4: 验证编译**

Unity Editor 重编译。检查是否有未适配的调用方引用旧的 `Register(ERedDotKey, ...)` 等签名——旧的调用方式通过隐式转换兼容，理论上无需改动。

搜索整个 `Assets/Scripts` 目录确认没有残留引用：
```bash
grep -r "ChangedStaticKeys\|ChangedDynamicKeys\|\.StaticKey\b\|\.DynamicKey\b" --include="*.cs" "Unity/Assets/Scripts/" | grep -v "RedDot/" | grep -v ".Gen.cs"
```

排除 RedDot 目录自身和自动生成代码后的结果应该为空。

- [ ] **Step 5: Commit**

```bash
git add "Unity/Assets/Scripts/Hotfix/Framework/RedDot/RedDotModule.cs"
git rm "Unity/Assets/Scripts/Hotfix/Framework/RedDot/RedDotModule.Feature.cs" "Unity/Assets/Scripts/Hotfix/Framework/RedDot/RedDotModule.Feature.cs.meta"
git commit -m "refactor: RedDotModule 合并 partial，统一 API 使用 RedDotKey"
```

---

### Task 5: 适配 CompRedDot

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Game/UI/Common/Comp/CompRedDot.cs`

**Interfaces:**
- Consumes: `RedDotKey`, `RedDotChangedEventArgs.ChangedKeys`（替代 `ChangedStaticKeys` + `ChangedDynamicKeys`）
- Produces: `CompRedDot` 使用统一的 `m_Key` 字段

- [ ] **Step 1: 修改 CompRedDot.cs**

将文件内容替换为：

```csharp
using System;
using FairyGUI;
using Hotfix.Framework.UI;
using Hotfix.Framework.RedDot;
using Hotfix.Framework.Event;
using Hotfix.Framework.Core;
using Hotfix.Game.Config;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.Game.UI
{
    public partial class CompRedDot
    {
        /// <summary>
        /// FGUI customData 中红点标识前缀
        /// </summary>
        private const string FlagRedDot = "red_dot:";

        /// <summary>
        /// 红点节点 Key（从 customData 解析）
        /// </summary>
        private RedDotKey m_Key;

        /// <summary>
        /// 是否已绑定红点 Key
        /// </summary>
        private bool m_HasKey;

        /// <summary>
        /// 初始化：自动解析 customData 中的 red_dot:&lt;key&gt; 并订阅红点变更事件
        /// </summary>
        private void OnInit()
        {
            var customData = data as string;
            if (!TryParseRedDotKey(customData, out var keyValue)) return;

            if (Enum.TryParse<ERedDotKey>(keyValue, true, out var staticKey))
            {
                m_Key = staticKey;
            }
            else
            {
                m_Key = keyValue;
            }

            m_HasKey = true;
            GlobalModule.EventModule.Subscribe(RedDotChangedEventArgs.EventId, OnRedDotChanged);
            RefreshCurrentState();
        }

        /// <summary>
        /// 销毁：取消订阅红点变更事件
        /// </summary>
        private void OnDispose()
        {
            if (m_HasKey)
                GlobalModule.EventModule.Unsubscribe(RedDotChangedEventArgs.EventId, OnRedDotChanged);
        }

        #region 内部实现

        /// <summary>
        /// EventModule 回调：本帧红点变更时检查是否需要刷新
        /// </summary>
        private void OnRedDotChanged(object sender, GameEventArgs e)
        {
            if (e is not RedDotChangedEventArgs args) return;
            if (!m_HasKey) return;

            foreach (var key in args.ChangedKeys)
            {
                if (key != m_Key) continue;
                RefreshCurrentState();
                return;
            }
        }

        /// <summary>
        /// 根据显示模式刷新 UI 控件
        /// </summary>
        private void RefreshUI(int redCount, ERedDotDisplayMode mode)
        {
            switch (mode)
            {
                case ERedDotDisplayMode.DotOnly:
                    txtCount.visible = false;
                    imgRedDot.visible = redCount > 0;
                    break;
                case ERedDotDisplayMode.DotNumber:
                    txtCount.visible = redCount >= 1;
                    imgRedDot.visible = redCount > 0;
                    txtCount.text = FormatRedDotCount(redCount);
                    break;
                case ERedDotDisplayMode.Auto:
                    txtCount.visible = redCount > 1;
                    imgRedDot.visible = redCount > 0;
                    txtCount.text = FormatRedDotCount(redCount);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }

        /// <summary>
        /// 格式化红点数量显示
        /// </summary>
        private static string FormatRedDotCount(int count)
        {
            return count switch
            {
                <= 0 => "0",
                > 99 => "99+",
                _ => count.ToString()
            };
        }

        /// <summary>
        /// 查询当前状态并刷新 UI
        /// </summary>
        private void RefreshCurrentState()
        {
            var state = RedDotModule.Instance.GetState(m_Key);
            RefreshUI(state.Count, state.DisplayMode);
        }

        /// <summary>
        /// 解析UI组件customData中的 red_dot:{key} 段（支持竖线分隔组合，如red_dot:Bag）
        /// </summary>
        private static bool TryParseRedDotKey(string customData, out string result)
        {
            result = null;

            if (string.IsNullOrEmpty(customData))
                return false;

            var segStart = customData.IndexOf(FlagRedDot, StringComparison.Ordinal);
            if (segStart < 0)
                return false;

            var dataStart = segStart + FlagRedDot.Length;
            var pipePos = customData.IndexOf('|', dataStart);
            result = pipePos >= 0
                ? customData.Substring(dataStart, pipePos - dataStart).Trim()
                : customData.Substring(dataStart).Trim();

            return !string.IsNullOrEmpty(result);
        }

        #endregion
    }
}
```

改动要点：
- `m_StaticKey` (`ERedDotKey?`) + `m_DynamicKey` (`string`) → `m_Key` (`RedDotKey`) + `m_HasKey` (`bool`)
- `OnInit` 中解析后统一赋值 `m_Key`（枚举成功用隐式转换 `m_Key = staticKey`，否则 `m_Key = keyValue`）
- `OnDispose` 中用 `m_HasKey` 判断是否已订阅
- `OnRedDotChanged` 中只遍历 `args.ChangedKeys` 一个列表
- `RefreshCurrentState` 中直接 `RedDotModule.Instance.GetState(m_Key)`，不再区分静态/动态

- [ ] **Step 2: 验证编译**

Unity Editor 重编译。确认 `CompRedDot` 编译通过且无其他调用方报错。

- [ ] **Step 3: Commit**

```bash
git add "Unity/Assets/Scripts/Hotfix/Game/UI/Common/Comp/CompRedDot.cs"
git commit -m "refactor: CompRedDot 适配 RedDotKey 统一 API"
```

---

### Task 6: 更新 README 文档

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/RedDot/README.md`

- [ ] **Step 1: 更新 README 中的 API 签名和说明**

将文档中的以下内容更新：

**4.1 RedDotModule 核心方法** 部分，将双轨 API 合并为统一签名：

```csharp
// === Calculator 注册 ===
void Register(RedDotKey key, Func<int> calculator, params string[] triggerEvents)
void Register(RedDotKey parentKey, RedDotKey dynamicKey, Func<int> calculator, params string[] triggerEvents)
void Unregister(RedDotKey key)

// === 状态查询 ===
RedDotState GetState(RedDotKey key)
bool HasNode(RedDotKey key)

// === 动态红点批量同步 ===
void SyncDynamicNode(RedDotKey parentKey, IReadOnlyList<long> ids, Func<long, int> calculator)

// === 已读持久化 ===
void MarkRead(RedDotKey key)
bool IsRead(RedDotKey key)

// === 清理策略 ===
void TryAutoClean(RedDotKey key)
```

**4.2 RedDotNode 核心属性** 表格，将 `StaticKey` / `DynamicKey` 替换为：

| 属性 | 类型 | 说明 |
|------|------|------|
| `Key` | `RedDotKey` | 统一节点标识符（支持 ERedDotKey 或 string 隐式转换） |

**4.4 RedDotChangedEventArgs** 将双 List 替换为：

```csharp
public sealed class RedDotChangedEventArgs : GameEventArgs
{
    public static readonly string EventId = typeof(RedDotChangedEventArgs).FullName;
    public readonly List<RedDotKey> ChangedKeys;  // 本帧变化的节点 Key 列表
    public static RedDotChangedEventArgs Create();
}
```

添加 **2. 核心特性** 条目：
```
- **统一 Key**：`RedDotKey` 结构体统一标识符，支持 ERedDotKey 和 string 隐式转换，消除双轨 API
```

**6. 目录结构** 更新为：

```text
RedDot/
├── RedDotKey.cs                   # 统一标识符结构体
├── RedDotModule.cs                # 核心模块（生命周期、树构建、OnUpdate、Calculator、状态查询、动态节点、持久化、广播）
├── RedDotNode.cs                  # 红点节点
├── RedDotState.cs                 # 状态查询结构体
├── RedDotChangedEventArgs.cs      # EventModule 广播事件参数
└── README.md                      # 本文档
```

- [ ] **Step 2: Commit**

```bash
git add "Unity/Assets/Scripts/Hotfix/Framework/RedDot/README.md"
git commit -m "docs: 同步 RedDot README 匹配重构后的 API"
```

---

### Task 7: 最终验证

- [ ] **Step 1: 完整编译**

在 Unity Editor 中执行 `Assets > Recompile All`，确认零编译错误。

- [ ] **Step 2: 搜索残留引用**

```bash
grep -rn "RedDotModule.Feature\|StaticNodeDict\|DynamicNodeDict\|m_ChangedStaticSet\|m_ChangedDynamicSet\|ChangedStaticKeys\|ChangedDynamicKeys" --include="*.cs" "Unity/Assets/Scripts/"
```
预期：零结果。

```bash
grep -rn "\.StaticKey\b\|\.DynamicKey\b" --include="*.cs" "Unity/Assets/Scripts/"
```
预期：仅在 `RedDotKey.cs`、`RedDotChangedEventArgs.cs`（事件 ID 常量）中出现，其他位置应为零。(`RedDotChangedEventArgs.EventId` 中的 `Dynamic` 不算)

- [ ] **Step 3: Play 模式验证**

在 Unity Editor 中进入 Play 模式，验证：
1. 红点模块正常初始化（控制台输出 `[RedDotModule] 初始化红点模块成功`）
2. 至少一个注册了 Calculator 的业务红点能正常显示
3. 触发业务事件后红点正确刷新
4. CompRedDot 组件正常解析 customData 并显示

- [ ] **Step 4: Commit**

```bash
git commit -m "chore: RedDot 重构最终验证通过" --allow-empty
```
