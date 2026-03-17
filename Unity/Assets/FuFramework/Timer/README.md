# FuFramework Timer Module

## 概述

Timer 模块是 FuFramework 中的定时器管理系统，基于 UniTask 实现，提供多种类型的计时器功能。该模块支持一次性计时器、时间间隔计时器、帧间隔计时器，具备暂停、恢复、停止等完整生命周期管理功能。

### 核心特性

- **多种计时器类型**：支持一次性、时间间隔、帧间隔三种计时器
- **基于 UniTask**：异步友好的计时器实现
- **完整生命周期管理**：启动、暂停、恢复、停止等完整操作
- **时间缩放控制**：支持忽略时间缩放，适用于UI动画等场景
- **抗时间跳跃**：处理卡顿情况，确保计时准确性
- **对象池管理**：使用 ReferencePool 实现对象复用
- **模块化设计**：支持 TimerRegister 进行分组管理

## 系统架构

### 核心类说明

#### 1. TimerModule
计时器管理器，继承自 FuModule，负责所有计时器的统一管理。

#### 2. TimerBase
计时器基类，定义计时器的通用接口和基础功能。

#### 3. NormalTimer
普通一次性计时器，在指定时间后触发完成回调。

#### 4. TimeTimer
时间间隔计时器，按照固定时间间隔重复执行回调。

#### 5. FrameTimer
帧间隔计时器，按照固定帧数间隔重复执行回调。

#### 6. TimerRegister
计时器注册器，用于模块级别的计时器分组管理。

### 技术架构图

```
TimerModule
├── m_TimerDict (计时器字典)
├── m_Lock (异步锁)
└── 核心方法
    ├── StartTimer() 启动一次性计时器
    ├── StartTimeTimer() 启动时间间隔计时器
    ├── StartFrameTimer() 启动帧间隔计时器
    ├── PauseTimer() 暂停计时器
    ├── ResumeTimer() 恢复计时器
    └── StopTimer() 停止计时器
    └── ExecuteTimerAsync() 异步执行计时器

TimerBase (抽象基类)
├── NormalTimer (一次性计时器)
├── TimeTimer (时间间隔计时器)
└── FrameTimer (帧间隔计时器)
```

## 快速开始

### 基本使用

#### 1. 获取计时器管理器

```csharp
using FuFramework.Timer.Runtime;
using UnityEngine;

public class TimerExample : MonoBehaviour
{
    private TimerModule m_TimerModule;

    private void Start()
    {
        // 获取计时器管理器
        m_TimerModule = ModuleManager.GetModule<TimerModule>();
        
        // 使用计时器
        UseNormalTimer();
        UseTimeTimer();
        UseFrameTimer();
    }
}
```

#### 2. 使用一次性计时器

```csharp
private void UseNormalTimer()
{
    // 启动一个3秒后执行的一次性计时器
    int timerId = m_TimerModule.StartTimer(
        duration: 3f,
        finishCallBack: () => Debug.Log("3秒计时器完成！"),
        updateCallBack: () => Debug.Log("计时器更新中..."),
        ignoreTimeScale: false
    );
    
    Debug.Log($"启动计时器，ID: {timerId}");
}
```

#### 3. 使用时间间隔计时器

```csharp
private void UseTimeTimer()
{
    int counter = 0;
    
    // 启动一个每2秒执行一次的时间间隔计时器，重复5次
    int timerId = m_TimerModule.StartTimeTimer(
        interval: 2f,
        intervalCallback: () => 
        {
            counter++;
            Debug.Log($"时间间隔计时器第{counter}次执行");
        },
        repeatCount: 5,
        immediate: true,  // 立即执行第一次回调
        ignoreTimeScale: false
    );
}
```

#### 4. 使用帧间隔计时器

```csharp
private void UseFrameTimer()
{
    int frameCounter = 0;
    
    // 启动一个每30帧执行一次的帧间隔计时器，无限循环
    int timerId = m_TimerModule.StartFrameTimer(
        frameInterval: 30,
        intervalCallback: () => 
        {
            frameCounter++;
            Debug.Log($"帧间隔计时器第{frameCounter}次执行，当前帧率: {1f / Time.deltaTime:F1}");
        },
        repeatCount: -1,  // -1表示无限循环
        immediate: true
    );
}
```

## 详细使用指南

### 计时器生命周期管理

#### 1. 启动计时器

