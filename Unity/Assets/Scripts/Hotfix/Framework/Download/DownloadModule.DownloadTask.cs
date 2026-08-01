using Hotfix.Framework.TaskPool;
using Hotfix.Framework.Core;

namespace Hotfix.Framework.Download
{
    public sealed partial class DownloadModule
    {
        /// <summary>
        /// 下载任务。
        /// 功能：
        ///     1. 继承于任务池的任务基类，这样可使用任务池优化下载任务。
        ///     1. 单个下载任务，存储了一次下载的任务信息，包括序列编号、状态、下载后存放全路径、原始下载地址、将缓冲区写入磁盘的临界大小、下载超时时长。
        /// </summary>
        private sealed class DownloadTask : TaskBase
        {
            /// 下载任务的序列编号
            private static int m_Serial;

            /// <summary>
            /// 获取或设置下载任务的状态。
            /// </summary>
            public DownloadTaskStatus Status { get; set; } = DownloadTaskStatus.Todo;

            /// <summary>
            /// 获取下载后存放全路径。
            /// </summary>
            public string DownloadedFullPath { get; private set; }

            /// <summary>
            /// 获取原始下载地址。
            /// </summary>
            public string DownloadUri { get; private set; }

            /// <summary>
            /// 获取将缓冲区写入磁盘的临界大小。
            /// </summary>
            public int FlushSize { get; private set; }

            /// <summary>
            /// 获取下载超时时长，以秒为单位。
            /// </summary>
            public float Timeout { get; private set; }

            /// <summary>
            /// 获取下载任务的描述。
            /// </summary>
            public override string Description => DownloadedFullPath;

            /// <summary>
            /// 清理下载任务。
            /// </summary>
            public override void Clear()
            {
                base.Clear();

                FlushSize          = 0;
                Timeout            = 0f;
                DownloadedFullPath = null;
                DownloadUri        = null;
                Status             = DownloadTaskStatus.Todo;
            }

            /// <summary>
            /// 创建下载任务。
            /// </summary>
            /// <param name="downloadedFullPath">下载后存放全路径。</param>
            /// <param name="downloadUri">原始下载地址。</param>
            /// <param name="tag">下载任务的标签。</param>
            /// <param name="priority">下载任务的优先级。</param>
            /// <param name="flushSize">将缓冲区写入磁盘的临界大小。</param>
            /// <param name="timeout">下载超时时长，以秒为单位。</param>
            /// <param name="userData">用户自定义数据。</param>
            /// <returns>创建的下载任务。</returns>
            public static DownloadTask Create(string downloadedFullPath, string downloadUri, string tag, int priority, int flushSize, float timeout, object userData)
            {
                var downloadTask = GlobalModule.ReferencePoolModule.Acquire<DownloadTask>();
                downloadTask.Initialize(++m_Serial, tag, priority, userData);
                downloadTask.DownloadedFullPath = downloadedFullPath;
                downloadTask.DownloadUri        = downloadUri;
                downloadTask.FlushSize          = flushSize;
                downloadTask.Timeout            = timeout;
                return downloadTask;
            }
        }
    }
}
