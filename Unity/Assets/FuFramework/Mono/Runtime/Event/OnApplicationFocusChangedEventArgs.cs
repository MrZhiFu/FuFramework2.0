using FuFramework.Event.Runtime;
using ReferencePool = FuFramework.Core.Runtime.ReferencePool;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable once CheckNamespace
namespace FuFramework.Mono.Runtime
{
    /// <summary>
    /// 程序前后台切换事件。
    /// </summary>
    public sealed class OnApplicationFocusChangedEventArgs : GameEventArgs
    {
        /// <summary>
        /// 程序前后台切换事件编号。
        /// </summary>
        public override string Id => EventId;

        /// <summary>
        /// 程序前后台切换事件编号。
        /// </summary>
        public static readonly string EventId = typeof(OnApplicationFocusChangedEventArgs).FullName;

        /// <summary>
        /// 是否是前台。
        /// </summary>
        public bool IsFocus { get; private set; }

        /// <summary>
        /// 初始化程序前后台切换事件的新实例。
        /// </summary>
        public OnApplicationFocusChangedEventArgs()
        {
            IsFocus = false;
        }

        /// <summary>
        /// 程序前后台切换事件。
        /// </summary>
        /// <param name="isFocus">是否是前台</param>
        /// <returns>程序前后台切换事件。</returns>
        public static OnApplicationFocusChangedEventArgs Create(bool isFocus)
        {
            var loadDictionaryUpdateChangedEventArgs = ReferencePool.Acquire<OnApplicationFocusChangedEventArgs>();
            loadDictionaryUpdateChangedEventArgs.IsFocus = isFocus;
            return loadDictionaryUpdateChangedEventArgs;
        }

        /// <summary>
        /// 清理前后台切换事件。
        /// </summary>
        public override void Clear()
        {
            IsFocus = false;
        }
    }
}