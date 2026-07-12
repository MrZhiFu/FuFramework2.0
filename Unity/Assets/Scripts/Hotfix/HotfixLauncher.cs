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
using FuFramework.TaskPool.Runtime;
using FuFramework.Variable.Runtime;
using FuFramework.ObjectPool.Runtime;
using FuFramework.Event.Runtime;
using FuFramework.Procedure.Runtime;
using FuFramework.Fsm.Runtime;
using FuFramework.UI.Runtime;
using FuFramework.Launcher.Runtime;
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

            // 协议消息处理器初始化：初始化所有协议对象
            ProtoMessageIdHandler.Init(HotfixProtoHandler.CurrentAssembly);

            // 注册热更层框架模块（含 Phase 2 起下沉到 Hotfix 的框架模块）
            ModuleManager.RegisterModule<ReferencePoolModule>(); // 引用池管理模块（Phase 2 下沉）
            ModuleManager.RegisterModule<ProcedureModule>(); // 流程管理模块（Phase 2 下沉）
            ModuleManager.RegisterModule<EventModule>();     // 事件管理模块（Phase 2 下沉）
            ModuleManager.RegisterModule<ObjectPoolModule>(); // 对象池管理模块（Phase 2 下沉）
            ModuleManager.RegisterModule<MonoModule>();      // Mono管理模块（Phase 2 下沉）
            ModuleManager.RegisterModule<TimerModule>();     // 计时器管理模块（Phase 2 下沉）
            ModuleManager.RegisterModule<AssetModule>();     // 资源管理模块（Phase 2 下沉）
            ModuleManager.RegisterModule<UIModule>();        // UI管理模块（Phase 2 下沉）
            ModuleManager.RegisterModule<RedDotModule>();

            // 设置FairyGUI的Loader加载器为自定义加载器
            FairyGUI.UIObjectFactory.SetLoaderExtension(typeof(CustomLoader));

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

            // 将 ModuleManager 的生命周期方法挂接到 AOT 侧 Launcher 委托（ModuleManager 已随 Task 15 下沉 Hotfix）
            FuFramework.Launcher.Runtime.Launcher.OnUpdate       = ModuleManager.Update;
            FuFramework.Launcher.Runtime.Launcher.OnLateUpdate   = ModuleManager.LateUpdate;
            FuFramework.Launcher.Runtime.Launcher.OnFixedUpdate  = ModuleManager.FixedUpdate;
            FuFramework.Launcher.Runtime.Launcher.DisposeModules = ModuleManager.Dispose;
            FuFramework.Launcher.Runtime.Launcher.ReInitModules  = ModuleManager.ReInit;

            // 加载配置表
            bootstrapView.SetTip("LoadConfig...");
            await LoadConfigAsync();

            // 加载初始必要的UI资源
            bootstrapView.SetTip("LoadInitUIAsset...");
            await LoadUIAsync();

            // 绑定自动生成的Fui自定义组件(HotFix下)
            CustomCompBind.BindAll();

            // 指定获取多语言的接口
            LocalizationModule.Instance.LocalizationProvider = new LocalizationProvider();

            // 打开登录界面
            GlobalModule.UIModule.OpenUI<WinLogin>();

            // 登录界面已打开，关闭 AOT 引导加载界面
            bootstrapView.Close();

            // 如果开启引导，则指定引导模块的动作执行器，并开始首个引导
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