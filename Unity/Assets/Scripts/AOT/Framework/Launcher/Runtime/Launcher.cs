using System;
using UnityEngine;
using System.Reflection;
using Cysharp.Threading.Tasks;
using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Launcher.Runtime
{
    /// <summary>
    /// AOT 入口类。
    /// 功能：
    ///     1. 启动 AOT 极简引导流程（下载资源、加载热更程序集）
    ///     2. 引导完成后反射调用 HotfixLauncher.MainAsync() 进入热更逻辑
    /// </summary>
    public class Launcher : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

#if ENABLE_SRDEBUGGER
            SRDebug.Init();
#endif

            FuLogger.LogInfo($"游戏版本号: {Application.version}, Unity版本号: {Application.unityVersion}");
        }

        private void Start()
        {
            // 启动 AOT 极简引导流程，引导完成后回调 InvokeHotfixEntryAsync 进入热更入口
            global::Launcher.BootstrapProcess.RunAsync(InvokeHotfixEntryAsync).Forget();
        }

        /// <summary>
        /// 热更入口回调。
        /// 由 AOT 引导流程在加载完 Hotfix 程序集后调用：随后反射调用热更入口 Hotfix.HotfixLauncher.MainAsync。
        /// </summary>
        /// <param name="view">AOT 加载界面句柄，透传给热更入口用于收尾关闭。</param>
        private static async UniTask InvokeHotfixEntryAsync(IBootstrapView view)
        {
            // 反射进入热更入口
            var hotfixAssembly = GetHotfixAssembly();
            if (hotfixAssembly == null)
            {
                FuLogger.LogError("[Launcher] 未找到已加载的 Hotfix 程序集，无法进入热更入口。");
                return;
            }

            var entryType  = hotfixAssembly.GetType("Hotfix.HotfixLauncher");
            var mainMethod = entryType?.GetMethod("MainAsync", BindingFlags.Public | BindingFlags.Static);
            if (mainMethod == null)
            {
                FuLogger.LogError("[Launcher] 未找到热更入口 Hotfix.HotfixLauncher.MainAsync。");
                return;
            }

            await (UniTask)mainMethod.Invoke(null, new object[] { view });
        }

        /// <summary>
        /// 获取已加载到当前应用域的 Hotfix 程序集。
        /// </summary>
        /// <returns>Hotfix 程序集，未找到返回 null。</returns>
        private static Assembly GetHotfixAssembly()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == "Hotfix")
                {
                    return assembly;
                }
            }

            return null;
        }

        /// <summary>
        /// 重启引导流程，供 GameDriven.RestartGame 调用。
        /// </summary>
        internal static void RestartBootstrap()
        {
            global::Launcher.BootstrapProcess.RunAsync(InvokeHotfixEntryAsync).Forget();
        }
    }
}
