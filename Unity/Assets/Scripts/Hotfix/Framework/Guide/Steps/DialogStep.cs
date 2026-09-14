using Hotfix.Game.Config;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;
namespace Hotfix.Framework.Guide
{
    /// <summary>
    /// 对话引导步骤。
    /// 功能：
    ///     1. 执行对话引导。
    ///     2. 对话引导完成后，移除对话引导。
    /// </summary>
    public class DialogStep : BaseStep
    {
        protected override void OnExecute()
        {
            base.OnExecute();

            // 空判与 ClickUIStep 保持一致：GuideAction 为 null 时告警并返回，
            // 否则 NRE 会被 GuideModule 的 catch 吞成静默跳步。
            if (GuideAction == null)
            {
                FuLogger.LogWarning("[DialogStep] 无法执行引导，引导动作执行器为null");
                return;
            }

            GuideAction.DoDialogGuide(StepInfo.DialogContent, Complete);
        }

        protected override void OnComplete()
        {
            // 与 ClickUIStep.OnComplete / WaitStep 一致，用 ?. 空判
            GuideAction?.EndDialogGuide();
            base.OnComplete();
        }

        /// <summary>
        /// 步骤取消。
        /// 覆写原因：DoDialogGuide 把本步骤的 Complete 作为 onConfirm 存进了 WinDialogGuide.m_OnConfirm，
        /// 而基类 Cancel() 只调 OnCancel()、不经过 Clear()；SkipCurrentStep / JumpToStep / ForceNextStep /
        /// GoToPreviousStep / InterruptGuide 取消本步骤后，对话框（及其持有的 onConfirm）仍然存活，
        /// 玩家点确认会驱动「已取消的步骤」调 Complete()（StepInfo 已失效则 NRE，实例被复用则 ABA 跳错步）。
        /// 故这里与 ClickUIStep.OnCancel 同款：结束对话引导（关闭 WinDialogGuide，随 OnDispose 清空 m_OnConfirm）。
        /// </summary>
        protected override void OnCancel()
        {
            GuideAction?.EndDialogGuide();
            base.OnCancel();
        }

        /// <summary>
        /// 清理步骤。
        /// 覆写原因：基类 Clear() 只置空 StepInfo，不会结束对话引导。步骤回池后 WinDialogGuide 仍可能持有
        /// 指向本实例的 onConfirm（同一池对象被复用时会驱动错误引导），故补上引导结束，与 OnCancel 口径一致。
        /// </summary>
        public override void Clear()
        {
            GuideAction?.EndDialogGuide();
            base.Clear();
        }

        /// <summary>
        /// 创建对话引导步骤实例
        /// </summary>
        /// <param name="stepInfo">步骤数据信息</param>
        /// <returns></returns>
        public static DialogStep Create(GuideStep stepInfo)
        {
            var step = ReferencePool.Acquire<DialogStep>();
            step.StepInfo = stepInfo;
            return step;
        }
    }
}
