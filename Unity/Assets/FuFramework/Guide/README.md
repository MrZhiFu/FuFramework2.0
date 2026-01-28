# FuFramework Guide Module

## 简介
FuFramework Guide 模块是一个功能强大的游戏引导系统，专为Unity游戏开发设计。它提供了灵活的引导流程管理，支持多种引导步骤类型，包括UI点击引导、对话引导、等待步骤等，能够满足各种游戏场景的引导需求。

## 核心特性

- **多种引导步骤**：支持点击UI、对话、等待等多种引导类型
- **灵活流程控制**：支持步骤跳转、中断、恢复等复杂流程
- **事件驱动机制**：提供完整的引导生命周期事件
- **对象池管理**：使用对象池优化性能
- **FairyGUI集成**：深度集成FairyGUI UI框架
- **配置化管理**：通过ScriptableObject进行可视化配置

## 核心类说明

### GuideManager
引导管理器，继承自 `FuModule`。
- **职责**：
  1. 管理引导的启动、执行、中断和完成
  2. 处理引导步骤的切换和流程控制
  3. 提供引导状态查询和事件通知
  4. 管理引导完成状态的缓存

### IGuideAction
引导动作执行接口。
- **职责**：
  1. 定义引导动作的统一接口
  2. 支持UI点击引导、对话引导等具体动作
  3. 提供全局遮罩显示/隐藏功能

### BaseStep
引导步骤基类，实现 `IReference` 接口。
- **职责**：
  1. 定义步骤的生命周期（执行、更新、完成、取消）
  2. 提供步骤状态管理和时间追踪
  3. 支持步骤间的自动跳转

## 引导步骤类型

### 1. ClickUIStep (UI点击引导)
引导用户点击指定的UI元素。
- **功能**：高亮显示目标UI，等待用户点击
- **适用场景**：按钮点击、界面操作引导

### 2. DialogStep (对话引导)
显示引导对话内容。
- **功能**：显示对话文本，等待用户确认
- **适用场景**：剧情介绍、功能说明

### 3. WaitStep (等待步骤)
等待指定时间后自动完成。
- **功能**：显示全局遮罩，等待时间结束
- **适用场景**：动画播放、延迟执行

### 4. DefaultStep (默认步骤)
立即完成的默认步骤。
- **功能**：无操作，立即跳转到下一步
- **适用场景**：占位步骤、条件跳转

## 配置结构

### GuideSetting (引导配置)
ScriptableObject配置资源，包含所有引导配置。
```csharp
public class GuideSetting : ScriptableObject
{
    public List<GuideInfo> m_Guides;           // 引导列表
    public Dictionary<string, GuideInfo> m_GuideDict;  // 引导字典
    public Dictionary<string, StepInfo> m_StepDict;    // 步骤字典
}
```

### GuideInfo (引导信息)
单个引导的配置信息。
```csharp
public class GuideInfo
{
    public string m_GuideId;           // 引导ID
    public string m_GuideName;         // 引导名称
    public string m_StartStepId;       // 开始步骤ID
    public List<StepInfo> m_Steps;     // 步骤列表
}
```

### StepInfo (步骤信息)
引导步骤的详细配置。
```csharp
public class StepInfo
{
    public string m_StepId;            // 步骤ID
    public StepType m_StepType;        // 步骤类型
    public string m_NextStepId;        // 下一个步骤ID
    public string m_TargetWindow;      // 目标窗口（UI点击引导）
    public string m_TargetUI;          // 目标UI（UI点击引导）
    public string m_DialogContent;     // 对话内容（对话引导）
    public float m_WaitTime;           // 等待时间（等待步骤）
}
```

## 使用指南

### 1. 基本引导使用
```csharp
using FuFramework.Guide.Runtime;

public class GameGuideController : MonoBehaviour
{
    private void Start()
    {
        // 获取引导管理器
        var guideManager = GlobalModule.GuideModule;
        
        // 设置引导动作执行器（需要实现IGuideAction接口）
        guideManager.GuideAction = new CustomGuideAction();
        
        // 订阅引导事件
        guideManager.OnGuideStarted += OnGuideStarted;
        guideManager.OnGuideFinished += OnGuideFinished;
        guideManager.OnStepChanged += OnStepChanged;
        
        // 启动引导
        if (!guideManager.IsGuideCompleted("new_user_guide"))
        {
            guideManager.StartGuide("new_user_guide");
        }
    }
    
    private void OnGuideStarted(string guideId)
    {
        Debug.Log($"引导开始：{guideId}");
    }
    
    private void OnGuideFinished(string guideId)
    {
        Debug.Log($"引导完成：{guideId}");
        // 保存引导完成状态
        PlayerPrefs.SetInt($"guide_completed_{guideId}", 1);
    }
    
    private void OnStepChanged(string guideId, string stepId)
    {
        Debug.Log($"引导 {guideId} 步骤切换到：{stepId}");
    }
}
```

