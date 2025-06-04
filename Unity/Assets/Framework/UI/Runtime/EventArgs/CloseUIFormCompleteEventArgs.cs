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
    /// 关闭界面完成事件。
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public sealed class CloseUIFormCompleteEventArgs : GameEventArgs
    {
        /// <summary>
        /// 关闭界面完成事件编号。
        /// </summary>
        public static readonly string EventId = typeof(CloseUIFormCompleteEventArgs).FullName;

        /// <summary>
        /// 初始化关闭界面完成事件的新实例。
        /// </summary>
        public CloseUIFormCompleteEventArgs()
        {
            SerialId = 0;
            UIFormAssetName = null;
            UIGroup = null;
            UserData = null;
        }

        /// <summary>
        /// 获取关闭界面完成事件编号。
        /// </summary>
        public override string Id
        {
            get { return EventId; }
        }

        /// <summary>
        /// 获取界面序列编号。
        /// </summary>
        public int SerialId { get; private set; }

        /// <summary>
        /// 获取界面资源名称。
        /// </summary>
        public string UIFormAssetName { get; private set; }

        /// <summary>
        /// 获取界面所属的界面组。
        /// </summary>
        public IUIGroup UIGroup { get; private set; }

        /// <summary>
        /// 获取用户自定义数据。
        /// </summary>
        public object UserData { get; private set; }

        /// <summary>
        /// 创建关闭界面完成事件。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <param name="uiGroup">界面所属的界面组。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>创建的关闭界面完成事件。</returns>
        public static CloseUIFormCompleteEventArgs Create(int serialId, string uiFormAssetName, IUIGroup uiGroup, object userData)
        {
            CloseUIFormCompleteEventArgs closeUIFormCompleteEventArgs = ReferencePool.Acquire<CloseUIFormCompleteEventArgs>();
            closeUIFormCompleteEventArgs.SerialId = serialId;
            closeUIFormCompleteEventArgs.UIFormAssetName = uiFormAssetName;
            closeUIFormCompleteEventArgs.UIGroup = uiGroup;
            closeUIFormCompleteEventArgs.UserData = userData;
            return closeUIFormCompleteEventArgs;
        }

        /// <summary>
        /// 清理关闭界面完成事件。
        /// </summary>
        public override void Clear()
        {
            SerialId = 0;
            UIFormAssetName = null;
            UIGroup = null;
            UserData = null;
        }
    }
}