```csharp
// 一次性计时器
int timerId = m_TimerModule.StartTimer(
    duration: 5f,
    finishCallBack: () => Debug.Log("计时器完成"),
    updateCallBack: () => Debug.Log("更新中..."),
    playerLoopTiming: PlayerLoopTiming.Update,
    ignoreTimeScale: true
);

// 时间间隔计时器
int timeTimerId = m_TimerModule.StartTimeTimer(
    interval: 1f,
    intervalCallback: () => Debug.Log("间隔回调"),
    repeatCount: 10,
    immediate: false,
    ignoreTimeScale: false
);

// 帧间隔计时器
int frameTimerId = m_TimerModule.StartFrameTimer(
    frameInterval: 60,
    intervalCallback: () => Debug.Log("帧间隔回调"),
    repeatCount: -1,
    immediate: true,
    playerLoopTiming: PlayerLoopTiming.Update
);
```

#### 2. 暂停和恢复计时器

```csharp
// 暂停单个计时器
m_TimerModule.PauseTimer(timerId);

// 恢复单个计时器
m_TimerModule.ResumeTimer(timerId);

// 暂停所有计时器
m_TimerModule.PauseAllTimers();

// 恢复所有计时器
m_TimerModule.ResumeAllTimers();
```

#### 3. 停止计时器

```csharp
// 停止单个计时器
m_TimerModule.StopTimer(timerId);

// 停止所有计时器
m_TimerModule.StopAllTimers();
```

#### 4. 查询计时器状态

```csharp
// 检查计时器是否存在
bool exists = m_TimerModule.IsTimerExist(timerId);

// 检查计时器是否暂停
bool isPaused = m_TimerModule.IsTimerPaused(timerId);

// 获取所有计时器名称
var timerNames = m_TimerModule.GetAllTimerNames();
foreach (var name in timerNames)
{
    Debug.Log($"计时器: {name}");
}
```

### 高级功能

#### 1. 使用 TimerRegister 进行分组管理

```csharp
using FuFramework.Timer.Runtime;

public class GameManager
{
    private TimerRegister m_GameTimerRegister;
    
    public void Initialize()
    {
        // 创建计时器注册器
        m_GameTimerRegister = TimerRegister.Create();
        
        // 使用注册器启动计时器
        m_GameTimerRegister.StartTimer(10f, () => Debug.Log("游戏倒计时结束"));
        m_GameTimerRegister.StartTimeTimer(5f, () => Debug.Log("每5秒自动保存"));
    }
    
    public void Cleanup()
    {
        // 清理注册器中的所有计时器
        ReferencePool.Runtime.ReferencePool.Release(m_GameTimerRegister);
    }
}
```

#### 2. 进度监控和自定义逻辑

```csharp
public class ProgressTimerExample : MonoBehaviour
{
    private TimerModule m_TimerModule;
    private int m_ProgressTimerId;
    
    private void Start()
    {
        m_TimerModule = ModuleManager.GetModule<TimerModule>();
        StartProgressTimer();
    }
    
    private void StartProgressTimer()
    {
        float totalDuration = 10f;
        float currentProgress = 0f;
        
        m_ProgressTimerId = m_TimerModule.StartTimer(
            duration: totalDuration,
            finishCallBack: () => 
            {
                Debug.Log("进度计时器完成！");
                currentProgress = 1f;
                UpdateProgressUI(currentProgress);
            },
            updateCallBack: () => 
            {
                // 计算当前进度
                currentProgress = GetTimerProgress(m_ProgressTimerId);
                UpdateProgressUI(currentProgress);
            },
            ignoreTimeScale: false
        );
    }
    
    private float GetTimerProgress(int timerId)
    {
        // 实际实现中需要通过反射或其他方式获取计时器进度
        // 这里仅为示例
        return 0.5f;
    }
    
    private void UpdateProgressUI(float progress)
    {
        Debug.Log($"当前进度: {progress:P0}");
    }
}
```

## 实际应用场景

### 1. 游戏倒计时系统

```csharp
public class CountdownSystem
{
    private TimerModule m_TimerModule;
    private int m_CountdownTimerId;
    private int m_RemainingSeconds = 60;
    
    public void StartCountdown(int seconds)
    {
        m_RemainingSeconds = seconds;
        m_TimerModule = ModuleManager.GetModule<TimerModule>();
        
        m_CountdownTimerId = m_TimerModule.StartTimeTimer(
            interval: 1f,
            intervalCallback: UpdateCountdown,
            repeatCount: seconds,
            immediate: true,
            ignoreTimeScale: true
        );
    }
    
    private void UpdateCountdown()
    {
        m_RemainingSeconds--;
        
        // 更新UI显示
        UpdateCountdownUI(m_RemainingSeconds);
        
        if (m_RemainingSeconds <= 0)
        {
            Debug.Log("倒计时结束！");
            OnCountdownFinished();
        }
    }
    
    private void UpdateCountdownUI(int seconds)
    {
        Debug.Log($"剩余时间: {seconds}秒");
    }
    
    private void OnCountdownFinished()
    {
        // 倒计时结束逻辑
    }
}
```

