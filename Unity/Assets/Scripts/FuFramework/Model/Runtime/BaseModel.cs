using System;
using FuFramework.Event.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Model.Runtime
{
    /// <summary>
    /// Model基类。
    /// 功能：
    ///     1. 提供Model事件的注册，广播功能。
    ///     2. 定义Model生命周期的接口。
    /// </summary>
    public abstract class BaseModel
    {
        /// <summary>
        /// 事件订阅器。
        /// </summary>
        private EventRegister EventRegister { get; set; }

        /// <summary>
        /// 初始化
        /// </summary>
        internal void Init()
        {
            EventRegister = EventRegister.Create();
            OnInitData();
            RegisterEvents();
        }

        /// <summary>
        /// 释放
        /// </summary>
        internal void Dispose()
        {
            OnDispose();
            EventRegister?.Release();
            EventRegister = null;
        }


        /// <summary>
        /// 初始化Model数据
        /// </summary>
        protected virtual void OnInitData() { }

        /// <summary>
        /// 释放Model资源。
        /// </summary>
        protected virtual void OnDispose() { }

        /// <summary>
        /// 注册Model事件。
        /// </summary>
        protected virtual void RegisterEvents() { }


        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <param name="eventId">事件Id</param>
        /// <param name="handler">事件处理方法</param>
        protected void Subscribe(string eventId, EventHandler<GameEventArgs> handler) => EventRegister?.Subscribe(eventId, handler);

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        /// <param name="eventId">事件Id</param>
        /// <param name="handler">事件处理方法</param>
        protected void UnSubscribe(string eventId, EventHandler<GameEventArgs> handler) => EventRegister?.UnSubscribe(eventId, handler);

        /// <summary>
        /// 抛出事件，这个操作是线程安全的，即使不在主线程中抛出，也可保证在主线程中回调事件处理函数，但事件会在抛出后的下一帧分发。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="eventArgs">消息对象</param>
        protected void Broadcast(object sender, GameEventArgs eventArgs) => EventRegister?.Broadcast(sender, eventArgs);

        /// <summary>
        /// 抛出事件，这个操作是线程安全的，即使不在主线程中抛出，也可保证在主线程中回调事件处理函数，但事件会在抛出后的下一帧分发。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="eventId">事件编号。</param>
        public void Broadcast(object sender, string eventId) => EventRegister?.Broadcast(sender, eventId);

        /// <summary>
        /// 立即抛出事件，这个操作不是线程安全的，事件会立刻分发。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="eventArgs">事件内容。</param>
        public void BroadcastNow(object sender, GameEventArgs eventArgs) => EventRegister?.BroadcastNow(sender, eventArgs);
    }
}