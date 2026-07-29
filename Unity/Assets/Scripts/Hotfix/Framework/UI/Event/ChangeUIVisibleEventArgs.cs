using Hotfix.Framework.ReferencePools;
﻿using Hotfix.Framework.Event;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.Framework.UI
{
    /// <summary>
    /// 改变界面可见状态事件。
    /// </summary>
    public sealed class ChangeUIVisibleEventArgs : GameEventArgs
    {
        /// <summary>
        /// 获取打开界面成功事件编号。
        /// </summary>
        public override string Id => EventId;

        /// <summary>
        /// 界面可见状态变化事件编号。
        /// </summary>
        public static readonly string EventId = typeof(ChangeUIVisibleEventArgs).FullName;

        /// <summary>
        /// 获取打开成功的界面。
        /// </summary>
        public WinBase Win { get; private set; }

        /// <summary>
        /// 获取加载持续时间。
        /// </summary>
        public bool Visible { get; private set; }

        /// <summary>
        /// 获取用户自定义数据。
        /// </summary>
        public object UserData { get; private set; }

        /// <summary>
        /// 初始化打开界面成功事件的新实例。
        /// </summary>
        public ChangeUIVisibleEventArgs()
        {
            Win   = null;
            Visible  = false;
            UserData = null;
        }

        /// <summary>
        /// 创建改变界面可见状态事件。
        /// </summary>
        /// <param name="win">打开成功的界面。</param>
        /// <param name="visible">显示状态。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>创建的打开界面成功事件。</returns>
        public static ChangeUIVisibleEventArgs Create(WinBase win, bool visible, object userData)
        {
            var uiSuccessEventArgs = ReferencePool.Acquire<ChangeUIVisibleEventArgs>();
            uiSuccessEventArgs.Win   = win;
            uiSuccessEventArgs.Visible  = visible;
            uiSuccessEventArgs.UserData = userData;
            return uiSuccessEventArgs;
        }

        /// <summary>
        /// 清理改变界面可见状态事件。
        /// </summary>
        public override void Clear()
        {
            Win   = null;
            Visible  = false;
            UserData = null;
        }
    }
}
