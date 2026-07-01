# FuFramework 横屏游戏安全区（刘海屏）适配方案

**最后更新**：2026-07-02  
**项目**：FuFramework 2.0  
**参考方案**：SLG 项目刘海屏适配方案（`C:\Users\Administrator\Desktop\刘海屏适配方案.md`）

## 概述

项目采用**两层适配机制**：

1. **全局级** — 扩展方法 `IgnoreSafeArea()`，使全屏背景/遮罩自动覆盖安全区外的内容
2. **UI 级** — `ViewBase.IgnoreSafeArea` 属性，控制单个 UI 是否遵守安全区

核心原理：读取 Unity `Screen.safeArea` 获取系统安全区，通过 `UIContentScaler.scaleFactor` 转换为 FairyGUI 设计坐标，自动调整组件位置和尺寸，监听屏幕方向变化实时更新。

> **注意**：本项目为横屏游戏，刘海/打孔通常位于屏幕左/右侧。

---

## 新增文件

### 1. `SafeAreaHelper.cs`

**路径**：`Unity/Assets/Scripts/FuFramework/UI/Runtime/Utility/SafeAreaHelper.cs`

**职责**：封装 Unity `Screen.safeArea`，提供设计坐标下的安全区偏移量，支持方向变化通知。

```csharp
public static class SafeAreaHelper
{
    /// <summary>当前安全区（设计坐标），初始化后调用 Refresh() 更新</summary>
    public static Rect Current { get; }

    /// <summary>左侧安全区偏移（设计坐标）</summary>
    public static float LeftInset { get; }

    /// <summary>右侧安全区偏移（设计坐标）</summary>
    public static float RightInset { get; }

    /// <summary>顶部安全区偏移（设计坐标）</summary>
    public static float TopInset { get; }

    /// <summary>底部安全区偏移（设计坐标）</summary>
    public static float BottomInset { get; }

    /// <summary>全屏宽度（设计坐标，含安全区）</summary>
    public static float FullWidth { get; }

    /// <summary>全屏高度（设计坐标，含安全区）</summary>
    public static float FullHeight { get; }

    /// <summary>安全区变化事件（方向切换、折叠屏等）</summary>
    public static event Action OnSafeAreaChanged;

    /// <summary>刷新安全区数据</summary>
    public static void Refresh();
}
```

**关键实现点**：
- 坐标转换：`设计坐标 = 物理像素 / UIContentScaler.scaleFactor`
- 方向监听：通过 `Screen.orientation` 变化或每帧检测 `Screen.safeArea` 变化来触发 `OnSafeAreaChanged`
- `Refresh()` 在 UI 模块初始化时调用一次，后续通过事件自动更新

---

### 2. `GObjectSafeAreaExt.cs`

**路径**：`Unity/Assets/Scripts/FuFramework/UI/Runtime/Utility/GObjectSafeAreaExt.cs`

**职责**：为 FairyGUI `GObject` 提供 `IgnoreSafeArea()` 扩展方法，使组件超出安全区覆盖到刘海/打孔区域。不修改 FairyGUI 源码。

```csharp
public static class GObjectSafeAreaExt
{
    /// <summary>
    /// 忽略安全区，使组件覆盖刘海/打孔区域。自动监听方向变化并调整。
    /// </summary>
    /// <param name="component">目标组件</param>
    /// <param name="type">Relation 类型，常用 RelationType.Size</param>
    public static void IgnoreSafeArea(this GObject component, RelationType type)
}
```

**内部逻辑**：
1. 清除旧 `relations`（防止冲突）
2. 读 `SafeAreaHelper` 计算偏移和尺寸（含 2px 冗余）
3. `SetXY` + `SetSize` 应用
4. `AddRelation(GRoot.inst, type)` 绑定根容器
5. 监听 `SafeAreaHelper.OnSafeAreaChanged`，方向变化时重新计算
6. `disposeAction` 中注销监听，防止泄漏

---

## 修改文件

### 3. `ViewBase.cs`

**路径**：`Unity/Assets/Scripts/FuFramework/UI/Runtime/View/ViewBase.cs`

**改动**：

| 位置 | 改动内容 |
|------|----------|
| 第 58 行 | `IsFullScreen` 重命名为 `IgnoreSafeArea`，默认值 `true`（保持现有行为不变） |
| 第 128 行 | `MakeFullScreen()` 之后，当 `IgnoreSafeArea == false` 时自动约束 UI 到安全区内 |

