using System;
using System.Collections;
using FuFramework.Core.Runtime;
using FuFramework.Fsm.Runtime;
using UnityEngine;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace FuFramework.Procedure.Runtime
{
    /// <summary>
    /// 流程组件。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/Procedure")]
    public sealed class ProcedureComponent : FuComponent
    {
        private IProcedureManager m_ProcedureManager;  // 流程管理器。
        private ProcedureBase     m_EntranceProcedure; // 入口流程。

        [Header("所有可用的流程类型")]
        [SerializeField] private string[] m_AvailableProcedureTypeNames;

        [Header("入口流程类型")]
        [SerializeField] private string m_EntranceProcedureTypeName;

        /// <summary>
        /// 获取流程管理器。
        /// </summary>
        public IProcedureManager Procedure => m_ProcedureManager;

        /// <summary>
        /// 获取当前流程。
        /// </summary>
        public ProcedureBase CurrentProcedure => m_ProcedureManager.CurrentProcedure;

        /// <summary>
        /// 获取当前流程持续时间。
        /// </summary>
        public float CurrentProcedureTime => m_ProcedureManager.CurrentProcedureTime;

        protected override void OnInit()
        {
            m_ProcedureManager = FuEntry.GetModule<IProcedureManager>();
            if (m_ProcedureManager == null) Log.Fatal("Procedure manager is invalid.");
            StartCoroutine(InitProcedures());
        }
        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
        }
        protected override void OnShutdown(ShutdownType shutdownType)
        {
        }
        
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

            m_ProcedureManager.Initialize(FuEntry.GetModule<IFsmManager>(), procedures);
            yield return new WaitForEndOfFrame();
            m_ProcedureManager.StartProcedure(m_EntranceProcedure.GetType());
        }

        /// <summary>
        /// 是否存在流程。
        /// </summary>
        /// <typeparam name="T">要检查的流程类型。</typeparam>
        /// <returns>是否存在流程。</returns>
        public bool HasProcedure<T>() where T : ProcedureBase => m_ProcedureManager.HasProcedure<T>();

        /// <summary>
        /// 是否存在流程。
        /// </summary>
        /// <param name="procedureType">要检查的流程类型。</param>
        /// <returns>是否存在流程。</returns>
        public bool HasProcedure(Type procedureType) => m_ProcedureManager.HasProcedure(procedureType);

        /// <summary>
        /// 获取流程。
        /// </summary>
        /// <typeparam name="T">要获取的流程类型。</typeparam>
        /// <returns>要获取的流程。</returns>
        public ProcedureBase GetProcedure<T>() where T : ProcedureBase => m_ProcedureManager.GetProcedure<T>();

        /// <summary>
        /// 获取流程。
        /// </summary>
        /// <param name="procedureType">要获取的流程类型。</param>
        /// <returns>要获取的流程。</returns>
        public ProcedureBase GetProcedure(Type procedureType) => m_ProcedureManager.GetProcedure(procedureType);
    }
}