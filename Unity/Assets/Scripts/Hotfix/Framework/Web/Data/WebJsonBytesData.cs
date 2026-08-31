using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Web
{
    /// <summary>
    /// 字节数组结果 JSON 请求数据（GetToBytes / PostToBytes）。
    /// </summary>
    public sealed class WebJsonBytesData : WebJsonDataBase
    {
        /// <summary>
        /// 字节数组结果的任务完成源。
        /// </summary>
        public readonly UniTaskCompletionSource<WebBufferResult> UniTaskCompletionBytesSource;

        /// <summary>
        /// 初始化字节数组结果的 GET 请求。
        /// </summary>
        /// <param name="url">请求 URL。</param>
        /// <param name="header">请求头信息。</param>
        /// <param name="isGet">是否为 GET 请求。</param>
        /// <param name="source">字节数组结果的任务完成源。</param>
        /// <param name="token">调用方取消令牌。</param>
        /// <param name="userData">用户自定义数据。</param>
        public WebJsonBytesData(string url, Dictionary<string, string> header, bool isGet, UniTaskCompletionSource<WebBufferResult> source, CancellationToken token, object userData = null)
            : base(isGet, url, header, token, userData)
        {
            UniTaskCompletionBytesSource = source;
        }

        /// <summary>
        /// 初始化字节数组结果的 POST 请求。
        /// </summary>
        /// <param name="url">请求 URL。</param>
        /// <param name="header">请求头信息。</param>
        /// <param name="form">表单数据。</param>
        /// <param name="source">字节数组结果的任务完成源。</param>
        /// <param name="token">调用方取消令牌。</param>
        /// <param name="userData">用户自定义数据。</param>
        public WebJsonBytesData(string url, Dictionary<string, string> header, Dictionary<string, object> form, UniTaskCompletionSource<WebBufferResult> source, CancellationToken token, object userData = null)
            : base(false, url, header, form, token, userData)
        {
            UniTaskCompletionBytesSource = source;
        }

        /// <summary>
        /// 释放资源，取消未完成的任务。
        /// </summary>
        public override void Dispose()
        {
            UniTaskCompletionBytesSource?.TrySetCanceled();
            base.Dispose();
        }
    }
}
