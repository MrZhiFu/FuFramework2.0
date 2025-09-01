using FuFramework.Core.Runtime;
using ReferencePool = FuFramework.Core.Runtime.ReferencePool;

// ReSharper disable once CheckNamespace
namespace FuFramework.Download.Runtime
{
    public sealed partial class DownloadManager
    {
        /// <summary>
        /// 下载任务。
        /// 是单个下载任务，存储了一次下载的所有变量信息，但自身并不执行下载，而是委托给了Agent代理进行下载，是一种代理模式的实现
        /// </summary>
        private sealed class DownloadTask : TaskBase
        {
            /// 下载任务的序列编号
            private static int _serial;

            /// <summary>
            /// 初始化下载任务的新实例。
            /// </summary>
            public DownloadTask()
            {
                FlushSize      = 0;
                Timeout        = 0f;
                DownloadedPath = null;
                DownloadUri    = null;
                Status         = DownloadTaskStatus.Todo;
            }

            /// <summary>
            /// 获取或设置下载任务的状态。
            /// </summary>
            public DownloadTaskStatus Status { get; set; }

            /// <summary>
            /// 获取下载后存放路径。
            /// </summary>
            public string DownloadedPath { get; private set; }

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
            public override string Description => DownloadedPath;

            /// <summary>
            /// 创建下载任务。
            /// </summary>
            /// <param name="downloadPath">下载后存放路径。</param>
            /// <param name="downloadUri">原始下载地址。</param>
            /// <param name="tag">下载任务的标签。</param>
            /// <param name="priority">下载任务的优先级。</param>
            /// <param name="flushSize">将缓冲区写入磁盘的临界大小。</param>
            /// <param name="timeout">下载超时时长，以秒为单位。</param>
            /// <param name="userData">用户自定义数据。</param>
            /// <returns>创建的下载任务。</returns>
            public static DownloadTask Create(string downloadPath, string downloadUri, string tag, int priority, int flushSize, float timeout, object userData)
            {
                var downloadTask = ReferencePool.Acquire<DownloadTask>();
                downloadTask.Initialize(++_serial, tag, priority, userData);
                downloadTask.DownloadedPath = downloadPath;
                downloadTask.DownloadUri    = downloadUri;
                downloadTask.FlushSize      = flushSize;
                downloadTask.Timeout        = timeout;
                return downloadTask;
            }

            /// <summary>
            /// 清理下载任务。
            /// </summary>
            public override void Clear()
            {
                base.Clear();

                FlushSize      = 0;
                Timeout        = 0f;
                DownloadedPath = null;
                DownloadUri    = null;
                Status         = DownloadTaskStatus.Todo;
            }
        }
    }
}