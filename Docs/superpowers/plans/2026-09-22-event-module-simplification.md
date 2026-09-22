# 事件模块优化改造实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 网络频道生命周期事件封送主线程后，EventPool 删除 `EEventPoolMode` 与全部锁，契约收敛为仅主线程。

**Architecture:** 两阶段顺序执行（顺序不可反）：阶段一给 `NetworkChannelBase` 加生命周期事件队列（接收线程入队、主线程 `Update` 首行排水），三个跨线程事件触发点全部改入队；阶段二重写 `EventPool`——删模式枚举与死分支、删两把锁、加 `#if UNITY_ASSERTIONS` 主线程断言、`Update` 重入回退改为断言报错。`NetworkModule` 与全部订阅方零改动。

**Tech Stack:** C# / Unity Hotfix 程序集；验证靠 unity-cli 编译 + grep 残留检查（项目无单测基建）。

**Spec:** `Docs/superpowers/specs/2026-09-22-event-module-simplification-design.md`（决策 D1~D4、锁序说明、Out of Scope 均以 spec 为准，本计划不重复论证）

## Global Constraints

- **全程不做 git 提交**。所有改动留在工作区，用户验证确认后按 `Docs/Git提交规范.md` 统一整理提交。
- 代码铁律（CLAUDE.md）：禁止原生 `Task` / `Coroutine` / `LINQ` / 运行时反射；本改造不引入新异步链。
- 注释与交流全部中文；注释密度与风格对齐被改文件既有代码（长注释是本仓库风格，删注释必须删得有依据）。
- 每个 Task 结束必须编译零错误才能进入下一个 Task。
- 编译验证方式（每个验证步骤相同，下文简称「编译验证」）：
  ```
  unity-cli system ping        # 确认 Editor 连通（预期返回 pong）
  unity-cli tool list          # 查看可用工具，若含编译类工具则调用
  # CLI 不可用时：请用户在 Unity Editor 中触发脚本刷新编译，回报 Console 结果
  # 验收标准：Console 零编译错误（Error），Warning 不阻塞
  ```
- 涉及删除 `.cs` 文件时必须连同名 `.meta` 一起删除。
- 不得改动 spec 第 6 节 Out of Scope 清单中的内容（收包热路径、ReferencePool、`HandleEvent`/`ForEachHandler`/`ForEachEvent` 重入回退等）。

---

### Task 1: NetworkChannelBase 生命周期事件封送基建

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Network/Channel/NetworkChannelBase.cs`（字段区 ~100-158 行、`Update` ~323 行、`PActive` setter ~105-114 行、`Close` ~740 行）

**Interfaces:**
- Produces: `protected void EnqueueLifecycleEvent(EChannelLifecycleEventType type, object userData = null, ENetworkErrorCode errorCode = ENetworkErrorCode.SocketError, SocketError socketError = SocketError.Success, string errorMessage = null)`；私有嵌套类型 `EChannelLifecycleEventType`（`Connected` / `Closed` / `Error`）。Task 2/3 的触发点替换依赖此签名。

- [ ] **Step 1: 新增嵌套类型、队列字段与出入队方法**

在 `m_HandlerBufferBusy` 字段（约 152 行）之后、`NetworkChannelConnected` 委托声明（约 154 行）之前插入：

```csharp
        /// <summary>
        /// 频道生命周期事件类型。
        /// 仅含跨线程触发的三类；MissHeartBeat 由主线程 Update 的心跳检查触发，无需封送。
        /// </summary>
        private enum EChannelLifecycleEventType
        {
            Connected,
            Closed,
            Error
        }

        /// <summary>
        /// 待主线程派发的生命周期事件记录。
        /// 生命周期事件频率极低（每连接至多 Connected/Closed 各一次、Error 罕见），直接 new，不入池。
        /// </summary>
        private sealed class LifecycleEvent
        {
            public EChannelLifecycleEventType Type;
            public object            UserData;
            public ENetworkErrorCode ErrorCode;
            public SocketError       SocketError;
            public string            ErrorMessage;
        }

        /// <summary>
        /// 生命周期事件队列：Socket 回调线程入队，主线程 Update 排水后触发。
        /// 互斥复用 PExecutionMessageLock（与数据包接收队列同族：接收线程生产、主线程消费）。
        /// 排水位置在 Update 活跃检查之前——保证 Close() 后入队的 Closed 事件必达。
        /// </summary>
        private readonly Queue<LifecycleEvent> m_LifecycleEventQueue = new();

        /// <summary>
        /// 将生命周期事件入队，由主线程 Update 排水时触发（封送，替代跨线程直接 Invoke）。
        /// 调用方保留「订阅者为 null 时抛异常」的既有行为，本方法不做判空。
        /// </summary>
        protected void EnqueueLifecycleEvent(EChannelLifecycleEventType type, object userData = null,
                                             ENetworkErrorCode errorCode = ENetworkErrorCode.SocketError,
                                             SocketError socketError = SocketError.Success,
                                             string errorMessage = null)
        {
            var lifecycleEvent = new LifecycleEvent
            {
                Type         = type,
                UserData     = userData,
                ErrorCode    = errorCode,
                SocketError  = socketError,
                ErrorMessage = errorMessage,
            };

            lock (PExecutionMessageLock)
            {
                m_LifecycleEventQueue.Enqueue(lifecycleEvent);
            }
        }

        /// <summary>
        /// 排水生命周期事件队列：锁内逐条出队、锁外触发（写法同 ProcessReceivedMessage 的出队-派发分离，
        /// handler 内同步 Close 频道只会继续入队，不会破坏本循环）。
        /// 触发的是现有公共委托字段，NetworkModule 等订阅方零改动。
        /// </summary>
        private void DrainLifecycleEvents()
        {
            while (true)
            {
                LifecycleEvent lifecycleEvent;
                lock (PExecutionMessageLock)
                {
                    if (m_LifecycleEventQueue.Count == 0) break;
                    lifecycleEvent = m_LifecycleEventQueue.Dequeue();
                }

                switch (lifecycleEvent.Type)
                {
                    case EChannelLifecycleEventType.Connected:
                        NetworkChannelConnected?.Invoke(this, lifecycleEvent.UserData);
                        break;
                    case EChannelLifecycleEventType.Closed:
                        NetworkChannelClosed?.Invoke(this);
                        break;
                    case EChannelLifecycleEventType.Error:
                        NetworkChannelError?.Invoke(this, lifecycleEvent.ErrorCode, lifecycleEvent.SocketError, lifecycleEvent.ErrorMessage);
                        break;
                }
            }
        }
