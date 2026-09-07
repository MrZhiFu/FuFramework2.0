using System;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Web
{
    /// <summary>
    /// Web 实时请求信息，用于调试面板展示当前等待/发送中的请求。
    /// 功能：
    ///     1. 提供请求状态、协议、方法与 URL 摘要。
    ///     2. 提供入队/发送时间用于计算等待与已发送时长。
    ///     3. 提供调用方是否已取消标记，用于暴露取消链路泄漏。
    ///     4. 持有原始请求数据对象引用，供面板展开查看完整报文。
    /// </summary>
    public readonly struct WebLiveRequestInfo
    {
        /// <summary>
        /// 请求当前状态（等待/发送中）。
        /// </summary>
        public EWebRequestState State { get; }

        /// <summary>
        /// 是否为 Pb 请求（false 为 JSON 请求）。
        /// </summary>
        public bool IsPb { get; }

        /// <summary>
        /// 是否为 GET 请求（false 为 POST 请求）。
        /// </summary>
        public bool IsGet { get; }

        /// <summary>
        /// 请求 URL。
        /// </summary>
        public string Url { get; }

        /// <summary>
        /// 请求入队时刻（UTC）。
        /// </summary>
        public DateTime EnqueueTimeUtc { get; }

        /// <summary>
        /// 请求开始发送时刻（UTC），等待中的请求为默认值。
        /// </summary>
        public DateTime SendTimeUtc { get; }

        /// <summary>
        /// 调用方取消令牌是否已触发（请求已失效但仍滞留，通常为取消链路泄漏）。
        /// </summary>
        public bool CallerCanceled { get; }

        /// <summary>
        /// 原始请求数据对象（WebJsonStringData / WebJsonBytesData / WebPbData）。
        /// </summary>
        public WebDataBase Data { get; }

        /// <summary>
        /// 初始化 Web 实时请求信息的新实例。
        /// </summary>
        /// <param name="state">请求当前状态。</param>
        /// <param name="isPb">是否为 Pb 请求。</param>
        /// <param name="isGet">是否为 GET 请求。</param>
        /// <param name="url">请求 URL。</param>
        /// <param name="enqueueTimeUtc">请求入队时刻（UTC）。</param>
        /// <param name="sendTimeUtc">请求开始发送时刻（UTC）。</param>
        /// <param name="callerCanceled">调用方取消令牌是否已触发。</param>
        /// <param name="data">原始请求数据对象。</param>
        public WebLiveRequestInfo(EWebRequestState state, bool isPb, bool isGet, string url, DateTime enqueueTimeUtc, DateTime sendTimeUtc, bool callerCanceled, WebDataBase data)
        {
            State          = state;
            IsPb           = isPb;
            IsGet          = isGet;
            Url            = url;
            EnqueueTimeUtc = enqueueTimeUtc;
            SendTimeUtc    = sendTimeUtc;
            CallerCanceled = callerCanceled;
            Data           = data;
        }
    }
}