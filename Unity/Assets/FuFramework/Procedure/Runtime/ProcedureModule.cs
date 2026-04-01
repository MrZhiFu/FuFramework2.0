using System;
using FuFramework.Fsm.Runtime;
using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Procedure.Runtime
{
    /// <summary>
    /// 流程管理器。
    /// </summary>
    [ModuleDependency(typeof(FsmModule))]
    public sealed class ProcedureModule : FuModule
    {
        /// <summary>
        /// 游戏框架模块优先级。
        /// </summary>
        /// <remarks>优先级较高的模块会优先轮询，并且关闭操作会后进行。</remarks>
        protected override int Priority => ModulePriority.Game;
        
        /// <summary>
        /// 有限状态机管理器
        /// </summary>
        private FsmModule m_FsmModule;

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
            m_FsmModule = ModuleManager.GetModule<FsmModule>();
            if (!m_FsmModule) throw new FuException("[ProcedureModule] 有限状态机管理器不能为空");
        }

        /// <summary>
        /// 关闭并清理游戏框架模块。
        /// </summary>
        protected override void OnDispose()
        {
            if (!m_FsmModule) return;

            if (m_ProcedureFsm != null)
            {
                m_FsmModule.DestroyFsm(m_ProcedureFsm);
                m_ProcedureFsm = null;
            }

            m_FsmModule = null;
        }

        /// <summary>
        /// 初始化流程状态机。
        /// </summary>
        /// <param name="procedure"></param>
        /// <exception cref="FuException"></exception>
        public void InitProcedures(ProcedureBase[] procedure)
        {
            if (!m_FsmModule) throw new FuException("[ProcedureModule] 有限状态机管理器不能为空");
            
            // ReSharper disable once CoVariantArrayConversion
            m_ProcedureFsm ??= m_FsmModule.CreateFsm(this, procedure);
            if (m_ProcedureFsm == null) throw new FuException("[ProcedureModule] 创建流程管理器失败.");
        }

        /// <summary>
        /// 开始流程。
        /// </summary>
        /// <typeparam name="T">要开始的流程类型。</typeparam>
        public void StartProcedure<T>() where T : ProcedureBase
        {
            if (m_ProcedureFsm == null) throw new FuException("[ProcedureModule] 流程管理器尚未初始化.");
            m_ProcedureFsm.Start<T>();
        }

        /// <summary>
        /// 开始流程。
        /// </summary>
        /// <param name="procedureType">要开始的流程类型。</param>
        public void StartProcedure(Type procedureType)
        {
            if (m_ProcedureFsm == null) throw new FuException("[ProcedureModule] 流程管理器尚未初始化.");
            m_ProcedureFsm.Start(procedureType);
        }

        /// <summary>
        /// 是否存在流程。
        /// </summary>
        /// <typeparam name="T">要检查的流程类型。</typeparam>
        /// <returns>是否存在流程。</returns>
        public bool HasProcedure<T>() where T : ProcedureBase
        {
            if (m_ProcedureFsm == null) throw new FuException("[ProcedureModule] 流程管理器尚未初始化.");
            return m_ProcedureFsm.HasState<T>();
        }

        /// <summary>
        /// 是否存在流程。
        /// </summary>
        /// <param name="procedureType">要检查的流程类型。</param>
        /// <returns>是否存在流程。</returns>
        public bool HasProcedure(Type procedureType)
        {
            if (m_ProcedureFsm == null) throw new FuException("[ProcedureModule] 流程管理器尚未初始化.");
            return m_ProcedureFsm.HasState(procedureType);
        }

        /// <summary>
        /// 获取流程。
        /// </summary>
        /// <typeparam name="T">要获取的流程类型。</typeparam>
        /// <returns>要获取的流程。</returns>
        public ProcedureBase GetProcedure<T>() where T : ProcedureBase
        {
            if (m_ProcedureFsm == null) throw new FuException("[ProcedureModule] 流程管理器尚未初始化.");
            return m_ProcedureFsm.GetState<T>();
        }

        /// <summary>
        /// 获取流程。
        /// </summary>
        /// <param name="procedureType">要获取的流程类型。</param>
        /// <returns>要获取的流程。</returns>
        public ProcedureBase GetProcedure(Type procedureType)
        {
            if (m_ProcedureFsm == null) throw new FuException("[ProcedureModule] 流程管理器尚未初始化.");
            return (ProcedureBase)m_ProcedureFsm.GetState(procedureType);
        }
    }
}