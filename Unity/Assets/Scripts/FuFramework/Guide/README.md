# FuFramework Guide Module

## 1. 简介

**FuFramework Guide** 模块是一个功能强大的游戏引导系统，专为 Unity 游戏开发设计。它提供了灵活的引导流程管理，支持多种引导步骤类型，包括 UI 点击引导、对话引导、等待步骤等，能够满足各种游戏场景的引导需求。

本模块与 **FairyGUI** UI 框架深度集成，通过 **ModuleSetting/GuideSetting** 模块进行可视化配置管理。

***

## 2. 特性

- **多种引导步骤**：支持点击 UI、对话、等待等多种引导类型
- **灵活流程控制**：支持步骤跳转、中断、恢复、回退等复杂流程
- **事件驱动机制**：提供完整的引导生命周期事件
- **对象池管理**：使用引用池优化性能，减少 GC 压力
- **FairyGUI 集成**：深度集成 FairyGUI UI 框架
- **配置化管理**：通过 ScriptableObject 进行可视化配置
- **完成状态缓存**：自动缓存引导完成状态到 PlayerPrefs
- **步骤历史记录**：支持返回上一步操作

***

## 3. 核心概念

### 3.1 引导 (Guide)

一个完整的引导流程，由多个步骤组成。每个引导有唯一的 ID、名称和起始步骤。

### 3.2 步骤 (Step)

引导流程中的单个节点，可以是点击 UI、显示对话、等待时间等。步骤之间通过 `NextStepId` 形成链式结构。

### 3.3 步骤类型 (StepType)

| 类型 | 说明 | 适用场景 |
|------|------|----------|
| `None` | 默认步骤，立即完成 | 占位步骤、条件跳转 |
| `ClickUI` | UI 点击引导 | 按钮点击、界面操作引导 |
| `Dialog` | 对话引导 | 剧情介绍、功能说明 |
| `Wait` | 等待步骤 | 动画播放、延迟执行 |

### 3.4 引导动作执行器 (IGuideAction)

定义引导动作的统一接口，由业务层实现具体的引导表现（如高亮 UI、显示对话框等）。

***

## 4. 核心类详解

### 4.1 GuideModule

引导管理器，继承自 `FuModule`，负责引导的完整生命周期管理。

#### 公开属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `IsGuiding` | `bool` | 是否正在引导中 |
| `CurrentGuideId` | `string` | 当前引导 ID |
| `CurrentStepId` | `string` | 当前步骤 ID |
| `CurrentGuide` | `GuideInfo` | 当前引导配置信息 |
| `CurrentStep` | `BaseStep` | 当前步骤实例 |
| `GuideAction` | `IGuideAction` | 引导动作执行接口 |

#### 事件

| 事件 | 参数 | 说明 |
|------|------|------|
| `OnGuideStarted` | `string guideId` | 引导开始事件 |
| `OnGuideFinished` | `string guideId` | 引导完成事件 |
| `OnStepChanged` | `string guideId, string stepId` | 步骤改变事件 |
| `OnGuideInterrupted` | `string guideId, bool markAsCompleted` | 引导中断事件 |
| `OnStepExecuting` | `BaseStep step` | 步骤开始执行事件 |
| `OnStepCompleted` | `BaseStep step` | 步骤完成事件 |

#### 主要方法

```csharp
// 通过引导ID开始引导
public bool StartGuideById(string guideId, bool forceRestart = false)

// 通过引导名称开始引导
public bool StartGuideByName(string guideName, bool forceRestart = false)

// 开始第一个引导
public bool StartFirstGuide(bool forceRestart = false)

// 完成当前步骤并进入下一步
public void CompleteCurrentStep()

// 跳过当前步骤
public void SkipCurrentStep()

// 返回上一步
public void GoToPreviousStep()

// 跳转到指定步骤
public bool JumpToStep(string stepId)

// 中断引导
public void InterruptGuide(bool markAsCompleted = false)

// 强制进入下一步
public void ForceNextStep()

// 检查引导是否已完成
public bool IsGuideCompleted(string guideId)

// 标记引导为已完成
public void MarkGuideAsCompleted(string guideId)

// 重置引导状态
public void ResetGuide(string guideId)
```

### 4.2 IGuideAction

引导动作执行接口，定义引导表现的具体实现。

