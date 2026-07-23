# UI 模块配置表化设计

**日期**: 2026-07-24
**状态**: 已确认
**目标**: 将 ViewBase 中的 UI 选项参数从硬编码 virtual 属性迁移为 Luban 配置表驱动，提升灵活性、可控性，并支持新手引导等外部模块查询 UI 配置。

---

## 一、动机

当前 ViewBase 中 5 个配置性参数通过 `protected virtual` 属性定义，子类通过代码 override 设定值：

```csharp
protected virtual EUILayer Layer                => EUILayer.Normal;
protected virtual EUITweenType TweenType        => EUITweenType.Fade;
protected virtual float TweenDuration           => 0.3f;
protected virtual bool AdjustNotch              => true;
protected virtual bool PauseCoveredUI           => false;
```

**问题：**
- 配置值和代码耦合，调整参数需要改 C# 重新编译
- 新手引导等外部模块无法查询 UI 的层级、动画等配置信息
- 无法运行时动态覆盖（如引导期间禁用动画）
- 新增 UI 时配置分散在代码中，不易统一管理和审查

## 二、方案概述

采用 **方案 B：配置驱动**，将参数迁移到 Luban 配置表 `UIConfig.xlsx`，每 UI 一行。ViewBase 去掉 virtual 属性，改为在 Init 时从配置表加载。业务子类清理所有 override 代码。同步修改 FGUI 代码生成插件模板，不再为新 UI 生成 override 桩代码。

## 三、配置表设计

### Luban Excel 表：`UIConfig.xlsx`

| 列名 | 类型 | 说明 | 默认值 |
|------|------|------|--------|
| `UIName` | string | 界面名称（主键），对应 ViewBase.UIName | — |
| `Layer` | enum:EUILayer | 界面层级 | Normal |
| `TweenType` | enum:EUITweenType | 打开/关闭动画类型 | Fade |
| `TweenDuration` | float | 动画时长（秒） | 0.3 |
| `AdjustNotch` | bool | 是否适配刘海/打孔区域 | true |
| `PauseCoveredUI` | bool | 被遮挡时是否暂停 | false |

- `UIName` 为主键，Luban 生成 `TbUIConfig` 字典表，`TryGet(UIName)` 取值。
- 若查表失败（未配置该 UI），使用 ViewBase 中硬编码的默认值。

### 新增文件

| 文件 | 说明 |
|------|------|
| `Config/Excels/UIConfig.xlsx` | Excel 配置源 |
| `Tables/UIConfig.cs` + `Tables/TbUIConfig.cs` | Luban 生成或手写配置类 |

### 现有文件修改

- `TableManager.cs` — 注册 `TbUIConfig` 表

## 四、ViewBase 改动

### 删除

```csharp
protected virtual EUILayer Layer                => EUILayer.Normal;
protected virtual EUITweenType TweenType        => EUITweenType.Fade;
protected virtual float TweenDuration           => 0.3f;
protected virtual bool AdjustNotch              => true;
protected virtual bool PauseCoveredUI           => false;
```

### 新增

```csharp
// Luban 生成的配置行，null 表示使用默认值
public UIConfig UIConfig { get; private set; }
```

### Init 方法中新增配置加载

```csharp
var tbUIConfig = ConfigModule.Instance.GetConfig<TbUIConfig>();
UIConfig = tbUIConfig.TryGet(UIName, out var row) ? row : null;
```

### 属性读取点适配

所有原 virtual 属性引用点改为读取 `UIConfig`，格式：

```csharp
// Layer → UIConfig?.Layer ?? EUILayer.Normal
// TweenType → UIConfig?.TweenType ?? EUITweenType.Fade
// TweenDuration → UIConfig?.TweenDuration ?? 0.3f
// AdjustNotch → UIConfig?.AdjustNotch ?? true
// PauseCoveredUI → UIConfig?.PauseCoveredUI ?? false
```

涉及的文件：
- `View/ViewBase.cs` — 安全区初始化、Visible 设置
- `View/ViewBase.Life.cs` — 动画逻辑、遮挡逻辑
- `Misc/UIGroup.cs` 或 `UIModule.UIGroup.cs` — 如有引用 `view.Layer`
- `UIModule.Open.cs` — 如有读取 Layer/TweenType 的地方

