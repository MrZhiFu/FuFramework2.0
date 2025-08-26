using System;
using System.Linq;
using System.Reflection;
using FuFramework.Asset.Runtime;
using FuFramework.Core.Runtime;
using FuFramework.Entry.Runtime;
using HybridCLR;
using Utility = FuFramework.Core.Runtime.Utility;

namespace Unity.Startup.Procedure
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
        private const string HotfixName = "Unity.Hotfix";

        /// <summary>
        /// 启动代码热更
        /// </summary>
        public static async void StartHotfix()
        {
            // 编辑器模式下，直接加载程序集
            if (ApplicationHelper.IsEditor)
            {
                var assemblies = Utility.Assembly.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    if (assembly.GetName().Name.Equals(HotfixName, StringComparison.OrdinalIgnoreCase))
                    {
                        Run(assembly);
                        break;
                    }
                }

                return;
            }

            // 非编辑器模式下，加载AOT DLL，加载Unity.Hotfix.dll，运行入口函数
            Log.Info("开始加载AOT DLL");
            var aotDlls = AOTGenericReferences.PatchedAOTAssemblyList.ToArray();
            foreach (var aotDll in aotDlls)
            {
                Log.Info("开始加载AOT DLL ==> " + aotDll);
                var assetHandle = await AssetManager.Instance.LoadAssetAsync<UnityEngine.Object>(Utility.Asset.Path.GetAOTCodePath(aotDll));
                var aotBytes    = assetHandle.GetAssetObject<UnityEngine.TextAsset>().bytes;
                RuntimeApi.LoadMetadataForAOTAssembly(aotBytes, HomologousImageMode.SuperSet);
            }

            Log.Info("结束加载AOT DLL");

            Log.Info("开始加载Unity.Hotfix.dll");
            var assetHotfixDllPath            = Utility.Asset.Path.GetCodePath(HotfixName + Utility.Const.FileNameSuffix.DLL);
            var assetHotfixDllOperationHandle = await AssetManager.Instance.LoadAssetAsync<UnityEngine.Object>(assetHotfixDllPath);
            var assemblyDataHotfixDll         = assetHotfixDllOperationHandle.GetAssetObject<UnityEngine.TextAsset>().bytes;

            Log.Info("开始加载程序集Hotfix");
            var hotfixAssembly = Assembly.Load(assemblyDataHotfixDll, null);
            Log.Info("加载程序集Hotfix 结束 Assembly " + hotfixAssembly.FullName);

            // 运行热更程序集入口函数
            Run(hotfixAssembly);
        }

        /// <summary>
        /// 运行热更程序集入口函数
        /// </summary>
        /// <param name="assembly"></param>
        private static void Run(Assembly assembly)
        {
            var entryType = assembly.GetType("Hotfix.HotfixLauncher");

            Log.Info("获取程序集Hotfix的入口类型 ==>" + entryType.FullName);
            var method = entryType.GetMethod("Main");

            Log.Info("获取程序集Hotfix的入口类型的入口方法 ==>" + method?.Name);
            method?.Invoke(null, null);
        }
    }
}