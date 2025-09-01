using FuFramework.Event.Runtime;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace FuFramework.UI.Runtime
{
    /// <summary>
    /// 界面可见状态变化事件。
    /// </summary>
    public sealed class UIVisibleChangedEventArgs : GameEventArgs
    {
        /// <summary>
        /// 获取打开界面成功事件编号。
        /// </summary>
        public override string Id => EventId;

        /// <summary>
        /// 界面可见状态变化事件编号。
        /// </summary>
        public static readonly string EventId = typeof(UIVisibleChangedEventArgs).FullName;

        /// <summary>
        /// 获取打开成功的界面。
        /// </summary>
        public ViewBase Base { get; private set; }

        /// <summary>
        /// 获取加载持续时间。
        /// </summary>
        public bool Visible { get; private set; }

        /// <summary>
        /// 获取用户自定义数据。
        /// </summary>
        public object UserData { get; private set; }

        /// <summary>
        /// 初始化打开界面成功事件的新实例。
        /// </summary>
        public UIVisibleChangedEventArgs()
        {
            Base     = null;
            Visible  = false;
            UserData = null;
        }

        /// <summary>
        /// 创建打开界面成功事件。
        /// </summary>
        /// <param name="iuiBase">打开成功的界面。</param>
        /// <param name="visible">显示状态。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>创建的打开界面成功事件。</returns>
        public static UIVisibleChangedEventArgs Create(ViewBase iuiBase, bool visible, object userData)
        {
            var uiSuccessEventArgs = ReferencePool.Runtime.ReferencePool.Acquire<UIVisibleChangedEventArgs>();
            uiSuccessEventArgs.Base     = iuiBase;
            uiSuccessEventArgs.Visible  = visible;
            uiSuccessEventArgs.UserData = userData;
            return uiSuccessEventArgs;
        }

        /// <summary>
        /// 清理打开界面成功事件。
        /// </summary>
        public override void Clear()
        {
            Base     = null;
            Visible  = false;
            UserData = null;
        }
    }
}