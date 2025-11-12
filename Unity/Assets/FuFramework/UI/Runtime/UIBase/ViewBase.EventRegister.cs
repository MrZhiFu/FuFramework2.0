using System;
using FuFramework.Core.Runtime;
using FuFramework.Event.Runtime;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace FuFramework.UI.Runtime
{
    /// <summary>
    /// 事件订阅器-界面的普通事件管理
    /// </summary>
    public abstract partial class ViewBase
    {
        /// <summary>
        /// 界面事件订阅器。
        /// </summary>
        private EventRegister EventRegister { get; set; }

        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <param name="eventId">事件ID</param>
        /// <param name="handler">事件处理回调</param>
        public void Subscribe(string eventId, EventHandler<GameEventArgs> handler)
        {
            FuGuard.NotNull(handler, nameof(handler));
            FuGuard.NotNullOrEmpty(eventId, nameof(eventId));
            FuGuard.NotNull(EventRegister, "事件订阅器为空, 请先初始化EventRegister.");
            EventRegister.Subscribe(eventId, handler);
        }

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        /// <param name="eventId">事件ID</param>
        /// <param name="handler">事件处理回调</param>
        public void UnSubscribe(string eventId, EventHandler<GameEventArgs> handler)
        {
            FuGuard.NotNull(handler, nameof(handler));
            FuGuard.NotNullOrEmpty(eventId, nameof(eventId));
            FuGuard.NotNull(EventRegister, "事件订阅器为空, 请先初始化EventRegister.");
            EventRegister.UnSubscribe(eventId, handler);
        }

        /// <summary>
        /// 抛出事件，这个操作是线程安全的，即使不在主线程中抛出，也可保证在主线程中回调事件处理函数，但事件会在抛出后的下一帧分发。
        /// </summary>
        /// <param name="sender">事件ID</param>
        /// <param name="eventArgs">事件对象</param>
        public void Broadcast(object sender, GameEventArgs eventArgs)
        {
            FuGuard.NotNull(sender, nameof(sender));
            FuGuard.NotNull(eventArgs, nameof(eventArgs));
            FuGuard.NotNull(EventRegister, "事件订阅器为空, 请先初始化EventRegister.");
            EventRegister.Broadcast(sender, eventArgs);
        }

        /// <summary>
        /// 抛出事件，这个操作是线程安全的，即使不在主线程中抛出，也可保证在主线程中回调事件处理函数，但事件会在抛出后的下一帧分发。
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="eventId">事件ID</param>
        public void Broadcast(object sender, string eventId)
        {
            FuGuard.NotNull(sender, nameof(sender));
            FuGuard.NotNullOrEmpty(eventId, nameof(eventId));
            FuGuard.NotNull(EventRegister, "事件订阅器为空, 请先初始化EventRegister.");
            EventRegister.Broadcast(sender, eventId);
        }

        /// <summary>
        /// 立即抛出事件，这个操作不是线程安全的，事件会立刻分发。
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="eventArgs">事件对象</param>
        public void BroadcastNow(object sender, GameEventArgs eventArgs)
        {
            FuGuard.NotNull(sender,        nameof(sender));
            FuGuard.NotNull(eventArgs,     nameof(eventArgs));
            FuGuard.NotNull(EventRegister, "事件订阅器为空, 请先初始化EventRegister.");
            EventRegister.BroadcastNow(sender, eventArgs);
        }

        /// <summary>
        /// 取消所有订阅
        /// </summary>
        public void UnSubscribeAll()
        {
            FuGuard.NotNull(EventRegister, "事件订阅器为空, 请先初始化EventRegister.");
            EventRegister.UnSubscribeAll();
        }

        /// <summary>
        /// 释放事件注册器
        /// </summary>
        private void ReleaseEventRegister()
        {
            EventRegister.Release();
            EventRegister = null;
        }
    }
}