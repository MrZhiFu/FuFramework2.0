using FuFramework.Fsm.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Procedure.Runtime
{
    /// <summary>
    /// 流程基类。
    /// 功能：继承自有限状态机基类，定义了流程的生命周期。可补充加入只属于流程的自定义逻辑。
    /// </summary>
    public abstract class ProcedureBase : FsmStateBase
    {
        /// <summary>
        /// 显示优先级。
        /// </summary>
        public virtual int Priority => 0;

        /// <summary>
        /// 状态初始化时调用。
        /// </summary>
        /// <param name="procedureOwner">流程持有者。</param>
        protected override void OnInit(Fsm.Runtime.Fsm procedureOwner)
        {
            base.OnInit(procedureOwner);
        }
    }
}