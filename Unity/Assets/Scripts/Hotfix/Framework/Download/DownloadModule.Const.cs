// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Download
{
	/// <summary>
	/// 下载管理模块——常量分部：下载运行时参数（单一事实源）。
	/// </summary>
	public sealed partial class DownloadModule
	{
		#region 任务参数

		/// <summary>
		/// 默认下载任务优先级。
		/// </summary>
		internal const int DEFAULT_PRIORITY = 0;

		/// <summary>
		/// 下载代理辅助器个数。
		/// </summary>
		private const int DOWNLOAD_AGENT_HELPER_COUNT = 3;

		/// <summary>
		/// 下载超时（秒）：OnInit 写入 Timeout 属性的初始值。
		/// </summary>
		internal const float TIMEOUT_SECONDS = 30f;

		/// <summary>
		/// 将缓冲区写入磁盘的临界大小（字节）：1 MiB，OnInit 写入 FlushSize 属性的初始值。
		/// </summary>
		internal const int FLUSH_SIZE_BYTES = 1024 * 1024;

		#endregion

		#region 网络与缓冲

		/// <summary>
		/// HTTP 416 Range Not Satisfiable：断点续传时请求范围超出文件长度即此码，据此判定"文件已完整"。
		/// </summary>
		internal const int RANGE_NOT_SATISFIABLE_ERROR_CODE = 416;

		/// <summary>
		/// 下载辅助器收发缓存字节数组长度（4 KiB）。
		/// </summary>
		internal const int CACHED_BYTES_LENGTH = 1024 * 4;

		#endregion
	}
}
