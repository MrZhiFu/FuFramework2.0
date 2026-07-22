# FuFramework RedDot Module

## 1. 简介

FuFramework RedDot 模块是游戏框架的红点提示系统，用于管理 UI 中的红点（未读标记）层级结构和计数。该模块采用树形结构管理红点节点，支持静态节点（配置表定义）和动态节点（运行时创建），计数变化时自动向上冒泡更新父节点并通知监听者刷新 UI。

## 2. 核心特性

- **树形层级**：红点节点按树形结构组织，子节点计数自动向上冒泡汇总
- **静态+动态**：静态节点由 Luban 配置表 `TbRedDot` 定义（通过 `ERedDotKey` 枚举标识），动态节点支持运行时通过 string Key 创建
- **自动冒泡**：叶子节点 `RawCount` 变化时，自动向上递归更新路径上所有父节点的 `TotalCount`
- **两种清理策略**：`Manual`（手动清除）和 `ViewAutoClean`（界面关闭自动清除）
- **两种显示模式**：`DotOnly`（仅显示红点）、`DotNumber`（红点+数字）、`Auto`（自动）
- **事件通知**：通过 `OnCountChanged` 事件通知监听者，注册时可选择立即通知当前状态

## 3. 核心概念

### 3.1 红点树形结构

```
                    Root (汇总所有红点)
                   /    \
           MainUI(3)    Bag(5)
          /    \        /    \
    Hero(2) Mail(1)  Item(3) Equip(2)
```

### 3.2 计数规则

- **RawCount**：节点自身的原始计数（叶子节点设置）
- **TotalCount**：RawCount + 所有子节点的 TotalCount 之和
- 修改叶子节点的 RawCount 后，会自动向上冒泡重新计算路径上所有父节点的 TotalCount
- 动态节点在 `RawCount` 归零后会自动从树中移除并回收

### 3.3 清理策略（ERedDotCleanStrategy）

命名空间：`Hotfix.Game.Config`

| 策略 | 值 | 说明 |
|------|-----|------|
| `Manual` | 0 | 手动清除，需要业务代码显式调用 |
| `ViewAutoClean` | 1 | 界面关闭时框架自动清除（通过 `TryAutoClean` 触发） |

### 3.4 显示模式（ERedDotDisplayMode）

命名空间：`Hotfix.Game.Config`

| 模式 | 值 | 说明 |
|------|-----|------|
| `DotOnly` | 0 | 只显示红点，不显示数字 |
| `DotNumber` | 1 | 红点+数字 |
| `Auto` | 2 | 自动 |

## 4. 核心类说明

### 4.1 RedDotModule

红点管理模块，继承自 `ModuleBase`。系统启动时自动在 `OnInit()` 中从 Luban 配置表 `TbRedDot` 构建静态节点树。

**核心属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| `Instance` | `RedDotModule` | 模块静态单例 |

**核心方法 — 静态节点（ERedDotKey 重载）：**

```csharp
// 注册/注销监听
void Register(ERedDotKey key, Action<int> onChange, bool immediateNotify = true)
void Unregister(ERedDotKey key, Action<int> onChange)

// 获取节点
RedDotNode GetNode(ERedDotKey key)

// 获取红点数量
int GetCount(ERedDotKey key)

// 检查节点是否存在
bool HasNode(ERedDotKey key)

// 设置红点数量
void SetCount(ERedDotKey key, int count)

// 增减红点数量
void AddCount(ERedDotKey key, int value = 1)
void SubCount(ERedDotKey key, int value = 1)

// 重置红点数量为 0
void ResetCount(ERedDotKey key)

// 添加动态子节点
RedDotNode AddDynamicChild(ERedDotKey parentKey, string childName)

// 自动清除（仅 ViewAutoClean 策略的节点生效）
void TryAutoClean(ERedDotKey key)
```

**核心方法 — 动态节点（string 重载）：**

```csharp
// 注册/注销监听
void Register(string key, Action<int> onChange, bool immediateNotify = true)
void Unregister(string key, Action<int> onChange)
void UnregisterAll(string key)

// 获取节点 / 数量
RedDotNode GetNode(string key)
int GetCount(string key)
bool HasNode(string key)

// 设置红点数量（归零时自动回收节点）
void SetCount(string key, int count)
void AddCount(string key, int value = 1)
void SubCount(string key, int value = 1)
void ResetCount(string key)
```

**通用方法：**

```csharp
// 清理所有节点的监听器
void ClearAllListeners()
```

### 4.2 RedDotNode

红点节点类，实现 `IReference` 接口（使用对象池管理）。分为静态节点（`StaticKey` 有值）和动态节点（`DynamicKey` 有值）。

命名空间：`Hotfix.Framework.RedDot`

**核心属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| `StaticKey` | `ERedDotKey?` | 静态节点 Key（配置表定义），动态节点为 null |
| `DynamicKey` | `string` | 动态节点 Key（运行时创建），静态节点为 null |
| `RawCount` | `int` | 节点的原始计数 |
| `TotalCount` | `int` | 节点的总计数（自身 + 所有子节点） |
| `Parent` | `RedDotNode` | 父节点 |
| `DisplayMode` | `ERedDotDisplayMode` | 显示模式（来自配置表） |
| `CleanStrategy` | `ERedDotCleanStrategy` | 清理策略（来自配置表） |

**事件：**

```csharp
event Action<int> OnCountChanged   // 计数变化时触发，参数为 TotalCount
```

**核心方法：**

