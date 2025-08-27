using Cysharp.Threading.Tasks;
using FuFramework.UI.Runtime;
using FuFramework.Fsm.Runtime;
using FuFramework.Core.Runtime;
using FuFramework.Procedure.Runtime;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Launcher.Procedure
{
    /// <summary>
    /// 启动流程
    /// 主要作用是：
    /// 1. 设置FairyGUI的Loader加载器为自定义加载器
    /// 2. 启动UI
    /// 3. 进入获取全局信息流程
    /// </summary>
    public class ProcedureLauncher : ProcedureBase
    {
        public override int Priority => 1; // 显示优先级

        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            Log.Info("<color=#43f656>------进入首次启动流程------</color>");

            // 设置FairyGUI的Loader加载器为自定义加载器
            FairyGUI.UIObjectFactory.SetLoaderExtension(typeof(CustomLoader));

            // 启动UI
            LauncherUIHelper.Start().Forget();

            // 启动流程
            Start(procedureOwner).Forget();
        }


        /// <summary>
        /// 进入获取全局信息流程
        /// </summary>
        private async UniTaskVoid Start(IFsm<IProcedureManager> procedureOwner)
        {
            await UniTask.NextFrame();
            ChangeState<ProcedureReqGlobalInfo>(procedureOwner);
        }
    }
}