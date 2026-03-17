using FuFramework.Core.Runtime;
using FuFramework.ModuleSetting.Runtime;
using FuFramework.ReferencePool.Runtime;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Guide.Runtime
{
    /// <summary>
    /// 步骤状态
    /// </summary>
    public enum StepState
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
        public StepInfo StepInfo { get; protected set; }

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
        public StepState State { get; private set; } = StepState.Idle;

        /// <summary>
        /// 步骤是否在执行中
        /// </summary>
        public bool IsExecuting => State == StepState.Executing;

        /// <summary>
        /// 步骤是否已完成
        /// </summary>
        public bool IsCompleted => State == StepState.Completed;

        /// <summary>
        /// 步骤执行动作对象
        /// </summary>
        public IGuideAction GuideAction => ModuleManager.GetModule<GuideManager>().GuideAction;

        #endregion

        #region 公共方法

        /// <summary>
        /// 执行步骤
        /// </summary>
        public void Execute()
        {
            State         = StepState.Executing;
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
            if (State == StepState.Executing)
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
            State = StepState.Completed;
            OnComplete();
            
            // 执行下一个步骤
            if (!string.IsNullOrEmpty(StepInfo.m_NextStepId))
                ModuleManager.GetModule<GuideManager>().JumpToStep(StepInfo.m_NextStepId);
        }

        /// <summary>
        /// 取消步骤
        /// </summary>
        public void Cancel()
        {
            State = StepState.Cancelled;
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
        /// 步骤失败
        /// </summary>
        protected virtual void OnFail(string reason = "")
        {
            State = StepState.Failed;
            FuLogger.LogWarning($"引导步骤{StepInfo.m_StepId}执行失败，失败原因：{reason}");
        }

        /// <summary>
        /// 检查步骤是否可以执行
        /// </summary>
        public virtual bool CanExecute() => true;

        /// <summary>
        /// 检查步骤是否可以完成
        /// </summary>
        public virtual bool CanComplete() => State == StepState.Executing;

        /// <summary>
        /// 清理
        /// </summary>
        public virtual void Clear() => StepInfo = null;

        #endregion
    }
}