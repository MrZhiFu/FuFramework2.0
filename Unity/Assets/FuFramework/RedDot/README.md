# FuFramework RedDot Module

## 概述

RedDot 模块是 FuFramework 中的红点管理系统，专门用于管理游戏和应用中的红点提示功能。该模块采用树形结构设计，支持父子节点的层级关系，能够自动计算总红点数量，并提供事件通知机制。

### 核心特性

- **树形结构管理**：支持父子节点层级关系，自动计算总计数
- **事件通知机制**：计数变化时自动通知所有监听者
- **对象池管理**：使用引用池减少 GC 分配
- **配置化驱动**：通过 ScriptableObject 配置文件初始化红点树结构
- **高性能设计**：优化的数据结构，支持大量节点管理

## 系统架构

### 核心类说明

#### 1. RedDotModule
红点系统管理器，继承自 FuModule，负责整个红点系统的生命周期管理。

**主要职责：**
- 初始化红点树结构
- 提供红点计数操作接口
- 管理事件监听机制
- 处理模块的初始化和关闭

#### 2. RedDotNode
红点节点类，实现 IReference 接口，支持对象池管理。

**主要属性：**
- `Key`：节点唯一标识
- `RawCount`：节点的原始计数
- `TotalCount`：节点的总计数（包含子节点）
- `Parent`：父节点引用
- `Path`：节点完整路径

### 技术架构

```
RedDotModule (管理器)
    ↓
RedDotNode (节点树)
    ├── 父节点
    │   ├── 子节点1
    │   ├── 子节点2
    │   └── 子节点3
    └── 兄弟节点
```

## 快速开始

### 1. 配置红点树结构

首先需要在 ModuleSetting 中配置红点树结构。创建一个 RedDotSetting ScriptableObject 配置文件，定义红点节点的层级关系。

```csharp
// 示例：在 ModuleSetting 中配置红点树
[CreateAssetMenu(fileName = "RedDotSetting", menuName = "FuFramework/RedDot Setting")]
public class RedDotSetting : ScriptableObject
{
    public List<RedDotNodeData> m_RootNodes = new();
}

[Serializable]
public class RedDotNodeData
{
    public string m_Key;
    public List<RedDotNodeData> m_Children;
}
```

### 2. 基本使用示例

```csharp
using FuFramework.RedDot.Runtime;

public class RedDotExample : MonoBehaviour
{
    private void Start()
    {
        var RedDotModule = GlobalModule.RedDotModule;
        
        // 注册红点变化监听
        RedDotModule.Register("mail_system", OnMailCountChanged);
        
        // 设置红点数量
        RedDotModule.SetCount("mail_system", 5);
        
        // 递增红点数量
        RedDotModule.IncrementCount("mail_system", 2);
        
        // 重置红点数量
        RedDotModule.ResetCount("mail_system");
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

## 详细使用指南

### 1. 红点树配置示例

```csharp
// 定义红点树结构
public static class RedDotKeys
{
    // 根节点
    public const string Main = "main";
    public const string Mail = "mail";
    public const string Task = "task";
    
    // 邮件系统子节点
    public const string MailSystem = "mail_system";
    public const string MailFriend = "mail_friend";
    public const string MailGuild = "mail_guild";
    
    // 任务系统子节点
    public const string TaskDaily = "task_daily";
    public const string TaskWeekly = "task_weekly";
    public const string TaskAchievement = "task_achievement";
}

// 对应的树形结构
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

### 2. 复杂场景使用示例

#### 邮件系统红点管理