### 业务 UI 子类清理

所有游戏 UI 子类（`WinLogin`、`WinBag`、`WinMain`、`WinGlobalLoading`、`WinGlobalMask`、`WinClickGuide`、`WinDialogGuide` 等）中若有 override 这些属性的代码，全部删除。

## 五、新手引导交互

配置表化后，Guide 模块可通过 `view.UIConfig` 读取 UI 的配置信息：

```csharp
var targetView = GlobalModule.UIModule.GetUI(targetWindowName) as ViewBase;
var config = targetView?.UIConfig;
// config?.Layer         — 决定引导遮罩层级
// config?.TweenType     — 决定是否等动画播完再开始引导
// config?.AdjustNotch   — 决定高亮区域计算是否考虑安全区
```

本次设计仅提供读取能力，不涉及运行时覆盖（覆盖层留到有实际需求时再加）。

## 六、FGUI 代码生成插件同步修改

### 文件：`FairyGUIProject/plugins/CSharpCodeGen/Template/WinTemplate.txt`

**删除** `#region 界面基本属性` 块中的以下行：

```csharp
protected override EUILayer Layer         => EUILayer.Normal;   // 界面所属的层级。
protected override EUITweenType TweenType => EUITweenType.Fade; // 界面打开/关闭时的动画效果。
protected override bool IsFullScreen      => true;              // 是否是全屏界面。（历史遗留，代码库中不存在此属性）
public override bool PauseCoveredUI       => false;             // 显示时是否暂停被覆盖的界面。
```

整块 `#region` 随之删除或保留空壳（仅注释说明参数已迁移到 UIConfig 配置表）。

### 文件：`FairyGUIProject/plugins/CSharpCodeGen/Template/WinGenTemplate.txt`

**不动**。该模板只生成 `UIName` 和 `PackageName` override，保持不变。

### 文件：`FairyGUIProject/plugins/CSharpCodeGen/Template/CompTemplate.txt`

需要检查是否存在类似 override 行。Component 不继承 ViewBase 完整属性，大概率不需要改。

## 七、变更范围汇总

### 框架层

| 文件 | 改动 |
|------|------|
| `Framework/UI/View/ViewBase.cs` | 删除 virtual 属性，新增 `UIConfig` 属性，Init 中加载配置，引用点适配 |
| `Framework/UI/View/ViewBase.Life.cs` | 属性引用点改为 `UIConfig?.Xxx ?? default` |
| `Framework/UI/Misc/UIGroup.cs` | 如有 `view.Layer` 引用需适配 |
| `Framework/UI/UIModule.UIGroup.cs` | 如有 `view.Layer` 引用需适配 |
| `Framework/UI/UIModule.Open.cs` | 如有属性引用需适配 |

### 业务层

| 文件 | 改动 |
|------|------|
| `Game/UI/**/Win*.cs` | 删除 Layer/TweenType/TweenDuration/AdjustNotch/PauseCoveredUI 的 override |

**不改动的文件：**

| 文件 | 原因 |
|------|------|
| `ViewBase.EventRegister.cs` | 无属性引用 |
| `ViewBase.TimerRegister.cs` | 无属性引用 |
| `ViewBase.UIEventRegister.cs` | 无属性引用 |
| 所有 `*.Gen.cs` | FGUI 自动生成，不涉及这些属性 |

### 配置层

| 新增 | 说明 |
|------|------|
| `Config/Excels/UIConfig.xlsx` | Excel 配置源 |
| `Tables/UIConfig.cs` + `Tables/TbUIConfig.cs` | Luban 生成配置类 |
| `TableManager.cs` | 注册 TbUIConfig 表 |

### FGUI 插件

| 文件 | 改动 |
|------|------|
| `FairyGUIProject/plugins/CSharpCodeGen/Template/WinTemplate.txt` | 删除 override 桩代码块 |

## 八、不改动范围

- 不修改 `UIModule` 的核心打开/关闭流程
- 不增加运行时覆盖机制（留到有实际需求时）
- 不修改 FairyGUI 包的加载和缓存逻辑
- 不修改 `.Gen.cs` 生成逻辑（`UIName`/`PackageName` 保持不动）
