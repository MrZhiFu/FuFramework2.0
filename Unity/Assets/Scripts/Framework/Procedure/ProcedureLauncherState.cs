using Cysharp.Threading.Tasks;
using GameFrameX.Fsm.Runtime;
using GameFrameX.Procedure.Runtime;

namespace Unity.Startup.Procedure
{
    /// <summary>
    /// 启动流程
    /// </summary>
    public class ProcedureLauncherState : ProcedureBase
    {
        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            FairyGUI.UIObjectFactory.SetLoaderExtension(typeof(FairyGuiExtensionLoader));
            LauncherUIHandler.Start();
            Start(procedureOwner);
        }

        private async void Start(IFsm<IProcedureManager> procedureOwner)
        {
            await UniTask.NextFrame();
            ChangeState<ProcedureGetGlobalInfoState>(procedureOwner);
        }
    }
}