// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 事件基类。
    /// </summary>
    public abstract class BaseEventArgs : FuEventArgs
    {
        /// <summary>
        /// 获取事件ID。
        /// </summary>
        public abstract string Id { get; }
    }
}