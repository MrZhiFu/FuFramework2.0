using System;
using System.IO;
using Hotfix.Framework.Core;
using Hotfix.Framework.Event;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Download
{
	/// <summary>
	/// 下载代理。
	/// 功能：
	///     1. 使用下载帮助类UnityWebRequest下载文件。
	///     2. 监听下载代理辅助器的下载进度相关事件。
	/// </summary>
	internal sealed class DownloadAgent : ITaskAgent<DownloadTask>, IDisposable
	{
		/// 下载代理辅助器
		private readonly UnityWebRequestDownloadAgentHelper m_helper;

		/// 下载文件流
		private FileStream m_fileStream;

		/// 等待刷新的大小(将缓冲区写入磁盘的临界大小)
		private int m_waitFlushSize;

		/// 是否已销毁
		private bool m_disposed;

		/// 事件管理模块
		private readonly EventModule m_eventModule = ModuleManager.GetModule<EventModule>();


		/// 下载开始委托
		public Action<DownloadAgent> m_DownloadAgentStart;

		/// 下载更新委托
		public Action<DownloadAgent, int> m_DownloadAgentUpdate;

		/// 下载成功委托
		public Action<DownloadAgent, long> m_DownloadAgentSuccess;

		/// 下载失败委托
		public Action<DownloadAgent, string> m_DownloadAgentFailure;


		/// <summary>
		/// 构造下载代理的新实例。
		/// </summary>
		/// <param name="downloadAgentHelper">下载代理辅助器。</param>
		public DownloadAgent(UnityWebRequestDownloadAgentHelper downloadAgentHelper)
		{
			m_helper = downloadAgentHelper ?? throw new InvalidOperationException("[DownloadAgent]下载代理辅助器为空!");

			Task             = null;
			m_fileStream     = null;
			m_waitFlushSize  = 0;
			WaitTime         = 0f;
			StartLength      = 0L;
			DownloadedLength = 0L;
			SavedLength      = 0L;
			m_disposed       = false;

			m_DownloadAgentStart   = null;
			m_DownloadAgentUpdate  = null;
			m_DownloadAgentSuccess = null;
			m_DownloadAgentFailure = null;
		}

		/// <summary>
		/// 获取下载任务。
		/// </summary>
		public DownloadTask Task { get; private set; }

		/// <summary>
		/// 获取已经等待时间。
		/// </summary>
		public float WaitTime { get; private set; }

		/// <summary>
		/// 获取开始下载时已经存在的大小。
		/// </summary>
		public long StartLength { get; private set; }

		/// <summary>
		/// 获取本次已经下载的大小。
		/// </summary>
		public long DownloadedLength { get; private set; }

		/// <summary>
		/// 获取当前的大小。
		/// </summary>
		public long CurrentLength => StartLength + DownloadedLength;

		/// <summary>
		/// 获取已经存盘的大小。
		/// </summary>
		public long SavedLength { get; private set; }

		/// <summary>
		/// 初始化下载代理。
		/// </summary>
		public void Initialize()
		{
			m_eventModule.Subscribe(DownloadAgentHelperUpdateBytesEventArgs.EventId,  _OnDownloadAgentHelperUpdateBytes);
			m_eventModule.Subscribe(DownloadAgentHelperUpdateLengthEventArgs.EventId, _OnDownloadAgentHelperUpdateLength);
			m_eventModule.Subscribe(DownloadAgentHelperCompleteEventArgs.EventId,     _OnDownloadAgentHelperComplete);
			m_eventModule.Subscribe(DownloadAgentHelperErrorEventArgs.EventId,        _OnDownloadAgentHelperError);
		}

		/// <summary>
		/// 下载代理轮询。
		/// </summary>
		/// <param name="deltaTime">逻辑帧间隔流逝时间，以秒为单位。</param>
		/// <param name="unscaledDeltaTime">无时间缩放的真实帧间隔流逝时间，以秒为单位。</param>
		public void Update(float deltaTime, float unscaledDeltaTime)
		{
			m_helper.OnUpdate();

			// 检查Task是否为null，避免空引用异常
			if (Task == null || Task.Status != DownloadTaskStatus.Doing) return;

			WaitTime += unscaledDeltaTime;
			if (WaitTime < Task.Timeout) return;

			// 调用下载代理辅助器错误事件
			var downloadAgentHelperErrorEventArgs = DownloadAgentHelperErrorEventArgs.Create(false, "Timeout");
			try
			{
				_OnDownloadAgentHelperError(this, downloadAgentHelperErrorEventArgs);
			}
			finally
			{
				// _OnDownloadAgentHelperError 内部会 File.Delete 损坏的 .download 文件、并回调
				// DownloadAgentFailure（失败事件订阅者也可能抛异常），任一抛出都会跳过回收；
				// 以 finally 保证错误事件参数必回引用池，不让异常逃逸时泄漏池对象。
				ReferencePool.Recycle(downloadAgentHelperErrorEventArgs);
			}
		}

		/// <summary>
		/// 关闭并清理下载代理。
		/// </summary>
		public void Shutdown()
		{
			Dispose();

			m_eventModule.Unsubscribe(DownloadAgentHelperUpdateBytesEventArgs.EventId,  _OnDownloadAgentHelperUpdateBytes);
			m_eventModule.Unsubscribe(DownloadAgentHelperUpdateLengthEventArgs.EventId, _OnDownloadAgentHelperUpdateLength);
			m_eventModule.Unsubscribe(DownloadAgentHelperCompleteEventArgs.EventId,     _OnDownloadAgentHelperComplete);
			m_eventModule.Unsubscribe(DownloadAgentHelperErrorEventArgs.EventId,        _OnDownloadAgentHelperError);
		}

		/// <summary>
		/// 开始处理下载任务。
		/// </summary>
		/// <param name="task">要处理的下载任务。</param>
		/// <returns>开始处理任务的状态。</returns>
		public EStartTaskStatus Start(DownloadTask task)
		{
			Task = task ?? throw new InvalidOperationException("[DownloadAgent] 任务不能为空.");

			Task.Status = DownloadTaskStatus.Doing;
			var downloadFile = $"{Task.DownloadedFullPath}.download";

			try
			{
				if (File.Exists(downloadFile))
				{
					m_fileStream = File.OpenWrite(downloadFile);
					m_fileStream.Seek(0L, SeekOrigin.End);
					StartLength      = SavedLength = m_fileStream.Length;
					DownloadedLength = 0L;
				}
				else
				{
					var directory = Path.GetDirectoryName(Task.DownloadedFullPath);
					if (directory != null && !Directory.Exists(directory))
						Directory.CreateDirectory(directory);

					m_fileStream = new FileStream(downloadFile, FileMode.Create, FileAccess.Write);
					StartLength  = SavedLength = DownloadedLength = 0L;
				}

				m_DownloadAgentStart?.Invoke(this);

				// 使用帮助类开始下载
				if (StartLength > 0L)
					m_helper.Download(Task.DownloadUri, StartLength); // 断点续传
				else
					m_helper.Download(Task.DownloadUri); // 全新下载

				return EStartTaskStatus.CanResume;
			}
			catch (Exception exception)
			{
				var downloadAgentHelperErrorEventArgs = DownloadAgentHelperErrorEventArgs.Create(false, exception.ToString());
				try
				{
					_OnDownloadAgentHelperError(this, downloadAgentHelperErrorEventArgs);
				}
				finally
				{
					// 同 Update：错误回调内的 File.Delete / 失败事件订阅者可能抛异常，finally 保证参数回收
					ReferencePool.Recycle(downloadAgentHelperErrorEventArgs);
				}

				return EStartTaskStatus.UnknownError;
			}
		}

		/// <summary>
		/// 重置下载代理。
		/// </summary>
		public void Reset()
		{
			m_helper.Reset();

			if (m_fileStream != null)
			{
				m_fileStream.Close();
				m_fileStream = null;
			}

			Task             = null;
			m_waitFlushSize  = 0;
			WaitTime         = 0f;
			StartLength      = 0L;
			DownloadedLength = 0L;
			SavedLength      = 0L;
		}

		/// <summary>
		/// 释放资源。
		/// </summary>
		public void Dispose()
		{
			if (m_disposed) return;

			if (m_fileStream != null)
			{
				m_fileStream.Dispose();
				m_fileStream = null;
			}

			m_disposed = true;

			// ReSharper disable once GCSuppressFinalizeForTypeWithoutDestructor
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// 判断事件是否来自本代理自己的下载辅助器。
		/// 四个下载辅助器事件的 Id 是共享的（多播语义），多个下载代理订阅了同一 Id，
		/// 一次 Broadcast 会分发给全部代理；若不过滤 sender，任一辅助器的数据/完成/错误事件
		/// 会被所有代理处理 → 并发下载时串写文件、并在任一任务完成时误完成他人任务。
		/// sender 为 <see cref="DownloadHandler"/> 时须属于本代理的 m_helper；
		/// sender 为 this（代理内部超时/异常路径的直接调用）则放行。
		/// </summary>
		private bool IsOwnEvent(object sender)
		{
			// 内部超时/异常路径：代理由自身直接调用回调
			if (ReferenceEquals(sender, this)) return true;

			// 完成/错误事件：由 UnityWebRequestDownloadAgentHelper.OnUpdate 以 helper 自身为 sender 广播
			//（该 helper 并不继承 DownloadHandler，故必须单独判定，否则完成/失败会被拦死、下载永不成功）
			if (ReferenceEquals(sender, m_helper)) return true;

			// 数据/长度事件：由 helper 持有的 DownloadHandler 在其 ReceiveData 中以 handler 自身为 sender 广播
			return sender is DownloadHandler handler && ReferenceEquals(handler.Owner, m_helper);
		}

		/// <summary>
		/// 下载代理辅助器更新数据流事件回调。
		/// </summary>
		private void _OnDownloadAgentHelperUpdateBytes(object sender, GameEventArgs eventArgs)
		{
			if (!IsOwnEvent(sender)) return;
			if (eventArgs is not DownloadAgentHelperUpdateBytesEventArgs e) return;

			WaitTime = 0f;

			try
			{
				// 检查Task是否为null，避免空引用异常
				if (Task == null) return;

				m_fileStream.Write(e.GetBytes(), e.Offset, e.Length);
				m_waitFlushSize += e.Length;
				SavedLength     += e.Length;

				if (m_waitFlushSize >= Task.FlushSize)
				{
					m_fileStream.Flush();
					m_waitFlushSize = 0;
				}
			}
			catch (Exception exception)
			{
				var downloadAgentHelperErrorEventArgs = DownloadAgentHelperErrorEventArgs.Create(false, exception.ToString());
				try
				{
					_OnDownloadAgentHelperError(this, downloadAgentHelperErrorEventArgs);
				}
				finally
				{
					// 同 Update：错误回调内的 File.Delete / 失败事件订阅者可能抛异常，finally 保证参数回收
					ReferencePool.Recycle(downloadAgentHelperErrorEventArgs);
				}
			}
		}

		/// <summary>
		/// 下载代理辅助器更新数据大小事件回调。
		/// </summary>
		private void _OnDownloadAgentHelperUpdateLength(object sender, GameEventArgs gameEventArgs)
		{
			if (!IsOwnEvent(sender)) return;
			if (gameEventArgs is not DownloadAgentHelperUpdateLengthEventArgs e) return;

			// 检查Task是否为null，避免空引用异常
			if (Task == null) return;

			WaitTime         =  0f;
			DownloadedLength += e.DeltaLength;
			m_DownloadAgentUpdate?.Invoke(this, e.DeltaLength);
		}

		/// <summary>
		/// 下载代理辅助器完成事件回调。
		/// </summary>
		private void _OnDownloadAgentHelperComplete(object sender, GameEventArgs gameEventArgs)
		{
			if (!IsOwnEvent(sender)) return;
			if (gameEventArgs is not DownloadAgentHelperCompleteEventArgs e) return;

			// 检查Task是否为null，避免空引用异常
			if (Task == null) return;

			WaitTime         = 0f;
			DownloadedLength = e.Length;

			if (SavedLength != CurrentLength)
				throw new InvalidOperationException("[DownloadAgent] 已存储的大小和当前大小不一致");

			m_helper.Reset();
			m_fileStream.Close();
			m_fileStream = null;

			if (File.Exists(Task.DownloadedFullPath))
				File.Delete(Task.DownloadedFullPath);

			File.Move($"{Task.DownloadedFullPath}.download", Task.DownloadedFullPath);

			Task.Status = DownloadTaskStatus.Done;
			Task.Done   = true;

			// 只在Task不为null时触发成功事件
			m_DownloadAgentSuccess?.Invoke(this, e.Length);
		}

		/// <summary>
		/// 下载代理辅助器错误事件回调。
		/// </summary>
		private void _OnDownloadAgentHelperError(object sender, GameEventArgs gameEventArgs)
		{
			if (!IsOwnEvent(sender)) return;
			if (gameEventArgs is not DownloadAgentHelperErrorEventArgs e) return;

			m_helper.Reset();
			if (m_fileStream != null)
			{
				m_fileStream.Close();
				m_fileStream = null;
			}

			// 检查Task是否为null，避免空引用异常
			if (Task != null)
			{
				if (e.DeleteDownloading)
					File.Delete($"{Task.DownloadedFullPath}.download");

				Task.Status = DownloadTaskStatus.Error;
				Task.Done   = true;

				// 只在Task不为null时触发失败事件
				m_DownloadAgentFailure?.Invoke(this, e.ErrorMessage);
			}
		}
	}
}