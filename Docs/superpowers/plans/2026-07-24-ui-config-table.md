# UI 模块配置表化 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 ViewBase 中的 Layer/TweenType/TweenDuration/AdjustNotch/PauseCoveredUI 五个 virtual 属性迁移为 Luban 配置表驱动，提升灵活性并支持外部模块查询。

**Architecture:** 新增 Luban 配置表 `UIConfig`（Excel → Luban 自动生成 C# 代码），ViewBase 在 Init 阶段加载配置行并缓存。删除所有 virtual 属性声明及业务子类 override，改为读取 `UIConfig` 配置行。同步修改 FGUI 代码生成插件模板，移除新建 UI 时的 override 桩代码。

**Tech Stack:** C#, FairyGUI, Luban 配置系统, Lua (FGUI 插件)

## Global Constraints

- 全程使用中文交流
- Git 提交遵循 Conventional Commits 中文版规范（`Docs/Git提交规范.md`）
- 代码风格遵循 `Docs/代码风格规范.md`
- 配置层 Layer/TweenType 在 Excel 中以 int 存储，代码侧强制转换为枚举
- 所有 Luban 生成代码位于 `Game/AutoGen/Tables/Generate/`，不可手动修改

---

### Task 1: 创建 UIConfig 配置表 Excel 并注册

**Files:**
- Create: `Config/Excels/Tables/U-UIConfig-UI配置表.xlsx`
- Modify: `Config/Excels/__beans__.xlsx`（Luban bean 注册）
- Modify: `Config/Excels/__tables__.xlsx`（Luban table 注册）

**Interfaces:**
- Produces: Luban 自动生成 `Tables/UIConfig.cs`（行数据类）、`Tables/TbUIConfig.cs`（表类）、更新 `TableManager.cs`

---

- [ ] **Step 1: 阅读现有 Luban 元数据 Excel 了解注册格式**

打开 `Config/Excels/__beans__.xlsx` 和 `Config/Excels/__tables__.xlsx`，查看现有表（如 Guide）的注册格式。重点关注字段名、类型、注释列的写法。

- [ ] **Step 2: 在 `__beans__.xlsx` 中注册 UIConfig bean**

参照现有 bean 格式（如 Guide），新增一行定义 UIConfig 的字段结构：

| 字段名 | 类型 | 分组 | 注释 |
|--------|------|------|------|
| UIName | string | c | 界面名称（主键） |
| Layer | int | c | 界面层级 |
| TweenType | int | c | 动画类型 |
| TweenDuration | float | c | 动画时长（秒） |
| AdjustNotch | bool | c | 是否适配刘海 |
| PauseCoveredUI | bool | c | 被遮挡时是否暂停 |

类型对应关系：EUILayer 枚举值用 int 存储，EUITweenType 枚举值用 int 存储，代码侧通过 `(EUILayer)row.Layer` 强制转换。

- [ ] **Step 3: 在 `__tables__.xlsx` 中注册 UIConfig 表**

参照现有表格式（如 Guide，分组标记为 `c`），新增一行注册 UIConfig 表，指定 key 字段为 `UIName`，分组标记为 `c`（客户端）。

- [ ] **Step 4: 创建 UIConfig Excel 数据文件**

创建 `Config/Excels/Tables/U-UIConfig-UI配置表.xlsx`，按 Luban 格式填入现有 UI 的初始数据：

| UIName | Layer | TweenType | TweenDuration | AdjustNotch | PauseCoveredUI |
|--------|-------|-----------|---------------|-------------|----------------|
| WinMain | 1500 | 1 | 0.3 | true | false |
| WinLogin | 2000 | 1 | 0.3 | true | false |
| WinPlayerList | 2000 | 1 | 0.3 | true | false |
| WinPlayerCreate | 2000 | 1 | 0.3 | true | false |
| WinLoginAnnouncement | 2000 | 1 | 0.3 | true | false |
| WinBag | 2000 | 1 | 0.3 | false | false |
| WinGlobalLoading | 2000 | 1 | 0.3 | false | false |
| WinDialogMessageBox | 2500 | 1 | 0.3 | true | false |
| WinGlobalMask | 3500 | 0 | 0.3 | false | false |
| WinClickGuide | 3500 | 1 | 0.3 | false | false |
| WinDialogGuide | 3500 | 1 | 0.3 | false | false |
| WinLoadingScene | 2000 | 1 | 0.3 | false | false |

