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
        /// 忽略安全区，使组件覆盖刘海/打孔区域。自动监听方向变化并调整尺寸。
        /// 适用场景：全屏背景、模态遮罩、引导遮挡层等需要超出安全区的内容。
        /// </summary>
        /// <param name="component">目标组件</param>
        /// <param name="type">Relation 类型，常用 RelationType.Size</param>
        public static void IgnoreSafeArea(this GObject component, RelationType type)
        {
            void OnSetScreen()
            {
                // 1. 清除旧的 Relation，防止与新 Relation 冲突
                component.relations.ClearAll();

                // 2. 计算偏移（+2px 冗余防止边界露缝）
                var offsetX = -SafeAreaHelper.LeftInset - 2;
                var offsetY = -SafeAreaHelper.TopInset  - 2;

                // 3. 调整位置和尺寸（覆盖安全区外的部分）
                component.SetXY(offsetX, offsetY);
                component.SetSize(SafeAreaHelper.FullWidth + 4, SafeAreaHelper.FullHeight + 4);

                // 4. 绑定到 GRoot 根容器
                component.AddRelation(GRoot.inst, type);
            }

            // 首次调用
            OnSetScreen();

            // 监听安全区变化（方向切换等）
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