```csharp
public class MailSystemManager : MonoBehaviour
{
    private void OnEnable()
    {
        // 注册所有邮件相关红点监听
        var RedDotModule = GlobalModule.RedDotModule;
        RedDotModule.Register(RedDotKeys.MailSystem, OnSystemMailCountChanged);
        RedDotModule.Register(RedDotKeys.MailFriend, OnFriendMailCountChanged);
        RedDotModule.Register(RedDotKeys.MailGuild, OnGuildMailCountChanged);
        RedDotModule.Register(RedDotKeys.Mail, OnTotalMailCountChanged);
    }
    
    private void OnDisable()
    {
        // 注销所有监听
        var RedDotModule = GlobalModule.RedDotModule;
        RedDotModule.Unregister(RedDotKeys.MailSystem, OnSystemMailCountChanged);
        RedDotModule.Unregister(RedDotKeys.MailFriend, OnFriendMailCountChanged);
        RedDotModule.Unregister(RedDotKeys.MailGuild, OnGuildMailCountChanged);
        RedDotModule.Unregister(RedDotKeys.Mail, OnTotalMailCountChanged);
    }
    
    // 收到新系统邮件
    public void OnReceiveSystemMail()
    {
        RedDotModule.IncrementCount(RedDotKeys.MailSystem);
    }
    
    // 阅读所有系统邮件
    public void OnReadAllSystemMails()
    {
        RedDotModule.ResetCount(RedDotKeys.MailSystem);
    }
    
    // 批量重置所有邮件红点
    public void ResetAllMailCounts()
    {
        RedDotModule.ResetCounts(
            RedDotKeys.MailSystem, 
            RedDotKeys.MailFriend, 
            RedDotKeys.MailGuild
        );
    }
    
    private void OnSystemMailCountChanged(int count)
    {
        UpdateSystemMailRedDot(count);
    }
    
    private void OnFriendMailCountChanged(int count)
    {
        UpdateFriendMailRedDot(count);
    }
    
    private void OnGuildMailCountChanged(int count)
    {
        UpdateGuildMailRedDot(count);
    }
    
    private void OnTotalMailCountChanged(int count)
    {
        UpdateTotalMailRedDot(count);
    }
}
```

#### 任务系统红点管理

```csharp
public class TaskSystemManager : MonoBehaviour
{
    private Dictionary<string, int> m_TaskCounts = new();
    
    public void InitializeTaskCounts()
    {
        // 从服务器获取任务数据
        var dailyTasks = GetDailyTasks();
        var weeklyTasks = GetWeeklyTasks();
        var achievementTasks = GetAchievementTasks();
        
        // 设置红点数量
        RedDotModule.SetCount(RedDotKeys.TaskDaily, dailyTasks.Count);
        RedDotModule.SetCount(RedDotKeys.TaskWeekly, weeklyTasks.Count);
        RedDotModule.SetCount(RedDotKeys.TaskAchievement, achievementTasks.Count);
    }
    
    // 完成任务
    public void CompleteTask(string taskType, string taskId)
    {
        switch (taskType)
        {
            case "daily":
                RedDotModule.DecrementCount(RedDotKeys.TaskDaily);
                break;
            case "weekly":
                RedDotModule.DecrementCount(RedDotKeys.TaskWeekly);
                break;
            case "achievement":
                RedDotModule.DecrementCount(RedDotKeys.TaskAchievement);
                break;
        }
    }
    
    // 刷新任务（如每日重置）
    public void RefreshTasks()
    {
        var newDailyTasks = GetDailyTasks();
        RedDotModule.SetCount(RedDotKeys.TaskDaily, newDailyTasks.Count);
    }
}
```

### 3. UI 组件集成示例

```csharp
public class RedDotUIComponent : MonoBehaviour
{
    [SerializeField] private string m_RedDotKey;
    [SerializeField] private GameObject m_RedDotObject;
    [SerializeField] private Text m_CountText;
    
    private void Start()
    {
        // 注册红点监听
        RedDotModule.Register(m_RedDotKey, OnRedDotCountChanged, true);
    }
    
    private void OnRedDotCountChanged(int count)
    {
        // 更新红点显示状态
        bool hasRedDot = count > 0;
        m_RedDotObject.SetActive(hasRedDot);
        
        // 更新数量显示
        if (m_CountText != null)
        {
            m_CountText.text = count > 99 ? "99+" : count.ToString();
        }
    }
    
    private void OnDestroy()
    {
        // 注销监听
        if (RedDotModule.HasInstance)
        {
            RedDotModule.Unregister(m_RedDotKey, OnRedDotCountChanged);
        }
    }
    
    // 点击红点区域（如阅读所有邮件）
    public void OnClickRedDotArea()
    {
        RedDotModule.ResetCount(m_RedDotKey);
    }
}
```

