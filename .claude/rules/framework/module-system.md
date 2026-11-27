---
paths:
  - "Unity/Assets/Scripts/**/Framework/**"
---

# 模块系统与常用机制

模块边界与注册顺序另见 `architecture/module-boundary.md`。

## ModuleBase 生命周期

`ModuleBase`（`Hotfix/Framework/Core/`）是纯 C# 抽象类，由 `ModuleManager` 驱动：

```csharp
protected internal virtual void OnInit() { }
protected internal virtual void OnUpdate(float deltaTime, float unscaledDeltaTime) { }
protected internal virtual void OnLateUpdate(float deltaTime, float unscaledDeltaTime) { }
protected internal virtual void OnFixedUpdate() { }
protected internal virtual void OnSecondUpdate() { }
protected internal virtual void OnDispose() { }
```

帧驱动由 Hotfix 侧 `GameDriven`（MonoSingleton）持有委托完成，模块不自己起 MonoBehaviour。

## 分部命名

`XxxModule.cs`（主）/ `.Const.cs`（常量收敛）/ `.API.cs`（运行时 API）/ `.Debug.cs`（Editor 调试，整文件 `#if UNITY_EDITOR`）/ 功能分部（如 `.Reconnect.cs`、`.State.cs`）。

**模块常量只放 `.Const.cs`**，不散落主文件——这是已定案的模块整理规范。

**常量用时直取，不得经字段/属性/方法参数中转。** 形如 `常量 → 属性初始化 → 方法参数 → 消费点` 的链条一律拆掉：消费点直接引用 `XxxModule.CONST`。带默认值的 API 参数若全项目无定制调用方，删参数而不是保留假想灵活性。
- 例外：参数是**数据组织维度**（如文件名）、或运行时调度语义（如任务优先级）时，参数本身保留，只删冗余重载。
- 常量可见性随消费点调整：跨类消费升 `internal`。

## Timer 模块

- 统一帧驱动已落地（同一帧批量推进），不要新增独立时间源。
- `TimerRegister` 契约：**批量所有权为底**（`Release → Recycle → Clear → StopAllTimers` 全量回收 + 退订），+ **`StartXxx` 返回 `int` id（失败 -1）** 提供按 id 操作出口（`PauseTimer(id)`/`StopTimer(id)`/`ResetTimer(id)`/`IsTimerExist(id)`/`IsTimerPaused(id)`）。
- 调用方忽略返回值时行为与旧 void 版一致。
- **改动 `StartXxx` 签名必须同步四层**：`TimerRegister` / `WinBase` / 生成模板 / 生成的 `*.Gen.cs`。
- 失败（-1）的计时器不加入清理列表，不影响批量回收。

## 对象池

`ObjectPoolModule` + `ReferencePoolModule`。高频短命对象走池，归还时**必须复位字段**（`MergeFrom`/`Clear` 陷阱：不复位会把上次状态带给下一个使用者）。

## 优化决策原则

- **优化前先查真实调用方**。为零调用方预留的机制（如堆缓存）会被裁定为过度设计。
- 可读性优先；机制等真实热点出现再加。
