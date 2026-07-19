using System;
using Hotfix.Framework.Core;
using Hotfix.Framework.Event;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.Framework.UI
{
    /// <summary>
    /// 界面基类分部类之一。
    /// 目标：提供一个事件订阅器，用于管理界面的业务逻辑事件。
    /// 功能：
    ///     1. 订阅事件。
    ///     2. 取消订阅事件。
    ///     3. 分发事件。
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
            handler.NotNull(nameof(handler));
            eventId.NotNullOrEmpty(nameof(eventId));
            EventRegister.NotNull("事件订阅器");
            EventRegister.Subscribe(eventId, handler);
        }

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        /// <param name="eventId">事件ID</param>
        /// <param name="handler">事件处理回调</param>
        public void UnSubscribe(string eventId, EventHandler<GameEventArgs> handler)
        {
            handler.NotNull(nameof(handler));
            eventId.NotNullOrEmpty(nameof(eventId));
            EventRegister.NotNull("事件订阅器");
            EventRegister.UnSubscribe(eventId, handler);
        }

        /// <summary>
        /// 抛出事件，这个操作是线程安全的，即使不在主线程中抛出，也可保证在主线程中回调事件处理函数，但事件会在抛出后的下一帧分发。
        /// </summary>
        /// <param name="sender">事件ID</param>
        /// <param name="eventArgs">事件对象</param>
        public void Broadcast(object sender, GameEventArgs eventArgs)
        {
            sender.NotNull(       nameof(sender));
            eventArgs.NotNull(    nameof(eventArgs));
            EventRegister.NotNull("事件订阅器");
            EventRegister.Broadcast(sender, eventArgs);
        }

        /// <summary>
        /// 抛出事件，这个操作是线程安全的，即使不在主线程中抛出，也可保证在主线程中回调事件处理函数，但事件会在抛出后的下一帧分发。
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="eventId">事件ID</param>
        public void Broadcast(object sender, string eventId)
        {
            sender.NotNull(nameof(sender));
            eventId.NotNullOrEmpty(nameof(eventId));
            EventRegister.NotNull("事件订阅器");
            EventRegister.Broadcast(sender, eventId);
        }

        /// <summary>
        /// 立即抛出事件，这个操作不是线程安全的，事件会立刻分发。
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="eventArgs">事件对象</param>
        public void BroadcastNow(object sender, GameEventArgs eventArgs)
        {
            sender.NotNull(       nameof(sender));
            eventArgs.NotNull(    nameof(eventArgs));
            EventRegister.NotNull("事件订阅器");
            EventRegister.BroadcastNow(sender, eventArgs);
        }

        /// <summary>
        /// 取消所有订阅
        /// </summary>
        public void UnSubscribeAll()
        {
            EventRegister.NotNull("事件订阅器");
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
