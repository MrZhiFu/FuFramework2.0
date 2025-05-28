using Cysharp.Threading.Tasks;
using GameFrameX.Fsm.Runtime;
using GameFrameX.Procedure.Runtime;

namespace Unity.Startup.Procedure
{
    /// <summary>
    /// 代码热更流程
    /// 主要作用是：
    /// 1.使用代码热更辅助器，加载热更程序集，并运行热更程序集入口函数
    /// </summary>
    public sealed class ProcedureHotfix : ProcedureBase
    {
        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            Start();
        }

        private async void Start()
        {
            await UniTask.DelayFrame();
            
            // 开始代码热更
            HotfixHelper.StartHotfix();
            
            // 释放启动流程的加载界面
            LauncherUIHelper.Dispose();
        }
    }
}