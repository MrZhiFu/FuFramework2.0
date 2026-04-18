using FuFramework.Event.Runtime;
using FuFramework.ReferencePool.Runtime;

// ReSharper disable once CheckNamespace
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable InconsistentNaming
namespace Launcher.Procedure
{
    /// <summary>
    /// 资源下载失败事件
    /// </summary>
    public sealed class AssetDownloadFailedEventArgs : GameEventArgs
    {
        public override string Id => EventId;

        private static readonly string EventId = typeof(AssetDownloadFailedEventArgs).FullName;

        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string Error { get; private set; }

        /// <summary>
        /// 包名称
        /// </summary>
        public string PackageName { get; set; }

        public override void Clear()
        {
            FileName    = null;
            Error       = null;
            PackageName = null;
        }

        /// <summary>
        /// 创建网络文件下载失败
        /// </summary>
        /// <param name="packageName">包名称</param>
        /// <param name="fileName">文件名</param>
        /// <param name="error">错误信息</param>
        /// <returns></returns>
        public static AssetDownloadFailedEventArgs Create(string packageName, string fileName, string error)
        {
            var assetWebFileDownloadFailed = ReferencePool.Acquire<AssetDownloadFailedEventArgs>();
            assetWebFileDownloadFailed.FileName    = fileName;
            assetWebFileDownloadFailed.Error       = error;
            assetWebFileDownloadFailed.PackageName = packageName;
            return assetWebFileDownloadFailed;
        }
    }
}