using System;
using System.Threading;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Web
{
    /// <summary>
    /// Web Pb 请求数据类，用于处理 Protocol Buffer（ProtoBuf）格式的 Web 请求。
    /// </summary>
    public sealed class WebPbData : WebDataBase
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
        /// 初始化 Web Pb 请求数据。
        /// </summary>
        /// <param name="url">请求 URL。</param>
        /// <param name="sendData">要发送的 Protocol Buffer 序列化数据。</param>
        /// <param name="source">请求任务的完成源。</param>
        /// <param name="token">调用方取消令牌。</param>
        /// <param name="userData">用户自定义数据。</param>
        public WebPbData(string url, byte[] sendData, UniTaskCompletionSource<WebBufferResult> source, CancellationToken token, object userData = null)
            : base(false, url, token, userData)
        {
            source.CheckNull(nameof(source));
            SendData         = sendData;
            CompletionSource = source;
        }

        /// <summary>
        /// 请求成功：提取字节数组结果并写回任务完成源。
        /// </summary>
        /// <param name="request">已完成的请求。</param>
        public override void Complete(UnityWebRequest request)
        {
            CompletionSource.TrySetResult(new WebBufferResult(UserData, request.downloadHandler.data));
        }

        /// <summary>
        /// 请求取消：取消未完成的任务。
        /// </summary>
        public override void CompleteCanceled() => CompletionSource.TrySetCanceled();

        /// <summary>
        /// 请求失败：向任务完成源写入异常。
        /// </summary>
        /// <param name="exception">异常。</param>
        public override void CompleteError(Exception exception) => CompletionSource.TrySetException(exception);

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