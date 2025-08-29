using System;
using FuFramework.Core.Runtime;
using UnityEngine;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable once CheckNamespace
namespace FuFramework.Event.Runtime
{
    /// <summary>
    /// 事件组件。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/Event")]
    public sealed class EventComponent : FuComponent
    {
        /// <summary>
        /// 事件管理器。
        /// </summary>
        private IEventManager m_EventManager;

        /// <summary>
        /// 获取事件处理函数的数量。
        /// </summary>
        public int EventHandlerCount => m_EventManager.EventHandlerCount;

        /// <summary>
        /// 获取事件数量。
        /// </summary>
        public int EventCount => m_EventManager.EventCount;

        protected override void OnInit()
        {
            m_EventManager = FuEntry.GetModule<IEventManager>();
            if (m_EventManager == null) Log.Fatal("事件管理器不存在.");
        }

        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds) { }
        protected override void OnShutdown(ShutdownType shutdownType) { }

        /// <summary>
        /// 获取事件处理函数的数量。
        /// </summary>
        /// <param name="id">事件类型编号。</param>
        /// <returns>事件处理函数的数量。</returns>
        public int Count(string id) => m_EventManager.Count(id);

        /// <summary>
        /// 检查是否存在事件处理函数。
        /// </summary>
        /// <param name="id">事件类型编号。</param>
        /// <param name="handler">要检查的事件处理函数。</param>
        /// <returns>是否存在事件处理函数。</returns>
        public bool Check(string id, EventHandler<GameEventArgs> handler) => m_EventManager.Check(id, handler);

        /// <summary>
        /// 检查订阅事件处理回调函数。当不存在时自动订阅
        /// </summary>
        /// <param name="id">事件类型编号。</param>
        /// <param name="handler">要订阅的事件处理回调函数。</param>
        public void Subscribe(string id, EventHandler<GameEventArgs> handler)
        {
            if (Check(id, handler)) return;
            m_EventManager.Subscribe(id, handler);
        }

        /// <summary>
        /// 取消订阅事件处理回调函数。
        /// </summary>
        /// <param name="id">事件类型编号。</param>
        /// <param name="handler">要取消订阅的事件处理回调函数。</param>
        public void Unsubscribe(string id, EventHandler<GameEventArgs> handler)
        {
            if (!Check(id, handler)) return;
            m_EventManager.Unsubscribe(id, handler);
        }

        /// <summary>
        /// 设置默认事件处理函数。
        /// </summary>
        /// <param name="handler">要设置的默认事件处理函数。</param>
        public void SetDefaultHandler(EventHandler<GameEventArgs> handler) => m_EventManager.SetDefaultHandler(handler);

        /// <summary>
        /// 抛出事件，这个操作是线程安全的，即使不在主线程中抛出，也可保证在主线程中回调事件处理函数，但事件会在抛出后的下一帧分发。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="e">事件内容。</param>
        public void Fire(object sender, GameEventArgs e) => m_EventManager.Fire(sender, e);

        /// <summary>
        /// 使用事件编号抛出事件，取巧地使用一个空事件包装一个事件编号, 这样可以避免创建过多的无需事件数据的事件对象。
        /// 这个操作是线程安全的，即使不在主线程中抛出，也可保证在主线程中回调事件处理函数，但事件会在抛出后的下一帧分发。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="eventId">事件编号。</param>
        public void Fire(object sender, string eventId)
        {
            FuGuard.NotNullOrEmpty(eventId, nameof(eventId));
            m_EventManager.Fire(sender, EmptyEventArgs.Create(eventId));
        }

        /// <summary>
        /// 抛出事件立即模式，这个操作不是线程安全的，事件会立刻分发。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="eventArgs">事件内容。</param>
        public void FireNow(object sender, GameEventArgs eventArgs) => m_EventManager.FireNow(sender, eventArgs);
    }
}