```

- [ ] **Step 2: Update 首行插入排水（先于活跃检查，Closed 必达的关键）**

```csharp
        public virtual void Update(float deltaTime, float unscaledDeltaTime)
        {
            // 先排水生命周期事件，再做活跃检查：频道 Close 后 PActive=false，若排水在检查之后，
            // 已入队的 Closed 事件将永远滞留队列（Update 不再被有效执行）。
            DrainLifecycleEvents();

            if (PSocket == null || !PActive) return;
            // ……以下原有逻辑不动
```

- [ ] **Step 3: Close() 内 Closed 改入队**

`Close()` 中（约 740 行）：

```csharp
                finally
                {
                    PSocket.Close();
                    PSocket = null;
                    // 原为 NetworkChannelClosed?.Invoke(this)（可能运行在 Socket 回调线程）——改为入队封送。
                    // PSocket==null 的早退保证重复 Close 不会重复入队；事件队列不被 Close 清空。
                    EnqueueLifecycleEvent(EChannelLifecycleEventType.Closed);
                }
```

- [ ] **Step 4: 删除 NetworkChannelActiveChanged（全仓零订阅的死事件）**

1. 删除字段声明（约 156 行）：`public Action<NetworkChannelBase, bool> NetworkChannelActiveChanged;`
2. `PActive` setter（约 105-114 行）删去触发行，保留卫语句：

```csharp
        protected bool PActive
        {
            get => m_PActive;
            set
            {
                if (m_PActive == value) return;
                m_PActive = value;
            }
        }
```

- [ ] **Step 5: 编译验证**

预期：零错误。

---

### Task 2: SystemTcpNetworkChannel 触发点改入队（6 处）

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Network/Channel/SystemTcpNetworkChannel.cs`（约 102-108、127-134、243-250、318-326、341-347、355 行）

**Interfaces:**
- Consumes: Task 1 的 `EnqueueLifecycleEvent` 与 `EChannelLifecycleEventType`。

- [ ] **Step 1: ReceiveCallback 的 EndReceive 异常（约 102-108 行）**

```csharp
            catch (Exception exception)
            {
                PActive = false;
                if (NetworkChannelError == null) throw;
                var socketException = exception as SocketException;
                EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
                    errorCode: ENetworkErrorCode.ReceiveError,
                    socketError: socketException?.SocketErrorCode ?? SocketError.Success,
                    errorMessage: exception.ToString());
                return;
            }
```

- [ ] **Step 2: ProcessReceivedBytes 解析异常（约 127-134 行）**

```csharp
            catch (Exception exception)
            {
                // 回包处理（畸形包头、未注册 messageId 等）抛出的异常绝不能在**线程池线程**上逃逸：
                // 原实现只包住了 EndReceive，解析阶段的异常会直接抛出，且抛出前没有续接 ReceiveAsync，
                // 连接会静默卡死。这里统一转为 NetworkChannelError 事件（封送主线程触发）。
                var socketException = exception as SocketException;
                EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
                    errorCode: ENetworkErrorCode.DeserializePacketError,
                    socketError: socketException?.SocketErrorCode ?? SocketError.Success,
                    errorMessage: exception.ToString());
            }
```

- [ ] **Step 3: ProcessSendMessage 频道关闭中（约 243-250 行）**

```csharp
            if (PActive == false)
            {
                PActive = false;
                EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
                    errorCode: ENetworkErrorCode.SocketError,
                    socketError: SocketError.Disconnecting,
                    errorMessage: "Network channel is closing.");
                return false;
            }
```

- [ ] **Step 4: 连接流程异常（约 318-326 行，保留 throw-if-null）**

```csharp
            catch (Exception exception)
            {
                if (NetworkChannelError == null) throw;
                var socketException = exception as SocketException;
                EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
                    errorCode: ENetworkErrorCode.ConnectError,
                    socketError: socketException?.SocketErrorCode ?? SocketError.Success,
                    errorMessage: exception.ToString());
            }
```

- [ ] **Step 5: ConnectCallback 异常（约 341-347 行）**

```csharp
            catch (Exception exception)
            {
                var socketException = exception as SocketException;
                EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
                    errorCode: ENetworkErrorCode.ConnectError,
                    socketError: socketException?.SocketErrorCode ?? SocketError.Success,
                    errorMessage: exception.ToString());
                Close();
                return;
            }
```

- [ ] **Step 6: ConnectCallback 连接成功（约 355 行）**

```csharp
            EnqueueLifecycleEvent(EChannelLifecycleEventType.Connected, m_ConnectState.UserData);
            PActive = true;
            ReceiveAsync();
```

- [ ] **Step 7: 编译验证**

预期：零错误。

---

### Task 3: WebSocketNetworkChannel 触发点改入队（8 处）

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Network/Channel/WebSocketNetworkChannel.cs`（约 145-154、190-209、212-241、279-304 行）

**Interfaces:**
- Consumes: Task 1 的 `EnqueueLifecycleEvent` 与 `EChannelLifecycleEventType`。

- [ ] **Step 1: ProcessSendMessage 频道关闭中（约 147-153 行）**

```csharp
            if (IsClose())
            {
                PActive = false;
                EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
                    errorCode: ENetworkErrorCode.SocketError,
                    socketError: SocketError.Disconnecting,
                    errorMessage: "Network channel is closing.");
                return false;
            }
