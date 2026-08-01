using FairyGUI;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;
using Hotfix.Framework.UI;
using Hotfix.Game.Config.Tables;
namespace Hotfix.Framework.Guide
{
    /// <summary>
    /// UI点击引导步骤。
    /// 功能：
    ///     1. 查找点击目标UI组件。
    ///     2. 添加目标UI点击回调。
    ///     3. 执行点击UI引导。
    ///     4. 点击UI引导完成后，移除点击回调。
    /// </summary>
    public class ClickUIStep : BaseStep
    {
        /// <summary>
        /// 点击目标UI组件
        /// </summary>
        private GComponent m_TargetUI;

        protected override void OnExecute()
        {
            base.OnExecute();
            var uiModule = ModuleManager.GetModule<UIModule>();
            if (uiModule == null) return;

            // 查找目标界面
            var targetWin = uiModule.Get(StepInfo.TargetWindow);
            if (targetWin == null)
            {
                FuLogger.LogWarning($"[ClickUIStep] 找不到目标界面: {StepInfo.TargetWindow}");
                return;
            }

            // 查找目标点击UI
            if (targetWin.WinUI.GetChild(StepInfo.TargetUI) is not GComponent targetClickUI)
            {
                FuLogger.LogWarning($"[ClickUIStep] 找不到目标点击UI: {StepInfo.TargetUI}");
                return;
            }

            m_TargetUI = targetClickUI;

            // 添加目标UI点击回调
            m_TargetUI.onClick.Add(Complete);

            // 执行点击UI引导
            if (GuideAction == null)
            {
                FuLogger.LogWarning("[ClickUIStep] 无法执行引导，引导动作执行器为null");
                return;
            }

            GuideAction.DoClickUIGuide(m_TargetUI);
        }

        protected override void OnComplete()
        {
            // 移除监听器，结束点击UI引导
            m_TargetUI?.onClick.Remove(Complete);
            GuideAction?.EndClickUIGuide();
            m_TargetUI = null;
            base.OnComplete();
        }

        /// <summary>
        /// 创建默认步骤实例
        /// </summary>
        /// <param name="stepInfo">步骤数据信息</param>
        /// <returns></returns>
        public static ClickUIStep Create(GuideStep stepInfo)
        {
            var step = GlobalModule.ReferencePoolModule.Acquire<ClickUIStep>();
            step.StepInfo = stepInfo;
            return step;
        }
    }
}
