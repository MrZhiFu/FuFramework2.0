using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Web
{
    /// <summary>
    /// 字符串结果 JSON 请求数据（GetToString / PostToString）。
    /// </summary>
    public sealed class WebJsonStringData : WebJsonDataBase
    {
        /// <summary>
        /// 字符串结果的任务完成源。
        /// </summary>
        public readonly UniTaskCompletionSource<WebStringResult> UniTaskCompletionStringSource;

        /// <summary>
        /// 初始化字符串结果的 GET 请求。
        /// </summary>
        /// <param name="url">请求 URL。</param>
        /// <param name="header">请求头信息。</param>
        /// <param name="isGet">是否为 GET 请求。</param>
        /// <param name="source">字符串结果的任务完成源。</param>
        /// <param name="token">调用方取消令牌。</param>
        /// <param name="userData">用户自定义数据。</param>
        public WebJsonStringData(string url, Dictionary<string, string> header, bool isGet, UniTaskCompletionSource<WebStringResult> source, CancellationToken token, object userData = null)
            : base(isGet, url, header, token, userData)
        {
            UniTaskCompletionStringSource = source;
        }

        /// <summary>
        /// 初始化字符串结果的 POST 请求。
        /// </summary>
        /// <param name="url">请求 URL。</param>
        /// <param name="header">请求头信息。</param>
        /// <param name="form">表单数据。</param>
        /// <param name="source">字符串结果的任务完成源。</param>
        /// <param name="token">调用方取消令牌。</param>
        /// <param name="userData">用户自定义数据。</param>
        public WebJsonStringData(string url, Dictionary<string, string> header, Dictionary<string, object> form, UniTaskCompletionSource<WebStringResult> source, CancellationToken token,
                                 object userData = null)
            : base(false, url, header, form, token, userData)
        {
            UniTaskCompletionStringSource = source;
        }

        /// <summary>
        /// 请求成功：提取文本结果并写回任务完成源。
        /// </summary>
        /// <param name="request">已完成的请求。</param>
        public override void Complete(UnityWebRequest request)
        {
            UniTaskCompletionStringSource.TrySetResult(new WebStringResult(UserData, request.downloadHandler.text));
        }

        /// <summary>
        /// 请求取消：取消未完成的任务。
        /// </summary>
        public override void CompleteCanceled() => UniTaskCompletionStringSource.TrySetCanceled();

        /// <summary>
        /// 请求失败：向任务完成源写入异常。
        /// </summary>
        /// <param name="exception">异常。</param>
        public override void CompleteError(Exception exception) => UniTaskCompletionStringSource.TrySetException(exception);

        /// <summary>
        /// 释放资源，取消未完成的任务。
        /// </summary>
        public override void Dispose()
        {
            UniTaskCompletionStringSource?.TrySetCanceled();
            base.Dispose();
        }
    }
}