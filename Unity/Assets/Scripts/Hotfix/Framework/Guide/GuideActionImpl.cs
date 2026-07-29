using System;
using Cysharp.Threading.Tasks;
using FairyGUI;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;
using Hotfix.Game.UI;
using UnityEngine;

namespace Hotfix.Framework.Guide
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
        public void DoClickUIGuide(GComponent targetUI) => ExecuteClickUIGuideAsync(targetUI).Forget();

        /// <summary>
        /// 结束点击UI引导
        /// </summary>
        public void EndClickUIGuide() => GlobalModule.UIModule.Close<WinClickGuide>();

        /// <summary>
        /// 显示对话引导
        /// </summary>
        public void DoDialogGuide(string content, Action onConfirm) => ExecuteDialogGuideAsync(content, onConfirm).Forget();

        /// <summary>
        /// 结束对话引导
        /// </summary>
        public void EndDialogGuide() => GlobalModule.UIModule.Close<WinDialogGuide>();

        /// <summary>
        /// 显示全局遮罩窗口
        /// </summary>
        public void ShowGlobalMask() => GlobalModule.UIModule.Open<WinGlobalMask>();

        /// <summary>
        /// 隐藏全局遮罩窗口
        /// </summary>
        public void HideGlobalMask() => GlobalModule.UIModule.Close<WinGlobalMask>();

        /// <summary>
        /// 执行点击UI引导
        /// </summary>
        /// <param name="targetUI">目标点击UI区域</param>
        private async UniTaskVoid ExecuteClickUIGuideAsync(GComponent targetUI)
        {
            FuLogger.LogInfo($"执行点击UI引导, 目标UI：{targetUI.name}");
            var winClickGuide = await GlobalModule.UIModule.OpenAsync<WinClickGuide>();
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
        private async UniTaskVoid ExecuteDialogGuideAsync(string content, Action onConfirm)
        {
            FuLogger.LogInfo("执行对话引导");
            var winDialogGuide = await GlobalModule.UIModule.OpenAsync<WinDialogGuide>();
            winDialogGuide.ShowDialog(content, onConfirm);
        }
    }
}
