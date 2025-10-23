using System;
using FuFramework.Event.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Model.Runtime
{
    /// <summary>
    /// 所有Model基类
    /// 1.提供该Model事件的注册，广播
    /// 2.提供一些初始化接口
    /// </summary>
    public abstract class BaseModel
    {
        /// 事件处理器注册器
        private readonly EventRegister m_EventRegister = new();

        /// <summary>
        /// 初始化
        /// </summary>
        public void Init()
        {
            InitData();
            RegisterEvents();
        }

        /// <summary>
        /// 被删除时调用(游戏退出时)
        /// </summary>
        public virtual void OnDispose()
        {
            UnRegisterEvents();
        }

        /// <summary>
        /// 用于初始化Model数据
        /// </summary>
        protected virtual void InitData() { }

        /// <summary>
        /// 在此方法中注册所有事件
        /// </summary>
        protected virtual void RegisterEvents() { }

        /// <summary>
        /// 反注册(清理)所有注册的事件
        /// </summary>
        private void UnRegisterEvents()
        {
            m_EventRegister.Clear();
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <param name="eventId">事件Id</param>
        /// <param name="handler">事件处理方法</param>
        public void Subscribe(string eventId, EventHandler<GameEventArgs> handler)
        {
            m_EventRegister.Subscribe(eventId, handler);
        }

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        /// <param name="eventId">事件Id</param>
        /// <param name="handler">事件处理方法</param>
        protected void UnSubscribe(string eventId, EventHandler<GameEventArgs> handler)
        {
            m_EventRegister.UnSubscribe(eventId, handler);
        }

        /// <summary>
        /// 广播事件
        /// </summary>
        /// <param name="sender">发送者对象</param>
        /// <param name="eArgs">事件参数</param>
        protected void Fire(object sender, GameEventArgs eArgs)
        {
            m_EventRegister.Fire(sender, eArgs);
        }
    }
}