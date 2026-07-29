using Hotfix.Framework.ReferencePools;
﻿using Hotfix.Framework.Event;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.Framework.UI
{
    /// <summary>
    /// 打开界面成功事件。
    /// </summary>
    public sealed class OpenSuccessEventArgs : GameEventArgs
    {
        /// <summary>
        /// 获取打开界面成功事件编号。
        /// </summary>
        public override string Id => EventId;

        /// <summary>
        /// 打开界面成功事件编号。
        /// </summary>
        public static readonly string EventId = typeof(OpenSuccessEventArgs).FullName;

        /// <summary>
        /// 获取打开成功的界面。
        /// </summary>
        public WinBase Win { get; private set; }

        /// <summary>
        /// 获取用户自定义数据。
        /// </summary>
        public object UserData { get; private set; }

        /// <summary>
        /// 初始化打开界面成功事件的新实例。
        /// </summary>
        public OpenSuccessEventArgs()
        {
            Win = null;
            UserData = null;
        }

        /// <summary>
        /// 创建打开界面成功事件。
        /// </summary>
        /// <param name="win">打开成功的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>创建的打开界面成功事件。</returns>
        public static OpenSuccessEventArgs Create(WinBase win, object userData)
        {
            var openUISuccessEventArgs = ReferencePool.Acquire<OpenSuccessEventArgs>();
            openUISuccessEventArgs.Win = win;
            openUISuccessEventArgs.UserData = userData;
            return openUISuccessEventArgs;
        }

        /// <summary>
        /// 清理打开界面成功事件。
        /// </summary>
        public override void Clear()
        {
            Win = null;
            UserData = null;
        }
    }
}