```

- [ ] **Step 2: ConnectAsync 取消/超时（约 190-196 行）**

```csharp
            catch (OperationCanceledException)
            {
                PIsConnecting = false;
                PActive       = false;
                EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
                    errorCode: ENetworkErrorCode.ConnectError,
                    socketError: SocketError.TimedOut,
                    errorMessage: $"WebSocket connect canceled or timeout after {ConnectTimeoutMilliseconds}ms.");
            }
```

- [ ] **Step 3: ConnectAsync 异常（约 197-209 行，保留 UniTaskVoid 不抛出兜底）**

```csharp
            catch (Exception exception)
            {
                PIsConnecting = false;
                var socketException = exception as SocketException;
                if (NetworkChannelError == null)
                {
                    // UniTaskVoid 中禁止抛出，否则异常无人接管。
                    FuLogger.LogError(exception.ToString());
                    return;
                }

                EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
                    errorCode: ENetworkErrorCode.ConnectError,
                    socketError: socketException?.SocketErrorCode ?? SocketError.Success,
                    errorMessage: exception.ToString());
            }
```

- [ ] **Step 4: ConnectCallback 异常（约 225-232 行，保留 throw-if-null）**

```csharp
            catch (Exception exception)
            {
                PActive = false;
                if (NetworkChannelError == null) throw;
                var socketException = exception as SocketException;
                EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
                    errorCode: ENetworkErrorCode.ConnectError,
                    socketError: socketException?.SocketErrorCode ?? SocketError.Success,
                    errorMessage: exception.ToString());
                return;
            }
```

- [ ] **Step 5: ConnectCallback 连接成功（约 239 行）**

```csharp
            EnqueueLifecycleEvent(EChannelLifecycleEventType.Connected, connectState.UserData);
            PActive = true;
```

- [ ] **Step 6: 包体无效且存在订阅者（约 279-286 行，保留原有控制流）**

```csharp
                    if (!processSuccess)
                    {
                        if (NetworkChannelError != null)
                        {
                            EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
                                errorCode: ENetworkErrorCode.DeserializePacketError,
                                errorMessage: "Packet body is invalid.");
                            return;
                        }
                    }
```

- [ ] **Step 7: 包头无效（约 298 行）**

```csharp
                    EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
                        errorCode: ENetworkErrorCode.DeserializePacketHeaderError,
                        errorMessage: "Packet header is invalid.");
```

- [ ] **Step 8: 包体解析异常（约 303 行）**

```csharp
                EnqueueLifecycleEvent(EChannelLifecycleEventType.Error,
                    errorCode: ENetworkErrorCode.DeserializePacketError,
                    errorMessage: "Packet body is invalid." + e.Message + "\n" + e.StackTrace);