```csharp
// 创建静态节点（由 RedDotModule 内部调用）
static RedDotNode Create(ERedDotKey key, RedDotNode parent, ERedDotDisplayMode displayMode, ERedDotCleanStrategy cleanStrategy)

// 创建动态节点（默认 DotOnly + Manual）
static RedDotNode CreateDynamic(string key, RedDotNode parent)

// 设置父节点
void SetParent(RedDotNode parent)

// 子节点管理
void AddChild(RedDotNode child)
void RemoveChild(RedDotNode child)

// 设置计数（自动向上传播）
void SetCount(int count)

// 获取子节点（只读）
IReadOnlyList<RedDotNode> GetChildren()

// 清除所有事件监听
void ClearAllListeners()

// 回收到对象池时清理
void Clear()
```

## 5. 使用示例

### 5.1 设置红点计数

```csharp
using Hotfix.Framework.RedDot;

// 设置邮件红点为 5
RedDotModule.Instance.SetCount(ERedDotKey.Mail, 5);

// 邮件已被阅读后，重置红点
RedDotModule.Instance.ResetCount(ERedDotKey.Mail);

// 递增 / 递减
RedDotModule.Instance.AddCount(ERedDotKey.Bag, 1);   // 背包红点 +1
RedDotModule.Instance.SubCount(ERedDotKey.Bag, 1);   // 背包红点 -1
```

### 5.2 注册监听红点变化

```csharp
using Hotfix.Framework.Core;
using Hotfix.Framework.RedDot;
using UnityEngine;

public class MailButton : MonoBehaviour
{
    private RedDotModule m_RedDotModule;
    private GameObject m_RedDotIcon;

    private void Start()
    {
        m_RedDotModule = ModuleManager.GetModule<RedDotModule>();

        // 注册监听（默认 immediateNotify = true，立即通知当前状态）
        m_RedDotModule.Register(ERedDotKey.Mail, OnMailRedDotChanged);
    }

    private void OnMailRedDotChanged(int count)
    {
        // 根据计数显示/隐藏红点图标
        m_RedDotIcon.SetActive(count > 0);
    }

    private void OnDestroy()
    {
        // 必须注销监听，避免内存泄漏
        m_RedDotModule?.Unregister(ERedDotKey.Mail, OnMailRedDotChanged);
    }
}
```

### 5.3 动态节点

```csharp
using Hotfix.Framework.RedDot;

// 为邮件静态节点下创建动态子节点（如具体某封邮件的红点）
var dynamicNode = RedDotModule.Instance.AddDynamicChild(ERedDotKey.Mail, "Mail_1001");

// 设置动态节点计数
RedDotModule.Instance.SetCount("Mail_1001", 1);

// 注册动态节点监听
RedDotModule.Instance.Register("Mail_1001", (count) =>
{
    // 更新特定邮件项的红点状态
});

// 重置动态节点（归零后会自动从树中移除并回收）
RedDotModule.Instance.ResetCount("Mail_1001");

// 注销动态节点所有监听
RedDotModule.Instance.UnregisterAll("Mail_1001");
```

### 5.4 自动清除策略

```csharp
using Hotfix.Framework.RedDot;

// 配置表中将某节点 CleanStrategy 设为 ViewAutoClean
// 在界面关闭时调用：
RedDotModule.Instance.TryAutoClean(ERedDotKey.Mail);

// 只有 CleanStrategy == ERedDotCleanStrategy.ViewAutoClean 的节点才会被清除
// 清除时会递归清除该节点及其所有子节点的计数
```

## 6. 目录结构

```text
RedDot/
├── RedDotModule.cs                # 红点管理模块
├── RedDotNode.cs                  # 红点节点
└── README.md                      # 本文档
```

## 7. 依赖

- **Hotfix.Framework.Core**：提供 `ModuleBase` 基类、`ModuleManager`
- **Hotfix.Framework.Config**：配置表系统（`ConfigModule`、Luban `TbRedDot`）
- **Hotfix.Framework.ReferencePools**：引用池（`ReferencePool`）
- **Hotfix.Game.Config**：配置表枚举（`ERedDotKey`、`ERedDotCleanStrategy`、`ERedDotDisplayMode`）
- **AOT.Framework.Core.Log**：日志（`FuLogger`）

## 8. 最佳实践

1. **静态节点优先**：固定结构的红点用 Luban 配置表定义，运行时无需关注节点创建
2. **动态节点按需创建**：动态内容（如具体某条消息）使用 `AddDynamicChild` 创建，归零后自动回收
3. **UI 分离监听**：每个红点 UI 组件只监听自己关心的节点，OnDestroy 中必须调用 `Unregister`
4. **及时注销**：界面销毁时必须注销监听，避免内存泄漏（可用 `UnregisterAll` 一次性清理动态节点）
5. **计数汇总**：利用树形结构自动汇总，父节点无需额外计算
6. **使用 TryAutoClean**：对于 ViewAutoClean 策略的节点，在界面关闭时统一清理

## 9. 注意事项

1. 静态节点树在 `OnInit()` 中自动构建（从配置表读取），无需手动调用初始化方法
2. 动态节点 Key 必须唯一，重复创建同名动态节点会返回已存在的节点
3. 动态节点 `RawCount` 归零后会自动调用 `RemoveChild` 从父节点移除，并从 `DynamicNodes` 字典中移除后回收
4. `TryAutoClean` 只对 `CleanStrategy == ERedDotCleanStrategy.ViewAutoClean` 的节点生效
5. 设置节点计数时，`OnCountChanged` 事件会在 `TotalCount` 实际变化时才触发（不变时不触发）
6. 叶子节点的计数变化会递归向上冒泡更新所有父节点
