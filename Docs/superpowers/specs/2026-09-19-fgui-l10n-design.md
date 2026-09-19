# FGUI 多语言插件（编辑器侧 + 运行时链）设计文档

- 日期：2026-09-19
- 状态：待用户审阅
- 分支：refactor/framework-modules-to-hotfix
- 参考方案：`多语言方案1-FGUI编辑器侧.md`、`多语言方案2-运行时解析与应用链.md`（桌面，参考项目已验证实现）

## 1. 背景与目标

本项目 UI 层（FGUI 界面）目前没有任何多语言能力：界面文本在编辑器里写死，或代码 `SetText` 写死。参考项目已验证了一套 FGUI 声明式多语言方案：**编辑器插件把语言 key 写进组件 `customData`，运行时在包构造时解析并自动应用对应语言文本**。本设计将该方案落地到本项目，适配本项目的两个关键差异：

1. **key 语义**：本项目为字符串 key（`L10nKey` 常量 / `TbLocalization` 字符串主键），不采用参考项目的 int langId；
2. **FairyGUI 结构**：本项目为原版结构（经典 `Setup_BeforeAdd/AfterAdd`，无 snapshot 重构）——改造以 **partial 分部 + 最小锚点** 方式落地，不做 snapshot 化。

## 2. 决策记录

| 决策点 | 结论 | 理由 |
|---|---|---|
| 运行时改造方式 | 方案 B：原版结构上最小侵入钩子 | snapshot 化是参考项目对 FairyGUI 的整体重构，为 L10n 做这个过度；原版结构下构造时解析一次，无可测性能差异 |
| 分部文件组织 | **partial 放 `Scripts/UI/CustomExt/`**（用户指定），namespace 仍为 `FairyGUI` | 原版 `UI/` 目录保持纯净，升级 FairyGUI 时整目录替换、分部文件独立保留；锚点收敛为单行 |
| key 语义 | **字符串 key**（编辑器填的 key 即 `L10nKey` / `TbLocalization` 主键） | 本项目整个多语言体系是字符串 key；编辑器插件本身存的就是字符串 |
| 段前缀 | **统一 `L10n:`**（编辑器写入 = 运行时解析） | 参考文档编辑器 `L10n:` 与运行时 `L10n&` 不一致是参考项目缺陷，不采用 |
| 语言切换刷新 | **WinBase 双轨**：`OnOpen()`（业务数据刷新，现状保留）+ `RefreshAllL10n(WinUI)`（声明式 L10n 遍历重应用，新增） | 与本项目 UI 层既定行为对齐（可见界面重走 OnOpen）；不可见界面重开时自然重构造，无需全局注册表 |
| 编辑器插件 | **重新开发**（参考项目实现仅作交互/格式参考），交付 `Tools/FairyGUI-Editor-master/plugin/CustomL10n/` | 用户决策；已起手的 `UIProject/assets/L10nKey/` 面板包并入 |
| 前置 bug 修复 | WinBase 订阅 `"Event.Localization.LanguageChange"`（硬编码字符串与真实 EventId 不匹配，订阅从未生效）——**已修复**（`69a285fd`，改用 `LanguageChangeEventArgs.EventId`） | 刷新链路的接收端此前是断的 |

## 3. 编辑器侧契约（运行时解析的输入约定）

| 项 | 约定 |
|---|---|
| 段格式 | 统一前缀 `L10n:`；customData 按 `\|` 分段与其他插件段共存 |
| 简单模式 | `L10n:{key}`，如 `L10n:common_continue_game` |
| 控制器模式 | `L10n:{ctrlName},{pageId}={key},...`（前提：组件配置 GearText 关联父组件控制器） |
| 组件范围 | GTextField→`text`、GButton→`title`、GLabel→`title` |
| key 语义 | 字符串 key；填错 key 运行时显示 `[key]` 并 LogWarning（开发期可见） |
| 坑约束 | **动态文本禁止绑 L10n**（代码每次 SetText 的数字/时间/进度等）——单一文本来源，绑了 L10n 的组件业务代码不得再 SetText 覆盖 |

**插件交付**：`Tools/FairyGUI-Editor-master/plugin/CustomL10n/`（Lua 插件：inspector 注册 + 输入面板 + 控制器页维护；交互设计参照参考文档 01，注册方式参照本地 `plugin/CustomInspector/` 样例；用户主导开发，已起手的 `L10nKey` 面板包并入）。插件本身存字符串 key，与参考项目通用。

## 4. FairyGUI 运行时改造（partial + 锚点）

### 4.1 新增分部文件（`Scripts/UI/CustomExt/`，namespace `FairyGUI`）

| 文件 | 内容 |
|---|---|
| `GObject.L10n.cs` | ① `_L10nGear` 字段（`IsCtrl` + `SimpleKey` + 控制器 `pageId→key` 表）② 静态委托 `public static Func<string, string> GetLanguageText` ③ 静态 `ParseL10nGear(string customData)`：按 `\|` 分段取 `L10n:` 段，段内含 `,` 且 `=` → 控制器模式 ④ `SetupL10nGear()`：锚点调用入口（从 `this.data` 字段取 customData 字符串 → 调 `ParseL10nGear` → 存 `_L10nGear` → 首次应用 → 控制器模式订阅页变更）⑤ `ApplyChildrenL10n()`：遍历 children 补调 Apply ⑥ `ApplyL10nGearData()`：简单模式直取；控制器模式从 `parent.GetController(ctrlName)` 按当前页取 key ⑦ `ResolveL10nText(key)`：委托空 → 原样返回 key ⑧ `ShouldHandleL10nGear()` / `OnApplyL10nText(key)` **virtual 声明**（基类 false/空）⑨ 静态 `RefreshAllL10n(GComponent root)`：递归遍历 children，命中组件重调 Apply（按当前控制器页）⑩ 控制器 `onStateChanged` 订阅/退订（`DisposeL10n()`） |
| `GTextField.L10n.cs` | `override ShouldHandleL10nGear()=>true`；`override OnApplyL10nText` → `this.text = ResolveL10nText(key)` |
| `GButton.L10n.cs` | 同上 → `this.title` |
| `GLabel.L10n.cs` | 同上 → `this.title` |

