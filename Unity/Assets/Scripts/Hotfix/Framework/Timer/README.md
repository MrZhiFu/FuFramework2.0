# FuFramework Timer Module

## 1. 概述

Timer 模块是 FuFramework 中的定时器管理系统，采用统一帧驱动（OnUpdate 每帧一次遍历）实现，提供多种类型的计时器功能。该模块支持一次性倒计时计时器、时间间隔计时器、帧间隔计时器，具备暂停、恢复、停止等完整生命周期管理功能。

### 1.1 核心特性

- **多种计时器类型**：支持倒计时、时间间隔、帧间隔三种计时器
- **统一帧驱动**：OnUpdate 每帧一次遍历驱动所有计时器，无每计时器独立异步链开销
- **完整生命周期管理**：启动、暂停、恢复、停止等完整操作
- **时间缩放控制**：支持忽略时间缩放，适用于UI动画等场景
- **抗时间跳跃**：处理卡顿情况，确保计时准确性
- **异常隔离**：单计时器回调异常只终结它自己，不影响其余计时器
- **对象池管理**：使用 ReferencePool 实现对象复用
- **模块化设计**：支持 TimerRegister 进行分组管理

## 2. 系统架构

### 2.1 类继承体系

```
ModuleBase (抽象基类)
    ↑
TimerModule (计时器管理模块，override OnUpdate 统一帧驱动)
    ├── m_timerDict (计时器字典)
    └── m_updateSnapshot (帧驱动复用快照缓冲)

TimerBase (抽象基类)
    ↑
    ├── CountdownTimer (倒计时计时器)
    ├── IntervalTimer (时间间隔计时器)
    └── FrameTimer (帧间隔计时器)

TimerRegister (计时器注册器)
    └── IReference (引用池接口)
```

### 2.2 技术架构

```
┌─────────────────────────────────────────────────────────────┐
│                     TimerModule                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  m_timerDict (Dictionary<int, TimerBase>)           │   │
│  │  - 存储所有活跃计时器                                │   │
│  └─────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  OnUpdate() 统一帧驱动                               │   │
│  │  - 每帧一次遍历（快照缓冲）驱动所有计时器             │   │
│  │  - 暂停跳过（标志位）                                 │   │
│  │  - 有效性校验 + 单计时器异常隔离                      │   │
│  │  - 抗时间跳跃处理（dt 钳制）                          │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              ↓
                    TimerBase.OnUpdate()
                              ↓
        ┌─────────────────────┼─────────────────────┐
        ↓                     ↓                     ↓
┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│CountdownTimer│    │IntervalTimer │    │ FrameTimer   │
│- 倒计时逻辑  │    │- 间隔回调    │    │- 帧间隔回调  │
│- 进度计算    │    │- 累计时间    │    │- 累计帧数    │
└──────────────┘    └──────────────┘    └──────────────┘
```

## 3. 核心类详解

### 3.1 TimerModule

计时器管理模块，继承自 ModuleBase，负责所有计时器的统一管理。

**核心字段：**

| 字段 | 类型 | 说明 |
|------|------|------|
| m_timerDict | Dictionary<int, TimerBase> | 计时器字典，Key为计时器ID |
| m_updateSnapshot | List<TimerBase> | 帧驱动遍历用的复用快照缓冲 |
| m_nextTimerId | int | 下一个计时器ID（自增） |

**核心属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| Count | int | 当前计时器数量 |
| OnTimerFinished | Action<int> | 计时器完成/停止时触发的事件 |

**核心方法：**

```csharp
// 启动倒计时计时器
public int StartCountdownTimer(float duration, Action finishCallBack, 
    Action updateCallBack = null, bool ignoreTimeScale = false)

// 启动时间间隔计时器
public int StartIntervalTimer(float interval, Action intervalCallback, 
    int repeatCount = -1, bool immediate = false, bool ignoreTimeScale = false)

// 启动帧间隔计时器
public int StartFrameTimer(int frameInterval, Action intervalCallback, 
    int repeatCount = -1, bool immediate = false)

// 暂停/恢复/停止计时器
public void PauseTimer(int timerId)
public void ResumeTimer(int timerId)
public void StopTimer(int timerId)

// 重置计时器进度（仅清零进度，不重发 immediate，不改暂停状态）
public void ResetTimer(int timerId)

// 批量操作
public void PauseAllTimers()
public void ResumeAllTimers()
public void StopAllTimers()
public void ResetAllTimers()

// 查询状态
public bool IsTimerExist(int timerId)
public bool IsTimerPaused(int timerId)

// 观察者快照（调试面板/运维工具用，不暴露计时器实例）
public TimerInfo[] GetAllTimerInfos()
public TimerModuleStatistics GetStatistics()
```

