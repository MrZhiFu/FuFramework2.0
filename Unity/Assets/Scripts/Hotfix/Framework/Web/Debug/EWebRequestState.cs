// ReSharper disable once CheckNamespace

namespace Hotfix.Framework.Web
{
    /// <summary>
    /// Web 请求的实时状态，用于调试面板展示当前在队列中或发送中的请求。
    /// </summary>
    public enum EWebRequestState
    {
        /// <summary>
        /// 正在等待队列中排队，尚未发送。
        /// </summary>
        Waiting = 0,

        /// <summary>
        /// 正在发送（已占用并发槽位，等待服务器响应）。
        /// </summary>
        Sending = 1,
    }
}