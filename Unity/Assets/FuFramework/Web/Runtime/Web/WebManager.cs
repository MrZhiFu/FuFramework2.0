using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;
using FuFramework.Core.Runtime;
using System.Collections.Generic;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable once CheckNamespace
namespace FuFramework.Web.Runtime
{
    /// <summary>
    /// Web请求管理器,实现HTTP GET和POST请求功能
    /// </summary>
    public partial class WebManager : FuModule, IWebManager
    {
        /// 用于构建URL的StringBuilder
        private readonly StringBuilder m_StringBuilder = new(256);

        /// 等待处理的普通请求队列
        private readonly Queue<WebJsonData> m_WaitingNormalQueue = new(256);

        /// 正在处理的普通请求列表
        private readonly List<WebJsonData> m_SendingNormalList = new(16);

        /// 用于存储请求和响应数据的内存流
        private readonly MemoryStream m_MemoryStream;

        /// JSON内容类型常量
        private const string JsonContentType = "application/json; charset=utf-8";

        /// 超时时间(秒)
        private float m_Timeout = 5f;

        /// <summary>
        /// 构造函数
        /// </summary>
        public WebManager()
        {
            MaxConnectionPerServer = 8;
            m_MemoryStream         = new MemoryStream();
            Timeout                = 5f;
        }

        /// <summary>
        /// 获取或设置超时时间(秒)
        /// </summary>
        public float Timeout
        {
            get => m_Timeout;
            set
            {
                m_Timeout      = value;
                RequestTimeout = TimeSpan.FromSeconds(value);
            }
        }

        /// <summary>
        /// 获取或设置每个服务器的最大连接数
        /// </summary>
        public int MaxConnectionPerServer { get; set; }

        /// <summary>
        /// 获取或设置请求超时时间
        /// </summary>
        public TimeSpan RequestTimeout { get; set; }

        /// <summary>
        /// 更新处理请求队列
        /// </summary>
        protected override void Update(float elapseSeconds, float realElapseSeconds)
        {
            lock (m_StringBuilder)
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

                UpdateProtoBuf(elapseSeconds, realElapseSeconds);
            }
        }

        /// <summary>
        /// 关闭时清理资源
        /// </summary>
        protected override void Shutdown()
        {
            while (m_WaitingNormalQueue.Count > 0)
            {
                var webData = m_WaitingNormalQueue.Dequeue();
                webData.Dispose();
            }

            m_WaitingNormalQueue.Clear();
            while (m_SendingNormalList.Count > 0)
            {
                var webData = m_SendingNormalList[0];
                m_SendingNormalList.RemoveAt(0);
                webData.Dispose();
            }

            m_SendingNormalList.Clear();
            ShutdownProtoBuf();

            m_MemoryStream.Dispose();
        }

        /// <summary>
        /// 发送Get 请求
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="userData">用户自定义数据</param>
        /// <returns></returns>
        public Task<WebStringResult> GetToString(string url, object userData = null)
        {
            return GetToString(url, null, null, userData);
        }

        /// <summary>
        /// 发送Get 请求
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="userData">用户自定义数据</param>
        /// <returns></returns>
        public Task<WebBufferResult> GetToBytes(string url, object userData = null)
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
        public Task<WebStringResult> GetToString(string url, Dictionary<string, string> queryString, object userData = null)
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
        public Task<WebBufferResult> GetToBytes(string url, Dictionary<string, string> queryString, object userData = null)
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
        public Task<WebStringResult> GetToString(string url, Dictionary<string, string> queryString, Dictionary<string, string> header,
                                                 object userData = null)
        {
            var uniTaskCompletionSource = new TaskCompletionSource<WebStringResult>();
            url = UrlHandler(url, queryString);

            var webJsonData = new WebJsonData(url, header, true, uniTaskCompletionSource, userData);
            m_WaitingNormalQueue.Enqueue(webJsonData);
            return uniTaskCompletionSource.Task;
        }

