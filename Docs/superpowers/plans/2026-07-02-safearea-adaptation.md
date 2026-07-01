# 横屏游戏安全区（刘海屏）适配 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为 FuFramework 横屏游戏实现安全区（刘海屏/打孔屏）适配，使 UI 可自动避让或覆盖安全区。

**Architecture:** 新增 `SafeAreaHelper` 封装 Unity `Screen.safeArea` 并提供设计坐标转换；新增 `IgnoreSafeArea()` 扩展方法覆盖安全区；修改 `ViewBase` 将 `IsFullScreen` 重命名为 `IgnoreSafeArea` 并自动约束非全屏 UI 到安全区内。

**Tech Stack:** C#, Unity 2022.3, FairyGUI, FuFramework

---

## 文件结构

| 文件 | 操作 | 职责 |
|------|------|------|
| `UI/Runtime/Utility/SafeAreaHelper.cs` | 新增 | 封装安全区数据，设计坐标转换，方向变化事件 |
| `UI/Runtime/Utility/GObjectSafeAreaExt.cs` | 新增 | `IgnoreSafeArea()` 扩展方法 |
| `UI/Runtime/View/ViewBase.cs` | 修改 | `IsFullScreen` → `IgnoreSafeArea`；增加安全区约束逻辑 |
| `UI/Runtime/View/ViewBase.Life.cs` | 修改 | `IsFullScreen` → `IgnoreSafeArea` 引用更新 |
| `UI/Runtime/UIModule.cs` | 修改 | `OnInit()` 中调用 `SafeAreaHelper.Refresh()` |
| `UI/README.md` | 修改 | 文档中 `IsFullScreen` → `IgnoreSafeArea` |

---

### Task 1: 创建 SafeAreaHelper.cs

**Files:**
- Create: `Unity/Assets/Scripts/FuFramework/UI/Runtime/Utility/SafeAreaHelper.cs`

- [ ] **Step 1: 编写 SafeAreaHelper 完整代码**

```csharp
using System;
using UnityEngine;

namespace FuFramework.UI.Runtime
{
    /// <summary>
    /// 安全区辅助工具。
    /// 封装 Unity Screen.safeArea，提供 FairyGUI 设计坐标下的安全区偏移量，并支持方向变化通知。
    /// </summary>
    public static class SafeAreaHelper
    {
        /// <summary>
        /// 当前安全区（设计坐标）。
        /// </summary>
        public static Rect Current { get; private set; }

        /// <summary>
        /// 左侧安全区偏移（设计坐标）。
        /// </summary>
        public static float LeftInset => Current.x;

        /// <summary>
        /// 右侧安全区偏移（设计坐标）。
        /// </summary>
        public static float RightInset { get; private set; }

        /// <summary>
        /// 顶部安全区偏移（设计坐标）。
        /// </summary>
        public static float TopInset => Current.y;

        /// <summary>
        /// 底部安全区偏移（设计坐标）。
        /// </summary>
        public static float BottomInset { get; private set; }

        /// <summary>
        /// 全屏宽度（设计坐标，含安全区）。
        /// </summary>
        public static float FullWidth { get; private set; }

        /// <summary>
        /// 全屏高度（设计坐标，含安全区）。
        /// </summary>
        public static float FullHeight { get; private set; }

        /// <summary>
        /// 安全区变化事件（方向切换、折叠屏等）。
        /// </summary>
        public static event Action OnSafeAreaChanged;

        private static Rect m_LastSafeArea;

        /// <summary>
        /// 刷新安全区数据。应在模块初始化时调用一次，后续通过 PollUpdate 自动检测变化。
        /// </summary>
        public static void Refresh()
        {
            Rect safeArea   = Screen.safeArea;
            float scaleFactor = FairyGUI.UIContentScaler.scaleFactor;
            if (scaleFactor <= 0) scaleFactor = 1;

            Current      = new Rect(safeArea.x / scaleFactor, safeArea.y / scaleFactor,
                                    safeArea.width / scaleFactor, safeArea.height / scaleFactor);
            RightInset   = (Screen.width  - safeArea.xMax) / scaleFactor;
            BottomInset  = (Screen.height - safeArea.yMax) / scaleFactor;
            FullWidth    = Screen.width  / scaleFactor;
            FullHeight   = Screen.height / scaleFactor;
            m_LastSafeArea = safeArea;
        }

        /// <summary>
        /// 每帧检测安全区是否变化（方向切换等）。
        /// 由 UIModule 驱动调用。
        /// </summary>
        public static void PollUpdate()
        {
            if (Screen.safeArea == m_LastSafeArea) return;
            Refresh();
            OnSafeAreaChanged?.Invoke();
        }
    }
}
```

