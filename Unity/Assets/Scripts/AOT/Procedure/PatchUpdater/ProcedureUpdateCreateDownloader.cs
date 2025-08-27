using FuFramework.Asset.Runtime;
using FuFramework.Fsm.Runtime;
using FuFramework.Procedure.Runtime;
using FuFramework.Core.Runtime;
using FuFramework.Entry.Runtime;
using YooAsset;

namespace Unity.Startup.Procedure
{
    /// <summary>
    /// 热更流程--创建资源下载器流程。
    /// 主要作用是：
    /// 1. 创建资源下载器。
    /// </summary>
    public class ProcedureUpdateCreateDownloader : ProcedureBase
    {
        private const int DownloadingMaxNum = 10; // 同时下载的最大文件数
        private const int FailedTryAgain    = 3;  // 失败后重试次数

        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            Log.Info("<color=#43f656>------进入热更流程：创建资源下载器------</color>");
            
            GameApp.Event.Fire(this, AssetPatchStatesChangeEventArgs.Create(AssetManager.Instance.DefaultPackageName, EPatchStates.CreateDownloader));
            CreateDownloader(procedureOwner);
        }

        /// <summary>
        /// 创建资源下载器
        /// </summary>
        /// <param name="procedureOwner"></param>
        private void CreateDownloader(IFsm<IProcedureManager> procedureOwner)
        {
            Log.Info("创建资源下载器.");

            var downloader = YooAssets.CreateResourceDownloader(DownloadingMaxNum, FailedTryAgain);

            // 将资源下载器保存到流程管理器的Data变量(Downloader)中。
            var downloaderObj = new VarObject();
            downloaderObj.SetValue(downloader);
            procedureOwner.SetData("Downloader", downloaderObj);

            if (downloader.TotalDownloadCount == 0)
            {
                Log.Info("没有发现需要下载的资源");
                ChangeState<ProcedureUpdateDone>(procedureOwner);
            }
            else
            {
                Log.Info($"一共发现了{downloader.TotalDownloadCount}个资源需要更新下载。");

                // 发现新更新文件后，挂起流程系统
                var totalDownloadCount = downloader.TotalDownloadCount;
                var totalDownloadBytes = downloader.TotalDownloadBytes;

                GameApp.Event.Fire(this, AssetFoundUpdateFilesEventArgs.Create(downloader.PackageName, totalDownloadCount, totalDownloadBytes));
                ChangeState<ProcedureUpdateDownload>(procedureOwner);
            }
        }
    }
}