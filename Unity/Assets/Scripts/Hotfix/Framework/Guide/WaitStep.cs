using Hotfix.Game.Config;
using Hotfix.Framework.Core;
namespace Hotfix.Framework.Guide
{
    /// <summary>
    /// 等待时间步骤
    /// </summary>
    public class WaitStep : BaseStep
    {
        /// <summary>
        /// 等待时间计时器。
        /// 功能：
        ///     1. 记录等待时间。
        ///     2. 等待时间到后，完成步骤。
        /// </summary>
        private float m_WaitTimer;

        protected override void OnExecute()
        {
            base.OnExecute();
            m_WaitTimer = 0f;
            GuideAction?.ShowGlobalMask(); // 打开全局遮罩窗口
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (IsExecuting)
            {
                m_WaitTimer += deltaTime;
                if (m_WaitTimer >= StepInfo.WaitTime)
                {
                    Complete();
                }
            }
        }

        protected override void OnComplete()
        {
            GuideAction?.HideGlobalMask(); // 隐藏全局遮罩窗口
            base.OnComplete();
        }

        /// <summary>
        /// 步骤取消。
        /// 覆写原因：遮罩由 OnExecute 打开，原实现只在 OnComplete 隐藏，而基类 Cancel() 只调 OnCancel()、
        /// 不经过 Clear()；SkipCurrentStep / JumpToStep / ForceNextStep / GoToPreviousStep / InterruptGuide 取消本步骤后，
        /// 遮罩会残留在屏幕上拦住所有点击。故取消路径做与 OnComplete 相同的隐藏（关闭不存在的窗口是幂等的）。
        /// </summary>
        protected override void OnCancel()
        {
            GuideAction?.HideGlobalMask(); // 隐藏全局遮罩窗口
            base.OnCancel();
        }

        /// <summary>
        /// 清理
        /// </summary>
        public override void Clear()
        {
            // 回池兜底：若该步未经 OnComplete/OnCancel 就被回收（如引导数据被杀），遮罩同样不能残留；
            // 与 OnCancel 一致地隐藏遮罩，保证「展示周期结束必关遮罩」这一不变量在三条路径上都成立。
            GuideAction?.HideGlobalMask(); // 隐藏全局遮罩窗口
            m_WaitTimer = 0f;
            base.Clear();
        }

        /// <summary>
        /// 创建等待时间步骤实例
        /// </summary>
        /// <param name="stepInfo">步骤数据信息</param>
        /// <returns></returns>
        public static WaitStep Create(GuideStep stepInfo)
        {
            var step = ReferencePool.Acquire<WaitStep>();
            step.StepInfo = stepInfo;
            return step;
        }
    }
}
