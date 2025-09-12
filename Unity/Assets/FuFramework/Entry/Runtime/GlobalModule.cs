using FuFramework.Asset.Runtime;
using FuFramework.Config.Runtime;
using FuFramework.Core.Runtime;
using FuFramework.Coroutine.Runtime;
using FuFramework.Download.Runtime;
using FuFramework.Event.Runtime;
using FuFramework.Fsm.Runtime;
using FuFramework.GlobalConfig.Runtime;
using FuFramework.Localization.Runtime;
using FuFramework.Mono.Runtime;
using FuFramework.Network.Runtime;
using FuFramework.ObjectPool.Runtime;
using FuFramework.Procedure.Runtime;
using FuFramework.ReferencePool.Runtime;
using FuFramework.Setting.Runtime;
using FuFramework.Timer.Runtime;
using FuFramework.UI.Runtime;
using FuFramework.Web.Runtime;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable once CheckNamespace
namespace FuFramework.Entry.Runtime
{
    /// <summary>
    /// 全局模块类
    /// </summary>
    public static class GlobalModule
    {
        public static BaseComponent         BaseModule           { get; private set; }
        public static ReferencePoolManager  ReferencePoolManager { get; private set; }
        public static AssetManager          AssetModule          { get; private set; }
        public static ConfigManager         ConfigModule         { get; private set; }
        public static CoroutineManager      CoroutineModule      { get; private set; }
        public static DownloadManager       DownloadModule       { get; private set; }
        public static EventManager          EntityModule         { get; private set; }
        public static EventManager          EventModule          { get; private set; }
        public static FsmManager            FsmModule            { get; private set; }
        public static GlobalConfigManager   GlobalConfigModule   { get; private set; }
        public static LocalizationManager   LocalizationModule   { get; private set; }
        public static MonoManager           MonoModule           { get; private set; }
        public static UIManager             UIModule             { get; private set; }
        public static NetworkManager      NetworkModule        { get; private set; }
        public static ObjectPoolManager     ObjectPoolModule     { get; private set; }
        public static ProcedureManager      ProcedureModule      { get; private set; }
        public static SettingManager      SettingModule        { get; private set; }
        public static TimerManager          TimerModule          { get; private set; }
        public static WebManager          WebModule            { get; private set; }

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
            BaseModule           = ModuleManager.RegisterModule<BaseComponent>();
            ReferencePoolManager = ModuleManager.RegisterModule<ReferencePoolManager>();
            AssetModule          = ModuleManager.RegisterModule<AssetManager>();
            ConfigModule         = ModuleManager.RegisterModule<ConfigManager>();
            CoroutineModule      = ModuleManager.RegisterModule<CoroutineManager>();
            DownloadModule       = ModuleManager.RegisterModule<DownloadManager>();
            EntityModule         = ModuleManager.RegisterModule<EventManager>();
            EventModule          = ModuleManager.RegisterModule<EventManager>();
            FsmModule            = ModuleManager.RegisterModule<FsmManager>();
            GlobalConfigModule   = ModuleManager.RegisterModule<GlobalConfigManager>();
            LocalizationModule   = ModuleManager.RegisterModule<LocalizationManager>();
            MonoModule           = ModuleManager.RegisterModule<MonoManager>();
            UIModule             = ModuleManager.RegisterModule<UIManager>();
            NetworkModule        = ModuleManager.RegisterModule<NetworkManager>();
            ObjectPoolModule     = ModuleManager.RegisterModule<ObjectPoolManager>();
            ProcedureModule      = ModuleManager.RegisterModule<ProcedureManager>();
            SettingModule        = ModuleManager.RegisterModule<SettingManager>();
            TimerModule          = ModuleManager.RegisterModule<TimerManager>();
            WebModule            = ModuleManager.RegisterModule<WebManager>();

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