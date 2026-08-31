using System;
using System.Threading;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Web
{
    /// <summary>
    /// Web 请求数据的基类，包含请求的基本信息。
    /// </summary>
    public abstract class WebDataBase : IDisposable
    {
        /// <summary>
        /// 是否为 GET 请求。
        /// </summary>
        public bool IsGet { get; }

        /// <summary>
        /// 请求 URL。
        /// </summary>
        public string URL { get; }

        /// <summary>
        /// 用户自定义数据。
        /// </summary>
        public object UserData { get; }

        /// <summary>
        /// 调用方取消令牌：请求存续期间观察它；调用方取消（如界面关闭）时请求随之中止。
        /// </summary>
        public CancellationToken Token { get; }

        /// <summary>
        /// 初始化 Web 请求数据。
        /// </summary>
        /// <param name="isGet">是否为 GET 请求。</param>
        /// <param name="url">请求 URL。</param>
        /// <param name="token">调用方取消令牌。</param>
        /// <param name="userData">用户自定义数据。</param>
        protected WebDataBase(bool isGet, string url, CancellationToken token, object userData = null)
        {
            UserData = userData;
            IsGet    = isGet;
            URL      = url;
            Token    = token;
        }

        /// <summary>
        /// 释放资源。
        /// </summary>
        public virtual void Dispose() { }
    }
}
