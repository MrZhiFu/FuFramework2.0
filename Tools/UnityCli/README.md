# Unity CLI 安装指南

## 简介

`unity-cli` 是 Unity Editor 的 CLI 工具，允许通过命令行（或 Claude Code）直接操控 Unity 编辑器，例如创建场景、操作 GameObject、编译代码、运行测试等。

本项目已在 `Packages/manifest.json` 中配置了 bridge 包，打开 Unity 项目时自动恢复。

## 一键安装

| 平台 | 文件 | 方式 |
|------|------|------|
| Windows | `install-unity-cli.bat` | 双击运行 |
| macOS | `install-unity-cli.sh` | `bash install-unity-cli.sh` |

脚本自动完成：下载 → 安装 → 配置 PATH。

## 验证

重启终端后运行：

```bash
unity-cli system ping
```

返回 `"pong"` 即表示连接成功（需提前打开 Unity 项目）。

## 手动安装

如果自动脚本失败，可手动操作：

1. 从 [GitHub Releases](https://github.com/akiojin/unity-cli/releases/latest) 下载对应平台的二进制文件
2. 放到任意目录，将目录加入系统 PATH
3. 打开 Unity 项目，bridge 包会自动加载

## 常用命令

| 命令 | 功能 |
|------|------|
| `unity-cli system ping` | 测试连接 |
| `unity-cli tool list` | 列出所有可用工具 |
| `unity-cli scene create` | 创建场景 |
| `unity-cli instances list` | 列出场景中的 GameObject |

## Claude Code 可用 Skill

安装 marketplace 插件 `akiojin/unity-cli` 后，直接用自然语言与 Claude Code 交流即可操作 Unity，无需手动拼命令。

| Skill | 用途 | 触发示例 |
|-------|------|----------|
| unity-scene-create | 创建场景、放置 GameObject | "新建一个场景"，"放一个立方体" |
| unity-scene-inspect | 查看场景内容 | "当前场景有哪些对象" |
| unity-gameobject-edit | 修改已有 GameObject | "把 Cube 移到 (1,2,3)" |
| unity-csharp-edit | 通过 LSP 修改 C# 代码 | "给 Player 添加一个方法" |
| unity-csharp-navigate | 查找符号、跳转 | "找到 xxx 的引用" |
| unity-csharp-reference | 代码库搜索 | "搜索所有 MonoBehaviour" |
| unity-editor-tools | 编辑器操作 | "Play"，"清控制台" |
| unity-prefab-workflow | Prefab 操作 | "打开 Prefab 编辑" |
| unity-input-system | 输入系统管理 | "添加 Input Action" |
| unity-asset-management | 资源管理 | "创建材质"，"刷新资源" |
| unity-addressables | Addressables 管理 | "构建 Addressables" |
| unity-playmode-testing | 运行时测试 | "进入 Play 模式测试" |
| unity-ui-automation | UI 自动化 | "点击按钮" |
| unity-development-loop | 开发迭代流程 | "编译并测试" |
| unity-cli-usage | 连接检查、工具发现 | "检查 Unity 连接" |
