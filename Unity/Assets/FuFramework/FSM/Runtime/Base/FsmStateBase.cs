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
    /// <typeparam name="T">有限状态机持有者类型。</typeparam>
    public abstract class FsmStateBase<T> where T : class
    {
        /// <summary>
        /// 状态初始化
        /// </summary>
        /// <param name="fsm">有限状态机引用。</param>
        protected internal virtual void OnInit(IFsm<T> fsm) { }

        /// <summary>
        /// 状态进入
        /// </summary>
        /// <param name="fsm">有限状态机引用。</param>
        protected internal virtual void OnEnter(IFsm<T> fsm) { }

        /// <summary>
        /// 状态轮询
        /// </summary>
        /// <param name="fsm">有限状态机引用。</param>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        protected internal virtual void OnUpdate(IFsm<T> fsm, float elapseSeconds, float realElapseSeconds) { }

        /// <summary>
        /// 状态离开
        /// </summary>
        /// <param name="fsm">有限状态机引用。</param>
        /// <param name="isShutdown">是否是关闭有限状态机时触发。</param>
        protected internal virtual void OnLeave(IFsm<T> fsm, bool isShutdown) { }

        /// <summary>
        /// 状态销毁
        /// </summary>
        /// <param name="fsm">有限状态机引用。</param>
        protected internal virtual void OnDestroy(IFsm<T> fsm) { }

        /// <summary>
        /// 切换状态。
        /// </summary>
        /// <typeparam name="TState">要切换到的状态类型。</typeparam>
        /// <param name="fsm">有限状态机引用。</param>
        public void ChangeToState<TState>(IFsm<T> fsm) where TState : FsmStateBase<T>
        {
            if (fsm is not Fsm<T> fsmImpl) throw new FuException("FSM is invalid.");
            fsmImpl.ChangeState<TState>();
        }

        /// <summary>
        /// 切换状态。
        /// </summary>
        /// <typeparam name="TState">要切换到的状态类型。</typeparam>
        /// <param name="fsm">有限状态机引用。</param>
        protected void ChangeState<TState>(IFsm<T> fsm) where TState : FsmStateBase<T>
        {
            if (fsm is not Fsm<T> fsmImpl) throw new FuException("FSM is invalid.");
            fsmImpl.ChangeState<TState>();
        }

        /// <summary>
        /// 切换状态。
        /// </summary>
        /// <param name="fsm">有限状态机引用。</param>
        /// <param name="stateType">要切换到的状态类型。</param>
        protected void ChangeState(IFsm<T> fsm, Type stateType)
        {
            if (fsm is not Fsm<T> fsmImpl) throw new FuException("FSM is invalid.");
            if (stateType == null) throw new FuException("State type is invalid.");

            if (!typeof(FsmStateBase<T>).IsAssignableFrom(stateType))
                throw new FuException(Utility.Text.Format("State type '{0}' is invalid.", stateType.FullName));

            fsmImpl.ChangeState(stateType);
        }
    }
}