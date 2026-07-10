using System;
using UnityEngine;
using UnityEngine.Networking;
using FuFramework.Core.Runtime;
using FuFramework.Event.Runtime;

namespace Hotfix.Download
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
    public sealed partial class UnityWebRequestDownloadAgentHelper
    {
        /// <summary>
        /// 范围不适用错误码。
        /// </summary>
        private const int RangeNotSatisfiableErrorCode = 416;

        /// <summary>
        /// 缓存目标数据的字节数组的长度。
        /// </summary>
        /// <remarks>
        /// 0x1000 = 4096
        /// </remarks>
        private const int CachedBytesLength = 0x1000;

        /// <summary>
        /// 缓存目标数据的字节数组。
        /// </summary>
        private readonly byte[] m_CachedBytes = new byte[CachedBytesLength];

        /// <summary>
        /// 记录是否已销毁。
        /// </summary>
        internal bool m_Disposed;

        /// <summary>
        /// Unity WebRequest。
        /// </summary>
        private UnityWebRequest m_UnityWebRequest;

        /// <summary>
        /// 事件管理模块。
        /// </summary>
        private readonly EventModule m_EventModule = ModuleManager.GetModule<EventModule>();

        /// <summary>
        /// 轮询更新。
        /// </summary>
        public void OnUpdate()
        {
            if (m_UnityWebRequest == null) return;
            if (!m_UnityWebRequest.isDone) return;

            var isError = m_UnityWebRequest.result != UnityWebRequest.Result.Success;
            if (isError)
            {
                var downloadAgentHelperErrorEventArgs = DownloadAgentHelperErrorEventArgs.Create(m_UnityWebRequest.responseCode == RangeNotSatisfiableErrorCode, m_UnityWebRequest.error);
                m_EventModule.Broadcast(this, downloadAgentHelperErrorEventArgs);
            }
            else
            {
                var downloadAgentHelperCompleteEventArgs = DownloadAgentHelperCompleteEventArgs.Create((long)m_UnityWebRequest.downloadedBytes);
                m_EventModule.Broadcast(this, downloadAgentHelperCompleteEventArgs);
            }
        }

        /// <summary>
        /// 通过下载代理辅助器下载指定地址的数据。
        /// </summary>
        /// <param name="downloadUri">下载地址。</param>
        public void Download(string downloadUri)
        {
            m_UnityWebRequest                    = new UnityWebRequest(downloadUri);
            m_UnityWebRequest.certificateHandler = new DownloadCertificateHandler();
            m_UnityWebRequest.downloadHandler    = new DownloadHandler(this);
            m_UnityWebRequest.SendWebRequest();
        }

        /// <summary>
        /// 通过下载代理辅助器下载指定地址的数据。
        /// </summary>
        /// <param name="downloadUri">下载地址。</param>
        /// <param name="fromPosition">下载数据起始位置。</param>
        public void Download(string downloadUri, long fromPosition)
        {
            m_UnityWebRequest                    = new UnityWebRequest(downloadUri);
            m_UnityWebRequest.certificateHandler = new DownloadCertificateHandler();
            m_UnityWebRequest.downloadHandler    = new DownloadHandler(this);
            m_UnityWebRequest.SetRequestHeader("Range", $"bytes={fromPosition}");
            m_UnityWebRequest.SendWebRequest();
        }

        /// <summary>
        /// 通过下载代理辅助器下载指定地址的数据。
        /// </summary>
        /// <param name="downloadUri">下载地址。</param>
        /// <param name="fromPosition">下载数据起始位置。</param>
        /// <param name="toPosition">下载数据结束位置。</param>
        public void Download(string downloadUri, long fromPosition, long toPosition)
        {
            m_UnityWebRequest                    = new UnityWebRequest(downloadUri);
            m_UnityWebRequest.certificateHandler = new DownloadCertificateHandler();
            m_UnityWebRequest.SetRequestHeader("Range", $"bytes={fromPosition}-{toPosition}");
            m_UnityWebRequest.downloadHandler = new DownloadHandler(this);
            m_UnityWebRequest.SendWebRequest();
        }

        /// <summary>
        /// 重置下载代理辅助器。
        /// </summary>
        public void Reset()
        {
            if (m_UnityWebRequest != null)
            {
                m_UnityWebRequest.Abort();
                m_UnityWebRequest.Dispose();
                m_UnityWebRequest = null;
            }

            Array.Clear(m_CachedBytes, 0, CachedBytesLength);
        }

        /// <summary>
        /// 释放资源。
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// 释放资源。
        /// </summary>
        /// <param name="disposing">释放资源标记。</param>
        private void Dispose(bool disposing)
        {
            if (m_Disposed) return;

            if (disposing && m_UnityWebRequest != null)
            {
                m_UnityWebRequest.Dispose();
                m_UnityWebRequest = null;
            }

            m_Disposed = true;
        }
    }
}
