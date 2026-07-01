# FuFramework UI Module

## 1. 概述

UI 模块是 FuFramework 中的用户界面管理系统，基于 FairyGUI 实现，提供完整的 UI 生命周期管理、层级管理、资源加载和动画效果。该模块采用模块化设计，支持界面分组、对象池复用、异步加载等高级功能。

### 1.1 核心特性

- **基于 FairyGUI**：强大的 UI 框架支持，跨平台兼容
- **完整的生命周期管理**：初始化、打开、关闭、暂停、恢复、被覆盖等完整流程
- **多层级的 UI 分组**：支持 WorldUI、MainUI、Normal、Window、Tips、Guide、Loading 等层级
- **对象池管理**：界面实例对象池，减少内存分配和GC压力
- **异步资源加载**：基于 YooAsset 的异步 UI 包加载机制
- **动画效果支持**：内置淡入淡出动画，支持自定义动画效果
- **事件驱动架构**：完整的界面打开/关闭事件通知机制
- **模块化设计**：支持界面组件化和自定义组件扩展

## 2. 系统架构

### 2.1 类继承体系

```
FuModule (抽象基类)
    ↑
UIModule (UI管理模块)
    ├── m_UIGroupDict (界面组字典)
    ├── m_LoadingDict (加载中界面字典)
    ├── m_WaitRecycleQueue (待回收界面队列)
    ├── m_InstancePool (界面实例对象池)
    └── PkgManager (FUI包管理器)

GComponent (FairyGUI)
    ↑
UIGroup (界面组)
    ├── m_UIInfoList (界面信息链表)
    ├── Pause (暂停状态)
    └── Refresh() (刷新界面组)

ObjectBase (对象池基类)
    ↑
ViewObject (界面对象池对象)
    └── Target (ViewBase)

ViewBase (抽象基类)
    ├── 生命周期方法 (OnInit/OnOpen/OnUpdate/OnClose)
    ├── 动画方法 (DoCustomOpenTween/DoCustomCloseTween)
    ├── EventRegister (事件注册器)
    ├── TimerRegister (计时器注册器)
    └── UIEventRegister (UI事件注册器)

IReference (引用池接口)
    ↑
ViewInfo (界面信息)
    ├── View (ViewBase)
    ├── Paused (是否暂停)
    └── Covered (是否被覆盖)
```

### 2.2 技术架构图

```
┌─────────────────────────────────────────────────────────────────┐
│                        UIModule                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  m_UIGroupDict (Dictionary<UILayer, UIGroup>)          │   │
│  │  - WorldUI, MainUI, Normal, Window, Tips, Guide, Loading│   │
│  └─────────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  m_InstancePool (ObjectPool<ViewObject>)               │   │
│  │  - 界面实例缓存与复用                                    │   │
│  └─────────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  PkgManager (FuiPkgManager)                            │   │
│  │  - 包加载、缓存、引用计数管理                            │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                              ↓
                    ┌─────────────────┐
                    │   UIGroup       │
                    │ (GComponent)    │
                    │  - m_UIInfoList │
                    └────────┬────────┘
                             ↓
                    ┌─────────────────┐
                    │   ViewInfo      │
                    │  - ViewBase     │
                    │  - Paused       │
                    │  - Covered      │
                    └────────┬────────┘
                             ↓
                    ┌─────────────────┐
                    │   ViewBase      │
                    │  - UIView       │
                    │  - EventReg     │
                    │  - TimerReg     │
                    │  - UIEventReg   │
                    └─────────────────┘
```

## 3. 核心类详解

### 3.1 UIModule

UI 管理模块，继承自 FuModule，负责所有 UI 界面的统一管理。

**核心字段：**

| 字段 | 类型 | 说明 |
|------|------|------|
| m_UIGroupDict | Dictionary<UILayer, UIGroup> | 界面组字典 |
| m_LoadingDict | Dictionary<int, string> | 加载中界面字典 |
| m_WaitRecycleQueue | Queue<ViewBase> | 待回收界面队列 |
| m_InstancePool | ObjectPool<ViewObject> | 界面实例对象池 |
| PkgManager | FuiPkgManager | FUI包管理器 |

**核心属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| InstanceAutoReleaseInterval | float | 对象池自动释放间隔 |
| InstanceCapacity | int | 对象池容量 |
| InstanceExpireTime | float | 对象过期时间 |
| UIGroupCount | int | 界面组数量 |

**核心方法：**

