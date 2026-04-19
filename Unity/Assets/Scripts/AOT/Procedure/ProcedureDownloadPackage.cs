using YooAsset;
using Cysharp.Threading.Tasks;
using FuFramework.Core.Runtime;
using FuFramework.Launcher.Runtime;
using FuFramework.Procedure.Runtime;
using FuFramework.Variable.Runtime;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Launcher.Procedure
{
    /// <summary>
    /// 热更流程--下载热更资源包流程。
    /// 功能：
    ///     1.下载热更资源包。
    ///     2.监听下载进度。
    ///     3.下载成功 => 进入更新完毕流程。
    ///     4.下载失败 => 重新回到创建下载器的流程。
    /// </summary>
    public class ProcedureDownloadPackage : ProcedureBase
    {
#if UNITY_EDITOR
        /// <summary>
        /// 在Inspector中的显示优先级
        /// </summary>
        public override int Priority => 7;
#endif

        protected override void OnEnter()
        {
            base.OnEnter();
            FuLogger.LogInfo("<color=#43f656>------进入热更流程：下载资源包------</color>");

            // 发送更新状态改变事件: 开始下载热更资源包
            var updateStateChangeEvent = AssetUpdateStateChangeEventArgs.Create(GlobalModule.AssetModule.DefaultPackageName, EUpdateStates.Download);
            GlobalModule.EventModule.Broadcast(this, updateStateChangeEvent);

            // 开始异步下载热更资源包
            BeginDownloadAsync().Forget();
        }

        /// <summary>
        /// 开始异步下载热更资源包
        /// </summary>
        /// <returns></returns>
        private async UniTaskVoid BeginDownloadAsync()
        {
            // 获取下载器
            if (Fsm.GetData<VarObject>("Downloader").GetValue() is not ResourceDownloaderOperation downloader)
            {
                FuLogger.LogError("下载器为空，无法下载资源包！");
                return;
            }

            // 设置下载回调
            downloader.DownloadErrorCallback  = DownloaderOnDownloadErrorCallback;
            downloader.DownloadUpdateCallback = OnDownloadProgressCallback;

            // 开始下载并等待下载完成
            downloader.BeginDownload();
            await downloader;

            // 下载是否成功
            if (downloader.Status != EOperationStatus.Succeed)
            {
                FuLogger.LogError("下载器状态异常，无法下载资源包！");
                return;
            }

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
            var downloadFailedEvent = AssetDownloadFailedEventArgs.Create(errorData.PackageName, errorData.FileName, errorData.ErrorInfo);
            GlobalModule.EventModule.Broadcast(this, downloadFailedEvent);
            ChangeState<ProcedureCreateDownloader>();
        }

        /// <summary>
        /// 下载中进度回调，发送下载进度事件，通过事件更新下载进度条
        /// </summary>
        /// <param name="data">下载中的数据</param>
        private void OnDownloadProgressCallback(DownloadUpdateData data)
        {
            var packageName          = data.PackageName;
            var totalDownloadCount   = data.TotalDownloadCount;
            var currentDownloadCount = data.CurrentDownloadCount;
            var totalDownloadBytes   = data.TotalDownloadBytes;
            var currentDownloadBytes = data.CurrentDownloadBytes;

            var downloadProgressEvent = AssetDownloadProgressEventArgs.Create(packageName, totalDownloadCount, currentDownloadCount, totalDownloadBytes, currentDownloadBytes);
            GlobalModule.EventModule.Broadcast(this, downloadProgressEvent);
        }
    }
}