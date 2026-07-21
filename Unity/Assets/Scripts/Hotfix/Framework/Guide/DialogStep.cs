using Hotfix.Framework.ReferencePools;
using Hotfix.Game.Tables.Tables;
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
            GuideAction.DoDialogGuide(StepInfo.DialogContent, Complete);
        }

        protected override void OnComplete()
        {
            GuideAction.EndDialogGuide();
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
