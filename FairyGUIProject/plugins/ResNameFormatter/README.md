# ResNameFormatter - 资源名称格式化工具

FairyGUI 编辑器插件，用于批量格式化资源名称，统一编辑器中的资源管理。

## 功能

- 遍历全部包的资源条目，按资源类型（image/component/font 等）分发命名规则执行重命名
- 重命名经 `pkg:RenameItem` 执行，保证 package.xml 与编辑器状态同步
- 变更通过控制台日志输出 OldName/NewName，完成后自动刷新工程

> **当前状态**：插件骨架已就绪，各类型的具体命名规则尚未实现（`ResNameFormatter.lua` 的 `getNewName` 中各类型分支为 todo，执行时不产生改名）。接入具体规则后即可批量生效。

## 使用方式

菜单路径：**工具 → 自定义-资源名称格式化器**（单击直接执行）

## 文件结构

```
ResNameFormatter/
├── main.lua               ← 插件入口（菜单注册、生命周期）
├── ResNameFormatter.lua   ← 核心逻辑（遍历资源、按类型分发重命名）
├── Tool.lua               ← 日志工具
├── package.json           ← 插件描述
└── README.md              ← 本文件
```
