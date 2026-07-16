using System;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    public static partial class Utility
    {
        /// <summary>
        /// JSON 相关的实用函数。
        /// 功能：
        ///     1. 使用Newtonsoft.Json序列化对象为 JSON 字符串。
        ///     2. 使用Newtonsoft.Json反序列化 JSON 字符串为对象。
        /// </summary>
        public static class Json
        {
            /// <summary>
            /// 将对象序列化为 JSON 字符串。
            /// </summary>
            /// <param name="obj">要序列化的对象。</param>
            /// <returns>序列化后的 JSON 字符串。</returns>
            public static string ToJson(object obj)
            {
                if (obj == null) return string.Empty;

                try
                {
                    return JsonConvert.SerializeObject(obj);
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException($"无法序列化为JSON，异常 '{exception}'.", exception);
                }
            }

            /// <summary>
            /// 将 JSON 字符串反序列化为对象。
            /// </summary>
            /// <typeparam name="T">对象类型。</typeparam>
            /// <param name="json">要反序列化的 JSON 字符串。</param>
            /// <returns>反序列化后的对象。</returns>
            public static T ToObject<T>(string json)
            {
                if (string.IsNullOrEmpty(json)) return default;

                try
                {
                    return JsonConvert.DeserializeObject<T>(json);
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException($"无法反序列化为JSON对象，异常 '{exception}'.", exception);
                }
            }

            /// <summary>
            /// 将 JSON 字符串反序列化为对象。
            /// </summary>
            /// <param name="objectType">对象类型。</param>
            /// <param name="json">要反序列化的 JSON 字符串。</param>
            /// <returns>反序列化后的对象。</returns>
            public static object ToObject(Type objectType, string json)
            {
                if (objectType == null) return default;

                try
                {
                    return JsonConvert.DeserializeObject(json, objectType);
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException($"无法反序列化为JSON对象，异常 '{exception}'.", exception);
                }
            }
        }
    }
}