- [ ] **Step 2: 验证文件已创建，编译无语法错误**

在 Unity Editor 中打开项目，确认 `SafeAreaHelper.cs` 编译通过（Console 无报错）。

---

### Task 2: 创建 GObjectSafeAreaExt.cs

**Files:**
- Create: `Unity/Assets/Scripts/FuFramework/UI/Runtime/Utility/GObjectSafeAreaExt.cs`

- [ ] **Step 1: 编写扩展方法完整代码**

```csharp
using FairyGUI;

namespace FuFramework.UI.Runtime
{
    /// <summary>
    /// 为 FairyGUI GObject 提供安全区相关扩展方法。
    /// 不修改 FairyGUI 源码。
    /// </summary>
    public static class GObjectSafeAreaExt
    {
        /// <summary>
        /// 忽略安全区，使组件覆盖刘海/打孔区域。自动监听方向变化并调整尺寸。
        /// 适用场景：全屏背景、模态遮罩、引导遮挡层等需要超出安全区的内容。
        /// </summary>
        /// <param name="component">目标组件</param>
        /// <param name="type">Relation 类型，常用 RelationType.Size</param>
        public static void IgnoreSafeArea(this GObject component, RelationType type)
        {
            void OnSetScreen()
            {
                // 1. 清除旧的 Relation，防止与新 Relation 冲突
                component.relations.ClearAll();

                // 2. 计算偏移（+2px 冗余防止边界露缝）
                var offsetX = -SafeAreaHelper.LeftInset - 2;
                var offsetY = -SafeAreaHelper.TopInset  - 2;

                // 3. 调整位置和尺寸（覆盖安全区外的部分）
                component.SetXY(offsetX, offsetY);
                component.SetSize(SafeAreaHelper.FullWidth + 4, SafeAreaHelper.FullHeight + 4);

                // 4. 绑定到 GRoot 根容器
                component.AddRelation(GRoot.inst, type);
            }

            // 首次调用
            OnSetScreen();

            // 监听安全区变化（方向切换等）
            SafeAreaHelper.OnSafeAreaChanged -= OnSetScreen;
            SafeAreaHelper.OnSafeAreaChanged += OnSetScreen;

            // 组件销毁时注销监听，防止内存泄漏
            component.onRemovedFromStage.Add(() =>
            {
                SafeAreaHelper.OnSafeAreaChanged -= OnSetScreen;
            });
        }
    }
}
```

- [ ] **Step 2: 验证编译通过**

在 Unity Editor 中确认 `GObjectSafeAreaExt.cs` 编译通过。

---

### Task 3: 修改 ViewBase.cs — 重命名 IsFullScreen + 增加安全区约束

**Files:**
- Modify: `Unity/Assets/Scripts/FuFramework/UI/Runtime/View/ViewBase.cs`

- [ ] **Step 1: 重命名属性** — 将第 58 行 `IsFullScreen` 改为 `IgnoreSafeArea`

```csharp
// 改前（第 58 行）
protected virtual bool IsFullScreen => true;

// 改后
/// <summary>
/// 是否忽略安全区（刘海/打孔区域）。默认 true，即全屏显示覆盖刘海区域。
/// 设为 false 时 UI 内容自动约束在安全区内，避让刘海/打孔。
/// </summary>
protected virtual bool IgnoreSafeArea => true;
```

