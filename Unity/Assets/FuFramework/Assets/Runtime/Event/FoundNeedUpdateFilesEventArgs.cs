using FuFramework.Event.Runtime;

// ReSharper disable once CheckNamespace
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable InconsistentNaming
namespace FuFramework.Asset.Runtime
{
    /// <summary>
    /// 发现需要更新的资源文件事件
    /// </summary>
    public sealed class FoundNeedUpdateFilesEventArgs : GameEventArgs
    {
        public override string Id => EventId;

        private static readonly string EventId = typeof(FoundNeedUpdateFilesEventArgs).FullName;

        /// <summary>
        /// 包名称
        /// </summary>
        public string PackageName { get; set; }

        /// <summary>
        /// 总数量
        /// </summary>
        public int TotalCount { get; private set; }

        /// <summary>
        /// 总大小
        /// </summary>
        public long TotalSizeBytes { get; private set; }

        public override void Clear()
        {
            PackageName    = null;
            TotalCount     = 0;
            TotalSizeBytes = 0;
        }

        /// <summary>
        /// 创建发现需要更新的资源文件事件
        /// </summary>
        /// <param name="packageName">包名称</param>
        /// <param name="totalCount">总数量</param>
        /// <param name="totalSizeBytes">总大小</param>
        /// <returns></returns>
        public static FoundNeedUpdateFilesEventArgs Create(string packageName, int totalCount, long totalSizeBytes)
        {
            var foundUpdateFiles = ReferencePool.Runtime.ReferencePool.Acquire<FoundNeedUpdateFilesEventArgs>();
            foundUpdateFiles.TotalCount     = totalCount;
            foundUpdateFiles.TotalSizeBytes = totalSizeBytes;
            foundUpdateFiles.PackageName    = packageName;
            return foundUpdateFiles;
        }
    }
}