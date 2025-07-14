using System;
using GameFrameX.Event.Runtime;

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
        protected void Subscribe(string id, EventHandler<GameEventArgs> handler) => EventRegister.Subscribe(id, handler);

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        /// <param name="id">消息ID</param>
        /// <param name="handler">处理对象</param>
        protected void UnSubscribe(string id, EventHandler<GameEventArgs> handler) => EventRegister.UnSubscribe(id, handler);

        /// <summary>
        /// 设置FUI某个可响应UI上的监听事件(会删除以前添加的事件)
        /// </summary>
        /// <param name="id">消息ID</param>
        /// <param name="e">消息对象</param>
        protected void Fire(string id, GameEventArgs e) => EventRegister.Fire(id, e);

        /// <summary>
        /// 取消所有订阅
        /// </summary>
        protected void UnSubscribeAll() => EventRegister.UnSubscribeAll();
    }
}