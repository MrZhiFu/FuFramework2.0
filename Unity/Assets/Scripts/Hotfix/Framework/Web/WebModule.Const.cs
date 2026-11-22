// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Web
{
	/// <summary>
	/// Web 管理模块——常量分部：请求协议运行时参数（单一事实源）。
	/// </summary>
	public partial class WebModule
	{
		#region 请求

		/// <summary>
		/// Pb 请求体 MIME 类型：请求头 Content-Type 固定取此值，服务器据此走 protobuf 解码。
		/// </summary>
		private const string PB_CONTENT_TYPE = "application/x-protobuf";

		#endregion
	}
}
