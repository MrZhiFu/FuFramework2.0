using FuFramework.Core.Runtime;
using GameFrameX.Event.Runtime;
using ReferencePool = FuFramework.Core.Runtime.ReferencePool;

// ReSharper disable once CheckNamespace
namespace FuFramework.Download.Runtime
{
    /// <summary>
    /// 下载代理辅助器完成事件。
    /// </summary>
    public sealed class DownloadAgentHelperCompleteEventArgs : GameEventArgs
    {
        private static readonly string s_EventId = typeof(DownloadAgentHelperCompleteEventArgs).FullName;
        
        /// <summary>
        /// 获取下载的数据大小。
        /// </summary>
        public long Length { get; private set; }

        /// <summary>
        /// 初始化下载代理辅助器完成事件的新实例。
        /// </summary>
        public DownloadAgentHelperCompleteEventArgs() => Length = 0L;

        public override string Id => s_EventId;

        /// <summary>
        /// 清理下载代理辅助器完成事件。
        /// </summary>
        public override void Clear() => Length = 0L;

        /// <summary>
        /// 创建下载代理辅助器完成事件。
        /// </summary>
        /// <param name="length">下载的数据大小。</param>
        /// <returns>创建的下载代理辅助器完成事件。</returns>
        public static DownloadAgentHelperCompleteEventArgs Create(long length)
        {
            if (length < 0L) throw new FuException("Length is invalid.");
            var downloadAgentHelperCompleteEventArgs = ReferencePool.Acquire<DownloadAgentHelperCompleteEventArgs>();
            downloadAgentHelperCompleteEventArgs.Length = length;
            return downloadAgentHelperCompleteEventArgs;
        }
    }
}