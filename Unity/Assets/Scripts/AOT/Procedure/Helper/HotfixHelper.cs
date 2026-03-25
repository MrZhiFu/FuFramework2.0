using System;
using HybridCLR;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using FuFramework.Core.Runtime;
using FuFramework.Entry.Runtime;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Launcher.Procedure
{
    /// <summary>
    /// 代码热更辅助类。
    /// 用于加载热更程序集，并运行热更程序集入口函数。
    /// </summary>
    public static class HotfixHelper
    {
        /// <summary>
        /// 热更程序集名称
        /// </summary>
        private const string HotfixName = "Game.Hotfix";

        /// <summary>
        /// 启动代码热更
        /// </summary>
        public static async UniTask StartHotfix()
        {
            // 编辑器模式下，直接加载程序集
            if (Utility.Application.IsEditor)
            {
                var assemblies = Utility.Assembly.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    var assemblyName = assembly.GetName().Name;
                    var isHotfix     = assemblyName.Equals(HotfixName, StringComparison.OrdinalIgnoreCase);
                    if (!isHotfix) continue;

                    // 等待热更程序集入口函数运行完毕
                    await Run(assembly);
                    break;
                }

                return;
            }

            // 非编辑器模式下，加载AOT DLL，加载Game.Hotfix.dll，运行入口函数
            FuLogger.LogInfo("开始加载AOT DLL");

            var aotDlls = AOTGenericReferences.PatchedAOTAssemblyList.ToArray();
            foreach (var aotDll in aotDlls)
            {
                FuLogger.LogInfo("开始加载AOT DLL ==> " + aotDll);
                var assetHandle = await GlobalModule.AssetModule.LoadAssetAsync<UnityEngine.Object>(Utility.AssetPath.GetAOTCodePath(aotDll));
                var aotBytes    = assetHandle.GetAssetObject<UnityEngine.TextAsset>().bytes;
                RuntimeApi.LoadMetadataForAOTAssembly(aotBytes, HomologousImageMode.SuperSet);
            }

            FuLogger.LogInfo("结束加载AOT DLL");

            FuLogger.LogInfo("开始加载Game.Hotfix.dll");
            var assetHotfixDllPath            = Utility.AssetPath.GetCodePath(HotfixName + Utility.Const.FileNameSuffix.DLL);
            var assetHotfixDllOperationHandle = await GlobalModule.AssetModule.LoadAssetAsync<UnityEngine.Object>(assetHotfixDllPath);
            var assemblyDataHotfixDll         = assetHotfixDllOperationHandle.GetAssetObject<UnityEngine.TextAsset>().bytes;

            FuLogger.LogInfo("开始加载程序集Hotfix");
            var hotfixAssembly = Assembly.Load(assemblyDataHotfixDll, null);
            FuLogger.LogInfo("加载程序集Hotfix 结束 Assembly " + hotfixAssembly.FullName);

            // 等待热更程序集入口函数运行完毕
            await Run(hotfixAssembly);
        }

        /// <summary>
        /// 运行热更程序集入口函数
        /// </summary>
        private static async UniTask Run(Assembly assembly)
        {
            var entryType = assembly.GetType("Hotfix.HotfixLauncher");
            FuLogger.LogInfo("获取程序集Hotfix的入口类型 ==>" + entryType.FullName);

            var mainMethod = entryType.GetMethod("Main");
            FuLogger.LogInfo("获取程序集Hotfix的入口类型的入口方法 ==>" + mainMethod?.Name);

            // 调用异步入口函数并等待完成
            var result = mainMethod?.Invoke(null, null);
            if (result is not UniTask mainTask)
            {
                FuLogger.LogError("[HotfixHelper] 入口函数不是异步可等待的");
                return;
            }

            await mainTask;
        }
    }
}