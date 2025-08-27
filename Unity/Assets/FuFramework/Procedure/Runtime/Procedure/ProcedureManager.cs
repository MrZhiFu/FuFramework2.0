using System;
using FuFramework.Core.Runtime;
using FuFramework.Fsm.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Procedure.Runtime
{
    /// <summary>
    /// 流程管理器。
    /// </summary>
    public sealed class ProcedureManager : FuModule, IProcedureManager
    {
        protected override int Priority => -2;

        private IFsmManager m_FsmManager; // 有限状态机管理器
        private IFsm<IProcedureManager> m_ProcedureFsm; // 流程管理器的有限状态机

        /// <summary>
        /// 初始化流程管理器的新实例。
        /// </summary>
        public ProcedureManager()
        {
            m_FsmManager = null;
            m_ProcedureFsm = null;
        }

        /// <summary>
        /// 获取当前流程。
        /// </summary>
        public ProcedureBase CurrentProcedure => m_ProcedureFsm?.CurrentStateBase as ProcedureBase;

        /// <summary>
        /// 获取当前流程持续时间。
        /// </summary>
        public float CurrentProcedureTime => m_ProcedureFsm?.CurrentStateTime ?? 0;

        /// <summary>
        /// 流程管理器轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        protected override void Update(float elapseSeconds, float realElapseSeconds) { }

        /// <summary>
        /// 关闭并清理流程管理器。
        /// </summary>
        protected override void Shutdown()
        {
            if (m_FsmManager == null) return;
            if (m_ProcedureFsm != null)
            {
                m_FsmManager.DestroyFsm(m_ProcedureFsm);
                m_ProcedureFsm = null;
            }

            m_FsmManager = null;
        }

        /// <summary>
        /// 初始化流程管理器。
        /// </summary>
        /// <param name="fsmManager">有限状态机管理器。</param>
        /// <param name="procedures">流程管理器包含的流程。</param>
        public void Initialize(IFsmManager fsmManager, params ProcedureBase[] procedures)
        {
            FuGuard.NotNull(fsmManager, nameof(fsmManager));
            m_FsmManager = fsmManager;
            
            // ReSharper disable once CoVariantArrayConversion
            m_ProcedureFsm = m_FsmManager.CreateFsm(this, procedures);
        }

        /// <summary>
        /// 开始流程。
        /// </summary>
        /// <typeparam name="T">要开始的流程类型。</typeparam>
        public void StartProcedure<T>() where T : ProcedureBase
        {
            if (m_ProcedureFsm == null) throw new FuException("You must initialize procedure first.");
            m_ProcedureFsm.Start<T>();
        }

        /// <summary>
        /// 开始流程。
        /// </summary>
        /// <param name="procedureType">要开始的流程类型。</param>
        public void StartProcedure(Type procedureType)
        {
            if (m_ProcedureFsm == null) throw new FuException("You must initialize procedure first.");
            m_ProcedureFsm.Start(procedureType);
        }

        /// <summary>
        /// 是否存在流程。
        /// </summary>
        /// <typeparam name="T">要检查的流程类型。</typeparam>
        /// <returns>是否存在流程。</returns>
        public bool HasProcedure<T>() where T : ProcedureBase
        {
            if (m_ProcedureFsm == null) throw new FuException("You must initialize procedure first.");
            return m_ProcedureFsm.HasState<T>();
        }

        /// <summary>
        /// 是否存在流程。
        /// </summary>
        /// <param name="procedureType">要检查的流程类型。</param>
        /// <returns>是否存在流程。</returns>
        public bool HasProcedure(Type procedureType)
        {
            if (m_ProcedureFsm == null) throw new FuException("You must initialize procedure first.");
            return m_ProcedureFsm.HasState(procedureType);
        }

        /// <summary>
        /// 获取流程。
        /// </summary>
        /// <typeparam name="T">要获取的流程类型。</typeparam>
        /// <returns>要获取的流程。</returns>
        public ProcedureBase GetProcedure<T>() where T : ProcedureBase
        {
            if (m_ProcedureFsm == null) throw new FuException("You must initialize procedure first.");
            return m_ProcedureFsm.GetState<T>();
        }

        /// <summary>
        /// 获取流程。
        /// </summary>
        /// <param name="procedureType">要获取的流程类型。</param>
        /// <returns>要获取的流程。</returns>
        public ProcedureBase GetProcedure(Type procedureType)
        {
            if (m_ProcedureFsm == null) throw new FuException("You must initialize procedure first.");
            return (ProcedureBase)m_ProcedureFsm.GetState(procedureType);
        }
    }
}