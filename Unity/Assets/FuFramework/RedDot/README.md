# 1. FuFramework RedDot Module

## 1. 简介

FuFramework RedDot 模块是游戏框架的红点管理系统，专门用于管理游戏和应用中的红点提示功能。该模块采用树形结构设计，支持父子节点的层级关系，能够自动计算总红点数量，并提供事件通知机制。

## 2. 核心特性

- **树形结构管理**：支持父子节点层级关系，自动计算总计数
- **事件通知机制**：计数变化时自动通知所有监听者
- **对象池管理**：使用引用池减少 GC 分配
- **配置化驱动**：通过 RedDotSetting (ScriptableObject) 配置文件初始化红点树结构
- **高性能设计**：优化的数据结构，支持大量节点管理

## 3. 核心概念

### 3.1 类继承与实现体系

```
【类继承体系】

FuModule (框架模块基类)
    └── RedDotModule (红点管理模块)

IReference (引用池接口)
    └── RedDotNode (红点节点)
        └── 实现 Clear() 方法用于对象池回收


【数据结构】

RedDotNodeData (配置数据结构)
    ├── m_Key: string          # 节点唯一标识
    ├── m_Children: List<RedDotNodeData>  # 子节点列表
    └── 用途: 在 RedDotSetting 中配置红点树结构


【模块依赖关系】

RedDotModule 依赖:
    └── ModuleSetting.Runtime.ModuleSetting
        └── RedDotSetting (ScriptableObject)
            └── m_RootNodes: List<RedDotNodeData>
                └── 构建完整的红点树
```

### 3.2 红点树架构

```
┌─────────────────────────────────────────────────────────────┐
│                    RedDotModule                             │
│                     (FuModule)                              │
│  ┌─────────────────────────────────────────────────────┐   │
│  │                    NodeDict                         │   │
│  │              Dictionary<string, RedDotNode>         │   │
│  │                                                       │   │
│  │  ┌─────────┐    ┌─────────┐    ┌─────────┐          │   │
│  │  │  Root1  │    │  Root2  │    │  Root3  │          │   │
│  │  │ (main)  │    │ (mail)  │    │ (task)  │          │   │
│  │  └────┬────┘    └────┬────┘    └────┬────┘          │   │
│  │       │              │              │               │   │
│  │  ┌────┴────┐    ┌────┴────┐    ┌────┴────┐          │   │
│  │  │ Child1  │    │ Child1  │    │ Child1  │          │   │
│  │  │ Child2  │    │ Child2  │    │ Child2  │          │   │
│  │  └─────────┘    └─────────┘    └─────────┘          │   │
│  │                                                       │   │
│  │  每个节点维护:                                        │   │
│  │  - Key: 唯一标识                                      │   │
│  │  - RawCount: 自身计数                                 │   │
│  │  - TotalCount: 总计数(自身+所有子节点)                │   │
│  │  - Parent: 父节点引用                                 │   │
│  │  - m_Children: 子节点列表                             │   │
│  │  - Path: 完整路径 (如 "main/mail/system")             │   │
│  │  - OnCountChanged: 计数变化事件                       │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
                    ┌──────────────────┐
                    │   RedDotSetting  │
                    │ (ScriptableObject)│
                    │   配置驱动初始化   │
                    └──────────────────┘
```

### 3.3 红点计数传播机制

```
【计数计算规则】

TotalCount = RawCount + Σ(所有子节点的 TotalCount)


【计数传播示例】

初始状态:
main (Raw: 0, Total: 0)
└── mail (Raw: 0, Total: 0)
    ├── system (Raw: 0, Total: 0)
    ├── friend (Raw: 0, Total: 0)
    └── guild (Raw: 0, Total: 0)


设置 system 节点计数为 5:

Step 1: system.SetCount(5)
    system: Raw=5, Total=5
    └── 触发 OnCountChanged(5)

Step 2: mail 收到子节点变化通知
    mail: Raw=0, Total=0+5=5
    └── 触发 OnCountChanged(5)

Step 3: main 收到子节点变化通知
    main: Raw=0, Total=0+5=5
    └── 触发 OnCountChanged(5)


最终状态:
main (Raw: 0, Total: 5)      ← UI显示: 5
└── mail (Raw: 0, Total: 5)  ← UI显示: 5
    ├── system (Raw: 5, Total: 5)  ← UI显示: 5
    ├── friend (Raw: 0, Total: 0)
    └── guild (Raw: 0, Total: 0)
```

