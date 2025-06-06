//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using UnityEngine.Scripting; // 确保引入命名空间

namespace GameFrameX.Runtime
{
    public sealed partial class EventPool<T> where T : BaseEventArgs
    {
        /// <summary>
        /// 事件结点。
        /// </summary>
        private sealed class EventNode : IReference
        {
            /// <summary>
            /// 发送者
            /// </summary>
            [Preserve]
            public object Sender { get; private set; } = null;

            /// <summary>
            /// 事件参数
            /// </summary>
            [Preserve]
            public T EventArgs { get; private set; } = null;

            /// <summary>
            /// 创建事件节点
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="eventArgs"></param>
            /// <returns></returns>
            [Preserve]
            public static EventNode Create(object sender, T eventArgs)
            {
                var eventNodeNode = ReferencePool.Acquire<EventNode>();
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