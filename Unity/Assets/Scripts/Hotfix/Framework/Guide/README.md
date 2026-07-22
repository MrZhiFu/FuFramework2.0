# FuFramework Guide Module

## 1. 简介

FuFramework Guide 模块是游戏框架的新手引导系统，采用配置表驱动的步骤式引导流程。该模块支持多种引导步骤类型（UI 点击、对话框、延时等待等），通过 FairyGUI 实现遮罩和高亮效果，并通过 PlayerPrefs 持久化已完成引导的状态以避免重复触发。

## 2. 核心特性

- **配置表驱动**：引导流程和步骤由 Luban 配置表定义，策划可独立配置
- **步骤式执行**：引导按预设步骤顺序执行，每步完成后自动推进
- **多种步骤类型**：支持 UI 点击引导（`ClickUIStep`）、对话框引导（`DialogStep`）、延时等待（`WaitStep`）、立即完成（`DefaultStep`）等步骤
- **引导缓存**：已完成的引导通过 `PlayerPrefs` 持久化，避免重复触发
- **步骤历史**：支持返回上一步（`GoToPreviousStep`），通过栈维护步骤历史
- **灵活控制**：支持跳转步骤、跳过当前步骤、中断引导、强制推进等操作
- **事件通知**：提供引导开始、完成、步骤变更等事件回调

## 3. 核心概念

### 3.1 引导架构

```
┌─────────────────────────────────────────────────────────────┐
│                     GuideModule                              │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  m_GuideDict (Dictionary<int, GuideData>)           │   │
│  │  - 引导数据字典                                     │   │
│  ├─────────────────────────────────────────────────────┤   │
│  │  m_AllStepDict (Dictionary<int, BaseStep>)          │   │
│  │  - 当前引导的所有步骤                                │   │
│  ├─────────────────────────────────────────────────────┤   │
│  │  m_StepHistoryStack (Stack<BaseStep>)               │   │
│  │  - 步骤历史记录栈                                   │   │
│  ├─────────────────────────────────────────────────────┤   │
│  │  m_GuideCompletionCacheDict (Dictionary<int, bool>) │   │
│  │  - 已完成的引导缓存                                  │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
                    ┌──────────────────┐
                    │   IGuideAction   │
                    │  (引导动作接口)   │
                    │  - 全局遮罩控制   │
                    │  - 对话框引导     │
                    │  - UI 点击引导    │
                    └──────────────────┘
```

### 3.2 步骤状态

```
Idle → Executing → Completed / Cancelled / Failed
```

## 4. 核心类说明

### 4.1 GuideModule

引导管理模块，继承自 `ModuleBase`。

**静态属性：**

```csharp
GuideModule Instance { get; }    // 模块单例
```

**公开属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| `IsGuiding` | `bool` | 是否正在引导中 |
| `CurrentGuideId` | `int?` | 当前引导 ID |
| `CurrentStepId` | `int?` | 当前步骤 ID |
| `CurrentGuide` | `GuideData` | 当前引导配置 |
| `CurrentStep` | `BaseStep` | 当前步骤 |
| `GuideAction` | `IGuideAction` | 引导动作执行接口（get/set） |

**公开方法：**

```csharp
// 启动引导
bool StartGuide(int guideId, bool forceRestart = false)
bool StartFirstGuide(bool forceRestart = false)

// 步骤控制
void CompleteCurrentStep()        // 完成当前步骤并进入下一步
void SkipCurrentStep()            // 跳过当前步骤（需步骤配置 CanJump）
void GoToPreviousStep()           // 返回上一步
bool JumpToStep(int stepId)       // 跳转到指定步骤
void ForceNextStep()              // 强制进入下一步，跳过条件检查

// 引导控制
void InterruptGuide(bool markAsCompleted = false)  // 中断引导

// 完成状态管理
bool IsGuideCompleted(int guideId)              // 检查引导是否已完成
void MarkGuideAsCompleted(int guideId)           // 标记引导为已完成
void ResetGuide(int guideId)                     // 重置引导状态

// 步骤查询
BaseStep GetStep(int stepId)                     // 获取步骤实例
Dictionary<int, BaseStep> GetAllSteps()          // 获取所有步骤
GuideData GetCurrentGuideInfo()                  // 获取当前引导信息
```

**事件：**

```csharp
event Action<int> OnGuideStarted           // 引导开始事件（参数：guideId）
event Action<int> OnGuideFinished          // 引导完成事件（参数：guideId）
event Action<int, int> OnStepChanged       // 步骤改变事件（参数：guideId, stepId）
event Action<int, bool> OnGuideInterrupted // 引导中断事件（参数：guideId, markAsCompleted）
event Action<BaseStep> OnStepExecuting     // 步骤开始执行事件
event Action<BaseStep> OnStepCompleted     // 步骤完成事件
```

