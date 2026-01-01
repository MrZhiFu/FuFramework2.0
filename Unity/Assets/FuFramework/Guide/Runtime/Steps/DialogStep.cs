using FuFramework.ModuleSetting.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Guide.Runtime
{
    /// <summary>
    /// 对话引导步骤
    /// </summary>
    public class DialogStep : BaseStep
    {
        protected override void OnExecute()
        {
            base.OnExecute();

            // 通过GuideManager显示对话
            // GuideManager.Instance.ShowDialog(dialogContent);
        }

        protected override void OnComplete()
        {
            base.OnComplete();
            // GuideManager.Instance.HideDialog();
        }

        protected override void OnCancel()
        {
            // GuideManager.Instance.HideDialog();
            base.OnCancel();
        }

        private void OnDialogConfirmed()
        {
            Complete();
        }

        /// <summary>
        /// 创建对话引导步骤实例
        /// </summary>
        /// <param name="stepInfo">步骤数据信息</param>
        /// <returns></returns>
        public static DialogStep Create(StepInfo stepInfo)
        {
            var step = ReferencePool.Runtime.ReferencePool.Acquire<DialogStep>();
            step.StepInfo = stepInfo;
            return step;
        }
    }
}