using System;
using FuFramework.Fsm.Runtime;
using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Procedure.Runtime
{
    /// <summary>
    /// 流程管理器。
    /// </summary>
    [ModuleDependency(typeof(FsmManager))]
    public sealed class ProcedureManager : FuModule
    {
        /// <summary>
        /// 游戏框架模块优先级。
        /// </summary>
        /// <remarks>优先级较高的模块会优先轮询，并且关闭操作会后进行。</remarks>
        protected override int Priority => ModulePriority.Game;
        
        /// <summary>
        /// 有限状态机管理器
        /// </summary>
        private FsmManager m_FsmManager;

        /// <summary>
        /// 流程管理器的有限状态机
        /// </summary>
        private Fsm.Runtime.Fsm m_ProcedureFsm;

        /// <summary>
        /// 获取当前流程。
        /// </summary>
        public ProcedureBase CurrentProcedure => m_ProcedureFsm?.CurrentStateBase as ProcedureBase;

        /// <summary>
        /// 获取当前流程持续时间。
        /// </summary>
        public float CurrentProcedureTime => m_ProcedureFsm?.CurrentStateTime ?? 0;

        /// <summary>
        /// 初始化
        /// </summary>
        protected override void OnInit()
        {
            m_FsmManager = ModuleManager.Instance.GetModule<FsmManager>();
            if (!m_FsmManager) throw new FuException("[ProcedureManager] 有限状态机管理器不能为空");
        }

        /// <summary>
        /// 关闭并清理游戏框架模块。
        /// </summary>
        /// <param name="shutdownType"></param>
        protected override void OnShutdown(ShutdownType shutdownType)
        {
            if (!m_FsmManager) return;

            if (m_ProcedureFsm != null)
            {
                m_FsmManager.DestroyFsm(m_ProcedureFsm);
                m_ProcedureFsm = null;
            }

            m_FsmManager = null;
        }

        /// <summary>
        /// 初始化流程状态机。
        /// </summary>
        /// <param name="procedure"></param>
        /// <exception cref="FuException"></exception>
        public void InitProcedures(ProcedureBase[] procedure)
        {
            if (!m_FsmManager) throw new FuException("[ProcedureManager] 有限状态机管理器不能为空");
            
            // ReSharper disable once CoVariantArrayConversion
            m_ProcedureFsm ??= m_FsmManager.CreateFsm(this, procedure);
            if (m_ProcedureFsm == null) throw new FuException("[ProcedureManager] 创建流程管理器失败.");
        }

        /// <summary>
        /// 开始流程。
        /// </summary>
        /// <typeparam name="T">要开始的流程类型。</typeparam>
        public void StartProcedure<T>() where T : ProcedureBase
        {
            if (m_ProcedureFsm == null) throw new FuException("[ProcedureManager] 流程管理器尚未初始化.");
            m_ProcedureFsm.Start<T>();
        }

        /// <summary>
        /// 开始流程。
        /// </summary>
        /// <param name="procedureType">要开始的流程类型。</param>
        public void StartProcedure(Type procedureType)
        {
            if (m_ProcedureFsm == null) throw new FuException("[ProcedureManager] 流程管理器尚未初始化.");
            m_ProcedureFsm.Start(procedureType);
        }

        /// <summary>
        /// 是否存在流程。
        /// </summary>
        /// <typeparam name="T">要检查的流程类型。</typeparam>
        /// <returns>是否存在流程。</returns>
        public bool HasProcedure<T>() where T : ProcedureBase
        {
            if (m_ProcedureFsm == null) throw new FuException("[ProcedureManager] 流程管理器尚未初始化.");
            return m_ProcedureFsm.HasState<T>();
        }

        /// <summary>
        /// 是否存在流程。
        /// </summary>
        /// <param name="procedureType">要检查的流程类型。</param>
        /// <returns>是否存在流程。</returns>
        public bool HasProcedure(Type procedureType)
        {
            if (m_ProcedureFsm == null) throw new FuException("[ProcedureManager] 流程管理器尚未初始化.");
            return m_ProcedureFsm.HasState(procedureType);
        }

        /// <summary>
        /// 获取流程。
        /// </summary>
        /// <typeparam name="T">要获取的流程类型。</typeparam>
        /// <returns>要获取的流程。</returns>
        public ProcedureBase GetProcedure<T>() where T : ProcedureBase
        {
            if (m_ProcedureFsm == null) throw new FuException("[ProcedureManager] 流程管理器尚未初始化.");
            return m_ProcedureFsm.GetState<T>();
        }

        /// <summary>
        /// 获取流程。
        /// </summary>
        /// <param name="procedureType">要获取的流程类型。</param>
        /// <returns>要获取的流程。</returns>
        public ProcedureBase GetProcedure(Type procedureType)
        {
            if (m_ProcedureFsm == null) throw new FuException("[ProcedureManager] 流程管理器尚未初始化.");
            return (ProcedureBase)m_ProcedureFsm.GetState(procedureType);
        }
    }
}