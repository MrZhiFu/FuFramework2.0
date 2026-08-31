using System.Threading;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Web
{
    /// <summary>
    /// Web ProtoBuf 请求数据类，用于处理 Protocol Buffer 格式的 Web 请求。
    /// </summary>
    public sealed class WebProtoBufData : WebDataBase
    {
        /// <summary>
        /// 请求任务的完成源，用于异步操作的控制和结果返回。
        /// </summary>
        public readonly UniTaskCompletionSource<WebBufferResult> CompletionSource;

        /// <summary>
        /// 要发送的 Protocol Buffer 序列化后的字节数组数据。
        /// </summary>
        public readonly byte[] SendData;

        /// <summary>
        /// 初始化 Web ProtoBuf 请求数据。
        /// </summary>
        /// <param name="url">请求 URL。</param>
        /// <param name="sendData">要发送的 Protocol Buffer 序列化数据。</param>
        /// <param name="task">请求任务的完成源。</param>
        /// <param name="token">调用方取消令牌。</param>
        /// <param name="userData">用户自定义数据。</param>
        public WebProtoBufData(string url, byte[] sendData, UniTaskCompletionSource<WebBufferResult> task, CancellationToken token, object userData) : base(false, url, token, userData)
        {
            task.CheckNull(nameof(task));
            SendData = sendData;
            CompletionSource = task;
        }

        /// <summary>
        /// 释放资源，取消未完成的任务。
        /// </summary>
        public override void Dispose()
        {
            CompletionSource?.TrySetCanceled();
            base.Dispose();
        }
    }
}
