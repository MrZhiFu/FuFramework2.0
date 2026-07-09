# CLAUDE.md

此文件为 Claude Code (claude.ai/code) 在本仓库中工作时提供指导。

## 交流语言

全程使用中文交流。

## Git 提交规范

详见 `Docs/Git提交规范.md`。格式：`<type>: <中文简短描述>`

## 项目架构概要

> 完整文档：`Docs/项目架构与技术栈.md`

### AOT / 热更分离 (HybridCLR) —— 核心红线
- **AOT** (`Scripts/AOT/`) — 启动、更新、加载热更 DLL。**绝不能引用热更程序集**。
- **热更** (`Scripts/Hotfix/`) — 所有业务逻辑。可以引用 AOT。
- 反射入口：`HotfixLauncher.MainAsync()`

### 模块系统
- 所有功能都是 `FuModule` 子类，按固定顺序注册（基础模块 → 功能模块）。
- 通过 `GlobalModule.<模块名>` 全局访问，延迟初始化单例。
- 依赖通过 `[ModuleDependency(typeof(XxxModule))]` 声明。

### UI 系统 (FairyGUI)
- `ViewBase` 基类，生命周期：`OnInit → OnOpen → OnClose → OnDispose`。
- 每个界面属于一个 `EUILayer`，通过 `FuiPkgManager` 加载 UI 包。
- 子步骤：`InitUIComp() → InitUIEvent() → InitEvent() → InitRedDot()`

### 其他关键约定
- 编辑器版本 2022.3.33f1c1，无自动化测试，手动在 Editor 中测试。
- 日志用 `FuLogger`，第三方代码放 `3rdPlugins/`，每个模块有 README。

## 代码风格规范

**必须严格遵守**，详见 `Docs/FuFramework代码风格规范.md`。
写 `.cs` 文件时 `.claude/rules/csharp.md` 会自动加载检查清单。
