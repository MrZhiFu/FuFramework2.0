using FuFramework.Fsm.Runtime;
using FuFramework.Core.Runtime;
using FuFramework.Asset.Runtime;
using FuFramework.Procedure.Runtime;
using FuFramework.Entry.Runtime;

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
        public override int Priority => 8; // 显示优先级
        
        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            Log.Info("<color=#43f656>------进入热更流程：创建资源下载器------</color>");
            
            GlobalModule.EventModule.Fire(this, AssetPatchStatesChangeEventArgs.Create(AssetManager.Instance.DefaultPackageName, EPatchStates.CreateDownloader));
            CreateDownloader(procedureOwner);
        }

        /// <summary>
        /// 创建资源下载器
        /// </summary>
        /// <param name="procedureOwner"></param>
        private void CreateDownloader(IFsm<IProcedureManager> procedureOwner)
        {
            // 创建资源下载器
            var downloader = AssetManager.Instance.CreateResourceDownloader();

            // 将资源下载器保存到流程管理器的Data变量(Downloader)中。
            var downloaderObj = new VarObject();
            downloaderObj.SetValue(downloader);
            procedureOwner.SetData("Downloader", downloaderObj);

            if (downloader.TotalDownloadCount == 0)
            {
                Log.Info("没有需要下载的资源");
                ChangeState<ProcedureUpdateDone>(procedureOwner);
            }
            else
            {
                Log.Info($"一共{downloader.TotalDownloadCount}个资源需要更新下载。");
                var totalDownloadCount = downloader.TotalDownloadCount;
                var totalDownloadBytes = downloader.TotalDownloadBytes;
                GlobalModule.EventModule.Fire(this, AssetFoundUpdateFilesEventArgs.Create(downloader.PackageName, totalDownloadCount, totalDownloadBytes));
                ChangeState<ProcedureDownloadPackage>(procedureOwner);
            }
        }
    }
}