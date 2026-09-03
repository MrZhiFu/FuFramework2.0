using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Web
{
    /// <summary>
    /// Web 管理模块。
    /// 功能：
    ///     1.实现HTTP GET和POST请求功能。
    /// 对外公共接口见 WebModule.API.cs，JSON 请求处理见 WebModule.Json.cs，Pb 请求处理见 WebModule.Pb.cs。
    /// </summary>
    public partial class WebModule : ModuleBase, ICancelAsync
    {
        /// <summary>
        /// 取消范围：内部 CTS + 在途计数 + 全部完成信号。每次 OnInit 重建（新生命周期 = 新 Token）。
        /// OnDispose 时 Cancel，在途 Web 请求随之取消；框架重启前经 CancelAllAsync 等待取消清理完成。
        /// </summary>
        private CancellationScope m_Scope;

        /// <summary>
        /// 用于构建 URL 的 StringBuilder。
        /// </summary>
        private readonly StringBuilder m_UrlStr = new(256);

        /// <summary>
        /// 等待发送的 JSON 请求队列。
        /// </summary>
        private readonly Queue<WebJsonDataBase> m_WaitingJsonQueue = new(256);

        /// <summary>
        /// 发送中的 JSON 请求列表。
        /// </summary>
        private readonly List<WebJsonDataBase> m_SendingJsonList = new(16);

        /// <summary>
        /// 等待发送的 Pb 请求队列。
        /// </summary>
        private readonly Queue<WebPbData> m_WaitingPbQueue = new(256);

        /// <summary>
        /// 发送中的 Pb 请求列表。
        /// </summary>
        private readonly List<WebPbData> m_SendingPbList = new(16);

        /// <summary>
        /// Pb 内容类型常量。
        /// </summary>
        private const string PbContentType = "application/x-protobuf";

        #region 请求入队

        /// <summary>
        /// GET 字符串请求入队（公共 API 核心）。
        /// </summary>
        /// <param name="url">请求地址。</param>
        /// <param name="queryString">URL 查询参数。</param>
        /// <param name="header">请求头。</param>
        /// <param name="token">调用方取消令牌。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>字符串请求的任务。</returns>
        private UniTask<WebStringResult> GetToStringReq(string url, Dictionary<string, string> queryString, Dictionary<string, string> header,
                                                        CancellationToken token, object userData = null)
        {
            // 模块已销毁或调用方已取消则拒绝新请求；先标准化 URL，避免后续抛异常时 TCS 已建未入队
            m_Scope.Token.ThrowIfCancellationRequested();
            token.ThrowIfCancellationRequested();
            url = NormalizeURL(url, queryString);
            var uniTaskCompletionSource = new UniTaskCompletionSource<WebStringResult>();

            var webJsonData = new WebJsonStringData(url, header, true, uniTaskCompletionSource, token, userData);
            m_WaitingJsonQueue.Enqueue(webJsonData);
            return uniTaskCompletionSource.Task;
        }

        /// <summary>
        /// GET 字节数组请求入队（公共 API 核心）。
        /// </summary>
        /// <param name="url">请求地址。</param>
        /// <param name="queryString">URL 查询参数。</param>
        /// <param name="header">请求头。</param>
        /// <param name="token">调用方取消令牌。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>字节数组请求的任务。</returns>
        private UniTask<WebBufferResult> GetToBytesReq(string url, Dictionary<string, string> queryString, Dictionary<string, string> header, CancellationToken token, object userData = null)
        {
            // 模块已销毁或调用方已取消则拒绝新请求；先标准化 URL，避免后续抛异常时 TCS 已建未入队
            m_Scope.Token.ThrowIfCancellationRequested();
            token.ThrowIfCancellationRequested();
            url = NormalizeURL(url, queryString);
            var uniTaskCompletionSource = new UniTaskCompletionSource<WebBufferResult>();

            var webJsonData = new WebJsonBytesData(url, header, true, uniTaskCompletionSource, token, userData);
            m_WaitingJsonQueue.Enqueue(webJsonData);
            return uniTaskCompletionSource.Task;
        }

        /// <summary>
        /// POST 字符串请求入队（公共 API 核心）。
        /// </summary>
        /// <param name="url">请求地址。</param>
        /// <param name="from">表单请求参数。</param>
        /// <param name="queryString">URL 查询参数。</param>
        /// <param name="header">请求头。</param>
        /// <param name="token">调用方取消令牌。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>字符串请求的任务。</returns>
        private UniTask<WebStringResult> PostToStringReq(string url, Dictionary<string, object> from, Dictionary<string, string> queryString, Dictionary<string, string> header,
                                                         CancellationToken token,
                                                         object userData = null)
        {
            // 模块已销毁或调用方已取消则拒绝新请求；先标准化 URL，避免后续抛异常时 TCS 已建未入队
            m_Scope.Token.ThrowIfCancellationRequested();
            token.ThrowIfCancellationRequested();
            url = NormalizeURL(url, queryString);
            var uniTaskCompletionSource = new UniTaskCompletionSource<WebStringResult>();

            var webJsonData = new WebJsonStringData(url, header, from, uniTaskCompletionSource, token, userData);
            m_WaitingJsonQueue.Enqueue(webJsonData);
            return uniTaskCompletionSource.Task;
        }

        /// <summary>
        /// POST 字节数组请求入队（公共 API 核心）。
        /// </summary>
        /// <param name="url">请求地址。</param>
        /// <param name="from">表单请求参数。</param>
        /// <param name="queryString">URL 查询参数。</param>
        /// <param name="header">请求头。</param>
        /// <param name="token">调用方取消令牌。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>字节数组请求的任务。</returns>
        private UniTask<WebBufferResult> PostToBytesReq(string url, Dictionary<string, object> from, Dictionary<string, string> queryString, Dictionary<string, string> header, CancellationToken token,
                                                        object userData = null)
        {
            // 模块已销毁或调用方已取消则拒绝新请求；先标准化 URL，避免后续抛异常时 TCS 已建未入队
            m_Scope.Token.ThrowIfCancellationRequested();
            token.ThrowIfCancellationRequested();
            url = NormalizeURL(url, queryString);
            var uniTaskCompletionSource = new UniTaskCompletionSource<WebBufferResult>();

            var webJsonData = new WebJsonBytesData(url, header, from, uniTaskCompletionSource, token, userData);
            m_WaitingJsonQueue.Enqueue(webJsonData);
            return uniTaskCompletionSource.Task;
        }

        #endregion

        /// <summary>
        /// 初始化：登记模块实例并重建取消范围（新生命周期 = 新 Token）。
        /// </summary>
        protected internal override void OnInit()
        {
            Instance = this;
            m_Scope  = new CancellationScope();
        }

        /// <summary>
        /// 轮询处理请求队列：每帧填满全部空闲并发槽位。
        /// </summary>
        /// <param name="deltaTime">上一帧经过的时间（秒）。</param>
        /// <param name="unscaledDeltaTime">上一帧经过的不受缩放影响的时间（秒）。</param>
        protected internal override void OnUpdate(float deltaTime, float unscaledDeltaTime)
        {
            // 更新处理 JSON 请求队列
            UpdateJsonReq();

            // 更新处理 Pb 请求队列
            UpdatePbReq();
        }

        /// <summary>
        /// 释放：取消在途请求并清空所有队列。
        /// </summary>
        protected internal override void OnDispose()
        {
            // 随模块销毁取消在途 Web 请求
            m_Scope.Cancel();

            // 清空 JSON / Pb 请求队列与列表，取消未完成任务
            ClearReq(m_WaitingJsonQueue, m_SendingJsonList);
            ClearReq(m_WaitingPbQueue,   m_SendingPbList);

            Instance = null;
        }

        /// <summary>
        /// URL 标准化：拼接查询参数并做 URL 编码。
        /// </summary>
        /// <param name="url">原始 URL。</param>
        /// <param name="queryString">查询参数字典。</param>
        /// <returns>标准化后的 URL。</returns>
        private string NormalizeURL(string url, Dictionary<string, string> queryString)
        {
            m_UrlStr.Clear();
            m_UrlStr.Append(url);

            if (queryString is not { Count: > 0 })
            {
                // 无查询参数：无需再操作 StringBuilder，直接返回原 URL
                return url;
            }

            // 拼接分隔符：URL 已含查询串（? 非末尾）时用 & 续接，已以 ? 结尾时直接拼，否则追加 ?
            if (url.IndexOf('?') < 0)
                m_UrlStr.Append("?");
            else if (!url.EndsWithFast("?"))
                m_UrlStr.Append("&");

            foreach (var kv in queryString)
            {
                // 键值做 URL 编码，防止空格/中文/& 等特殊字符破坏查询串；value 可能为 null，EscapeURL 对 null 行为跨版本不确定，先归一
                m_UrlStr.AppendFormat("{0}={1}&", UnityWebRequest.EscapeURL(kv.Key), UnityWebRequest.EscapeURL(kv.Value ?? string.Empty));
            }

            url = m_UrlStr.ToString(0, m_UrlStr.Length - 1);
            m_UrlStr.Clear();

            return url;
        }

        /// <summary>
        /// 清空请求等待队列与发送列表，逐个取消未完成的任务（释放资源并 TrySetCanceled）。
        /// </summary>
        /// <typeparam name="T">请求数据类型（继承自 WebDataBase）。</typeparam>
        /// <param name="waitingQueue">等待发送的请求队列。</param>
        /// <param name="sendingList">发送中的请求列表。</param>
        private static void ClearReq<T>(Queue<T> waitingQueue, List<T> sendingList) where T : WebDataBase
        {
            while (waitingQueue.Count > 0)
            {
                waitingQueue.Dequeue().Dispose();
            }

            waitingQueue.Clear();

            while (sendingList.Count > 0)
            {
                var webData = sendingList[0];
                sendingList.RemoveAt(0);
                webData.Dispose();
            }

            sendingList.Clear();
        }

        /// <summary>
        /// 登记在途并注册完成回调（发送请求的共享骨架）。
        /// 完成回调统一走 WebDataBase 的完成写回协议（Complete/CompleteCanceled/CompleteError），
        /// 由子类决定结果形态（字符串/字节数组/Pb）。
        /// </summary>
        /// <typeparam name="T">请求数据类型（继承自 WebDataBase）。</typeparam>
        /// <param name="webData">请求数据。</param>
        /// <param name="sendingList">发送中的请求列表（用于完成时释放槽位）。</param>
        /// <param name="unityWebRequest">已发送的请求（未释放前调用）。</param>
        /// <param name="asyncOperation">请求异步操作。</param>
        /// <returns>是否成功登记在途（本方法不失败，恒返回 true）。</returns>
        private bool SendRequest<T>(T webData, List<T> sendingList, UnityWebRequest unityWebRequest,
                                    UnityWebRequestAsyncOperation asyncOperation) where T : WebDataBase
        {
            // 构建 + 发送成功后才登记在途：失败路径（调用方处理）无在途登记，计数不泄漏
            var capturedToken = m_Scope.Token;   // 发起时捕获生命周期 Token：模块销毁/重启（OnDispose Cancel）后旧在途请求据此识别取消，不向旧生命周期调用方抛网络错误
            var inFlight      = m_Scope.Begin(); // 登记在途：CancelAsync 等待本请求清理完毕

            // 任一 token 取消即 Abort 中断传输，避免在途请求等到超时才被回收；完成回调中注销。
            // 模块 scope token 恒可取消；仅在存在可取消 token 时创建链接源，减少无谓分配。
            CancellationTokenSource linkedCts         = null;
            var                     abortRegistration = default(CancellationTokenRegistration);
            if (capturedToken.CanBeCanceled || webData.Token.CanBeCanceled)
            {
                linkedCts = CancellationTokenSource.CreateLinkedTokenSource(capturedToken, webData.Token);
                abortRegistration = linkedCts.Token.Register(() =>
                {
                    try
                    {
                        unityWebRequest.Abort();
                    }
                    catch
                    {
                        // 请求可能已释放，忽略
                    }
                });
            }

            asyncOperation.completed += _ =>
            {
                try
                {
                    // 无论成功/失败/取消，先释放并发槽位：否则调用方取消会永久占用槽位，最终阻塞全部新请求
                    sendingList.Remove(webData);

                    // 模块销毁/重启（旧 scope 于 OnDispose Cancel，capturedToken 已触发）或调用方取消：按取消处理，不再写回结果。
                    // 注：重启时序 DisposeModules（OnDispose→Cancel）恒先于重新初始化，故无需再比对实时 m_Scope.Token。
                    if (capturedToken.IsCancellationRequested || webData.Token.IsCancellationRequested)
                    {
                        webData.CompleteCanceled();
                        return;
                    }

                    if (unityWebRequest.result != UnityWebRequest.Result.Success)
                    {
                        FuLogger.LogInfo($"Web Response: {webData.URL} \n Content: {unityWebRequest.error}");

                        // 超时抛 TimeoutException（保持旧 HttpWebRequest 契约），其余抛通用异常
                        if (IsTimeout(unityWebRequest))
                            webData.CompleteError(new TimeoutException(unityWebRequest.error));
                        else
                            webData.CompleteError(new Exception(unityWebRequest.error));
                        return;
                    }

                    webData.Complete(unityWebRequest);
                }
                finally
                {
                    abortRegistration.Dispose(); // 注销取消回调，释放对请求的引用
                    linkedCts?.Dispose();        // 释放链接 CTS（可能未创建）
                    unityWebRequest.Dispose();   // 无论成败均释放原生资源（UnityWebRequest 官方要求）
                    inFlight.Dispose();          // 结束在途登记，CancelAsync 据此等待清理完毕
                }
            };
            return true;
        }

        /// <summary>
        /// 判断 UnityWebRequest 是否超时。
        /// UnityWebRequest.Result 无 TimedOut 枚举，超时表现为 ConnectionError + error 文本 "Request timeout"，
        /// 故以 error 文本是否包含 "timeout" 判定（大小写不敏感）。
        /// </summary>
        /// <param name="unityWebRequest">请求对象（未释放前调用）。</param>
        /// <returns>是否超时。</returns>
        private static bool IsTimeout(UnityWebRequest unityWebRequest)
        {
            if (unityWebRequest.error == null) return false;

            return unityWebRequest.error.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}