```csharp
// 打开界面
public void OpenUI<T>(object userData = null) where T : ViewBase, new()
public async UniTask<T> OpenUIAsync<T>(object userData = null) where T : ViewBase, new()

// 关闭界面
public void CloseUI<T>() where T : ViewBase
public void CloseUI(int serialId)
public void CloseUI(ViewBase view)
public void CloseUINow<T>() where T : ViewBase
public void CloseAllUIs()

// 获取界面
public T GetUI<T>() where T : ViewBase
public ViewBase GetUI(int serialId)
public ViewBase GetTopUI(UILayer? uiLayer = null)
public ViewBase[] GetAllLoadedUIs()

// 查询界面
public bool HasUI<T>() where T : ViewBase
public bool HasUI(int serialId)
public bool IsLoadingUI(string uiName)

// 界面组管理
public bool AddUIGroup(UILayer layer)
public UIGroup GetUIGroup(UILayer layer)
public bool HasUIGroup(UILayer layer)

// 对象池设置
public void SetUILocked(object uiView, bool locked)
public void SetUIPriority(object uiView, int priority)
```

**打开界面流程：**

1. 检查是否已在加载中或已存在
2. 分配临时序列号
3. 尝试从对象池获取实例
4. 如对象池无实例，创建新的 ViewBase 实例
5. 检查并加载 UI 包（异步）
6. 创建 FairyGUI 界面组件
7. 初始化界面（Init）
8. 添加到界面组
9. 触发打开回调和事件

### 3.2 ViewBase

界面基类，所有 UI 界面必须继承此类。

**核心属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| SerialId | int | 界面序列编号 |
| UIView | GComponent | FairyGUI 显示对象 |
| UserData | object | 用户自定义数据 |
| UIName | string | 界面名称（可重写） |
| PackageName | string | UI包名称（可重写） |
| Layer | UILayer | 界面层级（可重写） |
| IgnoreSafeArea | bool | 是否忽略安全区/刘海屏（可重写，默认true），false 时自动约束到安全区内 |
| PauseCoveredUI | bool | 是否暂停被覆盖界面（可重写） |
| TweenType | UITweenType | 动画类型（可重写） |
| TweenDuration | float | 动画时长（可重写，默认0.3s） |
| UIGroup | UIGroup | 所属界面组 |
| Visible | bool | 是否可见 |

**生命周期方法：**

```csharp
// 初始化 - 只执行一次
protected virtual void OnInit()

// 打开 - 每次打开时执行
protected virtual void OnOpen()

// 更新 - 每帧执行
protected virtual void OnUpdate(float deltaTime, float unscaledDeltaTime)

// 暂停/恢复
protected virtual void OnPause()
protected virtual void OnResume()

// 被覆盖/恢复
protected virtual void OnBeCover()
protected virtual void OnReveal()

// 关闭
protected virtual void OnClose()

// 回收/销毁
protected virtual void OnRecycle()
protected virtual void OnDispose()

// 自定义动画
protected virtual void DoCustomOpenTween()
protected virtual GTweener DoCustomCloseTween()
```

**事件注册方法：**

```csharp
// 业务事件
public void Subscribe(string eventId, EventHandler<GameEventArgs> handler)
public void UnSubscribe(string eventId, EventHandler<GameEventArgs> handler)
public void Broadcast(object sender, GameEventArgs eventArgs)
public void BroadcastNow(object sender, GameEventArgs eventArgs)

// UI事件
public void AddUIListener(EventListener listener, EventCallback1 callback)
public void SetUIListener(EventListener listener, EventCallback1 callback)
public void RemoveUIListener(EventListener listener, EventCallback1 callback)
public void ClearUIListener(EventListener listener)
public void ClearAllUIListener()

// 计时器
public void StartCountdownTimer(float duration, Action finishCallBack = null, ...)
public void StartIntervalTimer(float interval, Action intervalCallback, ...)
public void StartFrameTimer(int frameInterval, Action intervalCallback, ...)
public void PauseTimer(int timerId)
public void ResumeTimer(int timerId)
public void StopTimer(int timerId)
```

### 3.3 UIGroup

界面组，管理同一层级下的多个界面，继承自 FairyGUI 的 GComponent。

**核心字段：**

| 字段 | 类型 | 说明 |
|------|------|------|
| m_UIInfoList | FuLinkedList<ViewInfo> | 界面信息链表 |
| m_Pause | bool | 组暂停状态 |
| Layer | UILayer | 界面组层级 |

