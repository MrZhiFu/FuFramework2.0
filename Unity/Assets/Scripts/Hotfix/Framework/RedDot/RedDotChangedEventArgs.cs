using System.Collections.Generic;
using Hotfix.Framework.Event;
using Hotfix.Framework.ReferencePools;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.RedDot
{
    /// <summary>
    /// 红点变更事件参数（每帧批量广播）
    /// UI 端订阅 EventModule 的此事件，按 ChangedKeys 过滤刷新
    /// </summary>
    public sealed class RedDotChangedEventArgs : GameEventArgs
    {
        /// <summary>
        /// 获取事件编号        /// </summary>
        public override string Id => EventId;

        /// <summary>
        /// 事件 ID 常量（用于 Subscribe/Unsubscribe）        /// </summary>
        public static readonly string EventId = typeof(RedDotChangedEventArgs).FullName;

        /// <summary>
        /// 本帧发生变化的红点节点 Key 列表        /// </summary>
        public readonly List<RedDotKey> ChangedKeys = new();

        /// <summary>
        /// 清空事件参数数据，用于重用        /// </summary>
        public override void Clear() => ChangedKeys.Clear();

        /// <summary>
        /// 创建事件参数实例        /// </summary>
        /// <returns>创建的事件参数实例</returns>
        public static RedDotChangedEventArgs Create()
        {
            var redDotChangedEventArgs = ReferencePool.Acquire<RedDotChangedEventArgs>();
            return redDotChangedEventArgs;
        }
    }
}
