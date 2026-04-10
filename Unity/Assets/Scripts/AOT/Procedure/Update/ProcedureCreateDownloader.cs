using FuFramework.Core.Runtime;
using FuFramework.Asset.Runtime;
using FuFramework.Procedure.Runtime;
using FuFramework.Launcher.Runtime;
using FuFramework.Variable.Runtime;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Launcher.Procedure
{
    /// <summary>
    /// 热更流程--创建资源下载器流程。
    /// 主要作用是：
    /// 1. 创建资源下载器。
    /// </summary>
    public class ProcedureCreateDownloader : ProcedureBase
    {
        /// <summary>
        /// 在Inspector中的显示优先级
        /// </summary>
        public override int Priority => 8;

        protected override void OnEnter()
        {
            base.OnEnter();
            FuLogger.LogInfo("<color=#43f656>------进入热更流程：创建资源下载器------</color>");

            GlobalModule.EventModule.Broadcast(this, AssetPatchStatesChangeEventArgs.Create(GlobalModule.AssetModule.DefaultPackageName, EPatchStates.CreateDownloader));
            CreateDownloader();
        }

        /// <summary>
        /// 创建资源下载器
        /// </summary>
        private void CreateDownloader()
        {
            // 创建资源下载器
            var downloader = GlobalModule.AssetModule.CreateResourceDownloader();

            // 将资源下载器保存到流程管理模块的Data变量(Downloader)中。
            var downloaderObj = new VarObject();
            downloaderObj.SetValue(downloader);
            Fsm.SetData("Downloader", downloaderObj);

            if (downloader.TotalDownloadCount == 0)
            {
                FuLogger.LogInfo("没有需要下载的资源");
                ChangeState<ProcedureUpdateDone>();
            }
            else
            {
                FuLogger.LogInfo($"一共{downloader.TotalDownloadCount}个资源需要更新下载。");
                var totalDownloadCount = downloader.TotalDownloadCount;
                var totalDownloadBytes = downloader.TotalDownloadBytes;
                GlobalModule.EventModule.Broadcast(this, AssetFoundUpdateFilesEventArgs.Create(downloader.PackageName, totalDownloadCount, totalDownloadBytes));
                ChangeState<ProcedureDownloadPackage>();
            }
        }
    }
}