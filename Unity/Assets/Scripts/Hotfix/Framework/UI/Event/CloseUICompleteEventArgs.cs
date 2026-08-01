using Hotfix.Framework.Event;
using Hotfix.Framework.Core;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.Framework.UI
{
    /// <summary>
    /// 关闭界面完成事件。
    /// </summary>
    public sealed class CloseUICompleteEventArgs : GameEventArgs
    {
        /// <summary>
        /// 获取关闭界面完成事件编号。
        /// </summary>
        public override string Id => EventId;

        /// <summary>
        /// 关闭界面完成事件编号。
        /// </summary>
        public static readonly string EventId = typeof(CloseUICompleteEventArgs).FullName;

        /// <summary>
        /// 获取界面序列编号。
        /// </summary>
        public int SerialId { get; private set; }

        /// <summary>
        /// 获取界面资源名称。
        /// </summary>
        public string WinName { get; private set; }

        /// <summary>
        /// 获取界面所属的界面组。
        /// </summary>
        public UIGroup UIGroup { get; private set; }

        /// <summary>
        /// 初始化关闭界面完成事件的新实例。
        /// </summary>
        public CloseUICompleteEventArgs()
        {
            SerialId = 0;
            WinName   = null;
            UIGroup  = null;
        }

        /// <summary>
        /// 创建关闭界面完成事件。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <param name="winName">界面资源名称。</param>
        /// <param name="uiGroup">界面所属的界面组。</param>
        /// <returns>创建的关闭界面完成事件。</returns>
        public static CloseUICompleteEventArgs Create(int serialId, string winName, UIGroup uiGroup)
        {
            var closeUICompleteEventArgs = GlobalModule.ReferencePoolModule.Acquire<CloseUICompleteEventArgs>();
            closeUICompleteEventArgs.SerialId = serialId;
            closeUICompleteEventArgs.WinName   = winName;
            closeUICompleteEventArgs.UIGroup  = uiGroup;
            return closeUICompleteEventArgs;
        }

        /// <summary>
        /// 清理关闭界面完成事件。
        /// </summary>
        public override void Clear()
        {
            SerialId = 0;
            WinName   = null;
            UIGroup  = null;
        }
    }
}