using FuFramework.Core.Runtime;
using FuFramework.Procedure.Runtime;
using FuFramework.Launcher.Runtime;
using FuFramework.Variable.Runtime;
using Launcher.UI;
using YooAsset;

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

            // 发送更新状态改变事件：创建资源下载器
            var updateStateChangeEvent = AssetUpdateStateChangeEventArgs.Create(GlobalModule.AssetModule.DefaultPackageName, EUpdateStates.CreateDownloader);
            GlobalModule.EventModule.Broadcast(this, updateStateChangeEvent);

            // 创建资源下载器
            CreateDownloader();
        }

        /// <summary>
        /// 创建资源下载器
        /// </summary>
        private void CreateDownloader()
        {
            // 创建资源下载器
            var downloader = GlobalModule.AssetModule.CreateResourceDownloader();
            if (downloader.TotalDownloadCount == 0)
            {
                FuLogger.LogInfo("没有需要下载的资源!");
                ChangeState<ProcedureUpdateDone>();
            }
            else
            {
                if (Fsm.GetData<VarObject>("UpdateConfig").Value is not RemoteUpdateConfig updateConfig)
                {
                    FuLogger.LogError("获取资源包的配置信息失败，请检查网络连接或配置信息是否正确！");
                    return;
                }

                // 需要显示更新提示框, 并等待用户点击更新后关闭提示框，然后跳转到下载资源包流程
                if (updateConfig.ShowUpdateTips)
                {
                    // 获取热更界面
                    var winLauncher = GlobalModule.UIModule.GetUI<WinLauncher>();
                    if (winLauncher == null)
                    {
                        FuLogger.LogError("热更界面获取失败！");
                        return;
                    }

                    winLauncher.ShowUpdateDialog(updateConfig.UpdateAnnouncement, () =>
                    {
                        winLauncher.SetUpdateSureUIState(false);
                        ToDownloadPackage(downloader);
                    });
                    return;
                }

                // 不需要显示更新提示框，直接跳转到下载资源包流程
                ToDownloadPackage(downloader);
            }

            // 移除流程中的保存的更新配置数据
            Fsm.RemoveData("UpdateConfig");
        }

        /// <summary>
        /// 跳转到下载资源包流程
        /// </summary>
        /// <param name="downloader"></param>
        private void ToDownloadPackage(ResourceDownloaderOperation downloader)
        {
            // 将资源下载器保存到流程管理模块数据中。
            var downloaderObj = new VarObject();
            downloaderObj.SetValue(downloader);
            Fsm.SetData("Downloader", downloaderObj);

            var totalDownloadCount = downloader.TotalDownloadCount;
            var totalDownloadBytes = downloader.TotalDownloadBytes;
            FuLogger.LogInfo($"一共'{totalDownloadCount}'个资源需要更新下载， 总大小为'{totalDownloadBytes}Byte'。");

            // 广播发现需要更新的资源事件
            var foundNeedUpdateAssetEventArgs = FoundNeedUpdateAssetEventArgs.Create(downloader.PackageName, totalDownloadCount, totalDownloadBytes);
            GlobalModule.EventModule.Broadcast(this, foundNeedUpdateAssetEventArgs);

            // 进入下载资源包流程
            ChangeState<ProcedureDownloadPackage>();
        }
    }
}