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
