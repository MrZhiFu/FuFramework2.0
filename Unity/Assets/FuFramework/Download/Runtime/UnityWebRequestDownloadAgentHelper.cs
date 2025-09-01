using System;
using UnityEngine;
using UnityEngine.Networking;
using FuFramework.Core.Runtime;
using FuFramework.Event.Runtime;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable once CheckNamespace
namespace FuFramework.Download.Runtime
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
    /// </summary>
    public partial class UnityWebRequestDownloadAgentHelper : MonoBehaviour
    {
        /// 范围不适用错误码。
        private const int RangeNotSatisfiableErrorCode = 416;
        
        /// 缓存目标数据的字节数组的长度(0x1000 = 4096)
        private const int CachedBytesLength = 0x1000;
        

        /// 缓存目标数据的字节数组
        private readonly byte[] m_CachedBytes = new byte[CachedBytesLength];

        /// 记录是否已销毁
        private bool m_Disposed;

        /// Unity WebRequest
        private UnityWebRequest m_UnityWebRequest;
        
        /// 事件管理器
        private readonly EventManager m_EventManager = ModuleManager.GetModule<EventManager>();

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
                m_EventManager.Fire(this, downloadAgentHelperErrorEventArgs);
            }
            else
            {
                var downloadAgentHelperCompleteEventArgs = DownloadAgentHelperCompleteEventArgs.Create((long)m_UnityWebRequest.downloadedBytes);
                m_EventManager.Fire(this, downloadAgentHelperCompleteEventArgs);
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
            m_UnityWebRequest.SetRequestHeader("Range", Utility.Text.Format("bytes={0}-", fromPosition));
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
            m_UnityWebRequest.SetRequestHeader("Range", Utility.Text.Format("bytes={0}-{1}", fromPosition, toPosition));
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
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源。
        /// </summary>
        /// <param name="disposing">释放资源标记。</param>
        protected virtual void Dispose(bool disposing)
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