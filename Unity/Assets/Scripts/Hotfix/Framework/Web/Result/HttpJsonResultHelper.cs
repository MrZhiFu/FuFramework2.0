using System;
using Newtonsoft.Json;
using AOT.Framework.Core.Log;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Web
{
    /// <summary>
    /// HTTP JSON 结果数据类。
    /// 功能：
    ///     1. 用于封装 HTTP 请求的返回结果，提供统一的结构以便于处理和解析响应数据。
    /// </summary>
    /// <typeparam name="T">消息类型，表示返回的数据对象的类型。</typeparam>
    public sealed class HttpJsonResultData<T>
    {
        /// <summary>
        /// 是否成功。
        /// 表示请求是否成功执行，成功为 true，失败为 false。
        /// </summary>
        public bool IsSuccess { get; set; } = false;

        /// <summary>
        /// 响应码。
        /// 表示请求的处理结果，为 0 表示成功，其他值表示不同的错误类型。
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// 数据对象。
        /// 包含请求成功时返回的数据，类型为 T。如果请求失败，可能为默认值或 null。
        /// </summary>
        public T Data { get; set; }
    }

    /// <summary>
    /// HTTP JSON 消息响应结构。
    /// 功能：
    ///     1. 用于封装 HTTP JSON 请求的消息响应结构，包含响应码、响应消息和响应数据。
    /// </summary>
    public sealed class HttpJsonResult
    {
        /// <summary>
        /// 响应码，0 表示响应成功。
        /// </summary>
        [JsonProperty(PropertyName = "code")]
        public int Code { get; set; }

        /// <summary>
        /// 响应消息。
        /// </summary>
        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }

        /// <summary>
        /// 响应数据。
        /// </summary>
        [JsonProperty(PropertyName = "data")]
        public string Data { get; set; }

        /// <summary>
        /// 将响应结构序列化为 JSON 字符串。
        /// </summary>
        /// <returns>序列化后的 JSON 字符串。</returns>
        public override string ToString() => JsonConvert.SerializeObject(this);
    }

    /// <summary>
    /// HTTP JSON 辅助类。
    /// 功能：
    ///     1. 将 JSON 字符串转换为 HttpJsonResultData&lt;T&gt; 对象。
    /// </summary>
    public static class HttpJsonResultHelper
    {
        /// <summary>
        /// 将 JSON 字符串转换为 HttpJsonResultData&lt;T&gt; 对象。
        /// 该方法尝试反序列化给定的 JSON 字符串，并根据 HTTP 响应的状态码设置 IsSuccess 属性。
        /// 如果响应成功，Data 属性将包含反序列化后的数据对象；否则，Data 将为默认值。
        /// </summary>
        /// <typeparam name="T">要反序列化为的对象类型，必须是类并具有无参数构造函数。</typeparam>
        /// <param name="jsonResult">包含 HTTP 响应的 JSON 字符串。</param>
        /// <returns>HttpJsonResultData&lt;T&gt; 对象，表示反序列化的结果。</returns>
        public static HttpJsonResultData<T> ToHttpJsonResultData<T>(this string jsonResult) where T : class, new()
        {
            var resultData = new HttpJsonResultData<T> { IsSuccess = false, };

            try
            {
                // 反序列化 JSON 字符串为 HttpJsonResult 对象
                var result = JsonConvert.DeserializeObject<HttpJsonResult>(jsonResult);

                // 检查响应码是否表示成功
                if (result.Code != 0)
                {
                    // 返回默认的失败结果
                    resultData.Code = result.Code;
                    return resultData;
                }

                // 设置成功标志
                resultData.IsSuccess = true;

                // 反序列化数据部分，如果数据为空则返回类型 T 的默认实例
                resultData.Data = string.IsNullOrEmpty(result.Data) ? new T() : JsonConvert.DeserializeObject<T>(result.Data);
            }
            catch (Exception e)
            {
                // 捕获并输出异常信息
                FuLogger.LogError(e);
            }

            return resultData;
        }
    }
}