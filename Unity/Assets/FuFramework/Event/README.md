# FuFramework Event Module

## 简介
FuFramework Event 模块是一个高性能、线程安全的事件管理系统。它提供了灵活的事件订阅/发布机制，支持多种事件池模式，并集成了对象池技术以提高性能。

## 核心类说明

### EventModule
全局事件管理器，继承自 `FuModule`。
- **职责**：
  1. 管理所有事件的订阅和发布
  2. 提供线程安全的事件处理机制
  3. 支持延迟处理和立即处理两种模式
  4. 集成对象池管理事件参数生命周期

### EventRegister
模块级事件注册器，实现 `IReference` 接口。
- **职责**：
  1. 为特定模块（如UI界面）提供独立的事件管理
  2. 自动管理事件订阅的生命周期
  3. 简化事件订阅和取消订阅操作

### BaseEventArgs / GameEventArgs
事件参数基类，继承自 `EventArgs` 并实现 `IReference` 接口。
- **职责**：
  1. 定义事件的基本结构和行为
  2. 支持对象池重用机制
  3. 提供事件ID标识功能

### EmptyEventArgs
轻量级空事件，用于不需要携带数据的事件通信。
- **职责**：
  1. 避免创建不必要的事件参数对象
  2. 通过事件ID进行简单的事件通信

### EventPool<T>
事件处理的核心容器，管理事件的订阅、发布和处理。
- **职责**：
  1. 实现线程安全的事件队列
  2. 支持多种事件池模式配置
  3. 提供延迟事件处理机制

## 事件池模式 (EEventPoolMode)
```csharp
[Flags]
public enum EEventPoolMode : byte
{
    Default = 0,                    // 必须存在有且只有一个事件处理函数
    AllowNoHandler = 1,            // 允许不存在事件处理函数
    AllowMultiHandler = 2,          // 允许存在多个事件处理函数
    AllowDuplicateHandler = 4       // 允许存在重复的事件处理函数
}
```

## 使用指南

### 1. 定义事件
```csharp
// 定义事件ID
public static class EventIds
{
    public const string PlayerDamage = "PlayerDamage";
    public const string PlayerLevelUp = "PlayerLevelUp";
}

// 创建自定义事件参数
public class PlayerDamageEventArgs : GameEventArgs
{
    public override string Id => EventIds.PlayerDamage;
    public int Damage { get; private set; }
    public GameObject Attacker { get; private set; }
    
    public override void Clear()
    {
        Damage = 0;
        Attacker = null;
    }
    
    public static PlayerDamageEventArgs Create(int damage, GameObject attacker)
    {
        var args = ReferencePool.Runtime.ReferencePool.Acquire<PlayerDamageEventArgs>();
        args.Damage = damage;
        args.Attacker = attacker;
        return args;
    }
}
```

### 2. 订阅事件
```csharp
public class PlayerController : MonoBehaviour
{
    private void Start()
    {
        // 订阅自定义事件
        GlobalModule.EventModule.Subscribe(EventIds.PlayerDamage, OnPlayerDamage);
        
        // 订阅空事件
        GlobalModule.EventModule.Subscribe(EventIds.PlayerLevelUp, OnPlayerLevelUp);
    }
    
    private void OnPlayerDamage(object sender, GameEventArgs e)
    {
        if (e is PlayerDamageEventArgs damageArgs)
        {
            Debug.Log($"玩家受到 {damageArgs.Damage} 点伤害");
        }
    }
    
    private void OnPlayerLevelUp(object sender, GameEventArgs e)
    {
        Debug.Log("玩家升级了！");
    }
    
    private void OnDestroy()
    {
        // 取消订阅
        GlobalModule.EventModule.Unsubscribe(EventIds.PlayerDamage, OnPlayerDamage);
        GlobalModule.EventModule.Unsubscribe(EventIds.PlayerLevelUp, OnPlayerLevelUp);
    }
}
```

### 3. 发布事件
```csharp
public class EnemyController : MonoBehaviour
{
    private void AttackPlayer()
    {
        // 发布自定义事件
        var damageArgs = PlayerDamageEventArgs.Create(10, gameObject);
        GlobalModule.EventModule.Broadcast(this, damageArgs);
        
        // 发布空事件
        GlobalModule.EventModule.Broadcast(this, EventIds.PlayerLevelUp);
    }
}
```

### 4. 使用 EventRegister 进行模块级事件管理
```csharp
public class UIModule : MonoBehaviour
{
    private EventRegister m_EventRegister;
    
    private void Start()
    {
        m_EventRegister = EventRegister.Create();
        
        // 使用EventRegister订阅事件
        m_EventRegister.Subscribe(EventIds.PlayerDamage, OnPlayerDamageUI);
        m_EventRegister.Subscribe(EventIds.PlayerLevelUp, OnPlayerLevelUpUI);
    }
    
    private void OnPlayerDamageUI(object sender, GameEventArgs e)
    {
        // 更新UI显示伤害信息
    }
    
    private void OnDestroy()
    {
        // 自动取消所有订阅
        m_EventRegister.UnSubscribeAll();
        ReferencePool.Runtime.ReferencePool.Release(m_EventRegister);
    }
}
```

## 编辑器扩展
`EventModuleInspector` 提供了可视化的事件监控功能：
- **实时统计**：显示已注册的事件处理函数数量和当前帧触发的事件数量
- **详细列表**：展示所有已注册的事件处理函数和当前帧触发的事件
- **调试信息**：显示事件发送者和处理函数的详细信息

## 性能优化建议
1. **使用 EmptyEventArgs**：对于不需要数据的事件，使用空事件避免创建不必要的对象
2. **合理使用 EventRegister**：对于模块级事件管理，使用EventRegister自动管理订阅生命周期
3. **避免频繁创建事件对象**：重用事件参数对象，利用对象池机制
4. **及时取消订阅**：在对象销毁时及时取消事件订阅，避免内存泄漏

## 注意事项
- **事件处理顺序**：事件处理函数的调用顺序与订阅顺序一致
- **异常处理**：事件处理函数中的异常会被捕获并记录，不会影响其他事件处理
- **内存管理**：事件参数对象会自动通过对象池管理，无需手动释放
- **线程安全**：`Broadcast` 方法是线程安全的，`BroadcastNow` 方法是非线程安全的