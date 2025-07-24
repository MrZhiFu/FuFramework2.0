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
    /// 打开界面失败事件。
    /// </summary>
    
    public sealed class OpenUIFailureEventArgs : GameEventArgs
    {
        /// <summary>
        /// 打开界面失败事件编号。
        /// </summary>
        public static readonly string EventId = typeof(OpenUIFailureEventArgs).FullName;

        /// <summary>
        /// 获取打开界面失败事件编号。
        /// </summary>
        public override string Id => EventId;

        /// <summary>
        /// 获取界面序列编号。
        /// </summary>
        public int SerialId { get; private set; }

        /// <summary>
        /// 获取界面资源名称。
        /// </summary>
        public string UIName { get; private set; }

        /// <summary>
        /// 获取用户自定义数据。
        /// </summary>
        public object UserData { get; private set; }

        /// <summary>
        /// 初始化打开界面失败事件的新实例。
        /// </summary>
        public OpenUIFailureEventArgs()
        {
            SerialId       = 0;
            UIName    = null;
            UserData       = null;
        }

        /// <summary>
        /// 创建打开界面失败事件。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <param name="uiName">界面资源名称。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>创建的打开界面失败事件。</returns>
        public static OpenUIFailureEventArgs Create(int serialId, string uiName, object userData)
        {
            var openUIFailureEventArgs = ReferencePool.Acquire<OpenUIFailureEventArgs>();
            openUIFailureEventArgs.SerialId     = serialId;
            openUIFailureEventArgs.UIName       = uiName;
            openUIFailureEventArgs.UserData     = userData;
            return openUIFailureEventArgs;
        }

        /// <summary>
        /// 清理打开界面失败事件。
        /// </summary>
        public override void Clear()
        {
            SerialId       = 0;
            UIName    = null;
            UserData       = null;
        }
    }
}