# AOT 启动链

| 功能 | 路径 |
|---|---|
| 入口 MonoBehaviour | `Unity/Assets/Scripts/AOT/Launcher.cs` |
| 启动流程编排 | `AOT/Launch/LaunchProcess.cs` |
| 启动界面 | `AOT/Launch/LaunchView.cs`、`ILaunchView.cs` |
| 启动资源加载 | `AOT/Launch/LaunchAssetHelper.cs` |
| 更新配置 | `AOT/Launch/UpdateConfig/` |
| AOT 本地化（进度条文本） | `AOT/Launch/Localization/LaunchLocalization.cs` |
| 全局设置 | `AOT/GameSetting.cs` |
| AOT 日志 / 扩展 / 工具 | `AOT/Framework/{Log,Extension,Utility}/` |
| 热更入口（模块注册） | `Hotfix/HotfixLauncher.cs` |
| 热更程序集定义 | `Hotfix/HotFix.asmdef` |
| AOT 程序集定义 | `AOT/AOT.asmdef` |
| 热更 DLL 产物 | `Unity/Assets/Bundles/Code/`（菜单 `FuFramework/Build/Copy Hotfix Code(复制热更新代码DLL到Assets>Bundles>Code)`） |
| AOT DLL 产物 | `Unity/Assets/Bundles/AOTCode/`（菜单 `FuFramework/Build/Copy AOT Code(复制AOT代码DLL到Assets>Bundles>AOTCode)`） |
| HybridCLR 配置 | `Unity/HybridCLRData/` |
| 服务端启动器 | `Server/FuFramework.Launcher/`（`quick_start-game.bat`） |

相关规则：`rules/architecture/project-structure.md`
