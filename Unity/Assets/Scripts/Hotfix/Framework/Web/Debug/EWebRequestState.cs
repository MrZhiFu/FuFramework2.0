// 本文件整体仅 Editor 编译（发布构建不存在）：内容为 Web 调试面板使用的数据结构，
// 主调用方 WebModule.Debug.cs 已整文件包裹，运行时构建无引用。
#if UNITY_EDITOR
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
#endif