### 4.2 EStepState / BaseStep

**步骤状态枚举：**

```csharp
public enum EStepState
{
    Idle,        // 空闲
    Executing,   // 执行中
    Completed,   // 已完成
    Cancelled,   // 被取消
    Failed       // 执行失败
}
```

**BaseStep 引导步骤基类**（实现 `IReference`）：

| 属性 | 类型 | 说明 |
|------|------|------|
| `StepInfo` | `GuideStep` | 步骤配置数据 |
| `ExecutionTime` | `float` | 步骤已执行时间 |
| `StartTime` | `float` | 步骤开始时间 |
| `State` | `EStepState` | 步骤状态 |
| `IsExecuting` | `bool` | 是否正在执行中 |
| `IsCompleted` | `bool` | 是否已完成 |
| `GuideAction` | `IGuideAction` | 引导动作接口 |

**公开方法：**

```csharp
void Execute()                // 执行步骤（将状态设为 Executing，调用 OnExecute）
void Update(float deltaTime)  // 步骤帧更新
void Complete()               // 完成步骤（将状态设为 Completed，调用 OnComplete）
void Cancel()                 // 取消步骤（将状态设为 Cancelled，调用 OnCancel）
```

**可重写的虚方法：**

```csharp
protected virtual void OnExecute()             // 步骤开始执行时调用
protected virtual void OnUpdate(float deltaTime)  // 每帧调用
protected virtual void OnComplete()            // 步骤完成时调用
protected virtual void OnCancel()              // 步骤取消时调用
public virtual bool CanExecute()               // 检查是否可以执行
public virtual bool CanComplete()              // 检查是否可以完成
public virtual void Clear()                    // 清理（引用池回收）
```

### 4.3 步骤类型

| 步骤类 | 说明 |
|------|------|
| `ClickUIStep` | UI 点击引导：查找目标 UI 组件并监听点击事件，通过 `IGuideAction.DoClickUIGuide` 显示高亮 |
| `DialogStep` | 对话框引导：通过 `IGuideAction.DoDialogGuide` 显示对话内容，确认后完成步骤 |
| `WaitStep` | 延时等待：等待 `StepInfo.WaitTime` 秒后自动完成，执行期间调用 `ShowGlobalMask` 遮挡 |
| `DefaultStep` | 默认步骤：立即完成（`CanComplete` 始终返回 true） |

所有步骤类均通过静态 `Create(GuideStep stepInfo)` 工厂方法从引用池中获取。

### 4.4 IGuideAction

引导动作执行接口，由热更代码中具体的实现类实现。

```csharp
public interface IGuideAction
{
    // 点击 UI 引导
    void DoClickUIGuide(GComponent targetUI);    // 显示点击 UI 引导高亮
    void EndClickUIGuide();                       // 结束点击 UI 引导

    // 对话引导
    void DoDialogGuide(string content, Action onConfirm = null);  // 显示对话引导
    void EndDialogGuide();                                         // 结束对话引导

    // 全局遮罩
    void ShowGlobalMask();                        // 显示全局遮罩窗口
    void HideGlobalMask();                        // 隐藏全局遮罩窗口
}
```

### 4.5 GuideActionImpl

`IGuideAction` 的默认实现类，通过 FairyGUI 窗口实现遮罩、对话框和 UI 高亮效果。内部使用 `GlobalModule.UIModule.OpenUIAsync<T>` 异步打开引导相关 UI 窗口（`WinClickGuide`、`WinDialogGuide`、`WinGlobalMask`）。

## 5. 使用示例

### 5.1 启动引导

```csharp
using Hotfix.Framework.Core;
using Hotfix.Framework.Guide;

public class GuideExample
{
    private GuideModule m_GuideModule;

    public void Init()
    {
        m_GuideModule = GuideModule.Instance;
    }

    // 在需要触发引导的地方调用
    public void TriggerGuide(int guideId)
    {
        // 检查引导是否已完成（自动检查 PlayerPrefs 缓存）
        if (m_GuideModule.IsGuideCompleted(guideId))
        {
            Debug.Log("引导已执行过，跳过");
            return;
        }

        // 检查是否有引导正在进行
        if (m_GuideModule.IsGuiding)
        {
            Debug.Log("已有引导在进行中");
            return;
        }

        // 启动引导（forceRestart = false 时会跳过已完成的引导）
        var success = m_GuideModule.StartGuide(guideId);
        if (success)
            Debug.Log($"引导 {guideId} 启动成功");
    }

    // 强制重新开始引导（忽略已完成的缓存）
    public void ForceRestartGuide(int guideId)
    {
        m_GuideModule.StartGuide(guideId, forceRestart: true);
    }

    // 开始第一个引导
    public void StartFirstGuide()
    {
        m_GuideModule.StartFirstGuide();
    }
}
```

