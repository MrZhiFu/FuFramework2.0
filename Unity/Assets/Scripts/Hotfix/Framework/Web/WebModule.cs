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
        private CancellationScope m_Scope = new();

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
        /// 等待处理的 ProtoBuf 请求队列。
        /// </summary>
        private readonly Queue<WebProtoBufData> m_WaitingPbQueue = new(256);

        /// <summary>
        /// 正在处理的 ProtoBuf 请求列表。
        /// </summary>
        private readonly List<WebProtoBufData> m_SendingPbList = new(16);

        /// <summary>
        /// ProtoBuf 内容类型常量。
        /// </summary>
        private const string ProtoBufContentType = "application/x-protobuf";

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
            if (m_SendingJsonList.Count < MaxConnectionPerServer && m_WaitingJsonQueue.Count > 0)
            {
                var webJsonData = m_WaitingJsonQueue.Dequeue();

                switch (webJsonData)
                {
                    case WebJsonStringData stringData:
                        MakeJsonStringRequest(stringData);
                        break;
                    case WebJsonBytesData bytesData:
                        MakeJsonBytesRequest(bytesData);
                        break;
                }

                m_SendingJsonList.Add(webJsonData);
            }

            // 更新处理 ProtoBuf 请求队列
            UpdateProtoBuf();
        }

        /// <summary>
        /// 释放。
        /// </summary>
        protected internal override void OnDispose()
        {
            // 随模块销毁取消在途 Web 请求
            m_Scope.Cancel();

            // 清理等待处理的请求队列
            while (m_WaitingJsonQueue.Count > 0)
            {
                var webData = m_WaitingJsonQueue.Dequeue();
                webData.Dispose();
            }

            m_WaitingJsonQueue.Clear();

            // 清理正在处理的请求列表
            while (m_SendingJsonList.Count > 0)
            {
                var webData = m_SendingJsonList[0];
                m_SendingJsonList.RemoveAt(0);
                webData.Dispose();
            }

            m_SendingJsonList.Clear();

            // 清理 ProtoBuf
            ShutdownProtoBuf();

            Instance = null;
        }

        #region JSON 请求处理

        /// <summary>
        /// 处理 JSON 字符串请求。
        /// </summary>
        private void MakeJsonStringRequest(WebJsonStringData webJsonData)
        {
            FuLogger.LogInfo($"Web Request: {webJsonData.URL} \n Header: {UtilityAOT.Json.ToJson(webJsonData.Header)} \n  Form: {UtilityAOT.Json.ToJson(webJsonData.Form)}");

            var capturedToken   = m_Scope.Token; // 发起时捕获生命周期 Token：重启后旧在途请求据此识别取消，不向旧生命周期调用方抛网络错误
            var unityWebRequest = webJsonData.IsGet ? UnityWebRequest.Get(webJsonData.URL) : UnityWebRequest.PostWwwForm(webJsonData.URL, string.Empty);

            unityWebRequest.timeout = (int)RequestTimeout.TotalSeconds;
            if (webJsonData.Form is { Count: > 0 })
            {
                unityWebRequest.SetRequestHeader("Content-Type", "application/json");
                var body     = UtilityAOT.Json.ToJson(webJsonData.Form);
                var postData = Encoding.UTF8.GetBytes(body);
                unityWebRequest.uploadHandler = new UploadHandlerRaw(postData);
            }

            if (webJsonData.Header is { Count: > 0 })
            {
                foreach (var kv in webJsonData.Header)
                {
                    unityWebRequest.SetRequestHeader(kv.Key, kv.Value);
                }
            }

            var asyncOperation = unityWebRequest.SendWebRequest();
            asyncOperation.completed += _ =>
            {
                try
                {
                    // 模块已销毁/生命周期变更（重启）：旧在途请求按取消处理，不再写回结果
                    if (capturedToken.IsCancellationRequested || capturedToken != m_Scope.Token || webJsonData.Token.IsCancellationRequested)
                    {
                        webJsonData.UniTaskCompletionStringSource.TrySetCanceled();
                        return;
                    }

                    m_SendingJsonList.Remove(webJsonData);
                    if (unityWebRequest.result != UnityWebRequest.Result.Success)
                    {
                        FuLogger.LogInfo($"Web Response: {webJsonData.URL} \n Header: {UtilityAOT.Json.ToJson(webJsonData.Header)} \n  Form: {UtilityAOT.Json.ToJson(webJsonData.Form)} \n Content: {unityWebRequest.error}");

                        // 超时抛 TimeoutException（保持旧 HttpWebRequest 契约），其余抛通用异常
                        if (IsUnityWebRequestTimeout(unityWebRequest))
                            webJsonData.UniTaskCompletionStringSource.TrySetException(new TimeoutException(unityWebRequest.error));
                        else
                            webJsonData.UniTaskCompletionStringSource.TrySetException(new Exception(unityWebRequest.error));
                        return;
                    }

                    FuLogger.LogInfo($"Web Response: {webJsonData.URL} \n Header: {UtilityAOT.Json.ToJson(webJsonData.Header)} \n  Form: {UtilityAOT.Json.ToJson(webJsonData.Form)} \n Content: {unityWebRequest.downloadHandler.text}");
                    webJsonData.UniTaskCompletionStringSource.TrySetResult(new WebStringResult(webJsonData.UserData, unityWebRequest.downloadHandler.text));
                }
                finally
                {
                    unityWebRequest.Dispose(); // 无论成败均释放原生资源（UnityWebRequest 官方要求）
                }
            };
        }

        /// <summary>
        /// 处理 JSON 字节数组请求。
        /// </summary>
        private void MakeJsonBytesRequest(WebJsonBytesData webJsonData)
        {
            FuLogger.LogInfo($"Web Request: {webJsonData.URL} \n Header: {UtilityAOT.Json.ToJson(webJsonData.Header)} \n  Form: {UtilityAOT.Json.ToJson(webJsonData.Form)}");

            var capturedToken   = m_Scope.Token; // 发起时捕获生命周期 Token：重启后旧在途请求据此识别取消，不向旧生命周期调用方抛网络错误
            var unityWebRequest = webJsonData.IsGet ? UnityWebRequest.Get(webJsonData.URL) : UnityWebRequest.PostWwwForm(webJsonData.URL, string.Empty);

            unityWebRequest.timeout = (int)RequestTimeout.TotalSeconds;
            if (webJsonData.Form is { Count: > 0 })
            {
                unityWebRequest.SetRequestHeader("Content-Type", "application/json");
                var body     = UtilityAOT.Json.ToJson(webJsonData.Form);
                var postData = Encoding.UTF8.GetBytes(body);
                unityWebRequest.uploadHandler = new UploadHandlerRaw(postData);
            }

            if (webJsonData.Header is { Count: > 0 })
            {
                foreach (var kv in webJsonData.Header)
                {
                    unityWebRequest.SetRequestHeader(kv.Key, kv.Value);
                }
            }

            var asyncOperation = unityWebRequest.SendWebRequest();
            asyncOperation.completed += _ =>
            {
                try
                {
                    // 模块已销毁/生命周期变更（重启）：旧在途请求按取消处理，不再写回结果
                    if (capturedToken.IsCancellationRequested || capturedToken != m_Scope.Token || webJsonData.Token.IsCancellationRequested)
                    {
                        webJsonData.UniTaskCompletionBytesSource.TrySetCanceled();
                        return;
                    }

                    m_SendingJsonList.Remove(webJsonData);
                    if (unityWebRequest.result != UnityWebRequest.Result.Success)
                    {
                        FuLogger.LogInfo($"Web Response: {webJsonData.URL} \n Header: {UtilityAOT.Json.ToJson(webJsonData.Header)} \n  Form: {UtilityAOT.Json.ToJson(webJsonData.Form)} \n Content: {unityWebRequest.error}");
                        // 超时抛 TimeoutException（保持旧 HttpWebRequest 契约），其余抛通用异常
                        if (IsUnityWebRequestTimeout(unityWebRequest))
                            webJsonData.UniTaskCompletionBytesSource.TrySetException(new TimeoutException(unityWebRequest.error));
                        else
                            webJsonData.UniTaskCompletionBytesSource.TrySetException(new Exception(unityWebRequest.error));
                        return;
                    }

                    FuLogger.LogInfo($"Web Response: {webJsonData.URL} \n Header: {UtilityAOT.Json.ToJson(webJsonData.Header)} \n  Form: {UtilityAOT.Json.ToJson(webJsonData.Form)} \n Content: {unityWebRequest.downloadHandler.data}");
                    webJsonData.UniTaskCompletionBytesSource.TrySetResult(new WebBufferResult(webJsonData.UserData, unityWebRequest.downloadHandler.data));
                }
                finally
                {
                    unityWebRequest.Dispose(); // 无论成败均释放原生资源（UnityWebRequest 官方要求）
                }
            };
        }

        #endregion

        #region ProtoBuf 请求处理

        /// <summary>
        /// 更新处理 ProtoBuf 请求队列。
        /// </summary>
        private void UpdateProtoBuf()
        {
            // 主线程模型：与 OnUpdate 同线程，无需加锁
            if (m_SendingPbList.Count >= MaxConnectionPerServer || m_WaitingPbQueue.Count <= 0) return;
            var webProtoBufData = m_WaitingPbQueue.Dequeue();
            MakeProtoBufBytesRequest(webProtoBufData);
            m_SendingPbList.Add(webProtoBufData);
        }

        /// <summary>
        /// 关闭 ProtoBuf 请求处理，清理资源。
        /// </summary>
        private void ShutdownProtoBuf()
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
        /// 执行 ProtoBuf 字节请求。
        /// </summary>
        /// <param name="webData">ProtoBuf 请求数据。</param>
        private void MakeProtoBufBytesRequest(WebProtoBufData webData)
        {
            var capturedToken   = m_Scope.Token; // 发起时捕获生命周期 Token：重启后旧在途请求据此识别取消，不向旧生命周期调用方抛网络错误
            var unityWebRequest = webData.IsGet ? UnityWebRequest.Get(webData.URL) : UnityWebRequest.PostWwwForm(webData.URL, string.Empty);

            unityWebRequest.timeout = (int)RequestTimeout.TotalSeconds;
            unityWebRequest.SetRequestHeader("Content-Type", ProtoBufContentType);
            unityWebRequest.uploadHandler = new UploadHandlerRaw(webData.SendData);

            var asyncOperation = unityWebRequest.SendWebRequest();
            asyncOperation.completed += _ =>
            {
                try
                {
                    // 模块已销毁/生命周期变更（重启）：旧在途请求按取消处理，不再写回结果
                    if (capturedToken.IsCancellationRequested || capturedToken != m_Scope.Token || webData.Token.IsCancellationRequested)
                    {
                        webData.CompletionSource.TrySetCanceled();
                        return;
                    }

                    m_SendingPbList.Remove(webData);
                    if (unityWebRequest.result != UnityWebRequest.Result.Success)
                    {
                        // 超时抛 TimeoutException（保持旧 HttpWebRequest 契约），其余抛通用异常
                        if (IsUnityWebRequestTimeout(unityWebRequest))
                            webData.CompletionSource.TrySetException(new TimeoutException(unityWebRequest.error));
                        else
                            webData.CompletionSource.TrySetException(new Exception(unityWebRequest.error));
                        return;
                    }

                    webData.CompletionSource.TrySetResult(new WebBufferResult(webData.UserData, unityWebRequest.downloadHandler.data));
                }
                finally
                {
                    unityWebRequest.Dispose(); // 无论成败均释放原生资源（UnityWebRequest 官方要求）
                }
            };
        }

        /// <summary>
        /// 内部 Post 请求处理方法。
        /// </summary>
        /// <param name="url">请求 URL。</param>
        /// <param name="message">消息对象。</param>
        /// <param name="token">调用方取消令牌。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>返回 WebBufferResult 类型的异步任务。</returns>
        private UniTask<WebBufferResult> PostInternal(string url, MessageObject message, CancellationToken token, object userData = null)
        {
            m_Scope.Token.ThrowIfCancellationRequested(); // 模块已销毁（Token 取消）则拒绝新请求
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
            var webData  = new WebProtoBufData(url, sendData, uniTaskCompletionSource, token, userData);
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

            if (queryString is not { Count: > 0 }) return url;

            if (!url.EndsWithFast("?"))
                m_UrlStr.Append("?");

            foreach (var kv in queryString)
            {
                m_UrlStr.AppendFormat("{0}={1}&", kv.Key, kv.Value);
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
        private static bool IsUnityWebRequestTimeout(UnityWebRequest unityWebRequest)
        {
            if (unityWebRequest.error == null) return false;

            return unityWebRequest.error.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) < 0;
        }
    }
}