**核心属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| Pause | bool | 是否暂停 |
| UICount | int | 界面数量 |
| CurrentViewBase | ViewBase | 当前（顶部）界面 |

**核心方法：**

```csharp
// 初始化
public void Init(UILayer layer)

// 更新
public void OnUpdate(float deltaTime, float unscaledDeltaTime)

// 界面管理
public void AddUI(ViewBase view)
public void RemoveUI(ViewBase view)
public bool HasUI(int serialId)
public bool HasUI(string uiName)
public ViewBase GetUI(int serialId)
public ViewBase GetUI(string uiName)
public ViewBase[] GetAllUIs()

// 刷新界面组状态
public void Refresh()
```

**界面组刷新机制：**

```csharp
// 从链表头部开始遍历
var current = m_UIInfoList.First;
var isCover = false;   // 是否覆盖后面的界面
var isPause = m_Pause; // 是否暂停的标志

while (current != null)
{
    // 处理被暂停的界面状态
    HandlePauseState(uiInfo, ref isPause);
    
    // 处理被覆盖的界面状态
    HandleCoverState(uiInfo, ref isCover);
    
    current = current.Next;
}
```

### 3.4 FuiPkgManager

FairyGUI 包管理器，负责 UI 包的加载、缓存和卸载管理。

**核心字段：**

| 字段 | 类型 | 说明 |
|------|------|------|
| m_LoadedPkgDict | Dictionary<string, UIPackage> | 已加载的包 |
| m_LoadingTasks | Dictionary<string, UniTask<UIPackage>> | 正在加载的任务 |
| m_LoadingCts | Dictionary<string, CancellationTokenSource> | 取消令牌源 |
| m_PkgAssetLoaderDict | Dictionary<string, AssetLoadRegister> | 资源加载器 |
| m_PkgRefCountDict | Dictionary<string, int> | 包引用计数 |
| m_FromResourcesPackages | List<string> | 从 Resources 加载的包名列表 |

**核心方法：**

```csharp
// 包管理
public bool HasPackage(string packageName)
public UniTask<UIPackage> AddPackageAsync(string pkgName)
public void ReleasePackage(string pkgName)
public void ReleaseAll()

// 引用计数
public void AddRef(string pkgName)
public void SubRef(string pkgName)
```

**包加载流程：**

1. 检查是否已加载或正在加载
2. 创建取消令牌源
3. 加载包描述文件（_fui.bytes）
4. 异步加载包内资源
5. 加载依赖包（并行）
6. 缓存并返回包实例

### 3.5 ViewInfo

界面信息类，存储界面在组中的状态信息。

**核心属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| View | ViewBase | 界面实例 |
| Paused | bool | 是否暂停 |
| Covered | bool | 是否被覆盖 |

### 3.6 ViewObject

界面对象池对象，用于对象池管理界面实例。

**核心方法：**

```csharp
// 创建
public static ViewObject Create(string uiName, ViewBase viewBase)

// 释放时调用
protected override void OnRelease()
```

### 3.7 UILayer 枚举

```csharp
public enum UILayer
{
    WorldUI = 0,      // 世界场景UI，如HUD、血条
    MainUI = 1500,    // 主界面
    Normal = 2000,    // 一般全屏界面
    Window = 2500,    // 窗口
    Tips = 3000,      // 提示
    Guide = 3500,     // 引导
    Loading = 4000,   // Loading
}
```

### 3.8 UITweenType 枚举

```csharp
public enum UITweenType
{
    None,    // 无动画
    Fade,    // 淡入淡出
    Custom,  // 自定义动画
}
```

## 4. 使用示例

### 4.1 创建自定义 UI 界面

