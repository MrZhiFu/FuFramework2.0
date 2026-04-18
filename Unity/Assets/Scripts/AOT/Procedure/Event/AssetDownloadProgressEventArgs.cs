using FuFramework.Event.Runtime;
using FuFramework.ReferencePool.Runtime;

// ReSharper disable once CheckNamespace
// ReSharper disable InconsistentNaming
namespace Launcher.Procedure
{
    /// <summary>
    /// 资源下载进度事件
    /// </summary>
    public sealed class AssetDownloadProgressEventArgs : GameEventArgs
    {
        public override string Id => EventId;

        public static readonly string EventId = typeof(AssetDownloadProgressEventArgs).FullName;

        /// <summary>
        /// 包名称
        /// </summary>
        public string PackageName { get; set; }

        /// <summary>
        /// 总下载数量
        /// </summary>
        public int TotalDownloadCount { get; private set; }

        /// <summary>
        /// 当前下载数量
        /// </summary>
        public int CurrentDownloadCount { get; private set; }

        /// <summary>
        /// 总下载大小
        /// </summary>
        public long TotalDownloadSizeBytes { get; private set; }

        /// <summary>
        /// 当前下载大小
        /// </summary>
        public long CurrentDownloadSizeBytes { get; private set; }

        public override void Clear()
        {
            PackageName              = null;
            TotalDownloadCount       = 0;
            CurrentDownloadCount     = 0;
            TotalDownloadSizeBytes   = 0;
            CurrentDownloadSizeBytes = 0;
        }

        /// <summary>
        /// 创建下载进度更新
        /// </summary>
        /// <param name="packageName">包名称</param>
        /// <param name="totalDownloadCount">总下载数量</param>
        /// <param name="currentDownloadCount">当前下载数量</param>
        /// <param name="totalDownloadSizeBytes">总下载大小</param>
        /// <param name="currentDownloadSizeBytes">当前下载大小</param>
        /// <returns></returns>
        public static AssetDownloadProgressEventArgs Create(string packageName, int totalDownloadCount, int currentDownloadCount, long totalDownloadSizeBytes, long currentDownloadSizeBytes)
        {
            var assetDownloadProgressUpdate = ReferencePool.Acquire<AssetDownloadProgressEventArgs>();
            assetDownloadProgressUpdate.TotalDownloadCount       = totalDownloadCount;
            assetDownloadProgressUpdate.CurrentDownloadCount     = currentDownloadCount;
            assetDownloadProgressUpdate.TotalDownloadSizeBytes   = totalDownloadSizeBytes;
            assetDownloadProgressUpdate.CurrentDownloadSizeBytes = currentDownloadSizeBytes;
            assetDownloadProgressUpdate.PackageName              = packageName;
            return assetDownloadProgressUpdate;
        }
    }
}