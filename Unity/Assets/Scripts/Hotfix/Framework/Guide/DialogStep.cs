using FuFramework.ReferencePool.Runtime;

using AOT.Framework.ModuleSetting.Runtime;
using AOT.Framework.ModuleSetting.Runtime.Guide;
namespace Hotfix.Guide
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
            GuideAction.DoDialogGuide(StepInfo.m_DialogContent, Complete);
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
        public static DialogStep Create(StepInfo stepInfo)
        {
            var step = ReferencePool.Acquire<DialogStep>();
            step.StepInfo = stepInfo;
            return step;
        }
    }
}
