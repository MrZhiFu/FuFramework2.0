using SimpleJSON;
using UnityEngine;
using Hotfix.Proto;
using Hotfix.Config;
using Hotfix.UI;
using Hotfix.Config.Tables;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FuFramework.Asset.Runtime;
using FuFramework.UI.Runtime;
using FuFramework.Network.Runtime;
using FuFramework.Core.Runtime;
using FuFramework.Entry.Runtime;
using Hotfix.Guide;
using LuBan.Runtime;
using Utility = FuFramework.Core.Runtime.Utility;
#if ENABLE_BINARY_CONFIG
#endif

namespace Hotfix
{
    /// <summary>
    /// 热修复代码入口
    /// </summary>
    public static class HotfixLauncher
    {
        /// <summary>
        /// 启动入口
        /// </summary>
        public static void Main()
        {
            FuLogger.LogInfo("<color=#43f656>------热更逻辑完毕，进入热更后的代码逻辑入口------</color>");

            // 协议消息处理器初始化：初始化所有协议对象
            ProtoMessageIdHandler.Init(HotfixProtoHandler.CurrentAssembly);

            // 加载配置表
            LoadConfig().Forget();

            // 加载初始UI
            LoadUI();

            // 指定引导模块的动作执行器，并开始首个引导
            GlobalModule.GuideModule.GuideAction = new GuideActionImpl();
            GlobalModule.GuideModule.StartFirstGuide();
        }

        /// <summary>
        /// 加载配置表
        /// </summary>
        private static async UniTaskVoid LoadConfig()
        {
            var tablesComponent = new TablesComponent();
            tablesComponent.Init(GlobalModule.ConfigModule);

#if ENABLE_BINARY_CONFIG
            // 使用二进制配置表
            await tablesComponent.LoadAsync(ConfigBufferLoader);
#else
            // 使用JSON配置表
            await tablesComponent.LoadAsync(ConfigLoader);
#endif
        }

        /// <summary>
        /// 加载UI
        /// </summary>
        private static void LoadUI()
        {
            var uiModule = GlobalModule.UIModule;
            
            // 添加通用UI资源包
            uiModule.PkgManger.AddPackageAsync("Common").Forget();

            // 打开登录界面
            uiModule.OpenUI<WinLogin>();
        }

#if ENABLE_BINARY_CONFIG
        /// <summary>
        /// 加载二进制配置表
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private static async Task<ByteBuf> ConfigBufferLoader(string file)
        {
            var configPath = Utility.AssetPath.GetConfigPath(file, Utility.Const.FileNameSuffix.Binary);
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
            var configPath = Utility.AssetPath.GetConfigPath(file, Utility.Const.FileNameSuffix.Json);
            var assetHandle = await GlobalModule.AssetModule.LoadAssetAsync<TextAsset>(configPath);
            return JSON.Parse(assetHandle.GetAssetObject<TextAsset>().text);
        }
#endif
    }
}