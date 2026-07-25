# FuFramework RedDot Module

## 1. 简介

FuFramework RedDot 模块是游戏框架的红点提示系统，用于管理 UI 中的红点（未读标记）层级结构和计数。该模块采用 **Pull 模式（Calculate Provider）** 驱动，业务模块注册计算函数，框架通过 EventModule 事件触发 + OnUpdate 批处理自动重算，最后通过 EventModule 批量广播变更通知 UI 刷新。

## 2. 核心特性

- **Pull 模式**：业务模块通过 `Register` 注册 `Func<int>` 计算函数，框架自动管理重算时机
- **OnUpdate 批处理**：多个脏节点在同一帧内统一重算、聚合、广播，避免重复计算
- **EventModule 广播**：通过 `RedDotChangedEventArgs` 批量通知变更节点，UI 端按 Key 过滤刷新
- **树形层级**：红点节点按树形结构组织，支持 Any/Sum 两种聚合逻辑
- **静态+动态**：静态节点由 Luban 配置表 `TbRedDot` 定义（`ERedDotKey` 枚举），动态节点运行时通过 string Key 创建
- **已读持久化**：`MarkRead` 通过 StorageModule 持久保存已读状态，重启后自动恢复
- **动态红点**：`SyncDynamicNodes` 管理 批量动态子节点（如每封邮件的独立红点）
- **两种清理策略**：`Manual`（手动清除）和 `ViewAutoClean`（界面关闭自动清除）
- **三种显示模式**：`DotOnly`（仅显示红点）、`DotNumber`（红点+数字）、`Auto`（自动）

## 3. 核心概念

### 3.1 数据流

```
[业务模块] Register(key, () => GetCount(), "EventId")
    ↓
[EventModule] 事件触发 → OnLeafTriggerEvent → node.IsDirty = true
    ↓
[RedDotModule.OnUpdate] CalculateProvider() → SetCount → UpdateTotalCount → 向上传播
    ↓ 收集变更 Key
[EventModule.Broadcast] RedDotChangedEventArgs(ChangedStaticKeys, ChangedDynamicKeys)
    ↓
[CompRedDot] 按 key 过滤 → GetState(key) → RefreshUI(Count, DisplayMode)
```

### 3.2 计数规则

- **RawCount**：节点自身的原始计数（由 CalculateProvider 计算）
- **TotalCount**：RawCount + 所有子节点的 TotalCount 之和
- 叶子节点重算后会自动向上冒泡更新路径上所有父节点
- 聚合逻辑受 `LogicType` 和 `IsActive` 控制

### 3.3 聚合逻辑（ERedDotLogicType）

命名空间：`Hotfix.Game.Config`

| 逻辑 | 值 | 说明 |
|------|-----|------|
| `Sum` | 0 | 求和：父节点 TotalCount = RawCount + Σ(子节点 TotalCount) |
| `Any` | 1 | 任一：父节点 TotalCount = RawCount + (任一子节点 TotalCount > 0 ? 1 : 0) |

### 3.4 清理策略（ERedDotCleanStrategy）

命名空间：`Hotfix.Game.Config`

| 策略 | 值 | 说明 |
|------|-----|------|
| `Manual` | 0 | 手动清除，需要业务代码显式调用 |
| `ViewAutoClean` | 1 | 界面关闭时框架自动清除（通过 `TryAutoClean` 触发） |

### 3.5 显示模式（ERedDotDisplayMode）

命名空间：`Hotfix.Game.Config`

| 模式 | 值 | 说明 |
|------|-----|------|
| `DotOnly` | 0 | 只显示红点，不显示数字 |
| `DotNumber` | 1 | 红点+数字 |
| `Auto` | 2 | =1 显示红点，>1 显示数字 |

## 4. 核心类说明

### 4.1 RedDotModule

红点管理模块，继承自 `ModuleBase`。系统启动时自动在 `OnInit()` 中从 Luban 配置表 `TbRedDot` 构建静态节点树。

```csharp
// 模块单例
public static RedDotModule Instance { get; private set; }
```

**核心方法：**

