# UI 模块重构：移除 ICustomComp，组件自持 Register

## 概述

移除 `ICustomComp` 接口和 `ViewBase.InitChildrenView()` 自动扫描机制，改为自定义组件自行持有 `FuiEventRegister`、`EventRegister`、`TimerRegister`，在 FairyGUI 生命周期 `ConstructFromXML` / `Dispose` 中自行创建和释放。

## 动机

1. **消除递归扫描开销**：当前 `ViewBase.Init()` 中调用 `InitChildrenView()` 递归遍历 FairyGUI 组件树查找 `ICustomComp`，每个 UI 打开都执行一次，纯运行时开销
2. **消除不必要耦合**：自定义组件持有 `uiView` 引用仅用于代理事件/定时器操作，组件完全可以自己管理这些 Register
3. **统一列表项行为**：动态创建的 GList 列表项无法被 `InitChildrenView()` 扫描到，需要在渲染回调中手动调用 `Init(this)`；改为自持 Register 后，列表项在 `ConstructFromXML` 中即完成初始化，无需额外处理
4. **简化代码生成器输出**：删除所有代理方法（`AddUIListener` / `Subscribe` / `StartCountdownTimer` 等约 170 行），`Comp*.Gen.cs` 大幅精简

## 设计方案

### 一、C# 侧改动

#### 1.1 删除 `ICustomComp.cs`

```
Unity/Assets/Scripts/Hotfix/Framework/UI/Fui/ICustomComp.cs → 删除
```

#### 1.2 修改 `ViewBase.cs`

- 删除 `InitChildrenView(GComponent curComp)` 方法（约 16 行）
- 删除 `Init()` 方法中对它的调用（L149）：`InitChildrenView(UIView);`

### 二、代码生成器改动

#### 2.1 `CompGenTemplate.txt` — 重写

**类声明**：移除 `ICustomComp` 接口

```diff
- public partial class #COMPNAME# : #COMPTYPE#, ICustomComp
+ public partial class #COMPNAME# : #COMPTYPE#
```

**字段**：用三个 Register 替换 `uiView`

```diff
- private ViewBase uiView;
+ private FuiEventRegister m_UIEventRegister;
+ private EventRegister m_EventRegister;
+ private TimerRegister m_TimerRegister;
```

**`ConstructFromXML`**：在 `InitUIComp()` 之后插入 Register 的创建，随后调用 `InitUIEvent()` 和 `OnInit()`

```csharp
public override void ConstructFromXML(XML xml)
{
    base.ConstructFromXML(xml);
    InitUIComp();
    m_UIEventRegister = FuiEventRegister.Create();
    m_EventRegister = EventRegister.Create();
    m_TimerRegister = TimerRegister.Create();
    InitUIEvent();
    OnInit();
}
```

**删除 `Init(ViewBase view)` 方法**（整个方法删除，约 15 行）

**`Dispose`**：释放 Register 替代 `uiView = null`

```diff
- FuLogger.LogInfo($"销毁{uiView.UIName}界面组件-{GetType().Name}");
+ FuLogger.LogInfo($"销毁界面组件-{GetType().Name}");
  OnDispose();
- uiView = null;
+ m_UIEventRegister?.Release();
+ m_EventRegister?.Release();
+ m_TimerRegister?.Release();
  base.Dispose();
```

**代理方法**：全部从 `uiView?.Xxx(...)` 改为调用自有 Register