```csharp
public interface IGuideAction
{
    // 执行点击UI引导
    void DoClickUIGuide(GComponent targetUI);
    
    // 结束点击UI引导
    void EndClickUIGuide();
    
    // 执行对话引导
    void DoDialogGuide(string content, Action onConfirm = null);
    
    // 结束对话引导
    void EndDialogGuide();
    
    // 显示全局遮罩窗口
    void ShowGlobalMask();
    
    // 隐藏全局遮罩窗口
    void HideGlobalMask();
}
```

### 4.3 BaseStep

引导步骤基类，实现 `IReference` 接口，支持引用池管理。

#### 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `StepInfo` | `StepInfo` | 步骤配置数据 |
| `ExecutionTime` | `float` | 步骤执行时间 |
| `StartTime` | `float` | 步骤开始时间 |
| `State` | `StepState` | 步骤状态 |
| `IsExecuting` | `bool` | 是否正在执行 |
| `IsCompleted` | `bool` | 是否已完成 |
| `GuideAction` | `IGuideAction` | 引导动作执行器 |

#### 步骤状态 (StepState)

```csharp
public enum StepState
{
    Idle,       // 空闲
    Executing,  // 执行中
    Completed,  // 已完成
    Cancelled,  // 被取消
    Failed      // 执行失败
}
```

#### 生命周期方法

```csharp
// 执行步骤
public void Execute()

// 步骤帧更新
public void Update(float deltaTime)

// 完成步骤
public void Complete()

// 取消步骤
public void Cancel()

// 子类可重写的方法
protected virtual void OnExecute() { }
protected virtual void OnUpdate(float deltaTime) { }
protected virtual void OnComplete() { }
protected virtual void OnCancel() { }
public virtual bool CanExecute() => true;
public virtual bool CanComplete() => State == StepState.Executing;
public virtual void Clear() => StepInfo = null;
```

### 4.4 内置步骤类型

#### ClickUIStep (UI 点击引导)

```csharp
public class ClickUIStep : BaseStep
{
    // 查找目标UI窗口
    var targetWin = uiModule.GetUI(StepInfo.m_TargetWindow);
    
    // 查找目标UI组件
    var targetClickUI = targetWin.UIView.GetChild(StepInfo.m_TargetUI) as GComponent;
    
    // 添加点击回调
    m_TargetUI.onClick.Add(Complete);
    
    // 执行引导动作
    GuideAction.DoClickUIGuide(m_TargetUI);
}
```

#### DialogStep (对话引导)

```csharp
public class DialogStep : BaseStep
{
    protected override void OnExecute()
    {
        // 显示对话，确认后自动完成
        GuideAction.DoDialogGuide(StepInfo.m_DialogContent, Complete);
    }
}
```

#### WaitStep (等待步骤)

```csharp
public class WaitStep : BaseStep
{
    protected override void OnUpdate(float deltaTime)
    {
        // 计时等待
        m_WaitTimer += deltaTime;
        if (m_WaitTimer >= StepInfo.m_WaitTime)
        {
            Complete();
        }
    }
}
```

#### DefaultStep (默认步骤)

```csharp
public class DefaultStep : BaseStep
{
    protected override void OnExecute()
    {
        // 默认步骤立即完成
        Complete(); 
    }
}
```

### 4.5 配置类

#### GuideSetting (引导配置)

```csharp
[CreateAssetMenu(fileName = "GuideSettings", menuName = "FuFramework/Guide Settings")]
public class GuideSetting : ScriptableObject
{
    // 引导列表
    [SerializeField] private List<GuideInfo> m_Guides = new();
    
    // 索引器：通过引导ID获取引导
    public GuideInfo this[string guideId] { get; }
    
    // 索引器：通过索引获取引导
    public GuideInfo this[int index] { get; }
    
    // 获取所有引导
    public IReadOnlyList<GuideInfo> AllGuides { get; }
    
    // 引导数量
    public int GuideCount { get; }
    
    // 总步骤数量
    public int TotalStepCount { get; }
}
```

#### GuideInfo (引导信息)

```csharp
[System.Serializable]
public class GuideInfo
{
    public string m_GuideId;        // 引导ID
    public string m_GuideName;      // 引导名称
    public string m_StartStepId;    // 开始步骤ID
    public List<StepInfo> m_Steps;  // 步骤列表
}
```

#### StepInfo (步骤信息)

