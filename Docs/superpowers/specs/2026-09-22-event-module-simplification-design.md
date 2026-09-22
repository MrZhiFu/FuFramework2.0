# 事件模块优化改造设计（EventPool 去模式去锁 + 网络事件封送主线程）

- 日期：2026-09-22
- 分支：`refactor/framework-modules-to-hotfix`
- 状态：已评审通过（分节确认），待用户终审 spec
- 提交纪律：**全程不做 git 提交**，实现完成后由用户验证确认，再按 `Docs/Git提交规范.md` 统一整理提交。

## 1. 背景与动机

1. `EEventPoolMode`（`Default` / `AllowNoHandler` / `AllowMultiHandler` / `AllowDuplicateHandler`）全仓只有一处实例化：`EventModule.cs:36` 传入 `AllowNoHandler | AllowMultiHandler`。`Default` 与 `AllowDuplicateHandler` 从未被使用——枚举是为不存在的需求付费，且迫使 `Subscribe` / `Unsubscribe` / `HandleEvent` 保留多处死分支。
2. EventPool 持有两把锁（`m_EventHandlerLock`、`m_EventQueue`）。处理器锁的竞争者实际不存在：全部 `Subscribe` / `Unsubscribe` 调用方（UI、Model、模块）与分发（`Update`）都在主线程。队列锁表面上有存在理由：**`SystemTcpNetworkChannel` 的 Socket 异步回调（线程池线程）会经 `NetworkModule` 直接 `Broadcast` 四类生命周期事件**——但这条跨线程路径本身是网络模块未封送的遗留，而非事件模块的设计目标。

## 2. 决策记录（已与用户逐项确认）

| # | 决策 | 内容 | 被否选项及原因 |
|---|---|---|---|
| D1 | 网络生命周期事件封送主线程 | Connected / Closed / Error 改走「入队 → 频道 `Update` 主线程排水」；随后 EventPool 全面去锁，契约改为仅主线程 | 保留跨线程 Broadcast：改动虽小但 Closed 插队到未派发数据包之前的竞态仍在；锁全保留：不解决任何实际问题 |
| D2 | 订阅语义硬编码为现状 | 允许多 handler + 允许无 handler + 同 `(id, handler)` 引用计数去重（多订阅者每事件只调一次） | 改为重复条目语义（同 handler 订阅 N 次调 N 次）：行为变化，需排查全部订阅方，收益仅为删掉刚修过 bug（`fec3d904`）且经验证的引用计数机制 |
| D3 | 工作组织 | 单分支两阶段：先网络封送、后 EventPool 精简，两个独立 commit；顺序不可反（先去锁会出现无保护的跨线程 Broadcast 窗口） | 统一队列（并入 MessageObject 泵）：动收包热路径，风险大于收益；拆两个分支串行：重构分支上拉长中间态，收益小 |
| D4 | `Update` 重入防护 | 删 `new List<Event>` 回退分支与 `ReferenceEquals` 条件复位；保留 `m_IsUpdatingEvents` 标志，`#if UNITY_ASSERTIONS` 下检测到嵌套打 Error（fail-fast）。已证实全仓无嵌套驱动 `Update` 的调用点（`ModuleManager.Update` 仅 `HotfixLauncher.cs:103` 一处挂接） | 全删（含标志）：未来若有人手动泵帧将静默丢事件 + 引用池计数漂移，排查困难 |

## 3. 阶段一：网络频道生命周期事件封送

### 3.1 现状事实

- 跨线程触发点（线程池线程，Socket 异步回调）：
  - `SystemTcpNetworkChannel.cs`：`:107` ReceiveError、`:133` DeserializePacketError、`:248` SocketError(Disconnecting)、`:324` ConnectError、`:344` ConnectError、`:355` Connected；
  - `WebSocketNetworkChannel.cs`：`:151`、`:194`、`:230`、`:239`、`:298`、`:303`；
  - `NetworkChannelBase.cs:740`：`Close()` 内的 Closed（`Close` 可从接收线程被调）。
- `MissHeartBeat` 由主线程 `Update` 的心跳检查触发（`NetworkChannelBase.cs:448`），不在封送范围。
- `NetworkChannelActiveChanged` 全仓零订阅（死事件），且同样可能从接收线程触发——顺带删除。
- 数据包热路径已有成熟封送模式：接收回调线程 `AddLast` 入 `m_ExecutionMessageLinkedList`（`PExecutionMessageLock` 互斥），主线程 `Update` → `ProcessReceivedMessage` 锁内逐条出队、锁外派发。

### 3.2 组件设计

`NetworkChannelBase` 新增（私有）：

