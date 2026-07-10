// ReSharper disable once CheckNamespace
namespace Hotfix.Web
{
    /// <summary>
    /// Web缓冲区结果类。
    /// 功能：
    ///     1.用于封装Web请求的缓冲区结果，包含用户自定义数据和请求结果。
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

        public WebBufferResult(object userData, byte[] result)
        {
            UserData = userData;
            Result   = result;
        }
    }
}
