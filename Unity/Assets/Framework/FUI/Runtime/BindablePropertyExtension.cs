using FairyGUI;
using GameFrameX.Runtime;

namespace GameFrameX.UI.FairyGUI.Runtime
{
    public static class BindablePropertyExtension
    {
        /// <summary>
        /// FGUI情况下使用此方法,链式调用,当FairyGUI.GObject销毁时自动注销事件
        /// </summary>
        /// <param name="bindableProperty"></param>
        /// <param name="gObject"></param>
        /// <returns></returns>
        public static void ClearWithGObjectDestroyed<T>(this BindableProperty<T> bindableProperty, GObject gObject)
        {
            gObject.onRemovedFromStage.Add(bindableProperty.Clear);
        }
    }
}