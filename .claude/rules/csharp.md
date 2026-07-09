---
globs: "**/*.cs"
version: 1.0.0
description: 打开 .cs 文件时自动加载 FuFramework C# 代码风格规范
alwaysApply: false
---

# FuFramework C# 代码风格规范（写代码时逐条检查）

> 完整参考文档：`Docs/FuFramework代码风格规范.md`

## 写代码前检查清单

每写一个 class / method / field / enum 时，逐条确认：

### 字段
- [ ] 私有字段是否用了 `m_` 前缀？`m_FsmDict` ✓ | `_fsmDict` ✗ | `fsmDict` ✗
- [ ] 字段名是否 PascalCase？`m_OnConfirm` ✓ | `m_onConfirm` ✗
- [ ] 是否加了 `readonly`？（能加就加）

### 枚举
- [ ] 手写枚举是否用了 `E` 前缀？`EUILayer` ✓ | `UILayer` ✗
- [ ] 自动生成枚举不检查（Luban/Proto 生成）

### 方法
- [ ] 返回 UniTask/UniTaskVoid 的方法是否加了 `Async` 后缀？
- [ ] fire-and-forget 调用是否加了 `.Forget()`？
- [ ] 事件处理方法是否用 `On` + 事件名？`OnBtnLoginClick`
- [ ] 生命周期方法（override）是否命名正确？`OnInit`、`OnOpen`、`OnClose`、`OnDispose`

### 注释
- [ ] 是否全部使用中文？
- [ ] 每个 class、method、property、field、enum member 是否都有 `<summary>`？
- [ ] TODO 格式是否正确？`// TODO：xxx`

### 格式
- [ ] 缩进是否用 Tab？
- [ ] 左大括号是否与声明同行（K&R）？
- [ ] 访问修饰符是否显式声明（不依赖默认值）？
- [ ] 类是否加了 `sealed`？（能加就加）

### 代码组织
- [ ] 文件是否超过 500 行？（超过则拆分为 partial class）
- [ ] Region 名称是否用中文？
- [ ] ViewBase 子类是否按 InitUIComp → InitUIEvent → InitEvent → InitRedDot 结构？

### 日志与错误
- [ ] 日志是否用 `FuLogger` + 中文 + 模块名前缀？`FuLogger.LogInfo($"[ModuleName] 消息")`
- [ ] 异常是否用 `FuException` + 中文消息？
- [ ] 卫语句是否用早返回模式？

## 跳过检查的文件

以下文件不应用上述规则：
- `**/Config/Generate/**` — Luban 自动生成
- `**/Protobuf/**` 输出目录 — Proto 自动生成
- `**/*.Gen.cs` — FGUI 自动生成（匈牙利风格字段：btnLogin、txtUsername）
- `**/UnityWebSocket/**` — 第三方命名空间
