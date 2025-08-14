using FuFramework.Core.Runtime;
using FuFramework.Event.Runtime;
using ReferencePool = FuFramework.Core.Runtime.ReferencePool;

// ReSharper disable once CheckNamespace
namespace FuFramework.Download.Runtime
{
    /// <summary>
    /// 下载代理辅助器更新数据大小事件。
    /// </summary>
    public sealed class DownloadAgentHelperUpdateLengthEventArgs : GameEventArgs
    {
        private static readonly string s_EventId = typeof(DownloadAgentHelperUpdateLengthEventArgs).FullName;

        /// <summary>
        /// 获取下载的增量数据大小。
        /// </summary>
        public int DeltaLength { get; private set; }

        /// <summary>
        /// 初始化下载代理辅助器更新数据大小事件的新实例。
        /// </summary>
        public DownloadAgentHelperUpdateLengthEventArgs()
        {
            DeltaLength = 0;
        }

        public override string Id => s_EventId;

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
            if (deltaLength <= 0) throw new FuException("Delta length is invalid.");

            var downloadAgentHelperUpdateLengthEventArgs = ReferencePool.Acquire<DownloadAgentHelperUpdateLengthEventArgs>();
            downloadAgentHelperUpdateLengthEventArgs.DeltaLength = deltaLength;
            return downloadAgentHelperUpdateLengthEventArgs;
        }
    }
}