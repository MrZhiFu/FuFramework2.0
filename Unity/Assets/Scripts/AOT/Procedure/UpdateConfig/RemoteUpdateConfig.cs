// ReSharper disable once CheckNamespace
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Launcher.Procedure
{
    /// <summary>
    /// 远端资源更新配置
    /// </summary>
    public sealed class RemoteUpdateConfig
    {
        /// <summary>
        /// 是否强更
        /// </summary>
        public bool ForceUpdate { get; set; }

        /// <summary>
        /// 是否显示更新提示
        /// </summary>
        public bool ShowUpdateTips { get; set; }

        /// <summary>
        /// 更新公告
        /// </summary>
        public string UpdateAnnouncement { get; set; }

        /// <summary>
        /// App下载地址
        /// </summary>
        public string AppDownloadUrl { get; set; }

        /// <summary>
        /// 资源下载地址
        /// </summary>
        public string ResDownloadUrl { get; set; }

        /// <summary>
        /// 资源下载备用地址
        /// </summary>
        public string ResDownloadBackupUrl { get; set; }
    }
}