```csharp
[System.Serializable]
public class StepInfo
{
    public string m_StepId;         // 步骤ID
    public StepType m_StepType;     // 步骤类型
    public string m_NextStepId;     // 下一个步骤ID
    public bool m_IsCanJump;        // 是否可以跳过
    
    // ClickUI 使用
    public string m_TargetWindow;   // 目标窗口
    public string m_TargetUI;       // 目标UI
    
    // Dialog 使用
    public string m_DialogContent;  // 对话内容
    
    // Wait 使用
    public float m_WaitTime;        // 等待时间
}
```

***

## 5. 使用示例

### 5.1 实现引导动作执行器

```csharp
using FairyGUI;
using FuFramework.Guide.Runtime;

public class CustomGuideAction : IGuideAction
{
    private GComponent m_CurrentTargetUI;
    private GComponent m_DialogWindow;
    private GGraph m_GlobalMask;
    
    public void DoClickUIGuide(GComponent targetUI)
    {
        m_CurrentTargetUI = targetUI;
        
        // 高亮显示目标UI
        targetUI.alpha = 0.8f;
        
        // 添加点击效果（如脉冲动画）
        var scaleTween = targetUI.TweenScale(new Vector2(1.1f, 1.1f), 0.5f);
        scaleTween.SetEase(EaseType.QuadOut);
        scaleTween.SetLoops(-1, LoopType.Yoyo);
    }
    
    public void EndClickUIGuide()
    {
        if (m_CurrentTargetUI != null)
        {
            m_CurrentTargetUI.alpha = 1f;
            m_CurrentTargetUI.TweenScale(Vector2.one, 0.2f);
            m_CurrentTargetUI = null;
        }
    }
    
    public void DoDialogGuide(string content, Action onConfirm = null)
    {
        m_DialogWindow = UIPackage.CreateObject("UI", "GuideDialog") as GComponent;
        GRoot.inst.AddChild(m_DialogWindow);
        
        var contentLabel = m_DialogWindow.GetChild("content") as GTextField;
        contentLabel.text = content;
        
        var confirmBtn = m_DialogWindow.GetChild("confirmBtn") as GButton;
        confirmBtn.onClick.Add(() =>
        {
            onConfirm?.Invoke();
            EndDialogGuide();
        });
    }
    
    public void EndDialogGuide()
    {
        if (m_DialogWindow != null)
        {
            m_DialogWindow.RemoveFromParent();
            m_DialogWindow.Dispose();
            m_DialogWindow = null;
        }
    }
    
    public void ShowGlobalMask()
    {
        m_GlobalMask = new GGraph();
        m_GlobalMask.DrawRect(GRoot.inst.width, GRoot.inst.height, 0, 
            new Color(0, 0, 0, 0.5f), new Color(0, 0, 0, 0.5f));
        GRoot.inst.AddChild(m_GlobalMask);
    }
    
    public void HideGlobalMask()
    {
        if (m_GlobalMask != null)
        {
            m_GlobalMask.RemoveFromParent();
            m_GlobalMask.Dispose();
            m_GlobalMask = null;
        }
    }
}
```

### 5.2 基本引导使用

```csharp
using FuFramework.Guide.Runtime;

public class GameGuideController : MonoBehaviour
{
    private void Start()
    {
        var guideModule = ModuleManager.GetModule<GuideModule>();
        
        // 设置引导动作执行器
        guideModule.GuideAction = new CustomGuideAction();
        
        // 订阅引导事件
        guideModule.OnGuideStarted += OnGuideStarted;
        guideModule.OnGuideFinished += OnGuideFinished;
        guideModule.OnStepChanged += OnStepChanged;
        
        // 启动引导
        if (!guideModule.IsGuideCompleted("new_user_guide"))
        {
            guideModule.StartGuideById("new_user_guide");
        }
    }
    
    private void OnGuideStarted(string guideId)
    {
        Debug.Log($"引导开始：{guideId}");
    }
    
    private void OnGuideFinished(string guideId)
    {
        Debug.Log($"引导完成：{guideId}");
    }
    
    private void OnStepChanged(string guideId, string stepId)
    {
        Debug.Log($"引导 {guideId} 步骤切换到：{stepId}");
    }
}
```

### 5.3 引导流程控制

