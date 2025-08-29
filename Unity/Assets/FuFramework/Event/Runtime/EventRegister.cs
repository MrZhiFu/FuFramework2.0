using System;
using FuFramework.Core.Runtime;
using ReferencePool = FuFramework.Core.Runtime.ReferencePool;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable once CheckNamespace
namespace FuFramework.Event.Runtime
{
    /// <summary>
    /// 事件注册器。
    /// 可用于单独管理属于自己模块的相关事件，如每个UI界面都可以单独管理自己的事件。
    /// </summary>
    public sealed class EventRegister : IReference
    {
        /// 事件管理器
        private static EventManager EventManager => ModuleManager.GetModule<EventManager>();

        /// <summary>
        /// 事件处理字典，key为消息ID，value为处理对象
        /// </summary>
        private readonly FuMultiDictionary<string, EventHandler<GameEventArgs>> m_EventHandlerDict = new();

        /// <summary>
        /// 创建事件订阅器
        /// </summary>
        /// <returns></returns>
        public static EventRegister Create()
        {
            return ReferencePool.Acquire<EventRegister>();
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
            m_EventHandlerDict.Add(id, handler);
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
            if (!m_EventHandlerDict.Remove(id, handler))
                throw new Exception(Utility.Text.Format("[EventRegister]事件订阅器中不存在指定消息ID '{0}' 的处理对象.", id));

            EventManager.Unsubscribe(id, handler);
        }

        /// <summary>
        /// 取消所有订阅
        /// </summary>
        public void UnSubscribeAll()
        {
            if (m_EventHandlerDict.Count == 0) return;

            foreach (var (id, eventHandlers) in m_EventHandlerDict)
            {
                foreach (var eventHandler in eventHandlers)
                {
                    EventManager.Unsubscribe(id, eventHandler);
                }
            }

            m_EventHandlerDict.Clear();
        }

        /// <summary>
        /// 触发事件，这个操作是线程安全的，即使不在主线程中抛出，也可保证在主线程中回调事件处理函数，但事件会在抛出后的下一帧分发。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="eventArgs">消息对象</param>
        public void Fire(object sender, GameEventArgs eventArgs) => EventManager.Fire(sender, eventArgs);

        /// <summary>
        /// 抛出事件，这个操作是线程安全的，即使不在主线程中抛出，也可保证在主线程中回调事件处理函数，但事件会在抛出后的下一帧分发。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="eventId">事件编号。</param>
        public void Fire(object sender, string eventId) => EventManager.Fire(sender, eventId);

        /// <summary>
        /// 抛出事件立即模式，这个操作不是线程安全的，事件会立刻分发。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="eventArgs">事件内容。</param>
        public void FireNow(object sender, GameEventArgs eventArgs) => EventManager.FireNow(sender, eventArgs);

        /// <summary>
        /// 清理
        /// </summary>
        public void Clear() => UnSubscribeAll();

        /// <summary>
        /// 将引用归还引用池-释放资源
        /// </summary>
        public void Release() => ReferencePool.Release(this);
    }
}