using FairyGUI;

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
        /// 忽略安全区，使组件覆盖刘海/打孔区域。自动监听方向变化并调整尺寸。
        /// 适用场景：全屏背景、模态遮罩、引导遮挡层等需要超出安全区的内容。
        /// </summary>
        /// <param name="component">目标组件</param>
        /// <param name="relationType">Relation 类型，常用 RelationType.Size（填满屏幕）</param>
        public static void IgnoreSafeArea(this GObject component, RelationType relationType)
        {
            bool isSetup = false;

            void OnSetScreen()
            {
                if (component == null || component.isDisposed) return;

                // 1. 仅清除与 GRoot 的旧 Relation，保留其他 Relation 不受影响
                component.relations.ClearFor(GRoot.inst);

                // 2. 计算偏移（含冗余边距防止浮点误差导致边界露缝）
                var overFlow = SafeAreaOverflow;
                var offsetX = -SafeAreaHelper.LeftInset - overFlow;
                var offsetY = -SafeAreaHelper.TopInset  - overFlow;

                // 3. 调整位置和尺寸（覆盖安全区外的部分）
                component.SetXY(offsetX, offsetY);
                component.SetSize(SafeAreaHelper.FullWidth + overFlow * 2, SafeAreaHelper.FullHeight + overFlow * 2);

                // 4. 绑定到 GRoot 根容器
                component.AddRelation(GRoot.inst, relationType);
            }

            // 首次调用
            OnSetScreen();

            // 防止重复调用时重复注册 onRemovedFromStage 和事件监听
            if (isSetup) return;
            isSetup = true;

            // 监听安全区变化（方向切换等），-= 防重复
            SafeAreaHelper.OnSafeAreaChanged -= OnSetScreen;
            SafeAreaHelper.OnSafeAreaChanged += OnSetScreen;

            // 组件销毁时注销监听，防止内存泄漏
            component.onRemovedFromStage.Add(() =>
            {
                SafeAreaHelper.OnSafeAreaChanged -= OnSetScreen;
            });
        }
    }
}
