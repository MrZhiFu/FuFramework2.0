---
paths:
  - "**/*.cs"
---

# C# 编码规范

本文件是 C# 编码规范的**单一事实源**（原 `Docs/代码风格规范.md` 已并入并删除）。写任何手写 `.cs` 前必须遵守。

## 五条铁律（最高优先级）

1. **禁止原生 `Task`**——一律 `UniTask`。
2. **禁止 Unity 协程 `Coroutine`**——一律 `UniTask`。
3. **禁止 LINQ**——手写循环 + 缓存（`foreach`/`for` 替代 `Where/Select/Any`；频繁查用 `Dictionary` 缓存）。
4. **运行时禁止反射**——Editor 程序集除外。
5. **异步链必须有生命周期所有者**——顶层对象持 `CancellationTokenIssuer` 并在生命周期结束 `Cancel`；helper 透传调用方的 `CancellationToken` 不自持；网络/资源类异步 API 的 `CancellationToken` 必须传且无默认值。详见 `framework/uni-task-async.md`。

违反以上任一条即为阻塞性缺陷，不接受"以后再改"。

## 命名

| 种类 | 规则 | 示例 |
|---|---|---|
| 类 / 方法 / 属性 | PascalCase | `FsmModule`、`LoadConfigAsync` |
| 模块类 | `XxxModule` 后缀 | `EventModule`、`RedDotModule` |
| Manager / Binder / Handler / Impl / 扩展 | `XxxManager`/`XxxBinder`/`XxxHandler`/`XxxImpl`/`XxxEx` | `BagManager`、`StringEx` |
| 接口 | `I` 前缀 | `IReference`、`INetworkChannel` |
| 枚举 | `E` 前缀 | `ELanguage`、`EEntityStatus` |
| 私有实例字段（含 readonly、Unity 序列化） | `m_` + camelCase | `m_fsmDict` |
| 非私有实例字段 | `m_` + PascalCase | `m_TableManager` |
| 私有静态字段 | `s_` + camelCase | `s_instance` |
| 非私有静态字段 | `s_` + PascalCase | `s_Config` |
| 私有静态只读 | `sr_` + camelCase | `sr_gameVersion` |
| 非私有静态只读 | `sr_` + PascalCase | `sr_AssetBundle` |
| 常量（任意可见性） | 无前缀 + ALL_CAPS | `MAX_COUNT`、`HOTFIX_DLL_NAME` |
| 局部变量 | camelCase + `var` | `var winLauncher` |
| 参数 | camelCase | `itemId` |

- **异步方法必须加 `Async` 后缀**（`UniTask` 与 `UniTaskVoid` 都要）。
- 事件处理 `On` + 事件名；渲染回调 `OnRenderList` + 控件名；生命周期 `OnInit/OnOpen/OnClose/OnDispose`；分部初始化 `InitUIComp → InitUIEvent → InitEvent → InitRedDot`。
- 存量代码为旧风格（`m_` + PascalCase），**修改存量文件时局部跟随现状**避免无关 diff；新文件/新字段按上表。

## 注释

- **全部中文**，无一例外。
- XML 文档注释 `///` 为主导，`<summary>` 必须换行，类/方法/属性/字段/枚举成员全要写（含私有）。
- 详见 `coding/comment-conventions.md`。

## 格式（含用户拍板的隐性约定）

- **缩进：Tab**，全项目统一。
- **括号：K&R**——左大括号与声明同行，右大括号独占一行；空方法体同行 `{ }`；极简卫语句可省大括号，稍复杂必加。
- **文件末尾不保留换行符**（`insert_final_newline = false`）。工作区出现"末尾无换行"的 diff 是约定形态，**不是损坏，禁止补换行**。
- **列对齐是约定**：`.editorconfig` 的 `resharper_int_align_*` 全开，相邻赋值会被格式化器对齐 `=` 并补值侧空格。**禁止判定为格式事故并改回单空格**。
- 行尾符 CRLF。BOM 在仓库内混用（Unity 与 Server 目录都有少部分文件带 BOM）——新文件跟随所在目录现状，**不要批量统一**。
- 访问修饰符**始终显式声明**；类尽量 `sealed`。
- 属性优先，简单只读用 `=>`；`readonly` 能加就加；用 `new()` 目标类型简化语法。
- `var` 只用于局部变量；字段/属性/参数/返回值一律显式类型。

## 代码组织

- **单文件不超过 500 行**，超过拆 partial。分部后缀语义固定：`Xxx.API.cs`（运行时 API）、`Xxx.Debug.cs`（整文件 `#if UNITY_EDITOR`）。
- `#region` 用于大类，名称用中文；小型数据类与 Proto 类不用。
- 方法 1-10 行、单一职责；复杂业务方法不超过 50 行。

## 错误处理与日志

- 卫语句早返回。
- 参数校验用扩展方法 `FuGuardEx`（`value.NotNull(nameof(value))`、`value.NotNullOrEmpty(nameof(name))`）。
- 异常用 BCL 内置类型（`InvalidOperationException` / `ArgumentNullException` / `OperationCanceledException`），中文消息带模块名前缀：`throw new InvalidOperationException($"[FsmModule] ...")`。
- **禁止 `Debug.Log` 等裸日志**，一律 `FuLogger.LogInfo/LogError/LogWarning/LogFatal`，字符串插值 + 模块名前缀（`FuLogger` 位于 AOT/Framework/Log）。

## 异步写法

```csharp
// fire-and-forget 必须 .Forget()
LoginAsync().Forget();

private async UniTaskVoid LoginAsync() { ... }
```

## 豁免（不检查风格）

Luban 生成（`Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/`）、Proto 生成（`Hotfix/Game/AutoGen/Proto/`）、FGUI 生成（`*.Gen.cs`，匈牙利字段如 `btnLogin`）、第三方 `UnityWebSocket`。

## 由 fu-code-review 执行的三项机械检查

以下三项原由 `.claude/hooks/check-style.ps1` 自动检查，现移交评审流程：

1. 私有字段必须是 `m_` 前缀（不是 `_` 前缀）。
2. `UniTask`/`UniTaskVoid` 方法必须有 `Async` 后缀。
3. 手写枚举必须有 `E` 前缀（生成代码豁免）。