**执行机制：**

1. **OnUpdate 统一帧驱动**：由 GameDriven.Update → ModuleManager.Update 每帧调用
   - 每帧一次遍历（复用快照缓冲防回调中修改容器）驱动所有计时器，无每计时器独立异步链
   - 暂停状态直接跳过 tick（标志位），恢复后天然无暂停时间泄漏
   - 有效性校验（Id 仍映射到本实例）防快照悬空；单计时器回调异常隔离回收
   - 限制最大 deltaTime（0.1s）防止时间跳跃
   - 自动清理完成/异常的计时器并派发 OnTimerFinished

### 3.2 TimerBase

计时器基类，定义计时器的通用接口和基础功能。

**核心属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| Id | int | 计时器ID |
| IgnoreTimeScale | bool | 是否忽略时间缩放 |
| IsPaused | bool | 是否暂停 |
| Immediate | bool | 是否需要执行首帧「立即」回调 |
| IsCompleted | bool | 是否已完成（抽象属性） |

**核心方法：**

```csharp
// 清理计时器（实现 IReference 接口）
public virtual void Clear()

// 更新计时器（抽象方法）
public abstract void OnUpdate(float deltaTime, int deltaFrames)

// 当计时器完成时调用（虚方法）
public virtual void OnComplete()

// 重置计时器进度（虚方法，基类无进度为空实现，子类按各自口径覆写）
public virtual void Reset()
```

### 3.3 CountdownTimer

倒计时计时器，在指定时间后触发完成回调。

**核心属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| DurationTime | float | 总持续时间（秒） |
| RemainingTime | float | 剩余时间（秒） |
| FinishCallBack | Action | 计时器结束时触发的回调 |
| UpdateCallBack | Action | 每帧更新时触发的回调 |
| Progress | float | 当前进度（0-1的归一化值） |

**核心方法：**

```csharp
// 更新计时器
public override void OnUpdate(float deltaTime, int _)

// 当计时器完成时调用
public override void OnComplete()

// 创建倒计时计时器（工厂方法）
public static CountdownTimer Create(int timerId, float duration, Action finishCallBack, 
    Action updateCallBack, bool ignoreTimeScale)
```

**使用示例：**

```csharp
// 启动一个3秒后执行的倒计时计时器
int timerId = timerModule.StartCountdownTimer(
    duration: 3f,
    finishCallBack: () => Debug.Log("倒计时完成！"),
    updateCallBack: () => Debug.Log("更新中..."),
    ignoreTimeScale: false
);
```

### 3.4 IntervalTimer

时间间隔计时器，按照固定时间间隔重复执行回调。

**核心属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| Interval | float | 执行间隔时间（秒） |
| IntervalCallback | Action | 间隔到达时触发的回调 |
| MaxCount | int | 最大执行次数（-1表示无限循环） |
| ExecutedCount | int | 已执行次数 |
| AccumulatedTime | float | 累计时间（抗时间跳跃） |

**核心方法：**

```csharp
// 更新计时器（使用累计时间机制处理卡顿）
public override void OnUpdate(float deltaTime, int _)

// 创建时间间隔计时器（工厂方法）
public static IntervalTimer Create(int timerId, float interval, Action intervalCallback, 
    int repeatCount, bool immediate, bool ignoreTimeScale)
```

**抗时间跳跃机制：**

```csharp
// 累计时间增量
AccumulatedTime += deltaTime;

// 使用while循环确保在卡顿情况下也能正确执行所有遗漏的回调
while (AccumulatedTime >= Interval && ExecutedCount < MaxCount)
{
    AccumulatedTime -= Interval;
    ExecutedCount++;
    IntervalCallback?.Invoke();
}
```

### 3.5 FrameTimer

帧间隔计时器，按照固定帧数间隔重复执行回调。

