using YooAsset;
using System.Collections;
using Cysharp.Threading.Tasks;
using FuFramework.Fsm.Runtime;
using FuFramework.Core.Runtime;
using FuFramework.Entry.Runtime;
using FuFramework.Asset.Runtime;
using FuFramework.Procedure.Runtime;

namespace Unity.Startup.Procedure
{
    /// <summary>
    /// 热更流程--下载热更资源包。
    /// 主要作用是：
    /// 1. 下载热更资源包
    /// 2. 监听下载进度
    /// 3. 下载失败后，回到创建下载器的流程
    /// 4. 下载成功后，切换到更新完毕流程
    /// </summary>
    public class ProcedureUpdateDownload : ProcedureBase
    {
        private IFsm<IProcedureManager> _procedureOwner;
        
        protected override async void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            Log.Info("<color=#43f656>------进入热更流程--下载热更资源包-----</color>");
            
            _procedureOwner = procedureOwner;
            GameApp.Event.Fire(this, AssetPatchStatesChangeEventArgs.Create(AssetManager.Instance.DefaultPackageName, EPatchStates.DownloadWebFiles));
            await BeginDownload(procedureOwner).ToUniTask();
        }
        
        /// <summary>
        /// 开始下载热更包
        /// </summary>
        /// <param name="procedureOwner"></param>
        /// <returns></returns>
        private IEnumerator BeginDownload(IFsm<IProcedureManager> procedureOwner)
        {
            var downloader = procedureOwner.GetData<VarObject>("Downloader").GetValue() as ResourceDownloaderOperation;
            if (downloader == null) yield break;
            
            downloader.DownloadErrorCallback  = DownloaderOnDownloadErrorCallback;
            downloader.DownloadUpdateCallback = OnDownloadProgressCallback;
            downloader.BeginDownload();
            yield return downloader;

            // 检测下载结果
            if (downloader.Status != EOperationStatus.Succeed) yield break;

            // 下载完成，切换到更新完毕流程
            ChangeState<ProcedureUpdateDone>(procedureOwner);
        }

        /// <summary>
        /// 下载失败回调, 重新创建下载器流程
        /// </summary>
        /// <param name="errorData"></param>
        private void DownloaderOnDownloadErrorCallback(DownloadErrorData errorData)
        {
            GameApp.Event.Fire(this, AssetWebFileDownloadFailedEventArgs.Create(errorData.PackageName, errorData.FileName, errorData.ErrorInfo));
            ChangeState<ProcedureUpdateCreateDownloader>(_procedureOwner);
        }

        /// <summary>
        /// 下载中进度回调，派发下载进度事件，通过事件更新进度条
        /// </summary>
        /// <param name="data">下载中的数据</param>
        private void OnDownloadProgressCallback(DownloadUpdateData data)
        {
            var packageName = data.PackageName;
            var totalDownloadCount = data.TotalDownloadCount;
            var currentDownloadCount = data.CurrentDownloadCount;
            var totalDownloadBytes = data.TotalDownloadBytes;
            var currentDownloadBytes = data.CurrentDownloadBytes;
            GameApp.Event.Fire(this, AssetDownloadProgressUpdateEventArgs.Create(packageName, totalDownloadCount, currentDownloadCount, totalDownloadBytes, currentDownloadBytes));
        }
    }
}