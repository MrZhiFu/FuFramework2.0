using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Event
{
    /// <summary>
    /// 空事件.
    /// 功能: 
    ///     1. 使用事件编号抛出事件，取巧地使用一个空事件包装一个事件编号, 这样可以避免创建过多的无需事件数据的事件对象.
    ///     2. 这样就可以支持使用枚举或字符串作为事件编号，并通过事件系统抛出。
    /// </summary>
    public sealed class EmptyEventArgs : GameEventArgs
    {
        public override string Id => m_EventId;

        /// <summary>
        /// 事件编号。
        /// 必须为实例字段：事件在下一帧才分发，静态字段会被同帧抛出的其它事件编号覆盖。
        /// </summary>
        private string m_EventId = typeof(EmptyEventArgs).FullName;

        /// <summary>
        /// 清理引用(归还引用池时调用)。
        /// </summary>
        public override void Clear()
        {
            m_EventId = typeof(EmptyEventArgs).FullName;
        }

        /// <summary>
        /// 创建空事件
        /// </summary>
        /// <param name="eventId">事件编号</param>
        /// <returns>空事件对象</returns>
        public static EmptyEventArgs Create(string eventId)
        {
            var eventArgs = ReferencePool.Acquire<EmptyEventArgs>();
            eventArgs.m_EventId = eventId;
            return eventArgs;
        }
    }
}