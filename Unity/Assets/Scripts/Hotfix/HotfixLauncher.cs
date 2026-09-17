using FairyGUI;
using UnityEngine;
using Cysharp.Threading.Tasks;
using AOT.Launch;
using AOT.Framework.ModuleSetting.Runtime;
using AOT.Framework.Core.Log;
using AOT.Framework.Core.Utility;
using AOT.Launch.Localization;
using Hotfix.Framework.Config;
using Hotfix.Framework.Guide;
using Hotfix.Framework.RedDot;
using Hotfix.Framework.Network;
using Hotfix.Framework.Core;
using Hotfix.Framework.Asset;
using Hotfix.Framework.Timer;
using Hotfix.Framework.Mono;
using Hotfix.Framework.Event;
using Hotfix.Framework.FSM;
using Hotfix.Framework.UI;
using Hotfix.Framework.Procedure;
using Hotfix.Framework.ObjectPool;
using Hotfix.Framework.Localization;
using Hotfix.Framework.Model;
using Hotfix.Framework.Scene;
using Hotfix.Framework.Storage;
using Hotfix.Framework.Sound;
using Hotfix.Framework.Web;
using Hotfix.Framework.Download;
using Hotfix.Framework.Entity;
using Hotfix.Game.UI;
using Hotfix.Game.Config;
using AOT.Launch.Localization;

#if ENABLE_BINARY_CONFIG
using Luban;
#else
using SimpleJSON;
#endif

namespace Hotfix
{
    /// <summary>
    /// 热更代码启动入口
    /// </summary>
    public static class HotfixLauncher
    {
        /// <summary>
        /// 表管理器实例（语言切换刷新用，每次启动/热重启由 LoadConfigAsync 覆盖）
        /// </summary>
        private static TableManager m_TableManager;

        /// <summary>
        /// 启动入口
        /// </summary>
        /// <param name="launchView">AOT 启动加载界面句柄，登录界面打开后关闭。</param>
        public static async UniTask MainAsync(ILaunchView launchView)
        {
            FuLogger.LogInfo("<color=#43f656>------热更逻辑完毕，进入热更后的代码逻辑入口------</color>");

            // 初始化协议消息处理器：
            InitProto();

            // 初始化 FairyGUI相关：注册自定义加载器（CustomLoader）与自定义组件绑定（CustomCompBind）。
            InitFGUI();

            // 将 ModuleManager 生命周期方法挂接到 GameDriven 委托
            HookGameDriven();

            // 注册基础模块(不依赖配置数据)
            RegisterBaseModules();

            // 配置表必须在依赖它的模块（Sound、Entity 等）初始化前加载
            await InitDependenciesAsync(launchView);

            // 功能模块：首次启动注册 / 重启重新初始化（配置已加载）
            RegisterFeatureModules();

            // 进入游戏
            EnterGame(launchView);
        }

        /// <summary>
        /// 初始化协议消息处理器：
        /// </summary>
        private static void InitProto()
        {
            ProtoMessageIdHandler.Init();
        }

        /// <summary>
        /// 初始化 FairyGUI相关：注册自定义加载器（CustomLoader）与自定义组件绑定（CustomCompBind）。
        /// </summary>
        private static void InitFGUI()
        {
            UIObjectFactory.SetLoaderExtension(typeof(CustomLoader));
            CustomCompBind.BindAll();
        }

        /// <summary>
        /// 将 ModuleManager 生命周期方法挂接到 GameDriven 委托
        /// </summary>
        private static void HookGameDriven()
        {
            GameDriven.Instance.OnUpdate          = ModuleManager.Update;
            GameDriven.Instance.OnLateUpdate      = ModuleManager.LateUpdate;
            GameDriven.Instance.OnFixedUpdate     = ModuleManager.FixedUpdate;
            GameDriven.Instance.OnPerSecondUpdate = ModuleManager.PerSecondUpdate;
            GameDriven.Instance.DisposeModules    = ModuleManager.Dispose;
        }

        /// <summary>
        /// 注册基础模块（配置表加载前，不依赖配置数据）。
        /// </summary>
        private static void RegisterBaseModules()
        {
            ModuleManager.RegisterModule<ConfigModule>();
            ModuleManager.RegisterModule<FsmModule>();
            ModuleManager.RegisterModule<ProcedureModule>();
            ModuleManager.RegisterModule<EventModule>();
            ModuleManager.RegisterModule<ObjectPoolModule>();
            ModuleManager.RegisterModule<MonoModule>();
            ModuleManager.RegisterModule<TimerModule>();
            ModuleManager.RegisterModule<AssetModule>();
            ModuleManager.RegisterModule<UIModule>();
            ModuleManager.RegisterModule<StorageModule>();
            ModuleManager.RegisterModule<LocalizationModule>();
        }

