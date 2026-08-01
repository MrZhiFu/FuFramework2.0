using System;
using Hotfix.Framework.Core;
using Hotfix.Framework.Event;

namespace Hotfix.Framework.Download
{
    /// <summary>
    /// 下载代理辅助器完成事件。
    /// </summary>
    public sealed class DownloadAgentHelperCompleteEventArgs : GameEventArgs
    {
        /// <summary>
        /// 下载代理辅助器完成事件编号。
        /// </summary>
        public override string Id => EventId;

        /// <summary>
        /// 下载代理辅助器完成事件编号。
        /// </summary>
        public static readonly string EventId = typeof(DownloadAgentHelperCompleteEventArgs).FullName;

        /// <summary>
        /// 获取下载的数据大小。
        /// </summary>
        public long Length { get; private set; }

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
            if (length < 0L) throw new InvalidOperationException("下载的数据大小无效，不能为负数.");
            var downloadAgentHelperCompleteEventArgs = GlobalModule.ReferencePoolModule.Acquire<DownloadAgentHelperCompleteEventArgs>();
            downloadAgentHelperCompleteEventArgs.Length = length;
            return downloadAgentHelperCompleteEventArgs;
        }
    }
}
