using Cysharp.Threading.Tasks;
using GameFrameX.Fsm.Runtime;
using GameFrameX.Procedure.Runtime;

namespace Unity.Startup.Procedure
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
        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            
            // 设置FairyGUI的Loader加载器为自定义加载器
            FairyGUI.UIObjectFactory.SetLoaderExtension(typeof(FuiCostumeLoader));
            
            // 启动UI
            LauncherUIHelper.Start();
            
            // 启动流程
            Start(procedureOwner);
        }

        
        /// <summary>
        /// 进入获取全局信息流程
        /// </summary>
        private async void Start(IFsm<IProcedureManager> procedureOwner)
        {
            await UniTask.NextFrame();
            ChangeState<ProcedureGetGlobalInfoFromGmServer>(procedureOwner);
        }
    }
}