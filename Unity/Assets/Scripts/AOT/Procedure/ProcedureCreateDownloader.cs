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
    /// 功能：
    ///     1. 创建资源下载器，并将其保存到流程管理模块的变量(Downloader)中。
    ///     2. 如果没有需要下载的资源，则直接进入资源更新完毕流程。
    ///     3. 如果有需要下载的资源，则进入下载资源包流程。
    /// </summary>
    public class ProcedureCreateDownloader : ProcedureBase
    {
#if UNITY_EDITOR
        /// <summary>
        /// 在Inspector中的显示优先级
        /// </summary>
        public override int Priority => 6;
#endif

        protected override void OnEnter()
        {
            base.OnEnter();
            FuLogger.LogInfo("<color=#43f656>------进入热更流程：创建资源下载器------</color>");

            GlobalModule.EventModule.Broadcast(this, AssetUpdateStateChangeEventArgs.Create(GlobalModule.AssetModule.DefaultPackageName, EUpdateStates.CreateDownloader));
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
                GlobalModule.EventModule.Broadcast(this, FoundNeedUpdateFilesEventArgs.Create(downloader.PackageName, totalDownloadCount, totalDownloadBytes));

                // 进入下载资源包流程
                ChangeState<ProcedureDownloadPackage>();
            }
        }
    }
}