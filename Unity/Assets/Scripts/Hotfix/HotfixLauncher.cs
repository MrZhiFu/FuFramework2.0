using UnityEngine;
using Hotfix.Proto;
using Hotfix.Config;
using Hotfix.ModuleConfig;
using Hotfix.UI;
using Hotfix.Guide;
using Hotfix.RedDot;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Hotfix.Network;
using FuFramework.Core.Runtime;
using FuFramework.Asset.Runtime;
using FuFramework.Timer.Runtime;
using FuFramework.Mono.Runtime;
using FuFramework.ReferencePool.Runtime;
using FuFramework.ObjectPool.Runtime;
using FuFramework.Event.Runtime;
using FuFramework.Fsm.Runtime;

using FuFramework.Procedure.Runtime;
using FuFramework.UI.Runtime;
using FuFramework.ModuleSetting.Runtime;
using Hotfix.Localization;
using Hotfix.Model;
using Hotfix.Scene;
using Hotfix.Storage;
using Hotfix.Sound;
using Hotfix.Web;
using Hotfix.Download;
using Hotfix.Entity;
using Utility = FuFramework.Core.Runtime.Utility;

#if ENABLE_BINARY_CONFIG
using Luban;
#else
using SimpleJSON;
#endif

namespace Hotfix
{
    /// <summary>
    /// 热更代码入口
    /// </summary>
    public static class HotfixLauncher
    {
        /// <summary>
        /// 启动入口
        /// </summary>
        /// <param name="bootstrapView">AOT 引导加载界面句柄，登录界面打开后关闭。</param>
        public static async UniTask MainAsync(IBootstrapView bootstrapView)
        {
            FuLogger.LogInfo("<color=#43f656>------热更逻辑完毕，进入热更后的代码逻辑入口------</color>");

            InitProto();
            SetupFairyGUI();
            RegisterModules();
            HookGameDriven();

            await InitFrameworkAsync(bootstrapView);
            EnterGame(bootstrapView);
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
            FairyGUI.UIObjectFactory.SetLoaderExtension(typeof(CustomLoader));
        }

        /// <summary>
        /// 注册所有框架模块（按依赖顺序）
        /// </summary>
        private static void RegisterModules()
        {
            // 基础模块
            ModuleManager.RegisterModule<ReferencePoolModule>();
            ModuleManager.RegisterModule<FsmModule>();
            ModuleManager.RegisterModule<ProcedureModule>();
            ModuleManager.RegisterModule<EventModule>();
            ModuleManager.RegisterModule<ObjectPoolModule>();
            ModuleManager.RegisterModule<MonoModule>();
            ModuleManager.RegisterModule<TimerModule>();
            ModuleManager.RegisterModule<AssetModule>();
            ModuleManager.RegisterModule<UIModule>();
            ModuleManager.RegisterModule<RedDotModule>();

            // 功能模块
            ModuleManager.RegisterModule<GuideModule>();
            ModuleManager.RegisterModule<StorageModule>();
            ModuleManager.RegisterModule<LocalizationModule>();
            ModuleManager.RegisterModule<ModelModule>();
            ModuleManager.RegisterModule<ConfigModule>();
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
        private static async UniTask InitFrameworkAsync(IBootstrapView bootstrapView)
        {
            bootstrapView.SetTip("LoadConfig...");
            await LoadConfigAsync();

            bootstrapView.SetTip("LoadInitUIAsset...");
            await LoadUIAsync();

            CustomCompBind.BindAll();

            LocalizationModule.Instance.LocalizationProvider = new LocalizationProvider();
        }

        /// <summary>
        /// 进入游戏：打开登录界面，关闭引导界面，启动新手引导
        /// </summary>
        private static void EnterGame(IBootstrapView bootstrapView)
        {
            GlobalModule.UIModule.OpenUI<WinLogin>();
            bootstrapView.Close();

            if (ModuleSetting.Instance.OpenGuide)
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
            return GlobalModule.UIModule.PkgManager.AddPackageAsync("Common");
        }

#if ENABLE_BINARY_CONFIG
        /// <summary>
        /// 加载二进制配置表
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private static async Task<ByteBuf> ConfigBufferLoader(string file)
        {
            var configPath = Utility.AssetPath.GetConfigPath(file);
            var assetHandle = await GlobalModule.AssetModule.LoadAssetAsync<TextAsset>(configPath);
            return ByteBuf.Wrap(assetHandle.GetAssetObject<TextAsset>().bytes);
        }
#else
        /// <summary>
        /// 加载json配置表
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private static async Task<JSONNode> ConfigLoader(string file)
        {
            var cfgPath     = Utility.AssetPath.GetConfigPath(file, ".json");
            var assetHandle = await GlobalModule.AssetModule.LoadAssetAsync<TextAsset>(cfgPath);
            return JSON.Parse(assetHandle.GetAssetObject<TextAsset>().text);
        }
#endif
    }
}