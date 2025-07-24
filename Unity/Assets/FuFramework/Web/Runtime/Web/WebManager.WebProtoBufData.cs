using System.Threading.Tasks;

namespace GameFrameX.Web.Runtime
{
    public partial class WebManager
    {
        /// <summary>
        /// Web ProtoBuf请求数据类，用于处理Protocol Buffer格式的Web请求
        /// </summary>
        private sealed class WebProtoBufData : WebData
        {
            /// <summary>
            /// 获取请求任务的完成源，用于异步操作的控制和结果返回
            /// </summary>
            public readonly TaskCompletionSource<WebBufferResult> Task;

            /// <summary>
            /// 获取要发送的Protocol Buffer序列化后的字节数组数据
            /// </summary>
            public readonly byte[] SendData;

            /// <summary>
            /// 初始化Web ProtoBuf请求数据
            /// </summary>
            /// <param name="url">请求URL</param>
            /// <param name="sendData">要发送的Protocol Buffer序列化数据</param>
            /// <param name="task">请求任务的完成源</param>
            /// <param name="userData">用户自定义数据</param>
            public WebProtoBufData(string url, byte[] sendData, TaskCompletionSource<WebBufferResult> task, object userData) : base(false, url, userData)
            {
                task.CheckNull(nameof(task));
                SendData = sendData;
                Task = task;
            }
        }
    }
}