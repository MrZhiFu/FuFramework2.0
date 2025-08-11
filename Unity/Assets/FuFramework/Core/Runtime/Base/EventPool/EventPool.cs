using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 事件池。
    /// </summary>
    /// <typeparam name="T">事件类型。</typeparam>
    public sealed partial class EventPool<T> where T : BaseEventArgs
    {
         /// 事件池模式
        private readonly EventPoolMode m_EventPoolMode;

        /// 事件默认处理器(T:事件参数数据类型)
        private EventHandler<T> m_DefaultHandler;


        /// 事件队列
        private readonly Queue<Event> m_EventQueue;

        /// 事件处理器多值字典, key:事件编号Id--Value:事件处理器
        private readonly FuMultiDictionary<string, EventHandler<T>> m_EventHandlerDict;

        /// 记录缓存的事件处理器字典, key:事件--Value:事件处理器链表节点
        private readonly Dictionary<object, LinkedListNode<EventHandler<T>>> m_CachedNodeDict;

        /// 记录需要移除的事件处理器字典, key:事件--Value:事件处理器链表节点
        private readonly Dictionary<object, LinkedListNode<EventHandler<T>>> m_WaitDelNodeDict;


        /// <summary>
        /// 初始化事件池的新实例。
        /// </summary>
        /// <param name="mode">事件池模式。</param>
        public EventPool(EventPoolMode mode)
        {
            m_EventPoolMode    = mode;
            m_DefaultHandler   = null;
            m_EventQueue       = new Queue<Event>();
            m_EventHandlerDict = new FuMultiDictionary<string, EventHandler<T>>();
            m_CachedNodeDict   = new Dictionary<object, LinkedListNode<EventHandler<T>>>();
            m_WaitDelNodeDict  = new Dictionary<object, LinkedListNode<EventHandler<T>>>();
        }

        /// <summary>
        /// 获取事件处理函数的数量。
        /// </summary>
        public int EventHandlerCount => m_EventHandlerDict.Count;

        /// <summary>
        /// 获取事件数量。
        /// </summary>
        // ReSharper disable once InconsistentlySynchronizedField
        public int EventCount => m_EventQueue?.Count ?? 0;

        /// <summary>
        /// 事件池轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            lock (m_EventQueue)
            {
                while (m_EventQueue.Count > 0)
                {
                    var tempEvent = m_EventQueue.Dequeue();
                    HandleEvent(tempEvent.Sender, tempEvent.EventArgs);
                    ReferencePool.Release(tempEvent);
                }
            }
        }

        /// <summary>
        /// 关闭并清理事件池。
        /// </summary>
        public void Shutdown()
        {
            Clear();
            m_EventHandlerDict.Clear();
            m_CachedNodeDict.Clear();
            m_WaitDelNodeDict.Clear();
            m_DefaultHandler = null;
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
        /// <param name="id">事件类型编号。</param>
        /// <returns>事件处理函数的数量。</returns>
        public int Count(string id)
        {
            return m_EventHandlerDict.TryGetValue(id, out var handlers) ? handlers.Count : 0;
        }

        /// <summary>
        /// 检查是否存在定事件对应的处理函数。
        /// </summary>
        /// <param name="id">事件类型编号。</param>
        /// <param name="handler">要检查的事件处理函数。</param>
        /// <returns>是否存在事件处理函数。</returns>
        public bool Check(string id, EventHandler<T> handler)
        {
            if (handler == null) throw new FuException("Event handler is invalid.");
            return m_EventHandlerDict.Contains(id, handler);
        }

        /// <summary>
        /// 订阅事件处理函数。
        /// </summary>
        /// <param name="id">事件类型编号。</param>
        /// <param name="handler">要订阅的事件处理函数。</param>
        public void Subscribe(string id, EventHandler<T> handler)
        {
            if (handler == null) throw new FuException("Event handler is invalid.");

            if (!m_EventHandlerDict.Contains(id))
            {
                m_EventHandlerDict.Add(id, handler);
                return;
            }

            if ((m_EventPoolMode & EventPoolMode.AllowMultiHandler) != EventPoolMode.AllowMultiHandler)
                throw new FuException(Utility.Text.Format("Event '{0}' not allow multi handler.", id));

            if ((m_EventPoolMode & EventPoolMode.AllowDuplicateHandler) != EventPoolMode.AllowDuplicateHandler && Check(id, handler))
                throw new FuException(Utility.Text.Format("Event '{0}' not allow duplicate handler.", id));

            m_EventHandlerDict.Add(id, handler);
        }

        /// <summary>
        /// 取消订阅事件处理函数。
        /// </summary>
        /// <param name="id">事件类型编号。</param>
        /// <param name="handler">要取消订阅的事件处理函数。</param>
        public void Unsubscribe(string id, EventHandler<T> handler)
        {
            if (handler == null) throw new FuException("Event handler is invalid.");

            if (m_CachedNodeDict.Count > 0)
            {
                foreach (var (eventObj, handlerNode) in m_CachedNodeDict)
                {
                    if (handlerNode == null || handlerNode.Value != handler) continue;
                    m_WaitDelNodeDict.Add(eventObj, handlerNode.Next);
                }

                if (m_WaitDelNodeDict.Count > 0)
                {
                    foreach (var (eventObj, linkedListNode) in m_WaitDelNodeDict)
                    {
                        m_CachedNodeDict[eventObj] = linkedListNode;
                    }

                    m_WaitDelNodeDict.Clear();
                }
            }

            if (!m_EventHandlerDict.Remove(id, handler))
                throw new FuException(Utility.Text.Format("Event '{0}' not exists specified handler.", id));
        }

        /// <summary>
        /// 设置默认事件处理函数。
        /// </summary>
        /// <param name="handler">要设置的默认事件处理函数。</param>
        public void SetDefaultHandler(EventHandler<T> handler) => m_DefaultHandler = handler;

        /// <summary>
        /// 抛出事件，这个操作是线程安全的，即使不在主线程中抛出，也可保证在主线程中回调事件处理函数，但事件会在抛出后的下一帧分发。
        /// </summary>
        /// <param name="sender">事件源。</param>
        /// <param name="eArgs">事件参数。</param>
        public void Fire(object sender, T eArgs)
        {
            if (eArgs == null) throw new FuException("Event is invalid.");

            var tempEvent = Event.Create(sender, eArgs);
            lock (m_EventQueue)
            {
                m_EventQueue.Enqueue(tempEvent);
            }
        }

        /// <summary>
        /// 抛出事件立即模式，这个操作不是线程安全的，事件会立刻分发。
        /// </summary>
        /// <param name="sender">事件源。</param>
        /// <param name="eArgs">事件参数。</param>
        public void FireNow(object sender, T eArgs)
        {
            if (eArgs == null) throw new FuException("Event is invalid.");
            HandleEvent(sender, eArgs);
        }

        /// <summary>
        /// 处理事件结点。
        /// </summary>
        /// <param name="sender">事件源。</param>
        /// <param name="eArgs">事件参数。</param>
        private void HandleEvent(object sender, T eArgs)
        {
            var noHandlerException = false;

            // 调用该事件Id下对应的所有处理函数
            if (m_EventHandlerDict.TryGetValue(eArgs.Id, out var handlers))
            {
                var curHandler = handlers.First;

                while (curHandler != null && curHandler != handlers.Terminal)
                {
                    m_CachedNodeDict[eArgs] = curHandler.Next != handlers.Terminal ? curHandler.Next : null;
                    curHandler.Value.Invoke(sender, eArgs);
                    curHandler = m_CachedNodeDict[eArgs];
                }

                m_CachedNodeDict.Remove(eArgs);
            }
            else if (m_DefaultHandler != null)
            {
                m_DefaultHandler.Invoke(sender, eArgs);
            }
            else if ((m_EventPoolMode & EventPoolMode.AllowNoHandler) == 0)
            {
                noHandlerException = true;
            }

            ReferencePool.Release(eArgs);

            if (noHandlerException)
                throw new FuException(Utility.Text.Format("Event '{0}' not allow no handler.", eArgs.Id));
        }
    }
}