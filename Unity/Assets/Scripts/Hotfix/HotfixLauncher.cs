using UnityEngine;
using Hotfix.Proto;
using Hotfix.Config;
using Hotfix.ModuleConfig;
using Hotfix.UI;
using Hotfix.Guide;
using Hotfix.RedDot;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FuFramework.Network.Runtime;
using FuFramework.Core.Runtime;
using FuFramework.Launcher.Runtime;
using FuFramework.ModuleSetting.Runtime;
using Hotfix.Localization;
using Hotfix.Model;
using Hotfix.Scene;
using Hotfix.Storage;
using Hotfix.Sound;
using Hotfix.Download;
using Launcher.UI;
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
        public static async UniTask MainAsync()
        {
            FuLogger.LogInfo("<color=#43f656>------热更逻辑完毕，进入热更后的代码逻辑入口------</color>");

            // 协议消息处理器初始化：初始化所有协议对象
            ProtoMessageIdHandler.Init(HotfixProtoHandler.CurrentAssembly);

            // 注册热更层框架模块
            ModuleManager.RegisterModule<RedDotModule>();
            ModuleManager.RegisterModule<GuideModule>();
            ModuleManager.RegisterModule<LocalizationModule>();
            ModuleManager.RegisterModule<ModelModule>();
            ModuleManager.RegisterModule<ConfigModule>();
            ModuleManager.RegisterModule<SceneModule>();
            ModuleManager.RegisterModule<StorageModule>();
            ModuleManager.RegisterModule<SoundModule>();
            ModuleManager.RegisterModule<DownloadModule>();

            // 获取热更界面
            var winLauncher = GlobalModule.UIModule.GetUI<WinLauncher>();
            if (winLauncher == null)
            {
                FuLogger.LogError("热更界面获取失败！");
                return;
            }
            
            // 加载配置表
            winLauncher.SetTipText("LoadConfig...");
            await LoadConfigAsync();

            // 加载初始必要的UI资源
            winLauncher.SetTipText("LoadInitUIAsset...");
            await LoadUIAsync();

            // 绑定自动生成的Fui自定义组件(HotFix下)
            BindCustomComps();

            // 指定获取多语言的接口
            LocalizationModule.Instance.LocalizationProvider = new LocalizationProvider();

            // 打开登录界面
            GlobalModule.UIModule.OpenUI<WinLogin>();

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

        //@formatter:off
        /// <summary>
        /// 绑定Fui自定义组件(AutoGen)
        /// 方法体由FUI导出时自动生成,请勿修改.
        /// 特殊:如果清理了FUI包里的所有自定义组件,且清理了它们的绑定代码,则可以删除对应的BindAll()调用.
        /// </summary>
        private static void BindCustomComps()
        {
            CommonBinder.BindAll();
            LoginBinder.BindAll();
            BagBinder.BindAll();
        }
        //@formatter:on

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