## 高级用法

### 1. 动态节点管理

```csharp
public class DynamicRedDotManager : MonoBehaviour
{
    // 动态创建红点节点（需要扩展 RedDotModule）
    public void CreateDynamicNode(string parentKey, string newNodeKey)
    {
        var parentNode = RedDotModule.GetNode(parentKey);
        if (parentNode != null)
        {
            // 动态创建节点逻辑（需要扩展现有系统）
            CreateDynamicNodeInternal(parentNode, newNodeKey);
        }
    }
    
    // 动态移除节点
    public void RemoveDynamicNode(string nodeKey)
    {
        // 动态移除节点逻辑
        RemoveDynamicNodeInternal(nodeKey);
    }
}
```

### 2. 条件红点控制

```csharp
public class ConditionalRedDotManager : MonoBehaviour
{
    [SerializeField] private string m_RedDotKey;
    [SerializeField] private int m_MinLevel = 10; // 最低等级要求
    
    private void Update()
    {
        // 根据条件控制红点显示
        int playerLevel = GetPlayerLevel();
        bool shouldShowRedDot = playerLevel >= m_MinLevel && HasNewContent();
        
        if (shouldShowRedDot)
        {
            RedDotModule.SetCount(m_RedDotKey, 1);
        }
        else
        {
            RedDotModule.ResetCount(m_RedDotKey);
        }
    }
    
    private bool HasNewContent()
    {
        // 检查是否有新内容
        return CheckServerForNewContent();
    }
}
```

### 3. 红点分组管理

```csharp
public class RedDotGroupManager : MonoBehaviour
{
    private readonly List<string> m_MailGroup = new()
    {
        RedDotKeys.MailSystem,
        RedDotKeys.MailFriend,
        RedDotKeys.MailGuild
    };
    
    private readonly List<string> m_TaskGroup = new()
    {
        RedDotKeys.TaskDaily,
        RedDotKeys.TaskWeekly,
        RedDotKeys.TaskAchievement
    };
    
    // 重置整个邮件组红点
    public void ResetMailGroup()
    {
        foreach (var key in m_MailGroup)
        {
            RedDotModule.ResetCount(key);
        }
    }
    
    // 获取组内总红点数量
    public int GetGroupTotalCount(List<string> groupKeys)
    {
        int total = 0;
        foreach (var key in groupKeys)
        {
            total += RedDotModule.GetCount(key);
        }
        return total;
    }
}
```

## 性能优化建议

### 1. 监听器管理优化

```csharp
public class OptimizedRedDotUsage : MonoBehaviour
{
    private Dictionary<string, Action<int>> m_RegisteredCallbacks = new();
    
    // 批量注册监听
    public void RegisterMultiple(List<string> keys, Action<int> callback)
    {
        foreach (var key in keys)
        {
            RedDotModule.Register(key, callback);
            m_RegisteredCallbacks[key] = callback;
        }
    }
    
    // 批量注销监听
    public void UnregisterMultiple(List<string> keys)
    {
        foreach (var key in keys)
        {
            if (m_RegisteredCallbacks.TryGetValue(key, out var callback))
            {
                RedDotModule.Unregister(key, callback);
                m_RegisteredCallbacks.Remove(key);
            }
        }
    }
    
    private void OnDestroy()
    {
        // 清理所有注册的监听
        foreach (var kvp in m_RegisteredCallbacks)
        {
            RedDotModule.Unregister(kvp.Key, kvp.Value);
        }
        m_RegisteredCallbacks.Clear();
    }
}
```

### 2. 红点更新频率控制

