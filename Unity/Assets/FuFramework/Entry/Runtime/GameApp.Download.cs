using FuFramework.Core.Runtime;
using FuFramework.Download.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entry.Runtime
{
    public static partial class GameApp
    {
        private static DownloadComponent _download;

        /// <summary>
        /// 获取下载组件。
        /// </summary>
        public static DownloadComponent Download
        {
            get
            {
                if (!_download) _download = GameEntry.GetComponent<DownloadComponent>();
                return _download;
            }
        }
    }
}