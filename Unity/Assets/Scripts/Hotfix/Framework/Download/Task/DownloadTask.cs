using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Download
{
	/// <summary>
	/// 下载任务（任务池任务基类派生）：存储一次下载的序列编号/状态/存放路径/原始地址；
	/// 写盘临界与超时为模块级固定参数，代理处直取 DownloadModule 常量。
	/// </summary>
	internal sealed class DownloadTask : TaskBase
	{
		/// 下载任务的序列编号
		private static int s_serial;

		/// <summary>
		/// 获取或设置下载任务的状态。
		/// </summary>
		public DownloadTaskStatus Status { get; set; } = DownloadTaskStatus.Todo;

		/// <summary>
		/// 获取下载后存放全路径。
		/// </summary>
		public string DownloadedFullPath { get; private set; }

		/// <summary>
		/// 获取原始下载地址。
		/// </summary>
		public string DownloadUri { get; private set; }

		/// <summary>
		/// 获取下载任务的描述。
		/// </summary>
		public override string Description => DownloadedFullPath;

		/// <summary>
		/// 清理下载任务。
		/// </summary>
		public override void Clear()
		{
			base.Clear();

			DownloadedFullPath = null;
			DownloadUri        = null;
			Status             = DownloadTaskStatus.Todo;
		}

		/// <summary>
		/// 创建下载任务。
		/// </summary>
		/// <param name="downloadedFullPath">下载后存放全路径。</param>
		/// <param name="downloadUri">原始下载地址。</param>
		/// <param name="tag">下载任务的标签。</param>
		/// <param name="priority">下载任务的优先级。</param>
		/// <param name="userData">用户自定义数据。</param>
		/// <returns>创建的下载任务。</returns>
		public static DownloadTask Create(string downloadedFullPath, string downloadUri, string tag, int priority, object userData)
		{
			var downloadTask = ReferencePool.Acquire<DownloadTask>();
			downloadTask.Initialize(++s_serial, tag, priority, userData);
			downloadTask.DownloadedFullPath = downloadedFullPath;
			downloadTask.DownloadUri        = downloadUri;
			return downloadTask;
		}
	}
}