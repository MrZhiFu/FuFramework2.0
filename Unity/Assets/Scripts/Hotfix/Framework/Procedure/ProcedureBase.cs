using Hotfix.Framework.FSM;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Procedure
{
    /// <summary>
    /// 流程基类。
    /// 功能：
    ///     1. 继承自有限状态机基类，定义了流程的生命周期。可补充加入只属于流程的自定义逻辑。
    /// </summary>
    public abstract class ProcedureBase : FsmStateBase
    {
#if UNITY_EDITOR
        /// <summary>
        /// 在Inspector中的显示优先级。
        /// </summary>
        public virtual int Priority => 0;
#endif

        /// <summary>
        /// 状态初始化时调用。
        /// </summary>
        /// <param name="procedureOwner">流程持有者。</param>
        // 注意：FsmStateBase.OnInit 为 protected internal；FSM 与 Procedure 现同属 Hotfix 程序集，
        // 同程序集重写须保留 internal（写成 protected 会触发 CS0507），请勿改为 protected override。
        protected internal override void OnInit(Fsm procedureOwner) => base.OnInit(procedureOwner);
    }
}
