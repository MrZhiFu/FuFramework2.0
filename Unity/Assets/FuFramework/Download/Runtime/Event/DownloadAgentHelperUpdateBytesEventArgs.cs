using FuFramework.Core.Runtime;
using FuFramework.Event.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Download.Runtime
{
    /// <summary>
    /// 下载代理辅助器更新数据流事件。
    /// </summary>
    public sealed class DownloadAgentHelperUpdateBytesEventArgs : GameEventArgs
    {
        /// <summary>
        /// 下载代理辅助器更新数据流事件编号
        /// </summary>
        public override string Id => EventId;

        /// <summary>
        /// 下载代理辅助器更新数据流事件编号
        /// </summary>
        public static readonly string EventId = typeof(DownloadAgentHelperUpdateBytesEventArgs).FullName;

        /// <summary>
        /// 下载的数据流。
        /// </summary>
        private byte[] m_Bytes;

        /// <summary>
        /// 获取数据流的偏移。
        /// </summary>
        public int Offset { get; private set; }

        /// <summary>
        /// 获取数据流的长度。
        /// </summary>
        public int Length { get; private set; }

        /// <summary>
        /// 清理下载代理辅助器更新数据流事件。
        /// </summary>
        public override void Clear()
        {
            m_Bytes = null;
            Offset  = 0;
            Length  = 0;
        }

        /// <summary>
        /// 获取下载的数据流。
        /// </summary>
        public byte[] GetBytes() => m_Bytes;

        /// <summary>
        /// 创建下载代理辅助器更新数据流事件。
        /// </summary>
        /// <param name="bytes">下载的数据流。</param>s
        /// <param name="offset">数据流的偏移。</param>
        /// <param name="length">数据流的长度。</param>
        /// <returns>创建的下载代理辅助器更新数据流事件。</returns>
        public static DownloadAgentHelperUpdateBytesEventArgs Create(byte[] bytes, int offset, int length)
        {
            if (bytes == null) throw new FuException("下载的数据流不能为空.");
            if (offset < 0  || offset          >= bytes.Length) throw new FuException("数据流的偏移不正确.");
            if (length <= 0 || offset + length > bytes.Length) throw new FuException("数据流的长度不正确.");

            var downloadAgentHelperUpdateBytesEventArgs = ReferencePool.Runtime.ReferencePool.Acquire<DownloadAgentHelperUpdateBytesEventArgs>();
            downloadAgentHelperUpdateBytesEventArgs.m_Bytes = bytes;
            downloadAgentHelperUpdateBytesEventArgs.Offset  = offset;
            downloadAgentHelperUpdateBytesEventArgs.Length  = length;
            return downloadAgentHelperUpdateBytesEventArgs;
        }
    }
}