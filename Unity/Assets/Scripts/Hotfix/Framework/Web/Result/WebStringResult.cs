// ReSharper disable once CheckNamespace

namespace Hotfix.Framework.Web
{
    /// <summary>
    /// Web 字符串请求结果类，用于封装 HTTP 请求返回的字符串数据。
    /// </summary>
    public sealed class WebStringResult
    {
        /// <summary>
        /// 请求返回的字符串结果。
        /// </summary>
        public string Result { get; }

        /// <summary>
        /// 用户自定义数据，在请求时传入的数据会原样返回。
        /// </summary>
        public object UserData { get; }

        /// <summary>
        /// 将请求结果转换为字符串表示形式。
        /// </summary>
        /// <returns>格式化的结果字符串，如 "[Result]:xxx"。</returns>
        public override string ToString() => $"[Result]:{Result}";

        /// <summary>
        /// 初始化 Web 字符串请求结果。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="result">请求返回的字符串结果。</param>
        public WebStringResult(object userData, string result)
        {
            UserData = userData;
            Result   = result;
        }
    }
}