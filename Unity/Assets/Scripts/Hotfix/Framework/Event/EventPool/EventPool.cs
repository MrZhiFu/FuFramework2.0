using System;
using System.Collections.Generic;
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
        /// 待删除的事件处理器列表（线程安全的取消订阅方案，确保事件处理时使用的是最新的处理函数handler列表）
        /// </summary>
        private readonly List<(string id, EventHandler<T> handler)> m_WaitRemoveHandlerList;

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
        /// 初始化事件池的新实例。
        /// </summary>
        /// <param name="mode">事件池模式。</param>
        public EventPool(EEventPoolMode mode)
        {
            m_PoolMode        = mode;
            m_DefaultHandler        = null;
            m_EventQueue            = new Queue<Event>();
            m_EventHandlerMultiDict = new FuMultiDictionary<string, EventHandler<T>>();
            m_WaitRemoveHandlerList = new List<(string, EventHandler<T>)>();
        }

        /// <summary>
        /// 获取事件处理函数的数量。
        /// </summary>
        public int EventHandlerCount
        {
            get
            {
                lock (m_EventHandlerLock)
                {
                    return m_EventHandlerMultiDict.Count;
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
                        ReferencePool.Recycle(tempEvent);
                    }
                }
                finally
                {
                    // 异常发生在 Recycle(tempEvent) 之前，故 [processed, Count) 均未被回收（HandleEvent 内部只回收 eArgs）
                    for (var i = processed; i < batch.Count; i++)
                    {
                        ReferencePool.Recycle(batch[i]);
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
                // 逐个出队并回收池节点：直接 Clear() 会把队列中的 Event 节点（池对象）丢弃，导致 ReferencePool 计数泄漏
                while (m_EventQueue.Count > 0)
                {
                    ReferencePool.Recycle(m_EventQueue.Dequeue());
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
        /// </summary>
        public void Subscribe(string id, EventHandler<T> handler)
        {
            if (handler == null) throw new InvalidOperationException("[EventPool]事件对应的处理函数不能为空!");

            lock (m_EventHandlerLock)
            {
                if (!m_EventHandlerMultiDict.Contains(id))
                {
                    m_EventHandlerMultiDict.Add(id, handler);
                    return;
                }

                if ((m_PoolMode & EEventPoolMode.AllowMultiHandler) != EEventPoolMode.AllowMultiHandler)
                    throw new InvalidOperationException($"[EventPool]事件 '{id}' 不允许多次注册处理函数!");

                if ((m_PoolMode & EEventPoolMode.AllowDuplicateHandler) != EEventPoolMode.AllowDuplicateHandler && Check(id, handler))
                    throw new InvalidOperationException($"[EventPool]事件 '{id}' 不允许重复注册处理函数!");

                m_EventHandlerMultiDict.Add(id, handler);
            }
        }

        /// <summary>
        /// 取消订阅事件处理函数。
        /// </summary>
        public void Unsubscribe(string id, EventHandler<T> handler)
        {
            if (handler == null) throw new InvalidOperationException("[EventPool]事件对应的处理函数不能为空!");

            // 先将待取消的handler添加到待删除列表，在事件处理时统一移除
            lock (m_EventHandlerLock)
            {
                m_WaitRemoveHandlerList.Add((id, handler));
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
        /// </summary>
        public void BroadcastNow(object sender, T eArgs)
        {
            if (eArgs == null) throw new InvalidOperationException("[EventPool]事件参数不能为空!");
            HandleEvent(sender, eArgs);
        }

        /// <summary>
        /// 遍历所有事件处理函数。
        /// </summary>
        public void ForEachHandler(Action<string, EventHandler<T>> action)
        {
            // 先在锁内快照 (id, handler) 再在锁外调用回调：
            // 直接持锁遍历链表时，回调内（同线程可重入取锁）新增订阅会以 AddBefore(range.End) 插在尾部被本次遍历再次访问，
            // 与 HandleEvent 同款问题；锁外调用同时避免持处理器锁执行用户代码。
            var snapshot = new List<(string id, EventHandler<T> handler)>();
            lock (m_EventHandlerLock)
            {
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
        
        /// <summary>
        /// 遍历所有事件。
        /// </summary>
        public void ForEachEvent(Action<object, T> action)
        {
            // 先在锁内快照 (Sender, EventArgs)，再在锁外调用回调：
            // 持 m_EventQueue 锁执行用户代码会与 Shutdown（处理器锁 → Clear 的队列锁）构成反向锁序而死锁；
            // 锁外调用同时避免长期占用队列锁阻塞其它线程的 Broadcast。
            var snapshot = new List<(object sender, T eventArgs)>();
            lock (m_EventQueue)
            {
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

        /// <summary>
        /// 处理事件结点。
        /// </summary>
        private void HandleEvent(object sender, T eArgs)
        {
            // 必须在 finally 的 Recycle 之前取出 Id：Recycle 会调用 eArgs.Clear()（如 EmptyEventArgs 把 Id 复位为类型全名），
            // 之后再读会得到失真的 Id；且对象已回池，可能被其它线程复用，属于脏读。
            var eventId = eArgs.Id;

            var noHandlerException = false;

            try
            {
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
                        for (var i = 0; i < snapshot.Count; i++)
                        {
                            snapshot[i].Invoke(sender, eArgs);
                        }
                    }
                    else if (defaultHandler != null)
                    {
                        defaultHandler.Invoke(sender, eArgs);
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
                ReferencePool.Recycle(eArgs);
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
                    m_EventHandlerMultiDict.Remove(id, handler);
                }

                m_WaitRemoveHandlerList.Clear();
            }
        }
    }
}
