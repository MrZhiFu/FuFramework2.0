using UnityEngine;
using Hotfix.Game.UI;
using Hotfix.Game.Config;
using Hotfix.Game.Config.Tables;
using Hotfix.Game.Proto;
using Hotfix.Framework.Config;
using Hotfix.Framework.Guide;
using Hotfix.Framework.RedDot;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Network;
using Hotfix.Framework.Core;
using AOT.Launch;
using AOT.Framework.ModuleSetting.Runtime;
using AOT.Framework.Core.Utility;
using AOT.Framework.Core.Log;
using FairyGUI;
using UtilityAOT = AOT.Framework.Core.Utility.UtilityAOT;
using Hotfix.Framework.Asset;
using Hotfix.Framework.Timer;
using Hotfix.Framework.Mono;
using Hotfix.Framework.ReferencePool;
using Hotfix.Framework.ObjectPool;
using Hotfix.Framework.Event;
using Hotfix.Framework.FSM;

using Hotfix.Framework.Procedure;
using Hotfix.Framework.UI;
using Hotfix.Framework.Localization;
using Hotfix.Framework.Model;
using Hotfix.Framework.Scene;
using Hotfix.Framework.Storage;
using Hotfix.Framework.Sound;
using Hotfix.Framework.Web;
using Hotfix.Framework.Download;
using Hotfix.Framework.Entity;
using Utility = Hotfix.Framework.Core.Utility;

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
        /// 启动入口
        /// </summary>
        /// <param name="launchView">AOT 启动加载界面句柄，登录界面打开后关闭。</param>
        public static async UniTask MainAsync(ILaunchView launchView)
        {
            FuLogger.LogInfo("<color=#43f656>------热更逻辑完毕，进入热更后的代码逻辑入口------</color>");

            InitProto();
            SetupFairyGUI();
            RegisterBaseModules();
            HookGameDriven();

            // 配置表必须在依赖它的模块（Sound、Entity 等）注册前加载
            await InitFrameworkAsync(launchView);
            RegisterFeatureModules();
            EnterGame(launchView);
        }

        /// <summary>
        /// 初始化协议消息处理器
        /// </summary>
        private static void InitProto()
        {
            ProtoMessageIdHandler.Init(HotfixProtoHandler.CurrentAssembly);
        }

        /// <summary>
        /// 设置 FairyGUI 自定义加载器
        /// </summary>
        private static void SetupFairyGUI()
        {
            UIObjectFactory.SetLoaderExtension(typeof(CustomLoader));
        }

        /// <summary>
        /// 注册基础模块（配置表加载前，不依赖配置数据）
        /// </summary>
        private static void RegisterBaseModules()
        {
            ModuleManager.RegisterModule<ConfigModule>();
            ModuleManager.RegisterModule<ReferencePoolModule>();
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
        /// 注册功能模块（配置表加载后，可能依赖配置数据）
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
        /// 将 ModuleManager 生命周期方法挂接到 GameDriven 委托
        /// </summary>
        private static void HookGameDriven()
        {
            GameDriven.Instance.OnUpdate          = ModuleManager.Update;
            GameDriven.Instance.OnLateUpdate      = ModuleManager.LateUpdate;
            GameDriven.Instance.OnFixedUpdate     = ModuleManager.FixedUpdate;
            GameDriven.Instance.OnPerSecondUpdate = ModuleManager.PerSecondUpdate;
            GameDriven.Instance.DisposeModules    = ModuleManager.Dispose;
            GameDriven.Instance.ReInitModules     = ModuleManager.ReInit;
        }

        /// <summary>
        /// 初始化框架依赖：配置表、UI 资源、自定义组件绑定、多语言
        /// </summary>
        private static async UniTask InitFrameworkAsync(ILaunchView launchView)
        {
            launchView.SetTip("LoadConfig...");
            await LoadConfigAsync();

            launchView.SetTip("LoadInitUIAsset...");
            await LoadUIAsync();

            CustomCompBind.BindAll();

            LocalizationModule.Instance.LocalizationProvider = new LocalizationProvider();
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
            
            if (GameSetting.Instance.OpenGuide)
            {
                GuideModule.Instance.GuideAction = new GuideActionImpl();
                GuideModule.Instance.StartFirstGuide();
            }
        }

        /// <summary>
        /// 加载配置表
        /// </summary>
        private static async UniTask LoadConfigAsync()
        {
            var tableManager = new TableManager();
            tableManager.Init(ConfigModule.Instance);

#if ENABLE_BINARY_CONFIG
            // 使用二进制配置表
            await tableManager.LoadAsync(ConfigBufferLoader);
#else
            // 使用JSON配置表
            await tableManager.LoadAsync(ConfigLoader);
#endif
        }

        /// <summary>
        /// 加载初始必要的UI
        /// </summary>
        private static UniTask LoadUIAsync()
        {
            // 添加通用UI资源包
            return GlobalModule.UIModule.PkgManager.LoadPkgAsync("Common");
        }

#if ENABLE_BINARY_CONFIG
        /// <summary>
        /// 加载二进制配置表
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private static async Task<ByteBuf> ConfigBufferLoader(string file)
        {
            var configPath = UtilityAOT.AssetPath.GetConfigPath(file);
            var assetHandle = await GlobalModule.AssetModule.LoadAssetAsync<TextAsset>(configPath);
            var bytes      = assetHandle.GetAssetObject<TextAsset>().bytes;
            assetHandle.Release(); // 启动一次性加载，解析后释放句柄，避免 provider 引用残留
            return ByteBuf.Wrap(bytes);
        }
#else
        /// <summary>
        /// 加载json配置表
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private static async Task<JSONNode> ConfigLoader(string file)
        {
            var cfgPath     = UtilityAOT.AssetPath.GetConfigPath(file, ".json");
            var assetHandle = await GlobalModule.AssetModule.LoadAssetAsync<TextAsset>(cfgPath);
            var text        = assetHandle.GetAssetObject<TextAsset>().text;
            assetHandle.Release(); // 启动一次性加载，解析后释放句柄，避免 provider 引用残留
            return JSON.Parse(text);
        }
#endif
    }
}
