using System;
using FairyGUI;
using FuFramework.Core.Runtime;
using AOT.Framework.Core.Log;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.UI.Runtime
{
    /// <summary>
    /// 安全区辅助工具。
    /// 封装 Unity Screen.safeArea，提供 FairyGUI 设计坐标下的安全区偏移量。
    /// 核心思路：把 GRoot 偏移到安全区起始位置，并缩放到安全区大小。所有需要适配刘海的普通 UI 挂载在 GRoot 下，自然就避开了刘海，
    ///         如果需要全屏的界面，就反向移动界面的起始位置，宽高设置为屏幕的宽高，这样就保证了全屏。比如横屏游戏，GRoot放到安全区后X轴偏移了50，则GRoot.SetXY(50，0)，
    ///         且GRoot的宽高设置为安全区 (safeArea)的宽高，这样就保证了GRoot下的所有界面都在安全区。如果有某个需要全屏的界面WinXxx，则WinXxx.SetXY(-50，0),
    ///         且WinXxx.SetSize(Screen.Width, Screed.Height)，这样就保证了该界面是铺满屏幕的(包括了刘海区域)。
    /// </summary>
    public static class SafeAreaHelper
    {
        /// <summary>
        /// 安全区左侧位置x偏移，即左侧刘海宽度，竖屏一般为0，横屏一般为刘海宽度。
        /// </summary>
        public static float OffsetX { get; private set; }

        /// <summary>
        /// 安全区顶部Y偏移，即顶部刘海高度，竖屏一般为刘海高度，横屏一般为0。
        /// </summary>
        public static float OffsetY { get; private set; }

        /// <summary>
        /// 安全区宽度。
        /// </summary>
        public static float SafeWidth { get; private set; }

        /// <summary>
        /// 安全区高度。
        /// </summary>
        public static float SafeHeight { get; private set; }

        /// <summary>
        /// 上一次安全区数据。供 OnUpdate 检测变化时使用。
        /// </summary>
        private static Rect m_LastSafeArea;

        /// <summary>
        /// 安全区变化事件（方向切换、折叠屏等）。
        /// </summary>
        public static event Action OnSafeAreaChanged;

        /// <summary>
        /// 刷新安全区数据。在 UI 模块初始化时调用。
        /// </summary>
        public static void Refresh()
        {
            var safeArea    = Screen.safeArea;
            var scaleFactor = UIContentScaler.scaleFactor;

            if (scaleFactor <= 0) scaleFactor = 1;

            // offsetX = 左侧不安全区宽度
            OffsetX = safeArea.x;

            // offsetY = 顶部不安全区高度，Y 轴翻转，Unity 坐标系 (左下角原点) → FairyGUI 坐标系 (左上角原点)
            OffsetY = Screen.height - (safeArea.y + safeArea.height);

            // FairyGUI 坐标系下的宽度和高度，需要除以FGUI的缩放因子，从Unity的物理像素转换为FairyGUI的物理像素。
            SafeWidth      = Mathf.Ceil(safeArea.width  / scaleFactor);
            SafeHeight     = Mathf.Ceil(safeArea.height / scaleFactor);
            m_LastSafeArea = safeArea;
        }

        /// <summary>
        /// 每帧检测安全区是否变化（方向切换等）。
        /// 由 UIModule.OnUpdate 驱动调用。
        /// </summary>
        public static void OnUpdate()
        {
            if (Screen.safeArea == m_LastSafeArea) return;

            FuLogger.LogInfo("[SafeAreaHelper]屏幕安全区刷新, "           +
                             $"旧安全区:{m_LastSafeArea}, "            +
                             $"新安全区:{Screen.safeArea}, "           +
                             $"安全区偏移量:({OffsetX}, {OffsetY}), "    +
                             $"安全区大小:({SafeWidth}, {SafeHeight})," +
                             $" UI缩放因子:{UIContentScaler.scaleFactor}");
            Refresh();
            OnSafeAreaChanged?.Invoke();
        }
    }
}