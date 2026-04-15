using Launcher.UI;
using Cysharp.Threading.Tasks;
using FuFramework.Asset.Runtime;
using FuFramework.Event.Runtime;
using FuFramework.Launcher.Runtime;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Launcher.Procedure
{
    /// <summary>
    /// 热更UI帮助类。
    /// 功能：
    ///     1.打开热更UI界面。
    ///     2.显示热更资源下载进度。
    ///     3.设置下载时的提示文本。
    /// </summary>
    public static class LauncherUIHelper
    {
        /// <summary>
        /// 热更进度显示UI界面
        /// </summary>
        private static WinLauncher m_WinLauncher;

        /// <summary>
        /// 开启热更进度显示UI
        /// </summary>
        public static async UniTask Start()
        {
            m_WinLauncher = await GlobalModule.UIModule.OpenUIAsync<WinLauncher>();
            GlobalModule.EventModule.Subscribe(AssetDownloadProgressEventArgs.EventId, OnAssetDownloadProgressUpdate);
        }

        /// <summary>
        /// 关闭并释放热更进度显示UI
        /// </summary>
        public static void Dispose()
        {
            GlobalModule.UIModule.CloseUI<WinLauncher>();
            m_WinLauncher = null;
        }

        /// <summary>
        /// 设置下载时的提示文本
        /// </summary>
        /// <param name="text"></param>
        public static void SetTipText(string text) => m_WinLauncher.SetTipText(text);

        /// <summary>
        /// 设置为更新完成状态
        /// </summary>
        public static void SetProgressUpdateFinish() => m_WinLauncher.SetUpdateState(true);

        /// <summary>
        /// 资源下载进度更新事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="gameEventArgs"></param>
        private static void OnAssetDownloadProgressUpdate(object sender, GameEventArgs gameEventArgs)
        {
            m_WinLauncher.SetUpdateState(false);
            var message       = (AssetDownloadProgressEventArgs)gameEventArgs;
            var progress      = message.CurrentDownloadSizeBytes / (message.TotalDownloadSizeBytes * 1f);
            var currentSizeMb = Utility.File.GetBytesSizeWithUnit(message.CurrentDownloadSizeBytes);
            var totalSizeMb   = Utility.File.GetBytesSizeWithUnit(message.TotalDownloadSizeBytes);
            m_WinLauncher.SetUpdateProgress(progress * 100);
            m_WinLauncher.SetTipText($"Downloading {currentSizeMb}/{totalSizeMb}");
        }
    }
}