        /// <summary>
        /// 处理JSON字符串请求
        /// </summary>
        private async void MakeJsonStringRequest(WebJsonData webJsonData)
        {
            Log.Info($"Web Request: {webJsonData.URL} \n Header: {Utility.Json.ToJson(webJsonData.Header)} \n  Form: {Utility.Json.ToJson(webJsonData.Form)}");

            var unityWebRequest = webJsonData.IsGet ? UnityWebRequest.Get(webJsonData.URL) : UnityWebRequest.PostWwwForm(webJsonData.URL, string.Empty);

            unityWebRequest.timeout = (int)RequestTimeout.TotalSeconds;
            if (webJsonData.Form is { Count: > 0 })
            {
                unityWebRequest.SetRequestHeader("Content-Type", "application/json");
                var body     = Utility.Json.ToJson(webJsonData.Form);
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
                m_SendingNormalList.Remove(webJsonData);
                if (unityWebRequest.result != UnityWebRequest.Result.Success && unityWebRequest.error != null)
                {
                    Log.Info($"Web Response: {webJsonData.URL} \n Header: {Utility.Json.ToJson(webJsonData.Header)} \n  Form: {Utility.Json.ToJson(webJsonData.Form)} \n Content: {unityWebRequest.error}");
                    webJsonData.UniTaskCompletionStringSource.TrySetException(new Exception(unityWebRequest.error));
                    return;
                }

                Log.Info($"Web Response: {webJsonData.URL} \n Header: {Utility.Json.ToJson(webJsonData.Header)} \n  Form: {Utility.Json.ToJson(webJsonData.Form)} \n Content: {unityWebRequest.downloadHandler.text}");
                webJsonData.UniTaskCompletionStringSource.SetResult(new WebStringResult(webJsonData.UserData, unityWebRequest.downloadHandler.text));
            };

            try
            {
                var request = WebRequest.CreateHttp(webJsonData.URL);
                request.Method      = webJsonData.IsGet ? WebRequestMethods.Http.Get : WebRequestMethods.Http.Post;
                request.Timeout     = (int)RequestTimeout.TotalMilliseconds; // 设置请求超时时间
                request.Credentials = CredentialCache.DefaultCredentials;
                if (webJsonData.Form is { Count: > 0 })
                {
                    request.ContentType = "application/json";
                    var body     = Utility.Json.ToJson(webJsonData.Form);
                    var postData = Encoding.UTF8.GetBytes(body);
                    request.ContentLength = postData.Length;
                    await using var requestStream = request.GetRequestStream();
                    await requestStream.WriteAsync(postData, 0, postData.Length);
                }

                if (webJsonData.Header is { Count: > 0 })
                {
                    foreach (var kv in webJsonData.Header)
                    {
                        request.Headers[kv.Key] = kv.Value;
                    }
                }

                using var response       = (HttpWebResponse)await request.GetResponseAsync();
                var       responseStream = response.GetResponseStream();
                if (responseStream == null) return;

                using var reader  = new StreamReader(responseStream);
                var       content = await reader.ReadToEndAsync();
                Log.Info($"Web Response: {webJsonData.URL} \n Header: {Utility.Json.ToJson(webJsonData.Header)} \n  Form: {Utility.Json.ToJson(webJsonData.Form)} \n Content: {content}");
                webJsonData.UniTaskCompletionStringSource.SetResult(new WebStringResult(webJsonData.UserData, content));
            }
            catch (WebException e)
            {
                // 捕获超时异常
                if (e.Status == WebExceptionStatus.Timeout)
                {
                    webJsonData.UniTaskCompletionStringSource.SetException(new TimeoutException(e.Message));
                    return;
                }

                Log.Info($"Web Response: {webJsonData.URL} \n Header: {Utility.Json.ToJson(webJsonData.Header)} \n  Form: {Utility.Json.ToJson(webJsonData.Form)} \n Content: {e.Message}");
                webJsonData.UniTaskCompletionStringSource.SetException(e);
            }
            catch (IOException e)
            {
                Log.Info($"Web Response: {webJsonData.URL} \n Header: {Utility.Json.ToJson(webJsonData.Header)} \n  Form: {Utility.Json.ToJson(webJsonData.Form)} \n Content: {e.Message}");
                webJsonData.UniTaskCompletionStringSource.SetException(e);
            }
            catch (Exception e)
            {
                Log.Info($"Web Response: {webJsonData.URL} \n Header: {Utility.Json.ToJson(webJsonData.Header)} \n  Form: {Utility.Json.ToJson(webJsonData.Form)} \n Content: {e.Message}");
                webJsonData.UniTaskCompletionStringSource.SetException(e);
            }
            finally
            {
                m_SendingNormalList.Remove(webJsonData);
            }
        }