```csharp
using FuFramework.UI.Runtime;
using FairyGUI;
using UnityEngine;

// 自定义主界面
public class MainUIView : ViewBase
{
    // 界面名称
    public override string UIName => "MainUI";
    
    // UI包名称
    public override string PackageName => "Main";
    
    // 界面层级
    protected override UILayer Layer => UILayer.MainUI;
    
    // 是否全屏界面
    protected override bool IgnoreSafeArea => true;
    
    // 动画类型
    protected override UITweenType TweenType => UITweenType.Fade;
    
    // 动画时长
    protected override float TweenDuration => 0.3f;
    
    // 界面组件引用
    private GButton m_StartButton;
    private GButton m_SettingButton;
    
    // 初始化界面
    protected override void OnInit()
    {
        // 获取界面组件
        m_StartButton = UIView.GetChild("start_btn") as GButton;
        m_SettingButton = UIView.GetChild("setting_btn") as GButton;
        
        // 注册按钮事件
        AddUIListener(m_StartButton.onClick, OnStartButtonClick);
        AddUIListener(m_SettingButton.onClick, OnSettingButtonClick);
        
        Debug.Log("主界面初始化完成");
    }
    
    // 界面打开
    protected override void OnOpen()
    {
        Debug.Log("主界面打开");
    }
    
    // 界面更新
    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        // 每帧更新逻辑
    }
    
    // 界面关闭
    protected override void OnClose()
    {
        Debug.Log("主界面关闭");
    }
    
    private void OnStartButtonClick()
    {
        Debug.Log("开始游戏按钮点击");
    }
    
    private void OnSettingButtonClick()
    {
        Debug.Log("设置按钮点击");
        CloseSelf();
    }
}
```

### 4.2 打开和关闭界面

```csharp
using FuFramework.UI.Runtime;
using UnityEngine;

public class GameController : MonoBehaviour
{
    private UIModule m_UIModule;
    
    private void Start()
    {
        // 获取 UI 管理器
        m_UIModule = ModuleManager.GetModule<UIModule>();
        
        // 打开主界面
        m_UIModule.OpenUI<MainUIView>();
        
        // 异步打开设置界面
        OpenSettingAsync();
    }
    
    private async void OpenSettingAsync()
    {
        var settingView = await m_UIModule.OpenUIAsync<SettingUIView>();
        if (settingView != null)
        {
            Debug.Log("设置界面打开成功");
        }
    }
    
    private void CloseMainUI()
    {
        // 关闭主界面
        m_UIModule.CloseUI<MainUIView>();
    }
    
    private void OnDestroy()
    {
        // 关闭所有界面
        m_UIModule?.CloseAllUIs();
    }
}
```

### 4.3 自定义动画效果

```csharp
public class AnimatedUIView : ViewBase
{
    public override string UIName => "AnimatedUI";
    protected override UITweenType TweenType => UITweenType.Custom;
    
    // 自定义打开动画
    protected override void DoCustomOpenTween()
    {
        // 缩放动画
        UIView.scale = Vector2.zero;
        UIView.TweenScale(Vector2.one, TweenDuration)
              .SetEase(EaseType.BackOut);
    }
    
    // 自定义关闭动画
    protected override GTweener DoCustomCloseTween()
    {
        // 旋转缩放动画
        return UIView.TweenScale(Vector2.zero, TweenDuration)
                     .SetEase(EaseType.BackIn);
    }
}
```

### 4.4 使用不同层级

```csharp
// 世界UI - HUD、血条等
public class HUDView : ViewBase
{
    protected override UILayer Layer => UILayer.WorldUI;
    protected override bool IgnoreSafeArea => false;
}

// 主界面
public class MainMenuView : ViewBase
{
    protected override UILayer Layer => UILayer.MainUI;
}

// 普通全屏界面
public class BattleView : ViewBase
{
    protected override UILayer Layer => UILayer.Normal;
}

// 窗口界面
public class ShopView : ViewBase
{
    protected override UILayer Layer => UILayer.Window;
    protected override bool IgnoreSafeArea => false;
}

// 提示界面
public class ToastView : ViewBase
{
    protected override UILayer Layer => UILayer.Tips;
    protected override bool IgnoreSafeArea => false;
}

// Loading界面
public class LoadingView : ViewBase
{
    protected override UILayer Layer => UILayer.Loading;
}
```

### 4.5 界面组操作

```csharp
public class UIGroupExample : MonoBehaviour
{
    private UIModule m_UIModule;
    
    private void Start()
    {
        m_UIModule = ModuleManager.GetModule<UIModule>();
        
        // 获取界面组
        var mainUIGroup = m_UIModule.GetUIGroup(UILayer.MainUI);
        
        // 暂停界面组
        mainUIGroup.Pause = true;
        
        // 恢复界面组
        mainUIGroup.Pause = false;
        
        // 获取当前界面组中的界面数量
        int uiCount = mainUIGroup.UICount;
        
        // 获取当前界面
        var currentView = mainUIGroup.CurrentViewBase;
    }
}
```

### 4.6 事件订阅与广播

