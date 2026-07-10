using FuFramework.UI.Runtime;
using FuFramework.Web.Runtime;
using FuFramework.Fsm.Runtime;
using FuFramework.Mono.Runtime;
using FuFramework.Core.Runtime;
using FuFramework.Event.Runtime;
using FuFramework.Sound.Runtime;
using FuFramework.Timer.Runtime;
using FuFramework.Asset.Runtime;
using FuFramework.Entity.Runtime;
using FuFramework.Network.Runtime;
using FuFramework.Download.Runtime;
using FuFramework.Procedure.Runtime;

using FuFramework.ObjectPool.Runtime;
using FuFramework.ReferencePool.Runtime;
using FuFramework.SaveData.Runtime;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable once CheckNamespace
namespace FuFramework.Launcher.Runtime
{
    /// <summary>
    /// 全局模块类。
    /// 功能：
    ///     1. 提供各个框架模块的访问入口，用于在热更代码中通过此类来访问各个模块的接口。
    /// </summary>
    public static class GlobalModule
    {
        private static ReferencePoolModule m_ReferencePoolModule; // 引用池模块
        private static ObjectPoolModule    m_ObjectPoolModule;    // 对象池模块
        private static EventModule         m_EventModule;         // 事件管理模块
        private static AssetModule         m_AssetModule;         // 资源管理模块

        private static TimerModule         m_TimerModule;         // 计时器管理模块
        private static DownloadModule      m_DownloadModule;      // 下载管理模块
        private static EntityModule        m_EntityModule;        // 实体管理模块
        private static FsmModule           m_FsmModule;           // 有限状态机管理模块
        private static ProcedureModule     m_ProcedureModule;     // 流程管理模块
        private static UIModule            m_UIModule;            // UI管理模块
        private static MonoModule          m_MonoModule;          // Mono管理模块
        private static SoundModule         m_SoundModule;         // 声音管理模块
        private static NetworkModule       m_NetworkModule;       // 网络管理模块
        private static WebModule           m_WebModule;           // Web管理模块
        private static StorageModule       m_StorageModule;       // 本地持久化管理模块
        // private static AdvertisementModule   m_AdvertisementModule;   // TODO 广告管理模块
        // private static GameAnalyticsModule   m_GameAnalyticsModule;   // TODO 游戏分析管理模块

        /// <summary>
        /// 获取引用池模块。
        /// </summary>
        public static ReferencePoolModule ReferencePoolModule => m_ReferencePoolModule ??= ModuleManager.GetModule<ReferencePoolModule>();

        /// <summary>
        /// 获取对象池模块。
        /// </summary>
        public static ObjectPoolModule ObjectPoolModule => m_ObjectPoolModule ??= ModuleManager.GetModule<ObjectPoolModule>();

        /// <summary>
        /// 获取事件管理模块。
        /// </summary>
        public static EventModule EventModule => m_EventModule ??= ModuleManager.GetModule<EventModule>();

        /// <summary>
        /// 获取资源管理模块。
        /// </summary>
        public static AssetModule AssetModule => m_AssetModule ??= ModuleManager.GetModule<AssetModule>();



        /// <summary>
        /// 获取计时器管理模块。
        /// </summary>
        public static TimerModule TimerModule => m_TimerModule ??= ModuleManager.GetModule<TimerModule>();

        /// <summary>
        /// 获取下载管理模块。
        /// </summary>
        public static DownloadModule DownloadModule => m_DownloadModule ??= ModuleManager.GetModule<DownloadModule>();

        /// <summary>
        /// 获取实体管理模块。
        /// </summary>
        public static EntityModule EntityModule => m_EntityModule ??= ModuleManager.GetModule<EntityModule>();

        /// <summary>
        /// 获取有限状态机管理模块。
        /// </summary>
        public static FsmModule FsmModule => m_FsmModule ??= ModuleManager.GetModule<FsmModule>();

        /// <summary>
        /// 获取流程管理模块。
        /// </summary>
        public static ProcedureModule ProcedureModule => m_ProcedureModule ??= ModuleManager.GetModule<ProcedureModule>();

        /// <summary>
        /// 获取UI管理模块。
        /// </summary>
        public static UIModule UIModule => m_UIModule ??= ModuleManager.GetModule<UIModule>();

        /// <summary>
        /// 获取Mono管理模块。
        /// </summary>
        public static MonoModule MonoModule => m_MonoModule ??= ModuleManager.GetModule<MonoModule>();

        /// <summary>
        /// 获取声音管理模块。
        /// </summary>
        public static SoundModule SoundModule => m_SoundModule ??= ModuleManager.GetModule<SoundModule>();

        /// <summary>
        /// 获取网络管理模块。
        /// </summary>
        public static NetworkModule NetworkModule => m_NetworkModule ??= ModuleManager.GetModule<NetworkModule>();

        /// <summary>
        /// 获取Web管理模块。
        /// </summary>
        public static WebModule WebModule => m_WebModule ??= ModuleManager.GetModule<WebModule>();

        /// <summary>
        /// 获取本地持久化管理模块。
        /// </summary>
        public static StorageModule StorageModule => m_StorageModule ??= ModuleManager.GetModule<StorageModule>();

        ///// <summary>
        ///// 获取广告管理模块。// TODO
        ///// </summary>
        // private static AdvertisementModule AdvertisementModule => m_AdvertisementModule ??= ModuleManager.GetModule<AdvertisementModule>();

        ///// <summary>
        ///// 获取游戏分析管理模块。// TODO
        ///// </summary>
        // private static GameAnalyticsModule GameAnalyticsModule => m_GameAnalyticsModule ?? ModuleManager.GetModule<GameAnalyticsModule>();
    }
}