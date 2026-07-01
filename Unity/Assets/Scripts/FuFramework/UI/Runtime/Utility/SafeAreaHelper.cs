using System;
using UnityEngine;

namespace FuFramework.UI.Runtime
{
    /// <summary>
    /// 安全区辅助工具。
    /// 封装 Unity Screen.safeArea，提供 FairyGUI 设计坐标下的安全区偏移量，并支持方向变化通知。
    /// </summary>
    public static class SafeAreaHelper
    {
        /// <summary>
        /// 当前安全区（设计坐标）。
        /// </summary>
        public static Rect Current { get; private set; }

        /// <summary>
        /// 左侧安全区偏移（设计坐标）。
        /// </summary>
        public static float LeftInset => Current.x;

        /// <summary>
        /// 右侧安全区偏移（设计坐标）。
        /// </summary>
        public static float RightInset { get; private set; }

        /// <summary>
        /// 顶部安全区偏移（设计坐标）。
        /// </summary>
        public static float TopInset => Current.y;

        /// <summary>
        /// 底部安全区偏移（设计坐标）。
        /// </summary>
        public static float BottomInset { get; private set; }

        /// <summary>
        /// 全屏宽度（设计坐标，含安全区）。
        /// </summary>
        public static float FullWidth { get; private set; }

        /// <summary>
        /// 全屏高度（设计坐标，含安全区）。
        /// </summary>
        public static float FullHeight { get; private set; }

        /// <summary>
        /// 安全区变化事件（方向切换、折叠屏等）。
        /// </summary>
        public static event Action OnSafeAreaChanged;

        private static Rect m_LastSafeArea;

        /// <summary>
        /// 刷新安全区数据。应在模块初始化时调用一次，后续通过 PollUpdate 自动检测变化。
        /// </summary>
        public static void Refresh()
        {
            Rect safeArea   = Screen.safeArea;
            float scaleFactor = FairyGUI.UIContentScaler.scaleFactor;
            if (scaleFactor <= 0) scaleFactor = 1;

            Current      = new Rect(safeArea.x / scaleFactor, safeArea.y / scaleFactor,
                                    safeArea.width / scaleFactor, safeArea.height / scaleFactor);
            RightInset   = (Screen.width  - safeArea.xMax) / scaleFactor;
            BottomInset  = (Screen.height - safeArea.yMax) / scaleFactor;
            FullWidth    = Screen.width  / scaleFactor;
            FullHeight   = Screen.height / scaleFactor;
            m_LastSafeArea = safeArea;
        }

        /// <summary>
        /// 每帧检测安全区是否变化（方向切换等）。
        /// 由 UIModule 驱动调用。
        /// </summary>
        public static void PollUpdate()
        {
            if (Screen.safeArea == m_LastSafeArea) return;
            Refresh();
            OnSafeAreaChanged?.Invoke();
        }
    }
}
