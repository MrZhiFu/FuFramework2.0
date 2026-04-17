using FuFramework.Core.Runtime;
using FuFramework.Asset.Runtime;
using FuFramework.Launcher.Runtime;
using FuFramework.Procedure.Runtime;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Launcher.Procedure
{
    /// <summary>
    /// 热更流程--资源更新完毕流程。
    /// 功能：
    ///     1. UI设置为更新完成状态
    ///     2. 进入代码热更流程
    /// </summary>
    public class ProcedureUpdateDone : ProcedureBase
    {
#if UNITY_EDITOR
        /// <summary>
        /// 在Inspector中的显示优先级
        /// </summary>
        public override int Priority => 8;
#endif

        protected override void OnEnter()
        {
            base.OnEnter();
            FuLogger.LogInfo("<color=#43f656>------进入热更流程：更新完毕------</color>");

            GlobalModule.EventModule.Broadcast(this, AssetUpdateStateChangeEventArgs.Create(GlobalModule.AssetModule.DefaultPackageName, EUpdateStates.UpdateDone));

            // UI设置为更新完成状态
            LauncherUIHelper.SetProgressUpdateFinish();
            LauncherUIHelper.SetTipText(string.Empty);

            // 资源热更流程更新完毕，进入代码热修复流程;
            ChangeState<ProcedureCodeHotfix>();
        }
    }
}