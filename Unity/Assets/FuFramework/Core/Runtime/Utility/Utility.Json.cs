using System;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    public static partial class Utility
    {
        /// <summary>
        /// JSON 相关的实用函数。
        /// 功能：
        /// 1. 使用Json辅助器序列化对象为 JSON 字符串。
        /// 2. 使用Json辅助器反序列化 JSON 字符串为对象。
        /// </summary>
        public static partial class Json
        {
            /// JSON 辅助器
            private static IJsonHelper _jsonHelper;

            /// <summary>
            /// 设置 JSON 辅助器。
            /// </summary>
            /// <param name="jsonHelper">要设置的 JSON 辅助器。</param>
            public static void SetJsonHelper(IJsonHelper jsonHelper) => _jsonHelper = jsonHelper;

            /// <summary>
            /// 将对象序列化为 JSON 字符串。
            /// </summary>
            /// <param name="obj">要序列化的对象。</param>
            /// <returns>序列化后的 JSON 字符串。</returns>
            public static string ToJson(object obj)
            {
                if (_jsonHelper == null) throw new FuException("JSON 辅助器未设置.");

                try
                {
                    return _jsonHelper.ToJson(obj);
                }
                catch (Exception exception)
                {
                    if (exception is FuException) throw;
                    throw new FuException(Text.Format("无法转换为 JSON 并出现异常 '{0}'.", exception), exception);
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
                if (_jsonHelper == null) throw new FuException("JSON 辅助器未设置.");

                try
                {
                    return _jsonHelper.ToObject<T>(json);
                }
                catch (Exception exception)
                {
                    if (exception is FuException) throw;
                    throw new FuException(Text.Format("无法转换为 JSON 并出现异常 '{0}'.", exception), exception);
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
                if (_jsonHelper == null) throw new FuException("JSON 辅助器未设置.");
                if (objectType   == null) throw new FuException("目标对象类型为空.");

                try
                {
                    return _jsonHelper.ToObject(objectType, json);
                }
                catch (Exception exception)
                {
                    if (exception is FuException) throw;
                    throw new FuException(Text.Format("无法转换为 JSON 并出现异常 '{0}'.", exception), exception);
                }
            }
        }
    }
}