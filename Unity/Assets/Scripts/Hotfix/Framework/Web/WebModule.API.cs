using System;
using System.Collections.Generic;
using System.Threading;
using ProtoBuf;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;
using Hotfix.Framework.Network;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Web
{
    /// <summary>
    /// Web 管理模块的公共 API。
    /// 功能：
    ///     1. 提供 GET/POST 请求：字符串（GetToString/PostToString）、字节数组（GetToBytes/PostToBytes）、Pb 强类型（Post&lt;T&gt;）。
    ///     2. 提供超时（Timeout/ReqTimeout）与每服务器并发上限（MaxConnectionPerServer）配置。
    ///     3. 实现 ICancelAsync：Token 观察生命周期取消，CancelAsync 供框架重启排水等待。
    /// </summary>
    public partial class WebModule
    {
        /// <summary>
        /// 模块单例。
        /// </summary>
        public static WebModule Instance { get; private set; }

        /// <summary>
        /// 取消令牌：模块销毁（OnDispose）后触发，在途操作观察它并中止。
        /// </summary>
        public CancellationToken Token => m_Scope.Token;

        /// <summary>
        /// 触发取消并等待在途操作完成清理后才返回。供框架重启取消清理。
        /// </summary>
        public UniTask CancelAsync() => m_Scope.CancelAsync();

        /// <summary>
        /// 获取或设置超时时间（秒）。
        /// </summary>
        public float Timeout { get; set; } = 5f;

        /// <summary>
        /// 获取或设置每个服务器的最大连接数。
        /// </summary>
        public int MaxConnectionPerServer { get; set; } = 8;

        /// <summary>
        /// 请求超时时间的 TimeSpan 表示。
        /// </summary>
        public TimeSpan ReqTimeout => TimeSpan.FromSeconds(Timeout);

        #region GET 返回字符串的请求

        /// <summary>
        /// 发送 GET 请求，返回字符串。
        /// </summary>
        /// <param name="url">请求地址。</param>
        /// <param name="token">调用方取消令牌。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>字符串结果。</returns>
        public UniTask<WebStringResult> GetToString(string url, CancellationToken token, object userData = null)
        {
            return GetToStringReq(url, null, null, token, userData);
        }

        /// <summary>
        /// 发送 GET 请求，返回字符串。
        /// </summary>
        /// <param name="url">请求地址。</param>
        /// <param name="queryString">请求参数。</param>
        /// <param name="token">调用方取消令牌。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>字符串结果。</returns>
        public UniTask<WebStringResult> GetToString(string url, Dictionary<string, string> queryString, CancellationToken token, object userData = null)
        {
            return GetToStringReq(url, queryString, null, token, userData);
        }

        #endregion

        #region GET 返回字节数组的请求

        /// <summary>
        /// 发送 GET 请求，返回字节数组。
        /// </summary>
        /// <param name="url">请求地址。</param>
        /// <param name="token">调用方取消令牌。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>字节数组结果。</returns>
        public UniTask<WebBufferResult> GetToBytes(string url, CancellationToken token, object userData = null)
        {
            return GetToBytesReq(url, null, null, token, userData);
        }

        /// <summary>
        /// 发送 GET 请求，返回字节数组。
        /// </summary>
        /// <param name="url">请求地址。</param>
        /// <param name="queryString">请求参数。</param>
        /// <param name="token">调用方取消令牌。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>字节数组结果。</returns>
        public UniTask<WebBufferResult> GetToBytes(string url, Dictionary<string, string> queryString, CancellationToken token, object userData = null)
        {
            return GetToBytesReq(url, queryString, null, token, userData);
        }

        #endregion

        #region POST 返回字符串的请求

        /// <summary>
        /// 发送 POST 请求，返回字符串。
        /// </summary>
        /// <param name="url">请求地址。</param>
        /// <param name="from">请求参数。</param>
        /// <param name="token">调用方取消令牌。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>字符串结果。</returns>
        public UniTask<WebStringResult> PostToString(string url, Dictionary<string, object> from, CancellationToken token, object userData = null)
            => PostToStringReq(url, from, null, null, token, userData);

        /// <summary>
        /// 发送 POST 请求，返回字符串。
        /// </summary>
        /// <param name="url">请求地址。</param>
        /// <param name="from">表单请求参数。</param>
        /// <param name="queryString">URL 请求参数。</param>
        /// <param name="token">调用方取消令牌。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>字符串结果。</returns>
        public UniTask<WebStringResult> PostToString(string url, Dictionary<string, object> from, Dictionary<string, string> queryString, CancellationToken token, object userData = null)
            => PostToStringReq(url, from, queryString, null, token, userData);

        #endregion

        #region POST 返回字节数组的请求

        /// <summary>
        /// 发送 POST 请求，返回字节数组。
        /// </summary>
        /// <param name="url">请求地址。</param>
        /// <param name="from">请求参数。</param>
        /// <param name="token">调用方取消令牌。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>字节数组结果。</returns>
        public UniTask<WebBufferResult> PostToBytes(string url, Dictionary<string, object> from, CancellationToken token, object userData = null)
            => PostToBytesReq(url, from, null, null, token, userData);

        /// <summary>
        /// 发送 POST 请求，返回字节数组。
        /// </summary>
        /// <param name="url">请求地址。</param>
        /// <param name="from">表单请求参数。</param>
        /// <param name="queryString">URL 请求参数。</param>
        /// <param name="token">调用方取消令牌。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>字节数组结果。</returns>
        public UniTask<WebBufferResult> PostToBytes(string url, Dictionary<string, object> from, Dictionary<string, string> queryString, CancellationToken token, object userData = null)
            => PostToBytesReq(url, from, queryString, null, token, userData);

        #endregion

        #region POST 返回 Pb 对象的请求

        /// <summary>
        /// 发送 Pb POST 请求，负责序列化、发送、接收、反序列化全过程。
        /// </summary>
        /// <param name="url">目标服务器的 URL 地址。</param>
        /// <param name="message">要发送的消息对象，必须继承自 MessageObject。</param>
        /// <param name="token">调用方取消令牌。</param>
        /// <typeparam name="T">返回的数据类型，必须继承自 MessageObject 并且实现 IResponseMessage 接口。</typeparam>
        /// <returns>反序列化后的响应数据，成功时恒非 null。</returns>
        /// <remarks>
        /// 协议失败（响应消息头反序列化失败、消息为空或 Id 无效、响应类型与 T 不匹配、消息体反序列化失败）抛
        /// <see cref="InvalidOperationException"/>；网络错误/超时抛 <see cref="TimeoutException"/> 或通用异常；
        /// 调用方取消抛 <see cref="OperationCanceledException"/>。调用方应 try-catch 或交由上层统一处理。
        /// </remarks>
        public async UniTask<T> Post<T>(string url, MessageObject message, CancellationToken token) where T : MessageObject, IResponseMessage
        {
            var webBufferResult = await PostPbReq(url, message, token);
            if (webBufferResult.IsNull())
                throw new InvalidOperationException($"Web 请求未返回结果: {url}");

            MessageHttpObject messageObjectHttp;
            try
            {
                messageObjectHttp = SerializerHelper.Deserialize<MessageHttpObject>(webBufferResult.Result);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"响应消息头反序列化失败: {url} ({e.Message})", e);
            }

            if (messageObjectHttp.IsNull() || messageObjectHttp.Id == default)
                throw new InvalidOperationException($"响应消息头解析失败（消息为空或 Id 无效）: {url}");

            var messageType = ProtoMessageIdHandler.GetRespTypeById(messageObjectHttp.Id);
            if (messageType != typeof(T))
            {
                throw new InvalidOperationException($"响应消息类型不匹配: 期望 '{typeof(T).FullName}', 实际 '{messageType?.FullName ?? "未知"}'");
            }

            T result;
            try
            {
                result = SerializerHelper.Deserialize<T>(messageObjectHttp.Body);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"响应消息体反序列化失败: {messageType.FullName} ({e.Message})", e);
            }

            if (result.IsNull())
                throw new InvalidOperationException($"响应消息体反序列化为空: {messageType.FullName}");

            return result;
        }

        #endregion
    }
}