```csharp
// === Calculate Provider 注册 ===
void Register(ERedDotKey key, Func<int> provider, params string[] triggerEvents)
void Register(string key, Func<int> provider, params string[] triggerEvents)
void Unregister(ERedDotKey key)
void Unregister(string key)

// === 状态查询 ===
RedDotState GetState(ERedDotKey key)
RedDotState GetState(string key)
bool HasNode(ERedDotKey key)
bool HasNode(string key)

// === 动态节点 ===
RedDotNode AddDynamicChild(ERedDotKey parentKey, string childName)
void RemoveDynamicChild(string childName)

// === 动态红点 ===
void SyncDynamicNodes(ERedDotKey parentKey, IReadOnlyList<long> dynamicKeys, Func<long, Func<int>> providerFactory)
void RefreshDynamicNode(ERedDotKey parentKey, long dynamicKey)
RedDotState GetDynamicState(ERedDotKey parentKey, long dynamicKey)

// === 已读持久化 ===
void MarkRead(ERedDotKey key)
void MarkRead(string key)
bool IsRead(ERedDotKey key)
bool IsRead(string key)

// === 清理策略 ===
void TryAutoClean(ERedDotKey key)
```

### 4.2 RedDotNode

红点节点类，实现 `IReference` 接口（使用对象池管理）。分为静态节点（`StaticKey` 有值）和动态节点（`DynamicKey` 有值）。

命名空间：`Hotfix.Framework.RedDot`

**核心属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| `StaticKey` | `ERedDotKey?` | 静态节点 Key，动态节点为 null |
| `DynamicKey` | `string` | 动态节点 Key，静态节点为 null |
| `RawCount` | `int` | 节点的原始计数 |
| `TotalCount` | `int` | 节点的总计数（自身 + 所有子节点） |
| `Parent` | `RedDotNode` | 父节点 |
| `DisplayMode` | `ERedDotDisplayMode` | 显示模式（来自配置表） |
| `CleanStrategy` | `ERedDotCleanStrategy` | 清理策略（来自配置表） |
| `LogicType` | `ERedDotLogicType` | 聚合逻辑（Sum / Any） |
| `IsActive` | `bool` | 是否激活（false 时 TotalCount 永远为 0） |
| `ShowOrder` | `int` | UI 显示排序权重 |
| `IsRead` | `bool` | 是否已读 |
| `IsDirty` | `bool` | 脏标记（本帧待重算） |
| `CalculateProvider` | `Func<int>` | 叶子计算函数 |
| `TriggerEvents` | `string[]` | 触发重算的 EventModule 事件 ID 列表 |

### 4.3 RedDotState

命名空间：`Hotfix.Framework.RedDot`

```csharp
public struct RedDotState
{
    public int Count;                    // 红点数量
    public int ShowOrder;                // 显示排序权重
    public bool IsActive;                // 节点是否激活
    public ERedDotDisplayMode DisplayMode; // 显示模式
    public static readonly RedDotState Empty; // 默认空状态
}
```

### 4.4 RedDotChangedEventArgs

命名空间：`Hotfix.Framework.RedDot`

```csharp
public sealed class RedDotChangedEventArgs : GameEventArgs
{
    public static readonly string EventId = typeof(RedDotChangedEventArgs).FullName;
    public readonly List<ERedDotKey> ChangedStaticKeys;   // 变化的静态节点
    public readonly List<string> ChangedDynamicKeys;       // 变化的动态节点
    public static RedDotChangedEventArgs Create();
}
```

### 4.5 CompRedDot（UI 组件）

FGUI 自定义组件，放置到界面后自动解析 `customData` 中的 `red_dot:<key>` 并订阅 EventModule 变更通知。

**customData 格式**：`red_dot:Bag_Item`（支持与其他插件竖线分隔，如 `i18n&key|red_dot:Bag_Item`）

**说明：** CompRedDot 通过 FGUI 编辑器放置后自动工作，无公开方法。动态红点（string Key）同样支持，只需在 customData 中配置非枚举名即可。


## 5. 使用示例

### 5.1 注册 Calculate Provider

```csharp
using Hotfix.Framework.RedDot;

// 为邮件红点注册计算函数，当 "MailChanged" 事件触发时自动重算
RedDotModule.Instance.Register(ERedDotKey.Mail, () =>
{
    return MailManager.Instance.GetUnreadCount();
}, "MailChanged");

// 注销 Calculate Provider（恢复为不活跃状态）
RedDotModule.Instance.Unregister(ERedDotKey.Mail);
```

### 5.2 业务层触发重算

```csharp
using Hotfix.Framework.Event;

// 邮件数据变更时广播事件，触发所有监听该事件的 Calculate Provider 重算
GlobalModule.EventModule.Broadcast(this, MailChangedEventArgs.EventId);
```

### 5.3 监听红点变化（UI 端）

