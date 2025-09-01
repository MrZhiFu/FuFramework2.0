using YooAsset;
using System.Collections;
using Cysharp.Threading.Tasks;
using FuFramework.Core.Runtime;
using FuFramework.Asset.Runtime;
using FuFramework.Entry.Runtime;
using FuFramework.Procedure.Runtime;
using FuFramework.Variable.Runtime;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Launcher.Procedure
{
    /// <summary>
    /// 热更流程--下载资源包。
    /// 主要作用是：
    /// 1. 下载热更包
    /// 2. 监听下载进度
    /// 3. 下载失败后，回到创建下载器的流程
    /// 4. 下载成功后，切换到更新完毕流程
    /// </summary>
    public class ProcedureDownloadPackage : ProcedureBase
    {
        public override int Priority => 9; // 显示优先级

        protected override void OnEnter()
        {
            base.OnEnter();
            Log.Info("<color=#43f656>------进入热更流程：下载资源包------</color>");

            GlobalModule.EventModule.Fire(this, AssetPatchStatesChangeEventArgs.Create(GlobalModule.AssetModule.DefaultPackageName, EPatchStates.Download));
            BeginDownload().ToUniTask().Forget();
        }

        /// <summary>
        /// 开始下载热更包
        /// </summary>
        /// <returns></returns>
        private IEnumerator BeginDownload()
        {
            var downloader = Fsm.GetData<VarObject>("Downloader").GetValue() as ResourceDownloaderOperation;
            if (downloader == null) yield break;

            downloader.DownloadErrorCallback  = DownloaderOnDownloadErrorCallback;
            downloader.DownloadUpdateCallback = OnDownloadProgressCallback;

            // 开始下载
            downloader.BeginDownload();
            yield return downloader;

            // 下载是否成功
            if (downloader.Status != EOperationStatus.Succeed) yield break;

            // 下载完成，移除记录的下载器，切换到更新完毕流程
            Fsm.RemoveData("Downloader");
            ChangeState<ProcedureUpdateDone>();
        }

        /// <summary>
        /// 下载失败回调, 重新创建下载器流程
        /// </summary>
        /// <param name="errorData"></param>
        private void DownloaderOnDownloadErrorCallback(DownloadErrorData errorData)
        {
            GlobalModule.EventModule.Fire(this, AssetWebFileDownloadFailedEventArgs.Create(errorData.PackageName, errorData.FileName, errorData.ErrorInfo));
            ChangeState<ProcedureCreateDownloader>();
        }

        /// <summary>
        /// 下载中进度回调，派发下载进度事件，通过事件更新进度条
        /// </summary>
        /// <param name="data">下载中的数据</param>
        private void OnDownloadProgressCallback(DownloadUpdateData data)
        {
            var packageName          = data.PackageName;
            var totalDownloadCount   = data.TotalDownloadCount;
            var currentDownloadCount = data.CurrentDownloadCount;
            var totalDownloadBytes   = data.TotalDownloadBytes;
            var currentDownloadBytes = data.CurrentDownloadBytes;
            GlobalModule.EventModule.Fire(this, AssetDownloadProgressUpdateEventArgs.Create(packageName, totalDownloadCount, currentDownloadCount, totalDownloadBytes, currentDownloadBytes));
        }
    }
}