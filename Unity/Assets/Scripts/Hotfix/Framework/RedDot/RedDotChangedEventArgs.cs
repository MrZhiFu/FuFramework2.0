using System.Collections.Generic;
using Hotfix.Framework.Event;
using Hotfix.Game.Config;

namespace Hotfix.Framework.RedDot
{
    /// <summary>
    /// 红点变更事件参数（每帧批量广播）
    /// UI 端订阅 EventModule 的此事件，按 ChangedStaticKeys / ChangedDynamicKeys 过滤刷新
    /// </summary>
    public sealed class RedDotChangedEventArgs : GameEventArgs
    {
        /// <summary>
        /// 事件 ID 常量（用于 Subscribe/Unsubscribe）
        /// </summary>
        public const string EventId = "Hotfix.Framework.RedDot.RedDotChanged";

        public override string Id => EventId;

        /// <summary>
        /// 本帧发生变化的静态节点 Key 列表
        /// </summary>
        public readonly List<ERedDotKey> ChangedStaticKeys = new();

        /// <summary>
        /// 本帧发生变化的动态节点 Key 列表
        /// </summary>
        public readonly List<string> ChangedDynamicKeys = new();

        public override void Clear()
        {
            ChangedStaticKeys.Clear();
            ChangedDynamicKeys.Clear();
        }

        public static RedDotChangedEventArgs Create()
        {
            return new RedDotChangedEventArgs();
        }
    }
}
