using System;
using System.Threading;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Web
{
    public partial class WebModule
    {
        /// <summary>
        /// Web请求数据的基类，包含请求的基本信息
        /// </summary>
        private class WebData : IDisposable
        {
            /// <summary>
            /// 获取是否为GET请求
            /// </summary>
            public bool IsGet { get; }

            /// <summary>
            /// 获取请求URL
            /// </summary>
            public string URL { get; }

            /// <summary>
            /// 获取用户自定义数据
            /// </summary>
            public object UserData { get; }

            /// <summary>
            /// 调用方取消令牌：请求存续期间观察它；调用方取消（如界面关闭）时请求随之中止。
            /// </summary>
            public CancellationToken Token { get; }

            /// <summary>
            /// 初始化Web请求数据
            /// </summary>
            /// <param name="isGet">是否为GET请求</param>
            /// <param name="url">请求URL</param>
            /// <param name="token">调用方取消令牌。</param>
            /// <param name="userData">用户自定义数据</param>
            protected WebData(bool isGet, string url, CancellationToken token, object userData = null)
            {
                UserData = userData;
                IsGet    = isGet;
                URL      = url;
                Token    = token;
            }

            /// <summary>
            /// 释放资源
            /// </summary>
            public virtual void Dispose() { }
        }
    }
}
