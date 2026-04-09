using System;
using System.Collections;
using FuFramework.Asset.Runtime;
using FuFramework.Config.Runtime;
using UnityEngine;
using FuFramework.Core.Runtime;
using FuFramework.Coroutine.Runtime;
using FuFramework.Download.Runtime;
using FuFramework.Entity.Runtime;
using FuFramework.Event.Runtime;
using FuFramework.Fsm.Runtime;
using FuFramework.GlobalConfig.Runtime;
using FuFramework.Guide.Runtime;
using FuFramework.Localization.Runtime;
using FuFramework.Model.Runtime;
using FuFramework.Mono.Runtime;
using FuFramework.Network.Runtime;
using FuFramework.ObjectPool.Runtime;
using FuFramework.Procedure.Runtime;
using FuFramework.RedDot.Runtime;
using FuFramework.ReferencePool.Runtime;
using FuFramework.SaveData.Runtime;
using FuFramework.Scene.Runtime;
using FuFramework.Sound.Runtime;
using FuFramework.Timer.Runtime;
using FuFramework.UI.Runtime;
using FuFramework.Web.Runtime;
using UnityEngine.SceneManagement;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entry.Runtime
{
    /// <summary>
    /// 入口类。
    /// 1. 启动游戏
    /// 2. 驱动框架生命周期
    /// </summary>
    public class Launcher : MonoBehaviour
    {
        /// <summary>
        /// 游戏框架所在的场景编号。
        /// </summary>
        private const int FrameworkSceneId = 0;

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
            // 注册框架各个模块
            RegisterModules();

            // 初始化启动流程
            StartCoroutine(InitProcedures());
        }


        /// <summary>
        /// 注册框架各个模块
        /// 注意：注册顺序不可修改，防止某些模块依赖于其他模块时出错。
        /// </summary>
        private void RegisterModules()
        {
            ModuleManager.RegisterModule<ReferencePoolModule>(); // 引用池管理模块
            ModuleManager.RegisterModule<ObjectPoolModule>();    // 对象池管理模块
            ModuleManager.RegisterModule<FsmModule>();           // 有限状态机管理模块
            ModuleManager.RegisterModule<ProcedureModule>();     // 流程管理模块
            ModuleManager.RegisterModule<EventModule>();         // 事件管理模块
            ModuleManager.RegisterModule<CoroutineModule>();     // 协程管理模块
            ModuleManager.RegisterModule<MonoModule>();          // Mono管理模块
            ModuleManager.RegisterModule<TimerModule>();         // 计时器管理模块
            ModuleManager.RegisterModule<AssetModule>();         // 资源管理模块
            ModuleManager.RegisterModule<DownloadModule>();      // 下载管理模块
            ModuleManager.RegisterModule<DataSaveModule>();      // 本地存储数据管理模块

            ModuleManager.RegisterModule<GlobalConfigModule>(); // 全局配置管理模块
            ModuleManager.RegisterModule<ConfigModule>();       // 配置管理模块
            ModuleManager.RegisterModule<SceneModule>();        // 场景管理模块
            ModuleManager.RegisterModule<SoundModule>();        // 声音管理模块
            ModuleManager.RegisterModule<EntityModule>();       // 实体管理模块
            ModuleManager.RegisterModule<NetworkModule>();      // 网络管理模块
            ModuleManager.RegisterModule<UIModule>();           // UI管理模块
            ModuleManager.RegisterModule<GuideModule>();        // 红点管理模块
            ModuleManager.RegisterModule<RedDotModule>();       // 引导管理模块
            ModuleManager.RegisterModule<LocalizationModule>(); // 本地化管理模块
            ModuleManager.RegisterModule<ModelModule>();        // 数据模型管理模块
            ModuleManager.RegisterModule<WebModule>();          // Web管理模块
        }

        /// <summary>
        /// 帧更新
        /// </summary>
        private void Update()
        {
            ModuleManager.OnUpdate(Time.deltaTime, Time.unscaledDeltaTime);
        }

        /// <summary>
        /// 延迟帧更新
        /// </summary>
        private void LateUpdate()
        {
            ModuleManager.OnLateUpdate(Time.deltaTime, Time.unscaledDeltaTime);
        }

        /// <summary>
        /// 固定帧更新
        /// </summary>
        private void FixedUpdate()
        {
            ModuleManager.OnFixedUpdate();
        }

        /// <summary>
        /// 对象销毁
        /// </summary>
        private void OnDestroy()
        {
            ModuleManager.OnDestroy();
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

            // 初始化流程管理模块
            var procedureModule = ModuleManager.GetModule<ProcedureModule>();
            procedureModule.InitProcedures(states);

            yield return new WaitForEndOfFrame();

            // 启动入口流程
            procedureModule.StartProcedure(m_EntryProcedure.GetType());
        }


        /// <summary>
        /// 重启游戏(如设置界面重启)
        /// </summary>
        public static void Restart()
        {
            SceneManager.LoadScene(FrameworkSceneId);
        }

        /// <summary>
        /// 退出游戏
        /// </summary>
        public static void Quit()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}