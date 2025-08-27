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
using UIManager = FuFramework.UI.Runtime.UIManager;
using Utility = FuFramework.Core.Runtime.Utility;
#if ENABLE_BINARY_CONFIG
#endif

namespace Hotfix
{
    /// <summary>
    /// 代码热更入口
    /// </summary>
    public static class HotfixLauncher
    {
        /// <summary>
        /// 启动入口
        /// </summary>
        public static void Main()
        {
            Log.Info("<color=#43f656>------热更逻辑完毕，进入热更代码逻辑入口------</color>");
            
            // 协议消息处理器初始化：初始化所有协议对象
            ProtoMessageIdHandler.Init(HotfixProtoHandler.CurrentAssembly);
            
            // 加载配置表
            LoadConfig();
            
            // 加载初始UI
            LoadUI();
        }

        /// <summary>
        /// 加载配置表
        /// </summary>
        static async void LoadConfig()
        {
            var tablesComponent = new TablesComponent();
            tablesComponent.Init(GameApp.Config);
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
            // 添加通用UI资源包
            FuiPackageManager.Instance.AddPackageAsync("Common").Forget();
            
            // 打开登录界面
            UIManager.Instance.OpenUI<WinLogin>();
            var item = GameApp.Config.GetConfig<TbSoundsConfig>().FirstOrDefault;
            Log.Info(item);
        }

#if ENABLE_BINARY_CONFIG
        /// <summary>
        /// 加载二进制配置表
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private static async Task<ByteBuf> ConfigBufferLoader(string file)
        {
            var assetHandle = await AssetManager.Instance.LoadAssetAsync<TextAsset>(Utility.Asset.Path.GetConfigPath(file, Utility.Const.FileNameSuffix.Binary));
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
            var assetHandle = await AssetManager.Instance.LoadAssetAsync<TextAsset>(Utility.Asset.Path.GetConfigPath(file, Utility.Const.FileNameSuffix.Json));
            return JSON.Parse(assetHandle.GetAssetObject<TextAsset>().text);
        }
#endif
    }
}