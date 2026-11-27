---
paths:
  - "{Config/**,**/Localization/**}"
---

# 本地化（L10N）

## 体系

- **语种数据化**：6 个语种（简中/繁中/英/日/韩/西），增删语种**零代码改动**——加一列 Excel + 重跑生成即可。
- **产物按语种拆分**：`Unity/Assets/Bundles/Config/Data`（业务表）与 `Unity/Assets/Bundles/Config/Localization`（`tblocalization_<语种>.bytes`）。
- **API 统一入口**：`GetL10nText`。取词双层合并已收敛为单入口，勿再分散调用。
- **英语兜底二段查询**：当前语种缺 key → 回退英语 → 再缺回退 key 本身。

## 本地化表

- 位置：`Config/Excels/Local/*.xlsx`。
- **必须有 `is_code`（bool）列**（缺列 Luban 报错终止）。`is_code=true` 的 key 才导出到 `L10nKey.cs`（代码引用用），false 只进数据产物供表间引用。
- key 为代号式；bean 的 `_Localization_Key` 在反序列化时固化，改 key 名需同步。

## AOT 前置本地化（热更进度条文本）

- 表：`Config/Excels/Local/L-LocalizationAOT-aot-热更前.xlsx`。**第 2 段必须连写 `LocalizationAOT` 成独立表名**，第 3 段 `aot` 是分组；**key 列头必须是 `key`**。
- aot 段**只生成 L10nKey + 数据**（不生成表代码——表代码硬依赖 Hotfix 基类，AOT 不能反向引用）。
- 生成静态类：Hotfix 侧 `L10nKey`，AOT 侧 `LaunchL10nKey`。
- **两段 cs-l10n-key 都必须带 `-x outputSaver.cs-l10n-key.cleanUpOutputDir=false`**——Luban 的 FileCleaner 默认会删掉输出目录中非本 target 的手写文件。
- 运行时：`AOT/Launch/Localization/LaunchLocalization.cs` 自包含解析（`ParseBin` 把行读进字典）。**bin 解析按 Excel 列序硬约定——改表列必须同步 `ParseBin` 的读列顺序**。数据在 `Assets/Resources/LaunchLocalizationText/`。
- `LaunchLocalization.InitializeAsync` **必须先于** `LaunchView.CreateAsync`（声明式 L10n 绑定在构建时即解析）。

## TableManager 接线（定案）

`TableManager.Localization.cs` 提供实例级 `SubscribeLanguageChange()` / `UnsubscribeLanguageChange()` / `OnLanguageChanged` / `RefreshTranslateText()`。`HotfixLauncher.m_TableManager` 只做生命周期簿记：加载前退订旧实例 → 加载 → 订阅新实例并登记。

**关键契约**：退订旧实例与覆盖 `m_TableManager` 必须在**固定次序**中完成，否则热重启泄漏订阅。

**已否决方案（勿再提）**：TableManager 静态单例、HotfixLauncher 静态 handler、放进 ConfigModule。

## 语言偏好双源（必须同步）

- AOT 侧：`PlayerPrefs` 键 `AOT_Localization_Language`（字符串，常量 `LaunchLocalization.LANGUAGE_PREF_KEY`）。
- 热更侧：`StorageModule` 键 `"Language"`。
- **热更侧切语言时必须同步写 PlayerPrefs**（`LocalizationModule.Language` setter 内），`OnInit` 启动时收敛回写——否则下次启动 AOT 界面语言与本侧不一致。

## FGUI 声明式多语言

- 编辑器插件 `FairyGUIProject/plugins/L10nKeyBind/` 把 key 写进组件 customData 的 `L10n:` 段。
- 运行时钩子在 `3rdPlugins/FairyGUI/Scripts/UI/CustomExt/*.L10n.cs`（partial）。
- **动态文本禁绑**（两套来源互覆）；**`INPUTTEXT` 禁绑**。
- 切语言刷新：`WinBase._OnLanguageChanged`（业务绑定）+ `GObject.RefreshAllL10n(WinUI)`。
- 叶子类（GTextField/GButton/GLabel）的 `Setup_AfterAdd` 会在基类应用 L10n 后又从包数据回填默认文本——**三个叶子必须在末尾重应用 `ApplyL10nGearData()`**；新增叶子类型同理。