### 3.4 生命周期管理

```
【模块生命周期】

OnInit()
    │
    ├── 读取 RedDotSetting 配置
    ├── 清空 NodeDict
    └── 递归构建红点树 (BuildNodeRecursive)
        ├── 从 ReferencePool 获取 RedDotNode
        ├── 设置 Key, Parent, Path
        ├── 添加到 NodeDict
        └── 递归处理子节点

OnDispose()
    │
    ├── 遍历 NodeDict 中的所有节点
    ├── 每个节点 Release 回 ReferencePool
    └── 清空 NodeDict


【节点生命周期】

Create(key, parent)
    │
    ├── ReferencePool.Acquire<RedDotNode>()
    ├── 初始化 Key, Parent, Path
    └── 返回节点

SetCount(count)
    │
    ├── 更新 RawCount
    └── 调用 UpdateTotalCount()
        ├── 计算子节点总计数
        ├── 更新 TotalCount
        ├── 触发 OnCountChanged 事件
        └── 递归通知父节点更新

Clear() (IReference 接口)
    │
    ├── 重置 Key, Path, Parent
    ├── 重置 RawCount, TotalCount
    ├── 清空 m_Children
    └── 清空 OnCountChanged 事件
```

## 4. 核心类详细说明

### 4.1 RedDotModule

红点管理模块，继承自 `FuModule`，负责整个红点系统的生命周期管理。

**核心功能：**

```csharp
public class RedDotModule : FuModule
{
    // 节点管理
    private static readonly Dictionary<string, RedDotNode> NodeDict;
    
    // 生命周期
    protected override void OnInit()           // 从配置初始化红点树
    protected override void OnDispose()        // 释放所有节点回对象池
    
    // 节点构建
    private void BuildNodeRecursive(RedDotNode parent, RedDotNodeData data)
    
    // 事件监听
    public void Register(string key, Action<int> onChange, bool immediateNotify = true)
    public void Unregister(string key, Action<int> onChange)
    public void UnregisterAll(string key)
    public void ClearAllListeners()
    
    // 节点查询
    public RedDotNode GetNode(string key)
    public int GetCount(string key)
    public bool HasNode(string key)
    
    // 计数操作
    public void SetCount(string key, int count)
    public void AddCount(string key, int value = 1)
    public void SubCount(string key, int value = 1)
    public void ResetCount(string key)
    public void ResetCounts(params string[] keys)
}
```

**模块依赖：**
- 依赖 `ModuleSetting.Runtime.ModuleSetting` 获取 `RedDotSetting` 配置
- 依赖 `ReferencePool` 进行节点对象池管理

### 4.2 RedDotNode

红点节点类，实现 `IReference` 接口，支持对象池管理。

**核心功能：**

```csharp
public class RedDotNode : IReference
{
    // 节点属性
    public string Key { get; private set; }           // 节点唯一标识
    public int RawCount { get; private set; }         // 自身红点计数
    public int TotalCount { get; private set; }       // 总计数(自身+子节点)
    public RedDotNode Parent { get; private set; }    // 父节点引用
    public string Path { get; private set; }          // 完整路径
    
    // 事件
    public event Action<int> OnCountChanged;          // 计数变化事件
    
    // 节点管理
    private readonly List<RedDotNode> m_Children;     // 子节点列表
    
    // 工厂方法
    public static RedDotNode Create(string key, RedDotNode parent)
    
    // 子节点管理
    public void AddChild(RedDotNode child)
    public IReadOnlyList<RedDotNode> GetChildren()
    
    // 计数管理
    public void SetCount(int count)
    private void UpdateTotalCount()
    
    // IReference 接口
    public void Clear()
    public void ClearAllListeners()
}
```

**实现细节：**
- 使用 `ReferencePool` 创建和回收，避免 GC 分配
- 计数变化时自动向上传播到父节点
- 支持事件监听，UI 组件可注册回调更新显示

## 5. 使用示例

### 5.1 配置红点树结构

首先需要在 `RedDotSetting` ScriptableObject 中配置红点树结构：

