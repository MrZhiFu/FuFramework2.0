# L10nKeyBind 多语言绑定插件

为 FairyGUI 编辑器提供多语言 key 的可视化绑定：选中组件 → Inspector 面板输入多语言 key → 以 `L10n:<key>` 段写入组件 `customData` → 随包发布。

## 使用

1. 在组件文档中选中**可绑定类型**的组件（`TEXT` 文本 / `RICHTEXT` 富文本 / `EXT_BUTTON` 按钮 / `EXT_LABEL` 标签）；
2. 右侧 Inspector 出现「多语言绑定」面板，输入多语言 key（如 `common_continue_game`）；
3. 失焦后写入组件 `customData`（支持 Ctrl+Z 撤销）；清空输入即解除绑定；
4. 发布包后，运行时自动按当前语言显示文本（无需业务代码参与）。

## 约定与坑

- **可绑定类型**二级判别（`main.lua`）：先按 `objectType`（`text` / `richtext` / `component`），component 再按 `extentionId`（`button` / `label`）；未收录值会输出诊断。`INPUTTEXT`（输入框）为运行时动态输入，**禁止绑定**。
- **Inspector 按 objectType 路由**：`ConnectInspector` 的对象类型参数是选中对象的 `objectType` 过滤，故拆分 `L10nKey_set_com`（component）/ `L10nKey_set_text`（text）/ `L10nKey_set_richtext`（richtext）三个连接，共用面板实现。
- **动态文本禁止绑**：代码每次打开会 `SetText` 的组件（数字/时间/进度）不要绑 L10n，两套文本来源会互相覆盖。
- **customData 分段共存**：`L10n:` 段与其他插件段（如 `red_dot:`）以 `|` 分隔互不干扰。
- **控制器模式**（逐页 key，`L10n:ctrl,0=key1,1=key2`）运行时已兼容（手填生效），编辑器交互按需扩展。

## 运行时链路

包构造时 `GObject.SetupL10nGear` 解析 `L10n:` 段 → `GetLanguageText` 委托（**两阶段注入**：AOT 阶段注 `LaunchLocalization.GetLanguage`，热更后由 `HotfixLauncher` 覆盖为 `LocalizationModule.GetLanguageText`）→ 文本写入 `text`/`title`；语言切换时 `WinBase._OnLanguageChanged` 调 `RefreshAllL10n` 遍历重应用。详见 `Docs/superpowers/specs/2026-09-19-fgui-l10n-design.md`。

## 文件

| 文件 | 说明 |
|---|---|
| `main.lua` | 插件入口：Inspector 注册（component/text/richtext 三路）、面板交互 |
| `common.lua` | customData 的 `L10n:` 段读写库（`|` 分段共存） |
| `L10nKey_fui.bytes` | 面板包（源工程 `Tools/FairyGUI-Editor-master/plugin/CustomInspector/UIProject/assets/L10nKey/`） |
| `icon.png` | 插件图标 |
| `package.json` | 插件清单 |
