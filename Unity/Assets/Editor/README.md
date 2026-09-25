# Editor — Unity 编辑器扩展

FuFramework 的编辑器专用代码：构建出包、热更管线、脚本宏、调试面板、资源导入规则等。
由 `Unity.Editor.asmdef` 统一编译为**仅 Editor 平台**生效的程序集，不参与运行时构建。

## 目录结构

```
Editor/
├── Unity.Editor.asmdef             # 程序集定义（引用 YooAsset / HybridCLR / AOT，仅 Editor）
├── ProjectSettingEditor.cs         # 编辑器启动时自动统一 PlayerSettings 关键项，并创建 Bundle 所需目录
├── UITextureAssetPostprocessor.cs  # UI 纹理导入自动设置（平台压缩格式、关 Mipmap、关可读）
└── FuFramework/                    # 框架编辑器工具
    ├── Common/
    │   ├── BuildProduct/           # 各平台出包（Windows / MacOS / Apk / AAB / WebGL / 微信小游戏 / Xcode）
    │   ├── BuildHotfix/            # 热更 / AOT DLL 复制、HotFix.asmdef 编辑器兼容标记（HybridCLR）
    │   ├── BuildWebGLTools/        # WebGL + HybridCLR il2cpp 目录设置命令行生成
    │   ├── Cropping/               # 代码防裁剪工具窗口
    │   ├── MiniGame/               # 微信 / 抖音小游戏宏定义开关
    │   ├── Misc/                   # 杂项：打开目录、删除本地数据、启动 HttpCDN、批处理执行、类型查询
    │   ├── Symbol/                 # 脚本宏定义：日志级别、网络日志、网络类型、SRDebugger
    │   ├── Toolbar/                # 编辑器顶部工具栏扩展（快速切场景、打开 C# 工程）
    │   ├── Inspector/              # Inspector 基类（提供编译开始 / 完成事件）
    │   └── FuMenuPriority.cs       # 所有菜单项 priority 集中定义（新增菜单必须在此登记）
    ├── Config/                     # 配置表导出（Json / Bin）、配置调试面板
    ├── Proto/                      # Proto 导出（客户端 / 服务端）
    ├── Localization/               # 多语言 key 清理（生成报告 / 查看报告 / 清理表）
    ├── ModuleSetting/              # GameSetting 资源 Inspector
    ├── Event/FSM/Timer/Inspector/  # 各模块 Inspector（预留）
    └── ObjectPool|RedDot|ReferencePool|Web/  # 各模块调试面板
```

## 菜单入口

编辑器顶部菜单 `FuFramework`，分组与顺序由 `FuMenuPriority` 统一管理：

| 分组 | 内容 |
| --- | --- |
| MiniGame | 微信 / 抖音小游戏宏开关 |
| Build | 各平台出包、热更 DLL 复制、asmdef 标记、HybridCLR 命令行 |
| 打开文件夹 | Data / Persistent / StreamingAssets / TempCache / ConsoleLog |
| 日志设置 | 日志总开关、级别宏开关、网络请求 / 响应日志 |
| SRDebugger工具 | SRDebugger 开关 |
| 配置表 | 导出 Json / Bin |
| 多语言检查 | 报告生成 / 查看 / 清理 |
| Proto | 客户端 / 服务端 / 全部导出 |
| 网络类型设置 | WebSocket 强制开关 |
| 工具类 | 代码防裁剪、删除本地游戏数据、启动 HttpCDN |
| 调试 | 配置 / 对象池 / 红点 / 引用池 / Web 调试面板（仅 Play 模式） |

## 注意事项

- **新增 / 调整菜单**：priority 一律引用 `FuMenuPriority` 常量，禁止写字面量；整组调序只改分组基准值。
- **跨命名空间引用常量**：使用别名 `using FuMenuPriority = FuFramework.Core.Editor.FuMenuPriority;`——裸 `using FuFramework.Core.Editor;` 会与 `Common/Misc/Type.cs` 中的 `Type` 类产生 CS0104 二义性。
- **ProjectSettingEditor**：每次编辑器启动强制覆盖包名、Splash、屏幕方向等关键设置，手动修改会被重置（有意为之的安全网）。
- **调试面板**（配置 / 对象池 / 红点 / 引用池 / Web）仅在 Play 模式下可用，通过反射访问 Hotfix 程序集中的模块实例。