```csharp
using Hotfix.Framework.Core;
using Hotfix.Framework.RedDot;
using Hotfix.Framework.Event;

// 方式一：使用 CompRedDot 组件（推荐）
// 在 FGUI 编辑器中放置 CompRedDot，设置 customData = "red_dot:Mail"
// 组件自动解析并订阅，无需任何代码

// 方式二：手动订阅 EventModule
private void Start()
{
    GlobalModule.EventModule.Subscribe(RedDotChangedEventArgs.EventId, OnRedDotChanged);
}

private void OnRedDotChanged(object sender, GameEventArgs e)
{
    if (e is not RedDotChangedEventArgs args) return;

    foreach (var key in args.ChangedStaticKeys)
    {
        if (key == ERedDotKey.Mail)
        {
            var state = RedDotModule.Instance.GetState(key);
            // 根据 state.Count / state.DisplayMode 更新 UI
        }
    }
}
```

### 5.4 动态红点

```csharp
// 同步邮箱的动态红点（每封邮件一个独立红点）
RedDotModule.Instance.SyncDynamicNodes(ERedDotKey.Mail, mailIds, (mailId) =>
{
    return () => MailManager.Instance.GetReadState(mailId) ? 0 : 1;
});

// 刷新单个动态节点
RedDotModule.Instance.RefreshDynamicNode(ERedDotKey.Mail, 1001);

// 查询动态节点状态
var state = RedDotModule.Instance.GetDynamicState(ERedDotKey.Mail, 1001);
```

### 5.5 已读持久化

```csharp
// 标记邮件红点为已读（计数归零 + 持久化，重启后保持）
RedDotModule.Instance.MarkRead(ERedDotKey.Mail);

// 检查是否已读
if (RedDotModule.Instance.IsRead(ERedDotKey.Mail))
{
    // ...
}
```

### 5.6 自动清除策略

```csharp
// 配置表中将某节点 CleanStrategy 设为 ViewAutoClean
// 在界面关闭时调用：
RedDotModule.Instance.TryAutoClean(ERedDotKey.Mail);
// 只有 CleanStrategy == ERedDotCleanStrategy.ViewAutoClean 的节点才会被递归清除
```

## 6. 目录结构

```text
RedDot/
├── RedDotModule.cs                # 核心模块（生命周期、树构建、OnUpdate、Calculate Provider、状态查询、动态节点）
├── RedDotModule.Feature.cs        # 功能扩展（动态红点、已读持久化、清理策略、内部广播）
├── RedDotNode.cs                  # 红点节点
├── RedDotState.cs                 # 状态查询结构体
├── RedDotChangedEventArgs.cs      # EventModule 广播事件参数
└── README.md                      # 本文档
```

## 7. 依赖

- **Hotfix.Framework.Core**：提供 `ModuleBase` 基类、`ModuleManager`、`GlobalModule`
- **Hotfix.Framework.Config**：配置表系统（`ConfigModule`、Luban `TbRedDot`）
- **Hotfix.Framework.ReferencePools**：引用池（`ReferencePool`）
- **Hotfix.Framework.Event**：事件系统（`EventModule`、`GameEventArgs`）
- **Hotfix.Framework.Storage**：本地存储（`StorageModule`，用于已读持久化）
- **Hotfix.Game.Config**：配置表枚举（`ERedDotKey`、`ERedDotLogicType`、`ERedDotCleanStrategy`、`ERedDotDisplayMode`）
- **AOT.Framework.Core.Log**：日志（`FuLogger`）

## 8. 最佳实践

1. **Calculate Provider 优先**：叶子节点使用 `Register` 注册计算函数，框架自动管理重算时机
2. **事件驱动**：业务数据变更时广播对应 EventModule 事件，触发 Calculate Provider 自动重算
3. **CompRedDot 零侵入**：UI 层优先使用 CompRedDot 组件，在 FGUI 编辑器中配置 `customData` 即可，无需写代码
4. **已读持久化**：对需要跨会话保持已读状态的红点，使用 `MarkRead`
5. **动态红点按需管理**：动态动态红点使用 `SyncDynamicNodes` 批量管理，框架自动处理增删
6. **聚合逻辑按需选择**：根节点用 Sum 汇总所有子红点，菜单入口用 Any 检测是否有新内容

## 9. 注意事项

1. 静态节点树在 `OnInit()` 中自动构建（从配置表读取），无需手动调用初始化方法
2. 动态节点 Key 必须唯一，重复创建同名动态节点会返回已存在的节点
3. Calculate Provider 不应执行昂贵操作，因为可能在同一个 OnUpdate 中多次调用
4. 同一节点不应同时注册 Calculate Provider 和手动调 SetCount（SetCount 为 internal，仅框架内部使用）
5. `IsActive = false` 的节点 TotalCount 永远为 0，且不参与父节点聚合
6. 已读状态仅抑制初始加载时的红点，运行时新的 Calculate Provider 计算结果会覆盖已读状态