> Layer 取值：WorldUI=0, MainUI=1500, Normal=2000, Window=2500, Tips=3000, Guide=3500, Loading=4000
> TweenType 取值：None=0, Fade=1, Custom=2

- [ ] **Step 5: 运行 Luban 生成工具**

```bash
cd "D:\_WorkSpace\Unity\FuFramework2.0\Config" && ./gen-client-json.bat
```

Expected: 生成成功，无报错。生成文件包括：
- `Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate/Tables/UIConfig.cs`
- `Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate/Tables/TbUIConfig.cs`
- `Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate/TableManager.cs`（已更新，包含 TbUIConfig 注册代码）

- [ ] **Step 6: 验证生成代码**

打开 `TbUIConfig.cs` 确认：
- `StrKeyDataDict` 以 `UIName` 为 key
- 可通过 `TbUIConfig.Get(string uIName)` 或直接访问 `StrKeyDataDict` 查询

打开 `TableManager.cs` 确认：
- 属性声明中存在 `internal Tables.TbUIConfig TbUIConfig { private set; get; }`
- `LoadAsync` 中存在 `TbUIConfig` 的实例化和加载注册代码

- [ ] **Step 7: 提交**

```bash
git add Config/Excels/ Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate/
git commit -m "feat: 新增 UIConfig Luban 配置表，定义 UI 界面运行时参数"
```

---

### Task 2: ViewBase 核心改造

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/UI/View/ViewBase.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/UI/View/ViewBase.Life.cs`

**Interfaces:**
- Consumes: `TbUIConfig`（Task 1 生成）、`ConfigModule.Instance.GetConfig<TbUIConfig>()`
- Produces: `public UIConfig UIConfig { get; private set; }`（替代被删除的5个 virtual 属性）

---

- [ ] **Step 1: 删除 ViewBase.cs 中的 5 个 virtual 属性声明**

定位 `ViewBase.cs` 第 44、60、65、70、75 行，删除以下代码块：

```csharp
// 删除第44行
public virtual bool PauseCoveredUI => false;

// 删除第56-60行（含注释）
/// <summary>
/// 是否适配刘海/打孔区域。默认 true，即 UI 跟随 GRoot 约束在安全区内，避让刘海/打孔。
/// 设为 false 时 UI 填满全屏覆盖刘海（全屏背景、遮罩等）。
/// </summary>
protected virtual bool AdjustNotch => true;

// 删除第62-65行（含注释）
/// <summary>
/// 界面所属的层级。
/// </summary>
protected virtual EUILayer Layer => EUILayer.Normal;

// 删除第67-70行（含注释）
/// <summary>
/// 界面打开/关闭时的动画类型。
/// </summary>
protected virtual EUITweenType TweenType => EUITweenType.Fade;

// 删除第72-75行（含注释）
/// <summary>
/// 界面打开/关闭时的动画时长。
/// </summary>
protected virtual float TweenDuration => 0.3f;
```

- [ ] **Step 2: 新增 UIConfig 属性和辅助方法**

在原 virtual 属性声明位置，新增：

```csharp
/// <summary>
/// UI 配置数据（来自 UIConfig 配置表）。为 null 时使用默认值。
/// </summary>
public UIConfig UIConfig { get; private set; }

/// <summary>
/// 获取界面所属的层级（仅框架内部使用，外部请读 UIConfig.Layer）。
/// </summary>
private EUILayer Layer => (EUILayer)(UIConfig?.Layer ?? (int)EUILayer.Normal);

/// <summary>
/// 获取界面打开/关闭时的动画类型（仅框架内部使用）。
/// </summary>
private EUITweenType TweenType => (EUITweenType)(UIConfig?.TweenType ?? (int)EUITweenType.Fade);

/// <summary>
/// 获取界面打开/关闭时的动画时长（仅框架内部使用）。
/// </summary>
private float TweenDuration => UIConfig?.TweenDuration ?? 0.3f;