### 2. 实现引导动作执行器
```csharp
using FairyGUI;
using FuFramework.Guide.Runtime;

public class CustomGuideAction : IGuideAction
{
    private GComponent m_CurrentTargetUI;
    private GComponent m_DialogWindow;
    
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
            // 恢复UI状态
            m_CurrentTargetUI.alpha = 1f;
            m_CurrentTargetUI.TweenScale(Vector2.one, 0.2f);
            m_CurrentTargetUI = null;
        }
    }
    
    public void DoDialogGuide(string content, Action onConfirm = null)
    {
        // 创建对话窗口
        m_DialogWindow = UIPackage.CreateObject("UI", "GuideDialog") as GComponent;
        GRoot.inst.AddChild(m_DialogWindow);
        
        // 设置对话内容
        var contentLabel = m_DialogWindow.GetChild("content") as GTextField;
        contentLabel.text = content;
        
        // 设置确认按钮回调
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
        // 显示全局遮罩
        var mask = new GGraph();
        mask.DrawRect(GRoot.inst.width, GRoot.inst.height, 0, 
            new Color(0, 0, 0, 0.3f), new Color(0, 0, 0, 0.3f));
        GRoot.inst.AddChild(mask);
    }
    
    public void HideGlobalMask()
    {
        // 隐藏全局遮罩
        // 实际实现中需要记录和管理遮罩对象
    }
}
```

### 3. 引导流程控制
```csharp
public class AdvancedGuideController : MonoBehaviour
{
    private GuideManager m_GuideManager;
    
    private void Start()
    {
        m_GuideManager = GlobalModule.GuideModule;
        
        // 检查引导条件
        if (ShouldShowBattleGuide())
        {
            StartBattleGuide();
        }
    }
    
    private bool ShouldShowBattleGuide()
    {
        // 检查玩家等级、任务进度等条件
        var playerLevel = PlayerDataManager.Instance.Level;
        var hasCompletedTutorial = PlayerPrefs.GetInt("tutorial_completed", 0) == 1;
        
        return playerLevel >= 5 && hasCompletedTutorial && 
               !m_GuideManager.IsGuideCompleted("battle_guide");
    }
    
    private void StartBattleGuide()
    {
        m_GuideManager.StartGuide("battle_guide");
        
        // 订阅步骤事件进行特殊处理
        m_GuideManager.OnStepExecuting += OnStepExecuting;
    }
    
    private void OnStepExecuting(BaseStep step)
    {
        // 根据步骤类型执行特殊逻辑
        switch (step.StepInfo.m_StepType)
        {
            case StepType.ClickUI:
                // 准备UI环境
                PrepareUIForGuide(step.StepInfo.m_TargetWindow);
                break;
            case StepType.Dialog:
                // 暂停游戏逻辑
                GameManager.Instance.PauseGame();
                break;
        }
    }
    
    private void PrepareUIForGuide(string targetWindow)
    {
        // 确保目标窗口已打开
        var uiManager = GlobalModule.UIModule;
        if (!uiManager.IsOpen(targetWindow))
        {
            uiManager.Open(targetWindow);
        }
    }
}
```

### 4. 通过 GlobalModule 访问引导模块
```csharp
// 启动引导
GlobalModule.GuideModule.StartGuide("main_guide");

// 检查引导状态
bool isGuiding = GlobalModule.GuideModule.IsGuiding;
bool isCompleted = GlobalModule.GuideModule.IsGuideCompleted("guide_id");

// 获取当前引导信息
string currentGuideId = GlobalModule.GuideModule.CurrentGuideId;
string currentStepId = GlobalModule.GuideModule.CurrentStepId;

// 中断引导
GlobalModule.GuideModule.InterruptGuide();

// 跳转到指定步骤
GlobalModule.GuideModule.JumpToStep("step_id");
```

## 配置示例

### 创建引导配置
1. 在Unity编辑器中右键 → Create → FuFramework → Guide Settings
2. 配置引导信息：