| 区域 | 旧实现 | 新实现 |
|------|--------|--------|
| `AddUIListener` | `uiView?.AddUIListener(...)` | `m_UIEventRegister.AddUIListener(...)` |
| `SetUIListener` | `uiView?.SetUIListener(...)` | `m_UIEventRegister.SetUIListener(...)` |
| `RemoveUIListener` | `uiView?.RemoveUIListener(...)` | `m_UIEventRegister.RemoveUIListener(...)` |
| `ClearUIListener` | `uiView?.ClearUIListener(...)` | `m_UIEventRegister.ClearUIListener(...)` |
| `ClearAllUIListener` | `uiView?.ClearAllUIListener()` | `m_UIEventRegister.ClearAllUIListener()` |
| `Subscribe` | `uiView?.Subscribe(...)` | `m_EventRegister.Subscribe(...)` |
| `UnSubscribe` | `uiView?.UnSubscribe(...)` | `m_EventRegister.UnSubscribe(...)` |
| `Broadcast` (3 个重载) | `uiView?.Broadcast(...)` | `m_EventRegister.Broadcast(...)` |
| `BroadcastNow` | `uiView?.BroadcastNow(...)` | `m_EventRegister.BroadcastNow(...)` |
| `UnSubscribeAll` | `uiView?.UnSubscribeAll()` | `m_EventRegister.UnSubscribeAll()` |
| `StartCountdownTimer` | `uiView?.StartCountdownTimer(...)` | `m_TimerRegister.StartCountdownTimer(...)` |
| `StartIntervalTimer` | `uiView?.StartIntervalTimer(...)` | `m_TimerRegister.StartIntervalTimer(...)` |
| `StartFrameTimer` | `uiView?.StartFrameTimer(...)` | `m_TimerRegister.StartFrameTimer(...)` |
| `PauseTimer` | `uiView?.PauseTimer(...)` | `m_TimerRegister.PauseTimer(...)` |
| `ResumeTimer` | `uiView?.ResumeTimer(...)` | `m_TimerRegister.ResumeTimer(...)` |
| `StopTimer` | `uiView.StopTimer(...)` | `m_TimerRegister.StopTimer(...)` |
| `PauseAllTimers` | `uiView?.PauseAllTimers()` | `m_TimerRegister.PauseAllTimers()` |
| `ResumeAllTimers` | `uiView?.ResumeAllTimers()` | `m_TimerRegister.ResumeAllTimers()` |
| `StopAllTimers` | `uiView?.StopAllTimers()` | `m_TimerRegister.StopAllTimers()` |
| `IsTimerExist` | `uiView != null && uiView.IsTimerExist(...)` | `m_TimerRegister.IsTimerExist(...)` |
| `IsTimerPaused` | `uiView != null && uiView.IsTimerPaused(...)` | `m_TimerRegister.IsTimerPaused(...)` |

> 注意：代理方法中不再使用 `?.` 空传播，因为 Register 在 `ConstructFromXML` 中保证创建。
> 但如果组件在构造前就触发了某些回调，保留 `?.` 是一种防御。评估后认为 `ConstructFromXML` 是 FairyGUI 内部调用的第一个方法，不会有提前触发的场景，故使用直接调用。

**using 修复**：

```diff
- using FuFramework.UI.Runtime;
+ using Hotfix.Framework.UI;
- using FuFramework.Event.Runtime;
+ using Hotfix.Framework.Event;
```

同时确认 `using Hotfix.Framework.Core;`、`using AOT.Framework.Core.Log;` 等实际需要的命名空间在模板中正确列出。

#### 2.2 `CompTemplate.txt` — 注释微调

`OnDispose` 注释修改：

```diff
- /// 注意：UI事件，业务逻辑事件，计时器会自动从所属的View中移除，无需在这里手动移除。
+ /// 注意：UI事件，业务逻辑事件，计时器在 Dispose 中统一释放，无需在这里手动移除。
```

#### 2.3 `WinTemplate.txt` — 修复旧命名空间

```diff
- using FuFramework.UI.Runtime;
+ using Hotfix.Framework.UI;
```

#### 2.4 `GenCommon.lua` — 删除列表项 Init 调用

`GenListOnRenderHandler` 函数中（约 L419）：

```diff
  \t\t\tcompItem.Init(this);\n
```

删除这一行。列表项在 `ConstructFromXML` 中已完成自初始化。

### 三、重新导出

修改完成后，在 FairyGUI Editor 中对所有包重新执行"发布"操作：

- 旧的 `Comp*.Gen.cs` 被自动覆盖为新版本
- 手写 `Comp*.cs` 文件不受影响（生成器检测到文件已存在则跳过）
- `CustomCompBind.cs` 不受影响（绑定逻辑不变）

### 四、需重新导出的包

当前项目中包含自定义组件（`Comp*`）的包：

- **Bag** — CompBagItem, CompBagContent, CompBagItemInfo, CompTypeItem, CompGoodItem
- **Common** — CompRedDot
- **Login** — CompPlayerListItem

## 影响面分析

| 影响范围 | 说明 |
|----------|------|
| `ICustomComp.cs` | 删除，无其他引用方（仅 `ViewBase.InitChildrenView` 和生成代码使用） |
| `ViewBase.cs` | 删除 `InitChildrenView` 方法，无外部调用者 |
| `Comp*.Gen.cs` | 重新导出后自动更新，无需手动修改 |
| `Comp*.cs` | **不受影响**，`OnInit()` / `OnDispose()` 签名和行为不变 |
| `Win*.cs` | 不受影响，Win 类从未实现 ICustomComp |
| `Win*.Gen.cs` | 不受影响 |
| `CustomCompBind.cs` | 不受影响 |

## 风险评估

- **低风险**：改动集中在模板和框架层，业务代码（`Comp*.cs`、`Win*.cs`）无需修改
- **回归验证**：重新导出后编译通过 + Unity Editor 中打开各 UI 界面确认组件交互正常
- **回滚方案**：git revert + 重新从旧模板导出即可恢复
