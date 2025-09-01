using FuFramework.Event.Runtime;
using FuFramework.ReferencePool.Runtime;

namespace Hotfix.Events
{
    /// <summary>
    /// 背包数量变化或者道具变化事件
    /// </summary>
    public sealed class BagChangedEventArgs : GameEventArgs
    {
        public override        string Id => EventId;
        public static readonly string EventId = typeof(BagChangedEventArgs).FullName;


        public override void Clear() { }

        /// <summary>
        /// 创建背包数量变化或者道具变化事件
        /// </summary>
        /// <returns></returns>
        public static BagChangedEventArgs Create()
        {
            var eventArgs = ReferencePool.Acquire<BagChangedEventArgs>();
            return eventArgs;
        }
    }
}