**引导配置示例：**
```json
{
  "m_GuideId": "new_user_guide",
  "m_GuideName": "新手指引",
  "m_StartStepId": "step_1",
  "m_Steps": [
    {
      "m_StepId": "step_1",
      "m_StepType": "Dialog",
      "m_NextStepId": "step_2",
      "m_DialogContent": "欢迎来到游戏世界！让我们开始冒险吧！"
    },
    {
      "m_StepId": "step_2", 
      "m_StepType": "ClickUI",
      "m_NextStepId": "step_3",
      "m_TargetWindow": "MainUI",
      "m_TargetUI": "startBtn"
    },
    {
      "m_StepId": "step_3",
      "m_StepType": "Wait",
      "m_NextStepId": "",
      "m_WaitTime": 2.0
    }
  ]
}
```

## 高级用法

### 1. 条件引导
```csharp
public class ConditionalGuideManager : MonoBehaviour
{
    public void CheckAndStartGuide(string guideId, Func<bool> condition)
    {
        var guideManager = GlobalModule.GuideModule;
        
        if (condition() && !guideManager.IsGuideCompleted(guideId))
        {
            guideManager.StartGuide(guideId);
        }
    }
    
    // 使用示例
    private void OnPlayerLevelUp(int newLevel)
    {
        CheckAndStartGuide("level_5_guide", () => newLevel == 5);
        CheckAndStartGuide("level_10_guide", () => newLevel == 10);
        CheckAndStartGuide("unlock_skill_guide", 
            () => SkillManager.Instance.UnlockedSkills.Count >= 3);
    }
}
```

### 2. 引导中断和恢复
```csharp
public class InterruptibleGuideController : MonoBehaviour
{
    private string m_InterruptedGuideId;
    private string m_InterruptedStepId;
    
    public void OnGamePaused()
    {
        var guideManager = GlobalModule.GuideModule;
        
        if (guideManager.IsGuiding)
        {
            // 记录中断状态
            m_InterruptedGuideId = guideManager.CurrentGuideId;
            m_InterruptedStepId = guideManager.CurrentStepId;
            
            // 中断引导
            guideManager.InterruptGuide();
        }
    }
    
    public void OnGameResumed()
    {
        if (!string.IsNullOrEmpty(m_InterruptedGuideId))
        {
            // 恢复引导
            var guideManager = GlobalModule.GuideModule;
            guideManager.StartGuide(m_InterruptedGuideId);
            
            if (!string.IsNullOrEmpty(m_InterruptedStepId))
            {
                guideManager.JumpToStep(m_InterruptedStepId);
            }
            
            // 清除中断记录
            m_InterruptedGuideId = null;
            m_InterruptedStepId = null;
        }
    }
}
```

### 3. 自定义引导步骤
```csharp
public class CustomAnimationStep : BaseStep
{
    private Animation m_TargetAnimation;
    
    protected override void OnExecute()
    {
        base.OnExecute();
        
        // 查找目标动画
        var animObject = GameObject.Find(StepInfo.m_TargetObject);
        if (animObject != null)
        {
            m_TargetAnimation = animObject.GetComponent<Animation>();
        }
        
        if (m_TargetAnimation != null)
        {
            // 播放动画
            m_TargetAnimation.Play();
            
            // 监听动画完成事件
            StartCoroutine(WaitForAnimationComplete());
        }
        else
        {
            // 动画不存在，直接完成
            Complete();
        }
    }
    
    private System.Collections.IEnumerator WaitForAnimationComplete()
    {
        while (m_TargetAnimation.isPlaying)
        {
            yield return null;
        }
        
        Complete();
    }
    
    protected override void OnComplete()
    {
        m_TargetAnimation = null;
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

## 性能优化建议

1. **对象池使用**：引导步骤使用对象池管理，避免频繁创建销毁
2. **事件清理**：在引导完成或中断时及时清理事件订阅
3. **资源预加载**：引导所需的UI资源提前预加载
4. **条件检查优化**：引导条件检查避免每帧执行
5. **内存管理**：及时释放不再使用的引导资源

## 注意事项

- **UI依赖**：引导系统深度依赖FairyGUI，确保UI框架正确集成
- **步骤顺序**：确保引导步骤的ID和跳转逻辑正确
- **异常处理**：添加适当的错误处理，避免引导流程中断
- **状态保存**：引导完成状态需要持久化保存
- **多语言支持**：对话内容需要考虑多语言支持

## 依赖模块

- **FuFramework.Core**：基础框架模块
- **FuFramework.ModuleSetting**：配置管理模块
- **FuFramework.UI**：UI管理模块
- **FairyGUI**：UI框架
- **Unity引擎**：基础运行环境

## 技术支持

如遇到引导问题，请检查：
1. 引导配置是否正确
2. UI组件路径是否匹配
3. 引导动作执行器是否设置
4. 事件订阅是否正确清理
5. 对象池引用是否正确释放