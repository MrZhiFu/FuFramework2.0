using System;
using GameFrameX.Runtime;

namespace GameFrameX.UI.Runtime
{
    /// <summary>
    /// 打开界面时，界面的信息数据，包括界面序列号，界面类型，是否全屏，是否暂停被覆盖的界面，用户自定义数据。
    /// 用来在界面打开时传递给界面实例，在界面关闭时传递给界面实例。并提供对象回收的功能。
    /// </summary>
    public sealed class OpenUIInfo : IReference
    {
        /// <summary>
        /// 获取界面序列编号。
        /// </summary>
        public int SerialId { get; private set; } = 0;

        /// <summary>
        /// 获取界面类型。
        /// </summary>
        public Type UIType { get; private set; }

        /// <summary>
        /// 获取界面是否全屏。
        /// </summary>
        public bool IsFullScreen { get; private set; } = false;

        /// <summary>
        /// 获取是否暂停被覆盖的界面。
        /// </summary>
        public bool PauseCoveredUI { get; private set; } = false;

        /// <summary>
        /// 获取用户自定义数据。
        /// </summary>
        public object UserData { get; private set; } = null;

        /// <summary>
        /// 创建打开界面的信息。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <param name="uiFormType">界面类型。</param>
        /// <param name="pauseCoveredUIForm">是否暂停被覆盖的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="isFullScreen">界面是否全屏。</param>
        /// <returns>创建的打开界面的信息。</returns>
        public static OpenUIInfo Create(int serialId, Type uiFormType, bool pauseCoveredUIForm, object userData, bool isFullScreen)
        {
            var openUIFormInfo = ReferencePool.Acquire<OpenUIInfo>();
            openUIFormInfo.SerialId           = serialId;
            openUIFormInfo.PauseCoveredUI = pauseCoveredUIForm;
            openUIFormInfo.UserData           = userData;
            openUIFormInfo.UIType           = uiFormType;
            openUIFormInfo.IsFullScreen       = isFullScreen;
            return openUIFormInfo;
        }

        /// <summary>
        /// 清理打开界面的信息。
        /// </summary>
        public void Clear()
        {
            SerialId           = 0;
            PauseCoveredUI = false;
            UserData           = null;
        }
    }
}