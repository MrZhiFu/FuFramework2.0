# FuFramework UI Module

## 概述

UI 模块是 FuFramework 中的用户界面管理系统，基于 FairyGUI 实现，提供完整的 UI 生命周期管理、层级管理、资源加载和动画效果。该模块采用模块化设计，支持界面分组、对象池复用、异步加载等高级功能。

### 核心特性

- **基于 FairyGUI**：强大的 UI 框架支持，跨平台兼容
- **完整的生命周期管理**：初始化、打开、关闭、暂停、恢复等完整流程
- **多层级的 UI 分组**：支持世界UI、主界面、普通界面、窗口、提示、引导、Loading 等层级
- **对象池管理**：界面实例对象池，减少内存分配和GC压力
- **异步资源加载**：基于 YooAsset 的异步 UI 包加载机制
- **动画效果支持**：内置淡入淡出动画，支持自定义动画效果
- **事件驱动架构**：完整的界面打开/关闭事件通知机制
- **模块化设计**：支持界面组件化和自定义组件扩展

## 系统架构

### 核心类说明

#### 1. UIManager
UI 管理器，继承自 FuModule，负责所有 UI 界面的统一管理。

#### 2. ViewBase
界面基类，所有 UI 界面必须继承此类，定义界面的生命周期和基础功能。

#### 3. UIGroup
界面组，管理同一层级下的多个界面，支持暂停、恢复等操作。

#### 4. FuiPackageManager
FairyGUI 包管理器，负责 UI 包的加载、缓存和卸载管理。

#### 5. UIInfo
界面信息类，记录界面在组中的状态信息。

#### 6. UILayer
界面层级枚举：WorldUI、MainUI、Normal、Window、Tip、Guide、Loading。

#### 7. UITweenType
界面动画类型枚举：None、Fade、Custom。

### 技术架构图

```
UIManager
├── m_UIGroupDict (界面组字典)
├── m_LoadingDict (加载中界面字典)
├── m_WaitRecycleQueue (待回收界面队列)
├── m_InstancePool (界面实例对象池)
└── 核心方法
    ├── OpenUI() / OpenUIAsync() 打开界面
    ├── CloseUI() / CloseUINow() 关闭界面
    ├── GetUI() / GetUIs() 获取界面
    ├── AddUIGroup() 添加界面组
    └── Update() 界面轮询更新

ViewBase (抽象基类)
├── 生命周期方法
│   ├── OnInit() 初始化
│   ├── OnOpen() 打开
│   ├── OnUpdate() 更新
│   ├── OnClose() 关闭
│   └── OnPause()/OnResume() 暂停/恢复
├── 动画方法
│   ├── OnCustomTweenOpen() 自定义打开动画
│   └── OnCustomTweenClose() 自定义关闭动画
└── 事件注册
    ├── EventRegister 事件注册器
    └── TimerRegister 计时器注册器
```

## 快速开始

### 基本使用

#### 1. 创建自定义 UI 界面

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
    protected override bool IsFullScreen => true;
    
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
        m_StartButton.onClick.Add(OnStartButtonClick);
        m_SettingButton.onClick.Add(OnSettingButtonClick);
        
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
        // 打开游戏场景或开始游戏逻辑
    }
    
    private void OnSettingButtonClick()
    {
        Debug.Log("设置按钮点击");
        // 打开设置界面
        var uiManager = ModuleManager.GetModule<UIManager>();
        uiManager.OpenUI<SettingUIView>();
    }
}
```

#### 2. 打开和关闭界面

```csharp
using FuFramework.UI.Runtime;
using UnityEngine;

public class GameController : MonoBehaviour
{
    private UIManager m_UIManager;
    
    private void Start()
    {
        // 获取 UI 管理器
        m_UIManager = ModuleManager.GetModule<UIManager>();
        
        // 打开主界面
        OpenMainUI();
        
        // 异步打开设置界面
        OpenSettingAsync();
    }
    
    private void OpenMainUI()
    {
        // 同步打开主界面
        m_UIManager.OpenUI<MainUIView>();
    }
    
    private async void OpenSettingAsync()
    {
        // 异步打开设置界面，并获取界面实例
        var settingView = await m_UIManager.OpenUIAsync<SettingUIView>();
        if (settingView != null)
        {
            Debug.Log("设置界面打开成功");
        }
    }
    
    private void CloseMainUI()
    {
        // 关闭主界面
        m_UIManager.CloseUI<MainUIView>();
    }
    
