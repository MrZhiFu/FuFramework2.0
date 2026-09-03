using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;
using Hotfix.Framework.Core;
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
            ClearJsonReq();
            ClearPbReq();

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