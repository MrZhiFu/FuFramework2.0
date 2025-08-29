using FuFramework.Config.Runtime;
using FuFramework.Core.Runtime;
using FuFramework.Coroutine.Runtime;
using FuFramework.Download.Runtime;
using FuFramework.Entity.Runtime;
using FuFramework.Event.Runtime;
using FuFramework.Fsm.Runtime;
using FuFramework.GlobalConfig.Runtime;
using FuFramework.Localization.Runtime;
using FuFramework.Mono.Runtime;
using FuFramework.Network.Runtime;
using FuFramework.Procedure.Runtime;
using FuFramework.Setting.Runtime;
using FuFramework.Timer.Runtime;
using FuFramework.Web.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entry.Runtime
{
    /// <summary>
    /// 全局模块类
    /// </summary>
    public static class GlobalModule
    {
        public static BaseComponent         BaseModule         { get; private set; }
        public static ConfigComponent       ConfigModule       { get; private set; }
        public static CoroutineComponent    CoroutineModule    { get; private set; }
        public static DownloadComponent     DownloadModule     { get; private set; }
        public static EntityComponent       EntityModule       { get; private set; }
        public static EventManager          EventModule        { get; private set; }
        public static FsmComponent          FsmModule          { get; private set; }
        public static GlobalConfigComponent GlobalConfigModule { get; private set; }
        public static LocalizationComponent LocalizationModule { get; private set; }
        public static MonoManager           MonoModule         { get; private set; }
        public static NetworkComponent      NetworkModule      { get; private set; }
        public static ObjectPoolComponent   ObjectPoolModule   { get; private set; }
        public static ProcedureComponent    ProcedureModule    { get; private set; }
        public static SettingComponent      SettingModule      { get; private set; }
        public static TimerComponent        TimerModule        { get; private set; }
        public static WebComponent          WebModule          { get; private set; }

        // private static SceneComponent SceneModule;
        // private static AdvertisementComponent AdvertisementModule;
        // private static GameAnalyticsComponent GameAnalyticsModule;
        // private static AssetComponent AssetModule;
        // private static RedDotComponent RedDotModule;
        // private static SoundComponent SoundModule;
        // private static UIComponent UiModule;

        /// <summary>
        /// 注册所有模块
        /// </summary>
        public static void RegisterModule()
        {
            BaseModule         = ModuleManager.RegisterModule<BaseComponent>();
            ConfigModule       = ModuleManager.RegisterModule<ConfigComponent>();
            CoroutineModule    = ModuleManager.RegisterModule<CoroutineComponent>();
            DownloadModule     = ModuleManager.RegisterModule<DownloadComponent>();
            EntityModule       = ModuleManager.RegisterModule<EntityComponent>();
            EventModule        = ModuleManager.RegisterModule<EventManager>();
            FsmModule          = ModuleManager.RegisterModule<FsmComponent>();
            GlobalConfigModule = ModuleManager.RegisterModule<GlobalConfigComponent>();
            LocalizationModule = ModuleManager.RegisterModule<LocalizationComponent>();
            MonoModule         = ModuleManager.RegisterModule<MonoManager>();
            NetworkModule      = ModuleManager.RegisterModule<NetworkComponent>();
            ObjectPoolModule   = ModuleManager.RegisterModule<ObjectPoolComponent>();
            ProcedureModule    = ModuleManager.RegisterModule<ProcedureComponent>();
            SettingModule      = ModuleManager.RegisterModule<SettingComponent>();
            TimerModule        = ModuleManager.RegisterModule<TimerComponent>();
            WebModule          = ModuleManager.RegisterModule<WebComponent>();

            // SceneModule = GameEntry.RegisterComponent<SceneComponent>();
            // AdvertisementModule = GameEntry.RegisterComponent<AdvertisementComponent>();
            // GameAnalyticsModule = GameEntry.RegisterComponent<GameAnalyticsComponent>();
            // AssetModule = GameEntry.RegisterComponent<AssetComponent>();
            // RedDotModule = GameEntry.RegisterComponent<RedDotComponent>();
            // SoundModule = GameEntry.RegisterComponent<SoundComponent>();
            // UiModule = GameEntry.RegisterComponent<UIComponent>();
        }

        /// <summary>
        /// 初始化所有模块
        /// </summary>
        public static void InitModule()
        {
            ModuleManager.Init();
        }

        /// <summary>
        /// 更新所有模块
        /// </summary>
        public static void UpdateModule(float elapseSeconds, float realElapseSeconds)
        {
            ModuleManager.Update(elapseSeconds, realElapseSeconds);
        }
    }
}