**核心属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| FrameInterval | int | 执行间隔帧数 |
| IntervalCallback | Action | 间隔到达时触发的回调 |
| MaxCount | int | 最大执行次数（-1表示无限循环） |
| ExecutedCount | int | 已执行次数 |
| AccumulatedFrames | int | 累计帧数（抗帧率波动） |

**核心方法：**

```csharp
// 更新计时器（使用累计帧数机制）
public override void OnUpdate(float _, int deltaFrames)

// 创建帧间隔计时器（工厂方法）
public static FrameTimer Create(int timerId, int frameInterval, Action intervalCallback, 
    int repeatCount, bool immediate)
```

**注意：** 帧间隔计时器始终忽略时间缩放，确保与帧率同步。

### 3.6 TimerRegister

计时器注册器，用于模块级别的计时器分组管理。

**核心字段：**

| 字段 | 类型 | 说明 |
|------|------|------|
| m_timerModule | TimerModule | 计时器管理模块（实例字段） |
| m_timerList | HashSet<int> | 记录所有计时器ID的集合（O(1) 增删查） |

**核心方法：**

```csharp
// 创建计时器注册器（工厂方法）
public static TimerRegister Create()

// 启动计时器（与 TimerModule 接口相同；返回计时器 Id 供按 id 操作使用，失败返回 -1；
// 不需要按 id 操作时可忽略返回值，批量清理不受影响）
public int StartCountdownTimer(float duration, Action finishCallBack = null, ...)
public int StartIntervalTimer(float interval, Action intervalCallback, ...)
public int StartFrameTimer(int frameInterval, Action intervalCallback, ...)

// 暂停/恢复/停止计时器
public void PauseTimer(int timerId)
public void ResumeTimer(int timerId)
public void StopTimer(int timerId)

// 重置计时器进度（仅清零进度，不重发 immediate，不改暂停状态）
public void ResetTimer(int timerId)

// 批量操作
public void PauseAllTimers()
public void ResumeAllTimers()
public void StopAllTimers()
public void ResetAllTimers()

// 清理和释放
public void Clear()
public void Release()
```

**使用示例：**

```csharp
// 创建计时器注册器
var timerRegister = TimerRegister.Create();

// 启动计时器
timerRegister.StartCountdownTimer(10f, () => Debug.Log("倒计时结束"));
timerRegister.StartIntervalTimer(5f, () => Debug.Log("每5秒执行"));

// 清理所有计时器
timerRegister.Release();
```

## 4. 使用示例

### 4.1 基本使用流程

```csharp
using Hotfix.Framework.Timer;
using UnityEngine;

public class TimerExample : MonoBehaviour
{
    private TimerModule m_timerModule;

    private void Start()
    {
        // 获取计时器管理器
        m_timerModule = ModuleManager.GetModule<TimerModule>();
        
        // 启动倒计时计时器
        int countdownId = m_timerModule.StartCountdownTimer(
            duration: 3f,
            finishCallBack: () => Debug.Log("倒计时完成！"),
            ignoreTimeScale: false
        );
        
        // 启动时间间隔计时器
        int intervalId = m_timerModule.StartIntervalTimer(
            interval: 1f,
            intervalCallback: () => Debug.Log("每秒执行一次"),
            repeatCount: 5,
            immediate: true
        );
        
        // 启动帧间隔计时器
        int frameId = m_timerModule.StartFrameTimer(
            frameInterval: 30,
            intervalCallback: () => Debug.Log("每30帧执行一次"),
            repeatCount: -1  // 无限循环
        );
    }
}
```

### 4.2 计时器生命周期管理

```csharp
// 暂停计时器
m_timerModule.PauseTimer(timerId);

// 恢复计时器
m_timerModule.ResumeTimer(timerId);

// 停止计时器
m_timerModule.StopTimer(timerId);

// 批量操作
m_timerModule.PauseAllTimers();
m_timerModule.ResumeAllTimers();
m_timerModule.StopAllTimers();
```

### 4.3 使用 TimerRegister 进行分组管理

```csharp
public class GameManager
{
    private TimerRegister m_gameTimerRegister;
    
    public void Initialize()
    {
        // 创建计时器注册器
        m_gameTimerRegister = TimerRegister.Create();
        
        // 使用注册器启动计时器
        m_gameTimerRegister.StartCountdownTimer(10f, () => Debug.Log("游戏倒计时结束"));
        m_gameTimerRegister.StartIntervalTimer(5f, () => Debug.Log("每5秒自动保存"));
    }
    
    public void Cleanup()
    {
        // 清理注册器中的所有计时器
        m_gameTimerRegister.Release();
    }
}
```

