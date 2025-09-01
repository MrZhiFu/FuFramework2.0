using FuFramework.Event.Runtime;

// ReSharper disable once CheckNamespace
// ReSharper disable InconsistentNaming
namespace FuFramework.Asset.Runtime
{
    /// <summary>
    /// 下载进度更新
    /// </summary>
    public sealed class AssetDownloadProgressUpdateEventArgs : GameEventArgs
    {
        public static readonly string EventId = typeof(AssetDownloadProgressUpdateEventArgs).FullName;

        public override string Id => EventId;

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
        public static AssetDownloadProgressUpdateEventArgs Create(string packageName, int totalDownloadCount, int currentDownloadCount, long totalDownloadSizeBytes, long currentDownloadSizeBytes)
        {
            var assetDownloadProgressUpdate = ReferencePool.Runtime.ReferencePool.Acquire<AssetDownloadProgressUpdateEventArgs>();
            assetDownloadProgressUpdate.TotalDownloadCount       = totalDownloadCount;
            assetDownloadProgressUpdate.CurrentDownloadCount     = currentDownloadCount;
            assetDownloadProgressUpdate.TotalDownloadSizeBytes   = totalDownloadSizeBytes;
            assetDownloadProgressUpdate.CurrentDownloadSizeBytes = currentDownloadSizeBytes;
            assetDownloadProgressUpdate.PackageName              = packageName;
            return assetDownloadProgressUpdate;
        }
    }
}