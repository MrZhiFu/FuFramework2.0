using System;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using FuFramework.Core.Runtime;
using FuFramework.Entry.Runtime;
using FuFramework.Procedure.Runtime;
using HybridCLR;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Launcher.Procedure
{
    /// <summary>
    /// 代码热更流程
    /// 主要作用是：
    /// 1.加载热更程序集，并运行热更程序集入口函数，从而进入热更代码逻辑。
    /// </summary>
    public sealed class ProcedureHotfix : ProcedureBase
    {
        /// <summary>
        /// 显示优先级
        /// </summary>
        public override int Priority => 11;

        /// <summary>
        /// 热更程序集名称
        /// </summary>
        private const string HotfixDllName = "Game.Hotfix";


        protected override void OnEnter()
        {
            base.OnEnter();
            FuLogger.LogInfo("<color=#43f656>------进入代码热更流程------</color>");
            Start().Forget();
        }

        /// <summary>
        /// 开始代码热更
        /// </summary>
        private static async UniTaskVoid Start()
        {
            // 等待一帧，确保热更完毕
            await UniTask.DelayFrame();

            // 等待加载热更程序集
            var hotfixAssembly = await LoadDll();
            if (hotfixAssembly == null)
            {
                FuLogger.LogFatal("<color=#ff0000>------热更程序集加载失败------</color>");
            }

            // 等待热更程序集入口函数运行完毕
            await Run(hotfixAssembly);

            FuLogger.LogInfo("<color=#43f656>------代码热更流程结束------</color>");

            // 释放整个启动流程的加载界面
            LauncherUIHelper.Dispose();
        }

        /// <summary>
        /// 加载热更程序集
        /// </summary>
        public static async UniTask<Assembly> LoadDll()
        {
            // 编辑器模式下，直接加载程序集
            if (Utility.Application.IsEditor)
            {
                var assemblies = Utility.Assembly.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    var assemblyName = assembly.GetName().Name;
                    var isHotfix     = assemblyName.Equals(HotfixDllName, StringComparison.OrdinalIgnoreCase);
                    if (!isHotfix) continue;

                    return assembly;
                }
            }

            // 非编辑器模式下，加载AOT DLL，加载Game.Hotfix.dll，运行入口函数
            FuLogger.LogInfo("开始加载AOT DLL");

            var aotDlls = AOTGenericReferences.PatchedAOTAssemblyList.ToArray();
            foreach (var aotDll in aotDlls)
            {
                FuLogger.LogInfo("开始加载AOT DLL ==> " + aotDll);
                var aotDllPath        = Utility.AssetPath.GetAOTCodePath(aotDll);
                var aotDllAssetHandle = await GlobalModule.AssetModule.LoadAssetAsync<UnityEngine.Object>(aotDllPath);
                var aotDllBytes       = aotDllAssetHandle.GetAssetObject<UnityEngine.TextAsset>().bytes;
                RuntimeApi.LoadMetadataForAOTAssembly(aotDllBytes, HomologousImageMode.SuperSet);
            }

            FuLogger.LogInfo("结束加载AOT DLL");

            FuLogger.LogInfo("开始加载Game.Hotfix.dll");
            var hotfixDllPath        = Utility.AssetPath.GetCodePath(HotfixDllName + Utility.Const.FileNameSuffix.DLL);
            var hotfixDllAssetHandle = await GlobalModule.AssetModule.LoadAssetAsync<UnityEngine.Object>(hotfixDllPath);
            var hotfixDllBytes       = hotfixDllAssetHandle.GetAssetObject<UnityEngine.TextAsset>().bytes;

            FuLogger.LogInfo("开始加载程序集Hotfix");
            var hotfixAssembly = Assembly.Load(hotfixDllBytes, null);
            FuLogger.LogInfo("加载程序集Hotfix 结束 Assembly " + hotfixAssembly.FullName);

            return hotfixAssembly;
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