---
paths:
  - "Unity/Assets/Scripts/**"
---

# 模块边界

## 模块系统

每个框架功能是一个 `ModuleBase` 子类（纯 C# 抽象类，不依赖 MonoBehaviour），由 `ModuleManager` 管理。生命周期：

```
OnInit → OnUpdate / OnLateUpdate / OnFixedUpdate / OnSecondUpdate → OnDispose
```

对应方法：`protected internal virtual void OnInit()`、`OnUpdate(float deltaTime, float unscaledDeltaTime)`、`OnLateUpdate(...)`、`OnFixedUpdate()`、`OnSecondUpdate()`、`OnDispose()`。

## 注册位置与顺序

全部注册在 `Unity/Assets/Scripts/Hotfix/HotfixLauncher.cs`，分两组：

- `RegisterBaseModules()`——基础模块（Config/Fsm/Procedure/Event/ObjectPool/Mono/Timer/Asset/UI/Storage/Localization）
- `RegisterFeatureModules()`——功能模块（RedDot/Guide/Model/Scene/Sound/Entity/Download/Web/Network）

**注册顺序不可随意改**：基础模块必须先于依赖它们的功能模块。**依赖只靠注册顺序保证**——新增模块必须评估落在 `RegisterBaseModules` 还是 `RegisterFeatureModules`，以及它与相邻模块的先后关系。

## 访问方式

通过 `GlobalModule.<模块名>` 访问（内部延迟初始化单例）。**禁止**在模块外 new 模块实例或调用其构造函数。

## 分部文件约定

模块的主类是 `sealed partial class`，按职责拆分部：

| 分部后缀 | 内容 |
|---|---|
| `XxxModule.cs` | 主逻辑 |
| `XxxModule.Const.cs` | 常量与静态只读字段收敛（模块常量只放这里，不散落主文件） |
| `XxxModule.API.cs` | 运行时观察者/便捷 API |
| `XxxModule.Debug.cs` | Editor 调试（整文件 `#if UNITY_EDITOR` 包裹） |
| 其他按功能 | 如 `NetworkModule.Reconnect.cs`、`NetworkModule.State.cs` |

单文件超 500 行必须拆。

## 依赖方向

```
AOT(Framework 工具) ← Hotfix/Framework/模块 ← Hotfix/Game 业务
```

- 单向：上层引用下层，**禁止反向**。
- 模块之间不直接引用对方的私有实现；需要跨模块能力时通过 `GlobalModule.Xxx` 的公开 API。
- `Core/`（ModuleBase/ModuleManager/GameDriven）是基座，业务模块可引用它，它不得引用任何业务模块。

## 生命周期与异步

- 模块内发起的异步链必须在 `OnDispose()` 中取消（持有 `CancellationTokenIssuer` 并 `Cancel()`）。
- 模块注册的帧回调经 `GameDriven` 驱动（`m_OnUpdate` 等委托），不要自己起 MonoBehaviour 或 `Update`。