```csharp
private enum EChannelLifecycleEventType { Connected, Closed, Error }

private sealed class LifecycleEvent
{
    public EChannelLifecycleEventType Type;
    public object           UserData;      // Connected 用
    public ENetworkErrorCode ErrorCode;    // Error 用
    public SocketError      SocketError;   // Error 用
    public string           ErrorMessage;  // Error 用
}

private readonly Queue<LifecycleEvent> m_LifecycleEventQueue = new();
```

- 生命周期事件频率极低（每连接至多 Connected/Closed 各一次、Error 罕见），直接 `new`，不池化。
- 队列互斥复用现有 `PExecutionMessageLock`（生产者=接收线程，消费者=主线程，与数据包队列同族）。
- `protected void EnqueueLifecycleEvent(...)` 供子类触发点调用；**「订阅者为 null 时 throw」的既有行为保留在触发点**（`if (NetworkChannelError == null) throw;` 原样，else 改为入队）。

`Update()` 首行插入排水，先于活跃检查——这是 Closed 必达的关键：

```csharp
public virtual void Update(float deltaTime, float unscaledDeltaTime)
{
    DrainLifecycleEvents();                     // 新增：必须在早退检查之前
    if (PSocket == null || !PActive) return;
    ... // 原有逻辑不动
}

private void DrainLifecycleEvents()
{
    while (true)
    {
        LifecycleEvent lifecycleEvent;
        lock (PExecutionMessageLock)            // 锁内仅出队
        {
            if (m_LifecycleEventQueue.Count == 0) break;
            lifecycleEvent = m_LifecycleEventQueue.Dequeue();
        }
        // 锁外触发（写法同 ProcessReceivedMessage 的出队-派发分离）；
        // 触发的就是现有公共 Action 字段 → NetworkModule 订阅零改动
        switch (lifecycleEvent.Type) { /* Connected / Closed / Error 分别 Invoke */ }
    }
}
```

### 3.3 触发点替换清单

- `SystemTcpNetworkChannel`：上述 6 处 `NetworkChannelXxx?.Invoke(...)` → `EnqueueLifecycleEvent(...)`；
- `WebSocketNetworkChannel`：上述 6 处同样替换；
- `NetworkChannelBase.Close()`：`NetworkChannelClosed?.Invoke(this)` → 入队；事件队列**不被 `Close` 清空**（Closed 刚入队）；数据包队列照旧清空（保留现有丢包语义）；
- 删除 `NetworkChannelActiveChanged` 事件、`PActive` setter 内触发、`Shutdown` 中的置空行。

### 3.4 线程与锁序说明

- `Close()` 持 `m_CloseLock` 期间入队需短暂获取 `PExecutionMessageLock`，锁序恒为 `m_CloseLock → PExecutionMessageLock`；排水侧（主线程）先释放 `PExecutionMessageLock` 再触发 handler，不形成反向嵌套，无死锁面。
- 排水期 handler 内同步 `Close()` 频道安全：出队与触发分离，`Close` 只会继续入队，不破坏排水循环。

### 3.5 生命周期规则

- 频道销毁（`DestroyNetworkChannel` / 模块 `Shutdown` 移除字典）后 `Update` 不再被调，未派发生命周期事件随频道对象一起回收，无泄漏；文档注明「销毁频道会丢弃未派发的生命周期事件」。
- 时序保证：订阅者不会在 Closed 之后看到该频道的任何数据包事件（数据包队列被 `Close` 清空 + Closed 入队排水的共同结果）。

## 4. 阶段二：EventPool 去模式、去锁

### 4.1 改动清单

| 项 | 动作 |
|---|---|
| `EEventPoolMode.cs` | 整文件删除 |
| 构造函数 | 改无参；`EventModule.cs:36` 同步改 `new EventPool<GameEventArgs>()` |
| `Subscribe` | 删「不允许多次注册」throw 分支、`AllowDuplicateHandler` 分支；引用计数逻辑原样保留 |
| `Unsubscribe` | 删「未计数订阅」分支（硬编码后条目必有计数；该分支现行为=退订从未订阅的 handler 的无害 no-op，删除后直接 no-op，可观测行为不变） |
| `HandleEvent` | 删 `noHandlerException` 整条路径（无 handler 且无默认 handler → 静默丢弃，即现 `AllowNoHandler` 行为）；默认 handler 逻辑保留 |
| 锁 | 删 `m_EventHandlerLock` 字段及全部 `lock (m_EventHandlerLock)` / `lock (m_EventQueue)` 块 |
| 主线程断言 | 构造时记录当前线程 ID；私有 `AssertMainThread()`（`#if UNITY_ASSERTIONS` 包裹，发布版零开销）加在全部公共入口 |
| `Update` 重入 | 删 `new List<Event>` 分支与 `ReferenceEquals` 条件复位；保留 `m_IsUpdatingEvents` + `#if UNITY_ASSERTIONS` 嵌套 Error（见 D4） |
| 其余重入回退 | `HandleEvent` / `ForEachHandler` / `ForEachEvent` 的回退分支**原样保留**（`BroadcastNow` 事件链是真实合法的嵌套来源） |
| 文档 | `Event/README.md`（删模式章节、写仅主线程契约）、`Network/README.md`（线程模型）同步 |

