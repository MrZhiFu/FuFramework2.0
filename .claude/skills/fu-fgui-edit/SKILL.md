---
name: fu-fgui-edit
description: FairyGUI 界面与插件的编辑操作流：改组件 → 发布 → 代码生成 → Unity 侧验证。用户在 FairyGUIProject 下改界面包、增删组件、调整绑定、或修改 FGUI 编辑器插件时使用。约束细则见 rules/ui/fgui-workflow.md。
---

# FairyGUI 编辑流程

约束细则以 `rules/ui/fgui-workflow.md` 为准，本技能只讲**操作顺序**。

## 1. 动手前

- **查 API 签名**：`Tools/FairyGUI-Editor-master/plugin/LuaAPI/<类型>.lua`（813 个类型）；全量 TS 版在同目录 `../TsAPI/editor.d.ts`。
  编辑器本体是 IL2CPP 打包，没有托管 DLL 可查；网络文档常受限。
- 读 `rules/ui/fgui-workflow.md` 与 `memory/traps-ui.md`，确认不踩已知坑。

## 2. 改界面包

| 动作 | 注意 |
|---|---|
| 改已有组件/控件 | 发布即生效 |
| **新增组件 XML** | 编辑器**不会自动导入**——必须重开工程确认资源库出现该组件，否则发布产物缺资源（表现为列表取默认项时空引用，不返回 null） |
| 改绑定的 L10n key | 写在组件 customData 的 `L10n:` 段 |
| 需要事件接线 | 落在**手写层**（Gen 层只生成首次骨架）；事件白名单在 `GenCommon.lua` 的 `COMP_EVENT_CONFIG` |
| 纯展示组件（如图片） | 不在白名单内，不生成事件 |

## 3. 发布（命令行，全自动）

```bash
bash .claude/publish-ui.sh              # 全部包
bash .claude/publish-ui.sh Login Login  # 指定包（可多个）
```

脚本会自动拉起 FairyGUI 编辑器、等工程就绪、发布、校验产物（FGUI 魔数 + mtime），跑完关闭它自己拉起的实例。**不需要你手动开编辑器点发布。**

只读查询（连编辑器都不用开）：

```bash
FairyGUIProject/fairygui-cli.exe components read Login WinLogin     # 读组件 XML
FairyGUIProject/fairygui-cli.exe packages list
FairyGUIProject/fairygui-cli.exe resources list Login
```

## 4. 验证

1. **产物 diff**：发布后 `git diff` 看生成的 `*.Gen.cs` 是否符合预期。
2. **编译**：`bash .claude/fu-unity-compile.sh`。
3. **运行时验收**：`play_game` → `eval_csharp` 查 FGUI 层级、点按钮、拖拽（见 `rules/tooling/unity-cli.md` 的 FairyGUI 自动化段）。

## 5. 插件开发

- 插件源码：`FairyGUIProject/plugins/`（含 `MCPBridge`）
- 改完插件后**重开 FairyGUI 编辑器**重载（`publish-ui.sh` 自动拉起时即加载最新）
- 面板源在 `Tools/FairyGUI-Editor-master/plugin/CustomInspector/UIProject/assets/` 时，注意同步

## 边界

- 只做 FairyGUI 侧改动，不碰业务逻辑 → 本技能
- 界面 + 业务逻辑一起改 → `fu-standard-dev`