```csharp
// RedDotSetting.cs (ScriptableObject)
[CreateAssetMenu(fileName = "RedDotSetting", menuName = "FuFramework/RedDot Setting")]
public class RedDotSetting : ScriptableObject
{
    public List<RedDotNodeData> m_RootNodes = new();
}

[Serializable]
public class RedDotNodeData
{
    public string m_Key;                           // 节点唯一标识
    public List<RedDotNodeData> m_Children;        // 子节点列表
}
```

**配置示例（在 Unity Inspector 中配置）：**

```
RedDotSetting
└── m_RootNodes
    ├── [0] main
    │   └── m_Children
    │       ├── [0] mail
    │       │   └── m_Children
    │       │       ├── [0] mail_system
    │       │       ├── [1] mail_friend
    │       │       └── [2] mail_guild
    │       └── [1] task
    │           └── m_Children
    │               ├── [0] task_daily
    │               ├── [1] task_weekly
    │               └── [2] task_achievement
    └── [1] shop
        └── m_Children
            ├── [0] shop_new
            └── [1] shop_discount
```

### 5.2 基本使用示例

```csharp
using FuFramework.RedDot.Runtime;
using UnityEngine;

public class RedDotExample : MonoBehaviour
{
    private void Start()
    {
        var redDotModule = GlobalModule.RedDotModule;
        
        // 注册红点变化监听
        redDotModule.Register("mail_system", OnMailCountChanged);
        
        // 设置红点数量
        redDotModule.SetCount("mail_system", 5);
        
        // 递增红点数量
        redDotModule.AddCount("mail_system", 2);
        
        // 递减红点数量
        redDotModule.SubCount("mail_system", 1);
        
        // 重置红点数量
        redDotModule.ResetCount("mail_system");
    }
    
    private void OnMailCountChanged(int count)
    {
        Debug.Log($"邮件系统红点数量变化: {count}");
        // 更新UI显示
        UpdateMailRedDotUI(count);
    }
    
    private void OnDestroy()
    {
        // 注销监听
        GlobalModule.RedDotModule.Unregister("mail_system", OnMailCountChanged);
    }
}
```

### 5.3 邮件系统红点管理

```csharp
public class MailSystemManager : MonoBehaviour
{
    private void OnEnable()
    {
        var redDotModule = GlobalModule.RedDotModule;
        
        // 注册所有邮件相关红点监听
        redDotModule.Register("mail_system", OnSystemMailCountChanged);
        redDotModule.Register("mail_friend", OnFriendMailCountChanged);
        redDotModule.Register("mail_guild", OnGuildMailCountChanged);
        redDotModule.Register("mail", OnTotalMailCountChanged);
        redDotModule.Register("main", OnMainRedDotCountChanged);
    }
    
    private void OnDisable()
    {
        var redDotModule = GlobalModule.RedDotModule;
        
        // 注销所有监听
        redDotModule.Unregister("mail_system", OnSystemMailCountChanged);
        redDotModule.Unregister("mail_friend", OnFriendMailCountChanged);
        redDotModule.Unregister("mail_guild", OnGuildMailCountChanged);
        redDotModule.Unregister("mail", OnTotalMailCountChanged);
        redDotModule.Unregister("main", OnMainRedDotCountChanged);
    }
    
    // 收到新系统邮件
    public void OnReceiveSystemMail()
    {
        GlobalModule.RedDotModule.AddCount("mail_system");
    }
    
    // 阅读所有系统邮件
    public void OnReadAllSystemMails()
    {
        GlobalModule.RedDotModule.ResetCount("mail_system");
    }
    
    // 批量重置所有邮件红点
    public void ResetAllMailCounts()
    {
        GlobalModule.RedDotModule.ResetCounts(
            "mail_system", 
            "mail_friend", 
            "mail_guild"
        );
    }
    
    private void OnSystemMailCountChanged(int count)
    {
        // 更新系统邮件红点UI
        systemMailRedDotText.text = count > 0 ? count.ToString() : "";
        systemMailRedDotObj.SetActive(count > 0);
    }
    
    private void OnTotalMailCountChanged(int count)
    {
        // 更新邮件总红点UI（包含所有子邮件类型）
        mailRedDotText.text = count > 0 ? count.ToString() : "";
        mailRedDotObj.SetActive(count > 0);
    }
    
    private void OnMainRedDotCountChanged(int count)
    {
        // 更新主界面红点（包含邮件、任务等所有子系统）
        mainRedDotObj.SetActive(count > 0);
    }
}
```

