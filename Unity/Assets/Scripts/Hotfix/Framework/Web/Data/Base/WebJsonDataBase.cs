using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.Networking;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Web
{
    /// <summary>
    /// Web JSON 请求数据基类：承载 JSON 请求的公共信息（Header/Form），具体结果类型由子类决定。
    /// </summary>
    public abstract class WebJsonDataBase : WebDataBase
    {
        /// <summary>
        /// 请求头信息。
        /// </summary>
        public Dictionary<string, string> Header { get; }

        /// <summary>
        /// 表单数据。
        /// </summary>
        public Dictionary<string, object> Form { get; }

        /// <summary>
        /// 初始化 JSON 请求数据（无表单）。
        /// </summary>
        /// <param name="isGet">是否为 GET 请求。</param>
        /// <param name="url">请求 URL。</param>
        /// <param name="header">请求头信息。</param>
        /// <param name="token">调用方取消令牌。</param>
        /// <param name="userData">用户自定义数据。</param>
        protected WebJsonDataBase(bool isGet, string url, Dictionary<string, string> header, CancellationToken token, object userData = null)
            : base(isGet, url, token, userData)
        {
            Header = header;
        }

        /// <summary>
        /// 初始化 JSON 请求数据（带表单）。
        /// </summary>
        /// <param name="isGet">是否为 GET 请求。</param>
        /// <param name="url">请求 URL。</param>
        /// <param name="header">请求头信息。</param>
        /// <param name="form">表单数据。</param>
        /// <param name="token">调用方取消令牌。</param>
        /// <param name="userData">用户自定义数据。</param>
        protected WebJsonDataBase(bool isGet, string url, Dictionary<string, string> header, Dictionary<string, object> form, CancellationToken token, object userData = null)
            : base(isGet, url, token, userData)
        {
            Header = header;
            Form   = form;
        }

        /// <summary>
        /// 请求成功：按子类结果类型提取并写回结果。
        /// </summary>
        /// <param name="request">已完成的请求。</param>
        public abstract void Complete(UnityWebRequest request);

        /// <summary>
        /// 请求取消：取消未完成的任务。
        /// </summary>
        public abstract void CompleteCanceled();

        /// <summary>
        /// 请求失败：向任务完成源写入异常。
        /// </summary>
        /// <param name="exception">异常。</param>
        public abstract void CompleteError(Exception exception);
    }
}