/// <summary>
/// 是否适配刘海/打孔区域（仅框架内部使用）。
/// </summary>
private bool AdjustNotch => UIConfig?.AdjustNotch ?? true;

/// <summary>
/// 显示时是否暂停被覆盖的界面。UIGroup 通过 view.PauseCoveredUI 外部访问，保持 public。
/// </summary>
public bool PauseCoveredUI => UIConfig?.PauseCoveredUI ?? false;
```

注意：
- `Layer`/`TweenType`/`TweenDuration`/`AdjustNotch` 从 `protected virtual` 改为 `private` —— 仅在 ViewBase 内部使用，不再允许子类 override。
- `PauseCoveredUI` 从 `public virtual` 改为 `public`（非 virtual）—— 保持与原有 API 兼容，UIGroup 通过 `viewInfo.View.PauseCoveredUI` 外部读取。

- [ ] **Step 3: 在 Init 方法中加载配置**

在 `ViewBase.cs` 的 `Init` 方法中，`m_UIModule = ModuleManager.GetModule<UIModule>();` 之后（约第 115 行），`if (!isNewInstance) return;` 之前，新增配置加载逻辑：

```csharp
// 加载 UI 配置表
var tbUIConfig = ConfigModule.Instance?.GetConfig<TbUIConfig>();
if (tbUIConfig != null && tbUIConfig.StrKeyDataDict.TryGetValue(UIName, out var row))
{
    UIConfig = row;
}
else
{
    UIConfig = null; // 查表失败时使用各属性 getter 中的默认值
}
```

需要在文件顶部新增 using：

```csharp
using Hotfix.Framework.Config;
using Hotfix.Game.Config.Tables;
```

注：`ConfigModule` 位于 `Hotfix.Framework.Config` 命名空间，`TbUIConfig` 和 `UIConfig` 由 Luban 生成在 `Hotfix.Game.Config.Tables` 命名空间。

- [ ] **Step 4: 在 Unity Editor 中编译验证**

在 Unity Editor 中打开项目，等待脚本编译完成。确认无编译错误。

Expected: 编译通过。如有编译错误，根据错误信息修复。

- [ ] **Step 5: 提交**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/UI/View/ViewBase.cs
git commit -m "refactor: ViewBase 配置参数迁移至 UIConfig 配置表"
```

---

### Task 3: 业务 UI 子类清理 — 删除 override

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Game/UI/Main/WinMain.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Game/UI/Login/WinLogin.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Game/UI/Login/WinPlayerList.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Game/UI/Login/WinPlayerCreate.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Game/UI/Login/WinLoginAnnouncement.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Game/UI/Bag/WinBag.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Game/UI/Common/WinGlobalLoading.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Game/UI/Common/WinGlobalMask.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Game/UI/Tips/WinDialogMessageBox.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Game/UI/Guide/WinClickGuide.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Game/UI/Guide/WinDialogGuide.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Game/UI/Loading/WinLoadingScene.cs`

**Interfaces:**
- Consumes: 无（仅删除代码）

---

- [ ] **Step 1: 删除 WinMain.cs 的 override 块**

`WinMain.cs` 第 16-24 行，删除整个 `#region 界面基本属性` 块：

```csharp
// 删除以下内容：
#region 界面基本属性(无特殊需求，可不做修改)

 //@formatter:off
 protected override EUILayer Layer         => EUILayer.MainUI;   // 界面所属的层级。
 protected override EUITweenType TweenType => EUITweenType.Fade; // 界面打开/关闭时的动画效果。
 public override bool PauseCoveredUI      => false;            // 显示时是否暂停被覆盖的界面。
//@formatter:on

#endregion
```

- [ ] **Step 2: 删除 WinLogin.cs 的 override 块**

`WinLogin.cs` 第 22-29 行区域（`#region 界面基本属性` 块），搜索并删除。包含 `Layer`、`TweenType`、`PauseCoveredUI` 三行 override。

- [ ] **Step 3: 删除 WinPlayerList.cs 的 override 块**

