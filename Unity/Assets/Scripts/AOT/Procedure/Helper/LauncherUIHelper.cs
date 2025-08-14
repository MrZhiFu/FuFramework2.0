using AOT.UI.View.Launcher;
using Cysharp.Threading.Tasks;
using FuFramework.UI.Runtime;
using FuFramework.Asset.Runtime;
using GameFrameX.Event.Runtime;
using FuFramework.Core.Runtime;
using Utility = FuFramework.Core.Runtime.Utility;

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
        private static WinLauncher _ui;

        /// <summary>
        /// 开启热更进度显示UI
        /// </summary>
        public static async UniTaskVoid Start()
        {
            _ui = await UIManager.Instance.OpenUIAsync<WinLauncher>();
            GameApp.Event.Subscribe(AssetDownloadProgressUpdateEventArgs.EventId, SetUpdateProgress);
        }

        /// <summary>
        /// 关闭并释放热更进度显示UI
        /// </summary>
        public static void Dispose()
        {
            UIManager.Instance.CloseUI<WinLauncher>();
            _ui = null;
        }

        /// <summary>
        /// 设置下载时的提示文本
        /// </summary>
        /// <param name="text"></param>
        public static void SetTipText(string text)
        {
            _ui.SetTipText(text);
        }

        /// <summary>
        /// 设置为更新完成状态
        /// </summary>
        public static void SetProgressUpdateFinish()
        {
            _ui.SetUpdateState(true);
        }

        /// <summary>
        /// 设置Asset下载进度更新事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="gameEventArgs"></param>
        private static void SetUpdateProgress(object sender, GameEventArgs gameEventArgs)
        {
            _ui.SetUpdateState(false);
            var message = (AssetDownloadProgressUpdateEventArgs)gameEventArgs;
            var progress = message.CurrentDownloadSizeBytes / (message.TotalDownloadSizeBytes * 1f);
            var currentSizeMb = Utility.File.GetBytesSize(message.CurrentDownloadSizeBytes);
            var totalSizeMb = Utility.File.GetBytesSize(message.TotalDownloadSizeBytes);
            _ui.SetUpdateProgress(progress * 100);
            _ui.SetTipText($"Downloading {currentSizeMb}/{totalSizeMb}");
        }
    }
}