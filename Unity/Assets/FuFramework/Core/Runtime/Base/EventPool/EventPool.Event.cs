// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    public sealed partial class EventPool<T> where T : BaseEventArgs
    {
        /// <summary>
        /// 事件定义
        /// </summary>
        private sealed class Event : IReference
        {
            /// <summary>
            /// 发送者
            /// </summary>
            public object Sender { get; private set; } = null;

            /// <summary>
            /// 事件参数
            /// </summary>
            public T EventArgs { get; private set; } = null;

            /// <summary>
            /// 创建事件节点
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="eventArgs"></param>
            /// <returns></returns>
            public static Event Create(object sender, T eventArgs)
            {
                var eventNodeNode = ReferencePool.Acquire<Event>();
                eventNodeNode.Sender    = sender;
                eventNodeNode.EventArgs = eventArgs;
                return eventNodeNode;
            }

            /// <summary>
            /// 释放事件节点
            /// </summary>
            public void Clear()
            {
                Sender    = null;
                EventArgs = null;
            }
        }
    }
}