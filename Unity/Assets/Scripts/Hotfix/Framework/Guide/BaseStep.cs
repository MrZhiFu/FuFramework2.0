using Hotfix.Framework.Core;
using UnityEngine;

using Hotfix.Game.Config;
namespace Hotfix.Framework.Guide
{
    /// <summary>
    /// 步骤状态
    /// </summary>
    public enum EStepState
    {
        /// <summary>
        /// 空闲
        /// </summary>
        Idle,

        /// <summary>
        /// 执行中
        /// </summary>
        Executing,

        /// <summary>
        /// 已完成
        /// </summary>
        Completed,

        /// <summary>
        /// 被取消
        /// </summary>
        Cancelled,

        /// <summary>
        /// 执行失败
        /// </summary>
        Failed
    }

    /// <summary>
    /// 引导步骤基类
    /// </summary>
    public abstract class BaseStep : IReference
    {
        #region 属性

        /// <summary>
        /// 步骤数据
        /// </summary>
        public GuideStep StepInfo { get; protected set; }

        /// <summary>
        /// 步骤执行时间
        /// </summary>
        public float ExecutionTime { get; private set; }

        /// <summary>
        /// 步骤开始时间
        /// </summary>
        public float StartTime { get; private set; }

        /// <summary>
        /// 步骤状态
        /// </summary>
        public EStepState State { get; private set; } = EStepState.Idle;

        /// <summary>
        /// 步骤是否在执行中
        /// </summary>
        public bool IsExecuting => State == EStepState.Executing;

        /// <summary>
        /// 步骤是否已完成
        /// </summary>
        public bool IsCompleted => State == EStepState.Completed;

        /// <summary>
        /// 步骤执行动作对象
        /// </summary>
        public IGuideAction GuideAction => GuideModule.Instance.GuideAction;

        #endregion

        #region 公共方法

        /// <summary>
        /// 执行步骤
        /// </summary>
        public void Execute()
        {
            State         = EStepState.Executing;
            StartTime     = Time.time;
            ExecutionTime = 0f;
            OnExecute();
        }

        /// <summary>
        /// 步骤帧更新
        /// </summary>
        /// <param name="deltaTime"></param>
        public void Update(float deltaTime)
        {
            if (State == EStepState.Executing)
            {
                ExecutionTime += deltaTime;
            }

            OnUpdate(deltaTime);
        }

        /// <summary>
        /// 完成步骤
        /// </summary>
        public void Complete()
        {
            State = EStepState.Completed;
            OnComplete();

            // 执行下一个步骤
            if (StepInfo.NextStepId.HasValue)
                GuideModule.Instance.JumpToStep(StepInfo.NextStepId.Value);
        }

        /// <summary>
        /// 取消步骤。
        /// 已完成的步骤直接返回：Complete() 在置为 Completed 后会调用 JumpToStep(NextStepId)，
        /// 而 JumpToStep 会对「当前步」再调一次 Cancel()；若此处把 Completed 覆写为 Cancelled，
        /// 该步的 IsCompleted 将恒为 false，且会多触发一次 OnCancel（如 ClickUIStep 会多走一遍
        /// 解绑 / EndClickUIGuide）。取消语义只对「尚未完成」的步骤有意义。
        /// </summary>
        public void Cancel()
        {
            if (State == EStepState.Completed) return;

            State = EStepState.Cancelled;
            OnCancel();
        }

        #endregion

        #region 子类虚方法

        /// <summary>
        /// 执行开始处理
        /// </summary>
        protected virtual void OnExecute() { }

        /// <summary>
        /// 步骤更新（每帧调用）
        /// </summary>
        protected virtual void OnUpdate(float deltaTime) { }

        /// <summary>
        /// 步骤完成
        /// </summary>
        protected virtual void OnComplete() { }

        /// <summary>
        /// 步骤取消
        /// </summary>
        protected virtual void OnCancel() { }

        /// <summary>
        /// 检查步骤是否可以执行
        /// </summary>
        public virtual bool CanExecute() => true;

        /// <summary>
        /// 检查步骤是否可以完成
        /// </summary>
        public virtual bool CanComplete() => State == EStepState.Executing;

        /// <summary>
        /// 清理。
        /// 复位全部执行期字段（不只 StepInfo）：本类是引用池对象，Clear() 即回池清理点，
        /// 若只置空 StepInfo，残留的 State=Completed/Cancelled、StartTime、ExecutionTime 会被下一次
        /// Acquire 的实例继承（IsExecuting/IsCompleted 判定失真，完成耗时统计串味），故一并复位为初态。
        /// </summary>
        public virtual void Clear()
        {
            StepInfo      = null;
            State         = EStepState.Idle;
            StartTime     = 0f;
            ExecutionTime = 0f;
        }

        #endregion
    }
}
