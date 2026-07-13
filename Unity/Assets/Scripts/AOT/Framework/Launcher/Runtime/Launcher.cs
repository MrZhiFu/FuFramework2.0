using UnityEngine;
using FuFramework.Core.Runtime;
using Launcher;

// ReSharper disable once CheckNamespace
namespace FuFramework.Launcher.Runtime
{
    /// <summary>
    /// AOT 入口类。
    /// 功能：
    ///     1. 确保跨场景存活
    ///     2. 启动 AOT 极简引导流程，引导完成后由 GameDriven 接管进入热更逻辑
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
            // 启动 AOT 极简引导流程，引导完成后回调 GameDriven.EnterHotfixAsync 进入热更入口
            BootstrapProcess.RunAsync(GameDriven.Instance.EnterHotfixAsync).Forget();
        }
    }
}
