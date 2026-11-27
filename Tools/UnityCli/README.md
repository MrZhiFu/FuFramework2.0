# Unity CLI

`unity-cli` 是 Unity Editor 的 CLI 工具，允许通过命令行（或 Claude Code）直接操控 Unity 编辑器：编译、Play、读运行时状态、注入 C# 代码求值等。

## 位置与使用

二进制**随仓库提供**，无需安装：

```
Tools/UnityCli/unity-cli.exe
```

调用（在**项目根**执行）：

```bash
Tools/UnityCli/unity-cli.exe system ping
Tools/UnityCli/unity-cli.exe tool call eval_csharp --json '{"code":"UnityEngine.Application.unityVersion"}'
```

需先打开 Unity 项目，bridge 包会随包管理器自动加载。

## 为什么随仓库提供

CLI 与 Unity 侧 bridge 包的版本**必须一致**（不一致会警告甚至命令失败）。两者同处一个仓库，升级时一并提交，版本永远对齐。

bridge 版本声明在 `Unity/Packages/manifest.json`：

```json
"com.akiojin.unity-cli-bridge": "https://github.com/akiojin/unity-cli.git?path=UnityCliBridge/Packages/unity-cli-bridge#<commit>"
```

## 升级流程

1. 从 [GitHub Releases](https://github.com/akiojin/unity-cli/releases/latest) 下载 `unity-cli-win-x64`，覆盖本目录的 `unity-cli.exe`
2. 把 `manifest.json` 里 bridge 的 commit hash 改成同版本
3. 删除 `Unity/Library/PackageCache` 下的旧 bridge 目录
4. 重启 Unity，等它重新拉取并编译 bridge
5. 验证：`Tools/UnityCli/unity-cli.exe system ping` 应返回 `"CLI and bridge are both <版本>"`

> 本机 GitHub release 下载常因网络中断失败，用 `curl -L -C -` 断点续传补救。

## 新机器必做：关闭 CLI 自更新

**`unity-cli.exe` 是版本库文件**，而 CLI 默认会定时检查 GitHub 更新并在下次启动时替换自身。这会导致两个后果：

- 工作区出现一个 35 MB 的二进制改动（版本失控）
- 首次运行阻塞约 135 秒（GitHub 访问慢时）

克隆仓库后**先设置**（用户级环境变量，配一次永久生效）：

```powershell
[Environment]::SetEnvironmentVariable('UNITY_CLI_NO_AUTO_UPDATE','1','User')
[Environment]::SetEnvironmentVariable('UNITY_CLI_HOST','127.0.0.1','User')
```

新终端生效。`.claude/verify-compile.sh` 内部已自带这两项设置，但**手动执行 `unity-cli` 命令时仍依赖用户级变量**。

## 完整能力

见 `.claude/rules/tooling/unity-cli.md`（AI 常驻加载）。