### 4.4 进度监控示例

```csharp
public void StartProgressTimer()
{
    float totalDuration = 10f;
    float elapsedTime = 0f;
    
    int timerId = m_timerModule.StartCountdownTimer(
        duration: totalDuration,
        finishCallBack: () => 
        {
            Debug.Log("进度计时器完成！");
            UpdateProgressUI(1f);
        },
        updateCallBack: () => 
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / totalDuration;
            UpdateProgressUI(progress);
        },
        ignoreTimeScale: false
    );
}

private void UpdateProgressUI(float progress)
{
    Debug.Log($"当前进度: {progress:P0}");
}
```

## 5. 目录结构

```
Timer/
├── 
│   ├── TimerModule.cs              # 计时器管理模块主类（帧驱动与操作）
│   ├── TimerRegister.cs            # 计时器注册器
│   ├── Base/
│   │   └── TimerBase.cs            # 计时器基类
│   ├── Sub/
│   │   ├── CountdownTimer.cs       # 倒计时计时器
│   │   ├── IntervalTimer.cs        # 时间间隔计时器
│   │   └── FrameTimer.cs           # 帧间隔计时器
│   ├── Debug/                      # Editor 专属（整文件 #if UNITY_EDITOR，发布构建不存在）
│   │   ├── TimerModule.Debug.cs    # 调试分部类（信息快照/生命周期统计/启动堆栈）
│   │   └── TimerInfo.cs            # 信息与统计快照结构体、类型枚举
└── README.md                       # 本文档
```

## 6. 依赖模块

- **Core**: 提供 ModuleBase 基类、日志工具
- **ReferencePool**: 提供对象池管理，用于计时器对象的复用
- **UniTask**: 提供异步任务支持

## 7. 设计特点

### 7.1 统一帧驱动

TimerModule override OnUpdate，每帧一次遍历驱动所有计时器，无每计时器独立异步链与 CancellationTokenSource 分配；
单计时器回调异常隔离回收，不影响其余计时器。

### 7.2 抗时间跳跃

- **时间间隔计时器**：使用累计时间机制，在卡顿后连续执行遗漏的回调
- **帧间隔计时器**：使用累计帧数机制，确保帧率波动时的执行准确性
- **最大 deltaTime 限制**：限制最大时间增量为 0.1 秒，防止极端情况

### 7.3 暂停机制

暂停计时器在帧驱动遍历中被标志位跳过（不累计时间），恢复后继续驱动，天然无暂停时间泄漏。

### 7.4 对象池管理

所有计时器类实现 `IReference` 接口，通过 ReferencePool 管理对象生命周期，减少 GC 压力。

### 7.5 分组管理

TimerRegister 提供模块级别的计时器分组管理，便于批量控制和生命周期管理。

## 8. 应用场景

1. **技能冷却**：使用倒计时计时器实现技能冷却时间
2. **倒计时系统**：使用间隔计时器实现每秒更新的倒计时
3. **动画控制**：使用帧间隔计时器实现与帧率同步的动画
4. **自动保存**：使用间隔计时器实现定期自动保存
5. **网络心跳**：使用间隔计时器实现定时心跳包发送
6. **UI动画**：使用忽略时间缩放的计时器实现流畅的UI动画

## 9. 注意事项

1. **线程安全**：TimerModule 设计为单线程使用，所有操作应在主线程执行
2. **回调耗时**：避免在计时器回调中执行耗时操作，可能影响其他计时器
3. **对象释放**：使用 TimerRegister 时，调用 Release() 方法而非直接归还引用池
4. **时间缩放**：忽略时间缩放的计时器使用 `Time.unscaledTime` 和 `Time.unscaledDeltaTime`
5. **停止即回收**：StopTimer/StopAllTimers/OnDispose 直接从字典移除并回收计时器实例（无在途异步链需要取消），并同步派发 OnTimerFinished
6. **调试面板**：Editor 菜单 FuFramework/调试/计时器调试面板（仅 Play 模式可用），支持查看全部计时器状态与进度、暂停/恢复/重置/停止（单个与批量）、生命周期统计与启动来源堆栈（启动时自动记录，用于定位计时器来源；Editor 专用，运行时代码零负担）
