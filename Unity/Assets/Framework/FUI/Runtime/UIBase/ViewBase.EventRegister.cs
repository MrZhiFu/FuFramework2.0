using System;
using GameFrameX.Event.Runtime;
using GameFrameX.Runtime;

namespace GameFrameX.UI.Runtime
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
        /// <param name="id">消息ID</param>
        /// <param name="handler">处理对象</param>
        public void Subscribe(string id, EventHandler<GameEventArgs> handler)
        {
            GameFrameworkGuard.NotNull(handler, nameof(handler));
            GameFrameworkGuard.NotNullOrEmpty(id, nameof(id));
            GameFrameworkGuard.NotNull(EventRegister, "事件订阅器为空, 请先初始化EventRegister.");
            EventRegister.Subscribe(id, handler);
        }

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        /// <param name="id">消息ID</param>
        /// <param name="handler">处理对象</param>
        public void UnSubscribe(string id, EventHandler<GameEventArgs> handler)
        {
            GameFrameworkGuard.NotNull(handler, nameof(handler));
            GameFrameworkGuard.NotNullOrEmpty(id, nameof(id));
            GameFrameworkGuard.NotNull(EventRegister, "事件订阅器为空, 请先初始化EventRegister.");
            EventRegister.UnSubscribe(id, handler);
        }

        /// <summary>
        /// 触发事件，这个操作是线程安全的，即使不在主线程中抛出，也可保证在主线程中回调事件处理函数，但事件会在抛出后的下一帧分发。
        /// </summary>
        /// <param name="id">消息ID</param>
        /// <param name="e">消息对象</param>
        public void Fire(string id, GameEventArgs e)
        {
            GameFrameworkGuard.NotNull(e, nameof(e));
            GameFrameworkGuard.NotNullOrEmpty(id, nameof(id));
            GameFrameworkGuard.NotNull(EventRegister, "事件订阅器为空, 请先初始化EventRegister.");
            EventRegister.Fire(id, e);
        }

        /// <summary>
        /// 抛出事件，这个操作是线程安全的，即使不在主线程中抛出，也可保证在主线程中回调事件处理函数，但事件会在抛出后的下一帧分发。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="eventId">事件编号。</param>
        public void Fire(object sender, string eventId)
        {
            GameFrameworkGuard.NotNull(sender, nameof(sender));
            GameFrameworkGuard.NotNullOrEmpty(eventId, nameof(eventId));
            GameFrameworkGuard.NotNull(EventRegister, "事件订阅器为空, 请先初始化EventRegister.");
            EventRegister.Fire(sender, eventId);
        }

        /// <summary>
        /// 抛出事件立即模式，这个操作不是线程安全的，事件会立刻分发。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="e">事件内容。</param>
        public void FireNow(object sender, GameEventArgs e)
        {
            GameFrameworkGuard.NotNull(sender, nameof(sender));
            GameFrameworkGuard.NotNull(e, nameof(e));
            GameFrameworkGuard.NotNull(EventRegister, "事件订阅器为空, 请先初始化EventRegister.");
            EventRegister.FireNow(sender, e);
        }

        /// <summary>
        /// 取消所有订阅
        /// </summary>
        public void UnSubscribeAll()
        {
            GameFrameworkGuard.NotNull(EventRegister, "事件订阅器为空, 请先初始化EventRegister.");
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