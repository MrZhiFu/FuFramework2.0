using System;
using System.Text;
using System.Threading;
using ProtoBuf;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;
using System.Collections.Generic;
using Hotfix.Framework.Network;
using UtilityAOT = AOT.Framework.Core.Utility.UtilityAOT;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Web
{
    /// <summary>
    /// Web 管理模块。
    /// 功能：
    ///     1.实现HTTP GET和POST请求功能。
    /// 对外公共接口见 WebModule.API.cs。
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
        /// 等待处理的 Json 格式请求队列。
        /// </summary>
        private readonly Queue<WebJsonDataBase> m_WaitingJsonQueue = new(256);

        /// <summary>
        /// 正在发送的 Json 格式请求列表。
        /// </summary>
        private readonly List<WebJsonDataBase> m_SendingJsonList = new(16);

        /// <summary>
        /// 等待处理的 Pb 请求队列。
        /// </summary>
        private readonly Queue<WebPbData> m_WaitingPbQueue = new(256);

        /// <summary>
        /// 正在处理的 Pb 请求列表。
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
        private UniTask<WebStringResult> GetToStringReq(string url, Dictionary<string, string> queryString, Dictionary<string, string> header,
                                                     CancellationToken token, object userData = null)
        {
            // 模块已销毁或调用方已取消则拒绝新请求
            m_Scope.Token.ThrowIfCancellationRequested();
            token.ThrowIfCancellationRequested();
            var uniTaskCompletionSource = new UniTaskCompletionSource<WebStringResult>();
            url = UrlHandler(url, queryString);

            var webJsonData = new WebJsonStringData(url, header, true, uniTaskCompletionSource, token, userData);
            m_WaitingJsonQueue.Enqueue(webJsonData);
            return uniTaskCompletionSource.Task;
        }

        /// <summary>
        /// GET 字节数组请求入队（公共 API 核心）。
        /// </summary>
        private UniTask<WebBufferResult> GetToBytesReq(string url, Dictionary<string, string> queryString, Dictionary<string, string> header, CancellationToken token, object userData = null)
        {
            // 模块已销毁或调用方已取消则拒绝新请求
            m_Scope.Token.ThrowIfCancellationRequested();
            token.ThrowIfCancellationRequested();
            var uniTaskCompletionSource = new UniTaskCompletionSource<WebBufferResult>();
            url = UrlHandler(url, queryString);

            var webJsonData = new WebJsonBytesData(url, header, true, uniTaskCompletionSource, token, userData);
            m_WaitingJsonQueue.Enqueue(webJsonData);
            return uniTaskCompletionSource.Task;
        }

        /// <summary>
        /// POST 字符串请求入队（公共 API 核心）。
        /// </summary>
        private UniTask<WebStringResult> PostToStringReq(string url, Dictionary<string, object> from, Dictionary<string, string> queryString, Dictionary<string, string> header, CancellationToken token,
                                                      object userData = null)
        {
            // 模块已销毁或调用方已取消则拒绝新请求
            m_Scope.Token.ThrowIfCancellationRequested();
            token.ThrowIfCancellationRequested();
            var uniTaskCompletionSource = new UniTaskCompletionSource<WebStringResult>();
            url = UrlHandler(url, queryString);

            var webJsonData = new WebJsonStringData(url, header, from, uniTaskCompletionSource, token, userData);
            m_WaitingJsonQueue.Enqueue(webJsonData);
            return uniTaskCompletionSource.Task;
        }

        /// <summary>
        /// POST 字节数组请求入队（公共 API 核心）。
        /// </summary>
        private UniTask<WebBufferResult> PostToBytesReq(string url, Dictionary<string, object> from, Dictionary<string, string> queryString, Dictionary<string, string> header, CancellationToken token,
                                                     object userData = null)
        {
            // 模块已销毁或调用方已取消则拒绝新请求
            m_Scope.Token.ThrowIfCancellationRequested();
            token.ThrowIfCancellationRequested();
            var uniTaskCompletionSource = new UniTaskCompletionSource<WebBufferResult>();
            url = UrlHandler(url, queryString);

            var webJsonData = new WebJsonBytesData(url, header, from, uniTaskCompletionSource, token, userData);
            m_WaitingJsonQueue.Enqueue(webJsonData);
            return uniTaskCompletionSource.Task;
        }

        #endregion

        /// <summary>
        /// 初始化。
        /// </summary>
        protected internal override void OnInit()
        {
            Instance = this;
            m_Scope  = new CancellationScope();
        }

        /// <summary>
        /// 轮询处理请求队列。
        /// </summary>
        protected internal override void OnUpdate(float deltaTime, float unscaledDeltaTime)
        {
            // 主线程模型（ModuleManager 驱动 + UWR 完成回调同在主线程）：队列/列表访问无需加锁
            // 每帧填满全部空闲并发槽位（而非每帧仅发一个），提升吞吐
            while (m_SendingJsonList.Count < MaxConnectionPerServer && m_WaitingJsonQueue.Count > 0)
            {
                var webJsonData = m_WaitingJsonQueue.Dequeue();
                // 构造失败（非法 URL/Header 等）已回写异常并返回 false，不入发送列表
                if (SendJsonReq(webJsonData))
                    m_SendingJsonList.Add(webJsonData);
            }

            // 更新处理 Pb 请求队列
            UpdatePbReq();
        }

        /// <summary>
        /// 释放。
        /// </summary>
        protected internal override void OnDispose()
        {
            // 随模块销毁取消在途 Web 请求
            m_Scope.Cancel();

            // 清空 JSON / Pb 请求队列与列表，取消未完成任务
            ClearJsonReq();
            ClearPbReq();

            Instance = null;
        }

        /// <summary>
        /// 清空 JSON 请求队列与列表，取消未完成的任务。
        /// </summary>
        private void ClearJsonReq()
        {
            while (m_WaitingJsonQueue.Count > 0)
            {
                var webData = m_WaitingJsonQueue.Dequeue();
                webData.Dispose();
            }

            m_WaitingJsonQueue.Clear();

            while (m_SendingJsonList.Count > 0)
            {
                var webData = m_SendingJsonList[0];
                m_SendingJsonList.RemoveAt(0);
                webData.Dispose();
            }

            m_SendingJsonList.Clear();
        }

        #region JSON 请求处理

        /// <summary>
        /// 构建并发送 JSON 请求（GET/POST），完成回调按子类结果类型（字符串或字节数组）写回。
        /// </summary>
        /// <param name="webJsonData">JSON 请求数据（WebJsonStringData 或 WebJsonBytesData）。</param>
        /// <returns>是否成功发起并登记在途；构造失败返回 false（已回写异常，调用方不会挂起）。</returns>
        private bool SendJsonReq(WebJsonDataBase webJsonData)
        {
            FuLogger.LogInfo($"Web Request: {webJsonData.URL} \n Header: {UtilityAOT.Json.ToJson(webJsonData.Header)} \n  Form: {UtilityAOT.Json.ToJson(webJsonData.Form)}");

            var capturedToken = m_Scope.Token; // 发起时捕获生命周期 Token：模块销毁/重启（OnDispose Cancel）后旧在途请求据此识别取消，不向旧生命周期调用方抛网络错误
            var sendingList   = m_SendingJsonList; // 捕获发送列表引用，完成回调不捕获 this（模块实例）
            var inFlight      = m_Scope.Begin(); // 登记在途：CancelAsync 等待本请求清理完毕

            UnityWebRequest unityWebRequest = null;
            CancellationTokenSource linkedCts = null;
            var abortRegistration = default(CancellationTokenRegistration);
            try
            {
                // POST 手动构造（避免 PostWwwForm 先生成空 WWWForm 再被替换的重复分配）
                unityWebRequest = webJsonData.IsGet
                    ? UnityWebRequest.Get(webJsonData.URL)
                    : new UnityWebRequest(webJsonData.URL, UnityWebRequest.kHttpVerbPOST)
                    {
                        downloadHandler = new DownloadHandlerBuffer(),
                    };

                unityWebRequest.timeout = (int)ReqTimeout.TotalSeconds;
                if (webJsonData.Form is { Count: > 0 })
                {
                    unityWebRequest.SetRequestHeader("Content-Type", "application/json");
                    var body     = UtilityAOT.Json.ToJson(webJsonData.Form);
                    var postData = Encoding.UTF8.GetBytes(body);
                    unityWebRequest.uploadHandler = new UploadHandlerRaw(postData);
                }
                else if (!webJsonData.IsGet)
                {
                    // 空表单 POST：保留 form-urlencoded 内容类型契约（与旧 PostWwwForm 一致），body 为空
                    unityWebRequest.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
                }

                if (webJsonData.Header is { Count: > 0 })
                {
                    foreach (var kv in webJsonData.Header)
                    {
                        unityWebRequest.SetRequestHeader(kv.Key, kv.Value);
                    }
                }

                var asyncOperation = unityWebRequest.SendWebRequest();

                // 任一 token 取消即 Abort 中断传输，避免在途请求等到超时才被回收；完成回调中注销。
                // 模块 scope token 恒可取消；仅在存在可取消 token 时创建链接源，减少无谓分配。
                // Abort 防御包裹：请求可能已由回调释放（Abort 与 completed 同在主线程，实际不并发，纯防御）。
                if (capturedToken.CanBeCanceled || webJsonData.Token.CanBeCanceled)
                {
                    linkedCts         = CancellationTokenSource.CreateLinkedTokenSource(capturedToken, webJsonData.Token);
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
                        sendingList.Remove(webJsonData);

                        // 模块销毁/重启（旧 scope 于 OnDispose Cancel，capturedToken 已触发）或调用方取消：按取消处理，不再写回结果。
                        // 注：重启时序 DisposeModules（OnDispose→Cancel）恒先于重新初始化，故无需再比对实时 m_Scope.Token。
                        if (capturedToken.IsCancellationRequested || webJsonData.Token.IsCancellationRequested)
                        {
                            webJsonData.CompleteCanceled();
                            return;
                        }

                        if (unityWebRequest.result != UnityWebRequest.Result.Success)
                        {
                            FuLogger.LogInfo($"Web Response: {webJsonData.URL} \n Header: {UtilityAOT.Json.ToJson(webJsonData.Header)} \n  Form: {UtilityAOT.Json.ToJson(webJsonData.Form)} \n Content: {unityWebRequest.error}");

                            // 超时抛 TimeoutException（保持旧 HttpWebRequest 契约），其余抛通用异常
                            if (IsTimeout(unityWebRequest))
                                webJsonData.CompleteError(new TimeoutException(unityWebRequest.error));
                            else
                                webJsonData.CompleteError(new Exception(unityWebRequest.error));
                            return;
                        }

                        FuLogger.LogInfo($"Web Response: {webJsonData.URL} \n Header: {UtilityAOT.Json.ToJson(webJsonData.Header)} \n  Form: {UtilityAOT.Json.ToJson(webJsonData.Form)}");
                        webJsonData.Complete(unityWebRequest);
                    }
                    finally
                    {
                        abortRegistration.Dispose();  // 注销取消回调，释放对请求的引用
                        linkedCts?.Dispose();         // 释放链接 CTS（可能未创建）
                        unityWebRequest.Dispose();     // 无论成败均释放原生资源（UnityWebRequest 官方要求）
                        inFlight.Dispose();            // 结束在途登记，CancelAsync 据此等待清理完毕
                    }
                };
                return true;
            }
            catch (Exception e)
            {
                // 构造/发送阶段异常：释放资源、结束在途登记、回写异常，避免调用方永久挂起
                abortRegistration.Dispose();
                linkedCts?.Dispose();
                unityWebRequest?.Dispose();
                inFlight.Dispose();
                webJsonData.CompleteError(e);
                return false;
            }
        }

        #endregion

        #region Pb 请求处理

        /// <summary>
        /// 更新处理 Pb 请求队列。
        /// </summary>
        private void UpdatePbReq()
        {
            // 每帧填满全部空闲并发槽位（而非每帧仅发一个），提升吞吐
            while (m_SendingPbList.Count < MaxConnectionPerServer && m_WaitingPbQueue.Count > 0)
            {
                var webPbData = m_WaitingPbQueue.Dequeue();
                // 构造失败（非法 URL/Header 等）已回写异常并返回 false，不入发送列表
                if (SendPbReq(webPbData))
                    m_SendingPbList.Add(webPbData);
            }
        }

        /// <summary>
        /// 清空 Pb 请求队列与列表，取消未完成的任务。
        /// </summary>
        private void ClearPbReq()
        {
            while (m_WaitingPbQueue.Count > 0)
            {
                var webData = m_WaitingPbQueue.Dequeue();
                webData.Dispose();
            }

            m_WaitingPbQueue.Clear();

            while (m_SendingPbList.Count > 0)
            {
                var webData = m_SendingPbList[0];
                m_SendingPbList.RemoveAt(0);
                webData.Dispose();
            }

            m_SendingPbList.Clear();
        }

        /// <summary>
        /// 构建并发送 Pb 请求，完成回调写回字节结果。
        /// </summary>
        /// <param name="webData">Pb 请求数据。</param>
        /// <returns>是否成功发起并登记在途；构造失败返回 false（已回写异常，调用方不会挂起）。</returns>
        private bool SendPbReq(WebPbData webData)
        {
            var capturedToken = m_Scope.Token; // 发起时捕获生命周期 Token：模块销毁/重启（OnDispose Cancel）后旧在途请求据此识别取消，不向旧生命周期调用方抛网络错误
            var sendingList   = m_SendingPbList; // 捕获发送列表引用，完成回调不捕获 this（模块实例）
            var inFlight      = m_Scope.Begin(); // 登记在途：CancelAsync 等待本请求清理完毕

            UnityWebRequest unityWebRequest = null;
            CancellationTokenSource linkedCts = null;
            var abortRegistration = default(CancellationTokenRegistration);
            try
            {
                // Pb 请求一律 POST；手动构造避免 PostWwwForm 先生成空 WWWForm 的重复分配
                unityWebRequest = new UnityWebRequest(webData.URL, UnityWebRequest.kHttpVerbPOST)
                {
                    downloadHandler = new DownloadHandlerBuffer(),
                };

                unityWebRequest.timeout = (int)ReqTimeout.TotalSeconds;
                unityWebRequest.SetRequestHeader("Content-Type", PbContentType);
                unityWebRequest.uploadHandler = new UploadHandlerRaw(webData.SendData);

                var asyncOperation = unityWebRequest.SendWebRequest();

                // 任一 token 取消即 Abort 中断传输，避免在途请求等到超时才被回收；完成回调中注销。
                // 模块 scope token 恒可取消；仅在存在可取消 token 时创建链接源，减少无谓分配。
                // Abort 防御包裹：请求可能已由回调释放（Abort 与 completed 同在主线程，实际不并发，纯防御）。
                if (capturedToken.CanBeCanceled || webData.Token.CanBeCanceled)
                {
                    linkedCts         = CancellationTokenSource.CreateLinkedTokenSource(capturedToken, webData.Token);
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
                            webData.CompletionSource.TrySetCanceled();
                            return;
                        }

                        if (unityWebRequest.result != UnityWebRequest.Result.Success)
                        {
                            // 超时抛 TimeoutException（保持旧 HttpWebRequest 契约），其余抛通用异常
                            if (IsTimeout(unityWebRequest))
                                webData.CompletionSource.TrySetException(new TimeoutException(unityWebRequest.error));
                            else
                                webData.CompletionSource.TrySetException(new Exception(unityWebRequest.error));
                            return;
                        }

                        webData.CompletionSource.TrySetResult(new WebBufferResult(webData.UserData, unityWebRequest.downloadHandler.data));
                    }
                    finally
                    {
                        abortRegistration.Dispose();  // 注销取消回调，释放对请求的引用
                        linkedCts?.Dispose();         // 释放链接 CTS（可能未创建）
                        unityWebRequest.Dispose();     // 无论成败均释放原生资源（UnityWebRequest 官方要求）
                        inFlight.Dispose();            // 结束在途登记，CancelAsync 据此等待清理完毕
                    }
                };
                return true;
            }
            catch (Exception e)
            {
                // 构造/发送阶段异常：释放资源、结束在途登记、回写异常，避免调用方永久挂起
                abortRegistration.Dispose();
                linkedCts?.Dispose();
                unityWebRequest?.Dispose();
                inFlight.Dispose();
                webData.CompletionSource.TrySetException(e);
                return false;
            }
        }

        /// <summary>
        /// Pb POST 请求入队（公共 API 核心）。
        /// </summary>
        /// <param name="url">请求 URL。</param>
        /// <param name="message">消息对象。</param>
        /// <param name="token">调用方取消令牌。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>返回 WebBufferResult 类型的异步任务。</returns>
        private UniTask<WebBufferResult> PostPbReq(string url, MessageObject message, CancellationToken token, object userData = null)
        {
            m_Scope.Token.ThrowIfCancellationRequested(); // 模块已销毁（Token 取消）则拒绝新请求
            token.ThrowIfCancellationRequested(); // 调用方已取消则拒绝新请求
            var uniTaskCompletionSource = new UniTaskCompletionSource<WebBufferResult>();
            url = UrlHandler(url, null);
            var id = ProtoMessageIdHandler.GetReqMessageIdByType(message.GetType());
            var messageHttpObject = new MessageHttpObject
            {
                Id       = id,
                UniqueId = message.UniqueId,
                Body     = SerializerHelper.Serialize(message),
            };
            var sendData = SerializerHelper.Serialize(messageHttpObject);
            var webData  = new WebPbData(url, sendData, uniTaskCompletionSource, token, userData);
            m_WaitingPbQueue.Enqueue(webData);
            return uniTaskCompletionSource.Task;
        }

        #endregion

        /// <summary>
        /// URL 标准化。
        /// </summary>
        /// <param name="url">原始 URL。</param>
        /// <param name="queryString">查询参数字典。</param>
        /// <returns>标准化后的 URL。</returns>
        private string UrlHandler(string url, Dictionary<string, string> queryString)
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