`WinPlayerList.cs` 第 22-28 行区域，删除 `Layer`、`TweenType`、`PauseCoveredUI` override。

- [ ] **Step 4: 删除 WinPlayerCreate.cs 的 override 块**

`WinPlayerCreate.cs` 第 18-24 行区域，删除 `Layer`、`TweenType`、`PauseCoveredUI` override。

- [ ] **Step 5: 删除 WinLoginAnnouncement.cs 的 override 块**

`WinLoginAnnouncement.cs` 第 13-19 行区域，删除 `Layer`、`TweenType`、`PauseCoveredUI` override。

- [ ] **Step 6: 删除 WinBag.cs 的 override 块**

`WinBag.cs` 第 13-21 行区域，删除 `Layer`、`TweenType`、`AdjustNotch`、`PauseCoveredUI` override。

- [ ] **Step 7: 删除 WinGlobalLoading.cs 的 override 块**

`WinGlobalLoading.cs` 第 13-21 行区域，删除 `Layer`、`TweenType`、`AdjustNotch`、`PauseCoveredUI` override。

- [ ] **Step 8: 删除 WinGlobalMask.cs 的 override 块**

`WinGlobalMask.cs` 第 11-19 行区域，删除 `Layer`、`TweenType`、`AdjustNotch`、`PauseCoveredUI` override。

- [ ] **Step 9: 删除 WinDialogMessageBox.cs 的 override 块**

`WinDialogMessageBox.cs` 第 14-21 行区域，删除 `Layer`、`TweenType`、`PauseCoveredUI` override。

- [ ] **Step 10: 删除 WinClickGuide.cs 的 override 块**

`WinClickGuide.cs` 第 13-21 行区域，删除 `Layer`、`TweenType`、`AdjustNotch`、`PauseCoveredUI` override。

- [ ] **Step 11: 删除 WinDialogGuide.cs 的 override 块**

`WinDialogGuide.cs` 第 19-27 行区域，删除 `Layer`、`TweenType`、`AdjustNotch`、`PauseCoveredUI` override。

- [ ] **Step 12: 删除 WinLoadingScene.cs 的 override 块**

`WinLoadingScene.cs` 第 13-21 行区域，删除 `Layer`、`TweenType`、`AdjustNotch`、`PauseCoveredUI` override。

- [ ] **Step 13: 全面搜索确认无遗漏**

```bash
cd "D:\_WorkSpace\Unity\FuFramework2.0" && grep -rn "override.*\(Layer\|TweenType\|TweenDuration\|AdjustNotch\|PauseCoveredUI\)" Unity/Assets/Scripts/ --include="*.cs"
```

Expected: 无输出（除 ViewBase.cs 本身已删除外，无任何遗留 override）。

- [ ] **Step 14: Unity Editor 编译验证**

在 Unity Editor 中等待脚本编译完成，确认无编译错误。

- [ ] **Step 15: 提交**

```bash
git add Unity/Assets/Scripts/Hotfix/Game/UI/
git commit -m "refactor: 清理业务 UI 子类的 override 属性，参数已迁移至 UIConfig 配置表"
```

---

### Task 4: FGUI 代码生成插件模板同步

**Files:**
- Modify: `FairyGUIProject/plugins/CSharpCodeGen/Template/WinTemplate.txt`

**Interfaces:**
- Produces: 新发布的 UI 不再生成 override 桩代码

---

- [ ] **Step 1: 修改 WinTemplate.txt**

定位并删除 `#region 界面基本属性` 块（第 9-18 行）：

**修改前：**
```csharp
    #region 界面基本属性(无特殊需求，可不做修改)
 
     //@formatter:off
     protected override EUILayer Layer         => EUILayer.Normal;   // 界面所属的层级。
     protected override EUITweenType TweenType => EUITweenType.Fade; // 界面打开/关闭时的动画效果。
     protected override bool IsFullScreen      => true;              // 是否是全屏界面。
     public override bool PauseCoveredUI       => false;             // 显示时是否暂停被覆盖的界面。
     //@formatter:on
     
     #endregion
```

**修改后：** 整块替换为一行注释：

