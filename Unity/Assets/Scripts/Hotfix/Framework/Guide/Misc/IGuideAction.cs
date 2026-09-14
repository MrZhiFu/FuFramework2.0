using System;
using FairyGUI;

namespace Hotfix.Framework.Guide
{
    /// <summary>
    /// 执行引导动作接口。
    /// 功能：
    ///     1. 定义执行引导动作的相关接口，由热更代码中指定具体的实现类。
    /// </summary>
    public interface IGuideAction
    {
        /// <summary>
        /// 执行点击UI引导
        /// </summary>
        /// <param name="targetUI">目标点击UI区域</param>
        public void DoClickUIGuide(GComponent targetUI);

        /// <summary>
        /// 结束点击UI引导
        /// </summary>
        public void EndClickUIGuide();

        /// <summary>
        /// 执行对话引导
        /// </summary>
        /// <param name="content">对话内容</param>
        /// <param name="onConfirm">对话提交回调</param>
        public void DoDialogGuide(string content, Action onConfirm = null);

        /// <summary>
        /// 结束对话引导
        /// </summary>
        public void EndDialogGuide();

        /// <summary>
        /// 显示全局遮罩窗口
        /// </summary>
        public void ShowGlobalMask();

        /// <summary>
        /// 隐藏全局遮罩窗口
        /// </summary>
        public void HideGlobalMask();
    }
}
