using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        /// <summary>
        /// 构造线程 ID（断言期校验公共入口仅被该线程访问）。
        /// 无条件编译：[Conditional] 语义要求被断言方法及其依赖字段始终存在。
        /// </summary>
        private readonly int m_CreatorThreadId;

        /// <summary>
        /// 初始化事件池的新实例。
        /// </summary>
        public EventPool()
        {
            m_EventQueue            = new Queue<Event>();
            m_EventHandlerMultiDict = new FuMultiDictionary<string, EventHandler<T>>();
            m_WaitRemoveHandlerList = new List<(string, EventHandler<T>)>();
            m_HandlerRefCountDict   = new Dictionary<(string, EventHandler<T>), int>();
            m_CreatorThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        /// <summary>
        /// 断言当前线程为构造线程（主线程）；违规立即报错定位，而非静默产生数据竞争。
        /// [Conditional("UNITY_ASSERTIONS")]：该符号仅在 Editor/Development 构建定义，
        /// 发布版所有调用点被编译器剥离，零运行时开销。
        /// </summary>
        [Conditional("UNITY_ASSERTIONS")]
        private void AssertMainThread()
        {
            if (Thread.CurrentThread.ManagedThreadId != m_CreatorThreadId)
                FuLogger.LogError($"[EventPool]事件池仅允许主线程访问，检测到跨线程调用（线程 ID:{Thread.CurrentThread.ManagedThreadId}）。");
        }

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
                    // 无处理器且无默认处理器：静默丢弃（原宽松模式语义，现为唯一语义）。
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
