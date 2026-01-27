using System;
using Cysharp.Threading.Tasks;
using FairyGUI;
using FuFramework.Core.Runtime;
using FuFramework.Entry.Runtime;
using FuFramework.Guide.Runtime;
using Hotfix.UI;
using UnityEngine;

namespace Hotfix.Guide
{
    /// <summary>
    /// 引导动作执行实现
    /// </summary>
    public class GuideActionImpl : IGuideAction
    {
        /// <summary>
        /// 执行点击UI引导
        /// </summary>
        /// <param name="targetUI">目标点击UI区域</param>
        public void DoClickUIGuide(GComponent targetUI) => _DoClickUIGuide(targetUI).Forget();

        /// <summary>
        /// 结束点击UI引导
        /// </summary>
        public void EndClickUIGuide()
        {
            FuLog.Info("结束点击UI引导");
            GlobalModule.UIModule.CloseUI<WinClickGuide>();
        }

        /// <summary>
        /// 显示对话引导
        /// </summary>
        public void DoDialogGuide(string content, Action onConfirm) => _DoDialogGuide(content, onConfirm).Forget();

        /// <summary>
        /// 结束对话引导
        /// </summary>
        public void EndDialogGuide()
        {
            GlobalModule.UIModule.CloseUI<WinDialogGuide>();
        }

        /// <summary>
        /// 执行点击UI引导
        /// </summary>
        /// <param name="targetUI">目标点击UI区域</param>
        private async UniTaskVoid _DoClickUIGuide(GComponent targetUI)
        {
            FuLog.Info($"执行点击UI引导, 目标UI：{targetUI.name}");
            var winClickGuide = await GlobalModule.UIModule.OpenUIAsync<WinClickGuide>();
            var targetRect = targetUI.TransformRect(new Rect(0, 0, targetUI.width, targetUI.height), winClickGuide.UIView);
            var clickArea = winClickGuide.GetClickArea();
            clickArea.size = targetRect.size;
            clickArea.position = targetRect.position;
        }

        /// <summary>
        /// 执行对话引导
        /// </summary>
        /// <param name="content">对话内容</param>
        /// <param name="onConfirm">对话提交回调</param>
        private async UniTaskVoid _DoDialogGuide(string content, Action onConfirm)
        {
            FuLog.Info("执行对话引导");
            var winDialogGuide = await GlobalModule.UIModule.OpenUIAsync<WinDialogGuide>();
            winDialogGuide.ShowDialog(content, onConfirm);
        }
    }
}