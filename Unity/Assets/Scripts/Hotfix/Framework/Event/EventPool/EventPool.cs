using System;
using System.Collections.Generic;
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
    /// </summary>
    /// <typeparam name="T">该事件池中的事件类型。</typeparam>
    public sealed partial class EventPool<T> where T : BaseEventArgs
    {
        /// <summary>
        /// 事件池模式
        /// </summary>
        private readonly EEventPoolMode m_PoolMode;

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
        /// 待删除的事件处理器列表（线程安全的取消订阅方案，确保事件处理时使用的是最新的处理函数handler列表）。
        /// 同一 (id, handler) 只登记一条（引用计数归零才登记、期间重新订阅即撤销登记），
        /// 否则「退订 → 重订阅」交错时，重复的登记会把后来者的订阅一并移除。
        /// </summary>
        private readonly List<(string id, EventHandler<T> handler)> m_WaitRemoveHandlerList;

        /// <summary>
        /// (id, handler) 条目的订阅引用计数：多个订阅者（如多个 EventRegister、多个模块）共享同一处理函数时，
        /// 各自计一份，退订只递减自己那一份，归零才真正移除条目。
        /// 仅在非 AllowDuplicateHandler 模式下使用（该模式按「同 handler 多条目」语义处理，不参与计数）。
        /// 不变式：计数 &gt; 0 ⇔ 条目在 m_EventHandlerMultiDict 中；计数 == 0 ⇔ 条目仍在字典中但已登记待移除。
        /// </summary>
        private readonly Dictionary<(string id, EventHandler<T> handler), int> m_HandlerRefCountDict;

        /// <summary>
        /// 事件处理器的同步锁
        /// </summary>
        private readonly object m_EventHandlerLock = new();

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
        /// 是否正在执行帧批次搬运（用于识别重入的嵌套 Update，避免嵌套时清空外层正在遍历的批次列表）
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
        /// 初始化事件池的新实例。
        /// </summary>
        /// <param name="mode">事件池模式。</param>
        public EventPool(EEventPoolMode mode)
        {
            m_PoolMode              = mode;
            m_DefaultHandler        = null;
            m_EventQueue            = new Queue<Event>();
            m_EventHandlerMultiDict = new FuMultiDictionary<string, EventHandler<T>>();
            m_WaitRemoveHandlerList = new List<(string, EventHandler<T>)>();
            m_HandlerRefCountDict   = new Dictionary<(string, EventHandler<T>), int>();
        }

        /// <summary>
        /// 获取事件处理函数的数量。
        /// 注意：多值字典自身的 Count 是「有订阅的事件 ID 数」，而 AllowMultiHandler 模式下同一事件可挂多个处理函数，
        /// 故此处遍历累加各事件的处理器条目数，保证与命名语义一致；仅供诊断/编辑器面板使用，勿在热路径调用。
        /// </summary>
        public int EventHandlerCount
        {
            get
            {
                lock (m_EventHandlerLock)
                {
                    var count = 0;
                    foreach (var (_, handlers) in m_EventHandlerMultiDict)
                    {
                        count += handlers.Count;
                    }

                    return count;
                }
            }
        }

        /// <summary>
        /// 获取事件数量。
        /// </summary>
        public int EventCount
        {
            get
            {
                lock (m_EventQueue)
                {
                    return m_EventQueue.Count;
                }
            }
        }

        /// <summary>
        /// 事件池轮询。
        /// </summary>
        public void Update(float deltaTime, float unscaledDeltaTime)
        {
            // 1) 锁内只把"本帧批次"搬运到局部列表，锁外再分发用户 handler：
            //    避免与 ForEachHandler（处理器锁 → 用户回调 → Broadcast 队列锁）形成相反的锁序而死锁，
            //    也避免持队列锁分发期间阻塞其它线程的 Broadcast。
            // 2) 批次数量在进入时固定，处理过程中新入队的事件留到下一帧，杜绝同帧无限级联。
            // 重入识别（写法同 HandleEvent 的 m_IsHandlingEvent）：
            // 嵌套 Update（如某 handler 内再次驱动帧更新）若复用同一缓存列表，内层 batch.Clear() 会清掉外层正在遍历的批次
            // → 外层事件既不分发也不回收。故非重入复用缓存字段、重入改用局部列表。
            List<Event> batch;
            if (m_IsUpdatingEvents)
            {
                batch = new List<Event>();
            }
            else
            {
                batch       = m_CachedEventBatch;
                m_IsUpdatingEvents = true;
            }

            try
            {
                lock (m_EventQueue)
                {
                    var count = m_EventQueue.Count;
                    if (count <= 0) return;

                    batch.Clear();
                    for (var i = 0; i < count; i++)
                    {
                        batch.Add(m_EventQueue.Dequeue());
                    }
                }

                // 锁已释放，逐条分发并回收；handler 抛异常时未处理的批次事件仍占用池对象，用 finally 兜底回收
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
                if (ReferenceEquals(batch, m_CachedEventBatch))
                    m_IsUpdatingEvents = false;
            }
        }

        /// <summary>
        /// 关闭并清理事件池。
        /// </summary>
        public void Shutdown()
        {
            lock (m_EventHandlerLock)
            {
                Clear();
                m_EventHandlerMultiDict.Clear();
                m_WaitRemoveHandlerList.Clear();
                m_HandlerRefCountDict.Clear();
                m_DefaultHandler = null;
            }
        }

        /// <summary>
        /// 清理事件。
        /// </summary>
        public void Clear()
        {
            lock (m_EventQueue)
            {
                // 逐个出队并回收池节点：直接 Clear() 会把队列中的 Event 节点（池对象）丢弃，导致 ReferencePool 计数泄漏。
                // 队列中的节点全部是「已入队、未分发」，其 EventArgs 也由本池持有，必须一并回收：
                // Event.Clear() 只置 null 不回收，只回收节点会让事件参数计数跨 Shutdown/重启持续累积（ClearAll 保留计数）。
                // 此处不会与 HandleEvent 的回收重叠——能进入本队列的节点必然尚未被 Update 出队分发。
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
        }

        /// <summary>
        /// 获取指定事件对应的处理函数的数量。
        /// </summary>
        public int Count(string id)
        {
            lock (m_EventHandlerLock)
            {
                return m_EventHandlerMultiDict.TryGetValue(id, out var handlers) ? handlers.Count : 0;
            }
        }

        /// <summary>
        /// 检查是否已存在指定事件对应的处理函数。
        /// </summary>
        public bool Check(string id, EventHandler<T> handler)
        {
            if (handler == null) throw new InvalidOperationException("[EventPool]事件对应的处理函数不能为空!");

            lock (m_EventHandlerLock)
            {
                return m_EventHandlerMultiDict.Contains(id, handler);
            }
        }

        /// <summary>
        /// 订阅事件处理函数。
        /// 同一 (id, handler) 可被多个订阅者重复订阅：非 AllowDuplicateHandler 模式下按条目引用计数（每订阅一次计一份，
        /// 分发时只调用一次），退订只递减自己那一份。AllowDuplicateHandler 模式下仍按「同 handler 多条目」语义逐条新增。
        /// </summary>
        public void Subscribe(string id, EventHandler<T> handler)
        {
            if (handler == null) throw new InvalidOperationException("[EventPool]事件对应的处理函数不能为空!");

            lock (m_EventHandlerLock)
            {
                // 该事件尚无任何订阅：直接建立首个条目
                if (!m_EventHandlerMultiDict.Contains(id))
                {
                    m_EventHandlerMultiDict.Add(id, handler);
                    m_HandlerRefCountDict[(id, handler)] = 1;
                    return;
                }

                // 已有订阅者在先，继续注册必须满足多处理函数模式
                if ((m_PoolMode & EEventPoolMode.AllowMultiHandler) != EEventPoolMode.AllowMultiHandler)
                    throw new InvalidOperationException($"[EventPool]事件 '{id}' 不允许多次注册处理函数!");

                // 同一 (id, handler) 再次订阅（多订阅者场景）：按引用计数累计，不重复入字典；
                // AllowDuplicateHandler 模式下允许出现多条目，逐条新增。
                if (m_EventHandlerMultiDict.Contains(id, handler))
                {
                    if ((m_PoolMode & EEventPoolMode.AllowDuplicateHandler) == EEventPoolMode.AllowDuplicateHandler)
                    {
                        m_EventHandlerMultiDict.Add(id, handler);
                        return;
                    }

                    // 计数为 0 表示条目此前已登记待移除（尚未被 ProcessWaitRemoveHandlers 摘除），
                    // 本次订阅即撤销该登记，令订阅立即生效——否则「退订 → 重订阅」在同一轮分发前交错时，新订阅会被延迟删除吞掉。
                    var key = (id, handler);
                    m_HandlerRefCountDict.TryGetValue(key, out var refCount);
                    m_HandlerRefCountDict[key] = refCount + 1;
                    m_WaitRemoveHandlerList.Remove(key);
                    return;
                }

                // 同一事件的不同处理函数（多归属）：追加到该事件的处理链表，各自独立计数
                m_EventHandlerMultiDict.Add(id, handler);
                m_HandlerRefCountDict[(id, handler)] = 1;
            }
        }

        /// <summary>
        /// 取消订阅事件处理函数。
        /// 引用计数 &gt; 1 时只递减自己那一份（其它订阅者的订阅保持有效）；归零才登记延迟移除。
        /// 退订契约：各订阅者只能退订自己登记的那一份——超额退订（次数超过自身订阅数）会继续消耗
        /// 其他订阅者的计数，导致他人订阅被静默移除且无告警；建议通过 EventRegister 按份管理订阅与退订。
        /// </summary>
        public void Unsubscribe(string id, EventHandler<T> handler)
        {
            if (handler == null) throw new InvalidOperationException("[EventPool]事件对应的处理函数不能为空!");

            // 先将待取消的handler添加到待删除列表，在事件处理时统一移除
            lock (m_EventHandlerLock)
            {
                var key = (id, handler);
                if (m_HandlerRefCountDict.TryGetValue(key, out var refCount))
                {
                    if (refCount > 1)
                    {
                        m_HandlerRefCountDict[key] = refCount - 1;
                        return;
                    }

                    // 归零（重复退订时 refCount 已为 0，保持 0 不再递减）：登记延迟移除，登记去重保证一条
                    m_HandlerRefCountDict[key] = 0;
                    if (!m_WaitRemoveHandlerList.Contains(key))
                        m_WaitRemoveHandlerList.Add(key);
                    return;
                }

                // 未计数的订阅（AllowDuplicateHandler 模式下同一 handler 有多条目）：每次退订登记一条，逐条摘除
                m_WaitRemoveHandlerList.Add(key);
            }
        }

        /// <summary>
        /// 设置默认事件处理函数。
        /// </summary>
        public void SetDefaultHandler(EventHandler<T> handler)
        {
            lock (m_EventHandlerLock)
            {
                m_DefaultHandler = handler;
            }
        }

        /// <summary>
        /// 抛出事件（线程安全，延迟处理）。
        /// 注意：分发结束即回收事件参数并 Clear（见 HandleEvent），处理函数不得转发或缓存收到的 eArgs，
        /// 否则其它处理函数/后续帧会观测到已被清空的数据；需要转发时请新建事件参数对象。
        /// 注意：本方法先从引用池取出事件节点、后取队列入队，与 Clear/Shutdown 的排空存在极小竞态窗口——
        /// 节点已取出而排空先行完成时，该事件将滞留队列且永不分发、永不回收（引用池计数漂移）。
        /// 因此事件池关停（模块 OnDispose）之后不得再调用本方法。
        /// </summary>
        public void Broadcast(object sender, T eArgs)
        {
            if (eArgs == null) throw new InvalidOperationException("[EventPool]事件参数不能为空!");

            var tempEvent = Event.Create(sender, eArgs);
            lock (m_EventQueue)
            {
                m_EventQueue.Enqueue(tempEvent);
            }
        }

        /// <summary>
        /// 立即抛出事件，这个操作不是线程安全的，事件会立刻分发。
        /// 注意：本方法返回时事件参数已被回收并 Clear，处理函数不得转发或缓存收到的 eArgs（同 Broadcast）。
        /// </summary>
        public void BroadcastNow(object sender, T eArgs)
        {
            if (eArgs == null) throw new InvalidOperationException("[EventPool]事件参数不能为空!");
            HandleEvent(sender, eArgs);
        }

        /// <summary>
        /// 遍历所有事件处理函数。
        /// 仅限主线程调用：重入识别标志非原子，与其他线程并发调用本方法会共用同一快照缓存，
        /// 内层 Clear 会破坏外层正在遍历的数据（主线程限制口径同 BroadcastNow）。
        /// </summary>
        public void ForEachHandler(Action<string, EventHandler<T>> action)
        {
            // 先在锁内快照 (id, handler) 再在锁外调用回调：
            // 直接持锁遍历链表时，回调内（同线程可重入取锁）新增订阅会以 AddBefore(range.End) 插在尾部被本次遍历再次访问，
            // 与 HandleEvent 同款问题；锁外调用同时避免持处理器锁执行用户代码。
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
                lock (m_EventHandlerLock)
                {
                    snapshot.Clear();
                    foreach (var (id, handlers) in m_EventHandlerMultiDict)
                    {
                        foreach (var handler in handlers)
                        {
                            snapshot.Add((id, handler));
                        }
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
        /// 仅限主线程调用：重入识别标志非原子，与其他线程并发调用本方法会共用同一快照缓存，
        /// 内层 Clear 会破坏外层正在遍历的数据（主线程限制口径同 BroadcastNow）。
        /// 回调内不得修改或回收未分发的事件参数（其所有权仍在事件池，分发结束后由池回收）。
        /// </summary>
        public void ForEachEvent(Action<object, T> action)
        {
            // 先在锁内快照 (Sender, EventArgs)，再在锁外调用回调：
            // 持 m_EventQueue 锁执行用户代码会与 Shutdown（处理器锁 → Clear 的队列锁）构成反向锁序而死锁；
            // 锁外调用同时避免长期占用队列锁阻塞其它线程的 Broadcast。
            // 重入识别与 ForEachHandler 一致：非重入复用缓存字段、重入改用局部列表，且缓存复用下零分配。
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
                lock (m_EventQueue)
                {
                    snapshot.Clear();
                    foreach (var tempEvent in m_EventQueue)
                    {
                        snapshot.Add((tempEvent.Sender, tempEvent.EventArgs));
                    }
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
            // 之后再读会得到失真的 Id；且对象已回池，可能被其它线程复用，属于脏读。
            // 取值本身也放进 try 内：本方法一旦被调用就无条件拥有 eArgs（finally 负责回收），
            // 若取值在 try 之外抛异常，finally 不会执行 → eArgs 不回收，而 Update 的兜底只回收「未分发」节点的参数，
            // 不会重复回收已交给本方法的参数，届时计数将永久泄漏。
            string eventId = null;

            var noHandlerException = false;

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
                    // 快照构建与 m_DefaultHandler 取值纳入处理器锁，与 Subscribe/Unsubscribe/Shutdown 互斥：
                    // 遍历链表到 range.End 这段不含用户代码，持锁安全；用户回调仍在锁外执行，
                    // 严禁把用户代码放进锁内，否则会与 Broadcast 的队列锁形成锁序反转。
                    EventHandler<T> defaultHandler = null;
                    var hasHandlers = false;
                    lock (m_EventHandlerLock)
                    {
                        if (m_EventHandlerMultiDict.TryGetValue(eventId, out var handlerRange))
                        {
                            hasHandlers = true;
                            for (var currentNode = handlerRange.First; currentNode != null && currentNode != handlerRange.End; currentNode = currentNode.Next)
                            {
                                snapshot.Add(currentNode.Value);
                            }
                        }
                        else
                        {
                            defaultHandler = m_DefaultHandler;
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
                    else if (defaultHandler != null)
                    {
                        // 默认处理器同样隔离：单个默认处理器异常不得中断本次分发，更不能向上逃逸。
                        try
                        {
                            defaultHandler.Invoke(sender, eArgs);
                        }
                        catch (Exception exception)
                        {
                            FuLogger.LogError($"[EventPool]处理事件 '{eventId}' 的默认处理函数时发生异常:{exception}");
                        }
                    }
                    else if ((m_PoolMode & EEventPoolMode.AllowNoHandler) == 0)
                    {
                        noHandlerException = true;
                    }
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
                // 注：分发阶段的 handler 异常已在上方逐个捕获记录（不再向上抛出），此处仅剩「无处理器」异常仍会抛出。
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

            if (noHandlerException)
                throw new InvalidOperationException($"[EventPool]事件 '{eventId}' 没有对应的处理函数!");
        }

        /// <summary>
        /// 处理所有待取消的订阅。
        /// </summary>
        private void ProcessWaitRemoveHandlers()
        {
            lock (m_EventHandlerLock)
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
}
