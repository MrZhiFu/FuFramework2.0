using System;
using Hotfix.Framework.Core;
using Hotfix.Framework.Event;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Download
{
	/// <summary>
	/// 下载代理辅助器更新数据大小事件。
	/// </summary>
	public sealed class DownloadAgentHelperUpdateLengthEventArgs : GameEventArgs
	{


		/// <summary>
		/// 事件来源（广播方注入）：sender 退场后并发多播场景按此字段区分「谁发的」
		/// （DownloadAgent.IsOwnEvent 据此过滤本代理的辅助器事件）。
		/// </summary>
		public object Source { get; private set; }

		/// <summary>
		/// 获取下载的增量数据大小。
		/// </summary>
		public int DeltaLength { get; private set; }

		/// <summary>
		/// 清理下载代理辅助器更新数据大小事件。
		/// </summary>
		public override void Clear()
		{
			Source      = null;
			DeltaLength = 0;
		}

		/// <summary>
		/// 创建下载代理辅助器更新数据大小事件。
		/// </summary>
		/// <param name="source">事件来源（广播方：DownloadHandler 或代理自身）。</param>
		/// <param name="deltaLength">下载的增量数据大小。</param>
		/// <returns>创建的下载代理辅助器更新数据大小事件。</returns>
		public static DownloadAgentHelperUpdateLengthEventArgs Create(object source, int deltaLength)
		{
			if (deltaLength <= 0) throw new InvalidOperationException("下载的增量数据大小必须大于0.");

			var downloadAgentHelperUpdateLengthEventArgs = ReferencePool.Acquire<DownloadAgentHelperUpdateLengthEventArgs>();
			downloadAgentHelperUpdateLengthEventArgs.Source      = source;
			downloadAgentHelperUpdateLengthEventArgs.DeltaLength = deltaLength;
			return downloadAgentHelperUpdateLengthEventArgs;
		}
	}
}