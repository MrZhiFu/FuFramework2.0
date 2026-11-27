---
paths:
  - "{FairyGUIProject/**,**/UI/**}"
---

# FairyGUI 工作流

## 查 API 签名

编辑器本体是 IL2CPP 打包（无托管 DLL 可查），网络文档常受限。本地两处是权威：

- `Tools/FairyGUI-Editor-master/plugin/LuaAPI/`——每类型一个文件（813 个类型）
- `Tools/FairyGUI-Editor-master/plugin/TsAPI/editor.d.ts`——全量 TS 版

## 验证方式

- 插件跑在 FairyGUI 编辑器内，**unity-cli 管不到**——运行时验证只能由用户在编辑器实测（重开编辑器重载插件）。
- 产物验证用「发布后 `git diff` 对照」。

## 发布与查询：fairygui-cli（已接入）

CLI 与插件已随项目提供：`FairyGUIProject/fairygui-cli.exe` + `FairyGUIProject/plugins/MCPBridge/`（编辑器插件，通信目录在 `bridge/`，已 gitignore）。

**发布（全自动，推荐）**：

```bash
bash .claude/publish-ui.sh              # 发布全部包
bash .claude/publish-ui.sh Login Main   # 只发布指定包
bash .claude/publish-ui.sh --check      # 只自检编辑器链路
```

脚本会：拉起编辑器（未开时）→ 等工程就绪 → 发布 → 落盘稳定门禁 → 产物自检（FGUI 魔数 + mtime）→ 关闭自己拉起的编辑器（复用的不关）。

**只读查询（不需要编辑器）**：

```bash
FairyGUIProject/fairygui-cli.exe packages list                    # 包列表
FairyGUIProject/fairygui-cli.exe resources list Login             # 包内资源
FairyGUIProject/fairygui-cli.exe components read Login WinLogin   # 读组件原始 XML
FairyGUIProject/fairygui-cli.exe components validate Login WinLogin
```

**编辑器内操作（需编辑器在线，脚本会自动拉起）**：

```bash
fairygui-cli editor open <包> <组件>      # 打开组件预览
fairygui-cli editor screenshot            # 编辑器/预览截图
fairygui-cli editor start-test <包> <组件> --device <设备>
```

**已知坑**：

- `fairygui-cli status` 的 `editor_process` 对 `Game.fairy` 恒报 down（它按特定窗口特征匹配），**但插件实际正常**——判断在线要用实际命令探测，不要依赖 `status.overall`。
- 同一工程开多个编辑器实例会互相抢 bridge 命令，发布结果不可信；脚本会检测并拒绝。
- 发布后插件内部状态若卡在 `previous publish still in progress`，重启编辑器即可清除。

## 编辑器插件开发

改 / 扩展 `FairyGUIProject/plugins/` 下的插件（含 `MCPBridge`）时：

| 资源 | 位置 |
|---|---|
| **编辑器完整 API 签名**（813 个类型，每类一文件）| `Tools/FairyGUI-Editor-master/plugin/LuaAPI/` |
| 最小插件示例 | `Tools/FairyGUI-Editor-master/plugin/HelloWorld/`（`main.lua` + `package.json`）|
| 完整插件示例（自定义属性面板）| `Tools/FairyGUI-Editor-master/plugin/CustomInspector/` |
| TS 声明（全量）| `Tools/FairyGUI-Editor-master/plugin/TsAPI/editor.d.ts` |
| 编辑器自身界面工程 | `Tools/FairyGUI-Editor-master/ui/FairyGUI-Editor.fairy`（只有改编辑器 UI 才动）|

改完插件必须**重开 FairyGUI 编辑器**才加载新版；`publish-ui.sh` 拉起编辑器时自然用的是最新版。

## CSharpCodeGen 架构决策（已定案，勿反向）

- **`InitUIEvent` 归手写层**：首次生成自动填注册代码，之后开发自管。Gen 层 `ConstructFromXML` 保留调用（与 `OnInit` 同模式）。
  **不能放回 Gen 层**——存量手写 `WinXxx.cs` 的 `OnInit` 调用它，Gen 层条件生成会让新增控件静默漏接线。
- **事件白名单**在 `GenCommon.lua` 的 `COMP_EVENT_CONFIG`（数据驱动，加类型 = 加一行）。未收录类型（GImage 等纯展示）不生成事件。
- 包级开关：未勾选「为本包生成代码」（`publishSettings.genCode`）则跳过生成。
- 手写文件只在首次生成时创建，**之后永不覆盖**；孤儿文件由 `CleanupOrphanedFiles` 自动清理。

## GetChild 不做索引化

**不要改 `GetChild(name)` 为 `GetChildAt(index)`**——层级调整会静默错位，收益纳秒级。百级子对象的巨型界面正解是**拆 Comp 组件**（项目已有 `CompXXX` 体系）。

## 手写 XML 的陷阱

修改包内已有 XML（如 `SetL10nKey.xml`）后编辑器重发布会生效；但**新增组件 XML 编辑器不会自动导入**（即使 `package.xml` 手动注册了 id），除非重开工程。否则发布产物缺资源 → `defaultItem` URL 解析为 null → `GList.AddItemFromPool` 内部 NRE（不返回 nil）。

诊断：用脚本扫 `Xxx_fui.bytes` 确认组件名字符串出现次数。修复：重开工程 → 确认资源库出现 → 重新发布。

## 被引用的组件必须标 exported="true"

**`GList` 的 `defaultItem`、`GearIcon` 的引用组件，必须在包 `package.xml` 里声明 `exported="true"`**：

```xml
<component id="l50q5" name="CompBagItem.xml" path="/Comp/" exported="true"/>
```

违反：组件不进发布产物 → 运行时 `AddItemFromPool` 取不到项（空白项或 NRE），而且**发布不会报任何错**。

为什么必须手动标：client-ai 那套用 `MyGenCode` 插件在发布前自动置位（`EnsureItemReferencedExported`），**本项目没有该插件**，MCPBridge 发布时会打警告 `MyGenCode 置位函数不可用` 然后跳过。所以只能靠 `package.xml` 里的静态声明。

检查方法：`grep -o 'defaultItem="ui://[^"]*"' <组件>.xml` 拿到引用，再到 `package.xml` 确认对应 `id` 带 `exported="true"`，最后 `grep -ac <组件名> Xxx_fui.bytes` 确认进了产物。

## 菜单优先级

## 菜单优先级

新增 Editor 菜单**必须先在 `Unity/Assets/Editor/FuFramework/Common/FuMenuPriority.cs` 登记常量**，禁止写数字字面量。按组分段（组基准间隔 ≥ 11，Unity 自动加分隔线）。跨命名空间引用该目录类型时沿用 using 别名惯例。

## 运行时 UI 约定

- `WinBase` 是界面基类（`Framework/UI/View/WinBase*.cs`）。
- 层级 `EUILayer`：`SceneUI=0` / `MainUI=1500` / `Normal=2000` / `Window=2500` / `Tips=3000` / `Guide=3500` / `Loading=4000`（值越大越靠前）。
- UI 包经 `FuiPkgManager` 加载，支持引用计数与依赖自动加载。
- 生命周期拆分：`InitUIComp → InitUIEvent → InitEvent → InitRedDot`。
- 声明式多语言绑定见 `framework/localization.md`。
