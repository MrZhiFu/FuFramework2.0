# 界面体系

| 功能 | 路径 |
|---|---|
| UI 模块主入口 | `Hotfix/Framework/UI/UIModule.cs` |
| 打开 / 关闭 / 获取界面 | `UIModule.Open.cs`、`UIModule.Close.cs`、`UIModule.Get.cs` |
| 层级与分组 | `UIModule.UIGroup.cs`、`UI/UIGroup.cs`、`UIModule.Const.cs` |
| 模糊背景 | `UIModule.Blur.cs` |
| UI 包管理（引用计数） | `UI/Fui/` |
| UI 事件 | `UI/Event/` |
| UI 工具 | `UI/Utility/` |
| 界面基类 | `Hotfix/Framework/UI/View/WinBase*.cs`（`WinBase.cs` / `.Life.cs` / `.EventRegister.cs` 等分部） |
| 业务界面 | `Hotfix/Game/UI/{Bag,Common,Guide,Loading,Login,Main}/` |
| 生成的界面代码 | `Hotfix/Game/AutoGen/UI/`（`*.Gen.cs`，勿手改） |
| FairyGUI 工程 | `FairyGUIProject/` |
| FGUI 编辑器插件 | `FairyGUIProject/plugins/`（含 `CSharpCodeGen`、`L10nKeyBind`） |
| FGUI Lua/TS API 签名参考 | `FairyGUIProject/plugins/LuaAPI/`、`Tools/FairyGUI-Editor-master/plugin/TsAPI/editor.d.ts` |
| FGUI 运行时代码（内嵌） | `Unity/Assets/3rdPlugins/FairyGUI/Scripts/` |
| FGUI L10n 运行时钩子 | `3rdPlugins/FairyGUI/Scripts/UI/CustomExt/*.L10n.cs` |
| 菜单优先级常量 | `Unity/Assets/Editor/FuFramework/Common/FuMenuPriority.cs` |
| AOT 侧启动界面 | `Unity/Assets/Scripts/AOT/Launch/UI/WinLauncher.Gen.cs` |

相关规则：`rules/ui/fgui-workflow.md`
