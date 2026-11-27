---
name: fu-fgui-edit
description: FairyGUI 界面与插件的编辑操作流：改组件 → 发布 → 代码生成 → Unity 侧验证。用户在 FairyGUIProject 下改界面包、增删组件、调整绑定、或修改 FGUI 编辑器插件时使用。约束细则见 rules/ui/fgui-workflow.md。
---

# FairyGUI 编辑流程

约束细则以 `rules/ui/fgui-workflow.md` 为准，本技能只讲**操作顺序**。

## 1. 动手前

- **查 API 签名**：`FairyGUIProject/plugins/LuaAPI/<类型>.lua`；全量 TS 版在 `Tools/FairyGUI-Editor-master/plugin/TsAPI/editor.d.ts`。
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

## 3. 发布

在 FairyGUI 编辑器内发布。确认：

- 包勾选了「为本包生成代码」（否则跳过生成）
- 手写文件不会被覆盖（只在首次生成时创建）
- 孤儿文件由生成器自动清理

## 4. 验证

1. **产物 diff**：发布后 `git diff` 看生成的 `*.Gen.cs` 是否符合预期。
2. **编译**：`bash .claude/verify-compile.sh`。
3. **运行时**：由用户在 Unity 里实测（FGUI 编辑器插件本身 unity-cli 管不到，只能靠用户在编辑器内验证）。

## 5. 插件开发

- 插件源码：`FairyGUIProject/plugins/`
- 改完让用户**重开 FairyGUI 编辑器**重载插件后再实测。
- 面板源在 `Tools/FairyGUI-Editor-master/plugin/CustomInspector/UIProject/assets/` 时，注意同步。

## 边界

- 只做 FairyGUI 侧改动，不碰业务逻辑 → 本技能
- 界面 + 业务逻辑一起改 → `fu-standard-dev`