```csharp
public class AdvancedGuideController : MonoBehaviour
{
    private GuideModule m_GuideModule;
    
    private void Start()
    {
        m_GuideModule = ModuleManager.GetModule<GuideModule>();
        
        // 检查引导条件
        if (ShouldShowBattleGuide())
        {
            StartBattleGuide();
        }
    }
    
    private bool ShouldShowBattleGuide()
    {
        var playerLevel = PlayerDataManager.Instance.Level;
        var hasCompletedTutorial = m_GuideModule.IsGuideCompleted("tutorial_guide");
        
        return playerLevel >= 5 && hasCompletedTutorial && 
               !m_GuideModule.IsGuideCompleted("battle_guide");
    }
    
    private void StartBattleGuide()
    {
        m_GuideModule.StartGuideById("battle_guide");
        m_GuideModule.OnStepExecuting += OnStepExecuting;
    }
    
    private void OnStepExecuting(BaseStep step)
    {
        switch (step.StepInfo.m_StepType)
        {
            case StepType.ClickUI:
                PrepareUIForGuide(step.StepInfo.m_TargetWindow);
                break;
            case StepType.Dialog:
                GameManager.Instance.PauseGame();
                break;
        }
    }
    
    // 跳过当前步骤
    public void OnSkipButtonClicked()
    {
        m_GuideModule.SkipCurrentStep();
    }
    
    // 返回上一步
    public void OnBackButtonClicked()
    {
        m_GuideModule.GoToPreviousStep();
    }
}
```

### 5.4 引导中断和恢复

```csharp
public class InterruptibleGuideController : MonoBehaviour
{
    private string m_InterruptedGuideId;
    private Stack<string> m_StepHistory = new Stack<string>();
    
    public void OnGamePaused()
    {
        var guideModule = ModuleManager.GetModule<GuideModule>();
        
        if (guideModule.IsGuiding)
        {
            m_InterruptedGuideId = guideModule.CurrentGuideId;
            guideModule.InterruptGuide();
        }
    }
    
    public void OnGameResumed()
    {
        if (!string.IsNullOrEmpty(m_InterruptedGuideId))
        {
            var guideModule = ModuleManager.GetModule<GuideModule>();
            guideModule.StartGuideById(m_InterruptedGuideId, true);
            m_InterruptedGuideId = null;
        }
    }
}
```

### 5.5 自定义引导步骤

```csharp
public class CustomAnimationStep : BaseStep
{
    private Animator m_TargetAnimator;
    
    protected override void OnExecute()
    {
        base.OnExecute();
        
        var animObject = GameObject.Find(StepInfo.m_TargetObject);
        if (animObject != null)
        {
            m_TargetAnimator = animObject.GetComponent<Animator>();
        }
        
        if (m_TargetAnimator != null)
        {
            m_TargetAnimator.Play("GuideAnimation");
            // 动画事件触发完成
        }
        else
        {
            Complete();
        }
    }
    
    // 动画结束回调
    public void OnAnimationComplete()
    {
        Complete();
    }
    
    protected override void OnComplete()
    {
        m_TargetAnimator = null;
        base.OnComplete();
    }
    
    public static CustomAnimationStep Create(StepInfo stepInfo)
    {
        var step = ReferencePool.Runtime.ReferencePool.Acquire<CustomAnimationStep>();
        step.StepInfo = stepInfo;
        return step;
    }
}
```

### 5.6 条件引导

```csharp
public class ConditionalGuideManager : MonoBehaviour
{
    private GuideModule m_GuideModule;
    
    private void Start()
    {
        m_GuideModule = ModuleManager.GetModule<GuideModule>();
    }
    
    public void CheckAndStartGuide(string guideId, Func<bool> condition)
    {
        if (condition() && !m_GuideModule.IsGuideCompleted(guideId))
        {
            m_GuideModule.StartGuideById(guideId);
        }
    }
    
    private void OnPlayerLevelUp(int newLevel)
    {
        CheckAndStartGuide("level_5_guide", () => newLevel == 5);
        CheckAndStartGuide("level_10_guide", () => newLevel == 10);
    }
}
```

***

## 6. 配置示例

### 6.1 创建引导配置

1. 在 Unity 编辑器中右键 → Create → FuFramework → Guide Settings
2. 配置引导信息

### 6.2 引导配置示例