```

- [ ] **Step 9: 编译验证**

预期：零错误。

---

### Task 4: 阶段一验证

- [ ] **Step 1: 残留检查（跨线程直接触发应清零）**

```
grep -rn "NetworkChannelActiveChanged" Unity/Assets/          # 预期：零命中
grep -rnE "NetworkChannel(Connected|Closed|Error)\??\.(Invoke)?\(" Unity/Assets/Scripts/Hotfix/Framework/Network/ --include="*.cs"
# 预期：仅剩 NetworkChannelBase.DrainLifecycleEvents 中的三处触发 + 声明行
```

- [ ] **Step 2: 编译验证**

预期：零错误。此步完成后阶段一收尾（阶段二的 commit 边界在此，最终由用户验证后统一整理提交）。

---

### Task 5: EventPool 重写（去模式 + 去锁 + 断言 + 重入调整）

**Files:**
- Delete: `Unity/Assets/Scripts/Hotfix/Framework/Event/EventPool/EEventPoolMode.cs` 及其 `.meta`
- Rewrite: `Unity/Assets/Scripts/Hotfix/Framework/Event/EventPool/EventPool.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Event/EventModule.cs:36`

**Interfaces:**
- Consumes: 无（`EventPool.Event.cs` 分部类不动）。
- Produces: `public EventPool()` 无参构造（唯一调用方 `EventModule.cs:36` 同步改）；全部公共方法契约「仅主线程」。

- [ ] **Step 1: 用以下完整内容重写 `EventPool.cs`**

（保留 `EventPool.Event.cs` 分部类不动；所有仍成立的正确性注释保留，仅删除锁序/线程安全相关论述）

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using AOT.Framework.Core.Log;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Event
{
    /// <summary>
    /// 事件池。
    /// 功能：
    ///     1. 用于管理池中的事件，提供一些便捷的方法。
    ///     2. 支持事件的订阅和取消订阅。
    ///     3. 支持事件的抛出和处理。
    ///     4. 支持事件的批量处理。
    /// 线程契约：仅允许主线程访问（跨线程产生的事件由网络频道封送到主线程后再 Broadcast）。
    /// </summary>
    /// <typeparam name="T">该事件池中的事件类型。</typeparam>
    public sealed partial class EventPool<T> where T : BaseEventArgs
    {
        /// <summary>
        /// 事件默认处理器
        /// </summary>
        private EventHandler<T> m_DefaultHandler;

        /// <summary>
        /// 事件队列
        /// </summary>
        private readonly Queue<Event> m_EventQueue;

        /// <summary>
        /// 帧分发批次缓存（复用同一列表，避免每次 Update 分配新列表产生 GC）
        /// </summary>
        private readonly List<Event> m_CachedEventBatch = new();

        /// <summary>
        /// 事件处理器多值字典，key为事件Id，value为事件处理函数列表
        /// </summary>
        private readonly FuMultiDictionary<string, EventHandler<T>> m_EventHandlerMultiDict;

        /// <summary>
        /// 待删除的事件处理器列表（延迟移除方案，确保事件处理时使用的是最新的处理函数handler列表）。
        /// 同一 (id, handler) 只登记一条（引用计数归零才登记、期间重新订阅即撤销登记），
        /// 否则「退订 → 重订阅」交错时，重复的登记会把后来者的订阅一并移除。
        /// </summary>
        private readonly List<(string id, EventHandler<T> handler)> m_WaitRemoveHandlerList;

        /// <summary>
        /// (id, handler) 条目的订阅引用计数：多个订阅者（如多个 EventRegister、多个模块）共享同一处理函数时，
        /// 各自计一份，退订只递减自己那一份，归零才真正移除条目；分发时只调用一次。
        /// 不变式：计数 &gt; 0 ⇔ 条目在 m_EventHandlerMultiDict 中；计数 == 0 ⇔ 条目仍在字典中但已登记待移除。
        /// </summary>
        private readonly Dictionary<(string id, EventHandler<T> handler), int> m_HandlerRefCountDict;

        /// <summary>
        /// 分发 handler 前的快照缓存（复用同一列表，避免每次分发分配新列表产生 GC）。
        /// 快照用于隔离「分发过程中新订阅的 handler」，仅非重入分发使用。
        /// </summary>
        private readonly List<EventHandler<T>> m_CachedHandlerSnapshot = new();

        /// <summary>
        /// 是否正在分发 handler（用于识别重入的嵌套分发，避免嵌套时覆盖外层快照）
        /// </summary>
        private bool m_IsHandlingEvent;

        /// <summary>
        /// 是否正在执行帧批次分发（用于检测嵌套 Update——嵌套时内层 batch.Clear() 会清掉
        /// 外层正在遍历的批次，契约上禁止嵌套驱动；断言期检测到即报错定位）。
        /// </summary>
        private bool m_IsUpdatingEvents;

        /// <summary>
        /// ForEachHandler 的 (id, handler) 快照缓存（复用同一列表，避免每次遍历分配新列表产生 GC）。
        /// 仅非重入遍历使用（重入识别见 m_IsForEachHandler）。
        /// </summary>
        private readonly List<(string id, EventHandler<T> handler)> m_CachedHandlerPairSnapshot = new();

        /// <summary>
        /// 是否正在遍历事件处理函数（用于识别重入的嵌套遍历，避免嵌套时清空外层正在遍历的快照列表）
        /// </summary>
        private bool m_IsForEachHandler;

        /// <summary>
        /// ForEachEvent 的 (sender, eventArgs) 快照缓存（复用同一列表，避免每次遍历分配新列表产生 GC）。
        /// 仅非重入遍历使用（重入识别见 m_IsForEachEvent）。
        /// </summary>
        private readonly List<(object sender, T eventArgs)> m_CachedEventArgsSnapshot = new();

        /// <summary>
        /// 是否正在遍历事件（用于识别重入的嵌套遍历，避免嵌套时清空外层正在遍历的快照列表）
        /// </summary>
        private bool m_IsForEachEvent;

#if UNITY_ASSERTIONS
        /// <summary>
        /// 构造线程 ID（断言期校验公共入口仅被该线程访问）
        /// </summary>
        private readonly int m_CreatorThreadId;
#endif

        /// <summary>
        /// 初始化事件池的新实例。
        /// </summary>
        public EventPool()
        {
            m_EventQueue            = new Queue<Event>();
            m_EventHandlerMultiDict = new FuMultiDictionary<string, EventHandler<T>>();
            m_WaitRemoveHandlerList = new List<(string, EventHandler<T>)>();
            m_HandlerRefCountDict   = new Dictionary<(string, EventHandler<T>), int>();
#if UNITY_ASSERTIONS
            m_CreatorThreadId = Thread.CurrentThread.ManagedThreadId;
#endif
        }

#if UNITY_ASSERTIONS
        /// <summary>
        /// 断言当前线程为构造线程（主线程）；违规立即报错定位，而非静默产生数据竞争。
        /// 发布版为空实现，零开销。
        /// </summary>
        private void AssertMainThread()
        {
            if (Thread.CurrentThread.ManagedThreadId != m_CreatorThreadId)
                FuLogger.LogError($"[EventPool]事件池仅允许主线程访问，检测到跨线程调用（线程 ID:{Thread.CurrentThread.ManagedThreadId}）。");
        }
#endif

        /// <summary>
        /// 获取事件处理函数的数量。
        /// 注意：多值字典自身的 Count 是「有订阅的事件 ID 数」，而多播语义下同一事件可挂多个处理函数，
        /// 故此处遍历累加各事件的处理器条目数，保证与命名语义一致；仅供诊断/编辑器面板使用，勿在热路径调用。
        /// </summary>
        public int EventHandlerCount
        {
            get
            {
                AssertMainThread();
                var count = 0;
                foreach (var (_, handlers) in m_EventHandlerMultiDict)
                {
                    count += handlers.Count;
                }

                return count;
            }
        }

        /// <summary>
        /// 获取事件数量。
        /// </summary>
        public int EventCount
        {
            get
            {
                AssertMainThread();
                return m_EventQueue.Count;
            }
        }

        /// <summary>
        /// 事件池轮询（仅由 EventModule.OnUpdate 每帧驱动一次，禁止嵌套驱动）。
        /// </summary>
        public void Update(float deltaTime, float unscaledDeltaTime)
        {
            AssertMainThread();

            // 批次数量在进入时固定，处理过程中新入队的事件留到下一帧，杜绝同帧无限级联。
            // 嵌套 Update（如事件处理函数内手动驱动帧更新）会复用同一批次列表，内层 Clear 会清掉
            // 外层正在遍历的批次 → 外层剩余事件既不分发也不回收。故契约禁止嵌套，断言期检测到立即报错。
            var batch = m_CachedEventBatch;
#if UNITY_ASSERTIONS
            if (m_IsUpdatingEvents)
                FuLogger.LogError("[EventPool]检测到嵌套 Update：禁止在事件处理函数内手动驱动帧更新，外层批次将被打断。");
#endif
            m_IsUpdatingEvents = true;

            try
            {
                var count = m_EventQueue.Count;
                if (count <= 0) return;

                batch.Clear();
                for (var i = 0; i < count; i++)
                {
                    batch.Add(m_EventQueue.Dequeue());
                }

                // 逐条分发并回收；handler 抛异常时未处理的批次事件仍占用池对象，用 finally 兜底回收
                var processed = 0;
                try
                {
                    for (; processed < batch.Count; processed++)
                    {
                        var tempEvent = batch[processed];
                        HandleEvent(tempEvent.Sender, tempEvent.EventArgs);
                        // 节点回收单独隔离（写法同 Clear 的逐项排水）：引用池不变式一旦被破坏（如使用计数归零），
                        // Recycle 会抛异常——不得逃逸到无保护的 ModuleManager.Update 中断当帧后续模块；
                        // 且此刻事件已分发、参数已由 HandleEvent 的 finally 回收，吞掉异常继续处理下一节点即可。
                        try
                        {
                            ReferencePool.Recycle(tempEvent);
                        }
                        catch (Exception exception)
                        {
                            FuLogger.LogError($"[EventPool]回收已分发事件节点异常:{exception}");
                        }
                    }
                }
                finally
                {
                    // 异常发生在 Recycle(tempEvent) 之前，故 [processed, Count) 的节点均未被回收。
                    // 其中 batch[processed] 已交给 HandleEvent（其 finally 无条件回收了 EventArgs，见 HandleEvent），
                    // 此处只补回收节点；batch[processed+1..Count) 是「出队但未分发」——EventArgs 仍由本池持有，
                    // 必须与节点一并回收：Event.Clear() 只把 EventArgs 置 null 而不回收，漏掉会让
                    // ReferencePool 的事件参数计数永久漂移（未分发即丢弃的路径不能指望 HandleEvent 兜底）。
                    // 每个回收各自 try/catch（与 Clear 的排水写法一致，见 Clear）：ReferencePool.Recycle 一旦抛异常，
                    // 不得中断整轮兜底——否则其后节点全部不再归还（计数永久漂移），且异常会顶替原始异常掩盖真正的故障点。
                    for (var i = processed; i < batch.Count; i++)
                    {
                        var unhandledEvent = batch[i];
                        if (i > processed)
                        {
                            try
                            {
                                ReferencePool.Recycle(unhandledEvent.EventArgs);
                            }
                            catch (Exception exception)
                            {
                                FuLogger.LogError($"[EventPool]兜底回收未分发事件参数异常:{exception}");
                            }
                        }

                        try
                        {
                            ReferencePool.Recycle(unhandledEvent);
                        }
                        catch (Exception exception)
                        {
                            FuLogger.LogError($"[EventPool]兜底回收事件节点异常:{exception}");
                        }
                    }
                }
            }
            finally
            {
                m_IsUpdatingEvents = false;
            }
        }

        /// <summary>
        /// 关闭并清理事件池。
        /// </summary>
        public void Shutdown()
        {
            AssertMainThread();
            Clear();
            m_EventHandlerMultiDict.Clear();
            m_WaitRemoveHandlerList.Clear();
            m_HandlerRefCountDict.Clear();
            m_DefaultHandler = null;
        }

        /// <summary>
        /// 清理事件。
        /// </summary>
        public void Clear()
        {
            AssertMainThread();

            // 逐个出队并回收池节点：直接 Clear() 会把队列中的 Event 节点（池对象）丢弃，导致 ReferencePool 计数泄漏。
            // 队列中的节点全部是「已入队、未分发」，其 EventArgs 也由本池持有，必须一并回收：
            // Event.Clear() 只置 null 不回收，只回收节点会让事件参数计数跨 Shutdown/重启持续累积（ClearAll 保留计数）。
            while (m_EventQueue.Count > 0)
            {
                var eventNode = m_EventQueue.Dequeue();
                // 逐项隔离回收：ReferencePool.Recycle 一旦抛异常，不得中断整轮排水，
                // 否则其后节点与事件参数全部不再归还（ReferencePool 计数永久漂移）；故各自 try/catch 吞掉并继续。
                try
                {
                    ReferencePool.Recycle(eventNode.EventArgs);
                }
                catch (Exception exception)
                {
                    FuLogger.LogError($"[EventPool]清理事件时回收事件参数异常:{exception}");
                }

                try
                {
                    ReferencePool.Recycle(eventNode);
                }
                catch (Exception exception)
                {
                    FuLogger.LogError($"[EventPool]清理事件时回收事件节点异常:{exception}");
                }
            }
        }

        /// <summary>
        /// 获取指定事件对应的处理函数的数量。
        /// </summary>
        public int Count(string id)
        {
            AssertMainThread();
            return m_EventHandlerMultiDict.TryGetValue(id, out var handlers) ? handlers.Count : 0;
        }

        /// <summary>
        /// 检查是否已存在指定事件对应的处理函数。
        /// </summary>
        public bool Check(string id, EventHandler<T> handler)
        {
            AssertMainThread();
            if (handler == null) throw new InvalidOperationException("[EventPool]事件对应的处理函数不能为空!");

            return m_EventHandlerMultiDict.Contains(id, handler);
        }

        /// <summary>
        /// 订阅事件处理函数。
        /// 同一 (id, handler) 可被多个订阅者重复订阅：按条目引用计数（每订阅一次计一份，分发时只调用一次），
        /// 退订只递减自己那一份。同一事件的不同处理函数（多播）各自独立条目与计数。
        /// </summary>
        public void Subscribe(string id, EventHandler<T> handler)
        {
            AssertMainThread();
            if (handler == null) throw new InvalidOperationException("[EventPool]事件对应的处理函数不能为空!");

            // 该事件尚无任何订阅：直接建立首个条目
            if (!m_EventHandlerMultiDict.Contains(id))
            {
                m_EventHandlerMultiDict.Add(id, handler);
                m_HandlerRefCountDict[(id, handler)] = 1;
                return;
            }

            // 同一 (id, handler) 再次订阅（多订阅者场景）：按引用计数累计，不重复入字典，分发时只调用一次
            if (m_EventHandlerMultiDict.Contains(id, handler))
            {
                // 计数为 0 表示条目此前已登记待移除（尚未被 ProcessWaitRemoveHandlers 摘除），
                // 本次订阅即撤销该登记，令订阅立即生效——否则「退订 → 重订阅」交错时，新订阅会被延迟删除吞掉。
                var key = (id, handler);
                m_HandlerRefCountDict.TryGetValue(key, out var refCount);
                m_HandlerRefCountDict[key] = refCount + 1;
                m_WaitRemoveHandlerList.Remove(key);
                return;
            }

            // 同一事件的不同处理函数（多播）：追加到该事件的处理链表，各自独立计数
            m_EventHandlerMultiDict.Add(id, handler);
            m_HandlerRefCountDict[(id, handler)] = 1;
        }

        /// <summary>
        /// 取消订阅事件处理函数。
        /// 引用计数 &gt; 1 时只递减自己那一份（其它订阅者的订阅保持有效）；归零才登记延迟移除。
        /// 退订契约：各订阅者只能退订自己登记的那一份——超额退订（次数超过自身订阅数）会继续消耗
        /// 其他订阅者的计数，导致他人订阅被静默移除且无告警；建议通过 EventRegister 按份管理订阅与退订。
        /// </summary>
        public void Unsubscribe(string id, EventHandler<T> handler)
        {
            AssertMainThread();
            if (handler == null) throw new InvalidOperationException("[EventPool]事件对应的处理函数不能为空!");

            var key = (id, handler);
            // 未计数的 (id, handler)（从未订阅）直接忽略
            if (!m_HandlerRefCountDict.TryGetValue(key, out var refCount)) return;

            if (refCount > 1)
            {
                m_HandlerRefCountDict[key] = refCount - 1;
                return;
            }

            // 归零（重复退订时 refCount 已为 0，保持 0 不再递减）：登记延迟移除，登记去重保证一条
            m_HandlerRefCountDict[key] = 0;
            if (!m_WaitRemoveHandlerList.Contains(key))
                m_WaitRemoveHandlerList.Add(key);
        }

        /// <summary>
        /// 设置默认事件处理函数。
        /// </summary>
        public void SetDefaultHandler(EventHandler<T> handler)
        {
            AssertMainThread();
            m_DefaultHandler = handler;
        }

        /// <summary>
        /// 抛出事件（延迟处理，下一帧分发）。
        /// 注意：分发结束即回收事件参数并 Clear（见 HandleEvent），处理函数不得转发或缓存收到的 eArgs，
        /// 否则其它处理函数/后续帧会观测到已被清空的数据；需要转发时请新建事件参数对象。
        /// 注意：事件池关停（模块 OnDispose）之后不得再调用本方法，否则事件将滞留队列永不回收。
        /// </summary>
        public void Broadcast(object sender, T eArgs)
        {
            AssertMainThread();
            if (eArgs == null) throw new InvalidOperationException("[EventPool]事件参数不能为空!");

            var tempEvent = Event.Create(sender, eArgs);
            m_EventQueue.Enqueue(tempEvent);
        }

        /// <summary>
        /// 立即抛出事件，事件会立刻分发。
        /// 注意：本方法返回时事件参数已被回收并 Clear，处理函数不得转发或缓存收到的 eArgs（同 Broadcast）。
        /// </summary>
        public void BroadcastNow(object sender, T eArgs)
        {
            AssertMainThread();
            if (eArgs == null) throw new InvalidOperationException("[EventPool]事件参数不能为空!");
            HandleEvent(sender, eArgs);
        }

        /// <summary>
        /// 遍历所有事件处理函数（仅限诊断/调试用途）。
        /// 回调内可重入触发分发/遍历——重入识别见 m_IsForEachHandler，重入时使用局部快照互不干扰。
        /// </summary>
        public void ForEachHandler(Action<string, EventHandler<T>> action)
        {
            AssertMainThread();

            // 先快照 (id, handler) 再调用回调：
            // 直接遍历链表时，回调内（同线程重入）新增订阅会以 AddBefore(range.End) 插在尾部被本次遍历再次访问，
            // 与 HandleEvent 同款问题；快照隔离同时避免回调期间集合被修改。
            // 重入识别（写法同 HandleEvent 的 m_IsHandlingEvent）：嵌套遍历若复用同一缓存列表，内层 Clear 会清掉
            // 外层正在遍历的快照 → 外层剩余项既不派发也不回收。故非重入复用缓存字段、重入改用局部列表。
            List<(string id, EventHandler<T> handler)> snapshot;
            if (m_IsForEachHandler)
            {
                snapshot = new List<(string id, EventHandler<T> handler)>();
            }
            else
            {
                snapshot           = m_CachedHandlerPairSnapshot;
                m_IsForEachHandler = true;
            }

            try
            {
                snapshot.Clear();
                foreach (var (id, handlers) in m_EventHandlerMultiDict)
                {
                    foreach (var handler in handlers)
                    {
                        snapshot.Add((id, handler));
                    }
                }

                for (var i = 0; i < snapshot.Count; i++)
                {
                    action(snapshot[i].id, snapshot[i].handler);
                }
            }
            finally
            {
                if (ReferenceEquals(snapshot, m_CachedHandlerPairSnapshot))
                    m_IsForEachHandler = false;
            }
        }

        /// <summary>
        /// 遍历所有事件。
        /// 回调内不得修改或回收未分发的事件参数（其所有权仍在事件池，分发结束后由池回收）。
        /// </summary>
        public void ForEachEvent(Action<object, T> action)
        {
            AssertMainThread();

            // 先快照 (Sender, EventArgs)，再调用回调：
            // 快照隔离避免回调期间队列被修改，重入识别与 ForEachHandler 一致：
            // 非重入复用缓存字段、重入改用局部列表，且缓存复用下零分配。
            List<(object sender, T eventArgs)> snapshot;
            if (m_IsForEachEvent)
            {
                snapshot = new List<(object sender, T eventArgs)>();
            }
            else
            {
                snapshot         = m_CachedEventArgsSnapshot;
                m_IsForEachEvent = true;
            }

            try
            {
                snapshot.Clear();
                foreach (var tempEvent in m_EventQueue)
                {
                    snapshot.Add((tempEvent.Sender, tempEvent.EventArgs));
                }

                for (var i = 0; i < snapshot.Count; i++)
                {
                    action(snapshot[i].sender, snapshot[i].eventArgs);
                }
            }
            finally
            {
                if (ReferenceEquals(snapshot, m_CachedEventArgsSnapshot))
                    m_IsForEachEvent = false;
            }
        }

        /// <summary>
        /// 处理事件结点。
        /// 本方法无条件拥有 eArgs：返回前（含异常路径）必定回收并 Clear 它，故处理函数不得转发或缓存收到的 eArgs，
        /// 否则转发目标会观测到已清空的数据，且与「回池后可能被复用」形成脏读。需要转发时请新建事件参数对象。
        /// </summary>
        private void HandleEvent(object sender, T eArgs)
        {
            // 必须在 finally 的 Recycle 之前取出 Id：Recycle 会调用 eArgs.Clear()（如 EmptyEventArgs 把 Id 复位为类型全名），
            // 之后再读会得到失真的 Id；且对象已回池，可能被复用，属于脏读。
            // 取值本身也放进 try 内：本方法一旦被调用就无条件拥有 eArgs（finally 负责回收），
            // 若取值在 try 之外抛异常，finally 不会执行 → eArgs 不回收，而 Update 的兜底只回收「未分发」节点的参数，
            // 不会重复回收已交给本方法的参数，届时计数将永久泄漏。
            string eventId = null;

            try
            {
                eventId = eArgs.Id;

                // 在处理事件前，先处理所有待取消的订阅，确保事件处理时使用的是最新的handler列表
                ProcessWaitRemoveHandlers();

                // 调用该事件Id下对应的所有处理函数。
                // 分发前先把该 id 的 handler 快照到临时列表再逐个调用：Subscribe 走 AddBefore(range.End) 插在尾部，
                // 若沿链表边遍历边调用，回调内新订阅（例如注册自身）的 handler 会被本次分发再次调用，可致同帧无限循环。
                List<EventHandler<T>> snapshot;
                if (m_IsHandlingEvent)
                {
                    // 重入（handler 内 BroadcastNow 触发嵌套分发）：复用外层快照会被清空，单独分配
                    snapshot = new List<EventHandler<T>>();
                }
                else
                {
                    snapshot = m_CachedHandlerSnapshot;
                    snapshot.Clear();
                    m_IsHandlingEvent = true;
                }

                try
                {
                    var hasHandlers = m_EventHandlerMultiDict.TryGetValue(eventId, out var handlerRange);
                    if (hasHandlers)
                    {
                        for (var currentNode = handlerRange.First; currentNode != null && currentNode != handlerRange.End; currentNode = currentNode.Next)
                        {
                            snapshot.Add(currentNode.Value);
                        }
                    }

                    if (hasHandlers)
                    {
                        // 逐个 handler 独立 try/catch（契约见 Event/README.md「事件处理函数异常会被捕获并记录，不影响其他处理函数」）：
                        // 任一处理函数抛异常只记录并继续调用后续 handler；绝不让异常穿透本次分发，
                        // 否则 Update 的 finally 会回收剩余未分发事件却不分发（静默丢事件），
                        // 异常继续逃到无保护的 ModuleManager.Update 后更会导致当帧其后所有模块停更。
                        for (var i = 0; i < snapshot.Count; i++)
                        {
                            try
                            {
                                snapshot[i].Invoke(sender, eArgs);
                            }
                            catch (Exception exception)
                            {
                                FuLogger.LogError($"[EventPool]处理事件 '{eventId}' 的处理函数时发生异常:{exception}");
                            }
                        }
                    }
                    else if (m_DefaultHandler != null)
                    {
                        // 默认处理器同样隔离：单个默认处理器异常不得中断本次分发，更不能向上逃逸。
                        try
                        {
                            m_DefaultHandler.Invoke(sender, eArgs);
                        }
                        catch (Exception exception)
                        {
                            FuLogger.LogError($"[EventPool]处理事件 '{eventId}' 的默认处理函数时发生异常:{exception}");
                        }
                    }
                    // 无处理器且无默认处理器：静默丢弃（原 AllowNoHandler 语义，现为唯一语义）。
                }
                finally
                {
                    if (ReferenceEquals(snapshot, m_CachedHandlerSnapshot))
                        m_IsHandlingEvent = false;
                }
            }
            finally
            {
                // 回收自身若抛异常（引用池内部异常）不得逃逸：Update 的兜底按「batch[processed] 已交给 HandleEvent、
                // 其 EventArgs 由本方法负责回收」处理，一旦此处异常冒出，Update 的节点回收 finally 会被连带打断，
                // 导致该节点及其后未分发节点一并漏回收（ReferencePool 计数永久漂移）。
                // 故吞掉回收异常，确保「回收失败也不漏节点回收」。
                // 注：分发阶段的 handler 异常已在上方逐个捕获记录，此处仅剩回收自身异常。
                try
                {
                    ReferencePool.Recycle(eArgs);
                }
                catch (Exception exception)
                {
                    // 有意不重抛：回收失败不应中断调用方（Update）的分发与节点回收流程。
                    // 但降级为可观测告警而非静默吞没——此处异常通常意味着「重复归还」这一硬保护被触发，
                    // 静默会掩盖 ABA 误用信号；记录告警即可保留可追溯性，同时不影响上层流程。
                    FuLogger.LogWarning($"[EventPool]回收事件参数异常（疑似重复归还误用）:{exception}");
                }
            }
        }

        /// <summary>
        /// 处理所有待取消的订阅。
        /// </summary>
        private void ProcessWaitRemoveHandlers()
        {
            if (m_WaitRemoveHandlerList.Count == 0) return;

            foreach (var (id, handler) in m_WaitRemoveHandlerList)
            {
                var key = (id, handler);

                // 登记之后又被重新订阅（Subscribe 会撤销登记，此处为双保险）：计数回到正数则不摘除
                if (m_HandlerRefCountDict.TryGetValue(key, out var refCount) && refCount > 0) continue;

                m_EventHandlerMultiDict.Remove(id, handler);
                m_HandlerRefCountDict.Remove(key);
            }

            m_WaitRemoveHandlerList.Clear();
        }
    }
}
```

