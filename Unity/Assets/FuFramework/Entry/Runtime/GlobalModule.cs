using FuFramework.UI.Runtime;
using FuFramework.Web.Runtime;
using FuFramework.Fsm.Runtime;
using FuFramework.Mono.Runtime;
using FuFramework.Core.Runtime;
using FuFramework.Scene.Runtime;
using FuFramework.Event.Runtime;
using FuFramework.Sound.Runtime;
using FuFramework.Timer.Runtime;
using FuFramework.Asset.Runtime;
using FuFramework.Config.Runtime;
using FuFramework.Entity.Runtime;
using FuFramework.Network.Runtime;
using FuFramework.Setting.Runtime;
using FuFramework.Download.Runtime;
using FuFramework.Procedure.Runtime;
using FuFramework.Coroutine.Runtime;
using FuFramework.ObjectPool.Runtime;
using FuFramework.GlobalConfig.Runtime;
using FuFramework.Localization.Runtime;
using FuFramework.ReferencePool.Runtime;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable once CheckNamespace
namespace FuFramework.Entry.Runtime
{
    /// <summary>
    /// 全局模块类。
    /// 功能：提供对各个框架模块的访问入口。
    /// </summary>
    public static class GlobalModule
    {
        /// <summary>
        /// 获取引用池模块。
        /// </summary>
        public static ReferencePoolManager ReferencePoolModule => ModuleManager.GetModule<ReferencePoolManager>();

        /// <summary>
        /// 获取对象池模块。
        /// </summary>
        public static ObjectPoolManager ObjectPoolModule => ModuleManager.GetModule<ObjectPoolManager>();

        /// <summary>
        /// 获取事件管理模块。
        /// </summary>
        public static EventManager EventModule => ModuleManager.GetModule<EventManager>();

        /// <summary>
        /// 获取资源管理模块。
        /// </summary>
        public static AssetManager AssetModule => ModuleManager.GetModule<AssetManager>();

        /// <summary>
        /// 获取配置管理模块。
        /// </summary>
        public static ConfigManager ConfigModule => ModuleManager.GetModule<ConfigManager>();

        /// <summary>
        /// 获取协程管理模块。
        /// </summary>
        public static CoroutineManager CoroutineModule => ModuleManager.GetModule<CoroutineManager>();

        /// <summary>
        /// 获取定时器管理模块。
        /// </summary>
        public static TimerManager TimerModule => ModuleManager.GetModule<TimerManager>();

        /// <summary>
        /// 获取下载管理模块。
        /// </summary>
        public static DownloadManager DownloadModule => ModuleManager.GetModule<DownloadManager>();

        /// <summary>
        /// 获取实体管理模块。
        /// </summary>
        public static EntityManager EntityModule => ModuleManager.GetModule<EntityManager>();

        /// <summary>
        /// 获取有限状态机管理模块。
        /// </summary>
        public static FsmManager FsmModule => ModuleManager.GetModule<FsmManager>();

        /// <summary>
        /// 获取流程管理模块。
        /// </summary>
        public static ProcedureManager ProcedureModule => ModuleManager.GetModule<ProcedureManager>();

        /// <summary>
        /// 获取UI管理模块。
        /// </summary>
        public static UIManager UIModule => ModuleManager.GetModule<UIManager>();

        /// <summary>
        /// 获取Fui包管理模块。
        /// </summary>
        public static FuiPackageManager FuiPackageManagerModule => ModuleManager.GetModule<FuiPackageManager>();

        /// <summary>
        /// 获取服务器相关全局配置管理模块。
        /// </summary>
        public static GlobalConfigManager GlobalConfigModule => ModuleManager.GetModule<GlobalConfigManager>();

        /// <summary>
        /// 获取本地化管理模块。
        /// </summary>
        public static LocalizationManager LocalizationModule => ModuleManager.GetModule<LocalizationManager>();

        /// <summary>
        /// 获取Mono管理模块。
        /// </summary>
        public static MonoManager MonoModule => ModuleManager.GetModule<MonoManager>();

        /// <summary>
        /// 获取场景管理模块。
        /// </summary>
        public static GameSceneManager SceneModule => ModuleManager.GetModule<GameSceneManager>();

        /// <summary>
        /// 获取声音管理模块。
        /// </summary>
        public static SoundManager SoundModule => ModuleManager.GetModule<SoundManager>();

        /// <summary>
        /// 获取网络管理模块。
        /// </summary>
        public static NetworkManager NetworkModule => ModuleManager.GetModule<NetworkManager>();

        /// <summary>
        /// 获取Web管理模块。
        /// </summary>
        public static WebManager WebModule => ModuleManager.GetModule<WebManager>();

        /// <summary>
        /// 获取本地持久化管理模块。
        /// </summary>
        public static SettingManager SettingModule => ModuleManager.GetModule<SettingManager>();

        ///// <summary>
        ///// 获取红点管理模块。
        ///// </summary>
        // private static RedDotManager RedDotModule => ModuleManager.GetModule<RedDotManager>();

        ///// <summary>
        ///// 获取广告管理模块。
        ///// </summary>
        // private static AdvertisementManager AdvertisementModule => ModuleManager.GetModule<AdvertisementManager>();

        ///// <summary>
        ///// 获取游戏分析管理模块。
        ///// </summary>
        // private static GameAnalyticsManager GameAnalyticsModule => ModuleManager.GetModule<GameAnalyticsManager>();
    }
}