```csharp
    // 界面基本属性(Layer、TweenType、AdjustNotch、PauseCoveredUI等)已迁移至 UIConfig 配置表，无需代码重写。
```

- [ ] **Step 2: 检查 CompTemplate.txt 是否需要修改**

```bash
grep -n "override\|Layer\|TweenType\|IsFullScreen\|PauseCoveredUI\|AdjustNotch" FairyGUIProject/plugins/CSharpCodeGen/Template/CompTemplate.txt
```

Expected: 无相关 override。Component 不继承 ViewBase 的完整属性集，无需修改。

- [ ] **Step 3: 提交**

```bash
git add FairyGUIProject/plugins/CSharpCodeGen/Template/WinTemplate.txt
git commit -m "refactor: FGUI 插件模板移除 override 桩代码，UI 参数已配置表化"
```

---

### Task 5: README 文档更新

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/UI/README.md`

---

- [ ] **Step 1: 更新 README 中的示例代码**

搜索 README.md 中包含 `override.*Layer`、`override.*AdjustNotch`、`override.*TweenType`、`override.*TweenDuration` 的行，将硬编码 override 示例更新为配置表驱动方式的说明。

具体改动（约第 449-651 行区域）：

- 保留 `AdjustNotch` 概念说明，补充"该参数现在通过 UIConfig 配置表设置"
- 将 `protected override EUILayer Layer => ...` 示例替换为配置表中文档说明
- 将动画示例中的 `override TweenType` / `override TweenDuration` 替换为配置表说明

- [ ] **Step 2: 新增配置表使用说明小节**

在 README 的"基本属性"相关章节新增一段，说明配置表的使用方式：

```markdown
### UI 配置表（UIConfig）

自 v2.x 起，UI 界面的基本属性（Layer、TweenType、TweenDuration、AdjustNotch、PauseCoveredUI）
由 `UIConfig` 配置表驱动，不再通过代码 override。

- **配置表路径**: `Config/Excels/Tables/U-UIConfig-UI配置表.xlsx`
- **查询配置**: `view.UIConfig` 获取当前 UI 的配置行
- **新增 UI**: 在 Excel 中添加一行（UIName 为 key），运行 `gen-client-json.bat` 重新生成代码
```

- [ ] **Step 3: 提交**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/UI/README.md
git commit -m "docs: 更新 UI 模块 README，反映配置表化后的使用方式"
```

---

### Task 6: 全局搜索残留引用与最终验证

**Files:**
- 全部项目 C# 文件（搜索验证，不直接修改）

---

- [ ] **Step 1: 搜索所有对已删除属性的外部引用**

```bash
cd "D:\_WorkSpace\Unity\FuFramework2.0" && grep -rn "\.Layer\b\|\.TweenType\b\|\.TweenDuration\b\|\.AdjustNotch\b\|\.PauseCoveredUI\b" Unity/Assets/Scripts/ --include="*.cs" | grep -v "UIConfig" | grep -v "EUILayer" | grep -v "EUITweenType" | grep -v "\.Layer\s*=" | grep -v "SortingOrder"
```

检查输出结果，确认所有对 Layer/TweenType 等属性的引用都来自 ViewBase 内部（已改为 private 属性 getter），而非外部通过 `view.Layer` 等直接访问。如有外部引用，需逐一评估并适配。

- [ ] **Step 2: Unity Editor 完整编译**

在 Unity Editor 中执行 `Assets > Reimport All` 或重启 Editor，确保所有脚本完全重新编译。

Expected: 零编译错误。

- [ ] **Step 3: 运行时验证（可选，需 Unity Editor Play Mode）**

进入 Play Mode，依次打开各个 UI 界面，验证：
- 界面层级正确（MainUI 在底层，Guide 在顶层）
- 动画正常播放（Fade 淡入淡出）
- 安全区适配正常（全屏界面覆盖刘海，普通界面限制在安全区内）
- PauseCoveredUI 行为正常（打开模态弹窗时下方界面暂停）

- [ ] **Step 4: 提交（如有修改）**

```bash
git add -A
git commit -m "chore: 清理 UIConfig 配置表化后的残留引用"
```
