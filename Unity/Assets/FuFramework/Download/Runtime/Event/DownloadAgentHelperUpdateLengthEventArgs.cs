using FuFramework.Core.Runtime;
using FuFramework.Event.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Download.Runtime
{
    /// <summary>
    /// 下载代理辅助器更新数据大小事件。
    /// </summary>
    public sealed class DownloadAgentHelperUpdateLengthEventArgs : GameEventArgs
    {
        /// <summary>
        /// 下载代理辅助器更新数据大小事件编号。
        /// </summary>
        public override string Id => EventId;

        /// <summary>
        /// 下载代理辅助器更新数据大小事件编号。
        /// </summary>
        public static readonly string EventId = typeof(DownloadAgentHelperUpdateLengthEventArgs).FullName;

        /// <summary>
        /// 获取下载的增量数据大小。
        /// </summary>
        public int DeltaLength { get; private set; }

        /// <summary>
        /// 清理下载代理辅助器更新数据大小事件。
        /// </summary>
        public override void Clear() => DeltaLength = 0;

        /// <summary>
        /// 创建下载代理辅助器更新数据大小事件。
        /// </summary>
        /// <param name="deltaLength">下载的增量数据大小。</param>
        /// <returns>创建的下载代理辅助器更新数据大小事件。</returns>
        public static DownloadAgentHelperUpdateLengthEventArgs Create(int deltaLength)
        {
            if (deltaLength <= 0) throw new FuException("下载的增量数据大小必须大于0.");

            var downloadAgentHelperUpdateLengthEventArgs = ReferencePool.Runtime.ReferencePool.Acquire<DownloadAgentHelperUpdateLengthEventArgs>();
            downloadAgentHelperUpdateLengthEventArgs.DeltaLength = deltaLength;
            return downloadAgentHelperUpdateLengthEventArgs;
        }
    }
}