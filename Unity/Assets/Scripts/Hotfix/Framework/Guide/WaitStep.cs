using FuFramework.ReferencePool.Runtime;

using AOT.Framework.ModuleSetting.Runtime;
using AOT.Framework.ModuleSetting.Runtime.Guide;
namespace Hotfix.Guide
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
                if (m_WaitTimer >= StepInfo.m_WaitTime)
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
        /// 清理
        /// </summary>
        public override void Clear()
        {
            base.Clear();
            m_WaitTimer = 0f;
        }

        /// <summary>
        /// 创建等待时间步骤实例
        /// </summary>
        /// <param name="stepInfo">步骤数据信息</param>
        /// <returns></returns>
        public static WaitStep Create(StepInfo stepInfo)
        {
            var step = ReferencePool.Acquire<WaitStep>();
            step.StepInfo = stepInfo;
            return step;
        }
    }
}
