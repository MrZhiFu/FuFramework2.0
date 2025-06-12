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
        public string UIAssetName { get; private set; }


        /// <summary>
        /// 获取是否暂停被覆盖的界面。
        /// </summary>
        public bool PauseCoveredUI { get; private set; }

        /// <summary>
        /// 获取错误信息。
        /// </summary>
        public string ErrorMessage { get; private set; }

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
            UIAssetName    = null;
            PauseCoveredUI = false;
            ErrorMessage   = null;
            UserData       = null;
        }

        /// <summary>
        /// 创建打开界面失败事件。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <param name="uiAssetName">界面资源名称。</param>
        /// <param name="pauseCoveredUI">是否暂停被覆盖的界面。</param>
        /// <param name="errorMessage">错误信息。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>创建的打开界面失败事件。</returns>
        public static OpenUIFailureEventArgs Create(int serialId, string uiAssetName, bool pauseCoveredUI, string errorMessage, object userData)
        {
            var openUIFailureEventArgs = ReferencePool.Acquire<OpenUIFailureEventArgs>();
            openUIFailureEventArgs.SerialId       = serialId;
            openUIFailureEventArgs.UIAssetName    = uiAssetName;
            openUIFailureEventArgs.PauseCoveredUI = pauseCoveredUI;
            openUIFailureEventArgs.ErrorMessage   = errorMessage;
            openUIFailureEventArgs.UserData       = userData;
            return openUIFailureEventArgs;
        }

        /// <summary>
        /// 清理打开界面失败事件。
        /// </summary>
        public override void Clear()
        {
            SerialId       = 0;
            UIAssetName    = null;
            PauseCoveredUI = false;
            ErrorMessage   = null;
            UserData       = null;
        }
    }
}