    private void OnDestroy()
    {
        // 关闭所有界面
        m_UIManager?.CloseAllUI();
    }
}
```

## 详细使用指南

### 界面生命周期管理

#### 1. 完整的生命周期流程

```csharp
public class CustomUIView : ViewBase
{
    public override string UIName => "CustomUI";
    public override string PackageName => "Common";
    
    // 初始化 - 只执行一次
    protected override void OnInit()
    {
        Debug.Log("界面初始化");
        // 初始化组件、事件注册等
    }
    
    // 打开 - 每次打开界面时执行
    protected override void OnOpen()
    {
        Debug.Log("界面打开");
        // 数据刷新、动画播放等
    }
    
    // 更新 - 每帧执行
    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        // 界面逻辑更新
    }
    
    // 暂停 - 界面被暂停时执行
    protected override void OnPause()
    {
        Debug.Log("界面暂停");
        // 暂停动画、计时器等
    }
    
    // 恢复 - 界面从暂停状态恢复时执行
    protected override void OnResume()
    {
        Debug.Log("界面恢复");
        // 恢复动画、计时器等
    }
    
    // 被遮挡 - 界面被其他界面遮挡时执行
    protected override void OnBeCover()
    {
        Debug.Log("界面被遮挡");
    }
    
    // 被遮挡恢复 - 界面从被遮挡状态恢复时执行
    protected override void OnReveal()
    {
        Debug.Log("界面被遮挡恢复");
    }
    
    // 关闭 - 界面关闭时执行
    protected override void OnClose()
    {
        Debug.Log("界面关闭");
        // 清理资源、取消事件注册等
    }
}
```

#### 2. 界面动画效果

```csharp
public class AnimatedUIView : ViewBase
{
    public override string UIName => "AnimatedUI";
    protected override UITweenType TweenType => UITweenType.Custom;
    
    // 自定义打开动画
    protected override void OnCustomTweenOpen()
    {
        // 缩放动画
        UIView.scale = Vector2.zero;
        UIView.TweenScale(Vector2.one, TweenDuration)
              .SetEase(EaseType.BackOut)
              .OnComplete(OnOpen);
    }
    
    // 自定义关闭动画
    protected override void OnCustomTweenClose()
    {
        // 旋转缩放动画
        UIView.TweenScale(Vector2.zero, TweenDuration)
              .SetEase(EaseType.BackIn)
              .OnComplete(() =>
              {
                  UIView.visible = false;
                  OnClose();
              });
    }
}
```

### 界面层级管理

#### 1. 使用不同的 UI 层级

```csharp
// 世界UI - HUD、血条等
public class HUDView : ViewBase
{
    protected override UILayer Layer => UILayer.WorldUI;
    protected override bool IsFullScreen => false;
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
    protected override bool IsFullScreen => false;
}

// 提示界面
public class ToastView : ViewBase
{
    protected override UILayer Layer => UILayer.Tip;
    protected override bool IsFullScreen => false;
}

// Loading界面
public class LoadingView : ViewBase
{
    protected override UILayer Layer => UILayer.Loading;
}
```

#### 2. 界面组操作

```csharp
public class UIGroupExample : MonoBehaviour
{
    private UIManager m_UIManager;
    