```csharp
// 改名（默认 true，现有行为不变）
protected virtual bool IgnoreSafeArea => true;

// Init 中增加安全区适配
if (IsFullScreen)  // 改为 IgnoreSafeArea
{
    UIView?.MakeFullScreen();
}
else
{
    // MakeFullScreen 填满全屏 + 然后限制内容区域到安全区
    UIView?.MakeFullScreen();
    ApplySafeAreaConstraint();
}

void ApplySafeAreaConstraint()
{
    UIView.x      = SafeAreaHelper.LeftInset;
    UIView.width  = SafeAreaHelper.FullWidth  - SafeAreaHelper.LeftInset - SafeAreaHelper.RightInset;
    UIView.y      = SafeAreaHelper.TopInset;
    UIView.height = SafeAreaHelper.FullHeight - SafeAreaHelper.TopInset  - SafeAreaHelper.BottomInset;
}
```

**子类使用**：只需重写属性即可控制：

```csharp
// 需要遵守安全区的 UI（如主菜单、设置面板）
protected override bool IgnoreSafeArea => false;

// 默认情况（全屏 UI，覆盖刘海区域）
// 无需重写，默认 IgnoreSafeArea = true
```

---

## 使用场景速查

| 场景 | 推荐方案 | 代码 |
|------|----------|------|
| 全屏背景图 | `IgnoreSafeArea()` | `bg.IgnoreSafeArea(RelationType.Size)` |
| 模态遮罩 | `IgnoreSafeArea()` | `mask.IgnoreSafeArea(RelationType.Size)` |
| 引导遮挡层 | `IgnoreSafeArea()` | `overlay.IgnoreSafeArea(RelationType.Size)` |
| 普通面板（需避让刘海） | 重写属性 | `protected override bool IgnoreSafeArea => false` |
| 沉浸式界面（战斗等） | 默认行为 | 不处理，`IgnoreSafeArea = true`（默认） |

---

## 数据流向

```
Unity Screen.safeArea (物理像素)
        │
        ▼
SafeAreaHelper (转换为设计坐标)
  ├─ LeftInset / RightInset / TopInset / BottomInset
  ├─ FullWidth / FullHeight
  └─ OnSafeAreaChanged 事件
        │
        ├──────────────────────────────┐
        │                              │
        ▼                              ▼
方案1：IgnoreSafeArea() 扩展方法    方案2：ViewBase 自动适配
  ├─ 清除旧 Relation                  ├─ IgnoreSafeArea = false
  ├─ 偏移超出安全区                   ├─ MakeFullScreen() 填满
  ├─ 绑定 GRoot                       └─ 约束 x/y/width/height 到安全区内
  └─ 监听方向变化
        │                              │
        └──────────────┬───────────────┘
                       ▼
               UI 正确显示，不被刘海/打孔遮挡
```

---

## 关键细节

### 1. FairyGUI 版本差异

本项目的 FairyGUI 版本 `GRoot` 无 `offset_x`/`offset_y` 属性和 `onOrientationChanged` 事件。改用 Unity 原生 `Screen.safeArea` + 自定义 `OnSafeAreaChanged` 事件替代。

### 2. Relation 清除必要性

扩展方法中必须先 `relations.ClearAll()` 再 `AddRelation(GRoot.inst, type)`，避免旧的 Relation 与新的冲突导致位置/尺寸计算错误。

### 3. 方向变化监听

- `SafeAreaHelper` 内部监听设备方向变化
- 触发 `OnSafeAreaChanged` 事件
- `IgnoreSafeArea()` 和 `ViewBase` 各自订阅该事件，方向变化后自动重新计算

### 4. 冗余边距

参考方案使用 `+4` 像素冗余防止浮点误差导致边界露缝。本文案等分到两侧各 `+2`。

---

## 自审清单

- [x] 无 TBD / TODO 占位符
- [x] 无内部矛盾
- [x] 范围聚焦于安全区适配，无额外重构
- [x] 命名统一：`SafeArea`、`IgnoreSafeArea`
- [x] FairyGUI 源码零修改
- [x] 现有 UI 行为不变（`IgnoreSafeArea` 默认 `true`）
