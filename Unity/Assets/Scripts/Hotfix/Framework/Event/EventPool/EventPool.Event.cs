using System;
using Hotfix.Framework.ReferencePool;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Event
{
    public sealed partial class EventPool<T> where T : BaseEventArgs
    {
        /// <summary>
        /// 事件定义。
        /// 功能：
        ///     1. 包装事件发送者和事件参数。
        /// </summary>
        private sealed class Event : IReference
        {
            /// <summary>
            /// 发送者
            /// </summary>
            public object Sender { get; private set; }

            /// <summary>
            /// 事件参数
            /// </summary>
            public T EventArgs { get; private set; }

            /// <summary>
            /// 创建事件节点
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="eventArgs"></param>
            /// <returns></returns>
            public static Event Create(object sender, T eventArgs)
            {
                var eventNodeNode = GlobalModule.ReferencePoolModule.Acquire<Event>();
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
