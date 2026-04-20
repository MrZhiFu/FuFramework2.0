using System;
using System.Reflection;
using Cysharp.Threading.Tasks;
using FuFramework.Core.Runtime;
using FuFramework.Launcher.Runtime;
using FuFramework.Procedure.Runtime;
using HybridCLR;
using Launcher.UI;
using YooAsset;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Launcher.Procedure
{
    /// <summary>
    /// 代码热修复流程
    /// 功能：
    ///     1.加载AOT程序集，补充AOT程序集的元数据
    ///     2.加载热更程序集，并运行热更程序集入口函数，进入热更代码逻辑。
    /// </summary>
    public sealed class ProcedureCodeHotfix : ProcedureBase
    {
#if UNITY_EDITOR
        /// <summary>
        /// 在Inspector中的显示优先级
        /// </summary>
        public override int Priority => 9;
#endif

        /// <summary>
        /// 热更程序集名称
        /// </summary>
        private const string HotfixDllName = "Game.Hotfix";


        protected override void OnEnter()
        {
            base.OnEnter();
            FuLogger.LogInfo("<color=#43f656>------进入代码热更流程------</color>");

            // 开始异步代码热修复
            StartAsync().Forget();
        }

        /// <summary>
        /// 开始异步代码热修复
        /// </summary>
        private static async UniTaskVoid StartAsync()
        {
            // 等待一帧，确保热更完毕
            await UniTask.NextFrame();

            // 等待加载热更程序集
            var hotfixAssembly = await LoadDll();
            if (hotfixAssembly == null)
            {
                FuLogger.LogFatal("<color=#ff0000>------热更程序集加载失败------</color>");
            }

            // 等待热更程序集入口函数运行完毕
            await Run(hotfixAssembly);

            FuLogger.LogInfo("<color=#43f656>------代码热更流程结束------</color>");

            // 关闭整个启动流程的加载界面
            GlobalModule.UIModule.CloseUI<WinLauncher>();
        }

        /// <summary>
        /// 加载AOT程序集和热更程序集
        /// </summary>
        public static async UniTask<Assembly> LoadDll()
        {
            // 编辑器模式 且 资源加载在编辑器模拟模式下，直接使用当前程序集
            if (Utility.Application.IsEditor && GlobalModule.AssetModule.PlayMode is EPlayMode.EditorSimulateMode)
            {
                FuLogger.LogInfo("-------------编辑器模式下，直接使用编辑器下的程序集-----------------");
                var assemblies = Utility.Assembly.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    var assemblyName = assembly.GetName().Name;
                    var isHotfix     = assemblyName.Equals(HotfixDllName, StringComparison.OrdinalIgnoreCase);
                    if (!isHotfix) continue;

                    return assembly;
                }
            }

            // 非编辑器模式下，加载补丁AOT DLL和 Hotfix DLL
            FuLogger.LogInfo("---------------开始加载AOT DLL---------------");
            foreach (var aotDll in AOTGenericReferences.PatchedAOTAssemblyList)
            {
                FuLogger.LogInfo("开始加载AOT DLL ==> " + aotDll);
                var aotDllPath        = Utility.AssetPath.GetAOTCodePath(aotDll);
                var aotDllAssetHandle = await GlobalModule.AssetModule.LoadAssetAsync<UnityEngine.Object>(aotDllPath);
                var aotDllBytes       = aotDllAssetHandle.GetAssetObject<UnityEngine.TextAsset>().bytes;
                RuntimeApi.LoadMetadataForAOTAssembly(aotDllBytes, HomologousImageMode.SuperSet);
            }

            FuLogger.LogInfo("---------------结束加载AOT DLL---------------");


            FuLogger.LogInfo("+++++++++++++++开始加载Hotfix DLL+++++++++++++++");
            var hotfixDllPath        = Utility.AssetPath.GetCodePath($"{HotfixDllName}.dll");
            var hotfixDllAssetHandle = await GlobalModule.AssetModule.LoadAssetAsync<UnityEngine.Object>(hotfixDllPath);
            var hotfixDllBytes       = hotfixDllAssetHandle.GetAssetObject<UnityEngine.TextAsset>().bytes;
            var hotfixAssembly       = Assembly.Load(hotfixDllBytes, null);
            FuLogger.LogInfo("加载完成 ==> " + hotfixAssembly.FullName);
            FuLogger.LogInfo("+++++++++++++++结束加载Hotfix DLL+++++++++++++++");

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