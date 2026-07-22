using Cysharp.Threading.Tasks;
using AOT.Framework.Core.Log;
using AOT.Launch;
using UnityEngine;

namespace AOT
{
    /// <summary>
    /// AOT 入口类。
    /// 功能：
    ///     1. 确保跨场景存活
    ///     2. 启动 AOT 极简启动流程，完成后由 GameDriven 接管进入热更逻辑
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
            // 启动流程
            LaunchProcess.RunAsync().Forget();
        }
    }
}