        /// <summary>
        /// 注册功能模块（配置表加载后，可能依赖配置数据）。
        /// </summary>
        private static void RegisterFeatureModules()
        {
            ModuleManager.RegisterModule<RedDotModule>();
            ModuleManager.RegisterModule<GuideModule>();
            ModuleManager.RegisterModule<ModelModule>();
            ModuleManager.RegisterModule<SceneModule>();
            ModuleManager.RegisterModule<SoundModule>();
            ModuleManager.RegisterModule<EntityModule>();
            ModuleManager.RegisterModule<DownloadModule>();
            ModuleManager.RegisterModule<WebModule>();
            ModuleManager.RegisterModule<NetworkModule>();
        }

        /// <summary>
        /// 初始化框架依赖：加载配置表与公共 UI 资源包、设置多语言 Provider。
        /// 须在依赖配置数据的功能模块（Sound/Entity 等）注册或重新初始化之前调用。
        /// </summary>
        private static async UniTask InitDependenciesAsync(ILaunchView launchView)
        {
            // 加载配置表
            launchView.SetTip(LaunchLocalization.GetLanguage(LaunchL10nKey.aot_res_loading_config));
            var tableManager = await LoadConfigAsync();

            // 加载初始必要的公共UI资源包
            launchView.SetTip(LaunchLocalization.GetLanguage(LaunchL10nKey.aot_res_loading_init_res));
            await LoadCommonUIAsync();

            // 设置本地化多语言提供者
            LocalizationModule.Instance.LocalizationProvider = new LocalizationProvider();

            // 多语言提供者就绪后翻译配置表
            tableManager.RefreshTranslateText();
        }

        /// <summary>
        /// 进入游戏：打开登录界面，关闭启动界面，启动新手引导
        /// </summary>
        private static void EnterGame(ILaunchView launchView)
        {
            GlobalModule.UIModule.Open<WinLogin>();
            launchView.Close();

            // 卸载Launcher界面资源包, 防止Launcher包常驻内存
            UIPackage.RemovePackage("UI/Launcher");

            // 引导
            if (GameSetting.Instance.OpenGuide)
            {
                GuideModule.Instance.GuideAction = new GuideActionImpl();
                GuideModule.Instance.StartFirstGuide();
            }
        }

        /// <summary>
        /// 加载配置表
        /// </summary>
        /// <returns>加载完成的表管理器实例</returns>
        private static async UniTask<TableManager> LoadConfigAsync()
        {
            // 热重启：退订旧实例的语言切换事件，避免重复刷新与委托引用导致旧实例无法回收
            m_TableManager?.UnsubscribeLanguageChange();

            var tableManager = new TableManager();
            tableManager.Init(ConfigModule.Instance);

#if ENABLE_BINARY_CONFIG
            // 使用二进制配置表
            await tableManager.LoadAsync(ConfigBufferLoader);
#else
            // 使用JSON配置表
            await tableManager.LoadAsync(ConfigLoader);
#endif

            // 订阅语言切换事件并登记实例（退订/覆盖次序固定，勿在别处重复登记）
            tableManager.SubscribeLanguageChange();
            m_TableManager = tableManager;
            return tableManager;
        }

        /// <summary>
        /// 加载初始必要的公共UI资源包
        /// </summary>
        private static UniTask LoadCommonUIAsync()
        {
            return GlobalModule.UIModule.PkgManager.LoadPkgAsync("Common");
        }

#if ENABLE_BINARY_CONFIG
        /// <summary>
        /// 加载二进制配置表
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private static async UniTask<ByteBuf> ConfigBufferLoader(string file)
        {
            var configPath = UtilityAOT.AssetPath.GetConfigPath(file);
            var assetHandle = await GlobalModule.AssetModule.LoadAssetAsync<TextAsset>(configPath, GlobalModule.AssetModule.Token);
            try
            {
                var textAsset = assetHandle.GetAssetObject<TextAsset>();
                if (textAsset == null)
                    throw new System.InvalidOperationException($"[HotfixLauncher] 配置文件加载失败：{configPath}");
                return ByteBuf.Wrap(textAsset.bytes);
            }
            finally
            {
                // 启动一次性加载，解析后释放句柄，避免 provider 引用残留
                // AutoUnload=false 下显式卸载，否则配置 bundle 常驻内存
                assetHandle.Release(); 
                GlobalModule.AssetModule.UnloadAsset(configPath);
            }
        }
#else
        /// <summary>
        /// 加载json配置表
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private static async UniTask<JSONNode> ConfigLoader(string file)
        {
            var cfgPath     = UtilityAOT.AssetPath.GetConfigPath(file, ".json");
            var assetHandle = await GlobalModule.AssetModule.LoadAssetAsync<TextAsset>(cfgPath, GlobalModule.AssetModule.Token);
            try
            {
                var textAsset = assetHandle.GetAssetObject<TextAsset>();
                if (textAsset == null)
                    throw new System.InvalidOperationException($"[HotfixLauncher] 配置文件加载失败：{cfgPath}");
                return JSON.Parse(textAsset.text);
            }
            finally
            {
                // 解析后释放句柄，避免 provider 引用残留
                // AutoUnload=false 下显式卸载，否则配置 bundle 常驻内存
                assetHandle.Release();
                GlobalModule.AssetModule.UnloadAsset(cfgPath);
            }
        }
#endif
    }
}