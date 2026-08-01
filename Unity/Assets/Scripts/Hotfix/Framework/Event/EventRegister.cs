using System;
using Hotfix.Framework.Core;
using Hotfix.Framework.ReferencePools;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Event
{
    /// <summary>
    /// 事件注册器。
    /// 功能：
    ///     1. 订阅事件。
    ///     2. 取消订阅事件。
    ///     3. 派发事件。
    /// </summary>
    public sealed class EventRegister : IReference
    {
        /// 事件管理模块
        private static EventModule m_EventModule;

        /// <summary>
        /// 事件处理多值字典，key为事件ID，value为事件处理对象列表。
        /// </summary>
        private readonly FuMultiDictionary<string, EventHandler<GameEventArgs>> m_EventHandlerDict = new();

        /// <summary>
        /// 创建事件订阅器
        /// </summary>
        /// <returns></returns>
        public static EventRegister Create()
        {
            m_EventModule = ModuleManager.GetModule<EventModule>();
            return GlobalModule.ReferencePoolModule.Acquire<EventRegister>();
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
            m_EventModule.Subscribe(id, handler);
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
                throw new Exception($"[EventRegister]事件订阅器中不存在指定消息ID '{id}' 的处理对象.");

            m_EventModule.Unsubscribe(id, handler);
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
                    m_EventModule.Unsubscribe(id, eventHandler);
                }
            }

            m_EventHandlerDict.Clear();
        }

        /// <summary>
        /// 派发事件(事件会在抛出后的下一帧分发)。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="eventArgs">消息对象</param>
        public void Broadcast(object sender, GameEventArgs eventArgs) => m_EventModule.Broadcast(sender, eventArgs);

        /// <summary>
        /// 派发事件(事件会在抛出后的下一帧分发)。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="eventId">事件编号。</param>
        public void Broadcast(object sender, string eventId) => m_EventModule.Broadcast(sender, eventId);

        /// <summary>
        /// 立即抛出事件(事件会立刻分发)。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="eventArgs">事件内容。</param>
        public void BroadcastNow(object sender, GameEventArgs eventArgs) => m_EventModule.BroadcastNow(sender, eventArgs);

        /// <summary>
        /// 清理
        /// </summary>
        public void Clear()
        {
            UnSubscribeAll();
            m_EventModule = null;
        }

        /// <summary>
        /// 将引用归还引用池-释放资源
        /// </summary>
        public void Release() => GlobalModule.ReferencePoolModule.Release(this);
    }
}
