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
        public void ShowDialog(string content, Action onConfirm = null)
        {
            FuLog.Info("显示对话引导");
        }

        /// <summary>
        /// 隐藏对话引导
        /// </summary>
        public void HideDialog()
        {
            FuLog.Info("隐藏对话引导");
        }
    }
}