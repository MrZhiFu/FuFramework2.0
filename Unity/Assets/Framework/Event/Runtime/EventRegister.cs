using System;
using GameFrameX.Event.Runtime;
using GameFrameX.Runtime;

namespace GameFrameX.UI.Runtime
{
    /// <summary>
    /// 事件注册器。
    /// 可用于单独管理属于自己模块的相关事件，如每个UI界面都可以单独管理自己的事件。
    /// </summary>
    public sealed class EventRegister : IReference
    {
        /// <summary>
        /// 持有者
        /// </summary>
        public object Owner { get; private set; }

        /// 事件管理器
        private static EventComponent EventManager => GameEntry.GetComponent<EventComponent>();

        /// <summary>
        /// 事件处理字典，key为消息ID，value为处理对象
        /// </summary>
        private readonly GameFrameworkMultiDictionary<string, EventHandler<GameEventArgs>> m_DicEventHandlerDict = new();

        /// <summary>
        /// 创建事件订阅器
        /// </summary>
        /// <param name="owner">持有者</param>
        /// <returns></returns>
        public static EventRegister Create(object owner)
        {
            var eventSubscriber = ReferencePool.Acquire<EventRegister>();
            eventSubscriber.Owner = owner;
            return eventSubscriber;
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <param name="id">消息ID</param>
        /// <param name="handler">处理对象</param>
        /// <exception cref="Exception"></exception>
        public void Subscribe(string id, EventHandler<GameEventArgs> handler)
        {
            if (handler == null) throw new Exception("[EventRegister]事件处理对象不能为空.");

            m_DicEventHandlerDict.Add(id, handler);
            EventManager.Subscribe(id, handler);
        }

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        /// <param name="id">消息ID</param>
        /// <param name="handler">处理对象</param>
        /// <exception cref="Exception"></exception>
        public void UnSubscribe(string id, EventHandler<GameEventArgs> handler)
        {
            if (!m_DicEventHandlerDict.Remove(id, handler))
                throw new Exception(Utility.Text.Format("[EventRegister]事件订阅器中不存在指定消息ID '{0}' 的处理对象.", id));

            EventManager.Unsubscribe(id, handler);
        }

        /// <summary>
        /// 触发事件，这个操作是线程安全的，即使不在主线程中抛出，也可保证在主线程中回调事件处理函数，但事件会在抛出后的下一帧分发。
        /// </summary>
        /// <param name="id">消息ID</param>
        /// <param name="e">消息对象</param>
        public void Fire(string id, GameEventArgs e)
        {
            if (!m_DicEventHandlerDict.Contains(id)) return;
            EventManager.Fire(this, e);
        }

        /// <summary>
        /// 抛出事件，这个操作是线程安全的，即使不在主线程中抛出，也可保证在主线程中回调事件处理函数，但事件会在抛出后的下一帧分发。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="eventId">事件编号。</param>
        public void Fire(object sender, string eventId)
        {
            EventManager.Fire(sender, EmptyEventArgs.Create(eventId));
        }

        /// <summary>
        /// 抛出事件立即模式，这个操作不是线程安全的，事件会立刻分发。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="e">事件内容。</param>
        public void FireNow(object sender, GameEventArgs e)
        {
            EventManager.FireNow(sender, e);
        }

        /// <summary>
        /// 取消所有订阅
        /// </summary>
        public void UnSubscribeAll()
        {
            if (m_DicEventHandlerDict.Count == 0) return;

            foreach (var (id, eventHandlers) in m_DicEventHandlerDict)
            {
                foreach (var eventHandler in eventHandlers)
                {
                    EventManager.Unsubscribe(id, eventHandler);
                }
            }

            m_DicEventHandlerDict.Clear();
        }

        /// <summary>
        /// 清理
        /// </summary>
        public void Clear()
        {
            UnSubscribeAll();
            Owner = null;
        }

        /// <summary>
        /// 将引用归还引用池-释放资源
        /// </summary>
        public void Release() => ReferencePool.Release(this);
    }
}