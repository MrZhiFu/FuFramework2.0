using FuFramework.Event.Runtime;
using ReferencePool = FuFramework.Core.Runtime.ReferencePool;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable once CheckNamespace
namespace FuFramework.Sound.Runtime
{
    /// <summary>
    /// 重置声音代理事件。
    /// </summary>
    public sealed class ResetSoundAgentEventArgs : GameEventArgs
    {
        /// <summary>
        /// 事件编号。
        /// </summary>
        public override string Id => EventId;

        /// <summary>
        /// 播放声音更新事件编号。
        /// </summary>
        public static readonly string EventId = typeof(ResetSoundAgentEventArgs).FullName;

        /// <summary>
        /// 初始化重置声音代理事件的新实例。
        /// </summary>
        public ResetSoundAgentEventArgs() { }

        /// <summary>
        /// 创建重置声音代理事件。
        /// </summary>
        /// <returns>创建的重置声音代理事件。</returns>
        public static ResetSoundAgentEventArgs Create()
        {
            var resetSoundAgentEventArgs = ReferencePool.Acquire<ResetSoundAgentEventArgs>();
            return resetSoundAgentEventArgs;
        }

        /// <summary>
        /// 清理重置声音代理事件。
        /// </summary>
        public override void Clear() { }
    }
}