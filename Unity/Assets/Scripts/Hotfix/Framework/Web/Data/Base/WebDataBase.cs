using System;
using System.Threading;
using UnityEngine.Networking;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Web
{
    /// <summary>
    /// Web 请求数据的基类，包含请求的基本信息与结果写回协议。
    /// 功能：
    ///     1. 承载请求基本信息（IsGet/URL/UserData/Token）。
    ///     2. 定义完成写回协议（Complete/CompleteCanceled/CompleteError），由子类实现写入对应任务完成源。
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

        /// <summary>
        /// 释放资源。基类默认空实现，子类重写为取消未完成的任务（TrySetCanceled）。
        /// </summary>
        public virtual void Dispose() { }
    }
}