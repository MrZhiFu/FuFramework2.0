using FuFramework.Event.Runtime;
using ReferencePool = FuFramework.Core.Runtime.ReferencePool;

// ReSharper disable once CheckNamespace
namespace FuFramework.Mono.Runtime
{
    /// <summary>
    /// 程序暂停状态变化事件。
    /// </summary>
    public sealed class OnApplicationPauseChangedEventArgs : GameEventArgs
    {
        /// <summary>
        /// 获取程序暂停状态变化事件编号。
        /// </summary>
        public override string Id => EventId;

        /// <summary>
        /// 程序是否是暂停状态变化更新事件编号。
        /// </summary>
        public static readonly string EventId = typeof(OnApplicationPauseChangedEventArgs).FullName;

        /// <summary>
        /// 是否暂停。
        /// </summary>
        public bool IsPause { get; private set; }

        /// <summary>
        /// 初始化程序暂停状态变化事件的新实例。
        /// </summary>
        public OnApplicationPauseChangedEventArgs()
        {
            IsPause = false;
        }
        
        /// <summary>
        /// 创建程序暂停状态变化事件。
        /// </summary>
        /// <param name="isPause">是否是前台</param>
        /// <returns>创建的程序暂停状态变化事件。</returns>
        public static OnApplicationPauseChangedEventArgs Create(bool isPause)
        {
            var loadDictionaryUpdateEventArgs = ReferencePool.Acquire<OnApplicationPauseChangedEventArgs>();
            loadDictionaryUpdateEventArgs.IsPause = isPause;
            return loadDictionaryUpdateEventArgs;
        }

        /// <summary>
        /// 清理前后台切换事件。
        /// </summary>
        public override void Clear()
        {
            IsPause = false;
        }
    }
}