- [ ] **Step 2: 删除 `EEventPoolMode.cs` 与 `EEventPoolMode.cs.meta`**

```powershell
Remove-Item "Unity/Assets/Scripts/Hotfix/Framework/Event/EventPool/EEventPoolMode.cs", "Unity/Assets/Scripts/Hotfix/Framework/Event/EventPool/EEventPoolMode.cs.meta"
```

- [ ] **Step 3: 修改 `EventModule.cs:36` 构造调用**

```csharp
            m_EventPool = new EventPool<GameEventArgs>();
```

- [ ] **Step 4: 编译验证**

预期：零错误。

- [ ] **Step 5: 残留检查**

```
grep -rn "EEventPoolMode\|AllowDuplicateHandler\|AllowMultiHandler\|AllowNoHandler\|m_EventHandlerLock\|m_PoolMode" Unity/Assets/ --include="*.cs"
# 预期：零命中（含 Assets/Editor——参照 Editor 反射耦合教训，README 由 Task 6 处理）
```

---

### Task 6: README 同步

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Event/README.md`（第 1-49 行简介/特性/3.1 节、约 605 行文件树）
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Network/README.md`（约 385 行注意事项）

- [ ] **Step 1: Event/README.md 简介与特性（第 3-18 行）替换为**

```markdown
## 1. 简介

FuFramework Event 模块是一个高性能的事件管理系统。它提供了灵活的事件订阅/发布机制，集成引用池技术以减少 GC 压力。模块仅允许主线程访问；跨线程产生的事件（如网络 Socket 回调）由网络频道封送到主线程后再 Broadcast。

***

## 2. 特性

- 仅主线程   ：公共 API 仅允许主线程调用，开发期（UNITY_ASSERTIONS）有断言拦截跨线程误用
- 延迟处理   ：事件默认在下一帧统一处理，避免在处理事件时修改订阅列表导致的异常
- 多播支持   ：支持一个事件对应多个处理函数；同一处理函数被多个订阅者订阅时按引用计数每事件只调用一次
- 对象池集成 ：事件参数和事件节点都通过引用池管理，减少GC压力
- 模块级管理 ：`EventRegister` 提供模块级的事件订阅管理，自动处理生命周期
```

