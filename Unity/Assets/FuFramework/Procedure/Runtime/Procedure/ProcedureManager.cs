using System;
using System.Collections;
using UnityEngine;
using FuFramework.Core.Runtime;
using FuFramework.Fsm.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Procedure.Runtime
{
    /// <summary>
    /// 流程管理器。
    /// </summary>
    public sealed class ProcedureManager : FuComponent
    {
        /// <summary>
        /// 游戏框架模块优先级。
        /// </summary>
        /// <remarks>优先级较高的模块会优先轮询，并且关闭操作会后进行。</remarks>
        protected override int Priority => -2;

        /// <summary>
        /// 有限状态机管理器
        /// </summary>
        private FsmManager m_FsmManager;

        /// <summary>
        /// 流程管理器的有限状态机
        /// </summary>
        private Fsm.Runtime.Fsm m_ProcedureFsm;

        [Header("所有可用的流程类型")]
        [SerializeField] private string[] m_AvailableProcedureTypeNames;

        [Header("入口流程类型")]
        [SerializeField] private string m_EntranceProcedureTypeName;

        /// <summary>
        /// 流程管理器包含的流程
        /// </summary>
        private ProcedureBase[] m_Procedures;

        /// <summary>
        /// 入口流程。
        /// </summary>
        private ProcedureBase m_EntranceProcedure;
        

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
            m_FsmManager = ModuleManager.GetModule<FsmManager>();
            if (!m_FsmManager) throw new FuException("You must add FsmManager module to your project.");

            // 初始化所有流程
            StartCoroutine(InitProcedures());
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
        /// 初始化获取所有流程
        /// </summary>
        /// <returns></returns>
        /// <exception cref="FuException"></exception>
        private IEnumerator InitProcedures()
        {
            var procedures = new ProcedureBase[m_AvailableProcedureTypeNames.Length];
            for (var i = 0; i < m_AvailableProcedureTypeNames.Length; i++)
            {
                var procedureType = Utility.Assembly.GetType(m_AvailableProcedureTypeNames[i]);
                if (procedureType == null)
                {
                    Log.Error("找不到流程类型 '{0}'.", m_AvailableProcedureTypeNames[i]);
                    yield break;
                }

                procedures[i] = (ProcedureBase)Activator.CreateInstance(procedureType);
                if (procedures[i] == null)
                {
                    Log.Error("创建流程实例失败 '{0}'.", m_AvailableProcedureTypeNames[i]);
                    yield break;
                }

                if (m_EntranceProcedureTypeName == m_AvailableProcedureTypeNames[i])
                {
                    m_EntranceProcedure = procedures[i];
                }
            }

            if (m_EntranceProcedure == null)
            {
                Log.Error("入口流程类型不存在!.");
                yield break;
            }

            if (m_Procedures == null || m_Procedures.Length == 0) throw new FuException("You must add at least one procedure to ProcedureManager.");

            // ReSharper disable once CoVariantArrayConversion
            m_ProcedureFsm = m_FsmManager.CreateFsm(this, m_Procedures);

            // 启动入口流程
            yield return new WaitForEndOfFrame();
            StartProcedure(m_EntranceProcedure.GetType());
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