```csharp
// 新手指引配置示例
var newUserGuide = new GuideInfo
{
    m_GuideId = "new_user_guide",
    m_GuideName = "新手指引",
    m_StartStepId = "step_welcome",
    m_Steps = new List<StepInfo>
    {
        new StepInfo
        {
            m_StepId = "step_welcome",
            m_StepType = StepType.Dialog,
            m_NextStepId = "step_click_start",
            m_DialogContent = "欢迎来到游戏世界！让我们开始冒险吧！"
        },
        new StepInfo
        {
            m_StepId = "step_click_start",
            m_StepType = StepType.ClickUI,
            m_NextStepId = "step_wait_animation",
            m_TargetWindow = "MainMenu",
            m_TargetUI = "startBtn"
        },
        new StepInfo
        {
            m_StepId = "step_wait_animation",
            m_StepType = StepType.Wait,
            m_NextStepId = "step_end",
            m_WaitTime = 2.0f
        },
        new StepInfo
        {
            m_StepId = "step_end",
            m_StepType = StepType.Dialog,
            m_NextStepId = "",
            m_DialogContent = "引导完成！祝您游戏愉快！"
        }
    }
};
```

***

## 7. 目录结构

```
FuFramework/Guide/
├── README.md                              # 模块说明文档
├── Runtime/                               # 运行时代码
│   ├── FuFramework.Guide.Runtime.asmdef   # 程序集定义
│   ├── GuideModule.cs                     # 引导管理模块
│   ├── IGuideAction.cs                    # 引导动作接口
│   └── Steps/                             # 步骤实现
│       ├── BaseStep.cs                    # 步骤基类
│       ├── ClickUIStep.cs                 # UI点击步骤
│       ├── DefaultStep.cs                 # 默认步骤
│       ├── DialogStep.cs                  # 对话步骤
│       └── WaitStep.cs                    # 等待步骤
```

***

## 8. 依赖

- **FuFramework.Core**：基础框架模块
- **FuFramework.ModuleSetting**：配置管理模块
- **FuFramework.UI**：UI 管理模块
- **FuFramework.ReferencePool**：引用池模块
- **FairyGUI**：UI 框架
- **UnityEngine**：Unity 引擎

***

## 9. 最佳实践

### 9.1 引导设计原则

- **单一职责**：每个引导专注于一个功能点
- **可跳过性**：非关键引导应支持跳过
- **状态保存**：及时保存引导完成状态
- **异常处理**：添加适当的错误处理

### 9.2 性能优化

- **对象池使用**：引导步骤使用引用池管理
- **事件清理**：引导完成时清理事件订阅
- **资源预加载**：引导所需资源提前预加载
- **条件检查优化**：避免每帧执行条件检查

### 9.3 代码组织

```csharp
// 推荐：将引导配置集中管理
public static class GuideConfig
{
    public const string NEW_USER_GUIDE = "new_user_guide";
    public const string BATTLE_GUIDE = "battle_guide";
    public const string SKILL_GUIDE = "skill_guide";
}

// 推荐：引导控制器统一管理
public class GuideManager : MonoBehaviour
{
    private GuideModule m_GuideModule;
    
    private void Start()
    {
        m_GuideModule = ModuleManager.GetModule<GuideModule>();
        m_GuideModule.GuideAction = new CustomGuideAction();
        
        // 订阅事件
        m_GuideModule.OnGuideStarted += OnGuideStarted;
        m_GuideModule.OnGuideFinished += OnGuideFinished;
    }
    
    private void OnDestroy()
    {
        // 取消订阅
        m_GuideModule.OnGuideStarted -= OnGuideStarted;
        m_GuideModule.OnGuideFinished -= OnGuideFinished;
    }
}
```

***

## 10. 注意事项

1. **UI 依赖**：引导系统深度依赖 FairyGUI，确保 UI 框架正确集成
2. **步骤顺序**：确保引导步骤的 ID 和跳转逻辑正确配置
3. **异常处理**：添加适当的错误处理，避免引导流程中断
4. **状态保存**：引导完成状态自动保存到 PlayerPrefs
5. **多语言支持**：对话内容需要考虑多语言支持
6. **事件清理**：在对象销毁时取消事件订阅，避免内存泄漏
7. **引用池管理**：自定义步骤需要正确实现 `Create` 方法和 `Clear` 方法
8. **步骤链验证**：配置引导时确保步骤链完整，避免死链