```csharp
public class ThrottledRedDotUpdater : MonoBehaviour
{
    private Dictionary<string, int> m_PendingUpdates = new();
    private float m_LastUpdateTime;
    private const float UPDATE_INTERVAL = 0.1f; // 100ms 更新间隔
    
    public void QueueRedDotUpdate(string key, int count)
    {
        m_PendingUpdates[key] = count;
        
        // 检查是否需要立即更新
        if (Time.time - m_LastUpdateTime >= UPDATE_INTERVAL)
        {
            ProcessPendingUpdates();
        }
    }
    
    private void Update()
    {
        // 定期处理积压的更新
        if (m_PendingUpdates.Count > 0 && Time.time - m_LastUpdateTime >= UPDATE_INTERVAL)
        {
            ProcessPendingUpdates();
        }
    }
    
    private void ProcessPendingUpdates()
    {
        foreach (var kvp in m_PendingUpdates)
        {
            RedDotModule.SetCount(kvp.Key, kvp.Value);
        }
        
        m_PendingUpdates.Clear();
        m_LastUpdateTime = Time.time;
    }
}
```

## 注意事项

### 1. 内存管理
- 及时注销不再使用的监听器
- 避免在频繁调用的方法中注册/注销监听
- 使用对象池减少 GC 压力

### 2. 性能考虑
- 避免在 Update 中频繁调用红点计数方法
- 对于大量节点，考虑使用批量操作
- 合理设计红点树结构，避免过深的层级

### 3. 错误处理
- 检查节点是否存在后再进行操作
- 处理监听器注册失败的情况
- 使用 try-catch 包装关键操作

### 4. 线程安全
- 红点操作应在主线程执行
- 避免多线程同时修改红点计数

## API 参考

### RedDotModule 类

#### 静态属性

##### Instance
```csharp
public static RedDotModule Instance { get; }
```
**功能**：获取红点管理器单例实例

#### 实例方法

##### Register(string key, Action<int> onChange, bool immediateNotify = true)
```csharp
public void Register(string key, Action<int> onChange, bool immediateNotify = true)
```
**功能**：注册节点状态变化的回调函数

**参数**：
- `key` (string)：节点的 key
- `onChange` (Action<int>)：节点状态变化的回调函数
- `immediateNotify` (bool)：是否立即通知当前状态，默认 true

**示例**：
```csharp
RedDotModule.Register("mail_system", OnMailCountChanged);
```

##### Unregister(string key, Action<int> onChange)
```csharp
public void Unregister(string key, Action<int> onChange)
```
**功能**：注销节点状态变化的回调函数

**参数**：
- `key` (string)：节点的 key
- `onChange` (Action<int>)：节点状态变化的回调函数

**示例**：
```csharp
RedDotModule.Unregister("mail_system", OnMailCountChanged);
```

##### SetCount(string key, int count)
```csharp
public void SetCount(string key, int count)
```
**功能**：设置节点的红点数量

**参数**：
- `key` (string)：节点的 key
- `count` (int)：红点数量

**示例**：
```csharp
RedDotModule.SetCount("mail_system", 5);
```

##### GetCount(string key)
```csharp
public int GetCount(string key)
```
**功能**：获取节点的红点数量

**参数**：
- `key` (string)：节点的 key

**返回值**：
- `int`：红点数量，节点不存在返回 0

**示例**：
```csharp
int count = RedDotModule.GetCount("mail_system");
```

## 常见问题解答

### Q: 红点数量不更新怎么办？
A: 检查是否正确注册了监听器，确认节点 key 是否正确，检查红点树配置是否包含该节点。

### Q: 如何实现红点的条件显示？
A: 可以在监听器中添加条件判断，或者使用 ConditionalRedDotManager 示例中的方法。

### Q: 红点系统支持动态节点吗？
A: 当前版本需要预先配置红点树结构，动态节点功能需要扩展实现。

### Q: 如何处理大量红点节点的性能问题？
A: 使用批量操作，控制更新频率，合理设计树形结构避免过深层级。

### Q: 红点计数可以设置为负数吗？
A: 不可以，红点计数会自动限制为不小于 0 的值。

## 总结

RedDot 模块为 FuFramework 提供了强大而灵活的红点管理系统，支持复杂的树形结构和事件通知机制。通过合理的配置和使用，可以轻松管理游戏中的各种红点提示需求。

该模块的设计注重性能和易用性，提供了完整的生命周期管理和错误处理机制，是游戏开发中红点功能的理想解决方案。