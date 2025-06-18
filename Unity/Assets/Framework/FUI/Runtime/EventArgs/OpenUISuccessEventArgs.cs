//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFrameX.Event.Runtime;
using GameFrameX.Runtime;

namespace GameFrameX.UI.Runtime
{
    /// <summary>
    /// 打开界面成功事件。
    /// </summary>
    public sealed class OpenUISuccessEventArgs : GameEventArgs
    {
        /// <summary>
        /// 打开界面成功事件编号。
        /// </summary>
        public static readonly string EventId = typeof(OpenUISuccessEventArgs).FullName;

        /// <summary>
        /// 获取打开界面成功事件编号。
        /// </summary>
        public override string Id => EventId;

        /// <summary>
        /// 获取打开成功的界面。
        /// </summary>
        public ViewBase ViewBase { get; private set; }

        /// <summary>
        /// 获取加载持续时间。
        /// </summary>
        public float Duration { get; private set; }

        /// <summary>
        /// 获取用户自定义数据。
        /// </summary>
        public object UserData { get; private set; }

        /// <summary>
        /// 初始化打开界面成功事件的新实例。
        /// </summary>
        public OpenUISuccessEventArgs()
        {
            ViewBase = null;
            Duration = 0f;
            UserData = null;
        }

        /// <summary>
        /// 创建打开界面成功事件。
        /// </summary>
        /// <param name="iuiBase">打开成功的界面。</param>
        /// <param name="duration">加载持续时间。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>创建的打开界面成功事件。</returns>
        public static OpenUISuccessEventArgs Create(ViewBase iuiBase, float duration, object userData)
        {
            var openUISuccessEventArgs = ReferencePool.Acquire<OpenUISuccessEventArgs>();
            openUISuccessEventArgs.ViewBase = iuiBase;
            openUISuccessEventArgs.Duration = duration;
            openUISuccessEventArgs.UserData = userData;
            return openUISuccessEventArgs;
        }

        /// <summary>
        /// 清理打开界面成功事件。
        /// </summary>
        public override void Clear()
        {
            ViewBase = null;
            Duration = 0f;
            UserData = null;
        }
    }
}