    private void Start()
    {
        m_UIManager = ModuleManager.GetModule<UIManager>();
        
        // 获取界面组
        var mainUIGroup = m_UIManager.GetUIGroup(UILayer.MainUI);
        
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

### 高级功能

#### 1. 事件注册器

```csharp
public class EventUIView : ViewBase
{
    private EventRegister m_EventRegister;
    
    protected override void OnInit()
    {
        m_EventRegister = EventRegister.Create();
        
        // 注册事件
        m_EventRegister.Register<PlayerLevelUpEventArgs>(OnPlayerLevelUp);
        m_EventRegister.Register<ItemObtainedEventArgs>(OnItemObtained);
    }
    
    protected override void OnClose()
    {
        // 清理事件注册
        m_EventRegister?.UnregisterAll();
        ReferencePool.Runtime.ReferencePool.Release(m_EventRegister);
    }
    
    private void OnPlayerLevelUp(PlayerLevelUpEventArgs args)
    {
        Debug.Log($"玩家升级到 {args.Level} 级");
        // 更新UI显示
    }
    
    private void OnItemObtained(ItemObtainedEventArgs args)
    {
        Debug.Log($"获得物品: {args.ItemName}");
        // 显示获得物品提示
    }
}
```

#### 2. 计时器注册器

```csharp
public class TimerUIView : ViewBase
{
    private TimerRegister m_TimerRegister;
    
    protected override void OnInit()
    {
        m_TimerRegister = TimerRegister.Create();
        
        // 启动计时器
        m_TimerRegister.StartTimer(5f, OnTimerComplete);
        m_TimerRegister.StartTimeTimer(1f, OnSecondTick, -1, true);
    }
    
    protected override void OnClose()
    {
        // 清理计时器
        ReferencePool.Runtime.ReferencePool.Release(m_TimerRegister);
    }
    
    private void OnTimerComplete()
    {
        Debug.Log("5秒计时器完成");
    }
    
    private void OnSecondTick()
    {
        // 每秒执行一次
        Debug.Log("秒计时器触发");
    }
}
```

#### 3. 自定义组件

```csharp
using FuFramework.UI.Runtime;

// 自定义组件接口
public interface ICustomComp
{
    void Init(GComponent view);
    void Dispose();
}

// 自定义组件实现
public class CustomButtonComp : ICustomComp
{
    private GButton m_Button;
    private System.Action m_OnClick;
    
    public void Init(GComponent view)
    {
        m_Button = view.GetChild("custom_btn") as GButton;
        m_Button.onClick.Add(OnButtonClick);
    }
    
    public void Dispose()
    {
        m_Button?.onClick.Remove(OnButtonClick);
    }
    
    public void SetClickCallback(System.Action onClick)
    {
        m_OnClick = onClick;
    }
    
    private void OnButtonClick()
    {
        m_OnClick?.Invoke();
    }
}

// 使用自定义组件的界面
public class CustomCompUIView : ViewBase
{
    private CustomButtonComp m_CustomButton;
    
    protected override void OnInit()
    {
        m_CustomButton = new CustomButtonComp();
        m_CustomButton.Init(UIView);
        m_CustomButton.SetClickCallback(OnCustomButtonClick);
    }
    
    protected override void OnClose()
    {
        m_CustomButton?.Dispose();
    }
    
    private void OnCustomButtonClick()
    {
        Debug.Log("自定义按钮点击");
    }
}
```

## 实际应用场景

### 1. 游戏主界面系统

```csharp
public class GameUISystem : MonoBehaviour
{
    private UIManager m_UIManager;
    
    private void Start()
    {
        m_UIManager = ModuleManager.GetModule<UIManager>();
        
        // 打开游戏主界面
        OpenGameUI();
    }
    
    private async void OpenGameUI()
    {
        // 打开Loading界面
        var loadingView = await m_UIManager.OpenUIAsync<LoadingView>();
        
        // 模拟资源加载
        await LoadGameResources();
        
        // 关闭Loading界面
        m_UIManager.CloseUI<LoadingView>();
        
        // 打开主界面
        m_UIManager.OpenUI<MainUIView>();
        
        // 打开HUD界面
        m_UIManager.OpenUI<HUDView>();
    }
    
    private async UniTask LoadGameResources()
    {
        // 模拟资源加载过程
        await UniTask.Delay(2000);
    }
    
    public void OpenShop()
    {
        // 打开商店界面
        m_UIManager.OpenUI<ShopView>();
    }
    
    public void ShowToast(string message)
    {
        // 显示提示信息
        // 实际实现中需要创建Toast界面并显示消息
    }
}
```

### 2. 界面栈管理

```csharp
public class UIStackManager
{
    private UIManager m_UIManager;
    private Stack<Type> m_UIStack = new Stack<Type>();
    
    public void PushUI<T>() where T : ViewBase
    {
        // 暂停当前界面
        if (m_UIStack.Count > 0)
        {
            var currentType = m_UIStack.Peek();
            // 实际实现中需要获取当前界面并暂停
        }
        
        // 打开新界面
        m_UIManager.OpenUI<T>();
        m_UIStack.Push(typeof(T));
    }
    
    public void PopUI()
    {
        if (m_UIStack.Count == 0) return;
        
        // 关闭当前界面
        var currentType = m_UIStack.Pop();
        // 实际实现中需要根据类型关闭界面
        
        // 恢复上一个界面
        if (m_UIStack.Count > 0)
        {
            var previousType = m_UIStack.Peek();
            // 实际实现中需要恢复上一个界面
        }
    }
}
```

### 3. 界面数据绑定

```csharp
public class DataBindingUIView : ViewBase
{
    private GTextField m_PlayerNameText;
    private GProgressBar m_HealthBar;
    private GLoader m_AvatarLoader;
    
    protected override void OnInit()
    {
        m_PlayerNameText = UIView.GetChild("player_name") as GTextField;
        m_HealthBar = UIView.GetChild("health_bar") as GProgressBar;
        m_AvatarLoader = UIView.GetChild("avatar") as GLoader;
        
        // 注册数据更新事件
        var eventManager = ModuleManager.GetModule<EventManager>();
        eventManager.Register<PlayerDataUpdatedEventArgs>(OnPlayerDataUpdated);
    }
    
    protected override void OnClose()
    {
        // 取消事件注册
        var eventManager = ModuleManager.GetModule<EventManager>();
        eventManager.Unregister<PlayerDataUpdatedEventArgs>(OnPlayerDataUpdated);
    }
    
    private void OnPlayerDataUpdated(PlayerDataUpdatedEventArgs args)
    {
        // 更新UI显示
        m_PlayerNameText.text = args.PlayerName;
        m_HealthBar.value = args.Health / args.MaxHealth * 100;
        m_AvatarLoader.url = args.AvatarUrl;
    }
}
```

## 性能优化建议

### 1. 合理使用界面对象池

```csharp
public class OptimizedUIExample : MonoBehaviour
{
    private UIManager m_UIManager;
    
    private void Start()
    {
        m_UIManager = ModuleManager.GetModule<UIManager>();
        
        // 配置界面实例对象池参数
        m_UIManager.InstanceCapacity = 20;           // 对象池容量
        m_UIManager.InstanceExpireTime = 300f;       // 对象过期时间（秒）
        m_UIManager.InstanceAutoReleaseInterval = 60f; // 自动释放间隔
    }
    
    public void ShowFrequentUI()
    {
        // 频繁使用的界面使用对象池
        m_UIManager.OpenUI<ToastView>();
        
        // 3秒后自动关闭
        StartCoroutine(CloseToastAfterDelay());
    }
    
    private System.Collections.IEnumerator CloseToastAfterDelay()
    {
        yield return new WaitForSeconds(3f);
        m_UIManager.CloseUI<ToastView>();
    }
}
```

### 2. 异步加载优化

```csharp
public class AsyncLoadingExample : MonoBehaviour
{
    private UIManager m_UIManager;
    private FuiPackageManager m_PackageManager;
    
    private async void Start()
    {
        m_UIManager = ModuleManager.GetModule<UIManager>();
        m_PackageManager = ModuleManager.GetModule<FuiPackageManager>();
        
        // 预加载常用UI包
        await PreloadUIPackages();
        
        // 打开界面（无需等待包加载）
        m_UIManager.OpenUI<MainUIView>();
    }
    
    private async UniTask PreloadUIPackages()
    {
        var packages = new[] { "Common", "Main", "Battle" };
        
        foreach (var package in packages)
        {
            await m_PackageManager.AddPackageAsync(package);
        }
    }
}
```

### 3. 界面层级优化

```csharp
// 合理分配界面层级，避免层级冲突
public class LayerOptimization
{
    // 静态界面使用较低层级
    public class StaticUIView : ViewBase
    {
        protected override UILayer Layer => UILayer.WorldUI;
    }
    
    // 动态界面使用适当层级
    public class DynamicUIView : ViewBase
    {
        protected override UILayer Layer => UILayer.Normal;
    }
    
    // 临时界面使用较高层级
    public class TemporaryUIView : ViewBase
    {
        protected override UILayer Layer => UILayer.Tip;
    }
}
```

## API 参考

### UIManager 主要方法

| 方法 | 描述 | 参数 | 返回值 |
|------|------|------|--------|
| `OpenUI<T>()` | 打开界面 | userData: 用户数据, isMultiple: 是否允许多实例 | void |
| `OpenUIAsync<T>()` | 异步打开界面 | userData: 用户数据, isMultiple: 是否允许多实例 | UniTask<T> |
| `CloseUI<T>()` | 关闭界面 | - | void |
| `CloseUI(int)` | 按序列号关闭界面 | serialId: 界面序列号 | void |
| `CloseUINow<T>()` | 立即关闭界面 | - | void |
| `GetUI<T>()` | 获取界面 | - | T |
| `GetUIs<T>()` | 获取所有同类型界面 | - | T[] |
| `HasUI<T>()` | 检查界面是否存在 | - | bool |
| `CloseAllUI()` | 关闭所有界面 | - | void |
| `AddUIGroup()` | 添加界面组 | layer: 界面层级 | bool |
| `GetUIGroup()` | 获取界面组 | layer: 界面层级 | UIGroup |

### ViewBase 主要属性和方法

| 属性/方法 | 描述 | 类型 |
|-----------|------|------|
| `UIName` | 界面名称 | string |
| `PackageName` | UI包名称 | string |
| `Layer` | 界面层级 | UILayer |
| `IsFullScreen` | 是否全屏界面 | bool |
| `TweenType` | 动画类型 | UITweenType |
| `TweenDuration` | 动画时长 | float |
| `OnInit()` | 初始化方法 | protected virtual void |
| `OnOpen()` | 打开方法 | protected virtual void |
| `OnUpdate()` | 更新方法 | protected virtual void |
| `OnClose()` | 关闭方法 | protected virtual void |
| `OnPause()` | 暂停方法 | protected virtual void |
| `OnResume()` | 恢复方法 | protected virtual void |

### UIGroup 主要属性和方法

| 属性/方法 | 描述 | 类型 |
|-----------|------|------|
| `Pause` | 界面组是否暂停 | bool |
| `UICount` | 界面数量 | int |
| `CurrentViewBase` | 当前界面 | ViewBase |
| `OnUpdate()` | 界面组更新 | void |
| `HasUI()` | 检查界面是否存在 | bool |
| `Refresh()` | 刷新界面组状态 | void |

## 注意事项

### 1. 内存管理
- 使用对象池管理界面实例，避免频繁创建销毁
- 及时关闭不再使用的界面
- 合理配置对象池参数，平衡内存使用和性能

### 2. 性能考虑
- 预加载常用UI包，减少运行时加载延迟
- 合理使用界面层级，避免层级冲突和过度绘制
- 避免在界面更新中进行耗时操作

### 3. 生命周期管理
- 确保界面生命周期方法的正确实现
- 在合适的时间注册和取消事件监听
- 及时清理界面资源，避免内存泄漏

### 4. 异步操作
- 使用异步方法打开界面，避免阻塞主线程
- 正确处理异步操作中的异常情况
- 使用取消令牌管理长时间运行的异步操作

## 常见问题解答

### Q: 如何选择合适的界面层级？
A: 根据界面类型和显示需求选择：
- **WorldUI**：世界场景中的UI，如HUD、血条等
- **MainUI**：游戏主界面，如菜单、主城界面
- **Normal**：普通全屏界面，如战斗界面、设置界面
- **Window**：窗口界面，如商店、背包
- **Tip**：提示信息，如Toast、弹窗
- **Guide**：引导界面
- **Loading**：加载界面

### Q: 界面动画如何自定义？
A: 重写 `OnCustomTweenOpen()` 和 `OnCustomTweenClose()` 方法：
```csharp
protected override void OnCustomTweenOpen()
{
    // 自定义打开动画
    UIView.TweenScale(Vector2.one, TweenDuration)
          .SetEase(EaseType.BackOut)
          .OnComplete(OnOpen);
}

protected override void OnCustomTweenClose()
{
    // 自定义关闭动画
    UIView.TweenScale(Vector2.zero, TweenDuration)
          .SetEase(EaseType.BackIn)
          .OnComplete(() =>
          {
              UIView.visible = false;
              OnClose();
          });
}
```

### Q: 如何处理界面间的数据传递？
A: 使用 `userData` 参数或事件系统：
```csharp
// 方法1：使用userData
var userData = new { playerName = "张三", level = 10 };
m_UIManager.OpenUI<PlayerInfoView>(userData);

// 方法2：使用事件系统
var eventManager = ModuleManager.GetModule<EventManager>();
eventManager.Broadcast(this, new PlayerDataUpdatedEventArgs(playerData));
```

### Q: 如何实现界面栈管理？
A: 使用栈结构管理界面打开顺序：
```csharp
public class UIStack
{
    private Stack<Type> m_Stack = new Stack<Type>();
    
    public void Push<T>() where T : ViewBase
    {
        // 暂停当前界面，打开新界面
        m_Stack.Push(typeof(T));
    }
    
    public void Pop()
    {
        // 关闭当前界面，恢复上一个界面
        m_Stack.Pop();
    }
}
```

### Q: 界面资源如何管理？
A: 使用 `FuiPackageManager` 进行包管理：
```csharp
var packageManager = ModuleManager.GetModule<FuiPackageManager>();

// 加载包
await packageManager.AddPackageAsync("Main");

// 检查包是否存在
bool hasPackage = packageManager.HasPackage("Main");

// 释放包（当引用计数为0时）
packageManager.ReleasePackage("Main");
```