### 4.2 原版文件锚点（每处 1-2 行，升级冲突面收敛）

| 原版文件 | 锚点 |
|---|---|
| `GObject.cs` | `Setup_AfterAdd` 末尾 → `SetupL10nGear();`（非组件类型如 GTextField 的应用点）；`Dispose` → `DisposeL10n();`（控制器退订，防泄漏） |
| `GComponent.cs` | `Setup_AfterAdd` 末尾 → `SetupL10nGear();` + `ApplyChildrenL10n();`（children 补调，控制器此时已就绪——FGUI 包格式 controller 段先于 children） |

**实施事实（计划阶段源码核对，推翻原验证点）**：原版 `GObject.Setup_BeforeAdd` 末尾（`GObject.cs:1969-1971`）已把编辑器 customData（`ReadS()` 字符串索引）读入 **`public object data` 公开字段**，且对所有组件类型通用（GButton/GLabel 经 GComponent 的 base 调用、GTextField 经自身 base 调用）——**无需动任何包读取点，无需包格式逆向**；`GComponent.cs:1535` 的 `Skip(2)` 注释有误导（组件特有属性段数据，与编辑器 customData 无关），保持不动。

### 4.3 包格式验证点（已在计划阶段解决）

- 原版 `GObject.Setup_BeforeAdd` 末尾已通用读取编辑器 customData 到 `public object data` 字段（`ReadS()` 字符串索引），所有组件类型覆盖——**无需包格式逆向，也无需参考项目源码对照**（spec 第 9 节实施前提取消）
- `GComponent.cs:1535` 的 `Skip(2)` 为组件特有属性段数据（注释"customData"误导），保持原样

## 5. 热更注入与刷新（两阶段委托注入）

```csharp
// AOT 阶段（LaunchLocalization.InitializeAsync 之后）——WinLauncher 界面也可绑 L10n
FairyGUI.GObject.GetLanguageText = key => LaunchLocalization.GetLanguage(key);

// 热更阶段（HotfixLauncher.InitDependenciesAsync，Provider 设置处）——覆盖为热更表
FairyGUI.GObject.GetLanguageText = key => LocalizationModule.Instance.GetLanguageText(key);
```

**WinBase 双轨刷新**（`_OnLanguageChanged`，订阅修复已完成）：

```csharp
private void _OnLanguageChanged(object sender, GameEventArgs e)
{
    if (Visible)
    {
        OnOpen();                                    // 现状保留：业务数据绑定刷新
        FairyGUI.GObject.RefreshAllL10n(WinUI);      // 新增：声明式 L10n 遍历重应用
    }
}
```

不可见界面不处理（下次打开重构造自然生效）；`WinLauncher`（AOT 界面）不走 WinBase，但启动期不切语言，构造时应用即正确。

## 6. 错误处理与边界

| 场景 | 行为 |
|---|---|
| key 查不到 | `GetLanguageText` 现行为：LogWarning + 返回 `[key]`——填错 key 开发期立即可见 |
| customData 无 L10n 段 / 非 L10n 组件 | 解析为空 + `ShouldHandleL10nGear()=false`，零开销跳过 |
| 委托未注入（Editor 非运行态 / AOT 早期） | 返回 key 原样显示 |
| 列表项池化复用 | 重走构造补调时重置 `_L10nGear` 并重应用（与参考项目行为一致） |
| 控制器被删除 | 组件 Apply 时 `GetController` 返回 null → 退回简单模式首 key 或跳过（实施时对齐参考行为） |

## 7. 验证计划

1. **编辑器侧**：FGUI 编辑器绑定 key → 发布 → 包内 customData 含 `L10n:` 段；面板对控制器组件逐页填 key
2. **运行时冒烟**（Play）：绑定组件显示对应语言文本 → `Language` 切换 → 可见界面（WinBase 双轨）文本即时刷新 → 控制器模式按页正确
3. **边界**：未绑组件零影响、key 填错显示 `[key]`、列表池化复用正确、委托未注入时原样显示 key
4. **AOT 阶段**：`WinLauncher` 若绑 L10n key，AOT 注入生效显示 AOT 表文本

## 8. 边界与不做的事

- 不做 snapshot 化重构（原版结构保持）
- 不做参考项目的 int langId / `LanguageConfig` blob（本项目字符串 key + Luban 表）
- 不做自动刷新的全局注册表（刷新范围=可见界面，走 WinBase 遍历）
- AOT 阶段的 `WinLauncher` L10n 绑定列为可选项（AOT 注入支持已包含）

## 9. 实施前提

- ~~参考项目的 FairyGUI 改造源码对照~~（已取消：计划阶段核对确认 customData 已在 `data` 字段，无包格式依赖）
- 编辑器插件开发由用户主导（`CustomL10n/`），本设计的 3 节契约是运行时解析的输入约定，插件实现须与之对齐
