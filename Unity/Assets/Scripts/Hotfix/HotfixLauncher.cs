using System;
using UnityEngine;
using Hotfix.Proto;
using Hotfix.Config;
using Hotfix.UI;
using Hotfix.Guide;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FuFramework.Network.Runtime;
using FuFramework.Core.Runtime;
using FuFramework.Entry.Runtime;
using FuFramework.ModuleSetting.Runtime;
using Launcher.Procedure;
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
        public static async UniTask Main()
        {
            FuLogger.LogInfo("<color=#43f656>------热更逻辑完毕，进入热更后的代码逻辑入口------</color>");

            // 协议消息处理器初始化：初始化所有协议对象
            ProtoMessageIdHandler.Init(HotfixProtoHandler.CurrentAssembly);

            // 加载配置表
            LauncherUIHelper.SetTipText("LoadConfig...");
            await LoadConfig();

            // 加载初始必要的UI资源
            LauncherUIHelper.SetTipText("LoadInitUIAsset...");
            await LoadUI();

            // 打开登录界面
            GlobalModule.UIModule.OpenUI<WinLogin>();

            // 如果开启引导，则指定引导模块的动作执行器，并开始首个引导
            if (ModuleSetting.Instance.OpenGuide)
            {
                GlobalModule.GuideModule.GuideAction = new GuideActionImpl();
                GlobalModule.GuideModule.StartFirstGuide();
            }
        }

        /// <summary>
        /// 加载配置表
        /// </summary>
        private static async UniTask LoadConfig()
        {
            var tableManager = new TableManager();
            tableManager.Init(GlobalModule.ConfigModule);

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
        private static async UniTask LoadUI()
        {
            // 添加通用UI资源包
            await GlobalModule.UIModule.PkgManager.AddPackageAsync("Common");
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
            var cfgPath     = Utility.AssetPath.GetConfigPath(file, Utility.Const.FileNameSuffix.Json);
            var assetHandle = await GlobalModule.AssetModule.LoadAssetAsync<TextAsset>(cfgPath);
            return JSON.Parse(assetHandle.GetAssetObject<TextAsset>().text);
        }
#endif
    }
}