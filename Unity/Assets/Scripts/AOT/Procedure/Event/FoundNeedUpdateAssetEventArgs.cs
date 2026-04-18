using FuFramework.Event.Runtime;
using FuFramework.ReferencePool.Runtime;

// ReSharper disable once CheckNamespace
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable InconsistentNaming
namespace Launcher.Procedure
{
    /// <summary>
    /// 发现需要更新的资源事件
    /// </summary>
    public sealed class FoundNeedUpdateAssetEventArgs : GameEventArgs
    {
        public override string Id => EventId;

        private static readonly string EventId = typeof(FoundNeedUpdateAssetEventArgs).FullName;

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
        public static FoundNeedUpdateAssetEventArgs Create(string packageName, int totalCount, long totalSizeBytes)
        {
            var foundUpdateFiles = ReferencePool.Acquire<FoundNeedUpdateAssetEventArgs>();
            foundUpdateFiles.TotalCount     = totalCount;
            foundUpdateFiles.TotalSizeBytes = totalSizeBytes;
            foundUpdateFiles.PackageName    = packageName;
            return foundUpdateFiles;
        }
    }
}