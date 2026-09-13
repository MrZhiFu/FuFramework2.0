using FairyGUI;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;
using Hotfix.Framework.UI;
using Hotfix.Game.Config;
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
        /// 步骤取消。
        /// 覆写原因：基类 Cancel() 只调 OnCancel()、不经过 Clear()；而本步骤被 SkipCurrentStep / JumpToStep /
        /// ForceNextStep / GoToPreviousStep 取消后仍留在 m_AllStepDict 中，若不解绑，m_TargetUI 未清、
        /// onClick 仍挂着 Complete —— 玩家点该 UI 会驱动「已取消的步骤」跳步。
        /// 因此这里做与 Clear() 完全相同的解绑（同一 Complete 方法组引用），并结束点击UI引导
        /// （取消在途打开 + 关闭已打开引导窗，见 GuideActionImpl.EndClickUIGuide）。
        /// Clear() 保留为回池兜底。
        /// </summary>
        protected override void OnCancel()
        {
            m_TargetUI?.onClick.Remove(Complete);
            m_TargetUI = null;
            GuideAction?.EndClickUIGuide();
            base.OnCancel();
        }

        /// <summary>
        /// 清理步骤。
        /// 覆写原因：基类 Clear() 只置空 StepInfo，不会移除 onClick 监听、也不会清空 m_TargetUI。
        /// 步骤被 Cancel 后回池（或直接 Clear 回收）时，池对象会长期持有旧界面的 GComponent（界面 onClick 仍挂着本步骤实例，
        /// 双向悬挂）；复用后 OnExecute 再次 Add 同一回调，同一次点击会触发两次 Complete。
        /// 此处用与 OnExecute 中 Add 完全相同的回调引用（Complete）做 Remove。
        /// </summary>
        public override void Clear()
        {
            m_TargetUI?.onClick.Remove(Complete);
            m_TargetUI = null;
            base.Clear();
        }

        /// <summary>
        /// 创建默认步骤实例
        /// </summary>
        /// <param name="stepInfo">步骤数据信息</param>
        /// <returns></returns>
        public static ClickUIStep Create(GuideStep stepInfo)
        {
            var step = ReferencePool.Acquire<ClickUIStep>();
            step.StepInfo = stepInfo;
            return step;
        }
    }
}