### 4.2 `AssertMainThread` 形态

```csharp
#if UNITY_ASSERTIONS
private readonly int m_CreatorThreadId;   // 构造函数中：Thread.CurrentThread.ManagedThreadId
private void AssertMainThread()
{
    if (Thread.CurrentThread.ManagedThreadId != m_CreatorThreadId)
        FuLogger.LogError("[EventPool]仅允许主线程调用，检测到跨线程访问。");
}
#endif
```

加在全部公共入口：`Subscribe` / `Unsubscribe` / `SetDefaultHandler` / `Broadcast` / `BroadcastNow` / `Update` / `Shutdown` / `Clear` / `Check` / `Count` / `EventHandlerCount` / `ForEachHandler` / `ForEachEvent`。

### 4.3 不变量（去锁后仍然成立的部分）

- 快照/重入机制、引用计数 + 待删除列表、对象池、逐 handler try/catch、`Update` 批次搬运与兜底回收、`Clear` 排水——全部与线程模型无关，逻辑不动；
- 稳态零分配特征不变（批次/快照复用、节点与参数池化）；
- `ReferencePool` 自带锁、跨模块共享，**不动**（接收线程仍会为数据包走引用池）。

## 5. 错误处理与边界情况

1. handler 异常：逐 handler try/catch 原样保留（单个异常不中断本次分发、不上抛）；
2. 回收异常：`Update` 兜底、`Clear` 排水、`HandleEvent` finally 的逐项隔离 try/catch 原样保留；
3. 去锁后的并发误用：开发期被 `AssertMainThread` 拦截；发布版无断言，但现状本就无真实并发，行为等价。未来若真出现跨线程广播需求，升级路径明确：只给事件队列单点加回锁；
4. 网络触发点「订阅者为 null 时 throw」：频道创建时 `NetworkModule` 必订阅，正常生命周期不命中，命中即配置错误，尽早暴露；
5. 同一帧多次 Error：多条排队、逐条派发，与现状多次 `Invoke` 一致。

## 6. 明确不做（Out of Scope）

- 收包热路径（`MessageObject` 泵）重构；
- `ReferencePool` 的锁与线程模型；
- `EventPool<T>` 泛型收窄为非泛型；
- `HandleEvent` / `ForEachHandler` / `ForEachEvent` 的重入回退分支；
- 引入单测基建；
- 其它模块（ObjectPool、UI 等）自身的锁。

## 7. 验证方式（项目无单测基建）

1. 编译验证：unity-cli 触发编译，Hotfix 与 Editor 程序集零错误；
2. 残留检查：全仓（含 `Assets/Editor`）grep `EEventPoolMode` / `AllowDuplicateHandler` / `AllowMultiHandler` / `AllowNoHandler` / `m_EventHandlerLock` 零命中；
3. Play 模式手工验证清单：
   - 登录 → 主界面流程正常（UI / RedDot / Sound 等订阅方收得到事件）；
   - 网络连接 → 断线：Connected / Closed 正常到达，且触发线程为主线程（临时插桩打线程 ID，验完还原）；
   - 同 handler 多订阅者：退订一份不影响他人（引用计数行为不变）；
   - `EventModuleInspector` 正常显示；
4. 可选插桩实测：Profiler GC Alloc 列确认 `Update` 稳态零分配与现状一致。

## 8. 涉及文件汇总

| 文件 | 改动 |
|---|---|
| `NetworkChannelBase.cs` | 新增 LifecycleEvent 队列与排水；`Close` 改入队；删 `ActiveChanged` |
| `SystemTcpNetworkChannel.cs` | 6 处触发点改入队 |
| `WebSocketNetworkChannel.cs` | 6 处触发点改入队 |
| `EventPool.cs` | 去模式、去锁、删重入回退、加断言 |
| `EEventPoolMode.cs` | 删除 |
| `EventModule.cs` | 构造调用一处 |
| `Event/README.md`、`Network/README.md` | 同步文档 |
| `NetworkModule.cs` / `NetworkModule.API.cs` | 零改动（验证确认） |
| `EventRegister.cs` 及全部订阅方 | 零改动 |
