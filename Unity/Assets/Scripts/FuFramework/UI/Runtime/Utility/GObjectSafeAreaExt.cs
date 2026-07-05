using FairyGUI;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.UI.Runtime
{
    /// <summary>
    /// 为 FairyGUI GObject 提供安全区相关扩展方法。
    /// 不修改 FairyGUI 源码。
    /// </summary>
    public static class GObjectSafeAreaExt
    {
        /// <summary>
        /// 每侧额外扩展像素，防止浮点误差导致边界露缝。
        /// </summary>
        private const float SafeAreaOverflow = 2f;

        /// <summary>
        /// 使指定组件忽略安全区，可覆盖刘海/打孔区域。自动监听方向变化并调整尺寸。
        /// 适用场景：全屏背景、遮罩、引导遮挡层等需要超出安全区的内容。
        /// 原理：GRoot 被移到了安全区内，此方法通过负偏移让组件反向扩展到 GRoot 之外，覆盖整屏。
        /// </summary>
        /// <param name="component">目标组件</param>
        /// <param name="relationType">关联类型，常用 RelationType.Size（填满屏幕）</param>
        public static void IgnoreSafeArea(this GObject component, RelationType relationType = RelationType.Size)
        {
            // 首次调用
            ApplyFullScreen();

            // 监听安全区变化（方向切换等），-= 防止重复注册
            SafeAreaHelper.OnSafeAreaChanged -= ApplyFullScreen;
            SafeAreaHelper.OnSafeAreaChanged += ApplyFullScreen;

            // 组件销毁时注销监听，防止内存泄漏
            component.onRemovedFromStage.Add(() => { SafeAreaHelper.OnSafeAreaChanged -= ApplyFullScreen; });
            return;

            void ApplyFullScreen()
            {
                if (component == null || component.isDisposed) return;

                // 1. 清除组件所有旧关联，防止与新关联冲突
                component.relations.ClearAll();

                // 2. 负偏移：反向扩展到 GRoot 之外，覆盖刘海区域（+2px 冗余防止边界露缝）
                var offsetX = -SafeAreaHelper.OffsetX - SafeAreaOverflow;
                var offsetY = -SafeAreaHelper.OffsetY - SafeAreaOverflow;

                // 3. 整屏尺寸（含溢出余量）
                component.SetXY(offsetX, offsetY);
                component.SetSize(Screen.width / UIContentScaler.scaleFactor + SafeAreaOverflow * 2, Screen.height / UIContentScaler.scaleFactor + SafeAreaOverflow * 2);

                // 4. 绑定到 GRoot 根容器
                component.AddRelation(GRoot.inst, relationType);
            }
        }
    }
}