### 2. 技能冷却系统

```csharp
public class SkillCoolDownSystem
{
    private TimerModule m_TimerModule;
    private Dictionary<string, int> m_SkillTimers = new();
    
    public void UseSkill(string skillName, float coolDownTime)
    {
        if (IsSkillCooling(skillName))
        {
            Debug.Log($"技能 {skillName} 正在冷却中");
            return;
        }
        
        // 启动冷却计时器
        int timerId = m_TimerModule.StartTimer(
            duration: coolDownTime,
            finishCallBack: () => OnSkillCoolDownFinished(skillName),
            ignoreTimeScale: false
        );
        
        m_SkillTimers[skillName] = timerId;
        Debug.Log($"使用技能 {skillName}，冷却时间: {coolDownTime}秒");
    }
    
    private bool IsSkillCooling(string skillName)
    {
        return m_SkillTimers.ContainsKey(skillName) && 
               m_TimerModule.IsTimerExist(m_SkillTimers[skillName]);
    }
    
    private void OnSkillCoolDownFinished(string skillName)
    {
        m_SkillTimers.Remove(skillName);
        Debug.Log($"技能 {skillName} 冷却完成");
    }
}
```

### 3. 动画和特效控制

```csharp
public class AnimationController
{
    private TimerModule m_TimerModule;
    
    public void PlayFadeAnimation(GameObject target, float duration)
    {
        // 使用忽略时间缩放的计时器，确保动画流畅
        m_TimerModule.StartTimer(
            duration: duration,
            finishCallBack: () => OnFadeComplete(target),
            updateCallBack: () => UpdateFadeProgress(target, duration),
            ignoreTimeScale: true
        );
    }
    
    private void UpdateFadeProgress(GameObject target, float totalDuration)
    {
        // 更新淡入淡出动画进度
        // 实际实现中需要获取计时器进度
    }
    
    private void OnFadeComplete(GameObject target)
    {
        Debug.Log($"{target.name} 淡入淡出动画完成");
    }
}
```

### 4. 网络心跳包

```csharp
public class NetworkHeartbeat
{
    private TimerModule m_TimerModule;
    private int m_HeartbeatTimerId;
    
    public void StartHeartbeat()
    {
        m_TimerModule = ModuleManager.GetModule<TimerModule>();
        
        // 每30秒发送一次心跳包
        m_HeartbeatTimerId = m_TimerModule.StartTimeTimer(
            interval: 30f,
            intervalCallback: SendHeartbeat,
            repeatCount: -1,  // 无限循环
            immediate: false,
            ignoreTimeScale: true  // 网络通信不受时间缩放影响
        );
    }
    
    private void SendHeartbeat()
    {
        Debug.Log("发送网络心跳包...");
        // 实际网络通信逻辑
    }
    
    public void StopHeartbeat()
    {
        m_TimerModule.StopTimer(m_HeartbeatTimerId);
    }
}
```

## 性能优化建议

### 1. 合理选择计时器类型

```csharp
// 时间精度要求高的场景使用时间间隔计时器
m_TimerModule.StartTimeTimer(0.1f, HighPrecisionUpdate);

// 与渲染帧同步的场景使用帧间隔计时器  
m_TimerModule.StartFrameTimer(2, FrameSyncUpdate);

// 简单的延时操作使用一次性计时器
m_TimerModule.StartTimer(3f, SimpleDelayCallback);
```

### 2. 避免频繁创建销毁计时器

```csharp
public class OptimizedTimerUsage
{
    private TimerModule m_TimerModule;
    private int m_ReusableTimerId;
    
    public void StartReusableTimer()
    {
        // 重用计时器ID，避免频繁创建
        if (m_TimerModule.IsTimerExist(m_ReusableTimerId))
        {
            m_TimerModule.StopTimer(m_ReusableTimerId);
        }
        
        m_ReusableTimerId = m_TimerModule.StartTimer(5f, ReusableCallback);
    }
    
    private void ReusableCallback()
    {
        Debug.Log("可重用计时器完成");
    }
}
```

### 3. 使用 TimerRegister 进行批量管理

```csharp
public class LevelTimerManager
{
    private TimerRegister m_LevelTimerRegister;
    
    public void StartLevelTimers()
    {
        m_LevelTimerRegister = TimerRegister.Create();
        
        // 关卡相关的所有计时器使用同一个注册器
        m_LevelTimerRegister.StartTimer(60f, OnLevelTimeUp);
        m_LevelTimerRegister.StartTimeTimer(10f, SpawnEnemyWave);
        m_LevelTimerRegister.StartFrameTimer(30, UpdateMiniMap);
    }
    
    public void CleanupLevelTimers()
    {
        // 一键清理所有关卡计时器
        ReferencePool.Runtime.ReferencePool.Release(m_LevelTimerRegister);
    }
}
```

