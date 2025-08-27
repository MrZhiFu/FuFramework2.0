using FuFramework.Fsm.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Procedure.Runtime
{
    /// <summary>
    /// 流程基类。
    /// 功能：继承自有限状态机基类，重新定义了流程的生命周期。
    /// </summary>
    public abstract class ProcedureBase : FsmStateBase<IProcedureManager>
    {
        /// <summary>
        /// 显示优先级。
        /// </summary>
        public virtual int Priority => 0;
        
        /// <summary>
        /// 状态初始化时调用。
        /// </summary>
        /// <param name="procedureOwner">流程持有者。</param>
        protected override void OnInit(IFsm<IProcedureManager> procedureOwner) => base.OnInit(procedureOwner);

        /// <summary>
        /// 进入状态时调用。
        /// </summary>
        /// <param name="procedureOwner">流程持有者。</param>
        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner) => base.OnEnter(procedureOwner);

        /// <summary>
        /// 状态轮询时调用。
        /// </summary>
        /// <param name="procedureOwner">流程持有者。</param>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds, float realElapseSeconds) =>
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

        /// <summary>
        /// 离开状态时调用。
        /// </summary>
        /// <param name="procedureOwner">流程持有者。</param>
        /// <param name="isShutdown">是否是关闭状态机时触发。</param>
        protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown) => base.OnLeave(procedureOwner, isShutdown);

        /// <summary>
        /// 状态销毁时调用。
        /// </summary>
        /// <param name="procedureOwner">流程持有者。</param>
        protected override void OnDestroy(IFsm<IProcedureManager> procedureOwner) => base.OnDestroy(procedureOwner);
    }
}