### 5.4 UI 红点组件封装

```csharp
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 红点UI组件 - 自动绑定红点系统
/// </summary>
public class RedDotUI : MonoBehaviour
{
    [SerializeField] private string m_RedDotKey;           // 红点节点key
    [SerializeField] private Text m_CountText;             // 数量显示文本
    [SerializeField] private GameObject m_RedDotObj;       // 红点对象
    [SerializeField] private bool m_ShowZero = false;      // 是否显示0
    
    private void OnEnable()
    {
        if (string.IsNullOrEmpty(m_RedDotKey)) return;
        
        // 注册监听，立即通知当前状态
        GlobalModule.RedDotModule.Register(m_RedDotKey, OnRedDotChanged, true);
    }
    
    private void OnDisable()
    {
        if (string.IsNullOrEmpty(m_RedDotKey)) return;
        
        GlobalModule.RedDotModule.Unregister(m_RedDotKey, OnRedDotChanged);
    }
    
    private void OnRedDotChanged(int count)
    {
        // 更新红点显示
        if (m_RedDotObj != null)
        {
            m_RedDotObj.SetActive(count > 0);
        }
        
        // 更新数量文本
        if (m_CountText != null)
        {
            m_CountText.text = count > 0 || m_ShowZero ? count.ToString() : "";
        }
    }
}
```

## 6. 目录结构

```
Assets/FuFramework/RedDot/
├── Runtime/
│   ├── FuFramework.RedDot.Runtime.asmdef    # 程序集定义
│   ├── RedDotModule.cs                       # 红点管理模块
│   └── RedDotNode.cs                         # 红点节点类
└── README.md                                 # 本文档
```

## 7. 依赖

| 模块 | 说明 |
|------|------|
| FuFramework.Core | 提供 FuModule 基类、FuLogger |
| FuFramework.ReferencePool | 提供 IReference 接口和 ReferencePool |
| FuFramework.ModuleSetting | 提供 RedDotSetting 配置 |

## 8. 最佳实践

### 8.1 红点树设计规范

```csharp
// 1. 定义统一的节点Key常量
public static class RedDotKeys
{
    // 根节点
    public const string Main = "main";
    
    // 邮件系统
    public const string Mail = "mail";
    public const string MailSystem = "mail_system";
    public const string MailFriend = "mail_friend";
    public const string MailGuild = "mail_guild";
    
    // 任务系统
    public const string Task = "task";
    public const string TaskDaily = "task_daily";
    public const string TaskWeekly = "task_weekly";
    public const string TaskAchievement = "task_achievement";
}

// 2. 在 RedDotSetting 中配置树形结构
// main
// ├── mail
// │   ├── mail_system
// │   ├── mail_friend
// │   └── mail_guild
// └── task
//     ├── task_daily
//     ├── task_weekly
//     └── task_achievement
```

### 8.2 事件监听管理

```csharp
public class RedDotManager : MonoBehaviour
{
    // 使用字典管理监听器，便于批量注销
    private Dictionary<string, Action<int>> m_Listeners = new();
    
    private void RegisterRedDot(string key, Action<int> callback)
    {
        m_Listeners[key] = callback;
        GlobalModule.RedDotModule.Register(key, callback);
    }
    
    private void UnregisterAll()
    {
        foreach (var pair in m_Listeners)
        {
            GlobalModule.RedDotModule.Unregister(pair.Key, pair.Value);
        }
        m_Listeners.Clear();
    }
    
    private void OnDestroy()
    {
        UnregisterAll();
    }
}
```

### 8.3 注意事项

1. **配置先行**：确保在 RedDotSetting 中正确配置红点树结构，否则节点操作会失败
2. **Key 唯一性**：所有节点的 Key 必须唯一，重复 Key 会导致初始化错误
3. **事件注销**：组件销毁时务必注销红点监听，避免内存泄漏和空引用
4. **计数传播**：父节点的 TotalCount 会自动计算所有子节点，无需手动设置
5. **对象池**：RedDotNode 使用引用池管理，模块销毁时会自动回收所有节点
6. **线程安全**：红点操作应在主线程进行，避免多线程问题
