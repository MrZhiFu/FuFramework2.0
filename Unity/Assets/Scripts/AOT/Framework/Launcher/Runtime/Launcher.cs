using System;
using UnityEngine;
using System.Reflection;
using Cysharp.Threading.Tasks;
using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Launcher.Runtime
{
    /// <summary>
    /// 入口类。
    /// 功能：
    ///     1. 启动 AOT 极简引导流程（下载资源、加载热更程序集）
    ///     2. 引导完成后注册框架模块并接管框架帧更新生命周期
    ///     3. 暂停/继续/退出/重启游戏。
    /// </summary>
    public partial class Launcher : MonoSingleton<Launcher>
    {
        /// <summary>
        /// 框架模块帧更新委托。引导完成后由 Hotfix 侧挂接，指向 ModuleManager.Update。
        /// </summary>
        public static Action<float, float> OnUpdate;

        /// <summary>
        /// 框架模块延迟帧更新委托。引导完成后由 Hotfix 侧挂接，指向 ModuleManager.LateUpdate。
        /// </summary>
        public static Action<float, float> OnLateUpdate;

        /// <summary>
        /// 框架模块固定帧更新委托。引导完成后由 Hotfix 侧挂接，指向 ModuleManager.FixedUpdate。
        /// </summary>
        public static Action OnFixedUpdate;

        /// <summary>
        /// 释放全部模块委托。由 Hotfix 侧挂接，指向 ModuleManager.Dispose。
        /// </summary>
        public static Action DisposeModules;

        /// <summary>
        /// 重新初始化全部模块委托。由 Hotfix 侧挂接，指向 ModuleManager.ReInit。
        /// </summary>
        public static Action ReInitModules;

        /// <summary>
        /// 初始化
        /// </summary>
        protected override void OnInit()
        {
#if ENABLE_SRDEBUGGER
            // 初始化运行时日志查看器
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
        /// 驱动框架模块帧更新
        /// </summary>
        private void Update()
        {
            OnUpdate?.Invoke(Time.deltaTime, Time.unscaledDeltaTime);
        }

        /// <summary>
        /// 驱动框架模块延迟帧更新
        /// </summary>
        private void LateUpdate()
        {
            OnLateUpdate?.Invoke(Time.deltaTime, Time.unscaledDeltaTime);
        }

        /// <summary>
        /// 驱动框架模块固定帧更新
        /// </summary>
        private void FixedUpdate()
        {
            OnFixedUpdate?.Invoke();
        }

        /// <summary>
        /// 热更入口回调。
        /// 由 AOT 引导流程在加载完 Hotfix 程序集后调用：注册框架模块、接管帧更新循环，
        /// 随后反射调用热更入口 Hotfix.HotfixLauncher.MainAsync。
        /// 说明：ModuleBase/ModuleManager 已下沉 Hotfix（Task 15），帧更新委托由 HotfixLauncher 挂接。
        /// </summary>
        /// <param name="view">AOT 加载界面句柄，透传给热更入口用于收尾关闭。</param>
        private static async UniTask InvokeHotfixEntryAsync(global::Launcher.BootstrapView view)
        {
            // 框架模块注册与帧更新委托挂接已全部移交 HotfixLauncher（Task 17）

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
                    return assembly;
            }

            return null;
        }
    }
}
