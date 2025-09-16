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
        public static BaseComponent         BaseModule           => ModuleManager.Instance.GetModule<BaseComponent>();
        public static ReferencePoolManager  ReferencePoolManager => ModuleManager.Instance.GetModule<ReferencePoolManager>();
        public static AssetManager          AssetModule          => ModuleManager.Instance.GetModule<AssetManager>();
        public static ConfigManager         ConfigModule         => ModuleManager.Instance.GetModule<ConfigManager>();
        public static CoroutineManager      CoroutineModule      => ModuleManager.Instance.GetModule<CoroutineManager>();
        public static DownloadManager       DownloadModule       => ModuleManager.Instance.GetModule<DownloadManager>();
        public static EntityManager          EntityModule        => ModuleManager.Instance.GetModule<EntityManager>();
        public static EventManager          EventModule          => ModuleManager.Instance.GetModule<EventManager>();
        public static FsmManager            FsmModule            => ModuleManager.Instance.GetModule<FsmManager>();
        public static GlobalConfigManager   GlobalConfigModule   => ModuleManager.Instance.GetModule<GlobalConfigManager>();
        public static LocalizationManager   LocalizationModule   => ModuleManager.Instance.GetModule<LocalizationManager>();
        public static MonoManager           MonoModule           => ModuleManager.Instance.GetModule<MonoManager>();
        public static UIManager             UIModule             => ModuleManager.Instance.GetModule<UIManager>();
        public static NetworkManager         NetworkModule       => ModuleManager.Instance.GetModule<NetworkManager>();
        public static ObjectPoolManager     ObjectPoolModule     => ModuleManager.Instance.GetModule<ObjectPoolManager>();
        public static ProcedureManager      ProcedureModule      => ModuleManager.Instance.GetModule<ProcedureManager>();
        public static SettingManager      SettingModule          => ModuleManager.Instance.GetModule<SettingManager>();
        public static TimerManager          TimerModule          => ModuleManager.Instance.GetModule<TimerManager>();
        public static WebManager          WebModule              => ModuleManager.Instance.GetModule<WebManager>();
        public static GameSceneManager      SceneModule          => ModuleManager.Instance.GetModule<GameSceneManager>();
        public static SoundManager SoundModule                   => ModuleManager.Instance.GetModule<SoundManager>();
        
        // private static RedDotManager RedDotModule => ModuleManager.GetModule<RedDotManager>();
        // private static AdvertisementManager AdvertisementModule => ModuleManager.GetModule<AdvertisementManager>();
        // private static GameAnalyticsManager GameAnalyticsModule => ModuleManager.GetModule<GameAnalyticsManager>();
    }
}