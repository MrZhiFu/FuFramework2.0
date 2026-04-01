using System;
using System.Collections;
using UnityEngine;
using FuFramework.Core.Runtime;
using FuFramework.Procedure.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entry.Runtime
{
    /// <summary>
    /// 入口类，用于启动游戏
    /// </summary>
    public class Launcher : MonoBehaviour
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
        /// 初始化
        /// </summary>
        private void Awake()
        {
            // 初始化模块管理器
            ModuleManager.Initialize();

            // 初始化启动流程
            StartCoroutine(InitProcedures());
        }

        /// <summary>
        /// 帧更新
        /// </summary>
        private void Update()
        {
            ModuleManager.Update(Time.deltaTime, Time.unscaledDeltaTime);
        }

        /// <summary>
        /// 延迟帧更新
        /// </summary>
        private void LateUpdate()
        {
            ModuleManager.LateUpdate(Time.deltaTime, Time.unscaledDeltaTime);
        }

        /// <summary>
        /// 固定帧更新
        /// </summary>
        private void FixedUpdate()
        {
            ModuleManager.FixedUpdate();
        }

        /// <summary>
        /// 初始化获取所有流程
        /// </summary>
        /// <returns></returns>
        /// <exception cref="FuException"></exception>
        private IEnumerator InitProcedures()
        {
            m_Procedures     = new ProcedureBase[m_AvailableProcedureTypeNames.Length];
            m_EntryProcedure = null;
            for (var i = 0; i < m_AvailableProcedureTypeNames.Length; i++)
            {
                var procedureType = Utility.Assembly.GetType(m_AvailableProcedureTypeNames[i]);
                if (procedureType is null)
                {
                    FuLogger.LogError($"[ProcedureModule] 找不到流程类型 '{m_AvailableProcedureTypeNames[i]}'.");
                    yield break;
                }

                m_Procedures[i] = Activator.CreateInstance(procedureType) as ProcedureBase;
                if (m_Procedures[i] is null)
                {
                    FuLogger.LogError($"[ProcedureModule] 创建流程实例'{m_AvailableProcedureTypeNames[i]}' 失败.");
                    yield break;
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
                yield break;
            }

            if (m_Procedures is null || m_Procedures.Length == 0)
                throw new FuException("[ProcedureModule] 必须至少有一个流程!");

            var states = new ProcedureBase[m_Procedures.Length];
            for (var i = 0; i < m_Procedures.Length; i++)
            {
                states[i] = m_Procedures[i];
            }

            // 初始化流程管理器
            var procedureModule = ModuleManager.GetModule<ProcedureModule>();
            procedureModule.InitProcedures(states);

            yield return new WaitForEndOfFrame();

            // 启动入口流程
            procedureModule.StartProcedure(m_EntryProcedure.GetType());
        }
    }
}