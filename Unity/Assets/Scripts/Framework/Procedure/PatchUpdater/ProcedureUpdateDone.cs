using GameFrameX.Asset.Runtime;
using GameFrameX.Fsm.Runtime;
using GameFrameX.Procedure.Runtime;
using GameFrameX.Runtime;

namespace Unity.Startup.Procedure
{
    /// <summary>
    /// 热更流程--更新完毕
    /// </summary>
    public class ProcedureUpdateDone : ProcedureBase
    {
        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);

            GameApp.Event.Fire(this, AssetPatchStatesChangeEventArgs.Create(AssetComponent.BuildInPackageName, EPatchStates.PatchDone));

            // UI设置为更新完成状态
            LauncherUIHelper.SetProgressUpdateFinish();
            LauncherUIHelper.SetTipText(string.Empty);

            Log.Info("资源热更流程更新完毕，进入游戏逻辑启动流程");
            ChangeState<ProcedureHotfix>(procedureOwner);
        }
    }
}