- [ ] **Step 2: 修改 Init 中的全屏逻辑** — 替换第 127-128 行的 if 块

```csharp
// 改前（第 127-128 行）
// 设置全屏
if (IsFullScreen) UIView?.MakeFullScreen();

// 改后
// 设置全屏和安全区适配
UIView?.MakeFullScreen();
if (!IgnoreSafeArea)
{
    ApplySafeAreaConstraint();
}
```

- [ ] **Step 3: 在 ViewBase 类末尾添加安全区约束方法和监听** — 在 `CloseSelf()` 方法之后插入

```csharp
/// <summary>
/// 将 UI 约束到安全区内（避让刘海/打孔区域）。
/// </summary>
private void ApplySafeAreaConstraint()
{
    UIView.x      = SafeAreaHelper.LeftInset;
    UIView.y      = SafeAreaHelper.TopInset;
    UIView.width  = SafeAreaHelper.FullWidth  - SafeAreaHelper.LeftInset - SafeAreaHelper.RightInset;
    UIView.height = SafeAreaHelper.FullHeight - SafeAreaHelper.TopInset  - SafeAreaHelper.BottomInset;
}

/// <summary>
/// 安全区变化回调（方向切换等）。
/// </summary>
private void _OnSafeAreaChanged()
{
    if (!IgnoreSafeArea)
    {
        ApplySafeAreaConstraint();
    }
}
```

- [ ] **Step 4: 在 Init 方法中注册安全区变化监听** — 在 `Init()` 方法末尾（`InitChildrenView(UIView)` 之后）添加

```csharp
// 注册安全区变化监听
if (!IgnoreSafeArea)
{
    SafeAreaHelper.OnSafeAreaChanged += _OnSafeAreaChanged;
}
```

- [ ] **Step 5: 在 _OnDispose 中注销监听** — 在 `ViewBase.Life.cs` 的 `_OnDispose()` 方法中注销

见 Task 4。

---

### Task 4: 修改 ViewBase.Life.cs — 重命名 IsFullScreen 引用 + 注销监听

**Files:**
- Modify: `Unity/Assets/Scripts/FuFramework/UI/Runtime/View/ViewBase.Life.cs`

- [ ] **Step 1: 修改第 100 行** — `IsFullScreen` → `IgnoreSafeArea`

```csharp
// 改前（第 100 行）
if (IsFullScreen) Visible = false;

// 改后
if (IgnoreSafeArea) Visible = false;
```

- [ ] **Step 2: 在 _OnDispose 中注销安全区监听** — 在 `ReleaseTimerRegister()` 之后添加

```csharp
// 改前（_OnDispose 末尾）
ReleaseEventRegister();   // 释放事件注册器
ReleaseUIEventRegister(); // 释放UI事件注册器
ReleaseTimerRegister();   // 释放计时器注册器

OnDispose();

// 改后
ReleaseEventRegister();   // 释放事件注册器
ReleaseUIEventRegister(); // 释放UI事件注册器
ReleaseTimerRegister();   // 释放计时器注册器

// 注销安全区变化监听
SafeAreaHelper.OnSafeAreaChanged -= _OnSafeAreaChanged;

OnDispose();
```

---

### Task 5: 修改 UIModule.cs — 初始化 SafeAreaHelper

**Files:**
- Modify: `Unity/Assets/Scripts/FuFramework/UI/Runtime/UIModule.cs`

- [ ] **Step 1: 在 OnInit 中调用 Refresh** — 在 `GRoot.inst.displayObject.stage.gameObject.transform.parent = transform;` 之后添加

```csharp
// 设置GRoot根节点
GRoot.inst.displayObject.stage.gameObject.transform.parent = transform;

// 初始化安全区数据
SafeAreaHelper.Refresh();
```

- [ ] **Step 2: 在 OnUpdate 中调用 PollUpdate** — 在第 127 行 `OnUpdate` 方法体开头添加

