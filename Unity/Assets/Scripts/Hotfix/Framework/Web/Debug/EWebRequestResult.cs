// ReSharper disable once CheckNamespace

namespace Hotfix.Framework.Web
{
    /// <summary>
    /// Web 请求的终结结果类型，用于调试面板展示最近请求的结局。
    /// </summary>
    public enum EWebRequestResult
    {
        /// <summary>
        /// 请求成功。
        /// </summary>
        Success = 0,

        /// <summary>
        /// 请求超时。
        /// </summary>
        Timeout = 1,

        /// <summary>
        /// 请求失败（网络错误 / 构建请求异常等）。
        /// </summary>
        Failed = 2,

        /// <summary>
        /// 请求被取消（调用方取消 / 模块销毁 / 调试面板急救取消）。
        /// </summary>
        Canceled = 3,
    }
}