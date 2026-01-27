using System;
using FairyGUI;

namespace FuFramework.Guide.Runtime
{
    /// <summary>
    /// 执行引导动作接口
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
        /// 显示对话
        /// </summary>
        public void ShowDialog(string content, Action onConfirm = null);

        /// <summary>
        /// 隐藏对话
        /// </summary>
        public void HideDialog();
    }
}