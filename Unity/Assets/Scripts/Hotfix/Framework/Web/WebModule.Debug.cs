using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UtilityAOT = AOT.Framework.Core.Utility.UtilityAOT;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Web
{
    /// <summary>
    /// Web 管理模块的调试埋点与调试查询接口。
    /// 功能：
    ///     1. 在请求入队/发送/完成咽喉点累计计数（发起/成功/失败/超时/取消、收发字节）。
    ///     2. 以环形缓冲保留最近 200 条请求终结记录，供调试面板查看失败/取消明细。
    ///     3. 提供实时请求快照（等待/发送中）与模块级统计快照查询。
    ///     4. 提供急救操作：清空全部等待/发送中请求并取消调用方任务（仅调试使用）。
    /// 说明：
    ///     纯增量埋点，不改动请求发送/取消/并发的既有语义；仅供编辑器调试面板反射读取。
    /// </summary>
    public partial class WebModule
    {
        /// <summary>
        /// 调试环形记录容量。
        /// </summary>
        internal const int DebugLogCapacity = 200;

        /// <summary>
        /// 记录中请求体文本的最大保留长度，超出部分截断。
        /// </summary>
        private const int DebugLogBodyMaxLength = 2000;

        #region 调试字段

        /// <summary>
        /// 累计发起请求总数（入队即计）。
        /// </summary>
        private int m_SubmitCount;

        /// <summary>
        /// 累计发起的 JSON 请求数。
        /// </summary>
        private int m_JsonSubmitCount;

        /// <summary>
        /// 累计发起的 Pb 请求数。
        /// </summary>
        private int m_PbSubmitCount;

        /// <summary>
        /// 累计成功请求数。
        /// </summary>
        private int m_SuccessCount;

        /// <summary>
        /// 累计失败请求数（网络错误 / 构建异常等）。
        /// </summary>
        private int m_FailedCount;

        /// <summary>
        /// 累计超时请求数。
        /// </summary>
        private int m_TimeoutCount;

        /// <summary>
        /// 累计取消请求数（调用方取消 / 模块销毁 / 调试急救取消）。
        /// </summary>
        private int m_CanceledCount;

        /// <summary>
        /// 累计发送字节数。
        /// </summary>
        private long m_SentBytes;

        /// <summary>
        /// 累计接收字节数。
        /// </summary>
        private long m_RecvBytes;

        /// <summary>
        /// 最近请求记录的环形缓冲（数组为容器）。
        /// </summary>
        private readonly WebLogEntry[] m_DebugLogRing = new WebLogEntry[DebugLogCapacity];

        /// <summary>
        /// 环形缓冲中最旧记录的索引。
        /// </summary>
        private int m_DebugLogHead;

        /// <summary>
        /// 环形缓冲中现有记录条数。
        /// </summary>
        private int m_DebugLogCount;

        #endregion

        #region 请求入队统计

        /// <summary>
        /// JSON 请求入队并登记调试计数（调试开关关闭时仅入队，不做打点）。
        /// </summary>
        /// <param name="webJsonData">JSON 请求数据。</param>
        private void EnqueueJsonReq(WebJsonDataBase webJsonData)
        {
            if (DebugRecordingEnabled)
            {
                webJsonData.EnqueueTimeUtc = DateTime.UtcNow;
                m_JsonSubmitCount++;
                m_SubmitCount++;
            }

            m_WaitingJsonQueue.Enqueue(webJsonData);
        }

        /// <summary>
        /// Pb 请求入队并登记调试计数（调试开关关闭时仅入队，不做打点）。
        /// </summary>
        /// <param name="webPbData">Pb 请求数据。</param>
        private void EnqueuePbReq(WebPbData webPbData)
        {
            if (DebugRecordingEnabled)
            {
                webPbData.EnqueueTimeUtc = DateTime.UtcNow;
                m_PbSubmitCount++;
                m_SubmitCount++;
            }

            m_WaitingPbQueue.Enqueue(webPbData);
        }

        #endregion

        #region 请求结束记账

        /// <summary>
        /// 记录构建请求阶段的失败（请求在发送前即因非法 URL/序列化异常终止）。
        /// </summary>
        /// <param name="webData">请求数据。</param>
        /// <param name="exception">构建/发送异常。</param>
        private void RecordBuildFailed(WebDataBase webData, Exception exception)
        {
            RecordResult(EWebRequestResult.Failed, webData, 0, 0, exception?.Message);
        }

        /// <summary>
        /// 按终结结果累计计数，并将摘要写入最近请求记录。
        /// </summary>
        /// <param name="result">请求终结结果。</param>
        /// <param name="webData">请求数据（须已设置入队时间）。</param>
        /// <param name="sendBytes">发送字节数。</param>
        /// <param name="recvBytes">接收字节数。</param>
        /// <param name="error">失败/取消原因，成功时为空。</param>
        private void RecordResult(EWebRequestResult result, WebDataBase webData, int sendBytes, int recvBytes, string error)
        {
            // 调试开关关闭时短路，避免正式运行承担计数与记录开销
            if (!DebugRecordingEnabled) return;

            switch (result)
            {
                case EWebRequestResult.Success:
                    m_SuccessCount++;
                    break;
                case EWebRequestResult.Timeout:
                    m_TimeoutCount++;
                    break;
                case EWebRequestResult.Failed:
                    m_FailedCount++;
                    break;
                case EWebRequestResult.Canceled:
                    m_CanceledCount++;
                    break;
            }

            m_SentBytes += sendBytes;
            m_RecvBytes += recvBytes;

            var nowUtc  = DateTime.UtcNow;
            var totalMs = webData.EnqueueTimeUtc == default ? 0 : Math.Max(0, (int)(nowUtc              - webData.EnqueueTimeUtc).TotalMilliseconds);
            var waitMs  = webData.SendTimeUtc    == default ? 0 : Math.Max(0, (int)(webData.SendTimeUtc - webData.EnqueueTimeUtc).TotalMilliseconds);

            // 请求体预览（JSON POST / Pb 类 JSON 文本）；过长则截断以控制环形记录内存
            var requestBody = webData.DebugRequestBody;
            if (!string.IsNullOrEmpty(requestBody) && requestBody.Length > DebugLogBodyMaxLength)
            {
                requestBody = requestBody.Substring(0, DebugLogBodyMaxLength) + "\n...（已截断）";
            }

            PushLog(new WebLogEntry(nowUtc, result, webData is WebPbData, webData.IsGet, webData.URL, waitMs, totalMs, sendBytes, recvBytes, error, requestBody));
        }

        /// <summary>
        /// 将记录写入环形缓冲（满时覆盖最旧记录）。
        /// </summary>
        /// <param name="entry">记录条目。</param>
        private void PushLog(WebLogEntry entry)
        {
            if (m_DebugLogCount == DebugLogCapacity)
            {
                m_DebugLogRing[m_DebugLogHead] = entry;
                m_DebugLogHead                 = (m_DebugLogHead + 1) % DebugLogCapacity;
            }
            else
            {
                var index = (m_DebugLogHead + m_DebugLogCount) % DebugLogCapacity;
                m_DebugLogRing[index] = entry;
                m_DebugLogCount++;
            }
        }

        #endregion

        #region 调试查询接口

        /// <summary>
        /// 调试记录开关（调试专用）。默认关闭；调试面板打开时置为 true、关闭时置回 false，以收敛运行时开销。
        /// </summary>
        public bool DebugRecordingEnabled { get; private set; }

        /// <summary>
        /// 设置调试记录开关（调试专用）。开启瞬间会清空既有历史，仅在此前为关闭状态时清空（可重复调用）。
        /// </summary>
        /// <param name="enabled">是否开启。</param>
        public void SetDebugRecording(bool enabled)
        {
            var wasEnabled = DebugRecordingEnabled;
            DebugRecordingEnabled = enabled;
            if (enabled && !wasEnabled) ClearDebugHistory();
        }

        /// <summary>
        /// 获取模块级调试统计与当前队列状态快照（调试专用）。
        /// </summary>
        /// <returns>模块调试信息快照。</returns>
        public WebModuleDebugInfo GetDebugSnapshot()
        {
            return new WebModuleDebugInfo(m_SubmitCount, m_JsonSubmitCount, m_PbSubmitCount, m_SuccessCount, m_FailedCount, m_TimeoutCount, m_CanceledCount,
                                          m_SentBytes, m_RecvBytes, m_WaitingJsonQueue.Count, m_SendingJsonList.Count, m_WaitingPbQueue.Count, m_SendingPbList.Count);
        }

        /// <summary>
        /// 获取当前所有等待/发送中请求的快照（调试专用）。按 JSON 等待、JSON 发送、Pb 等待、Pb 发送分组顺序返回。
        /// </summary>
        /// <returns>实时请求信息数组。</returns>
        public WebLiveRequestInfo[] GetCurrentRequests()
        {
            var totalCount = m_WaitingJsonQueue.Count + m_SendingJsonList.Count + m_WaitingPbQueue.Count + m_SendingPbList.Count;
            var infos      = new WebLiveRequestInfo[totalCount];
            var index      = 0;

            AppendLiveJsonInfos(infos, ref index, m_WaitingJsonQueue, EWebRequestState.Waiting);
            AppendLiveJsonInfos(infos, ref index, m_SendingJsonList,  EWebRequestState.Sending);
            AppendLivePbInfos(infos, ref index, m_WaitingPbQueue, EWebRequestState.Waiting);
            AppendLivePbInfos(infos, ref index, m_SendingPbList,  EWebRequestState.Sending);

            return infos;
        }

        /// <summary>
        /// 获取最近请求记录（调试专用）。返回数组按时间从新到旧排列，最多 DebugLogCapacity 条。
        /// </summary>
        /// <returns>最近请求记录数组。</returns>
        public WebLogEntry[] GetRecentLogs()
        {
            var logs = new WebLogEntry[m_DebugLogCount];
            for (var i = 0; i < m_DebugLogCount; i++)
            {
                // 环形索引递增方向为从旧到新，倒序填充使返回数组从新到旧
                var ringIndex = (m_DebugLogHead + i) % DebugLogCapacity;
                logs[m_DebugLogCount - 1 - i] = m_DebugLogRing[ringIndex];
            }

            return logs;
        }

        /// <summary>
        /// 清空调试统计与最近请求记录（调试专用）。
        /// </summary>
        public void ClearDebugHistory()
        {
            m_SubmitCount     = 0;
            m_JsonSubmitCount = 0;
            m_PbSubmitCount   = 0;
            m_SuccessCount    = 0;
            m_FailedCount     = 0;
            m_TimeoutCount    = 0;
            m_CanceledCount   = 0;
            m_SentBytes       = 0;
            m_RecvBytes       = 0;
            m_DebugLogHead    = 0;
            m_DebugLogCount   = 0;
        }

        /// <summary>
        /// 急救操作：清空全部等待/发送中的请求并取消调用方任务，释放全部并发槽位（调试专用）。
        /// 发送中的请求会标记调试取消，其底层传输不再记账，随自然超时回收。
        /// </summary>
        public void CancelAllPendingForDebug()
        {
            while (m_WaitingJsonQueue.Count > 0)
            {
                CancelRequestForDebug(m_WaitingJsonQueue.Dequeue(), "等待中被调试面板取消");
            }

            while (m_WaitingPbQueue.Count > 0)
            {
                CancelRequestForDebug(m_WaitingPbQueue.Dequeue(), "等待中被调试面板取消");
            }

            while (m_SendingJsonList.Count > 0)
            {
                var webData = m_SendingJsonList[0];
                m_SendingJsonList.RemoveAt(0);
                CancelRequestForDebug(webData, "发送中被调试面板取消");
            }

            while (m_SendingPbList.Count > 0)
            {
                var webData = m_SendingPbList[0];
                m_SendingPbList.RemoveAt(0);
                CancelRequestForDebug(webData, "发送中被调试面板取消");
            }
        }

        /// <summary>
        /// 急救取消单个请求：标记调试取消并释放调用方任务，登记取消记录。
        /// </summary>
        /// <param name="webData">请求数据。</param>
        /// <param name="reason">取消原因。</param>
        private void CancelRequestForDebug(WebDataBase webData, string reason)
        {
            // 发送中请求标记后在途完成回调不再重复记账；等待中请求无完成回调，标记无害
            webData.IsDebugCanceled = true;
            webData.Dispose();
            RecordResult(EWebRequestResult.Canceled, webData, 0, 0, reason);
        }

        #endregion

        #region 手动测试请求

        /// <summary>
        /// 手动测试请求是否正在运行（调试专用）。
        /// </summary>
        public bool DebugManualRunning { get; private set; }

        /// <summary>
        /// 手动测试请求的状态文本：空闲 / 请求中 / 成功 / 失败 / 已取消 / 参数错误（调试专用）。
        /// </summary>
        public string DebugManualStatus { get; private set; } = "空闲";

        /// <summary>
        /// 手动测试请求的结果内容（响应文本或错误信息，调试专用）。
        /// </summary>
        public string DebugManualMessage { get; private set; } = "";

        /// <summary>
        /// 发送手动测试请求（调试专用）。GET 直接请求；POST 将请求体按 JSON 解析后发送。
        /// 请求会走模块既有入队/发送管道，因此自动进入最近请求记录。
        /// </summary>
        /// <param name="url">请求 URL。</param>
        /// <param name="isPost">是否为 POST（false 为 GET）。</param>
        /// <param name="postBody">POST 请求体（JSON 文本），仅 isPost 为 true 时生效。</param>
        public void SendDebugRequest(string url, bool isPost, string postBody)
        {
            if (DebugManualRunning) return;

            if (string.IsNullOrWhiteSpace(url))
            {
                DebugManualStatus  = "参数错误";
                DebugManualMessage = "URL 不能为空";
                return;
            }

            SendDebugRequestAsync(url, isPost, postBody).Forget();
        }

        /// <summary>
        /// 手动测试请求异步执行体。
        /// </summary>
        /// <param name="url">请求 URL。</param>
        /// <param name="isPost">是否为 POST。</param>
        /// <param name="postBody">POST 请求体（JSON 文本）。</param>
        private async UniTaskVoid SendDebugRequestAsync(string url, bool isPost, string postBody)
        {
            DebugManualRunning = true;
            DebugManualStatus  = "请求中";
            DebugManualMessage = "";

            try
            {
                string responseText;
                if (isPost)
                {
                    Dictionary<string, object> form = null;
                    if (!string.IsNullOrWhiteSpace(postBody))
                    {
                        try
                        {
                            form = UtilityAOT.Json.ToObject<Dictionary<string, object>>(postBody);
                        }
                        catch (Exception)
                        {
                            DebugManualStatus  = "参数错误";
                            DebugManualMessage = "POST 请求体不是合法的 JSON 对象";
                            return;
                        }
                    }

                    var result = await PostToString(url, form, m_Scope.Token);
                    responseText = result?.Result;
                }
                else
                {
                    var result = await GetToString(url, m_Scope.Token);
                    responseText = result?.Result;
                }

                DebugManualStatus  = "成功";
                DebugManualMessage = TruncateManualResult(responseText);
            }
            catch (OperationCanceledException)
            {
                DebugManualStatus  = "已取消";
                DebugManualMessage = "";
            }
            catch (Exception e)
            {
                DebugManualStatus  = "失败";
                DebugManualMessage = e.Message;
            }
            finally
            {
                DebugManualRunning = false;
            }
        }

        /// <summary>
        /// 截断手动测试请求结果文本。
        /// </summary>
        /// <param name="text">原始结果文本。</param>
        /// <returns>截断后的文本。</returns>
        private static string TruncateManualResult(string text)
        {
            if (string.IsNullOrEmpty(text)) return "(空响应)";
            return text.Length <= 2000 ? text : text.Substring(0, 2000) + $"\n...（已截断，共 {text.Length} 字符）";
        }

        /// <summary>
        /// 清空手动测试请求的状态与结果（调试专用）。
        /// </summary>
        public void ClearDebugManualResult()
        {
            DebugManualStatus  = "空闲";
            DebugManualMessage = "";
        }

        #endregion

        #region 实时快照辅助

        /// <summary>
        /// 将 JSON 请求容器中的请求填入实时快照数组。
        /// </summary>
        /// <param name="infos">实时快照数组。</param>
        /// <param name="index">当前写入索引（引用）。</param>
        /// <param name="container">JSON 请求容器。</param>
        /// <param name="state">请求状态。</param>
        private static void AppendLiveJsonInfos(WebLiveRequestInfo[] infos, ref int index, IEnumerable<WebJsonDataBase> container, EWebRequestState state)
        {
            foreach (var data in container)
            {
                infos[index++] = new WebLiveRequestInfo(state, false, data.IsGet, data.URL, data.EnqueueTimeUtc, data.SendTimeUtc, data.Token.IsCancellationRequested, data);
            }
        }

        /// <summary>
        /// 将 Pb 请求容器中的请求填入实时快照数组。
        /// </summary>
        /// <param name="infos">实时快照数组。</param>
        /// <param name="index">当前写入索引（引用）。</param>
        /// <param name="container">Pb 请求容器。</param>
        /// <param name="state">请求状态。</param>
        private static void AppendLivePbInfos(WebLiveRequestInfo[] infos, ref int index, IEnumerable<WebPbData> container, EWebRequestState state)
        {
            foreach (var data in container)
            {
                infos[index++] = new WebLiveRequestInfo(state, true, data.IsGet, data.URL, data.EnqueueTimeUtc, data.SendTimeUtc, data.Token.IsCancellationRequested, data);
            }
        }

        #endregion
    }
}