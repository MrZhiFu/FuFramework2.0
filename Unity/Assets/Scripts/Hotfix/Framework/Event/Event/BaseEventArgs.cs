using System;
using Hotfix.Framework.ReferencePools;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Event
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
