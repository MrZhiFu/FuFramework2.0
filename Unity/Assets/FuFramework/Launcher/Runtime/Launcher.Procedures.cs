using System;
using UnityEngine;
using FuFramework.Core.Runtime;
using FuFramework.Procedure.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Launcher.Runtime
{
    /// <summary>
    /// Launcher 流程管理部分
    /// </summary>
    public partial class Launcher
    {
        [Header("所有可用的流程类型")]
        [SerializeField] private string[] m_AvailableProcedureTypeNames;

        [Header("入口流程类型")]
        [SerializeField] private string m_EntryProcedureTypeName;

        /// <summary>
        /// 所有可用的流程类型
        /// </summary>
        private ProcedureBase[] m_Procedures;

        /// <summary>
        /// 入口流程
        /// </summary>
        private ProcedureBase m_EntryProcedure;

        /// <summary>
        /// 获取当前流程
        /// </summary>
        public ProcedureBase CurrentProcedure => ModuleManager.GetModule<ProcedureModule>()?.CurrentProcedure ?? m_EntryProcedure;

        /// <summary>
        /// 开始游戏流程
        /// </summary>
        private void StartProcedure()
        {
            // 获取所有流程
            var states = GetProcedures();

            // 初始化流程管理模块
            var procedureModule = ModuleManager.GetModule<ProcedureModule>();
            procedureModule.InitProcedures(states);

            // 启动入口流程
            procedureModule.StartProcedure(m_EntryProcedure.GetType());
        }

        /// <summary>
        /// 获取所有启动流程
        /// </summary>
        /// <returns></returns>
        private ProcedureBase[] GetProcedures()
        {
            m_Procedures     = new ProcedureBase[m_AvailableProcedureTypeNames.Length];
            m_EntryProcedure = null;

            for (var i = 0; i < m_AvailableProcedureTypeNames.Length; i++)
            {
                var procedureType = Utility.Assembly.GetType(m_AvailableProcedureTypeNames[i]);
                if (procedureType is null)
                {
                    FuLogger.LogError($"[ProcedureModule] 找不到流程类型 '{m_AvailableProcedureTypeNames[i]}'.");
                    return null;
                }

                m_Procedures[i] = Activator.CreateInstance(procedureType) as ProcedureBase;
                if (m_Procedures[i] is null)
                {
                    FuLogger.LogError($"[ProcedureModule] 创建流程实例'{m_AvailableProcedureTypeNames[i]}' 失败.");
                    return null;
                }

                // 设置入口流程
                if (m_EntryProcedureTypeName == m_AvailableProcedureTypeNames[i])
                {
                    m_EntryProcedure = m_Procedures[i];
                }
            }

            if (m_EntryProcedure is null)
            {
                FuLogger.LogError("[ProcedureModule] 入口流程类型不存在!.");
                return null;
            }

            if (m_Procedures is null || m_Procedures.Length == 0)
            {
                FuLogger.LogError("[ProcedureModule] 必须至少有一个流程!.");
                return null;
            }

            var states = new ProcedureBase[m_Procedures.Length];
            for (var i = 0; i < m_Procedures.Length; i++)
            {
                states[i] = m_Procedures[i];
            }

            return states;
        }
    }
}