- [ ] **Step 2: Event/README.md 删除 3.1 节（第 22-49 行「核心概念」的 EEventPoolMode 小节），后续小节编号顺延**

删除后「核心概念」直接从「事件参数基类」开始。

- [ ] **Step 3: Event/README.md 文件树删除 EEventPoolMode.cs 行（约 605 行）**

- [ ] **Step 4: Network/README.md 注意事项（约 385 行）替换为**

```markdown
1. **主线程限制**：网络回调（数据包消息与 Connected/Closed/Error 生命周期事件）均在主线程执行——
   Socket 回调线程只入队，主线程 channel.Update 排水后触发；消息处理器中避免阻塞操作
```

- [ ] **Step 5: 编译验证**

预期：零错误（README 不影响编译，此步确认工作区整体仍干净）。

---

### Task 7: 最终验证与交付

- [ ] **Step 1: 全套残留检查（一次性跑完）**

```
grep -rn "EEventPoolMode\|AllowDuplicateHandler\|AllowMultiHandler\|AllowNoHandler" Unity/Assets/
grep -rn "m_EventHandlerLock\|m_PoolMode" Unity/Assets/
grep -rn "NetworkChannelActiveChanged" Unity/Assets/
# 三项预期均为零命中
```

- [ ] **Step 2: 编译验证**

预期：Hotfix 与 Editor 程序集零错误。

- [ ] **Step 3: 输出 Play 模式手工验证清单给用户**（执行方不代替用户跑游戏，逐项列出待用户确认）：

1. 登录 → 主界面流程正常（UI / RedDot / Sound 等订阅方收得到事件）；
2. 网络连接 → 断线：Connected / Closed / Error 正常到达，且触发线程为主线程（可临时插桩打 `Thread.CurrentThread.ManagedThreadId`，验完还原）；
3. 同 handler 多订阅者：退订一份不影响他人；
4. `EventModuleInspector` 正常显示；
5. （可选）Profiler GC Alloc 确认 `EventPool.Update` 稳态零分配。

- [ ] **Step 4: 汇报改动文件清单**（7 个文件：EventPool.cs 重写、EEventPoolMode.cs 删除、EventModule.cs、NetworkChannelBase.cs、SystemTcpNetworkChannel.cs、WebSocketNetworkChannel.cs、两份 README），等待用户验证后按 `Docs/Git提交规范.md` 整理提交（建议两笔：`refactor(network)` 封送 + `refactor(event)` 去模式去锁，最终以用户确认为准）。
