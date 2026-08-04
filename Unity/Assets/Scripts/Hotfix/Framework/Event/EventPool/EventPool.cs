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
            lock (m_EventQueue)
            {
                while (m_EventQueue.Count > 0)
                {
                    var tempEvent = m_EventQueue.Dequeue();
                    HandleEvent(tempEvent.Sender, tempEvent.EventArgs);
                    GlobalModule.ReferencePoolModule.Recycle(tempEvent);
                }
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
                m_EventQueue.Clear();
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
            lock (m_EventHandlerLock)
            {
                foreach (var (id, handlers) in m_EventHandlerMultiDict)
                {
                    foreach (var handler in handlers)
                    {
                        action(id, handler);
                    }
                }
            }
        }
        
        /// <summary>
        /// 遍历所有事件。
        /// </summary>
        public void ForEachEvent(Action<object, T> action)
        {
            lock (m_EventQueue)
            {
                foreach (var tempEvent in m_EventQueue)
                {
                    action(tempEvent.Sender, tempEvent.EventArgs);
                }
            }
        }

        /// <summary>
        /// 处理事件结点。
        /// </summary>
        private void HandleEvent(object sender, T eArgs)
        {
            var noHandlerException = false;

            try
            {
                // 在处理事件前，先处理所有待取消的订阅，确保事件处理时使用的是最新的handler列表
                ProcessWaitRemoveHandlers();

                // 调用该事件Id下对应的所有处理函数
                if (m_EventHandlerMultiDict.TryGetValue(eArgs.Id, out var handlerRange))
                {
                    var currentNode = handlerRange.First;
                    while (currentNode != null && currentNode != handlerRange.End)
                    {
                        currentNode.Value.Invoke(sender, eArgs);
                        currentNode = currentNode.Next;
                    }
                }
                else if (m_DefaultHandler != null)
                {
                    m_DefaultHandler.Invoke(sender, eArgs);
                }
                else if ((m_PoolMode & EEventPoolMode.AllowNoHandler) == 0)
                {
                    noHandlerException = true;
                }
            }
            finally
            {
                GlobalModule.ReferencePoolModule.Recycle(eArgs);
            }

            if (noHandlerException)
                throw new InvalidOperationException($"[EventPool]事件 '{eArgs.Id}' 没有对应的处理函数!");
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
