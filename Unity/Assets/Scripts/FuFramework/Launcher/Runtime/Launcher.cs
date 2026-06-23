using UnityEngine;
using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Launcher.Runtime
{
    /// <summary>
    /// 入口类。
    /// 功能：
    ///     1. 注册框架模块
    ///     2. 启动游戏流程
    ///     3. 驱动框架生命周期
    ///     4. 暂停/继续/退出/重启游戏。
    /// </summary>
    public partial class Launcher : MonoSingleton<Launcher>
    {
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
            // 注册框架模块
            RegisterModules();

            // 开始游戏流程
            StartProcedure();
        }

        /// <summary>
        /// 驱动框架模块帧更新
        /// </summary>
        private void Update()
        {
            ModuleManager.Update(Time.deltaTime, Time.unscaledDeltaTime);
        }

        /// <summary>
        /// 驱动框架模块延迟帧更新
        /// </summary>
        private void LateUpdate()
        {
            ModuleManager.LateUpdate(Time.deltaTime, Time.unscaledDeltaTime);
        }

        /// <summary>
        /// 驱动框架模块固定帧更新
        /// </summary>
        private void FixedUpdate()
        {
            ModuleManager.FixedUpdate();
        }
    }
}