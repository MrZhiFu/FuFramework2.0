using System;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Web
{
    /// <summary>
    /// Web 最近请求记录条目，用于调试面板展示请求的终结结果摘要。
    /// 功能：
    ///     1. 记录请求的完成时刻、结果类型、协议与方法。
    ///     2. 记录等待耗时、总耗时与收发字节数。
    ///     3. 记录失败/取消原因。
    /// </summary>
    public readonly struct WebLogEntry
    {
        /// <summary>
        /// 请求完成时刻（UTC）。
        /// </summary>
        public DateTime CompleteTimeUtc { get; }

        /// <summary>
        /// 请求终结结果。
        /// </summary>
        public EWebRequestResult Result { get; }

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
        /// 请求在队列中的等待耗时（毫秒）。
        /// </summary>
        public int WaitMs { get; }

        /// <summary>
        /// 请求从入队到完成的总体耗时（毫秒）。
        /// </summary>
        public int TotalMs { get; }

        /// <summary>
        /// 发送字节数。
        /// </summary>
        public int SendBytes { get; }

        /// <summary>
        /// 接收字节数。
        /// </summary>
        public int RecvBytes { get; }

        /// <summary>
        /// 失败/取消原因，成功时为空。
        /// </summary>
        public string Error { get; }

        /// <summary>
        /// 请求体文本（JSON POST 序列化文本），供调试面板展开记录时展示/复制。
        /// </summary>
        public string RequestBody { get; }

        /// <summary>
        /// 初始化 Web 最近请求记录条目的新实例。
        /// </summary>
        /// <param name="completeTimeUtc">请求完成时刻（UTC）。</param>
        /// <param name="result">请求终结结果。</param>
        /// <param name="isPb">是否为 Pb 请求。</param>
        /// <param name="isGet">是否为 GET 请求。</param>
        /// <param name="url">请求 URL。</param>
        /// <param name="waitMs">请求在队列中的等待耗时（毫秒）。</param>
        /// <param name="totalMs">请求从入队到完成的总体耗时（毫秒）。</param>
        /// <param name="sendBytes">发送字节数。</param>
        /// <param name="recvBytes">接收字节数。</param>
        /// <param name="error">失败/取消原因。</param>
        /// <param name="requestBody">请求体文本（JSON POST 序列化文本）。</param>
        public WebLogEntry(DateTime completeTimeUtc, EWebRequestResult result, bool isPb, bool isGet, string url, int waitMs, int totalMs, int sendBytes, int recvBytes, string error, string requestBody)
        {
            CompleteTimeUtc = completeTimeUtc;
            Result          = result;
            IsPb            = isPb;
            IsGet           = isGet;
            Url             = url;
            WaitMs          = waitMs;
            TotalMs         = totalMs;
            SendBytes       = sendBytes;
            RecvBytes       = recvBytes;
            Error           = error;
            RequestBody     = requestBody;
        }
    }
}