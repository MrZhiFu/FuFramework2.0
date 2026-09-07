// ReSharper disable once CheckNamespace

namespace Hotfix.Framework.Web
{
    /// <summary>
    /// Web 模块调试信息快照，用于调试面板展示模块级统计与当前队列状态。
    /// 功能：
    ///     1. 提供累计计数（发起/成功/失败/超时/取消）与收发字节。
    ///     2. 提供当前等待/发送中的请求数量，用于计算并发占用率。
    /// </summary>
    public readonly struct WebModuleDebugInfo
    {
        /// <summary>
        /// 累计发起请求总数（入队即计）。
        /// </summary>
        public int SubmitCount { get; }

        /// <summary>
        /// 累计发起的 JSON 请求数。
        /// </summary>
        public int JsonSubmitCount { get; }

        /// <summary>
        /// 累计发起的 Pb 请求数。
        /// </summary>
        public int PbSubmitCount { get; }

        /// <summary>
        /// 累计成功请求数。
        /// </summary>
        public int SuccessCount { get; }

        /// <summary>
        /// 累计失败请求数（网络错误 / 构建异常等）。
        /// </summary>
        public int FailedCount { get; }

        /// <summary>
        /// 累计超时请求数。
        /// </summary>
        public int TimeoutCount { get; }

        /// <summary>
        /// 累计取消请求数。
        /// </summary>
        public int CanceledCount { get; }

        /// <summary>
        /// 累计发送字节数。
        /// </summary>
        public long SentBytes { get; }

        /// <summary>
        /// 累计接收字节数。
        /// </summary>
        public long RecvBytes { get; }

        /// <summary>
        /// 当前等待发送的 JSON 请求数。
        /// </summary>
        public int WaitingJsonCount { get; }

        /// <summary>
        /// 当前发送中的 JSON 请求数。
        /// </summary>
        public int SendingJsonCount { get; }

        /// <summary>
        /// 当前等待发送的 Pb 请求数。
        /// </summary>
        public int WaitingPbCount { get; }

        /// <summary>
        /// 当前发送中的 Pb 请求数。
        /// </summary>
        public int SendingPbCount { get; }

        /// <summary>
        /// 当前所有等待发送的请求总数。
        /// </summary>
        public int WaitingCount => WaitingJsonCount + WaitingPbCount;

        /// <summary>
        /// 当前所有发送中的请求总数。
        /// </summary>
        public int SendingCount => SendingJsonCount + SendingPbCount;

        /// <summary>
        /// 初始化 Web 模块调试信息快照的新实例。
        /// </summary>
        /// <param name="submitCount">累计发起请求总数。</param>
        /// <param name="jsonSubmitCount">累计发起的 JSON 请求数。</param>
        /// <param name="pbSubmitCount">累计发起的 Pb 请求数。</param>
        /// <param name="successCount">累计成功请求数。</param>
        /// <param name="failedCount">累计失败请求数。</param>
        /// <param name="timeoutCount">累计超时请求数。</param>
        /// <param name="canceledCount">累计取消请求数。</param>
        /// <param name="sentBytes">累计发送字节数。</param>
        /// <param name="recvBytes">累计接收字节数。</param>
        /// <param name="waitingJsonCount">当前等待发送的 JSON 请求数。</param>
        /// <param name="sendingJsonCount">当前发送中的 JSON 请求数。</param>
        /// <param name="waitingPbCount">当前等待发送的 Pb 请求数。</param>
        /// <param name="sendingPbCount">当前发送中的 Pb 请求数。</param>
        public WebModuleDebugInfo(int submitCount, int jsonSubmitCount, int pbSubmitCount, int successCount, int failedCount, int timeoutCount, int canceledCount,
                                  long sentBytes, long recvBytes, int waitingJsonCount, int sendingJsonCount, int waitingPbCount, int sendingPbCount)
        {
            SubmitCount      = submitCount;
            JsonSubmitCount  = jsonSubmitCount;
            PbSubmitCount    = pbSubmitCount;
            SuccessCount     = successCount;
            FailedCount      = failedCount;
            TimeoutCount     = timeoutCount;
            CanceledCount    = canceledCount;
            SentBytes        = sentBytes;
            RecvBytes        = recvBytes;
            WaitingJsonCount = waitingJsonCount;
            SendingJsonCount = sendingJsonCount;
            WaitingPbCount   = waitingPbCount;
            SendingPbCount   = sendingPbCount;
        }
    }
}