using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;
using System.Collections.Generic;
using UtilityAOT = AOT.Framework.Core.Utility.UtilityAOT;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Web
{
    /// <summary>
    /// Web管理模块。
    /// 功能：
    ///     1.实现HTTP GET和POST请求功能。
    /// </summary>
    public partial class WebModule : ModuleBase, ICancelAsync
    {
        /// <summary>
        /// 模块单例
        /// </summary>
        public static WebModule Instance { get; private set; }

        /// <summary>
        /// 取消范围：内部 CTS + 在途计数 + 全部完成信号。每次 OnInit 重建（新生命周期 = 新 Token）。
        /// OnDispose 时 Cancel，在途 Web 请求随之取消；框架重启前经 CancelAllAsync 等待取消清理完成。
        /// </summary>
        private CancellationScope m_Scope = new();

        /// <summary>
        /// 取消令牌：模块销毁（OnDispose）后触发，在途操作观察它并中止。
        /// </summary>
        public CancellationToken Token => m_Scope.Token;

        /// <summary>
        /// 触发取消并等待在途操作完成清理后才返回。供框架重启取消清理。
        /// </summary>
        public UniTask CancelAsync() => m_Scope.CancelAsync();

        /// <summary>
        /// 用于构建URL的StringBuilder
        /// </summary>
        private readonly StringBuilder m_UrlStr = new(256);

        /// <summary>
        /// 等待处理的普通请求队列
        /// </summary>
        private readonly Queue<WebJsonData> m_WaitingNormalQueue = new(256);

        /// <summary>
        /// 正在处理的普通请求列表
        /// </summary>
        private readonly List<WebJsonData> m_SendingNormalList = new(16);

        /// <summary>
        /// 获取或设置超时时间(秒)
        /// </summary>
        public float Timeout { get; set; } = 5f;

        /// <summary>
        /// 获取或设置每个服务器的最大连接数
        /// </summary>
        public int MaxConnectionPerServer { get; set; } = 8;

        /// <summary>
        /// 获取或设置请求超时时间
        /// </summary>
        public TimeSpan RequestTimeout => TimeSpan.FromSeconds(Timeout);

        /// <summary>
        /// 初始化
        /// </summary>
        protected internal override void OnInit()
        {
            Instance = this;
            m_Scope = new CancellationScope(); 
        }

        /// <summary>
        /// 轮询处理请求队列
        /// </summary>
        protected internal override void OnUpdate(float deltaTime, float unscaledDeltaTime)
        {
            lock (m_UrlStr)
            {
                if (m_SendingNormalList.Count < MaxConnectionPerServer && m_WaitingNormalQueue.Count > 0)
                {
                    var webJsonData = m_WaitingNormalQueue.Dequeue();

                    if (webJsonData.UniTaskCompletionStringSource != null)
                        MakeJsonStringRequest(webJsonData);
                    else
                        MakeJsonBytesRequest(webJsonData);

                    m_SendingNormalList.Add(webJsonData);
                }

                UpdateProtoBuf();
            }
        }

        /// <summary>
        /// 释放
        /// </summary>
        protected internal override void OnDispose()
        {
            // 随模块销毁取消在途 Web 请求
            m_Scope.Cancel();

            // 清理等待处理的请求队列
            while (m_WaitingNormalQueue.Count > 0)
            {
                var webData = m_WaitingNormalQueue.Dequeue();
                webData.Dispose();
            }
            m_WaitingNormalQueue.Clear();
            
            // 清理正在处理的请求列表
            while (m_SendingNormalList.Count > 0)
            {
                var webData = m_SendingNormalList[0];
                m_SendingNormalList.RemoveAt(0);
                webData.Dispose();
            }

            m_SendingNormalList.Clear();
            
            // 清理ProtoBuf
            ShutdownProtoBuf();

            Instance = null;
        }

        /// <summary>
        /// 发送Get 请求
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="userData">用户自定义数据</param>
        /// <returns></returns>
        public UniTask<WebStringResult> GetToString(string url, object userData = null)
        {
            return GetToString(url, null, null, userData);
        }

        /// <summary>
        /// 发送Get 请求
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="userData">用户自定义数据</param>
        /// <returns></returns>
        public UniTask<WebBufferResult> GetToBytes(string url, object userData = null)
        {
            return GetToBytes(url, null, null, userData);
        }

        /// <summary>
        /// 发送Get 请求
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="queryString">请求参数</param>
        /// <param name="userData">用户自定义数据</param>
        /// <returns></returns>
        public UniTask<WebStringResult> GetToString(string url, Dictionary<string, string> queryString, object userData = null)
        {
            return GetToString(url, queryString, null, userData);
        }

        /// <summary>
        /// 发送Get 请求
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="queryString">请求参数</param>
        /// <param name="userData">用户自定义数据</param>
        /// <returns></returns>
        public UniTask<WebBufferResult> GetToBytes(string url, Dictionary<string, string> queryString, object userData = null)
        {
            return GetToBytes(url, queryString, null, userData);
        }

        /// <summary>
        /// 发送Get 请求
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="queryString">请求参数</param>
        /// <param name="header">请求头</param>
        /// <param name="userData">用户自定义数据</param>
        /// <returns></returns>
        private UniTask<WebStringResult> GetToString(string url, Dictionary<string, string> queryString, Dictionary<string, string> header,
                                                     object userData = null)
        {
            // 模块已销毁（Token 取消）则拒绝新请求
            m_Scope.Token.ThrowIfCancellationRequested();
            var uniTaskCompletionSource = new UniTaskCompletionSource<WebStringResult>();
            url = UrlHandler(url, queryString);

            var webJsonData = new WebJsonData(url, header, true, uniTaskCompletionSource, userData);
            m_WaitingNormalQueue.Enqueue(webJsonData);
            return uniTaskCompletionSource.Task;
        }

        /// <summary>
        /// 处理JSON字符串请求
        /// </summary>
        private void MakeJsonStringRequest(WebJsonData webJsonData)
        {
            FuLogger.LogInfo($"Web Request: {webJsonData.URL} \n Header: {UtilityAOT.Json.ToJson(webJsonData.Header)} \n  Form: {UtilityAOT.Json.ToJson(webJsonData.Form)}");

            var capturedToken = m_Scope.Token; // 发起时捕获生命周期 Token：重启后旧在途请求据此识别取消，不向旧生命周期调用方抛网络错误
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
                    if (capturedToken.IsCancellationRequested || capturedToken != m_Scope.Token)
                    {
                        webJsonData.UniTaskCompletionStringSource.TrySetCanceled();
                        return;
                    }

                    m_SendingNormalList.Remove(webJsonData);
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
        /// 处理JSON字节数组请求
        /// </summary>
        private void MakeJsonBytesRequest(WebJsonData webJsonData)
        {
            FuLogger.LogInfo($"Web Request: {webJsonData.URL} \n Header: {UtilityAOT.Json.ToJson(webJsonData.Header)} \n  Form: {UtilityAOT.Json.ToJson(webJsonData.Form)}");

            var capturedToken = m_Scope.Token; // 发起时捕获生命周期 Token：重启后旧在途请求据此识别取消，不向旧生命周期调用方抛网络错误
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
                    if (capturedToken.IsCancellationRequested || capturedToken != m_Scope.Token)
                    {
                        webJsonData.UniTaskCompletionBytesSource.TrySetCanceled();
                        return;
                    }

                    m_SendingNormalList.Remove(webJsonData);
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

        /// <summary>
        /// 发送Get 请求
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="queryString">请求参数</param>
        /// <param name="header">请求头</param>
        /// <param name="userData">用户自定义数据</param>
        /// <returns></returns>
        public UniTask<WebBufferResult> GetToBytes(string url, Dictionary<string, string> queryString, Dictionary<string, string> header, object userData = null)
        {
            m_Scope.Token.ThrowIfCancellationRequested(); // 模块已销毁（Token 取消）则拒绝新请求
            var uniTaskCompletionSource = new UniTaskCompletionSource<WebBufferResult>();
            url = UrlHandler(url, queryString);

            var webJsonData = new WebJsonData(url, header, true, uniTaskCompletionSource, userData);
            m_WaitingNormalQueue.Enqueue(webJsonData);
            return uniTaskCompletionSource.Task;
        }

        /// <summary>
        /// 发送Post 请求
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="from">请求参数</param>
        /// <param name="userData">用户自定义数据</param>
        /// <returns></returns>
        public UniTask<WebStringResult> PostToString(string url, Dictionary<string, object> from, object userData = null)
            => PostToString(url, from, null, null, userData);

        /// <summary>
        /// 发送Post 请求
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="from">请求参数</param>
        /// <param name="userData">用户自定义数据</param>
        /// <returns></returns>
        public UniTask<WebBufferResult> PostToBytes(string url, Dictionary<string, object> from, object userData = null)
            => PostToBytes(url, from, null, null, userData);

        /// <summary>
        /// 发送Post 请求
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="from">表单请求参数</param>
        /// <param name="queryString">URl请求参数</param>
        /// <param name="userData">用户自定义数据</param>
        /// <returns></returns>
        public UniTask<WebStringResult> PostToString(string url, Dictionary<string, object> from, Dictionary<string, string> queryString, object userData = null)
            => PostToString(url, from, queryString, null, userData);

        /// <summary>
        /// 发送Post 请求
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="from">表单请求参数</param>
        /// <param name="queryString">URl请求参数</param>
        /// <param name="userData">用户自定义数据</param>
        /// <returns></returns>
        public UniTask<WebBufferResult> PostToBytes(string url, Dictionary<string, object> from, Dictionary<string, string> queryString, object userData = null)
            => PostToBytes(url, from, queryString, null, userData);

        /// <summary>
        /// 发送Post 请求
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="from">表单请求参数</param>
        /// <param name="queryString">URl请求参数</param>
        /// <param name="header">请求头</param>
        /// <param name="userData">用户自定义数据</param>
        /// <returns></returns>
        public UniTask<WebStringResult> PostToString(string url, Dictionary<string, object> from, Dictionary<string, string> queryString, Dictionary<string, string> header, object userData = null)
        {
            m_Scope.Token.ThrowIfCancellationRequested(); // 模块已销毁（Token 取消）则拒绝新请求
            var uniTaskCompletionSource = new UniTaskCompletionSource<WebStringResult>();
            url = UrlHandler(url, queryString);

            var webJsonData = new WebJsonData(url, header, from, uniTaskCompletionSource, userData);
            m_WaitingNormalQueue.Enqueue(webJsonData);
            return uniTaskCompletionSource.Task;
        }

        /// <summary>
        /// 发送Post 请求
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="from">表单请求参数</param>
        /// <param name="queryString">URl请求参数</param>
        /// <param name="header">请求头</param>
        /// <param name="userData">用户自定义数据</param>
        /// <returns></returns>
        public UniTask<WebBufferResult> PostToBytes(string url, Dictionary<string, object> from, Dictionary<string, string> queryString, Dictionary<string, string> header, object userData = null)
        {
            m_Scope.Token.ThrowIfCancellationRequested(); // 模块已销毁（Token 取消）则拒绝新请求
            var uniTaskCompletionSource = new UniTaskCompletionSource<WebBufferResult>();
            url = UrlHandler(url, queryString);

            var webJsonData = new WebJsonData(url, header, from, uniTaskCompletionSource, userData);
            m_WaitingNormalQueue.Enqueue(webJsonData);
            return uniTaskCompletionSource.Task;
        }

        /// <summary>
        /// URL 标准化
        /// </summary>
        /// <param name="url">原始URL</param>
        /// <param name="queryString">查询参数字典</param>
        /// <returns>标准化后的URL</returns>
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
            return unityWebRequest.error != null
                && unityWebRequest.error.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
