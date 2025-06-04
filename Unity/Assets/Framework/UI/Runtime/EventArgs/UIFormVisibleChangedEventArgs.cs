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
    /// 界面激活状态变化事件。
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public sealed class UIFormVisibleChangedEventArgs : GameEventArgs
    {
        /// <summary>
        /// 界面激活状态变化事件编号。
        /// </summary>
        public static readonly string EventId = typeof(UIFormVisibleChangedEventArgs).FullName;

        /// <summary>
        /// 初始化打开界面成功事件的新实例。
        /// </summary>
        public UIFormVisibleChangedEventArgs()
        {
            UIForm = null;
            Visible = false;
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
        public bool Visible { get; private set; }

        /// <summary>
        /// 获取用户自定义数据。
        /// </summary>
        public object UserData { get; private set; }

        /// <summary>
        /// 创建打开界面成功事件。
        /// </summary>
        /// <param name="uiForm">打开成功的界面。</param>
        /// <param name="visible">显示状态。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>创建的打开界面成功事件。</returns>
        public static UIFormVisibleChangedEventArgs Create(IUIForm uiForm, bool visible, object userData)
        {
            UIFormVisibleChangedEventArgs uiFormSuccessEventArgs = ReferencePool.Acquire<UIFormVisibleChangedEventArgs>();
            uiFormSuccessEventArgs.UIForm = (UIForm)uiForm;
            uiFormSuccessEventArgs.Visible = visible;
            uiFormSuccessEventArgs.UserData = userData;
            return uiFormSuccessEventArgs;
        }

        /// <summary>
        /// 清理打开界面成功事件。
        /// </summary>
        public override void Clear()
        {
            UIForm = null;
            Visible = false;
            UserData = null;
        }
    }
}