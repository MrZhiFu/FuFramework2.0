# CLAUDE.md

此文件为 Claude Code (claude.ai/code) 在本仓库中工作时提供指导。

## Unity Editor 自动化 (unity-cli)

本项目已配置 unity-cli，Claude Code 可通过 TCP 直接操控 Unity Editor（创建场景、操作 GameObject、编译代码、Play/Stop、读写 C# 文件等）。

- **安装**: Windows 双击 `Tools/UnityCli/install-unity-cli.bat`，macOS 运行 `bash Tools/UnityCli/install-unity-cli.sh`
- **验证**: `unity-cli system ping` → 返回 `"pong"` 即连通
- **详请**: `Tools/UnityCli/README.md`

## 项目架构

详见 `Docs/项目架构与技术栈.md`。

## 交流语言

**必须严格遵守**，全程使用中文交流。

## Git 提交规范

**必须严格遵守：**`Docs/Git提交规范.md`。

## 代码风格规范

**必须严格遵守：**`Docs/代码风格规范.md`。
