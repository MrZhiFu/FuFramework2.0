using Cysharp.Threading.Tasks;
using FuFramework.Fsm.Runtime;
using FuFramework.Core.Runtime;
using FuFramework.Procedure.Runtime;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Launcher.Procedure
{
    /// <summary>
    /// 代码热修复流程
    /// 主要作用是：
    /// 1.使用代码热修复辅助器，加载热更程序集，并运行热更程序集入口函数
    /// </summary>
    public sealed class ProcedureHotfix : ProcedureBase
    {
        public override int Priority => 11; // 显示优先级

        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            Log.Info("<color=#43f656>------进入代码热修复流程------</color>");

            Start().Forget();
        }

        /// <summary>
        /// 开始代码热修复
        /// </summary>
        private async UniTaskVoid Start()
        {
            await UniTask.DelayFrame();      // 等待一帧，确保热更完毕
            await HotfixHelper.StartHotfix(); // 开始代码热修复
            LauncherUIHelper.Dispose();       // 释放启动流程的加载界面
        }
    }
}