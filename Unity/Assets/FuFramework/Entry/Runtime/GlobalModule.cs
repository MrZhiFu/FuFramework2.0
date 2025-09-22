using FuFramework.Asset.Runtime;
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
using FuFramework.ObjectPool.Runtime;
using FuFramework.Procedure.Runtime;
using FuFramework.ReferencePool.Runtime;
using FuFramework.Scene.Runtime;
using FuFramework.Setting.Runtime;
using FuFramework.Sound.Runtime;
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
        public static ReferencePoolManager  ReferencePoolManager => ModuleManager.GetModule<ReferencePoolManager>();
        public static AssetManager          AssetModule          => ModuleManager.GetModule<AssetManager>();
        public static ConfigManager         ConfigModule         => ModuleManager.GetModule<ConfigManager>();
        public static CoroutineManager      CoroutineModule      => ModuleManager.GetModule<CoroutineManager>();
        public static DownloadManager       DownloadModule       => ModuleManager.GetModule<DownloadManager>();
        public static EntityManager          EntityModule        => ModuleManager.GetModule<EntityManager>();
        public static EventManager          EventModule          => ModuleManager.GetModule<EventManager>();
        public static FsmManager            FsmModule            => ModuleManager.GetModule<FsmManager>();
        public static GlobalConfigManager   GlobalConfigModule   => ModuleManager.GetModule<GlobalConfigManager>();
        public static LocalizationManager   LocalizationModule   => ModuleManager.GetModule<LocalizationManager>();
        public static MonoManager           MonoModule           => ModuleManager.GetModule<MonoManager>();
        public static UIManager             UIModule             => ModuleManager.GetModule<UIManager>();
        public static NetworkManager         NetworkModule       => ModuleManager.GetModule<NetworkManager>();
        public static ObjectPoolManager     ObjectPoolModule     => ModuleManager.GetModule<ObjectPoolManager>();
        public static ProcedureManager      ProcedureModule      => ModuleManager.GetModule<ProcedureManager>();
        public static SettingManager      SettingModule          => ModuleManager.GetModule<SettingManager>();
        public static TimerManager          TimerModule          => ModuleManager.GetModule<TimerManager>();
        public static WebManager          WebModule              => ModuleManager.GetModule<WebManager>();
        public static GameSceneManager      SceneModule          => ModuleManager.GetModule<GameSceneManager>();
        public static SoundManager SoundModule                   => ModuleManager.GetModule<SoundManager>();
        
        // private static RedDotManager RedDotModule => ModuleManager.GetModule<RedDotManager>();
        // private static AdvertisementManager AdvertisementModule => ModuleManager.GetModule<AdvertisementManager>();
        // private static GameAnalyticsManager GameAnalyticsModule => ModuleManager.GetModule<GameAnalyticsManager>();
    }
}