        /// <summary>
        /// 处理JSON字节数组请求
        /// </summary>
        private async void MakeJsonBytesRequest(WebJsonData webJsonData)
        {
            Log.Info($"Web Request: {webJsonData.URL} \n Header: {Utility.Json.ToJson(webJsonData.Header)} \n  Form: {Utility.Json.ToJson(webJsonData.Form)}");

#if UNITY_WEBGL
            var unityWebRequest = webJsonData.IsGet ? UnityWebRequest.Get(webJsonData.URL) : UnityWebRequest.PostWwwForm(webJsonData.URL, string.Empty);

            unityWebRequest.timeout = (int)RequestTimeout.TotalSeconds;
            if (webJsonData.Form is { Count: > 0 })
            {
                unityWebRequest.SetRequestHeader("Content-Type", "application/json");
                var body = Utility.Json.ToJson(webJsonData.Form);
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
                m_SendingNormalList.Remove(webJsonData);
                if (unityWebRequest.result != UnityWebRequest.Result.Success || unityWebRequest.error != null)
                {
                    Log.Info($"Web Response: {webJsonData.URL} \n Header: {Utility.Json.ToJson(webJsonData.Header)} \n  Form: {Utility.Json.ToJson(webJsonData.Form)} \n Content: {unityWebRequest.error}");
                    webJsonData.UniTaskCompletionBytesSource.TrySetException(new Exception(unityWebRequest.error));
                    return;
                }

                Log.Info($"Web Response: {webJsonData.URL} \n Header: {Utility.Json.ToJson(webJsonData.Header)} \n  Form: {Utility.Json.ToJson(webJsonData.Form)} \n Content: {unityWebRequest.downloadHandler.data}");
                webJsonData.UniTaskCompletionBytesSource.SetResult(new WebBufferResult(webJsonData.UserData, unityWebRequest.downloadHandler.data));
            };
#else
            try
            {
                var request = WebRequest.CreateHttp(webJsonData.URL);
                request.Method      = webJsonData.IsGet ? WebRequestMethods.Http.Get : WebRequestMethods.Http.Post;
                request.Timeout     = (int)RequestTimeout.TotalMilliseconds; // 设置请求超时时间
                request.Credentials = CredentialCache.DefaultCredentials;
                if (webJsonData.Header is { Count: > 0 })
                {
                    foreach (var kv in webJsonData.Header)
                    {
                        request.Headers[kv.Key] = kv.Value;
                    }
                }

                if (webJsonData.Form is { Count: > 0 })
                {
                    request.ContentType = "application/json";
                    var body     = Utility.Json.ToJson(webJsonData.Form);
                    var postData = Encoding.UTF8.GetBytes(body);
                    request.ContentLength = postData.Length;
                    await using var requestStream = request.GetRequestStream();
                    await requestStream.WriteAsync(postData, 0, postData.Length);
                }

                using var       response       = (HttpWebResponse)await request.GetResponseAsync();
                await using var responseStream = response.GetResponseStream();
                if (responseStream == null) return;
                m_MemoryStream.SetLength(responseStream.Length);
                m_MemoryStream.Position = 0;
                await responseStream.CopyToAsync(m_MemoryStream);
                var resultData = m_MemoryStream.ToArray();
                Log.Info($"Web Response: {webJsonData.URL} \n Header: {Utility.Json.ToJson(webJsonData.Header)} \n  Form: {Utility.Json.ToJson(webJsonData.Form)} \n Content: {resultData}");
                webJsonData.UniTaskCompletionBytesSource.SetResult(new WebBufferResult(webJsonData.UserData, resultData)); // 将流的内容复制到内存流中并转换为byte数组 
            }
            catch (WebException e)
            {
                Log.Info($"Web Response: {webJsonData.URL} \n Header: {Utility.Json.ToJson(webJsonData.Header)} \n  Form: {Utility.Json.ToJson(webJsonData.Form)} \n Content: {e.Message}");

                // 捕获超时异常
                if (e.Status == WebExceptionStatus.Timeout)
                {
                    webJsonData.UniTaskCompletionBytesSource.SetException(new TimeoutException(e.Message));
                    return;
                }

                webJsonData.UniTaskCompletionBytesSource.SetException(e);
            }
            catch (IOException e)
            {
                Log.Info($"Web Response: {webJsonData.URL} \n Header: {Utility.Json.ToJson(webJsonData.Header)} \n  Form: {Utility.Json.ToJson(webJsonData.Form)} \n Content: {e.Message}");
                webJsonData.UniTaskCompletionBytesSource.SetException(e);
            }
            catch (Exception e)
            {
                Log.Info($"Web Response: {webJsonData.URL} \n Header: {Utility.Json.ToJson(webJsonData.Header)} \n  Form: {Utility.Json.ToJson(webJsonData.Form)} \n Content: {e.Message}");
                webJsonData.UniTaskCompletionBytesSource.SetException(e);
            }
            finally
            {
                m_SendingNormalList.Remove(webJsonData);
            }

#endif
        }