### 5.2 步骤控制

```csharp
// 完成当前步骤（自动推入下一步）
m_GuideModule.CompleteCurrentStep();

// 跳过当前步骤（仅当步骤配置了 CanJump 时生效）
m_GuideModule.SkipCurrentStep();

// 返回到上一步
m_GuideModule.GoToPreviousStep();

// 跳转到指定步骤
m_GuideModule.JumpToStep(1002);

// 强制进入下一步（跳过条件检查）
m_GuideModule.ForceNextStep();
```

### 5.3 引导控制

```csharp
// 中断引导（不标记完成）
m_GuideModule.InterruptGuide();

// 中断引导并标记为已完成
m_GuideModule.InterruptGuide(markAsCompleted: true);
```

### 5.4 引导状态管理

```csharp
// 检查引导是否已完成
bool isCompleted = m_GuideModule.IsGuideCompleted(guideId);

// 手动标记引导为已完成
m_GuideModule.MarkGuideAsCompleted(guideId);

// 重置引导状态（用于测试或重新触发）
m_GuideModule.ResetGuide(guideId);

// 获取当前引导信息
var guideInfo = m_GuideModule.GetCurrentGuideInfo();
if (guideInfo != null)
    Debug.Log($"当前引导: {guideInfo.Name} ({guideInfo.Id})");

// 获取指定步骤实例
var step = m_GuideModule.GetStep(1001);
if (step != null)
    Debug.Log($"步骤状态: {step.State}");
```

### 5.5 监听引导事件

```csharp
m_GuideModule.OnGuideStarted += (guideId) =>
{
    Debug.Log($"引导开始: {guideId}");
};

m_GuideModule.OnGuideFinished += (guideId) =>
{
    Debug.Log($"引导完成: {guideId}");
};

m_GuideModule.OnStepChanged += (guideId, stepId) =>
{
    Debug.Log($"步骤变更: Guide={guideId}, Step={stepId}");
};

m_GuideModule.OnStepCompleted += (step) =>
{
    Debug.Log($"步骤完成: {step.StepInfo.Id}");
};

m_GuideModule.OnGuideInterrupted += (guideId, markAsCompleted) =>
{
    Debug.Log($"引导中断: {guideId}, 标记完成={markAsCompleted}");
};
```

## 6. 目录结构

```text
Guide/
├── GuideModule.cs           # 引导管理模块
├── BaseStep.cs              # 引导步骤基类 + EStepState 枚举
├── ClickUIStep.cs           # UI 点击引导步骤
├── DefaultStep.cs           # 默认引导步骤（立即完成）
├── DialogStep.cs            # 对话引导步骤
├── WaitStep.cs              # 等待步骤（定时器）
├── IGuideAction.cs          # 引导动作执行接口
├── GuideActionImpl.cs       # 引导动作实现
└── README.md                # 本文档
```

## 7. 依赖

- **Hotfix.Framework.Core**：提供 ModuleBase 基类
- **Hotfix.Framework.Config**：配置表系统（TbGuide、TbGuideStep）
- **Hotfix.Framework.ReferencePools**：引用池
- **Hotfix.Framework.UI**：FairyGUI UI 管理
- **FairyGUI**：引导 UI 渲染
- **UniTask**：异步操作支持

## 8. 最佳实践

1. **引导 ID 管理**：在 Luban 配置表中统一定义引导 ID，避免硬编码
2. **步骤粒度**：每个步骤应完成一个单一的引导目标，保持步骤简洁
3. **设置 GuideAction**：在启动引导前需先设置 `GuideModule.Instance.GuideAction`，否则步骤的引导 UI 不会显示
4. **容错处理**：`ClickUIStep` 会在找不到目标 UI 时输出警告但不阻塞流程
5. **跳过支持**：在配置表中为可跳过的步骤设置 `CanJump = true`
6. **事件驱动**：通过事件监听引导流程变化，实现 UI 层的响应逻辑

## 9. 注意事项

1. 引导过程中通过 `m_CurrentStep.Update(deltaTime)` 驱动步骤帧更新，不会阻塞主循环
2. `ClickUIStep` 需要目标 UI 已加载到场景中（通过 `UIModule.GetUI` 查找）
3. 引导完成状态通过 `PlayerPrefs` 持久化（key 格式：`Guide_Completed_{guideId}`）
4. `GoToPreviousStep` 依赖 `m_StepHistoryStack`，在构建新引导时会清空历史栈
5. 调用 `InterruptGuide()` 会取消当前步骤并清空引导数据
6. `ForceNextStep` 在下一步不存在时自动调用 `FinishGuide` 完成引导
