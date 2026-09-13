using System;
using System.Threading;
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
    public class GuideActionImpl : IGuideAction, IDisposable
    {
        /// <summary>
        /// 点击UI引导异步链的生命周期取消源（本类即该异步链的所有者，随会话常驻）。
        /// 生命周期 = 一次「点击UI引导」的展示周期：DoClickUIGuide 用 Recreate 开启新一轮令牌，
        /// EndClickUIGuide（步骤完成或步骤取消时调用）用 Cancel 结束本轮，
        /// 在途的 OpenAsync 延续据此识别「引导已结束」并回收窗口，避免引导窗永久驻留。
        /// </summary>
        private readonly LifecycleCancellationSource m_ClickGuideCancellation = new();

        /// <summary>
        /// 对话引导异步链的生命周期取消源（本类即该异步链的所有者，随会话常驻）。
        /// 生命周期 = 一次「对话引导」的展示周期：DoDialogGuide 用 Recreate 开启新一轮令牌，
        /// EndDialogGuide（步骤完成、步骤取消、步骤回池等结束路径都会调用）用 Cancel 结束本轮，
        /// 在途的 OpenAsync 延续据此识别「引导已结束」并关闭对话框，避免把已回收步骤的 Complete 写进窗口。
        /// </summary>
        private readonly LifecycleCancellationSource m_DialogGuideCancellation = new();

        /// <summary>
        /// 执行点击UI引导
        /// </summary>
        /// <param name="targetUI">目标点击UI区域</param>
        public void DoClickUIGuide(GComponent targetUI)
        {
            // 本轮展示开启新生命周期令牌：上一轮的旧令牌作废，旧链不再写回本次引导
            m_ClickGuideCancellation.Recreate();
            ExecuteClickUIGuideAsync(targetUI, m_ClickGuideCancellation.Token).Forget();
        }

        /// <summary>
        /// 结束点击UI引导
        /// </summary>
        public void EndClickUIGuide()
        {
            // 先取消在途的打开链（否则步骤取消后窗口仍会被迟开、无人回收），再关闭已打开的引导窗
            m_ClickGuideCancellation.Cancel();
            GlobalModule.UIModule.Close<WinClickGuide>();
        }

        /// <summary>
        /// 显示对话引导
        /// </summary>
        public void DoDialogGuide(string content, Action onConfirm)
        {
            // 本轮展示开启新生命周期令牌：上一轮的旧令牌作废，旧链不再写回本次对话引导（与 DoClickUIGuide 同款）
            m_DialogGuideCancellation.Recreate();
            ExecuteDialogGuideAsync(content, onConfirm, m_DialogGuideCancellation.Token).Forget();
        }

        /// <summary>
        /// 结束对话引导
        /// </summary>
        public void EndDialogGuide()
        {
            // 先取消在途的打开链（否则步骤取消/回池/引导结束后对话框仍会被迟开，且其 m_OnConfirm 已指向失效步骤），
            // 再关闭已打开的对话框（与 EndClickUIGuide 同款）。
            // DialogStep 的 OnComplete/OnCancel/Clear 均经此方法收尾，故三条结束路径都会触发本取消。
            m_DialogGuideCancellation.Cancel();
            GlobalModule.UIModule.Close<WinDialogGuide>();
        }

        /// <summary>
        /// 释放：取消并释放两条引导链的生命周期取消源，避免 CTS 泄漏。
        /// 由持有方（引导模块，见 GuideModule.GuideAction）在销毁时调用；本类不再复用时只走 Dispose，复用场景走 Recreate。
        /// </summary>
        public void Dispose()
        {
            m_ClickGuideCancellation.Dispose();
            m_DialogGuideCancellation.Dispose();
        }

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
        /// <param name="token">点击UI引导展示周期的取消令牌（由 m_ClickGuideCancellation 提供）</param>
        private async UniTaskVoid ExecuteClickUIGuideAsync(GComponent targetUI, CancellationToken token)
        {
            // 整条链包 try/catch：原实现只在 await 之后按令牌兜底（仅覆盖「窗口已创建且已取消」），
            // 若 OpenAsync / 取区域过程中抛异常（如重启引导时界面已被销毁），异常会跳出整个兜底分支，
            // 只留一条 UniTask 调度器日志，引导窗滞留在屏幕上 → 这里补上异常路径的回收。
            try
            {
                FuLogger.LogInfo($"执行点击UI引导, 目标UI：{targetUI.name}");

                // UIModule.OpenAsync 暂不支持取消令牌，而引导窗在 UIModule 内部创建、取消无法回溯到在途加载，
                // 故在 await 结束后统一按令牌兜底：引导已结束（步骤被 Cancel / 回收）则立即回收刚打开的窗口，
                // 不再设置点击区域——否则取消后的延续会把引导窗留在屏幕上（窗口永久驻留）。
                var winClickGuide = await GlobalModule.UIModule.OpenAsync<WinClickGuide>();
                if (winClickGuide == null || token.IsCancellationRequested)
                {
                    GlobalModule.UIModule.Close<WinClickGuide>();
                    return;
                }

                var targetRect = targetUI.TransformRect(new Rect(0, 0, targetUI.width, targetUI.height), winClickGuide.WinUI);
                var clickArea = winClickGuide.GetClickArea();
                clickArea.size = targetRect.size;
                clickArea.position = targetRect.position;
            }
            catch (Exception e)
            {
                FuLogger.LogError($"[GuideActionImpl] 执行点击UI引导失败: {e.Message}\n{e.StackTrace}");
                GlobalModule.UIModule.Close<WinClickGuide>();
            }
        }

        /// <summary>
        /// 执行对话引导
        /// </summary>
        /// <param name="content">对话内容</param>
        /// <param name="onConfirm">对话提交回调</param>
        /// <param name="token">对话引导展示周期的取消令牌（由 m_DialogGuideCancellation 提供）</param>
        private async UniTaskVoid ExecuteDialogGuideAsync(string content, Action onConfirm, CancellationToken token)
        {
            // 整条链包 try/catch（与 ExecuteClickUIGuideAsync 同款）：原实现只在 await 之后按令牌兜底，
            // 若 OpenAsync 过程中抛异常（如重启引导时对话框界面已被销毁），异常会跳出兜底分支，
            // 对话框可能滞留在屏幕上 → 这里补上异常路径的关闭。
            try
            {
                FuLogger.LogInfo("执行对话引导");

                // UIModule.OpenAsync 暂不支持取消令牌，而对话框在 UIModule 内部创建、取消无法回溯到在途加载，
                // 故在 await 结束后统一按令牌兜底（与 ExecuteClickUIGuideAsync 同款）：对话引导已结束
                // （步骤被 Cancel / 回池 / 引导结束）则立即关闭刚打开的对话框，不写入 onConfirm——
                // 否则取消后的延续会把「已回收步骤」的 Complete 写进窗口 m_OnConfirm（悬挂委托 / ABA 跳错步）。
                var winDialogGuide = await GlobalModule.UIModule.OpenAsync<WinDialogGuide>();
                if (winDialogGuide == null || token.IsCancellationRequested)
                {
                    GlobalModule.UIModule.Close<WinDialogGuide>();
                    return;
                }

                winDialogGuide.ShowDialog(content, onConfirm);
            }
            catch (Exception e)
            {
                FuLogger.LogError($"[GuideActionImpl] 执行对话引导失败: {e.Message}\n{e.StackTrace}");
                GlobalModule.UIModule.Close<WinDialogGuide>();
            }
        }
    }
}
