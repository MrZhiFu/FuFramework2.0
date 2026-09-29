using System;
using UnityEngine.Networking;
using Hotfix.Framework.Core;
using Hotfix.Framework.Event;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Download
{
	/// <summary>
	/// 下载证书验证处理器。目前不做任何处理，直接返回true。
	/// </summary>
	public sealed class DownloadCertificateHandler : CertificateHandler
	{
		protected override bool ValidateCertificate(byte[] certificateData)
		{
			// return base.ValidateCertificate(certificateData);
			return true;
		}
	}

	/// <summary>
	/// 使用 UnityWebRequest 实现的下载代理辅助器。
	/// 功能：
	///     1. 用于下载指定地址的数据。
	///     2. 支持断点续传。
	///     3. 提供下载进度事件和下载完成事件。
	/// </summary>
	public sealed class UnityWebRequestDownloadAgentHelper
	{
		/// <summary>
		/// 范围不适用错误码。
		/// </summary>
		private const int RANGE_NOT_SATISFIABLE_ERROR_CODE = 416;

		/// <summary>
		/// 缓存目标数据的字节数组的长度。
		/// </summary>
		/// <remarks>
		/// 0x1000 = 4096
		/// </remarks>
		private const int CACHED_BYTES_LENGTH = 0x1000;

		/// <summary>
		/// 缓存目标数据的字节数组。
		/// </summary>
		internal readonly byte[] m_cachedBytes = new byte[CACHED_BYTES_LENGTH];

		/// <summary>
		/// 记录是否已销毁。
		/// </summary>
		internal bool m_disposed;

		/// <summary>
		/// Unity WebRequest。
		/// </summary>
		internal UnityWebRequest m_unityWebRequest;

		/// <summary>
		/// 事件管理模块。
		/// </summary>
		private readonly EventModule m_eventModule = ModuleManager.GetModule<EventModule>();

		/// <summary>
		/// 轮询更新。
		/// </summary>
		public void OnUpdate()
		{
			if (m_unityWebRequest == null) return;
			if (!m_unityWebRequest.isDone) return;

			var isError = m_unityWebRequest.result != UnityWebRequest.Result.Success;
			if (isError)
			{
				var downloadAgentHelperErrorEventArgs = DownloadAgentHelperErrorEventArgs.Create(m_unityWebRequest.responseCode == RANGE_NOT_SATISFIABLE_ERROR_CODE, m_unityWebRequest.error);
				m_eventModule.Broadcast(this, downloadAgentHelperErrorEventArgs);
			}
			else
			{
				var downloadAgentHelperCompleteEventArgs = DownloadAgentHelperCompleteEventArgs.Create((long)m_unityWebRequest.downloadedBytes);
				m_eventModule.Broadcast(this, downloadAgentHelperCompleteEventArgs);
			}
		}

		/// <summary>
		/// 通过下载代理辅助器下载指定地址的数据。
		/// </summary>
		/// <param name="downloadUri">下载地址。</param>
		public void Download(string downloadUri)
		{
			m_unityWebRequest                    = new UnityWebRequest(downloadUri);
			m_unityWebRequest.certificateHandler = new DownloadCertificateHandler();
			m_unityWebRequest.downloadHandler    = new DownloadHandler(this);
			m_unityWebRequest.SendWebRequest();
		}

		/// <summary>
		/// 通过下载代理辅助器下载指定地址的数据。
		/// </summary>
		/// <param name="downloadUri">下载地址。</param>
		/// <param name="fromPosition">下载数据起始位置。</param>
		public void Download(string downloadUri, long fromPosition)
		{
			m_unityWebRequest                    = new UnityWebRequest(downloadUri);
			m_unityWebRequest.certificateHandler = new DownloadCertificateHandler();
			m_unityWebRequest.downloadHandler    = new DownloadHandler(this);
			m_unityWebRequest.SetRequestHeader("Range", $"bytes={fromPosition}");
			m_unityWebRequest.SendWebRequest();
		}

		/// <summary>
		/// 通过下载代理辅助器下载指定地址的数据。
		/// </summary>
		/// <param name="downloadUri">下载地址。</param>
		/// <param name="fromPosition">下载数据起始位置。</param>
		/// <param name="toPosition">下载数据结束位置。</param>
		public void Download(string downloadUri, long fromPosition, long toPosition)
		{
			m_unityWebRequest                    = new UnityWebRequest(downloadUri);
			m_unityWebRequest.certificateHandler = new DownloadCertificateHandler();
			m_unityWebRequest.SetRequestHeader("Range", $"bytes={fromPosition}-{toPosition}");
			m_unityWebRequest.downloadHandler = new DownloadHandler(this);
			m_unityWebRequest.SendWebRequest();
		}

		/// <summary>
		/// 重置下载代理辅助器。
		/// </summary>
		public void Reset()
		{
			if (m_unityWebRequest != null)
			{
				m_unityWebRequest.Abort();
				m_unityWebRequest.Dispose();
				m_unityWebRequest = null;
			}

			Array.Clear(m_cachedBytes, 0, CACHED_BYTES_LENGTH);
		}

		/// <summary>
		/// 释放资源。
		/// </summary>
		public void Dispose() => Dispose(true);

		/// <summary>
		/// 释放资源。
		/// </summary>
		/// <param name="disposing">释放资源标记。</param>
		private void Dispose(bool disposing)
		{
			if (m_disposed) return;

			if (disposing && m_unityWebRequest != null)
			{
				m_unityWebRequest.Dispose();
				m_unityWebRequest = null;
			}

			m_disposed = true;
		}
	}
}