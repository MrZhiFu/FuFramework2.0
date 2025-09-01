using Cysharp.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace FuFramework.Download.Runtime
{
    public sealed partial class DownloadManager
    {
        /// <summary>
        /// 下载任务信息数据。
        /// 用于包装一个异步的下载任务的信息，包括标签、URL、序列编号、用户自定义数据等。
        /// </summary>
        private sealed class DownloadData
        {
            /// <summary>
            /// 下载任务的标签。
            /// </summary>
            public string Tag      { get; private set; }

            /// <summary>
            /// 下载任务的URL。
            /// </summary>
            public string Url      { get; private set; }

            /// <summary>
            /// 下载任务的序列编号。
            /// </summary>
            public int    SerialId { get; private set; }

            /// <summary>
            /// 用户自定义数据。
            /// </summary>
            public object UserData { get; private set; }

            /// <summary>
            /// 下载任务的任务完成回调包装。
            /// </summary>
            public UniTaskCompletionSource<bool> Tcs { get; private set; }


            /// <summary>
            /// 初始化下载数据的新实例。
            /// </summary>
            public DownloadData(string url, string tag, int serialId, object userData)
            {
                Url      = url;
                Tag      = tag;
                SerialId = serialId;
                UserData = userData;
                Tcs      = new UniTaskCompletionSource<bool>();
            }
        }
    }
}