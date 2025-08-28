using Launcher.UI;
using Cysharp.Threading.Tasks;
using FuFramework.UI.Runtime;
using FuFramework.Asset.Runtime;
using FuFramework.Event.Runtime;
using FuFramework.Entry.Runtime;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Launcher.Procedure
{
    /// <summary>
    /// 热更进度显示UI帮助类
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
            m_WinLauncher = await UIManager.Instance.OpenUIAsync<WinLauncher>();
            GameApp.Event.Subscribe(AssetDownloadProgressUpdateEventArgs.EventId, SetUpdateProgress);
        }

        /// <summary>
        /// 关闭并释放热更进度显示UI
        /// </summary>
        public static void Dispose()
        {
            UIManager.Instance.CloseUI<WinLauncher>();
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
        /// 设置Asset下载进度更新事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="gameEventArgs"></param>
        private static void SetUpdateProgress(object sender, GameEventArgs gameEventArgs)
        {
            m_WinLauncher.SetUpdateState(false);
            var message = (AssetDownloadProgressUpdateEventArgs)gameEventArgs;
            var progress = message.CurrentDownloadSizeBytes / (message.TotalDownloadSizeBytes * 1f);
            var currentSizeMb = Utility.File.GetBytesSize(message.CurrentDownloadSizeBytes);
            var totalSizeMb = Utility.File.GetBytesSize(message.TotalDownloadSizeBytes);
            m_WinLauncher.SetUpdateProgress(progress * 100);
            m_WinLauncher.SetTipText($"Downloading {currentSizeMb}/{totalSizeMb}");
        }
    }
}