## API 参考

### TimerModule 主要方法

| 方法 | 描述 | 参数 | 返回值 |
|------|------|------|--------|
| `StartTimer()` | 启动一次性计时器 | duration: 持续时间, finishCallBack: 完成回调, updateCallBack: 更新回调, playerLoopTiming: 更新时间点, ignoreTimeScale: 是否忽略时间缩放 | int (计时器ID) |
| `StartTimeTimer()` | 启动时间间隔计时器 | interval: 间隔时间, intervalCallback: 间隔回调, repeatCount: 重复次数, immediate: 是否立即执行, ignoreTimeScale: 是否忽略时间缩放 | int (计时器ID) |
| `StartFrameTimer()` | 启动帧间隔计时器 | frameInterval: 帧间隔, intervalCallback: 间隔回调, repeatCount: 重复次数, immediate: 是否立即执行, playerLoopTiming: 更新时间点 | int (计时器ID) |
| `PauseTimer()` | 暂停计时器 | timerId: 计时器ID | void |
| `ResumeTimer()` | 恢复计时器 | timerId: 计时器ID | void |
| `StopTimer()` | 停止计时器 | timerId: 计时器ID | void |
| `PauseAllTimers()` | 暂停所有计时器 | - | void |
| `ResumeAllTimers()` | 恢复所有计时器 | - | void |
| `StopAllTimers()` | 停止所有计时器 | - | void |
| `IsTimerExist()` | 检查计时器是否存在 | timerId: 计时器ID | bool |
| `IsTimerPaused()` | 检查计时器是否暂停 | timerId: 计时器ID | bool |
| `GetAllTimerNames()` | 获取所有计时器名称 | - | IEnumerable<string> |

### 属性

| 属性 | 类型 | 描述 |
|------|------|------|
| `Count` | int | 当前计时器数量 |

### TimerRegister 主要方法

| 方法 | 描述 | 参数 |
|------|------|------|
| `StartTimer()` | 启动一次性计时器 | 同 TimerModule.StartTimer() |
| `StartTimeTimer()` | 启动时间间隔计时器 | 同 TimerModule.StartTimeTimer() |
| `StartFrameTimer()` | 启动帧间隔计时器 | 同 TimerModule.StartFrameTimer() |
| `PauseTimer()` | 暂停计时器 | timerId: 计时器ID |
| `ResumeTimer()` | 恢复计时器 | timerId: 计时器ID |
| `StopTimer()` | 停止计时器 | timerId: 计时器ID |

## 注意事项

### 1. 内存管理
- 使用 `ReferencePool` 管理计时器对象生命周期
- 及时停止不再需要的计时器
- 使用 `TimerRegister` 进行分组管理，便于批量清理

### 2. 性能考虑
- 避免在每帧更新中创建大量计时器
- 合理选择计时器类型和参数
- 使用忽略时间缩放时注意性能影响

### 3. 线程安全
- TimerModule 设计为单线程使用
- 在多线程环境中需要额外的同步机制
- 避免并发操作计时器字典

### 4. 异常处理
- 计时器回调中应包含异常处理
- 使用 try-catch 包装关键逻辑
- 确保计时器异常不会影响系统稳定性

## 常见问题解答

### Q: 如何选择合适的计时器类型？
A: 根据需求选择：
- **一次性计时器**：简单的延时操作，如技能冷却、动画延时
- **时间间隔计时器**：精确的时间控制，如倒计时、定期保存
- **帧间隔计时器**：与渲染帧同步，如动画控制、物理模拟

### Q: 忽略时间缩放有什么作用？
A: 忽略时间缩放可以确保计时器不受 `Time.timeScale` 影响，适用于：
- UI动画和特效
- 网络通信
- 后台逻辑处理
- 需要稳定时间基准的场景

### Q: 如何处理卡顿导致的计时器跳跃？
A: 计时器系统内置了抗时间跳跃机制：
- 时间间隔计时器使用累计时间机制
- 帧间隔计时器使用累计帧数机制
- 限制最大 deltaTime 防止极端情况

### Q: 计时器回调中能否进行耗时操作？
A: 不建议在计时器回调中进行耗时操作，因为：
- 可能影响其他计时器的执行
- 可能导致帧率下降
- 建议将耗时操作放到其他线程或协程中处理

### Q: 如何实现计时器的进度查询？
A: 目前系统未直接提供进度查询接口，但可以通过以下方式实现：
- 在更新回调中自行计算进度
- 使用外部变量记录开始时间和当前时间
- 结合 Unity 的 Time 类进行计算