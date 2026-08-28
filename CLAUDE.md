# CLAUDE.md

此文件为 Claude Code (claude.ai/code) 在本仓库中工作时提供指导。

## Unity Editor 自动化 (unity-cli)

本项目已配置 unity-cli，Claude Code 可通过 TCP 直接操控 Unity Editor（创建场景、操作 GameObject、编译代码、Play/Stop、读写 C# 文件等）。

- **安装**: Windows 双击 `Tools/UnityCli/install-unity-cli.bat`，macOS 运行 `bash Tools/UnityCli/install-unity-cli.sh`
- **验证**: `unity-cli system ping` → 返回 `"pong"` 即连通
- **详请**: `Tools/UnityCli/README.md`

## 项目架构

详见 `Docs/项目架构与技术栈.md`。

## 代码铁律

**必须严格遵守**，以下五项为硬性铁律，禁止以任何理由违反：

1. **杜绝使用原生 `Task`**，一律使用 `UniTask` 代替；
2. **杜绝使用 Unity 协程（`Coroutine`）**，一律使用 `UniTask` 代替；
3. **杜绝使用 LINQ**，一律使用手写循环与缓存代替；
4. **运行时杜绝使用反射（`Reflection`）**；
5. **每个异步链必须有生命周期所有者**：发起异步任务的顶层对象持有 `LifecycleCancellationSource` 并于生命周期结束 `Cancel`（窗口 OnClose、模块 OnDispose）；helper 方法透传调用方的 `CancellationToken`，不自持；网络/资源类异步 API（Web/Asset/Entity/Scene）的 `CancellationToken` 参数必传（无默认值）。

## 交流语言

**必须严格遵守**，全程使用中文交流。

## Git 提交规范

**必须严格遵守：**`Docs/Git提交规范.md`。

## 代码风格规范

**必须严格遵守：**`Docs/代码风格规范.md`。
