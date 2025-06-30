using GameFrameX.Asset.Runtime;
using GameFrameX.Event.Runtime;
using GameFrameX.Runtime;
using GameFrameX.UI.FairyGUI.Runtime;

namespace Unity.Startup.Procedure
{
    /// <summary>
    /// 热更进度显示UI帮助类
    /// </summary>
    public static class LauncherUIHelper
    {
        /// <summary>
        /// 热更进度显示UI界面
        /// </summary>
        private static UILauncher _ui;

        /// <summary>
        /// 开启热更进度显示UI
        /// </summary>
        public static async void Start()
        {
            _ui = await UIManager.Instance.OpenUIAsync<UILauncher>(isFromResources:true);
            GameApp.Event.CheckSubscribe(AssetDownloadProgressUpdateEventArgs.EventId, SetUpdateProgress);
        }

        /// <summary>
        /// 关闭并释放热更进度显示UI
        /// </summary>
        public static void Dispose()
        {
            UIManager.Instance.CloseUI<UILauncher>();
            _ui = null;
        }

        /// <summary>
        /// 设置下载时的提示文本
        /// </summary>
        /// <param name="text"></param>
        public static void SetTipText(string text)
        {
            _ui.m_TipText.text = text;
        }

        /// <summary>
        /// 设置为更新完成状态
        /// </summary>
        public static void SetProgressUpdateFinish()
        {
            _ui.m_IsDownload.SetSelectedIndex(0);
        }

        /// <summary>
        /// 设置Asset下载进度更新事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="gameEventArgs"></param>
        private static void SetUpdateProgress(object sender, GameEventArgs gameEventArgs)
        {
            _ui.m_IsDownload.SetSelectedIndex(1);
            var message       = (AssetDownloadProgressUpdateEventArgs)gameEventArgs;
            var progress      = message.CurrentDownloadSizeBytes / (message.TotalDownloadSizeBytes * 1f);
            var currentSizeMb = Utility.File.GetBytesSize(message.CurrentDownloadSizeBytes);
            var totalSizeMb   = Utility.File.GetBytesSize(message.TotalDownloadSizeBytes);
            _ui.m_ProgressBar.value = progress * 100;
            _ui.m_TipText.text      = $"Downloading {currentSizeMb}/{totalSizeMb}";
        }
    }
}