```csharp
// 改前（OnUpdate 方法开头）
protected override void OnUpdate(float deltaTime, float unscaledDeltaTime)
{
    // 回收等待回收的界面
    while (m_WaitRecycleQueue.Count > 0)

// 改后
protected override void OnUpdate(float deltaTime, float unscaledDeltaTime)
{
    // 检测安全区变化（方向切换等）
    SafeAreaHelper.PollUpdate();

    // 回收等待回收的界面
    while (m_WaitRecycleQueue.Count > 0)
```

---

### Task 6: 更新 README.md 文档

**Files:**
- Modify: `Unity/Assets/Scripts/FuFramework/UI/README.md`

- [ ] **Step 1: 更新第 186 行属性表**

```markdown
// 改前
| IsFullScreen | bool | 是否全屏（可重写，默认true） |

// 改后
| IgnoreSafeArea | bool | 是否忽略安全区/刘海屏（可重写，默认true），false 时自动约束到安全区内 |
```

- [ ] **Step 2: 更新第 424 行示例代码**

```csharp
// 改前
protected override bool IsFullScreen => true;

// 改后
protected override bool IgnoreSafeArea => true;
```

- [ ] **Step 3: 更新第 560、579、586 行示例代码**

```csharp
// 改前
protected override bool IsFullScreen => false;

// 改后
protected override bool IgnoreSafeArea => false;
```

---

### Task 7: 验证

- [ ] **Step 1: 编译验证** — 在 Unity Editor 中确认所有脚本编译通过，Console 无报错

- [ ] **Step 2: 功能验证** — 在 Unity Editor 中手动测试：
  1. 打开一个 `IgnoreSafeArea = true`（默认）的 UI，确认行为不变
  2. 打开一个 `IgnoreSafeArea = false` 的 UI，确认内容约束在安全区内
  3. 调用 `someChild.IgnoreSafeArea(RelationType.Size)`，确认背景覆盖全屏
  4. （如可模拟）切换设备方向，确认 UI 自动适配

- [ ] **Step 3: Commit**

```bash
git add Unity/Assets/Scripts/FuFramework/UI/Runtime/Utility/SafeAreaHelper.cs
git add Unity/Assets/Scripts/FuFramework/UI/Runtime/Utility/GObjectSafeAreaExt.cs
git add Unity/Assets/Scripts/FuFramework/UI/Runtime/View/ViewBase.cs
git add Unity/Assets/Scripts/FuFramework/UI/Runtime/View/ViewBase.Life.cs
git add Unity/Assets/Scripts/FuFramework/UI/Runtime/UIModule.cs
git add Unity/Assets/Scripts/FuFramework/UI/README.md
git commit -m "feat: 横屏游戏安全区（刘海屏）适配

- 新增 SafeAreaHelper 封装 Unity Screen.safeArea，提供设计坐标转换和方向变化事件
- 新增 IgnoreSafeArea() 扩展方法，使组件可覆盖安全区
- 重命名 ViewBase.IsFullScreen → IgnoreSafeArea（默认 true，向后兼容）
- IgnoreSafeArea=false 时 UI 自动约束到安全区内
- UIModule 初始化时自动刷新安全区数据"
```

---

## 自审清单

- [x] **覆盖设计文档所有需求** — SafeAreaHelper ✓, GObjectSafeAreaExt ✓, ViewBase 改名 + 约束 ✓, UIModule 初始化 ✓, README 更新 ✓
- [x] **无占位符** — 所有代码和命令均为完整内容
- [x] **类型一致** — `IgnoreSafeArea` 命名贯穿所有 Task，`SafeAreaHelper` 方法签名一致，`_OnSafeAreaChanged` 在 ViewBase.cs 定义、ViewBase.Life.cs 使用一致
- [x] **Task 5 Step 2 已确认** — `UIModule.OnUpdate()` 存在（第 125 行），`PollUpdate()` 插入到方法体开头。
