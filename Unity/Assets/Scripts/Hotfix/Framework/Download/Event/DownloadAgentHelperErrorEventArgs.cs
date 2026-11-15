using Hotfix.Framework.Event;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Download
{
	/// <summary>
	/// 下载代理辅助器错误事件。
	/// </summary>
	public sealed class DownloadAgentHelperErrorEventArgs : GameEventArgs
	{


		/// <summary>
		/// 事件来源（广播方注入）：sender 退场后并发多播场景按此字段区分「谁发的」
		/// （DownloadAgent.IsOwnEvent 据此过滤本代理的辅助器事件）。
		/// </summary>
		public object Source { get; private set; }

		/// <summary>
		/// 获取是否需要删除正在下载的文件。
		/// </summary>
		public bool DeleteDownloading { get; private set; }

		/// <summary>
		/// 获取错误信息。
		/// </summary>
		public string ErrorMessage { get; private set; }

		/// <summary>
		/// 清理下载代理辅助器错误事件。
		/// </summary>
		public override void Clear()
		{
			Source            = null;
			DeleteDownloading = false;
			ErrorMessage      = null;
		}

		/// <summary>
		/// 创建下载代理辅助器错误事件。
		/// </summary>
		/// <param name="source">事件来源（广播方：UnityWebRequestDownloadAgentHelper 或代理自身）。</param>
		/// <param name="deleteDownloading">是否需要删除正在下载的文件。</param>
		/// <param name="errorMessage">错误信息。</param>
		/// <returns>创建的下载代理辅助器错误事件。</returns>
		public static DownloadAgentHelperErrorEventArgs Create(object source, bool deleteDownloading, string errorMessage)
		{
			var downloadAgentHelperErrorEventArgs = ReferencePool.Acquire<DownloadAgentHelperErrorEventArgs>();
			downloadAgentHelperErrorEventArgs.Source            = source;
			downloadAgentHelperErrorEventArgs.DeleteDownloading = deleteDownloading;
			downloadAgentHelperErrorEventArgs.ErrorMessage      = errorMessage;
			return downloadAgentHelperErrorEventArgs;
		}
	}
}