using System;
using System.Collections.Generic;
using System.Threading;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Web
{
    /// <summary>
    /// Web JSON 请求数据基类：承载 JSON 请求的公共信息（Header/Form）。
    /// 完成写回协议继承自 WebDataBase，由子类（字符串/字节数组）实现。
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
    }
}