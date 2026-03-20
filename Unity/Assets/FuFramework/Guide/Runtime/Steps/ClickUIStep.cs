using FairyGUI;
using FuFramework.Core.Runtime;
using FuFramework.ModuleSetting.Runtime;
using FuFramework.UI.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Guide.Runtime
{
    /// <summary>
    /// UI点击引导步骤
    /// </summary>
    public class ClickUIStep : BaseStep
    {
        /// <summary>
        /// 点击目标UI组件
        /// </summary>
        private GComponent targetUI;

        protected override void OnExecute()
        {
            base.OnExecute();
            var uiModule = ModuleManager.GetModule<UIModule>();
            if (uiModule == null) return;

            // 查找目标界面
            var targetWin = uiModule.GetUI(StepInfo.m_TargetWindow);
            if (targetWin == null)
            {
                FuLogger.LogWarning($"[ClickUIStep] 找不到目标界面: {StepInfo.m_TargetWindow}");
                return;
            }

            // 查找目标点击UI
            if (targetWin.UIView.GetChild(StepInfo.m_TargetUI) is not GComponent targetClickUI)
            {
                FuLogger.LogWarning($"[ClickUIStep] 找不到目标点击UI: {StepInfo.m_TargetUI}");
                return;
            }

            targetUI = targetClickUI;

            // 添加目标UI点击回调
            targetUI.onClick.Add(Complete);

            // 执行点击UI引导
            if (GuideAction == null)
            {
                FuLogger.LogWarning("[ClickUIStep] 无法执行引导，引导动作执行器为null");
                return;
            }

            GuideAction.DoClickUIGuide(targetUI);
        }

        protected override void OnComplete()
        {
            // 移除监听器，结束点击UI引导
            targetUI?.onClick.Remove(Complete);
            GuideAction?.EndClickUIGuide();
            targetUI = null;
            base.OnComplete();
        }

        /// <summary>
        /// 创建默认步骤实例
        /// </summary>
        /// <param name="stepInfo">步骤数据信息</param>
        /// <returns></returns>
        public static ClickUIStep Create(StepInfo stepInfo)
        {
            var step = ReferencePool.Runtime.ReferencePool.Acquire<ClickUIStep>();
            step.StepInfo = stepInfo;
            return step;
        }
    }
}