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
        /// 所属有限状态机。
        /// </summary>
        protected Fsm Fsm { get; private set; }

        /// <summary>
        /// 状态初始化
        /// </summary>
        /// <param name="fsm">有限状态机引用。</param>
        protected internal virtual void OnInit(Fsm fsm)
        {
            Fsm = fsm;
        }

        /// <summary>
        /// 状态进入
        /// </summary>
        protected internal virtual void OnEnter() { }

        /// <summary>
        /// 状态轮询
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        protected internal virtual void OnUpdate(float elapseSeconds, float realElapseSeconds) { }

        /// <summary>
        /// 状态离开
        /// </summary>
        /// <param name="isShutdown">是否是关闭有限状态机时触发。</param>
        protected internal virtual void OnLeave(bool isShutdown) { }

        /// <summary>
        /// 状态销毁
        /// </summary>
        protected internal virtual void OnDestroy() { }

        #endregion

        #region 切换状态

        /// <summary>
        /// 切换状态。
        /// </summary>
        /// <typeparam name="TState">要切换到的状态类型。</typeparam>
        protected void ChangeState<TState>() where TState : FsmStateBase
        {
            if (Fsm is null) throw new FuException("FSM is invalid.");
            Fsm.ChangeState<TState>();
        }

        /// <summary>
        /// 切换状态。
        /// </summary>
        /// <param name="state">要切换到的状态类型。</param>
        protected void ChangeState(Type state)
        {
            if (state == null) throw new FuException("State type is invalid.");

            if (!typeof(FsmStateBase).IsAssignableFrom(state))
                throw new FuException(Utility.Text.Format("State type '{0}' is invalid.", state.FullName));

            if (Fsm is null) throw new FuException("FSM is invalid.");
            Fsm.ChangeState(state);
        }

        #endregion
    }
}