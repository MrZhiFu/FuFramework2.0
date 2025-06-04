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
    [UnityEngine.Scripting.Preserve]
    public sealed class OpenUIFormSuccessEventArgs : GameEventArgs
    {
        /// <summary>
        /// 打开界面成功事件编号。
        /// </summary>
        public static readonly string EventId = typeof(OpenUIFormSuccessEventArgs).FullName;

        /// <summary>
        /// 初始化打开界面成功事件的新实例。
        /// </summary>
        public OpenUIFormSuccessEventArgs()
        {
            UIForm = null;
            Duration = 0f;
            UserData = null;
        }

        /// <summary>
        /// 获取打开界面成功事件编号。
        /// </summary>
        public override string Id
        {
            get { return EventId; }
        }

        /// <summary>
        /// 获取打开成功的界面。
        /// </summary>
        public UIForm UIForm { get; private set; }

        /// <summary>
        /// 获取加载持续时间。
        /// </summary>
        public float Duration { get; private set; }

        /// <summary>
        /// 获取用户自定义数据。
        /// </summary>
        public object UserData { get; private set; }

        /// <summary>
        /// 创建打开界面成功事件。
        /// </summary>
        /// <param name="uiForm">打开成功的界面。</param>
        /// <param name="duration">加载持续时间。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>创建的打开界面成功事件。</returns>
        public static OpenUIFormSuccessEventArgs Create(IUIForm uiForm, float duration, object userData)
        {
            OpenUIFormSuccessEventArgs openUIFormSuccessEventArgs = ReferencePool.Acquire<OpenUIFormSuccessEventArgs>();
            openUIFormSuccessEventArgs.UIForm = (UIForm)uiForm;
            openUIFormSuccessEventArgs.Duration = duration;
            openUIFormSuccessEventArgs.UserData = userData;
            return openUIFormSuccessEventArgs;
        }

        /// <summary>
        /// 清理打开界面成功事件。
        /// </summary>
        public override void Clear()
        {
            UIForm = null;
            Duration = 0f;
            UserData = null;
        }
    }
}