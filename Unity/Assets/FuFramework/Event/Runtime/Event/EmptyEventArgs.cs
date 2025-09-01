
// ReSharper disable once CheckNamespace
namespace FuFramework.Event.Runtime
{
    /// <summary>
    /// 空事件.
    /// 功能: 使用事件编号抛出事件，取巧地使用一个空事件包装一个事件编号, 这样可以避免创建过多的无需事件数据的事件对象.
    /// 这样就可以使用枚举或字符串作为事件编号，并通过事件系统抛出。
    /// </summary>
    public sealed class EmptyEventArgs : GameEventArgs
    {
        public override string Id => m_EventId;
        private static  string m_EventId = typeof(EmptyEventArgs).FullName;

        public override void Clear() { }

        /// <summary>
        /// 创建空事件
        /// </summary>
        /// <param name="eventId">事件编号</param>
        /// <returns>空事件对象</returns>
        public static EmptyEventArgs Create(string eventId)
        {
            var eventArgs = ReferencePool.Runtime.ReferencePool.Acquire<EmptyEventArgs>();
            m_EventId = eventId;
            return eventArgs;
        }
    }
}