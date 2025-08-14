using System.Collections;
using Cysharp.Threading.Tasks;
using FuFramework.Asset.Runtime;
using GameFrameX.Fsm.Runtime;
using GameFrameX.Procedure.Runtime;
using FuFramework.Core.Runtime;
using YooAsset;

namespace Unity.Startup.Procedure
{
    /// <summary>
    /// 热更流程--下载热更包。
    /// 主要作用是：
    /// 1. 下载热更包
    /// 2. 监听下载进度
    /// 3. 下载失败后，回到创建下载器的流程
    /// 4. 下载成功后，切换到更新完毕流程
    /// </summary>
    public class ProcedureUpdateDownload : ProcedureBase
    {
        private IFsm<IProcedureManager> _procedureOwner;
        
        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            _procedureOwner = procedureOwner;
            
            GameApp.Event.Fire(this, AssetPatchStatesChangeEventArgs.Create(AssetComponent.BuildInPackageName, EPatchStates.DownloadWebFiles));
            BeginDownload(procedureOwner).ToUniTask();
        }
        
        /// <summary>
        /// 开始下载热更包
        /// </summary>
        /// <param name="procedureOwner"></param>
        /// <returns></returns>
        private IEnumerator BeginDownload(IFsm<IProcedureManager> procedureOwner)
        {
            var downloader = (ResourceDownloaderOperation)procedureOwner.GetData<VarObject>("Downloader").GetValue();

            downloader.OnDownloadErrorCallback    = DownloaderOnDownloadErrorCallback;
            downloader.OnDownloadProgressCallback = OnDownloadProgressCallback;
            downloader.BeginDownload();
            yield return downloader;

            // 检测下载结果
            if (downloader.Status != EOperationStatus.Succeed)
            {
                yield break;
            }

            // 下载完成，切换到更新完毕流程
            ChangeState<ProcedureUpdateDone>(procedureOwner);
        }
        
        /// <summary>
        /// 下载失败回调, 重新创建下载器流程
        /// </summary>
        /// <param name="packageName"></param>
        /// <param name="name"></param>
        /// <param name="error"></param>
        private void DownloaderOnDownloadErrorCallback(string packageName, string name, string error)
        {
            GameApp.Event.Fire(this, AssetWebFileDownloadFailedEventArgs.Create(packageName, name, error));
            ChangeState<ProcedureUpdateCreateDownloader>(_procedureOwner);
        }

        /// <summary>
        /// 下载中进度回调，派发下载进度事件，通过事件更新进度条
        /// </summary>
        /// <param name="packageName"></param>
        /// <param name="totalDownloadCount"></param>
        /// <param name="currentDownloadCount"></param>
        /// <param name="totalDownloadBytes"></param>
        /// <param name="currentDownloadBytes"></param>
        
        private void OnDownloadProgressCallback(string packageName, int totalDownloadCount, int currentDownloadCount, long totalDownloadBytes, long currentDownloadBytes)
        {
            GameApp.Event.Fire(this, AssetDownloadProgressUpdateEventArgs.Create(packageName, totalDownloadCount, currentDownloadCount, totalDownloadBytes, currentDownloadBytes));
        }
    }
}