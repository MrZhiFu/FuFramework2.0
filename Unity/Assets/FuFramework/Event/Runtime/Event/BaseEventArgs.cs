using System;
using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Event.Runtime
{
    /// <summary>
    /// 事件基类。
    /// </summary>
    public abstract class BaseEventArgs : EventArgs, IReference
    {
        /// <summary>
        /// 获取事件ID。
        /// </summary>
        public abstract string Id { get; }

        /// <summary>
        /// 清理引用。
        /// </summary>
        public abstract void Clear();
    }
}