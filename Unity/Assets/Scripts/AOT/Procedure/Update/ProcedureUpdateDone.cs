using FuFramework.Fsm.Runtime;
using FuFramework.Core.Runtime;
using FuFramework.Asset.Runtime;
using FuFramework.Procedure.Runtime;
using FuFramework.Entry.Runtime;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Launcher.Procedure
{
    /// <summary>
    /// 热更流程--更新完毕
    /// </summary>
    public class ProcedureUpdateDone : ProcedureBase
    {
        public override int Priority => 10; // 显示优先级
        
        protected override void OnEnter(Fsm procedureOwner)
        {
            base.OnEnter(procedureOwner);
            Log.Info("<color=#43f656>------进入热更流程：更新完毕------</color>");
           
            GlobalModule.EventModule.Fire(this, AssetPatchStatesChangeEventArgs.Create(GlobalModule.AssetModule.DefaultPackageName, EPatchStates.UpdateDone));

            // UI设置为更新完成状态
            LauncherUIHelper.SetProgressUpdateFinish();
            LauncherUIHelper.SetTipText(string.Empty);

            // 资源热更流程更新完毕，进入代码热修复流程;
            ChangeState<ProcedureHotfix>(procedureOwner);
        }
    }
}