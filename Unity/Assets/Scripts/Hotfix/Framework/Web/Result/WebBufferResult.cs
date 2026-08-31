// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Web
{
    /// <summary>
    /// Web 字节数组结果类。
    /// 功能：
    ///     1.用于封装 Web 请求的字节数组结果，包含用户自定义数据和请求结果。
    /// </summary>
    public sealed class WebBufferResult
    {
        /// <summary>
        /// 请求结果
        /// </summary>
        public byte[] Result { get; }

        /// <summary>
        /// 用户自定义数据
        /// </summary>
        public object UserData { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="userData">用户自定义数据</param>
        /// <param name="result">请求结果</param>
        public WebBufferResult(object userData, byte[] result)
        {
            UserData = userData;
            Result   = result;
        }
    }
}
