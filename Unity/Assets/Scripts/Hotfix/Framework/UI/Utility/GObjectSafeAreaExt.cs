using FairyGUI;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.UI
{
    /// <summary>
    /// 为 FairyGUI GObject 提供安全区相关扩展方法。
    /// 不修改 FairyGUI 源码。
    /// </summary>
    public static class GObjectSafeAreaExt
    {
        /// <summary>
        /// 使指定组件忽略安全区，可覆盖刘海/打孔区域。自动监听方向变化并调整尺寸。
        /// 适用场景：全屏背景、遮罩、引导遮挡层等需要超出安全区的内容。
        /// 原理：GRoot 被移到了安全区内，此方法通过负偏移让组件反向扩展到 GRoot 之外，覆盖整屏。
        /// </summary>
        /// <param name="component">目标组件</param>
        /// <param name="sideExpand">每侧额外扩展量，防止浮点误差导致边界露缝。</param>
        /// <param name="relationType">关联类型，常用 RelationType.Size（填满屏幕）</param>
        public static void IgnoreSafeArea(this GObject component, float sideExpand = 2f, RelationType relationType = RelationType.Size)
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

                // 2. 负偏移：组件是 GRoot 子节点，坐标为设计坐标（渲染 × scaleFactor）。
                //    SafeAreaHelper.OffsetX 是屏幕像素（用于 GRoot 自身定位），此处须除以 scaleFactor
                //    转成设计坐标，再额外偏移 sideExpand 防止边界露缝。
                var scaleFactor = UIContentScaler.scaleFactor;
                if (scaleFactor <= 0) scaleFactor = 1;

                var offsetX = -SafeAreaHelper.OffsetX / scaleFactor - sideExpand;
                var offsetY = -SafeAreaHelper.OffsetY / scaleFactor - sideExpand;
                component.SetXY(offsetX, offsetY);

                // 3. 整屏尺寸（设计坐标）：屏宽/scaleFactor 为整屏宽高，+ sideExpand×2 覆盖左右/上下两侧冗余。
                component.SetSize(Screen.width / scaleFactor + sideExpand * 2, Screen.height / scaleFactor + sideExpand * 2);

                // 4. 绑定到 GRoot 根容器
                component.AddRelation(GRoot.inst, relationType);
            }
        }
    }
}