```csharp
public class EventExampleView : ViewBase
{
    public override string UIName => "EventExample";
    
    protected override void OnInit()
    {
        // 订阅事件
        Subscribe("PlayerLevelUp", OnPlayerLevelUp);
        Subscribe("GoldChanged", OnGoldChanged);
    }
    
    protected override void OnOpen()
    {
        // 广播事件
        Broadcast(this, new GameEventArgs("UIOpened", this));
    }
    
    protected override void OnClose()
    {
        // 取消所有订阅
        UnSubscribeAll();
    }
    
    private void OnPlayerLevelUp(object sender, GameEventArgs e)
    {
        Debug.Log("玩家升级了！");
    }
    
    private void OnGoldChanged(object sender, GameEventArgs e)
    {
        Debug.Log($"金币变化: {e.Data}");
    }
}
```

### 4.7 计时器使用

```csharp
public class TimerExampleView : ViewBase
{
    public override string UIName => "TimerExample";
    
    protected override void OnOpen()
    {
        // 启动倒计时
        StartCountdownTimer(10f, () => Debug.Log("倒计时结束"));
        
        // 启动间隔计时器
        StartIntervalTimer(1f, () => Debug.Log("每秒执行"), repeatCount: 5, immediate: true);
        
        // 启动帧间隔计时器
        StartFrameTimer(30, () => Debug.Log("每30帧执行"));
    }
    
    protected override void OnPause()
    {
        // 暂停所有计时器
        PauseAllTimers();
    }
    
    protected override void OnResume()
    {
        // 恢复所有计时器
        ResumeAllTimers();
    }
    
    protected override void OnClose()
    {
        // 停止所有计时器
        StopAllTimers();
    }
}
```

### 4.8 监听界面事件

```csharp
public class UIEventListener : MonoBehaviour
{
    private UIModule m_UIModule;
    private EventModule m_EventModule;
    
    private void Start()
    {
        m_UIModule = ModuleManager.GetModule<UIModule>();
        m_EventModule = ModuleManager.GetModule<EventModule>();
        
        // 订阅界面打开成功事件
        m_EventModule.Subscribe(OpenUISuccessEventArgs.EventId, OnOpenUISuccess);
        
        // 订阅界面关闭完成事件
        m_EventModule.Subscribe(CloseUICompleteEventArgs.EventId, OnCloseUIComplete);
        
        // 订阅界面可见性变化事件
        m_EventModule.Subscribe(ChangeUIVisibleEventArgs.EventId, OnUIVisibleChanged);
    }
    
    private void OnOpenUISuccess(object sender, GameEventArgs e)
    {
        var args = e as OpenUISuccessEventArgs;
        Debug.Log($"界面打开成功: {args.ViewBase.UIName}");
    }
    
    private void OnCloseUIComplete(object sender, GameEventArgs e)
    {
        var args = e as CloseUICompleteEventArgs;
        Debug.Log($"界面关闭完成: {args.UIName}");
    }
    
    private void OnUIVisibleChanged(object sender, GameEventArgs e)
    {
        var args = e as ChangeUIVisibleEventArgs;
        Debug.Log($"界面可见性变化: {args.UIView.UIName}, 可见: {args.Visible}");
    }
    
    private void OnDestroy()
    {
        m_EventModule?.UnSubscribeAll(this);
    }
}
```

## 5. 目录结构

```
FuFramework/UI/
├── Runtime/
│   ├── UIModule.cs                    # UI管理模块主类
│   ├── UIModule.Open.cs               # 打开界面功能
│   ├── UIModule.Close.cs              # 关闭界面功能
│   ├── UIModule.Get.cs                # 获取界面功能
│   ├── UIModule.UIGroup.cs            # 界面组管理
│   ├── View/
│   │   ├── ViewBase.cs                # 界面基类
│   │   ├── ViewBase.Life.cs           # 生命周期方法
│   │   ├── ViewBase.EventRegister.cs  # 事件注册功能
│   │   ├── ViewBase.TimerRegister.cs  # 计时器功能
│   │   ├── ViewBase.UIEventRegister.cs# UI事件功能
│   │   ├── ViewInfo.cs                # 界面信息
│   │   └── ViewObject.cs              # 界面对象池对象
│   ├── Misc/
│   │   ├── UIGroup.cs                 # 界面组
│   │   ├── UILayer.cs                 # 界面层级枚举
│   │   └── UITweenType.cs             # 动画类型枚举
│   ├── Fui/
│   │   ├── FuiPkgManager.cs           # FUI包管理器
│   │   ├── FuiEventRegister.cs        # FUI事件注册器
│   │   ├── ICustomComp.cs             # 自定义组件接口
│   │   └── CustomLoader.cs            # 自定义加载器
│   ├── Event/
│   │   ├── OpenUISuccessEventArgs.cs  # 打开成功事件
│   │   ├── OpenUIFailureEventArgs.cs  # 打开失败事件
│   │   ├── CloseUICompleteEventArgs.cs# 关闭完成事件
│   │   └── ChangeUIVisibleEventArgs.cs# 可见性变化事件
│   └── FuFramework.UI.Runtime.asmdef
├── Editor/
│   ├── UITextureAssetPostprocessor.cs # 纹理资源后处理器
│   └── FuFramework.UI.Editor.asmdef
└── README.md                          # 本文档
```