        /// <summary>
        /// 发送Get 请求
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="queryString">请求参数</param>
        /// <param name="header">请求头</param>
        /// <param name="userData">用户自定义数据</param>
        /// <returns></returns>
        public Task<WebBufferResult> GetToBytes(string url, Dictionary<string, string> queryString, Dictionary<string, string> header, object userData = null)
        {
            var uniTaskCompletionSource = new TaskCompletionSource<WebBufferResult>();
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
        public Task<WebStringResult> PostToString(string url, Dictionary<string, object> from, object userData = null)
            => PostToString(url, from, null, null, userData);

        /// <summary>
        /// 发送Post 请求
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="from">请求参数</param>
        /// <param name="userData">用户自定义数据</param>
        /// <returns></returns>
        public Task<WebBufferResult> PostToBytes(string url, Dictionary<string, object> from, object userData = null)
            => PostToBytes(url, from, null, null, userData);

        /// <summary>
        /// 发送Post 请求
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="from">表单请求参数</param>
        /// <param name="queryString">URl请求参数</param>
        /// <param name="userData">用户自定义数据</param>
        /// <returns></returns>
        public Task<WebStringResult> PostToString(string url, Dictionary<string, object> from, Dictionary<string, string> queryString, object userData = null)
            => PostToString(url, from, queryString, null, userData);

        /// <summary>
        /// 发送Post 请求
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="from">表单请求参数</param>
        /// <param name="queryString">URl请求参数</param>
        /// <param name="userData">用户自定义数据</param>
        /// <returns></returns>
        public Task<WebBufferResult> PostToBytes(string url, Dictionary<string, object> from, Dictionary<string, string> queryString, object userData = null)
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
        public Task<WebStringResult> PostToString(string url, Dictionary<string, object> from, Dictionary<string, string> queryString, Dictionary<string, string> header, object userData = null)
        {
            var uniTaskCompletionSource = new TaskCompletionSource<WebStringResult>();
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
        public Task<WebBufferResult> PostToBytes(string url, Dictionary<string, object> from, Dictionary<string, string> queryString, Dictionary<string, string> header, object userData = null)
        {
            var uniTaskCompletionSource = new TaskCompletionSource<WebBufferResult>();
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
            m_StringBuilder.Clear();
            m_StringBuilder.Append(url);

            if (queryString is not { Count: > 0 }) return url;

            if (!url.EndsWithFast("?"))
                m_StringBuilder.Append("?");

            foreach (var kv in queryString)
            {
                m_StringBuilder.AppendFormat("{0}={1}&", kv.Key, kv.Value);
            }

            url = m_StringBuilder.ToString(0, m_StringBuilder.Length - 1);
            m_StringBuilder.Clear();

            return url;
        }
    }
}