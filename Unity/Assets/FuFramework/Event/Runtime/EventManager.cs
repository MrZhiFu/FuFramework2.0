using System;
using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Event.Runtime
{
    /// <summary>
    /// 事件管理器。
    /// </summary>
    public sealed class EventManager : FuComponent
    {
        /// <summary>
        /// 获取游戏框架模块优先级。
        /// </summary>
        /// <remarks>优先级较高的模块会优先轮询，并且关闭操作会后进行。</remarks>
        protected override int Priority => 7;

        /// <summary>
        /// 事件池。
        /// </summary>
        private EventPool<GameEventArgs> m_EventPool;

        /// <summary>
        /// 获取事件处理函数的数量。
        /// </summary>
        public int EventHandlerCount => m_EventPool.EventHandlerCount;

        /// <summary>
        /// 获取事件数量。
        /// </summary>
        public int EventCount => m_EventPool.EventCount;

        /// <summary>
        /// 初始化
        /// </summary>
        protected override void OnInit()
        {
            m_EventPool = new EventPool<GameEventArgs>(EEventPoolMode.AllowNoHandler | EEventPoolMode.AllowMultiHandler);
        }

        /// <summary>
        /// 游戏框架模块轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            m_EventPool.Update(elapseSeconds, realElapseSeconds);
        }

        /// <summary>
        /// 关闭并清理游戏框架模块。
        /// </summary>
        /// <param name="shutdownType"></param>
        protected override void OnShutdown(ShutdownType shutdownType)
        {
            m_EventPool.Shutdown();
        }

        /// <summary>
        /// 获取事件处理函数的数量。
        /// </summary>
        /// <param name="id">事件类型编号。</param>
        /// <returns>事件处理函数的数量。</returns>
        public int Count(string id) => m_EventPool.Count(id);

        /// <summary>
        /// 检查是否已存在指定事件对应的处理函数。
        /// </summary>
        /// <param name="id">事件类型编号。</param>
        /// <param name="handler">要检查的事件处理函数。</param>
        /// <returns>是否存在事件处理函数。</returns>
        public bool Check(string id, EventHandler<GameEventArgs> handler)
        {
            return m_EventPool.Check(id, handler);
        }

        /// <summary>
        /// 订阅事件处理函数。
        /// </summary>
        /// <param name="id">事件类型编号。</param>
        /// <param name="handler">要订阅的事件处理函数。</param>
        public void Subscribe(string id, EventHandler<GameEventArgs> handler)
        {
            if (Check(id, handler)) return;
            m_EventPool.Subscribe(id, handler);
        }

        /// <summary>
        /// 取消订阅事件处理函数。
        /// </summary>
        /// <param name="id">事件类型编号。</param>
        /// <param name="handler">要取消订阅的事件处理函数。</param>
        public void Unsubscribe(string id, EventHandler<GameEventArgs> handler)
        {
            if (!Check(id, handler)) return;
            m_EventPool.Unsubscribe(id, handler);
        }

        /// <summary>
        /// 设置默认事件处理函数。
        /// </summary>
        /// <param name="handler">要设置的默认事件处理函数。</param>
        public void SetDefaultHandler(EventHandler<GameEventArgs> handler) => m_EventPool.SetDefaultHandler(handler);

        /// <summary>
        /// 抛出事件，这个操作是线程安全的，即使不在主线程中抛出，也可保证在主线程中回调事件处理函数，但事件会在抛出后的下一帧分发。
        /// </summary>
        /// <param name="sender">事件源。</param>
        /// <param name="e">事件参数。</param>
        public void Fire(object sender, GameEventArgs e) => m_EventPool.Fire(sender, e);

        /// <summary>
        /// 使用事件编号抛出事件，取巧地使用一个空事件包装一个事件编号, 这样可以避免创建过多的无需事件数据的事件对象。
        /// 这个操作是线程安全的，即使不在主线程中抛出，也可保证在主线程中回调事件处理函数，但事件会在抛出后的下一帧分发。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="eventId">事件编号。</param>
        public void Fire(object sender, string eventId)
        {
            FuGuard.NotNullOrEmpty(eventId, nameof(eventId));
            Fire(sender, EmptyEventArgs.Create(eventId));
        }
        
        /// <summary>
        /// 抛出事件立即模式，这个操作不是线程安全的，事件会立刻分发。
        /// </summary>
        /// <param name="sender">事件源。</param>
        /// <param name="e">事件参数。</param>
        public void FireNow(object sender, GameEventArgs e) => m_EventPool.FireNow(sender, e);
    }
}