## 6. 依赖模块

- **Core**: 提供 FuModule 基类、日志工具、链表等数据结构
- **Event**: 提供事件系统支持
- **Timer**: 提供计时器功能
- **ObjectPool**: 提供对象池管理
- **ReferencePool**: 提供引用池管理
- **Asset**: 提供资源加载功能
- **Localization**: 提供本地化支持
- **FairyGUI**: UI 框架
- **UniTask**: 异步任务支持

## 7. 设计特点

### 7.1 层级管理

使用 UILayer 枚举定义 7 个层级，每个层级对应一个 UIGroup：

- 层级值越大，显示在越上层
- 每个层级独立管理自己的界面列表
- 支持层级暂停/恢复

### 7.2 界面生命周期

完整的 8 个生命周期阶段：

1. **OnInit**：初始化，只执行一次
2. **OnOpen**：打开，每次打开执行
3. **OnUpdate**：更新，每帧执行
4. **OnPause**：暂停，界面被暂停时执行
5. **OnResume**：恢复，从暂停恢复时执行
6. **OnBeCover**：被覆盖，被其他界面遮挡时执行
7. **OnReveal**：恢复显示，从被遮挡恢复时执行
8. **OnClose**：关闭，界面关闭时执行

### 7.3 对象池管理

- 界面实例使用 ObjectPool 管理
- 支持设置锁定和优先级
- 自动释放策略减少内存占用

### 7.4 包管理

- 使用引用计数管理包生命周期
- 支持依赖包自动加载
- 异步加载避免阻塞主线程

### 7.5 动画系统

三种动画类型：

- **None**：无动画
- **Fade**：淡入淡出
- **Custom**：自定义动画（重写方法实现）

### 7.6 事件系统

- 内置 EventRegister 管理业务事件
- FuiEventRegister 管理 UI 事件
- 全局事件广播机制

## 8. 应用场景

1. **游戏主界面系统**：主菜单、设置、商店等
2. **HUD 系统**：血条、小地图、技能栏等
3. **弹窗系统**：提示、确认框、奖励展示等
4. **Loading 界面**：场景切换、资源加载等
5. **引导系统**：新手引导、功能引导等

## 9. 注意事项

1. **线程安全**：所有 UI 操作应在主线程执行
2. **资源管理**：及时关闭不再使用的界面，释放资源
3. **对象池配置**：合理配置对象池参数，平衡内存和性能
4. **包引用计数**：注意包的引用计数管理，避免内存泄漏
5. **动画回调**：自定义动画需要正确调用回调
6. **事件清理**：界面关闭时清理所有事件监听

## 10. 常见问题

### Q1: 如何传递数据给界面？

A: 使用 userData 参数：

```csharp
var data = new PlayerData { Name = "Player1", Level = 10 };
m_UIModule.OpenUI<PlayerInfoView>(data);
```

在 OnInit 或 OnOpen 中通过 UserData 属性获取：

```csharp
protected override void OnOpen()
{
    var data = UserData as PlayerData;
    // 使用数据更新界面
}
```

### Q2: 如何实现界面栈管理？

A: 使用 CloseSelf() 关闭当前界面，框架会自动处理被覆盖界面的恢复：

```csharp
// 在按钮回调中
private void OnBackButtonClick()
{
    CloseSelf(); // 关闭当前界面，自动恢复上一个界面
}
```

### Q3: 如何预加载 UI 包？

A: 使用 PkgManager：

```csharp
await m_UIModule.PkgManager.AddPackageAsync("Main");
```

### Q4: 如何获取当前最顶部的界面？

A: 使用 GetTopUI：

```csharp
// 获取指定层级的顶部界面
var topView = m_UIModule.GetTopUI(UILayer.Window);

// 获取所有层级中最顶部的界面
var topView = m_UIModule.GetTopUI();
```
