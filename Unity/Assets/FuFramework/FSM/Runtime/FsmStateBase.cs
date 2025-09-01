using System;
using FuFramework.Core.Runtime;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable once CheckNamespace
namespace FuFramework.Fsm.Runtime
{
    /// <summary>
    /// 状态基类。
    /// 功能：定义有限状态机状态的基本接口。包括初始化、进入、轮询、离开、销毁生命周期，以及状态切换。
    /// </summary>
    public abstract class FsmStateBase
    {
        #region 生命周期

        /// <summary>
        /// 状态初始化
        /// </summary>
        /// <param name="fsm">有限状态机引用。</param>
        protected internal virtual void OnInit(Fsm fsm) { }

        /// <summary>
        /// 状态进入
        /// </summary>
        /// <param name="fsm">有限状态机引用。</param>
        protected internal virtual void OnEnter(Fsm fsm) { }

        /// <summary>
        /// 状态轮询
        /// </summary>
        /// <param name="fsm">有限状态机引用。</param>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        protected internal virtual void OnUpdate(Fsm fsm, float elapseSeconds, float realElapseSeconds) { }

        /// <summary>
        /// 状态离开
        /// </summary>
        /// <param name="fsm">有限状态机引用。</param>
        /// <param name="isShutdown">是否是关闭有限状态机时触发。</param>
        protected internal virtual void OnLeave(Fsm fsm, bool isShutdown) { }

        /// <summary>
        /// 状态销毁
        /// </summary>
        /// <param name="fsm">有限状态机引用。</param>
        protected internal virtual void OnDestroy(Fsm fsm) { }

        #endregion

        #region 切换状态

        /// <summary>
        /// 切换状态。
        /// </summary>
        /// <typeparam name="TState">要切换到的状态类型。</typeparam>
        /// <param name="fsm">有限状态机引用。</param>
        protected void ChangeState<TState>(Fsm fsm) where TState : FsmStateBase
        {
            if (fsm is null) throw new FuException("FSM is invalid.");
            fsm.ChangeState<TState>();
        }

        /// <summary>
        /// 切换状态。
        /// </summary>
        /// <param name="fsm">有限状态机引用。</param>
        /// <param name="state">要切换到的状态类型。</param>
        protected void ChangeState(Fsm fsm, Type state)
        {
            if (fsm is null) throw new FuException("FSM is invalid.");
            if (state == null) throw new FuException("State type is invalid.");

            if (!typeof(FsmStateBase).IsAssignableFrom(state))
                throw new FuException(Utility.Text.Format("State type '{0}' is invalid.", state.FullName));

            fsm.ChangeState(state);
        }

        #endregion
    }
}